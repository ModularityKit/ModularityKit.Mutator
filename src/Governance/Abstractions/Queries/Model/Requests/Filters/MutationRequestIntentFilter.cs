using ModularityKit.Mutator.Abstractions.Intent;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Groups intent oriented request query filters.
/// </summary>
public sealed record MutationRequestIntentFilter
{
    /// <summary>
    /// Mutation categories to include.
    /// </summary>
    public IReadOnlySet<string> Categories { get; init; } = new HashSet<string>();

    /// <summary>
    /// Tags to include from the request intent.
    /// </summary>
    public IReadOnlySet<string> Tags { get; init; } = new HashSet<string>();

    /// <summary>
    /// Tag matching strategy.
    /// </summary>
    public MutationRequestTagMatchMode TagMatchMode { get; init; } = MutationRequestTagMatchMode.Any;

    /// <summary>
    /// Exact metadata key/value pairs to match against request intent metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Minimum estimated blast radius scope to include.
    /// </summary>
    public BlastRadiusScope? MinimumBlastRadiusScope { get; init; }

    /// <summary>
    /// Maximum estimated blast radius scope to include.
    /// </summary>
    public BlastRadiusScope? MaximumBlastRadiusScope { get; init; }
}
