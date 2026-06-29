using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Storage.Queries.Materialization;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Queries.Reading;

/// <summary>
/// Orchestrates Redis backed governed request query reads.
/// </summary>
internal sealed class RedisMutationRequestQueryReader
{
    private readonly RedisMutationRequestQueryDocumentLoader _documentLoader;

    /// <summary>
    /// Initializes a new query reader instance.
    /// </summary>
    /// <param name="documentLoader">The Redis query document loader.</param>
    public RedisMutationRequestQueryReader(
        RedisMutationRequestQueryDocumentLoader documentLoader)
    {
        ArgumentNullException.ThrowIfNull(documentLoader);

        _documentLoader = documentLoader;
    }

    /// <summary>
    /// Reads governed requests for a specific state identifier.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> GetByStateId(
        string stateId,
        CancellationToken cancellationToken = default)
        => await _documentLoader.LoadByStateIdAsync(stateId, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Reads pending governed requests, optionally narrowed by pending reason.
    /// </summary>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> GetPending(
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
        => await _documentLoader.LoadPendingAsync(reason, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Reads pending governed requests for a specific state identifier.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> GetPendingByStateId(
        string stateId,
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
        => await _documentLoader.LoadPendingByStateIdAsync(stateId, reason, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Reads governed requests matching the supplied general query.
    /// </summary>
    /// <param name="query">The request query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> QueryAsync(
        MutationRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var requests = await _documentLoader.LoadByRequestQueryAsync(query, cancellationToken).ConfigureAwait(false);
        return RedisMutationRequestQueryMaterializer.ApplyQuery(requests, query);
    }

    /// <summary>
    /// Reads pending governed requests and applies an optional in-memory query filter.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching pending requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> GetPendingRequestsAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new MutationRequestQuery();
        var requests = await _documentLoader.LoadPendingAsync(reason: null, cancellationToken).ConfigureAwait(false);

        return RedisMutationRequestQueryMaterializer.ApplyPendingQuery(requests, effectiveQuery);
    }

    /// <summary>
    /// Reads the pending approval queue and applies an optional in-memory query filter.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching pending approval-queue requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> GetPendingApprovalQueueAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new MutationRequestQuery();
        var requests = await _documentLoader.LoadPendingAsync(PendingMutationReason.Approval, cancellationToken)
            .ConfigureAwait(false);

        return RedisMutationRequestQueryMaterializer.ApplyPendingApprovalQueueQuery(requests, effectiveQuery);
    }

    /// <summary>
    /// Reads recent approval active governed requests.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <param name="take">An optional result limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching recent-approval requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> GetRecentApprovalsAsync(
        MutationRequestQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? MutationRequestQueries.RecentApprovals();
        var requests = await _documentLoader.LoadByRequestQueryAsync(effectiveQuery, cancellationToken).ConfigureAwait(false);
        return RedisMutationRequestQueryMaterializer.ApplyRecentApprovalsQuery(requests, effectiveQuery, take);
    }

    /// <summary>
    /// Reads pending approval views for governed requests.
    /// </summary>
    /// <param name="query">The optional approval query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching approval views.</returns>
    public async Task<IReadOnlyList<MutationApprovalView>> GetPendingApprovalsAsync(
        MutationApprovalQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? MutationApprovalQuery.Pending();
        var requests = await _documentLoader.LoadByRequestQueryAsync(effectiveQuery.RequestQuery, cancellationToken)
            .ConfigureAwait(false);
        return RedisMutationRequestQueryMaterializer.ApplyPendingApprovalViewQuery(requests, effectiveQuery);
    }

    /// <summary>
    /// Reads recent decision views for governed requests.
    /// </summary>
    /// <param name="query">The optional decision query.</param>
    /// <param name="take">An optional result limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching decision views.</returns>
    public async Task<IReadOnlyList<MutationRequestDecisionView>> GetRecentDecisionsAsync(
        MutationRequestDecisionQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQuery = query ?? new MutationRequestDecisionQuery();
        var requests = await _documentLoader.LoadByRequestQueryAsync(effectiveQuery.RequestQuery, cancellationToken)
            .ConfigureAwait(false);
        return RedisMutationRequestQueryMaterializer.ApplyRecentDecisionQuery(requests, effectiveQuery, take);
    }
}
