using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;
using ModularityKit.Mutator.Governance.Runtime.Execution.Persistence;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Outcome;

/// <summary>
/// Maps governed execution success and failure into terminal request state transitions.
/// </summary>
internal sealed class GovernedExecutionOutcomeHandler(GovernedExecutionRequestPersistence persistence)
{
    private readonly GovernedExecutionRequestPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

    /// <summary>
    /// Persists rejected execution outcome when governed execution throws exception.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="execution">Resolved governed execution context.</param>
    /// <param name="exception">Exception thrown during governed execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PersistException<TState>(
        GovernedExecutionContext<TState> execution, Exception exception, CancellationToken cancellationToken) =>
        await PersistRejectedExecution(
            execution.Resolution.Request,
            execution.GovernanceContext,
            $"Governed execution threw '{exception.GetType().Name}': {exception.Message}",
            GovernedExecutionFailureMetadataFactory.CreateExceptionMetadata(execution.CurrentStateVersion, exception),
            sideEffects: null,
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Persists rejected governed request state and appends rejection decision.
    /// </summary>
    /// <param name="request">Current persisted request snapshot.</param>
    /// <param name="governanceContext">Context describing the actor or service recording the rejection.</param>
    /// <param name="reason">Human-readable rejection reason.</param>
    /// <param name="metadata">Additional metadata captured for the rejection decision.</param>
    /// <param name="sideEffects">Optional side effects to persist with the rejected request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted rejected request snapshot.</returns>
    public async Task<MutationRequest> PersistRejectedExecution(
        MutationRequest request,
        MutationContext governanceContext,
        string reason,
        IReadOnlyDictionary<string, object> metadata,
        IReadOnlyList<SideEffect>? sideEffects,
        CancellationToken cancellationToken)
    {
        var decision = GovernedExecutionDecisionFactory.CreateRejectedDecision(
            request,
            governanceContext,
            reason,
            metadata);

        var rejectedRequest = request with
        {
            Status = MutationRequestStatus.Rejected,
            PendingReason = null,
            UpdatedAt = decision.Timestamp,
            Decisions = [.. request.Decisions, decision],
            SideEffects = sideEffects ?? []
        };

        return await _persistence.Persist(request, rejectedRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Persists successful governed execution state and appends executed decision.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="request">Current persisted request snapshot.</param>
    /// <param name="resultingStateVersion">Resulting state version produced by successful execution.</param>
    /// <param name="governanceContext">Context describing the actor or service recording the execution.</param>
    /// <param name="mutationResult">Core mutation result to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted executed request snapshot.</returns>
    public async Task<MutationRequest> PersistExecutedRequest<TState>(
        MutationRequest request,
        string resultingStateVersion,
        MutationContext governanceContext,
        MutationResult<TState> mutationResult,
        CancellationToken cancellationToken)
    {
        var decision = GovernedExecutionDecisionFactory.CreateExecutedDecision(
            request,
            governanceContext,
            resultingStateVersion,
            mutationResult);

        var executedRequest = request with
        {
            Status = MutationRequestStatus.Executed,
            PendingReason = null,
            Versioning = request.Versioning with
            {
                ExpectedStateVersion = resultingStateVersion,
                ResultingStateVersion = resultingStateVersion,
                ExecutedAt = decision.Timestamp
            },
            UpdatedAt = decision.Timestamp,
            Decisions = [.. request.Decisions, decision],
            SideEffects = mutationResult.SideEffects.ToList()
        };

        return await _persistence.Persist(request, executedRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds result object for request that did not execute through the core mutation engine.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="resolution">Version-resolution outcome and latest persisted request snapshot.</param>
    /// <param name="mutationResult">Optional mutation result when execution reached the engine but did not complete successfully.</param>
    /// <returns>Governed execution result describing a non-executed request.</returns>
    public GovernedExecutionResult<TState> BuildNonExecutedResult<TState>(
        MutationRequestVersionResolution resolution, MutationResult<TState>? mutationResult = null) =>
        new()
        {
            Request = resolution.Request,
            Resolution = resolution,
            MutationResult = mutationResult,
            WasExecuted = false,
            ExecutionKind = resolution.Request.Execution.Kind,
            Compensation = resolution.Request.Execution.Compensation
        };

    /// <summary>
    /// Builds result object for request that executed successfully.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="resolution">Version-resolution outcome that gated execution.</param>
    /// <param name="mutationResult">Core mutation result produced by successful execution.</param>
    /// <param name="executedRequest">Persisted executed request snapshot.</param>
    /// <param name="resultingStateVersion">Resulting state version produced by successful execution.</param>
    /// <returns>Governed execution result describing a successful execution.</returns>
    public GovernedExecutionResult<TState> BuildExecutedResult<TState>(MutationRequestVersionResolution resolution,
        MutationResult<TState> mutationResult, MutationRequest executedRequest, string resultingStateVersion) =>
        new()
        {
            Request = executedRequest,
            Resolution = resolution with { Request = executedRequest },
            MutationResult = mutationResult,
            WasExecuted = true,
            ExecutionKind = executedRequest.Execution.Kind,
            Compensation = executedRequest.Execution.Compensation,
            ResultingStateVersion = resultingStateVersion
        };


    /// <summary>
    /// Maps core mutation result into rejected or executed governed request outcome.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="execution">Resolved governed execution context.</param>
    /// <param name="mutationResult">Core mutation result to interpret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Governed execution result after persisting the terminal request state.</returns>
    public async Task<GovernedExecutionResult<TState>> HandleMutationResult<TState>(
        GovernedExecutionContext<TState> execution, MutationResult<TState> mutationResult, CancellationToken cancellationToken)
    {
        if (!mutationResult.IsSuccess || mutationResult.NewState is null)
        {
            var rejectedRequest = await PersistRejectedExecution(
                execution.Resolution.Request,
                execution.GovernanceContext,
                GovernedExecutionDecisionFactory.BuildRejectedExecutionReason(mutationResult),
                GovernedExecutionFailureMetadataFactory.CreateRejectedExecutionMetadata(
                    execution.CurrentStateVersion,
                    mutationResult),
                mutationResult.SideEffects,
                cancellationToken).ConfigureAwait(false);

            return BuildNonExecutedResult(
                execution.Resolution with { Request = rejectedRequest },
                mutationResult);
        }

        var resultingStateVersion = GovernedExecutionResultingStateVersionResolver.Resolve(
            execution,
            mutationResult.NewState);

        var executedRequest = await PersistExecutedRequest(
            execution.Resolution.Request,
            resultingStateVersion,
            execution.GovernanceContext,
            mutationResult,
            cancellationToken).ConfigureAwait(false);

        return BuildExecutedResult(
            execution.Resolution,
            mutationResult,
            executedRequest,
            resultingStateVersion);
    }
}
