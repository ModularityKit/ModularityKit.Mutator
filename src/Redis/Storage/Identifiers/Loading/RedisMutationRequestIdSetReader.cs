using ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Models;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Loading;

/// <summary>
/// Provides higher level Redis request id set reads for candidate execution.
/// </summary>
internal sealed class RedisMutationRequestIdSetReader(
    RedisMutationRequestIdentifierSetLoader setLoader)
{
    private readonly RedisMutationRequestIdentifierSetLoader _setLoader = setLoader ?? throw new ArgumentNullException(nameof(setLoader));

    /// <summary>
    /// Loads request identifiers from a single Redis set.
    /// </summary>
    /// <param name="key">The Redis set key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public async Task<IReadOnlyList<string>> LoadIdsAsync(RedisKey key, CancellationToken cancellationToken) =>
        await _setLoader.LoadAsync(RedisMutationRequestIdentifierSetOperation.Members, [key], cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Loads request identifiers from the union of the supplied Redis sets.
    /// </summary>
    /// <param name="keys">The Redis set keys to union.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public async Task<IReadOnlyList<string>> LoadUnionedIdsAsync(IReadOnlyList<RedisKey> keys, CancellationToken cancellationToken) =>
        await _setLoader.LoadAsync(RedisMutationRequestIdentifierSetOperation.Union, keys, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Loads request identifiers from the intersection of two Redis sets.
    /// </summary>
    /// <param name="left">The left Redis set key.</param>
    /// <param name="right">The right Redis set key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public async Task<IReadOnlyList<string>> LoadIntersectedIdsAsync(RedisKey left, RedisKey right, CancellationToken cancellationToken) =>
        await _setLoader.LoadAsync(RedisMutationRequestIdentifierSetOperation.Intersection, [left, right], cancellationToken)
            .ConfigureAwait(false);
}
