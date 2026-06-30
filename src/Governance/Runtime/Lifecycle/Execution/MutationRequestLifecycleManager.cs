using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Runtime.Lifecycle.State;

namespace ModularityKit.Mutator.Governance.Runtime.Lifecycle.Execution;

/// <summary>
/// Applies explicit runtime transitions to governed mutation requests and persists decision history.
/// </summary>
public sealed class MutationRequestLifecycleManager(IMutationRequestStore requestStore) : IMutationRequestLifecycleManager
{
    private readonly IMutationRequestStore _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
    private readonly MutationRequestTransitionExecutor _transitionExecutor = new(requestStore);

    public async Task<MutationRequest> Submit(
        MutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _requestStore.Create(request, cancellationToken).ConfigureAwait(false);
    }

    public Task<MutationRequest> MoveToPending(
        string requestId,
        PendingMutationReason pendingReason,
        MutationContext decisionContext,
        string? reason = null,
        DateTimeOffset? expiresAt = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _transitionExecutor.Execute(
            requestId,
            MutationRequestStatus.Pending,
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
            decisionContext,
            reason,
            request => request with
            {
                Lifecycle = request.Lifecycle with
                {
                    PendingReason = pendingReason,
                    ExpiresAt = expiresAt
                }
            },
            metadata,
            cancellationToken);
    }

    public Task<MutationRequest> Approve(
        string requestId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _transitionExecutor.Execute(
            requestId,
            MutationRequestStatus.Approved,
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
            decisionContext,
            reason,
            request => request with
            {
                Lifecycle = request.Lifecycle with
                {
                    PendingReason = null
                }
            },
            metadata,
            cancellationToken);
    }

    public Task<MutationRequest> Reject(
        string requestId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _transitionExecutor.Execute(
            requestId,
            MutationRequestStatus.Rejected,
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Rejected),
            decisionContext,
            reason,
            MutationRequestLifecycleState.ClearPendingState,
            metadata,
            cancellationToken);
    }

    public Task<MutationRequest> Cancel(
        string requestId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _transitionExecutor.Execute(
            requestId,
            MutationRequestStatus.Canceled,
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Canceled),
            decisionContext,
            reason,
            MutationRequestLifecycleState.ClearPendingState,
            metadata,
            cancellationToken);
    }

    public Task<MutationRequest> Expire(
        string requestId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _transitionExecutor.Execute(
            requestId,
            MutationRequestStatus.Expired,
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Expired),
            decisionContext,
            reason,
            MutationRequestLifecycleState.ClearPendingState,
            metadata,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MutationRequest>> ExpireDueRequests(
        DateTimeOffset now,
        MutationContext decisionContext,
        CancellationToken cancellationToken = default)
    {
        var pendingRequests = await _requestStore
            .GetPending(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var expiredRequests = new List<MutationRequest>();

        foreach (var request in pendingRequests)
        {
            if (request.ExpiresAt is null || request.ExpiresAt > now)
                continue;

            var reason = $"Pending request expired at '{request.ExpiresAt:O}'.";

            var expiredRequest = await Expire(
                request.RequestId,
                decisionContext,
                reason,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            expiredRequests.Add(expiredRequest);
        }

        return expiredRequests;
    }

    public Task<MutationRequest> Supersede(
        string requestId,
        string supersedingRequestId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(supersedingRequestId))
            throw new ArgumentException("Superseding request ID is required.", nameof(supersedingRequestId));

        var transitionMetadata = MutationRequestLifecycleState.MergeMetadata(
            metadata,
            new Dictionary<string, object>
            {
                ["SupersedingRequestId"] = supersedingRequestId
            });

        return _transitionExecutor.Execute(
            requestId,
            MutationRequestStatus.Superseded,
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Superseded),
            decisionContext,
            reason ?? $"Superseded by request '{supersedingRequestId}'.",
            MutationRequestLifecycleState.ClearPendingState,
            transitionMetadata,
            cancellationToken);
    }

    public Task<MutationRequest> MarkExecuted(
        string requestId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _transitionExecutor.Execute(
            requestId,
            MutationRequestStatus.Executed,
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
            decisionContext,
            reason,
            MutationRequestLifecycleState.ClearPendingState,
            metadata,
            cancellationToken);
    }

    public Task<IReadOnlyList<MutationRequest>> GetPending(
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
    {
        return _requestStore.GetPending(reason, cancellationToken);
    }

    public Task<IReadOnlyList<MutationRequest>> GetPendingByStateId(
        string stateId,
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
    {
        return _requestStore.GetPendingByStateId(stateId, reason, cancellationToken);
    }

}
