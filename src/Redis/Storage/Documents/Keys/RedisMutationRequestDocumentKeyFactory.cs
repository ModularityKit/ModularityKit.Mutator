using ModularityKit.Mutator.Governance.Redis.Keys;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Documents.Keys;

/// <summary>
/// Creates Redis document keys for governed mutation requests.
/// </summary>
internal sealed class RedisMutationRequestDocumentKeyFactory(RedisMutationRequestKeyspace keyspace)
{
    private readonly RedisMutationRequestKeyspace _keyspace = keyspace ?? throw new ArgumentNullException(nameof(keyspace));

    /// <summary>
    /// Creates document keys for the supplied request identifiers.
    /// </summary>
    /// <param name="requestIds">The request identifiers to map.</param>
    /// <returns>The Redis document keys.</returns>
    public IReadOnlyList<RedisKey> CreateKeys(IEnumerable<string> requestIds) =>
        requestIds.Where(requestId => !string.IsNullOrWhiteSpace(requestId))
        .Distinct(StringComparer.Ordinal)
        .Select(_keyspace.RequestData)
        .ToArray();
}
