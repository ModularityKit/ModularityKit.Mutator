using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Keys;
using ModularityKit.Mutator.Governance.Redis.Serialization;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Reading;

/// <summary>
/// Reads individual governed mutation requests from Redis persistence storage.
/// </summary>
internal sealed class RedisMutationRequestPersistenceDocumentReader(
    IDatabase database,
    RedisMutationRequestKeyspace keyspace)
{
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly RedisMutationRequestKeyspace _keyspace = keyspace ?? throw new ArgumentNullException(nameof(keyspace));

    /// <summary>
    /// Reads a single governed mutation request by identifier.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request if it exists; otherwise <see langword="null" />.</returns>
    public async Task<MutationRequest?> GetAsync(
        string requestId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await _database.StringGetAsync(_keyspace.RequestData(requestId)).ConfigureAwait(false);
        return payload.HasValue
            ? RedisMutationRequestSerializer.Deserialize(payload!)
            : null;
    }
}
