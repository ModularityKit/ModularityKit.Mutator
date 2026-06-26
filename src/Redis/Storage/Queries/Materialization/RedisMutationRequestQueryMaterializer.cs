using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Queries.Materialization;

/// <summary>
/// Applies governance query evaluators to materialized Redis request documents.
/// </summary>
internal static class RedisMutationRequestQueryMaterializer
{
    /// <summary>
    /// Applies a general request query to already materialized requests.
    /// </summary>
    /// <param name="requests">The materialized requests.</param>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>The filtered request results.</returns>
    public static IReadOnlyList<MutationRequest> ApplyQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        return RedisMutationRequestOrdering.ByCreated(
            requests.Where(request => MutationRequestQueryEvaluator.Matches(request, query)));
    }

    /// <summary>
    /// Applies a pending request query to already materialized requests.
    /// </summary>
    /// <param name="requests">The materialized requests.</param>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>The filtered pending-request results.</returns>
    public static IReadOnlyList<MutationRequest> ApplyPendingQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        return RedisMutationRequestOrdering.ByCreated(
            requests.Where(request =>
                request.Status == MutationRequestStatus.Pending &&
                MutationRequestQueryEvaluator.Matches(request, query)));
    }

    /// <summary>
    /// Applies a pending approval queue query to already materialized requests.
    /// </summary>
    /// <param name="requests">The materialized requests.</param>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>The filtered pending-approval-queue results.</returns>
    public static IReadOnlyList<MutationRequest> ApplyPendingApprovalQueueQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        return RedisMutationRequestOrdering.ByCreated(
            requests.Where(request =>
                request.Status == MutationRequestStatus.Pending &&
                request.PendingReason == PendingMutationReason.Approval &&
                MutationRequestQueryEvaluator.Matches(request, query)));
    }

    /// <summary>
    /// Applies a recent approvals query to already materialized requests.
    /// </summary>
    /// <param name="requests">The materialized requests.</param>
    /// <param name="query">The query to evaluate.</param>
    /// <param name="take">An optional result limit.</param>
    /// <returns>The filtered recent-approval results.</returns>
    public static IReadOnlyList<MutationRequest> ApplyRecentApprovalsQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestQuery query,
        int? take)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        var results = requests
            .Where(request =>
                MutationRequestQueryEvaluator.Matches(request, query) &&
                MutationRequestQueryEvaluator.HasApprovalActivity(request));

        return RedisMutationRequestOrdering.ByRecentApprovals(results, take);
    }

    /// <summary>
    /// Applies a pending approval view query to already materialized requests.
    /// </summary>
    /// <param name="requests">The materialized requests.</param>
    /// <param name="query">The approval query to evaluate.</param>
    /// <returns>The filtered approval-view results.</returns>
    public static IReadOnlyList<MutationApprovalView> ApplyPendingApprovalViewQuery(
        IEnumerable<MutationRequest> requests,
        MutationApprovalQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        var views = RedisMutationRequestViewProjector
            .ToApprovalViews(requests)
            .Where(view => MutationApprovalQueryEvaluator.Matches(view.Request, view.Approval, query));

        return RedisMutationRequestOrdering.ByPendingApprovalView(views);
    }

    /// <summary>
    /// Applies a recent decision query to already materialized requests.
    /// </summary>
    /// <param name="requests">The materialized requests.</param>
    /// <param name="query">The decision query to evaluate.</param>
    /// <param name="take">An optional result limit.</param>
    /// <returns>The filtered decision-view results.</returns>
    public static IReadOnlyList<MutationRequestDecisionView> ApplyRecentDecisionQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestDecisionQuery query,
        int? take)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        var views = RedisMutationRequestViewProjector
            .ToDecisionViews(requests)
            .Where(view => MutationRequestDecisionQueryEvaluator.Matches(view.Request, view.Decision, query));

        return RedisMutationRequestOrdering.ByRecentDecisionView(views, take);
    }
}
