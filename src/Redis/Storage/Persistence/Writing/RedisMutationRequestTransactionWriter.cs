using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Models;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Writing;

/// <summary>
/// Appends Redis transaction commands for governed mutation request create and update operations.
/// </summary>
internal sealed class RedisMutationRequestTransactionWriter(
    RedisMutationRequestIndexWriter indexWriter)
{
    private readonly RedisMutationRequestIndexWriter _indexWriter = indexWriter ?? throw new ArgumentNullException(nameof(indexWriter));

    /// <summary>
    /// Writes the Redis transaction commands required to create a new request.
    /// </summary>
    /// <param name="transaction">The Redis transaction to append commands to.</param>
    /// <param name="record">The persistence record to store.</param>
    /// <param name="request">The request being created.</param>
    public void WriteCreate(ITransaction transaction, RedisMutationRequestPersistenceRecord record, MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(request);

        _ = transaction.AddCondition(Condition.KeyNotExists(record.DataKey));
        _ = transaction.AddCondition(Condition.KeyNotExists(record.RevisionKey));
        _ = transaction.StringSetAsync(record.DataKey, record.Payload);
        _ = transaction.StringSetAsync(record.RevisionKey, record.Revision);
        _indexWriter.Add(transaction, request);
    }

    /// <summary>
    /// Writes the Redis transaction commands required to update an existing request revision.
    /// </summary>
    /// <param name="transaction">The Redis transaction to append commands to.</param>
    /// <param name="record">The persistence record to store.</param>
    /// <param name="expectedRevision">The expected current revision value.</param>
    /// <param name="currentRequest">The currently stored request state.</param>
    /// <param name="persistedRequest">The updated request state to persist.</param>
    public void WriteUpdate(
        ITransaction transaction,
        RedisMutationRequestPersistenceRecord record,
        long expectedRevision,
        MutationRequest currentRequest,
        MutationRequest persistedRequest)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(currentRequest);
        ArgumentNullException.ThrowIfNull(persistedRequest);

        _ = transaction.AddCondition(Condition.StringEqual(record.RevisionKey, expectedRevision));
        _ = transaction.StringSetAsync(record.DataKey, record.Payload);
        _ = transaction.StringSetAsync(record.RevisionKey, record.Revision);
        _indexWriter.Remove(transaction, currentRequest);
        _indexWriter.Add(transaction, persistedRequest);
    }
}
