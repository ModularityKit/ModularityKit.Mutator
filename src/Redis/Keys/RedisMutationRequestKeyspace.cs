using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Configuration;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Keys;

/// <summary>
/// Centralizes Redis key naming for governed mutation requests.
/// </summary>
public sealed class RedisMutationRequestKeyspace
{
    private readonly string _keyPrefix;

    /// <summary>
    /// Initializes a new keyspace instance for the configured Redis prefix.
    /// </summary>
    /// <param name="options">The Redis provider options.</param>
    public RedisMutationRequestKeyspace(RedisMutationRequestStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.KeyPrefix))
            throw new ArgumentException("Redis key prefix cannot be empty.", nameof(options));

        _keyPrefix = options.KeyPrefix;
    }

    /// <summary>
    /// Gets the Redis key that stores all known request identifiers.
    /// </summary>
    /// <returns>The Redis key for the global request-id set.</returns>
    public RedisKey RequestIds() => $"{_keyPrefix}:requests:ids";

    /// <summary>
    /// Gets the Redis key that stores the serialized document for a request.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <returns>The Redis key for the request document.</returns>
    public RedisKey RequestData(string requestId) => $"{_keyPrefix}:requests:{requestId}:data";

    /// <summary>
    /// Gets the Redis key that stores the optimistic-concurrency revision for a request.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <returns>The Redis key for the request revision.</returns>
    public RedisKey RequestRevision(string requestId) => $"{_keyPrefix}:requests:{requestId}:revision";

    /// <summary>
    /// Gets the Redis key for requests grouped by state identifier.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <returns>The Redis key for requests targeting the supplied state.</returns>
    public RedisKey RequestsByStateId(string stateId) => $"{_keyPrefix}:states:{stateId}:requests";

    /// <summary>
    /// Gets the Redis key for requests grouped by governance status.
    /// </summary>
    /// <param name="status">The request status.</param>
    /// <returns>The Redis key for requests in the supplied status.</returns>
    public RedisKey RequestsByStatus(MutationRequestStatus status)
        => $"{_keyPrefix}:status:{status.ToString().ToLowerInvariant()}:requests";

    /// <summary>
    /// Gets the Redis key for all pending requests.
    /// </summary>
    /// <returns>The Redis key for the global pending-request set.</returns>
    public RedisKey PendingRequestIds() => $"{_keyPrefix}:pending:requests";

    /// <summary>
    /// Gets the Redis key for pending requests grouped by pending reason.
    /// </summary>
    /// <param name="reason">The pending reason.</param>
    /// <returns>The Redis key for the pending-request set of the supplied reason.</returns>
    public RedisKey PendingRequestIds(PendingMutationReason reason)
        => $"{_keyPrefix}:pending:{reason.ToString().ToLowerInvariant()}:requests";

    /// <summary>
    /// Enumerates the secondary-index keys that should contain the supplied request.
    /// </summary>
    /// <param name="request">The request to index.</param>
    /// <returns>The Redis keys representing all indexes for the request.</returns>
    internal IReadOnlyList<RedisKey> EnumerateIndexes(MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var keys = new List<RedisKey>
        {
            RequestIds(),
            RequestsByStateId(request.StateId),
            RequestsByStatus(request.Status)
        };

        if (request.Status == MutationRequestStatus.Pending)
        {
            keys.Add(PendingRequestIds());

            if (request.PendingReason.HasValue)
                keys.Add(PendingRequestIds(request.PendingReason.Value));
        }

        return keys;
    }
}
