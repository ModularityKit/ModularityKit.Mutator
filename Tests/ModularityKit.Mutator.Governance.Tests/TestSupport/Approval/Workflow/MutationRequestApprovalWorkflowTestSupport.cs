using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Approval.Workflow;

/// <summary>
/// Builds mutation requests and contexts used by approval workflow tests.
/// </summary>
internal static class MutationRequestApprovalWorkflowTestSupport
{
    /// <summary>
    /// Creates a request that requires a single approval path.
    /// </summary>
    public static MutationRequest CreateLinearApprovalRequest()
    {
        return MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Needs privileged access"),
            requirements:
            [
                PolicyRequirement.Approval("alice", "Manager approval")
            ],
            expectedStateVersion: "v10");
    }

    /// <summary>
    /// Creates a request that exercises approval mapping for manager, quorum, role, and group targets.
    /// </summary>
    public static MutationRequest CreateTargetMappingApprovalRequest(DateTimeOffset expiresAt)
    {
        return MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Needs privileged access"),
            requirements:
            [
                PolicyRequirement.Approval("alice", "Manager approval"),
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Security quorum",
                    Data = new
                    {
                        Approvers = new[] { "bob", "carol", "dave" },
                        StepOrder = 2,
                        ApprovalGroupId = "security-quorum",
                        Quorum = 2,
                        ExpiresAt = expiresAt,
                        Reason = "Security sign-off"
                    }
                },
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Finance role approval",
                    Data = new
                    {
                        ApproverRole = "finance-approver",
                        StepOrder = 3,
                        Reason = "Finance sign-off"
                    }
                },
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Operations group approval",
                    Data = new
                    {
                        ApproverGroup = "ops-oncall",
                        StepOrder = 4,
                        Reason = "Operations sign-off"
                    }
                }
            ],
            expectedStateVersion: "v10");
    }

    /// <summary>
    /// Creates a request that exercises quorum-based approval requirements.
    /// </summary>
    public static MutationRequest CreateQuorumApprovalRequest()
    {
        return MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Needs privileged access"),
            requirements:
            [
                PolicyRequirement.Approval("alice", "Manager approval"),
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Security quorum",
                    Data = new
                    {
                        Approvers = new[] { "bob", "carol", "dave" },
                        StepOrder = 2,
                        ApprovalGroupId = "security-quorum",
                        Quorum = 2,
                        Reason = "Security sign-off"
                    }
                },
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Finance role approval",
                    Data = new
                    {
                        ApproverRole = "finance-approver",
                        StepOrder = 3,
                        Reason = "Finance sign-off"
                    }
                }
            ],
            expectedStateVersion: "v10");
    }

    /// <summary>
    /// Creates a request that mixes role and group approval requirements.
    /// </summary>
    public static MutationRequest CreateRoleAndGroupApprovalRequest()
    {
        return MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:deploy",
            stateType: "DeploymentState",
            mutationType: "ApproveDeploymentMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need deployment approval"),
            requirements:
            [
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Security role approval",
                    Data = new
                    {
                        ApproverRole = "security-admin",
                        StepOrder = 1,
                        Reason = "Security review"
                    }
                },
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Operations group approval",
                    Data = new
                    {
                        ApproverGroup = "ops-oncall",
                        StepOrder = 2,
                        Reason = "Operational readiness"
                    }
                }
            ],
            expectedStateVersion: "v7");
    }

    /// <summary>
    /// Creates a request whose approval requirements are already expired.
    /// </summary>
    public static MutationRequest CreateExpiredApprovalRequest()
    {
        return MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:billing",
            stateType: "BillingState",
            mutationType: "IncreaseQuotaMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need urgent quota increase"),
            requirements:
            [
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Manager approval",
                    Data = new
                    {
                        Approver = "alice",
                        StepOrder = 1,
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                        Reason = "Manager sign-off"
                    }
                }
            ],
            expectedStateVersion: "v5");
    }

    /// <summary>
    /// Creates a user context that carries role metadata for approval tests.
    /// </summary>
    public static MutationContext CreateRoleContext(
        string actorId,
        string actorName,
        string reason,
        params string[] roles)
    {
        return MutationContext.User(actorId, actorName, reason) with
        {
            Metadata = new Dictionary<string, object>
            {
                ["ActorRoles"] = roles
            }
        };
    }

    /// <summary>
    /// Creates a user context that carries group metadata for approval tests.
    /// </summary>
    public static MutationContext CreateGroupContext(
        string actorId,
        string actorName,
        string reason,
        params string[] groups)
    {
        return MutationContext.User(actorId, actorName, reason) with
        {
            Metadata = new Dictionary<string, object>
            {
                ["ActorGroups"] = groups
            }
        };
    }

    /// <summary>
    /// Creates the baseline intent used by approval workflow scenarios.
    /// </summary>
    public static MutationIntent CreateIntent()
    {
        return new MutationIntent
        {
            OperationName = "GrantRole",
            Category = "Security",
            Description = "Grant elevated role to tenant operator"
        };
    }
}
