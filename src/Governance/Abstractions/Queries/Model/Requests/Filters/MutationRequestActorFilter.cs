namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Groups actor oriented request query filters.
/// </summary>
public sealed record MutationRequestActorFilter
{
    /// <summary>
    /// Actor identifiers to include.
    /// </summary>
    public IReadOnlySet<string> ActorIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// Actor names to include.
    /// </summary>
    public IReadOnlySet<string> ActorNames { get; init; } = new HashSet<string>();
}
