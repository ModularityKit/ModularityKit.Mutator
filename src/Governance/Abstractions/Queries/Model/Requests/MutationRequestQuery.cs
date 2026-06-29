using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;

/// <summary>
/// Defines storage agnostic filters for governed mutation request queries.
/// </summary>
public sealed record MutationRequestQuery
{
    /// <summary>
    /// Identifier oriented filters such as request, state, and mutation identity.
    /// </summary>
    public MutationRequestScopeFilter Scope { get; init; } = new();

    /// <summary>
    /// Actor oriented filters derived from the request context.
    /// </summary>
    public MutationRequestActorFilter Actor { get; init; } = new();

    /// <summary>
    /// Intent oriented filters such as category, tags, metadata, and blast radius.
    /// </summary>
    public MutationRequestIntentFilter Intent { get; init; } = new();

    /// <summary>
    /// Request-level governance metadata filters.
    /// </summary>
    public MutationRequestMetadataFilter Metadata { get; init; } = new();

    /// <summary>
    /// Lifecycle and decision-history filters.
    /// </summary>
    public MutationRequestLifecycleFilter Lifecycle { get; init; } = new();

    /// <summary>
    /// Time-window filters for request creation and update activity.
    /// </summary>
    public MutationRequestTimeRangeFilter TimeRange { get; init; } = new();
}
