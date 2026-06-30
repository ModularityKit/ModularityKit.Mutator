using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Queries.Builders;

/// <summary>
/// Builds approval and decision views used by query store tests.
/// </summary>
internal static class MutationRequestQueryStoreViewBuilders
{
    /// <summary>
    /// Creates an approval view fixture with the requested approval status.
    /// </summary>
    public static MutationRequest CreateApprovalViewRequest(
        string requestId,
        string approverId,
        string approverRole,
        string approverGroup,
        string category,
        MutationApprovalRequirementStatus approvalStatus)
        => MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = category,
                Tags = new HashSet<string> { "approval" }
            },
            context: MutationContext.User("requester", "Requester", "Need approval"),
            requirements:
            [
                PolicyRequirement.Approval(approverId, "Need review")
            ])
        with
        {
            RequestId = requestId,
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Approval
            },
            ApprovalRequirements =
            [
                new MutationApprovalRequirement
                {
                    ApproverId = approverId,
                    ApproverRole = approverRole,
                    ApproverGroup = approverGroup,
                    Status = approvalStatus,
                    StepOrder = 1
                }
            ]
        };

    /// <summary>
    /// Creates a decision view fixture with the supplied decision history.
    /// </summary>
    public static MutationRequest CreateDecisionViewRequest(
        string requestId,
        IReadOnlyList<MutationRequestDecision> decisions)
        => MutationRequestFactory.Pending(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security"
            },
            context: MutationContext.User("requester", "Requester", "Need execution"),
            pendingReason: PendingMutationReason.ExternalCheck)
        with
        {
            RequestId = requestId,
            Decisions = decisions,
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.ExternalCheck,
                UpdatedAt = decisions.Max(decision => decision.Timestamp)
            }
        };
}
