namespace ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Models;

/// <summary>
/// Defines how Redis request id candidates should be loaded for queries.
/// </summary>
internal enum RedisMutationRequestCandidateOperation
{
    /// <summary>
    /// Uses an already known explicit list of request identifiers.
    /// </summary>
    ExplicitIds = 0,

    /// <summary>
    /// Loads identifiers from a single Redis set.
    /// </summary>
    SingleSet = 1,

    /// <summary>
    /// Loads identifiers from the union of multiple Redis sets.
    /// </summary>
    Union = 2,

    /// <summary>
    /// Loads identifiers from the intersection of multiple Redis sets.
    /// </summary>
    Intersection = 3
}
