using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Storage.Persistence;
using ModularityKit.Mutator.Governance.Runtime.Storage.Queries.Materialization;

namespace ModularityKit.Mutator.Governance.Runtime.Storage.Reading;

/// <summary>
/// Handles query oriented reads for in-memory governed requests.
/// </summary>
internal sealed class InMemoryMutationRequestReader(InMemoryMutationRequestSnapshotSource snapshotSource)
{
    private readonly InMemoryMutationRequestSnapshotSource _snapshotSource =
        snapshotSource ?? throw new ArgumentNullException(nameof(snapshotSource));

    /// <summary>
    /// Queries governed requests using the supplied criteria.
    /// </summary>
    /// <param name="query">The query to evaluate.</param>
    /// <returns>The matching requests.</returns>
    public IReadOnlyList<MutationRequest> Query(MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _snapshotSource.Read(requests =>
            InMemoryMutationRequestQueryMaterializer.ApplyQuery(requests.Values, query));
    }

    /// <summary>
    /// Gets pending governed requests, optionally narrowed by additional criteria.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <returns>The matching pending requests.</returns>
    public IReadOnlyList<MutationRequest> GetPendingRequests(MutationRequestQuery? query = null)
    {
        var effectiveQuery = query ?? new MutationRequestQuery();

        return _snapshotSource.Read(requests =>
            InMemoryMutationRequestQueryMaterializer.ApplyPendingQuery(requests.Values, effectiveQuery));
    }

    /// <summary>
    /// Gets the pending approval queue, optionally narrowed by additional criteria.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <returns>The matching pending approval queue requests.</returns>
    public IReadOnlyList<MutationRequest> GetPendingApprovalQueue(MutationRequestQuery? query = null)
    {
        var effectiveQuery = query ?? new MutationRequestQuery();

        return _snapshotSource.Read(requests =>
            InMemoryMutationRequestQueryMaterializer.ApplyPendingApprovalQueueQuery(requests.Values, effectiveQuery));
    }

    /// <summary>
    /// Gets recent approval-driven requests, optionally narrowed by additional criteria.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <param name="take">The optional maximum number of results to return.</param>
    /// <returns>The matching recent approval requests.</returns>
    public IReadOnlyList<MutationRequest> GetRecentApprovals(
        MutationRequestQuery? query = null,
        int? take = null)
    {
        var effectiveQuery = query ?? MutationRequestQueries.RecentApprovals();

        return _snapshotSource.Read(requests =>
            InMemoryMutationRequestQueryMaterializer.ApplyRecentApprovalsQuery(requests.Values, effectiveQuery, take));
    }

    /// <summary>
    /// Gets approval-oriented projections for governed requests.
    /// </summary>
    /// <param name="query">The optional approval query.</param>
    /// <returns>The matching approval views.</returns>
    public IReadOnlyList<MutationApprovalView> GetPendingApprovals(MutationApprovalQuery? query = null)
    {
        var effectiveQuery = query ?? MutationApprovalQuery.Pending();

        return _snapshotSource.Read(requests =>
            InMemoryMutationRequestQueryMaterializer.ApplyPendingApprovalViewQuery(requests.Values, effectiveQuery));
    }

    /// <summary>
    /// Gets recent decision-oriented projections across governed requests.
    /// </summary>
    /// <param name="query">The optional decision query.</param>
    /// <param name="take">The optional maximum number of results to return.</param>
    /// <returns>The matching decision views.</returns>
    public IReadOnlyList<MutationRequestDecisionView> GetRecentDecisions(
        MutationRequestDecisionQuery? query = null,
        int? take = null)
    {
        var effectiveQuery = query ?? new MutationRequestDecisionQuery();

        return _snapshotSource.Read(requests =>
            InMemoryMutationRequestQueryMaterializer.ApplyRecentDecisionQuery(requests.Values, effectiveQuery, take));
    }
}
