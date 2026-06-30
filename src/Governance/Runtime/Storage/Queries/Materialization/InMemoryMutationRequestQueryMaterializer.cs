using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Runtime.Storage.Queries.Materialization;

/// <summary>
/// Applies governance query evaluators to in-memory request snapshots.
/// </summary>
internal static class InMemoryMutationRequestQueryMaterializer
{
    /// <summary>
    /// Applies general request query to in-memory request snapshots.
    /// </summary>
    /// <param name="requests">The in-memory request snapshots.</param>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>The filtered request results.</returns>
    public static IReadOnlyList<MutationRequest> ApplyQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        return InMemoryMutationRequestOrdering.ByCreated(
            requests.Where(request => MutationRequestQueryEvaluator.Matches(request, query)));
    }

    /// <summary>
    /// Applies pending request query to in-memory request snapshots.
    /// </summary>
    /// <param name="requests">The in-memory request snapshots.</param>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>The filtered pending-request results.</returns>
    public static IReadOnlyList<MutationRequest> ApplyPendingQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        return InMemoryMutationRequestOrdering.ByCreated(
            requests.Where(request =>
                request.Status == MutationRequestStatus.Pending &&
                MutationRequestQueryEvaluator.Matches(request, query)));
    }

    /// <summary>
    /// Applies pending approval queue query to in-memory request snapshots.
    /// </summary>
    /// <param name="requests">The in-memory request snapshots.</param>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>The filtered pending-approval-queue results.</returns>
    public static IReadOnlyList<MutationRequest> ApplyPendingApprovalQueueQuery(
        IEnumerable<MutationRequest> requests,
        MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        return InMemoryMutationRequestOrdering.ByCreated(
            requests.Where(request =>
                request.Status == MutationRequestStatus.Pending &&
                request.PendingReason == PendingMutationReason.Approval &&
                MutationRequestQueryEvaluator.Matches(request, query)));
    }

    /// <summary>
    /// Applies recent approvals query to in-memory request snapshots.
    /// </summary>
    /// <param name="requests">The in-memory request snapshots.</param>
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

        return InMemoryMutationRequestOrdering.ByRecentApprovals(results, take);
    }

    /// <summary>
    /// Applies pending approval view query to in-memory request snapshots.
    /// </summary>
    /// <param name="requests">The in-memory request snapshots.</param>
    /// <param name="query">The approval query to evaluate.</param>
    /// <returns>The filtered approval-view results.</returns>
    public static IReadOnlyList<MutationApprovalView> ApplyPendingApprovalViewQuery(
        IEnumerable<MutationRequest> requests,
        MutationApprovalQuery query)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(query);

        var views = InMemoryMutationRequestViewProjector
            .ToApprovalViews(requests)
            .Where(view => MutationApprovalQueryEvaluator.Matches(view.Request, view.Approval, query));

        return InMemoryMutationRequestOrdering.ByPendingApprovalView(views);
    }

    /// <summary>
    /// Applies recent decision query to in-memory request snapshots.
    /// </summary>
    /// <param name="requests">The in-memory request snapshots.</param>
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

        var views = InMemoryMutationRequestViewProjector
            .ToDecisionViews(requests)
            .Where(view => MutationRequestDecisionQueryEvaluator.Matches(view.Request, view.Decision, query));

        return InMemoryMutationRequestOrdering.ByRecentDecisionView(views, take);
    }
}
