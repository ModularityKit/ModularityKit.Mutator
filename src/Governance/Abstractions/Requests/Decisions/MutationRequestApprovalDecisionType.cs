namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

/// <summary>
/// Represents approval-specific decisions recorded during governance workflow.
/// </summary>
public enum MutationRequestApprovalDecisionType
{
    Requested = 0,
    Granted = 1,
    Rejected = 2,
    QuorumSatisfied = 3,
    Expired = 4
}
