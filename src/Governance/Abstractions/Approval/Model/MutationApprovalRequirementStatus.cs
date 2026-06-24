namespace ModularityKit.Mutator.Governance.Abstractions.Approval.Model;

/// <summary>
/// Represents the current state of a request-level approval requirement.
/// </summary>
public enum MutationApprovalRequirementStatus
{
    /// <summary>
    /// The approval requirement is still waiting for a decision.
    /// </summary>
    Pending = 0,
    /// <summary>
    /// The approval requirement has been approved.
    /// </summary>
    Approved = 1,
    /// <summary>
    /// The approval requirement has been rejected.
    /// </summary>
    Rejected = 2,
    /// <summary>
    /// The approval requirement has been satisfied by quorum or equivalent policy.
    /// </summary>
    Satisfied = 3,
    /// <summary>
    /// The approval requirement expired before it was resolved.
    /// </summary>
    Expired = 4
}
