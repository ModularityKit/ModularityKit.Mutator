using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence;
using ModularityKit.Mutator.Governance.Redis.Storage.Queries;
using ModularityKit.Mutator.Governance.Redis.Storage.Queries.Reading;

namespace ModularityKit.Mutator.Governance.Redis.Storage;

/// <summary>
/// Implementation of governed mutation request storage and query access.
/// </summary>
public sealed class RedisMutationRequestStore : IMutationRequestStore, IMutationRequestQueryStore
{
    private readonly RedisMutationRequestPersistence _persistence;
    private readonly RedisMutationRequestQueryReader _queryReader;

    internal RedisMutationRequestStore(
        RedisMutationRequestPersistence persistence,
        RedisMutationRequestQueryReader queryReader)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _queryReader = queryReader ?? throw new ArgumentNullException(nameof(queryReader));
    }

    /// <summary>
    /// Creates governed mutation request in storage.
    /// </summary>
    /// <param name="request">The request to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted mutation request.</returns>
    public Task<MutationRequest> Create(
        MutationRequest request,
        CancellationToken cancellationToken = default)
        => _persistence.Create(request, cancellationToken);

    /// <summary>
    /// Attempts to store governed mutation request update using optimistic concurrency.
    /// </summary>
    /// <param name="request">The request to store.</param>
    /// <param name="expectedRevision">The expected current revision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted request if the update succeeds; otherwise <see langword="null" />.</returns>
    public Task<MutationRequest?> TryStore(
        MutationRequest request,
        long expectedRevision,
        CancellationToken cancellationToken = default)
        => _persistence.TryStore(request, expectedRevision, cancellationToken);

    /// <summary>
    /// Reads governed mutation request by identifier.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request if it exists; otherwise <see langword="null" />.</returns>
    public Task<MutationRequest?> Get(
        string requestId,
        CancellationToken cancellationToken = default)
        => _persistence.Get(requestId, cancellationToken);

    /// <summary>
    /// Reads governed mutation requests for specific state identifier.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> GetByStateId(
        string stateId,
        CancellationToken cancellationToken = default)
        => _queryReader.GetByStateId(stateId, cancellationToken);

    /// <summary>
    /// Reads pending governed mutation requests, optionally narrowed by reason.
    /// </summary>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> GetPending(
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
        => _queryReader.GetPending(reason, cancellationToken);

    /// <summary>
    /// Reads pending governed mutation requests for specific state identifier.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> GetPendingByStateId(
        string stateId,
        PendingMutationReason? reason = null,
        CancellationToken cancellationToken = default)
        => _queryReader.GetPendingByStateId(stateId, reason, cancellationToken);

    /// <summary>
    /// Reads governed mutation requests matching the supplied query.
    /// </summary>
    /// <param name="query">The request query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> QueryAsync(
        MutationRequestQuery query,
        CancellationToken cancellationToken = default)
        => _queryReader.QueryAsync(query, cancellationToken);

    /// <summary>
    /// Reads pending governed mutation requests using an optional additional query filter.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching pending requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> GetPendingRequestsAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default)
        => _queryReader.GetPendingRequestsAsync(query, cancellationToken);

    /// <summary>
    /// Reads the pending approval queue using an optional additional query filter.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching pending approval-queue requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> GetPendingApprovalQueueAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default)
        => _queryReader.GetPendingApprovalQueueAsync(query, cancellationToken);

    /// <summary>
    /// Recently reads approval active governed mutation requests.
    /// </summary>
    /// <param name="query">The optional request query.</param>
    /// <param name="take">The optional result limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> GetRecentApprovalsAsync(
        MutationRequestQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default)
        => _queryReader.GetRecentApprovalsAsync(query, take, cancellationToken);

    /// <summary>
    /// Reads pending approval views using an optional approval query filter.
    /// </summary>
    /// <param name="query">The optional approval query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching approval views.</returns>
    public Task<IReadOnlyList<MutationApprovalView>> GetPendingApprovalsAsync(
        MutationApprovalQuery? query = null,
        CancellationToken cancellationToken = default)
        => _queryReader.GetPendingApprovalsAsync(query, cancellationToken);

    /// <summary>
    /// Reads recent decision views using an optional decision query filter.
    /// </summary>
    /// <param name="query">The optional decision query.</param>
    /// <param name="take">The optional result limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching decision views.</returns>
    public Task<IReadOnlyList<MutationRequestDecisionView>> GetRecentDecisionsAsync(
        MutationRequestDecisionQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default)
        => _queryReader.GetRecentDecisionsAsync(query, take, cancellationToken);
}
