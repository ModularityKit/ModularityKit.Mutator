using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Runtime.Execution.Persistence;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Outcome;

/// <summary>
/// Maps governed execution success and failure into terminal request state transitions.
/// </summary>
internal sealed class GovernedExecutionOutcomeHandler(GovernedExecutionRequestPersistence persistence)
{
    private readonly GovernedExecutionRequestPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

    public async Task<MutationRequest> PersistRejectedExecution(
        MutationRequest request,
        MutationContext governanceContext,
        string reason,
        IReadOnlyDictionary<string, object> metadata,
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
            Decisions = [.. request.Decisions, decision]
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
            Decisions = [.. request.Decisions, decision]
        };

        return await _persistence.Persist(request, executedRequest, cancellationToken).ConfigureAwait(false);
    }

    public GovernedExecutionResult<TState> BuildNonExecutedResult<TState>(
        MutationRequestVersionResolution resolution,
        MutationResult<TState>? mutationResult = null)
    {
        return new GovernedExecutionResult<TState>
        {
            Request = resolution.Request,
            Resolution = resolution,
            MutationResult = mutationResult,
            WasExecuted = false
        };
    }

    public GovernedExecutionResult<TState> BuildExecutedResult<TState>(
        MutationRequestVersionResolution resolution,
        MutationResult<TState> mutationResult,
        MutationRequest executedRequest,
        string resultingStateVersion)
    {
        return new GovernedExecutionResult<TState>
        {
            Request = executedRequest,
            Resolution = resolution with { Request = executedRequest },
            MutationResult = mutationResult,
            WasExecuted = true,
            ResultingStateVersion = resultingStateVersion
        };
    }
}
