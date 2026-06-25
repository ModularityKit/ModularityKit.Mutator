using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Models;

/// <summary>
/// Represents the Redis persistence payload for governed mutation request write.
/// </summary>
/// <param name="RequestId">The governed request identifier.</param>
/// <param name="DataKey">The Redis key for the serialized request document.</param>
/// <param name="RevisionKey">The Redis key for the request revision value.</param>
/// <param name="Payload">The serialized request payload.</param>
/// <param name="Revision">The revision value to persist.</param>
internal sealed record RedisMutationRequestPersistenceRecord(
    string RequestId,
    RedisKey DataKey,
    RedisKey RevisionKey,
    RedisValue Payload,
    long Revision);
