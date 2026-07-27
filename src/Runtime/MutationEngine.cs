using System.Diagnostics;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Interception;
using ModularityKit.Mutator.Abstractions.Metrics;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime.Internal.Execution;
using ModularityKit.Mutator.Runtime.Internal.Evaluation;
using ModularityKit.Mutator.Runtime.Diagnostics;

namespace ModularityKit.Mutator.Runtime;

/// <summary>
/// Coordinates mutation execution and runtime governance.
/// </summary>
/// <remarks>
/// Handles mutation execution, policy evaluation, interception, auditing,
/// history tracking, metrics, concurrency control, and failure processing.
/// </remarks>
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
    private readonly IPolicyRegistry _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
    private readonly IInterceptorPipeline _interceptorPipeline = interceptorPipeline ?? throw new ArgumentNullException(nameof(interceptorPipeline));
    private readonly IMutationHistoryStore _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
    private readonly IMetricsCollector _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    private readonly MutationEngineOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly MutationExecutionConcurrencyGate _concurrencyGate = CreateConcurrencyGate(options);
    private static long _executionCounter;
    private readonly MutationExecutionFailureHandler _failureHandler = new(interceptorPipeline, auditor);
    private readonly MutationExecutionPipeline _executionPipeline =
        new(
            new MutationPolicyEvaluator(policyRegistry, options),
            interceptorPipeline,
            new MutationExecutionModeRunner(executor, options),
            new MutationExecutionOutcomeProcessor(interceptorPipeline, auditor, historyStore, metricsCollector),
            options);

    /// <summary>
    /// Executes a single mutation using the full governance pipeline.
    /// </summary>
    /// <typeparam name="TState">The type of the state being mutated.</typeparam>
    /// <param name="mutation">The mutation to execute.</param>
    /// <param name="state">The current state.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>
    /// A <see cref="MutationResult{TState}" /> containing the execution outcome,
    /// produced changes, and resulting state.
    /// </returns>
    public async Task<MutationResult<TState>> ExecuteAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken = default)
    {
        var executionId = Interlocked.Increment(ref _executionCounter).ToString("x8");
        var stopwatch = Stopwatch.StartNew();
        IMetricsScope? metricsScope = null;

        await using var executionLease = await _concurrencyGate
            .EnterAsync(mutation.Context.StateId, cancellationToken)
            .ConfigureAwait(false);

        if (_options.EnableDetailedMetrics)
            metricsScope = _metricsCollector.BeginScope(executionId);

        var executionContext = new MutationExecutionContext<TState>
        {
            Mutation = mutation,
            State = state,
            ExecutionId = executionId,
            Stopwatch = stopwatch,
            MetricsScope = metricsScope,
            CancellationToken = cancellationToken
        };

        try
        {
            return await ExecutePipelineAsync(executionContext).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MutationException ex)
        {
            stopwatch.Stop();

            await _failureHandler.HandleKnownExceptionAsync(
                executionContext,
                ex,
                stopwatch.Elapsed).ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            throw await _failureHandler.HandleUnexpectedExceptionAsync(
                executionContext,
                ex,
                stopwatch.Elapsed).ConfigureAwait(false);
        }
        finally
        {
            metricsScope?.Dispose();
        }
    }

    /// <summary>
    /// Delegates the core execution flow to the internal mutation pipeline.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="executionContext">The shared execution context carrying the mutation, state, and runtime metadata.</param>
    /// <returns>The <see cref="MutationResult{TState}" /> produced by the pipeline.</returns>
    private Task<MutationResult<TState>> ExecutePipelineAsync<TState>(MutationExecutionContext<TState> executionContext)
        => _executionPipeline.ExecuteAsync(executionContext);

    /// <summary>
    /// Executes a batch of mutations as a single logical transaction.
    /// </summary>
    /// <typeparam name="TState">The type of the state being mutated.</typeparam>
    /// <param name="mutations">The sequence of mutations to execute.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>
    /// A <see cref="BatchMutationResult{TState}" /> describing the outcome of the batch execution.
    /// </returns>
    /// <remarks>
    /// Batch execution is ordered and sequential. Each step passes through the same core concurrency
    /// controls as a single execution. Fail-fast vs best-effort behavior is controlled by <see cref="MutationEngineOptions" />.
    /// </remarks>
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

    /// <summary>
    /// Executes a batch of mutations as a single logical transaction.
    /// </summary>
    /// <typeparam name="TState">The type of the state being mutated.</typeparam>
    /// <param name="state">The initial state.</param>
    /// <param name="mutations">The mutations to execute in order.</param>
    /// <returns>
    /// A <see cref="BatchMutationResult{TState}" /> describing the outcome of the batch execution.
    /// </returns>
    /// <remarks>
    /// This overload is optimized for call sites that want a compact mutation list without
    /// manually allocating an array.
    /// </remarks>
    public Task<BatchMutationResult<TState>> ExecuteBatchAsync<TState>(
        TState state,
        params IMutation<TState>[] mutations)
        => ExecuteBatchAsync(mutations, state);

    /// <summary>
    /// Registers a global mutation policy.
    /// </summary>
    /// <typeparam name="TState">The state type the policy applies to.</typeparam>
    /// <param name="policy">The policy to register.</param>
    /// <remarks>
    /// Global policies participate in evaluation for every compatible mutation
    /// and represent the primary governance mechanism.
    /// </remarks>
    public void RegisterPolicy<TState>(IMutationPolicy<TState> policy) =>
        _policyRegistry.Register(policy);

    /// <summary>
    /// Registers a global mutation interceptor.
    /// </summary>
    /// <param name="interceptor">The interceptor to register.</param>
    /// <remarks>
    /// Interceptors observe and react to mutation lifecycle events but must not
    /// directly alter mutation semantics.
    /// </remarks>
    public void RegisterInterceptor(IMutationInterceptor interceptor) =>
        _interceptorPipeline.Register(interceptor);

    /// <summary>
    /// Retrieves the mutation history for a given state identifier.
    /// </summary>
    /// <param name="stateId">The identifier of the state.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="MutationHistory" /> containing all recorded mutations for the state.
    /// </returns>
    public async Task<MutationHistory> GetHistoryAsync(string stateId, CancellationToken cancellationToken = default) =>
        await _historyStore.GetHistoryAsync(stateId, cancellationToken);

    /// <summary>
    /// Retrieves aggregated mutation execution statistics.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="MutationStatistics" /> snapshot representing engine-level metrics.
    /// </returns>
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

    /// <summary>
    /// Creates the runtime concurrency gate from configured engine options.
    /// </summary>
    /// <param name="options">The engine options containing the <see cref="MutationEngineOptions.MaxConcurrentMutations" /> value.</param>
    /// <returns>A configured <see cref="MutationExecutionConcurrencyGate" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="MutationEngineOptions.MaxConcurrentMutations" /> is less than 1.</exception>
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
