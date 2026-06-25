using ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Models;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Loading;

/// <summary>
/// Executes low-level Redis set operations used to resolve request identifiers.
/// </summary>
internal sealed class RedisMutationRequestIdentifierSetLoader(IDatabase database)
{
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    /// <summary>
    /// Loads request identifiers using the supplied Redis set operation.
    /// </summary>
    /// <param name="operation">The Redis set operation to execute.</param>
    /// <param name="keys">The Redis set keys participating in the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The normalized request identifiers.</returns>
    public async Task<IReadOnlyList<string>> LoadAsync(RedisMutationRequestIdentifierSetOperation operation, IReadOnlyList<RedisKey> keys, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (keys.Count == 0)
            return [];

        var values = operation switch
        {
            RedisMutationRequestIdentifierSetOperation.Members => await _database
                .SetMembersAsync(keys[0])
                .ConfigureAwait(false),
            RedisMutationRequestIdentifierSetOperation.Union => await LoadCombinedAsync(
                SetOperation.Union,
                keys,
                cancellationToken).ConfigureAwait(false),
            RedisMutationRequestIdentifierSetOperation.Intersection => await LoadCombinedAsync(
                SetOperation.Intersect,
                keys,
                cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported identifier set operation '{operation}'.")
        };

        return RedisMutationRequestIdentifierValueNormalizer.Normalize(values);
    }

    private async Task<RedisValue[]> LoadCombinedAsync(SetOperation operation, IReadOnlyList<RedisKey> keys, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (keys.Count == 1)
            return await _database.SetMembersAsync(keys[0]).ConfigureAwait(false);

        return await _database.SetCombineAsync(operation, keys.ToArray()).ConfigureAwait(false);
    }
}
