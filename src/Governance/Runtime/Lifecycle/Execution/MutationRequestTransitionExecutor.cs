using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Lifecycle;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Runtime.Lifecycle.State;
using ModularityKit.Mutator.Governance.Runtime.Lifecycle.Validation;

namespace ModularityKit.Mutator.Governance.Runtime.Lifecycle.Execution;

/// <summary>
/// Executes a single guarded lifecycle transition for a governed mutation request.
/// </summary>
internal sealed class MutationRequestTransitionExecutor(IMutationRequestStore requestStore)
{
    private readonly IMutationRequestStore _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));

    /// <summary>
    /// Loads the current request, validates the target transition, appends the decision, and persists the new revision.
    /// </summary>
    public async Task<MutationRequest> Execute(
        string requestId,
        MutationRequestStatus targetStatus,
        MutationRequestDecisionType decisionType,
        MutationContext decisionContext,
        string? reason,
        Func<MutationRequest, MutationRequest> applyState,
        IReadOnlyDictionary<string, object>? metadata,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID is required.", nameof(requestId));

        ArgumentNullException.ThrowIfNull(decisionContext);
        ArgumentNullException.ThrowIfNull(applyState);

        var request = await GetRequired(requestId, cancellationToken).ConfigureAwait(false);

        MutationRequestTransitionValidator.Validate(request.Status, targetStatus, request.RequestId);

        var decision = MutationRequestDecision.Create(
            decisionType,
            decisionContext,
            reason,
            metadata);

        var updatedRequest = applyState(request) with
        {
            Lifecycle = request.Lifecycle with
            {
                Status = targetStatus,
                UpdatedAt = decision.Timestamp
            },
            Decisions = [.. request.Decisions, decision]
        };

        var persistedRequest = await _requestStore
            .TryStore(updatedRequest, request.Revision, cancellationToken)
            .ConfigureAwait(false);

        if (persistedRequest is null)
            throw new MutationRequestConcurrencyException(request.RequestId, request.Revision);

        return persistedRequest;
    }

    /// <summary>
    /// Loads a required request or raises a governance not-found exception.
    /// </summary>
    private async Task<MutationRequest> GetRequired(
        string requestId,
        CancellationToken cancellationToken)
    {
        var request = await _requestStore.Get(requestId, cancellationToken).ConfigureAwait(false);

        if (request is null)
            throw new MutationRequestNotFoundException(requestId);

        return request;
    }
}
