namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

/// <summary>
/// Represents version-resolution decisions recorded while reconciling expected and current state versions.
/// </summary>
public enum MutationRequestVersionResolutionDecisionType
{
    /// <summary>
    /// The request version matched the current state version.
    /// </summary>
    Validated = 0,
    /// <summary>
    /// The request must be revalidated against the latest state.
    /// </summary>
    RevalidationRequired = 1,
    /// <summary>
    /// The request must obtain renewed approval before proceeding.
    /// </summary>
    RenewedApprovalRequired = 2,
    /// <summary>
    /// The request was rejected because it was stale.
    /// </summary>
    RejectedAsStale = 3
}
