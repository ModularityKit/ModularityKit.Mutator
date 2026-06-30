using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Interception;
using ModularityKit.Mutator.Abstractions.Metrics;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime.Internal.Evaluation;
using ModularityKit.Mutator.Runtime.Diagnostics;

namespace ModularityKit.Mutator.Runtime.Internal.Execution;

/// <summary>
/// Handles blocked, failed, and completed mutation outcomes after policy evaluation and execution.
/// </summary>
internal sealed class MutationExecutionOutcomeProcessor(
    IInterceptorPipeline interceptorPipeline,
    IMutationAuditor auditor,
    IMutationHistoryStore historyStore,
    IMetricsCollector metricsCollector)
{
    private readonly IInterceptorPipeline _interceptorPipeline =
        interceptorPipeline ?? throw new ArgumentNullException(nameof(interceptorPipeline));

    private readonly IMutationAuditor _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
    private readonly IMutationHistoryStore _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
    private readonly IMetricsCollector _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));

    /// <summary>
    /// Handles a policy-blocked mutation result.
    /// </summary>
    public async Task<MutationResult<TState>> HandleBlockedPolicyAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        PolicyDecision policyDecision)
    {
        var blockedResult = MutationResult<TState>.PolicyBlocked(policyDecision);

        await _interceptorPipeline.OnPolicyBlockedAsync(
            executionContext.Mutation.Intent,
            executionContext.Mutation.Context,
            executionContext.State!,
            policyDecision,
            executionContext.ExecutionId,
            executionContext.CancellationToken).ConfigureAwait(false);

        await AuditFailureAsync(
            executionContext.Mutation,
            blockedResult,
            executionContext.ExecutionId,
            executionContext.Stopwatch.Elapsed).ConfigureAwait(false);

        return await FinalizeResultAsync(executionContext, blockedResult).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles a validation-failed mutation result.
    /// </summary>
    public async Task<MutationResult<TState>> HandleValidationFailureAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        MutationResult<TState> validationFailureResult)
    {
        await AuditFailureAsync(
            executionContext.Mutation,
            validationFailureResult,
            executionContext.ExecutionId,
            executionContext.Stopwatch.Elapsed).ConfigureAwait(false);

        return await FinalizeResultAsync(executionContext, validationFailureResult).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes a mutation result after execution and policy modifications.
    /// </summary>
    public async Task<MutationResult<TState>> CompleteMutationAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        MutationResult<TState> mutationResult,
        PolicyDecision policyDecision)
    {
        var totalElapsed = executionContext.Stopwatch.Elapsed;
        var finalizedMutationResult = PolicyModificationApplier.Apply(mutationResult, policyDecision.Modifications);

        await _interceptorPipeline.OnAfterMutationAsync(
            executionContext.Mutation.Intent,
            executionContext.Mutation.Context,
            executionContext.State,
            finalizedMutationResult.NewState,
            finalizedMutationResult.Changes,
            executionContext.ExecutionId,
            executionContext.CancellationToken).ConfigureAwait(false);

        await AuditSuccessAsync(
            executionContext.Mutation,
            finalizedMutationResult,
            policyDecision,
            executionContext.ExecutionId,
            totalElapsed).ConfigureAwait(false);

        if (finalizedMutationResult.IsSuccess && executionContext.Mutation.Context.Mode == MutationMode.Commit)
        {
            await StoreInHistoryAsync(
                executionContext.Mutation,
                finalizedMutationResult,
                executionContext.ExecutionId,
                totalElapsed,
                executionContext.CancellationToken).ConfigureAwait(false);
        }

        return await FinalizeResultAsync(executionContext, finalizedMutationResult).ConfigureAwait(false);
    }

    /// <summary>
    /// Finalizes a runtime result by recording metrics and attaching total execution time.
    /// </summary>
    public async Task<MutationResult<TState>> FinalizeResultAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        MutationResult<TState> result)
    {
        var totalElapsed = executionContext.Stopwatch.Elapsed;
        executionContext.MetricsScope?.RecordStateSize(StateSizeEstimator.Estimate(executionContext.State));

        if (executionContext.MetricsScope is not null)
        {
            await _metricsCollector.RecordAsync(
                executionContext.ExecutionId,
                executionContext.MetricsScope.Build(),
                executionContext.CancellationToken).ConfigureAwait(false);
        }

        return result with
        {
            Metrics = result.Metrics with { ExecutionTime = totalElapsed }
        };
    }

    private async Task AuditSuccessAsync<TState>(
        IMutation<TState> mutation,
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

        await _auditor.AuditAsync(entry).ConfigureAwait(false);
    }

    private async Task AuditFailureAsync<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        string executionId,
        TimeSpan duration)
    {
        var entry = MutationAuditEntryFactory.CreateFailure(
            mutation,
            result,
            executionId,
            duration);

        await _auditor.AuditAsync(entry).ConfigureAwait(false);
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

        await _historyStore.StoreAsync(entry, cancellationToken).ConfigureAwait(false);
    }
}
