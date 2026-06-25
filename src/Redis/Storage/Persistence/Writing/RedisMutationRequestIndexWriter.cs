using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Keys;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Writing;

/// <summary>
/// Maintains Redis secondary indexes for governed mutation request writes.
/// </summary>
internal sealed class RedisMutationRequestIndexWriter(
    RedisMutationRequestKeyspace keyspace)
{
    private readonly RedisMutationRequestKeyspace _keyspace = keyspace ?? throw new ArgumentNullException(nameof(keyspace));

    /// <summary>
    /// Adds a request to all Redis secondary indexes implied by its current state.
    /// </summary>
    /// <param name="transaction">The Redis transaction to append commands to.</param>
    /// <param name="request">The request to index.</param>
    public void Add(ITransaction transaction, MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(request);

        foreach (var indexKey in _keyspace.EnumerateIndexes(request))
            _ = transaction.SetAddAsync(indexKey, request.RequestId);
    }

    /// <summary>
    /// Removes a request from all Redis secondary indexes implied by its current state.
    /// </summary>
    /// <param name="transaction">The Redis transaction to append commands to.</param>
    /// <param name="request">The request to remove from indexes.</param>
    public void Remove(ITransaction transaction, MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(request);

        foreach (var indexKey in _keyspace.EnumerateIndexes(request))
            _ = transaction.SetRemoveAsync(indexKey, request.RequestId);
    }
}
