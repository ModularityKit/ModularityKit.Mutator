namespace ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;

/// <summary>
/// Describes the outcome of version-aware request resolution.
/// </summary>
public enum MutationRequestVersionResolutionOutcome
{
    /// <summary>
    /// The request can be executed with its approved version.
    /// </summary>
    ExecuteApprovedVersion = 0,
    /// <summary>
    /// The request should be revalidated on the latest state.
    /// </summary>
    RevalidateOnLatestState = 1,
    /// <summary>
    /// The request was rejected as stale.
    /// </summary>
    RejectedAsStale = 2,
    /// <summary>
    /// The request requires renewed approval.
    /// </summary>
    RequiresRenewedApproval = 3
}
