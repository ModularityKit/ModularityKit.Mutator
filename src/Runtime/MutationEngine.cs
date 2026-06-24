using System.Diagnostics;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Interception;
using ModularityKit.Mutator.Abstractions.Metrics;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime.Internal;
using ModularityExecutionContext = ModularityKit.Mutator.Abstractions.Context.ExecutionContext;

namespace ModularityKit.Mutator.Runtime;

internal sealed class MutationEngine(
    IMutationExecutor executor,
    IPolicyRegistry policyRegistry,
    IInterceptorPipeline interceptorPipeline,
    IMutationAuditor auditor,
    IMutationHistoryStore historyStore,
    IMetricsCollector metricsCollector,
    MutationEngineOptions options)
    : IMutationEngine
{
    private readonly IMutationExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly IPolicyRegistry _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
    private readonly IInterceptorPipeline _interceptorPipeline = interceptorPipeline ?? throw new ArgumentNullException(nameof(interceptorPipeline));
    private readonly IMutationAuditor _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
    private readonly IMutationHistoryStore _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
    private readonly IMetricsCollector _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    private readonly MutationEngineOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly MutationExecutionConcurrencyGate _concurrencyGate = CreateConcurrencyGate(options);

    public async Task<MutationResult<TState>> ExecuteAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        IMetricsScope? metricsScope = null;

        await using var executionLease = await _concurrencyGate
            .EnterAsync(mutation.Context.StateId, cancellationToken)
            .ConfigureAwait(false);

        if (_options.EnableDetailedMetrics)
            metricsScope = _metricsCollector.BeginScope(executionId);

        try
        {
            return await ExecutePipelineAsync(
                mutation,
                state,
                executionId,
                stopwatch,
                metricsScope,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await _interceptorPipeline.OnMutationFailedAsync(
                mutation.Intent,
                mutation.Context,
                state!,
                ex,
                executionId,
                cancellationToken);

            await AuditExceptionAsync(mutation, state, ex, executionId, stopwatch.Elapsed);

            throw new MutationException(
                $"Mutation execution failed: {ex.Message}",
                ex)
            {
                ExecutionId = executionId
            };
        }
        finally
        {
            metricsScope?.Dispose();
        }
    }

    private async Task<MutationResult<TState>> ExecutePipelineAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        string executionId,
        Stopwatch stopwatch,
        IMetricsScope? metricsScope,
        CancellationToken cancellationToken)
    {
        await _interceptorPipeline.OnBeforeMutationAsync(
            mutation.Intent,
            mutation.Context,
            state!,
            executionId,
            cancellationToken);

        var policyEvaluationStart = stopwatch.Elapsed;
        var policyDecision = await EvaluatePoliciesAsync(mutation, state, cancellationToken);
        metricsScope?.RecordPolicyEvaluationTime(stopwatch.Elapsed - policyEvaluationStart);

        if (!policyDecision.IsAllowed)
        {
            var policyBlockedResult = MutationResult<TState>.PolicyBlocked(policyDecision);

            await _interceptorPipeline.OnPolicyBlockedAsync(
                mutation.Intent,
                mutation.Context,
                state!,
                policyDecision,
                executionId,
                cancellationToken);

            await AuditFailureAsync(mutation, state, policyBlockedResult, executionId, stopwatch.Elapsed);

            return await FinalizeResultAsync(
                policyBlockedResult,
                state,
                executionId,
                stopwatch,
                metricsScope,
                cancellationToken);
        }

        if (mutation.Context.Mode != MutationMode.Commit || _options.AlwaysValidate)
        {
            var validationStart = stopwatch.Elapsed;
            var validation = mutation.Validate(state);
            metricsScope?.RecordValidationTime(stopwatch.Elapsed - validationStart);

            if (!validation.IsValid)
            {
                var validationFailureResult = MutationResult<TState>.Failure(validation);
                await AuditFailureAsync(mutation, state, validationFailureResult, executionId, stopwatch.Elapsed);

                return await FinalizeResultAsync(
                    validationFailureResult,
                    state,
                    executionId,
                    stopwatch,
                    metricsScope,
                    cancellationToken);
            }
        }

        var mutationResult = await ExecuteByModeAsync(mutation, state, cancellationToken, executionId);
        var totalElapsed = stopwatch.Elapsed;

        mutationResult = PolicyModificationApplier.Apply(mutationResult, policyDecision.Modifications);

        await _interceptorPipeline.OnAfterMutationAsync(
            mutation.Intent,
            mutation.Context,
            state,
            mutationResult.NewState,
            mutationResult.Changes,
            executionId,
            cancellationToken);

        await AuditSuccessAsync(
            mutation,
            state,
            mutationResult,
            policyDecision,
            executionId,
            totalElapsed);

        if (mutationResult.IsSuccess && mutation.Context.Mode == MutationMode.Commit)
        {
            await StoreInHistoryAsync(
                mutation,
                mutationResult,
                executionId,
                totalElapsed,
                cancellationToken);
        }

        return await FinalizeResultAsync(
            mutationResult,
            state,
            executionId,
            stopwatch,
            metricsScope,
            cancellationToken);
    }

    private async Task<MutationResult<TState>> FinalizeResultAsync<TState>(
        MutationResult<TState> result,
        TState state,
        string executionId,
        Stopwatch stopwatch,
        IMetricsScope? metricsScope,
        CancellationToken cancellationToken)
    {
        var totalElapsed = stopwatch.Elapsed;
        metricsScope?.RecordStateSize(StateSizeEstimator.Estimate(state));

        if (metricsScope != null)
            await _metricsCollector.RecordAsync(executionId, metricsScope.Build(), cancellationToken);

        return result with
        {
            Metrics = result.Metrics with { ExecutionTime = totalElapsed }
        };
    }

    private Task<MutationResult<TState>> ExecuteByModeAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken,
        string executionId)
    {
        var executionContext = new ModularityExecutionContext
        {
            ExecutionId = executionId,
            Timeout = _options.ExecutionTimeout,
            CancellationToken = cancellationToken
        };

        return mutation.Context.Mode switch
        {
            MutationMode.Simulate => Task.FromResult(mutation.Simulate(state)),
            MutationMode.Validate => Task.FromResult(BuildValidationOnlyResult(mutation, state)),
            _ => _executor.ExecuteAsync(
                mutation,
                state,
                executionContext,
                cancellationToken)
        };
    }

    private static MutationResult<TState> BuildValidationOnlyResult<TState>(
        IMutation<TState> mutation,
        TState state)
    {
        var validation = mutation.Validate(state);
        return validation.IsValid
            ? MutationResult<TState>.Success(state, ChangeSet.Empty)
            : MutationResult<TState>.Failure(validation);
    }

    public async Task<BatchMutationResult<TState>> ExecuteBatchAsync<TState>(
        IEnumerable<IMutation<TState>> mutations,
        TState state,
        CancellationToken cancellationToken = default)
    {
        return await MutationBatchExecutor.ExecuteAsync(
            mutations,
            state,
            _options.StopBatchOnFirstFailure,
            ExecuteAsync,
            cancellationToken);
    }

    public void RegisterPolicy<TState>(IMutationPolicy<TState> policy) =>
        _policyRegistry.Register(policy);

    public void RegisterInterceptor(IMutationInterceptor interceptor) =>
        _interceptorPipeline.Register(interceptor);

    public async Task<MutationHistory> GetHistoryAsync(string stateId, CancellationToken cancellationToken = default) =>
        await _historyStore.GetHistoryAsync(stateId, cancellationToken);

    public async Task<MutationStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var metrics = await _metricsCollector.GetAggregatedAsync(
            now.AddDays(-30),
            now,
            cancellationToken);

        return new MutationStatistics
        {
            TotalExecuted = metrics.TotalMutations,
            AverageExecutionTime = metrics.AverageExecutionTime,
            MedianExecutionTime = metrics.P50ExecutionTime,
            P95ExecutionTime = metrics.P95ExecutionTime,
            LastUpdatedAt = now
        };
    }

    private Task<PolicyDecision> EvaluatePoliciesAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        var policies = _policyRegistry.GetPolicies<TState>();

        foreach (var policy in policies.OrderByDescending(p => p.Priority))
        {
            var decision = policy.Evaluate(mutation, state);

            if (!decision.IsAllowed || decision.Modifications != null)
                return Task.FromResult(decision);
        }

        return Task.FromResult(PolicyDecision.Allow());
    }

    private async Task AuditSuccessAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        MutationResult<TState> result,
        PolicyDecision policyDecision,
        string executionId,
        TimeSpan duration)
    {
        var entry = MutationAuditEntryFactory.CreateSuccess(
            mutation,
            result,
            policyDecision,
            executionId,
            duration);

        await _auditor.AuditAsync(entry);
    }

    private async Task AuditFailureAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        MutationResult<TState> result,
        string executionId,
        TimeSpan duration)
    {
        var entry = MutationAuditEntryFactory.CreateFailure(
            mutation,
            result,
            executionId,
            duration);

        await _auditor.AuditAsync(entry);
    }

    private async Task AuditExceptionAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        Exception exception,
        string executionId,
        TimeSpan duration)
    {
        var entry = MutationAuditEntryFactory.CreateException(
            mutation,
            exception,
            executionId,
            duration);

        await _auditor.AuditAsync(entry);
    }

    private async Task StoreInHistoryAsync<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        string executionId,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var stateId = MutationAuditEntryFactory.ResolveStateId(mutation.Context);
        if (string.IsNullOrEmpty(stateId))
            return;

        var entry = MutationAuditEntryFactory.CreateHistoryEntry(
            mutation,
            result,
            executionId,
            stateId,
            duration);

        await _historyStore.StoreAsync(entry, cancellationToken);
    }

    private static MutationExecutionConcurrencyGate CreateConcurrencyGate(MutationEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxConcurrentMutations < 1)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.MaxConcurrentMutations,
                "MaxConcurrentMutations must be greater than zero.");

        return new MutationExecutionConcurrencyGate(options.MaxConcurrentMutations);
    }
}
