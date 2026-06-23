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
    public void PendingApproval_maps_policy_requirements_into_visible_request_approval_requirements()
    {
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
                    Description = "Security and finance approval",
                    Data = new
                    {
                        Approvers = new[] { "bob", "carol" },
                        StepOrder = 2,
                        Reason = "Cross-functional sign-off"
                    }
                }
            ],
            expectedStateVersion: "v10");

        Assert.Equal(MutationRequestStatus.Pending, request.Status);
        Assert.Equal(PendingMutationReason.Approval, request.PendingReason);
        Assert.Equal(3, request.ApprovalRequirements.Count);
        Assert.Collection(
            request.ApprovalRequirements.OrderBy(requirement => requirement.StepOrder).ThenBy(requirement => requirement.ApproverId),
            first =>
            {
                Assert.Equal("alice", first.ApproverId);
                Assert.Equal(1, first.StepOrder);
                Assert.Equal(MutationApprovalRequirementStatus.Pending, first.Status);
            },
            second =>
            {
                Assert.Equal("bob", second.ApproverId);
                Assert.Equal(2, second.StepOrder);
            },
            third =>
            {
                Assert.Equal("carol", third.ApproverId);
                Assert.Equal(2, third.StepOrder);
            });
    }

    [Fact]
    public async Task ApproveRequirement_enforces_step_order_and_marks_request_approved_after_final_approval()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(CreateMultiStepApprovalRequest());

        var stepTwoApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverId == "carol");
        var invalidStep = await Assert.ThrowsAsync<InvalidMutationApprovalActionException>(() =>
            manager.ApproveRequirement(
                request.RequestId,
                stepTwoApproval.ApprovalId,
                MutationContext.User("carol", "Carol", "Approve too early")));

        Assert.Equal(stepTwoApproval.ApprovalId, invalidStep.ApprovalId);

        var aliceApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverId == "alice");
        var afterAlice = await manager.ApproveRequirement(
            request.RequestId,
            aliceApproval.ApprovalId,
            MutationContext.User("alice", "Alice", "Manager approved"));

        Assert.Equal(MutationRequestStatus.Pending, afterAlice.Status);
        Assert.Equal(MutationApprovalRequirementStatus.Approved, afterAlice.ApprovalRequirements.Single(requirement => requirement.ApproverId == "alice").Status);

        var bobApproval = afterAlice.ApprovalRequirements.Single(requirement => requirement.ApproverId == "bob");
        var afterBob = await manager.ApproveRequirement(
            request.RequestId,
            bobApproval.ApprovalId,
            MutationContext.User("bob", "Bob", "Security approved"));

        Assert.Equal(MutationRequestStatus.Pending, afterBob.Status);

        var finalCarolApproval = afterBob.ApprovalRequirements.Single(requirement => requirement.ApproverId == "carol");
        var afterCarol = await manager.ApproveRequirement(
            request.RequestId,
            finalCarolApproval.ApprovalId,
            MutationContext.User("carol", "Carol", "Finance approved"));

        Assert.Equal(MutationRequestStatus.Approved, afterCarol.Status);
        Assert.Null(afterCarol.PendingReason);
        Assert.All(afterCarol.ApprovalRequirements, requirement => Assert.Equal(MutationApprovalRequirementStatus.Approved, requirement.Status));
        Assert.Equal(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
            afterCarol.Decisions[^1].Type);
        Assert.Contains(
            afterCarol.Decisions,
            decision => decision.Type == MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Granted));
    }

    [Fact]
    public async Task RejectRequirement_marks_request_rejected_and_records_explicit_history()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(CreateMultiStepApprovalRequest());
        var aliceApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverId == "alice");

        var rejected = await manager.RejectRequirement(
            request.RequestId,
            aliceApproval.ApprovalId,
            MutationContext.User("alice", "Alice", "Manager rejected"),
            reason: "Insufficient justification");

        Assert.Equal(MutationRequestStatus.Rejected, rejected.Status);
        Assert.Null(rejected.PendingReason);
        Assert.Equal(MutationApprovalRequirementStatus.Rejected, rejected.ApprovalRequirements.Single(requirement => requirement.ApprovalId == aliceApproval.ApprovalId).Status);
        Assert.Equal(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Rejected),
            rejected.Decisions[^1].Type);
        Assert.Contains(
            rejected.Decisions,
            decision => decision.Type == MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Rejected));
        Assert.Contains(rejected.Decisions, decision => decision.Reason == "Insufficient justification");
    }

    private static MutationRequest CreateMultiStepApprovalRequest()
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
                    Description = "Security review",
                    Data = new
                    {
                        Approver = "bob",
                        StepOrder = 1,
                        Reason = "Security sign-off"
                    }
                },
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Finance review",
                    Data = new
                    {
                        Approver = "carol",
                        StepOrder = 2,
                        Reason = "Budget sign-off"
                    }
                }
            ],
            expectedStateVersion: "v10");
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
