namespace ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Models;

/// <summary>
/// Defines the Redis set operation used to resolve request identifiers.
/// </summary>
internal enum RedisMutationRequestIdentifierSetOperation
{
    /// <summary>
    /// Reads the members of a single Redis set.
    /// </summary>
    Members = 0,

    /// <summary>
    /// Reads the union of multiple Redis sets.
    /// </summary>
    Union = 1,

    /// <summary>
    /// Reads the intersection of multiple Redis sets.
    /// </summary>
    Intersection = 2
}
