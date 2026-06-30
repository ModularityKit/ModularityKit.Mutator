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

    public async Task PersistException<TState>(
        GovernedExecutionContext<TState> execution, Exception exception, CancellationToken cancellationToken)
    {
        await PersistRejectedExecution(
            execution.Resolution.Request,
            execution.GovernanceContext,
            $"Governed execution threw '{exception.GetType().Name}': {exception.Message}",
            GovernedExecutionFailureMetadataFactory.CreateExceptionMetadata(execution.CurrentStateVersion, exception),
            sideEffects: null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MutationRequest> PersistRejectedExecution(
        MutationRequest request,
        MutationContext governanceContext,
        string reason,
        IReadOnlyDictionary<string, object> metadata,
        IReadOnlyList<SideEffect>? sideEffects,
        CancellationToken cancellationToken)
    {
        var decision = GovernedExecutionDecisionFactory.CreateRejectedDecision(
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

    public async Task<MutationRequest> PersistExecutedRequest<TState>(
        MutationRequest request,
        string resultingStateVersion,
        MutationContext governanceContext,
        MutationResult<TState> mutationResult,
        CancellationToken cancellationToken)
    {
        var decision = GovernedExecutionDecisionFactory.CreateExecutedDecision(
            governanceContext,
            resultingStateVersion,
            mutationResult);

        var executedRequest = request with
        {
            Status = MutationRequestStatus.Executed,
            PendingReason = null,
            ExpectedStateVersion = resultingStateVersion,
            ResultingStateVersion = resultingStateVersion,
            ExecutedAt = decision.Timestamp,
            UpdatedAt = decision.Timestamp,
            Decisions = [.. request.Decisions, decision],
            SideEffects = mutationResult.SideEffects.ToList()
        };

        return await _persistence.Persist(request, executedRequest, cancellationToken).ConfigureAwait(false);
    }

    public GovernedExecutionResult<TState> BuildNonExecutedResult<TState>(
        MutationRequestVersionResolution resolution, MutationResult<TState>? mutationResult = null) =>
        new()
        {
            Request = resolution.Request,
            Resolution = resolution,
            MutationResult = mutationResult,
            WasExecuted = false
        };

    public GovernedExecutionResult<TState> BuildExecutedResult<TState>(MutationRequestVersionResolution resolution,
        MutationResult<TState> mutationResult, MutationRequest executedRequest, string resultingStateVersion) =>
        new()
        {
            Request = executedRequest,
            Resolution = resolution with { Request = executedRequest },
            MutationResult = mutationResult,
            WasExecuted = true,
            ResultingStateVersion = resultingStateVersion
        };


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
