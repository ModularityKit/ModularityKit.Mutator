namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

/// <summary>
/// Groups governance request decisions by the runtime concern that produced them.
/// </summary>
public enum MutationRequestDecisionCategory
{
    /// <summary>
    /// Lifecycle decisions describe request state transitions.
    /// </summary>
    Lifecycle = 0,
    /// <summary>
    /// Approval decisions describe request-level approval processing.
    /// </summary>
    Approval = 1,
    /// <summary>
    /// Version-resolution decisions describe stale and version-aware handling.
    /// </summary>
    VersionResolution = 2
}
