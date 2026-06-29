namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Controls how tag filters are evaluated.
/// </summary>
public enum MutationRequestTagMatchMode
{
    /// <summary>
    /// Match requests that contain at least one of the requested tags.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Match requests that contain all requested tags.
    /// </summary>
    All = 1
}
