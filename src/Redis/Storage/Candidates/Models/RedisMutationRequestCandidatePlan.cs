using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Models;

/// <summary>
/// Represents a planned Redis candidate-id lookup operation.
/// </summary>
/// <param name="Operation">The candidate lookup operation to execute.</param>
/// <param name="Keys">The Redis keys participating in the operation.</param>
/// <param name="ExplicitRequestIds">The explicit request identifiers when no Redis set lookup is required.</param>
internal sealed record RedisMutationRequestCandidatePlan(
    RedisMutationRequestCandidateOperation Operation,
    IReadOnlyList<RedisKey> Keys,
    IReadOnlyList<string>? ExplicitRequestIds = null);
