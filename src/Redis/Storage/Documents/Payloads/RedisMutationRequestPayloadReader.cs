using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Documents.Payloads;

/// <summary>
/// Loads raw request document payloads from Redis.
/// </summary>
internal sealed class RedisMutationRequestPayloadReader(IDatabase database)
{
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    /// <summary>
    /// Loads raw payload values for the supplied document keys.
    /// </summary>
    /// <param name="keys">The Redis document keys to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw Redis payload values.</returns>
    public async Task<IReadOnlyList<RedisValue>> LoadAsync(IReadOnlyList<RedisKey> keys, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (keys.Count == 0)
            return [];

        var values = await _database.StringGetAsync(keys.ToArray()).ConfigureAwait(false);
        return values;
    }
}
