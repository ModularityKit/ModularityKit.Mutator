namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

/// <summary>
/// Represents approval-specific decisions recorded during governance workflow.
/// </summary>
public enum MutationRequestApprovalDecisionType
{
    /// <summary>
    /// The approval requirement was requested.
    /// </summary>
    Requested = 0,
    /// <summary>
    /// The approval requirement was granted.
    /// </summary>
    Granted = 1,
    /// <summary>
    /// The approval requirement was rejected.
    /// </summary>
    Rejected = 2,
    /// <summary>
    /// The approval quorum for a group was satisfied.
    /// </summary>
    QuorumSatisfied = 3,
    /// <summary>
    /// The approval requirement expired.
    /// </summary>
    Expired = 4
}
