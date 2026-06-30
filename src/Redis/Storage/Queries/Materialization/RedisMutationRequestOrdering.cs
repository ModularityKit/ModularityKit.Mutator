using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Queries.Materialization;

/// <summary>
/// Applies common ordering rules for Redis backed governance query results.
/// </summary>
internal static class RedisMutationRequestOrdering
{
    /// <summary>
    /// Orders request results by creation time and request identifier.
    /// </summary>
    /// <param name="requests">Requests to order.</param>
    /// <returns>Materialized request results in ascending creation order.</returns>
    public static IReadOnlyList<MutationRequest> ByCreated(IEnumerable<MutationRequest> requests)
        => [.. requests
            .OrderBy(request => request.CreatedAt)
            .ThenBy(request => request.RequestId)];

    /// <summary>
    /// Orders requests by the most recent approval activity and applies an optional result limit.
    /// </summary>
    /// <param name="requests">Requests to order.</param>
    /// <param name="take">Optional maximum number of results to return.</param>
    /// <returns>Materialized request results ordered for recent approval views.</returns>
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

        return [.. results];
    }

    /// <summary>
    /// Orders pending approval projections by request creation and approval step sequence.
    /// </summary>
    /// <param name="views">Approval views to order.</param>
    /// <returns>Materialized approval views in pending queue order.</returns>
    public static IReadOnlyList<MutationApprovalView> ByPendingApprovalView(
        IEnumerable<MutationApprovalView> views)
        => [.. views
            .OrderBy(view => view.Request.CreatedAt)
            .ThenBy(view => view.Request.RequestId)
            .ThenBy(view => view.Approval.StepOrder)
            .ThenBy(view => view.Approval.ApprovalId)];

    /// <summary>
    /// Orders decision projections by decision recency and applies an optional result limit.
    /// </summary>
    /// <param name="views">Decision views to order.</param>
    /// <param name="take">Optional maximum number of results to return.</param>
    /// <returns>Materialized decision views ordered from newest to oldest.</returns>
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

        return [.. results];
    }
}
