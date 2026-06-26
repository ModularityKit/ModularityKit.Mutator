using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

/// <summary>
/// Defines storage agnostic filters for decision oriented governance queries.
/// </summary>
public sealed record MutationRequestDecisionQuery
{
    /// <summary>
    /// Request level filters applied before decisions are projected.
    /// </summary>
    public MutationRequestQuery RequestQuery { get; init; } = new();

    /// <summary>
    /// Decision categories to include.
    /// </summary>
    public IReadOnlySet<MutationRequestDecisionCategory> Categories { get; init; }
        = new HashSet<MutationRequestDecisionCategory>();

    /// <summary>
    /// Decision codes to include.
    /// </summary>
    public IReadOnlySet<string> Codes { get; init; } = new HashSet<string>();

    /// <summary>
    /// Decision actor identifiers to include.
    /// </summary>
    public IReadOnlySet<string> ActorIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// Decision actor names to include.
    /// </summary>
    public IReadOnlySet<string> ActorNames { get; init; } = new HashSet<string>();

    /// <summary>
    /// Inclusive lower bound for decision timestamps.
    /// </summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>
    /// Inclusive upper bound for decision timestamps.
    /// </summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>
    /// Creates query for recent version-resolution decisions.
    /// </summary>
    public static MutationRequestDecisionQuery RecentVersionResolutions()
        => new()
        {
            Categories = new HashSet<MutationRequestDecisionCategory>
            {
                MutationRequestDecisionCategory.VersionResolution
            }
        };

    /// <summary>
    /// Creates query for recent execution outcomes.
    /// </summary>
    public static MutationRequestDecisionQuery RecentExecutionOutcomes()
        => new()
        {
            Categories = new HashSet<MutationRequestDecisionCategory>
            {
                MutationRequestDecisionCategory.Lifecycle
            },
            Codes = new HashSet<string>
            {
                MutationRequestLifecycleDecisionType.Executed.ToString(),
                MutationRequestLifecycleDecisionType.Rejected.ToString(),
                MutationRequestLifecycleDecisionType.Canceled.ToString(),
                MutationRequestLifecycleDecisionType.Expired.ToString(),
                MutationRequestLifecycleDecisionType.Superseded.ToString()
            }
        };
}
