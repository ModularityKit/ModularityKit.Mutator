namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Groups identifier oriented request query filters.
/// </summary>
public sealed record MutationRequestScopeFilter
{
    /// <summary>
    /// Specific request identifiers to include.
    /// </summary>
    public IReadOnlySet<string> RequestIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// State identifiers to include.
    /// </summary>
    public IReadOnlySet<string> StateIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// State types to include.
    /// </summary>
    public IReadOnlySet<string> StateTypes { get; init; } = new HashSet<string>();

    /// <summary>
    /// Mutation types to include.
    /// </summary>
    public IReadOnlySet<string> MutationTypes { get; init; } = new HashSet<string>();
}
