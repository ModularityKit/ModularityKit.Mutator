using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Approval.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Approval;

public sealed class MutationRequestApprovalWorkflowTests
{
    [Fact]
    public void PendingApproval_maps_id_role_group_quorum_and_expiration_targets()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var request = MutationRequestFactory.PendingApproval(
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
                        Reason = "Operational readiness"
                    }
                }
            ],
            expectedStateVersion: "v10");

        Assert.Equal(MutationRequestStatus.Pending, request.Status);
        Assert.Equal(PendingMutationReason.Approval, request.PendingReason);
        Assert.Equal(6, request.ApprovalRequirements.Count);

        var securityApprovals = request.ApprovalRequirements
            .Where(requirement => requirement.ApprovalGroupId == "security-quorum")
            .OrderBy(requirement => requirement.ApproverId)
            .ToList();

        Assert.Equal(3, securityApprovals.Count);
        Assert.All(securityApprovals, requirement =>
        {
            Assert.Equal(2, requirement.RequiredApprovals);
            Assert.Equal(expiresAt, requirement.ExpiresAt);
            Assert.Equal(2, requirement.StepOrder);
        });

        var financeApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverRole == "finance-approver");
        Assert.Equal(3, financeApproval.StepOrder);

        var operationsApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverGroup == "ops-oncall");
        Assert.Equal(4, operationsApproval.StepOrder);
    }

    [Fact]
    public async Task ApproveRequirement_supports_quorum_groups_and_marks_remaining_group_requirements_satisfied()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(CreateQuorumApprovalRequest());

        var aliceApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverId == "alice");
        var afterAlice = await manager.ApproveRequirement(
            request.RequestId,
            aliceApproval.ApprovalId,
            MutationContext.User("alice", "Alice", "Manager approved"));

        Assert.Equal(MutationRequestStatus.Pending, afterAlice.Status);

        var bobApproval = afterAlice.ApprovalRequirements.Single(requirement => requirement.ApproverId == "bob");
        var afterBob = await manager.ApproveRequirement(
            request.RequestId,
            bobApproval.ApprovalId,
            MutationContext.User("bob", "Bob", "Security approved"));

        Assert.Equal(MutationRequestStatus.Pending, afterBob.Status);

        var carolApproval = afterBob.ApprovalRequirements.Single(requirement => requirement.ApproverId == "carol");
        var afterCarol = await manager.ApproveRequirement(
            request.RequestId,
            carolApproval.ApprovalId,
            MutationContext.User("carol", "Carol", "Security approved"));

        Assert.Equal(MutationRequestStatus.Pending, afterCarol.Status);
        Assert.Contains(afterCarol.Decisions, decision =>
            decision.Type == MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.QuorumSatisfied));

        var securityGroup = afterCarol.ApprovalRequirements
            .Where(requirement => requirement.ApprovalGroupId == "security-quorum")
            .ToList();

        Assert.Equal(2, securityGroup.Count(requirement => requirement.Status == MutationApprovalRequirementStatus.Approved));
        Assert.Equal(1, securityGroup.Count(requirement => requirement.Status == MutationApprovalRequirementStatus.Satisfied));

        var financeApproval = afterCarol.ApprovalRequirements.Single(requirement => requirement.ApproverRole == "finance-approver");
        var afterFinance = await manager.ApproveRequirement(
            request.RequestId,
            financeApproval.ApprovalId,
            CreateRoleContext("frank", "Frank", "Finance approved", "finance-approver"));

        Assert.Equal(MutationRequestStatus.Approved, afterFinance.Status);
        Assert.Null(afterFinance.PendingReason);
    }

    [Fact]
    public async Task ApproveRequirement_accepts_role_and_group_targeting()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(CreateRoleAndGroupApprovalRequest());

        var roleApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverRole == "security-admin");
        var afterRole = await manager.ApproveRequirement(
            request.RequestId,
            roleApproval.ApprovalId,
            CreateRoleContext("sara", "Sara", "Security role approved", "security-admin"));

        Assert.Equal(MutationApprovalRequirementStatus.Approved, afterRole.ApprovalRequirements.Single(requirement => requirement.ApprovalId == roleApproval.ApprovalId).Status);
        Assert.Equal(MutationRequestStatus.Pending, afterRole.Status);

        var groupApproval = afterRole.ApprovalRequirements.Single(requirement => requirement.ApproverGroup == "ops-oncall");
        var afterGroup = await manager.ApproveRequirement(
            request.RequestId,
            groupApproval.ApprovalId,
            CreateGroupContext("oliver", "Oliver", "Operations approved", "ops-oncall"));

        Assert.Equal(MutationRequestStatus.Approved, afterGroup.Status);
    }

    [Fact]
    public async Task RejectRequirement_persists_structured_rejection_reason()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(CreateLinearApprovalRequest());
        var aliceApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverId == "alice");

        var rejection = new MutationApprovalRejectionReason
        {
            Code = "missing-justification",
            Category = "policy",
            Message = "Change request did not include business justification.",
            Metadata = new Dictionary<string, object>
            {
                ["TicketId"] = "CHG-42"
            }
        };

        var rejected = await manager.RejectRequirement(
            request.RequestId,
            aliceApproval.ApprovalId,
            MutationContext.User("alice", "Alice", "Manager rejected"),
            rejection: rejection);

        var rejectedRequirement = rejected.ApprovalRequirements.Single(requirement => requirement.ApprovalId == aliceApproval.ApprovalId);

        Assert.Equal(MutationRequestStatus.Rejected, rejected.Status);
        Assert.Equal(MutationApprovalRequirementStatus.Rejected, rejectedRequirement.Status);
        Assert.NotNull(rejectedRequirement.Rejection);
        Assert.Equal("missing-justification", rejectedRequirement.Rejection!.Code);
        Assert.Contains(rejected.Decisions, decision =>
            decision.Type == MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Rejected) &&
            Equals(decision.Metadata["RejectionCode"], "missing-justification"));
    }

    [Fact]
    public async Task ExpirePendingApprovals_rejects_requests_with_expired_approval_requirements()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(CreateExpiredApprovalRequest());

        var expired = await manager.ExpirePendingApprovals(
            DateTimeOffset.UtcNow,
            MutationContext.Service("approval-timeout-monitor", "Expire stale approvals"));

        var expiredRequest = Assert.Single(expired);

        Assert.Equal(request.RequestId, expiredRequest.RequestId);
        Assert.Equal(MutationRequestStatus.Rejected, expiredRequest.Status);
        Assert.Contains(expiredRequest.ApprovalRequirements, requirement => requirement.Status == MutationApprovalRequirementStatus.Expired);
        Assert.Contains(expiredRequest.Decisions, decision =>
            decision.Type == MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Expired));
    }

    [Fact]
    public async Task ApproveRequirement_throws_domain_exception_when_requirement_is_expired()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(CreateExpiredApprovalRequest());
        var expiredApproval = request.ApprovalRequirements.Single();

        var exception = await Assert.ThrowsAsync<MutationApprovalRequirementExpiredException>(() =>
            manager.ApproveRequirement(
                request.RequestId,
                expiredApproval.ApprovalId,
                MutationContext.User("alice", "Alice", "Approve expired requirement")));

        Assert.Equal(request.RequestId, exception.RequestId);
        Assert.Equal(expiredApproval.ApprovalId, exception.ApprovalId);
    }

    private static MutationRequest CreateLinearApprovalRequest()
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

    private static MutationRequest CreateQuorumApprovalRequest()
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

    private static MutationRequest CreateRoleAndGroupApprovalRequest()
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

    private static MutationRequest CreateExpiredApprovalRequest()
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

    private static MutationContext CreateRoleContext(
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

    private static MutationContext CreateGroupContext(
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

    private static MutationIntent CreateIntent()
    {
        return new MutationIntent
        {
            OperationName = "GrantRole",
            Category = "Security",
            Description = "Grant elevated role to tenant operator"
        };
    }
}
