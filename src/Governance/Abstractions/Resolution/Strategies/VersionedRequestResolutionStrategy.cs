namespace ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;

/// <summary>
/// Strategy to apply when a mutation request is resolved against a newer state version than expected.
/// </summary>
public enum VersionedRequestResolutionStrategy
{
    /// <summary>
    /// Reject the request if the observed state version differs from the expected version.
    /// </summary>
    RejectStale = 0,
    /// <summary>
    /// Send the request back through approval when the state has drifted.
    /// </summary>
    RequireRenewedApproval = 1,
    /// <summary>
    /// Revalidate the request against the latest state before execution.
    /// </summary>
    RevalidateOnLatestState = 2
}
