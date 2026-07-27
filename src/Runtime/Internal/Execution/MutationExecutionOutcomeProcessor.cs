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
/// Processes mutation outcomes after policy evaluation and mutation execution.
/// </summary>
/// <remarks>
/// Handles policy blocked, validation failed, successfully completed, and finalized
/// mutation outcomes. Coordinates interceptor notifications, audit recording,
/// mutation history persistence, and metrics collection.
/// </remarks>
/// <param name="interceptorPipeline">Pipeline responsible for notifying registered mutation interceptors about execution outcomes.</param>
/// <param name="auditor">Auditor responsible for recording mutation success and failure audit entries. </param>
/// <param name="historyStore">Store responsible for persisting mutation history for committed mutations.</param>
/// <param name="metricsCollector">Collector responsible for recording mutation execution metrics.</param>
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
    /// Handles mutation that was blocked by a policy decision.
    /// </summary>
    /// <typeparam name="TState">The type of state associated with the mutation.</typeparam>
    /// <param name="executionContext">
    /// The context containing the mutation, current state, execution metadata,
    /// cancellation token, and metrics information.
    /// </param>
    /// <param name="policyDecision">The policy decision that blocked the mutation.</param>
    /// <returns>Policy blocked mutation result with audit and execution metrics finalized.</returns>
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
    /// Handles mutation that failed validation.
    /// </summary>
    /// <typeparam name="TState">The type of state associated with the mutation.</typeparam>
    /// <param name="executionContext">
    /// The context containing the mutation, current state, execution metadata,
    /// cancellation token, and metrics information.
    /// </param>
    /// <param name="validationFailureResult">The mutation result containing validation errors.</param>
    /// <returns>The validation failure result with audit and execution metrics finalized.</returns>
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
    /// Completes mutation execution after applying policy modifications and
    /// processing successful mutation outcomes.
    /// </summary>
    /// <typeparam name="TState">The type of state associated with the mutation.</typeparam>
    /// <param name="executionContext">
    /// The context containing the mutation, current state, execution metadata,
    /// cancellation token, and metrics information.
    /// </param>
    /// <param name="mutationResult">The result produced by the mutation execution.</param>
    /// <param name="policyDecision">The policy decision containing any modifications to apply to the mutation result.</param>
    /// <returns>
    /// The finalized mutation result after interceptor notification, auditing,
    /// optional history persistence, and metrics processing.
    /// </returns>
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
    /// Finalizes mutation result by recording execution metrics and attaching
    /// the total execution duration to the result.
    /// </summary>
    /// <typeparam name="TState">The type of state associated with the mutation.</typeparam>
    /// <param name="executionContext">The context containing execution timing and metrics information.</param>
    /// <param name="result">The mutation result to finalize.</param>
    /// <returns>The mutation result with finalized execution metrics.</returns>
    public Task<MutationResult<TState>> FinalizeResultAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        MutationResult<TState> result)
    {
        var totalElapsed = executionContext.Stopwatch.Elapsed;
        executionContext.MetricsScope?.RecordStateSize(StateSizeEstimator.Estimate(executionContext.State));

        if (executionContext.MetricsScope is not null)
        {
            return FinalizeWithMetricsAsync(executionContext, result, totalElapsed);
        }

        return Task.FromResult(result);
    }

    private async Task<MutationResult<TState>> FinalizeWithMetricsAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        MutationResult<TState> result,
        TimeSpan totalElapsed)
    {
        await _metricsCollector.RecordAsync(
            executionContext.ExecutionId,
            executionContext.MetricsScope!.Build(),
            executionContext.CancellationToken).ConfigureAwait(false);

        return result with
        {
            Metrics = result.Metrics with { ExecutionTime = totalElapsed }
        };
    }

    /// <summary>
    /// Records successful mutation execution in the audit system when auditing is enabled.
    /// </summary>
    /// <typeparam name="TState">The type of state associated with the mutation.</typeparam>
    /// <param name="mutation">The mutation that was executed.</param>
    /// <param name="result">The resulting mutation result.</param>
    /// <param name="policyDecision">The policy decision applied to the mutation.</param>
    /// <param name="executionId">The unique identifier of the mutation execution.</param>
    /// <param name="duration">The total execution duration.</param>
    private async Task AuditSuccessAsync<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        PolicyDecision policyDecision,
        string executionId,
        TimeSpan duration)
    {
        if (!_auditor.IsEnabled)
            return;

        var entry = MutationAuditEntryFactory.CreateSuccess(
            mutation,
            result,
            policyDecision,
            executionId,
            duration);

        await _auditor.AuditAsync(entry).ConfigureAwait(false);
    }

    /// <summary>
    /// Records failed mutation execution in the audit system when auditing is enabled.
    /// </summary>
    /// <typeparam name="TState">The type of state associated with the mutation.</typeparam>
    /// <param name="mutation">The mutation that failed.</param>
    /// <param name="result">The resulting mutation failure.</param>
    /// <param name="executionId">The unique identifier of the mutation execution.</param>
    /// <param name="duration">The total execution duration.</param>
    private async Task AuditFailureAsync<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        string executionId,
        TimeSpan duration)
    {
        if (!_auditor.IsEnabled)
            return;

        var entry = MutationAuditEntryFactory.CreateFailure(
            mutation,
            result,
            executionId,
            duration);

        await _auditor.AuditAsync(entry).ConfigureAwait(false);
    }

    /// <summary>
    /// Stores successful committed mutation in the mutation history store when history
    /// persistence is enabled and state identifier can be resolved.
    /// </summary>
    /// <typeparam name="TState">The type of state associated with the mutation.</typeparam>
    /// <param name="mutation">The mutation that was executed.</param>
    /// <param name="result">The resulting mutation result.</param>
    /// <param name="executionId">The unique identifier of the mutation execution.</param>
    /// <param name="duration">The total execution duration.</param>
    /// <param name="cancellationToken">Token that can be used to cancel the history persistence operation. </param>
   private async Task StoreInHistoryAsync<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        string executionId,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        if (!_historyStore.IsEnabled)
            return;

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
