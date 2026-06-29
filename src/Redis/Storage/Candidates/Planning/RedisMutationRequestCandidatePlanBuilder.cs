using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Redis.Keys;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Models;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Planning;

/// <summary>
/// Builds candidate id lookup plans for Redis backed request queries.
/// </summary>
internal sealed class RedisMutationRequestCandidatePlanBuilder(RedisMutationRequestKeyspace keyspace)
{
    private readonly RedisMutationRequestKeyspace _keyspace = keyspace ?? throw new ArgumentNullException(nameof(keyspace));

    /// <summary>
    /// Builds plan that loads all known request identifiers.
    /// </summary>
    /// <returns>The candidate plan.</returns>
    public RedisMutationRequestCandidatePlan BuildAllRequestsPlan()
        => Single(_keyspace.RequestIds());

    /// <summary>
    /// Builds plan that loads request identifiers for specific state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <returns>The candidate plan.</returns>
    public RedisMutationRequestCandidatePlan BuildByStateIdPlan(string stateId)
        => Single(_keyspace.RequestsByStateId(stateId));

    /// <summary>
    /// Builds plan that loads pending request identifiers, optionally narrowed by pending reason.
    /// </summary>
    /// <param name="reason">The optional pending reason.</param>
    /// <returns>The candidate plan.</returns>
    public RedisMutationRequestCandidatePlan BuildPendingPlan(PendingMutationReason? reason)
        => Single(GetPendingKey(reason));

    /// <summary>
    /// Builds plan that loads pending request identifiers for specific state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="reason">The optional pending reason.</param>
    /// <returns>The candidate plan.</returns>
    public RedisMutationRequestCandidatePlan BuildPendingByStateIdPlan(string stateId, PendingMutationReason? reason)
        => Intersect(_keyspace.RequestsByStateId(stateId), GetPendingKey(reason));

    /// <summary>
    /// Builds best effort Redis candidate plan for the supplied request query.
    /// </summary>
    /// <param name="query">The request query to analyze.</param>
    /// <returns>The candidate plan.</returns>
    public RedisMutationRequestCandidatePlan BuildQueryPlan(MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Scope.RequestIds.Count > 0)
            return Explicit(query.Scope.RequestIds);

        if (query.Lifecycle.PendingReasons.Count > 0)
            return Union(query.Lifecycle.PendingReasons.Select(_keyspace.PendingRequestIds));

        if (query.Lifecycle.Statuses.Count > 0)
        {
            var pendingOnly = query.Lifecycle.Statuses.All(status => status == MutationRequestStatus.Pending);
            return pendingOnly
                ? BuildPendingPlan(reason: null)
                : Union(query.Lifecycle.Statuses.Select(_keyspace.RequestsByStatus));
        }

        if (query.Scope.StateIds.Count > 0)
            return Union(query.Scope.StateIds.Select(_keyspace.RequestsByStateId));

        return BuildAllRequestsPlan();
    }

    private RedisMutationRequestCandidatePlan Explicit(IEnumerable<string> requestIds)
        => new(RedisMutationRequestCandidateOperation.ExplicitIds, Keys: [],
            ExplicitRequestIds: requestIds
                .Where(requestId => !string.IsNullOrWhiteSpace(requestId))
                .Distinct(StringComparer.Ordinal)
                .ToArray());

    private RedisMutationRequestCandidatePlan Single(RedisKey key)
        => new(RedisMutationRequestCandidateOperation.SingleSet, Keys: [key]);

    private RedisMutationRequestCandidatePlan Union(IEnumerable<RedisKey> keys)
    {
        var materialized = keys.Distinct().ToArray();
        if (materialized.Length == 1)
            return Single(materialized[0]);

        return new RedisMutationRequestCandidatePlan(
            RedisMutationRequestCandidateOperation.Union,
            materialized);
    }

    private RedisMutationRequestCandidatePlan Intersect(RedisKey left, RedisKey right)
        => new(RedisMutationRequestCandidateOperation.Intersection, [left, right]);

    private RedisKey GetPendingKey(PendingMutationReason? reason)
        => reason.HasValue ? _keyspace.PendingRequestIds(reason.Value) : _keyspace.PendingRequestIds();
}
