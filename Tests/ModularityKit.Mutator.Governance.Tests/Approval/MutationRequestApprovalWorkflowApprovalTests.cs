using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Runtime.Approval.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Approval.Workflow;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Approval;

public sealed partial class MutationRequestApprovalWorkflowTests
{
    [Fact]
    public void PendingApproval_maps_id_role_group_quorum_and_expiration_targets()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var request = MutationRequestApprovalWorkflowTestSupport.CreateLinearApprovalRequest();

        Assert.Equal(MutationRequestStatus.Pending, request.Status);
        Assert.Equal(PendingMutationReason.Approval, request.PendingReason);
        Assert.Equal(6, request.ApprovalRequirements.Count);

        var securityApprovals = request.ApprovalRequirements
            .Where(requirement => requirement.ApprovalGroupId == "security-quorum")
            .OrderBy(requirement => requirement.ApproverId)
            .ToList();

        Assert.Equal(3, securityApprovals.Count);

        void Action(MutationApprovalRequirement requirement)
        {
            Assert.Equal(2, requirement.RequiredApprovals);
            Assert.Equal(expiresAt, requirement.ExpiresAt);
            Assert.Equal(2, requirement.StepOrder);
        }

        Assert.All(securityApprovals, Action);

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
        var request = await store.Create(MutationRequestApprovalWorkflowTestSupport.CreateQuorumApprovalRequest());

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
            MutationRequestApprovalWorkflowTestSupport.CreateRoleContext("frank", "Frank", "Finance approved", "finance-approver"));

        Assert.Equal(MutationRequestStatus.Approved, afterFinance.Status);
        Assert.Null(afterFinance.PendingReason);
    }

    [Fact]
    public async Task ApproveRequirement_accepts_role_and_group_targeting()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(MutationRequestApprovalWorkflowTestSupport.CreateRoleAndGroupApprovalRequest());

        var roleApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverRole == "security-admin");
        var afterRole = await manager.ApproveRequirement(
            request.RequestId,
            roleApproval.ApprovalId,
            MutationRequestApprovalWorkflowTestSupport.CreateRoleContext("sara", "Sara", "Security role approved", "security-admin"));

        Assert.Equal(MutationApprovalRequirementStatus.Approved, afterRole.ApprovalRequirements.Single(requirement => requirement.ApprovalId == roleApproval.ApprovalId).Status);
        Assert.Equal(MutationRequestStatus.Pending, afterRole.Status);

        var groupApproval = afterRole.ApprovalRequirements.Single(requirement => requirement.ApproverGroup == "ops-oncall");
        var afterGroup = await manager.ApproveRequirement(
            request.RequestId,
            groupApproval.ApprovalId,
            MutationRequestApprovalWorkflowTestSupport.CreateGroupContext("oliver", "Oliver", "Operations approved", "ops-oncall"));

        Assert.Equal(MutationRequestStatus.Approved, afterGroup.Status);
    }
}
