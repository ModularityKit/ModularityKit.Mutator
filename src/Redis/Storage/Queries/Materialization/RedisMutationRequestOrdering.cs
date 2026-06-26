using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Queries.Materialization;

/// <summary>
/// Applies common ordering rules for Redis backed governance query results.
/// </summary>
internal static class RedisMutationRequestOrdering
{
    public static IReadOnlyList<MutationRequest> ByCreated(IEnumerable<MutationRequest> requests)
        => requests
            .OrderBy(request => request.CreatedAt)
            .ThenBy(request => request.RequestId)
            .ToList();

    public static IReadOnlyList<MutationRequest> ByRecentApprovals(
        IEnumerable<MutationRequest> requests,
        int? take)
    {
        IEnumerable<MutationRequest> results = requests
            .OrderByDescending(MutationRequestQueryEvaluator.GetRecentApprovalTimestamp)
            .ThenByDescending(request => request.UpdatedAt)
            .ThenBy(request => request.RequestId);

        if (take is >= 0)
            results = results.Take(take.Value);

        return results.ToList();
    }

    public static IReadOnlyList<MutationApprovalView> ByPendingApprovalView(
        IEnumerable<MutationApprovalView> views)
        => views
            .OrderBy(view => view.Request.CreatedAt)
            .ThenBy(view => view.Request.RequestId)
            .ThenBy(view => view.Approval.StepOrder)
            .ThenBy(view => view.Approval.ApprovalId)
            .ToList();

    public static IReadOnlyList<MutationRequestDecisionView> ByRecentDecisionView(
        IEnumerable<MutationRequestDecisionView> views,
        int? take)
    {
        IEnumerable<MutationRequestDecisionView> results = views
            .OrderByDescending(view => view.Decision.Timestamp)
            .ThenByDescending(view => view.Request.UpdatedAt)
            .ThenBy(view => view.Request.RequestId);

        if (take is >= 0)
            results = results.Take(take.Value);

        return results.ToList();
    }
}
