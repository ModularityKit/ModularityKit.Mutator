using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;

namespace ModularityKit.Mutator.Governance.Runtime.Storage;

/// <summary>
/// In-memory store for governance mutation requests.
/// Suitable for examples, tests, and local development.
/// </summary>
public sealed class InMemoryMutationRequestStore : IMutationRequestStore, IMutationRequestQueryStore
{
    private readonly Dictionary<string, MutationRequest> _requests = new();
    private readonly Lock _lock = new();

    public Task<MutationRequest> Create(
        MutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock)
        {
            if (_requests.ContainsKey(request.RequestId))
                throw new MutationRequestAlreadyExistsException(request.RequestId);

            var persistedRequest = request with
            {
                Revision = 0
            };

            _requests[request.RequestId] = persistedRequest;
            return Task.FromResult(persistedRequest);
        }
    }

    public Task<MutationRequest?> TryStore(
        MutationRequest request,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock)
        {
            if (!_requests.TryGetValue(request.RequestId, out var currentRequest))
                return Task.FromResult<MutationRequest?>(null);

            if (currentRequest.Revision != expectedRevision)
                return Task.FromResult<MutationRequest?>(null);

            var persistedRequest = request with
            {
                Revision = expectedRevision + 1
            };

            _requests[request.RequestId] = persistedRequest;
            return Task.FromResult<MutationRequest?>(persistedRequest);
        }
    }

    public Task<MutationRequest?> Get(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _requests.TryGetValue(requestId, out var request);
            return Task.FromResult(request);
        }
    }

    public Task<IReadOnlyList<MutationRequest>> GetByStateId(
        string stateId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var requests = _requests.Values
                .Where(request => request.StateId == stateId)
                .OrderBy(request => request.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<MutationRequest>>(requests);
        }
    }

    public Task<IReadOnlyList<MutationRequest>> GetPending(
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var requests = _requests.Values
                .Where(request =>
                    request.Status == MutationRequestStatus.Pending &&
                    (reason is null || request.PendingReason == reason))
                .OrderBy(request => request.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<MutationRequest>>(requests);
        }
    }

    public Task<IReadOnlyList<MutationRequest>> GetPendingByStateId(
        string stateId,
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var requests = _requests.Values
                .Where(request =>
                    request.StateId == stateId &&
                    request.Status == MutationRequestStatus.Pending &&
                    (reason is null || request.PendingReason == reason))
                .OrderBy(request => request.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<MutationRequest>>(requests);
        }
    }

    public Task<IReadOnlyList<MutationRequest>> QueryAsync(
        MutationRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (_lock)
        {
            var requests = _requests.Values
                .Where(request => MutationRequestQueryEvaluator.Matches(request, query))
                .OrderBy(request => request.CreatedAt)
                .ThenBy(request => request.RequestId)
                .ToList();

            return Task.FromResult<IReadOnlyList<MutationRequest>>(requests);
        }
    }

    public Task<IReadOnlyList<MutationRequest>> GetPendingRequestsAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var requests = _requests.Values
                .Where(request => MutationRequestQueryEvaluator.Matches(request, query ?? new MutationRequestQuery()) &&
                                   request.Status == MutationRequestStatus.Pending)
                .OrderBy(request => request.CreatedAt)
                .ThenBy(request => request.RequestId)
                .ToList();

            return Task.FromResult<IReadOnlyList<MutationRequest>>(requests);
        }
    }

    public Task<IReadOnlyList<MutationRequest>> GetPendingApprovalQueueAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var requests = _requests.Values
                .Where(request =>
                    MutationRequestQueryEvaluator.Matches(request, query ?? new MutationRequestQuery()) &&
                    request.Status == MutationRequestStatus.Pending &&
                    request.PendingReason == PendingMutationReason.Approval)
                .OrderBy(request => request.CreatedAt)
                .ThenBy(request => request.RequestId)
                .ToList();

            return Task.FromResult<IReadOnlyList<MutationRequest>>(requests);
        }
    }

    public Task<IReadOnlyList<MutationRequest>> GetRecentApprovalsAsync(
        MutationRequestQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? MutationRequestQueries.RecentApprovals();

        lock (_lock)
        {
            var requests = _requests.Values
                .Where(request => MutationRequestQueryEvaluator.Matches(request, effectiveQuery) &&
                                   MutationRequestQueryEvaluator.HasApprovalActivity(request))
                .OrderByDescending(MutationRequestQueryEvaluator.GetRecentApprovalTimestamp)
                .ThenByDescending(request => request.UpdatedAt)
                .ThenBy(request => request.RequestId)
                .ToList();

            if (take.HasValue && take.Value >= 0)
                requests = requests.Take(take.Value).ToList();

            return Task.FromResult<IReadOnlyList<MutationRequest>>(requests);
        }
    }

    public Task<IReadOnlyList<MutationApprovalView>> GetPendingApprovalsAsync(
        MutationApprovalQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? MutationApprovalQuery.Pending();

        lock (_lock)
        {
            var approvals = _requests.Values
                .SelectMany(request => request.ApprovalRequirements.Select(approval => new MutationApprovalView
                {
                    Request = request,
                    Approval = approval
                }))
                .Where(view => MutationApprovalQueryEvaluator.Matches(view.Request, view.Approval, effectiveQuery))
                .OrderBy(view => view.Request.CreatedAt)
                .ThenBy(view => view.Request.RequestId)
                .ThenBy(view => view.Approval.StepOrder)
                .ThenBy(view => view.Approval.ApprovalId)
                .ToList();

            return Task.FromResult<IReadOnlyList<MutationApprovalView>>(approvals);
        }
    }

    public Task<IReadOnlyList<MutationRequestDecisionView>> GetRecentDecisionsAsync(
        MutationRequestDecisionQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new MutationRequestDecisionQuery();

        lock (_lock)
        {
            IEnumerable<MutationRequestDecisionView> decisions = _requests.Values
                .SelectMany(request => request.Decisions.Select(decision => new MutationRequestDecisionView
                {
                    Request = request,
                    Decision = decision
                }))
                .Where(view => MutationRequestDecisionQueryEvaluator.Matches(
                    view.Request,
                    view.Decision,
                    effectiveQuery))
                .OrderByDescending(view => view.Decision.Timestamp)
                .ThenByDescending(view => view.Request.UpdatedAt)
                .ThenBy(view => view.Request.RequestId);

            if (take.HasValue && take.Value >= 0)
                decisions = decisions.Take(take.Value);

            return Task.FromResult<IReadOnlyList<MutationRequestDecisionView>>(decisions.ToList());
        }
    }
}
