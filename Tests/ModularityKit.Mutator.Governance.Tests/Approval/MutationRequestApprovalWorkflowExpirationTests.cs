using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;
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
    public async Task RejectRequirement_persists_structured_rejection_reason()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = await store.Create(MutationRequestApprovalWorkflowTestSupport.CreateLinearApprovalRequest());
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
        var request = await store.Create(MutationRequestApprovalWorkflowTestSupport.CreateExpiredApprovalRequest());

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
        var request = await store.Create(MutationRequestApprovalWorkflowTestSupport.CreateExpiredApprovalRequest());
        var expiredApproval = request.ApprovalRequirements.Single();

        var exception = await Assert.ThrowsAsync<MutationApprovalRequirementExpiredException>(() =>
            manager.ApproveRequirement(
                request.RequestId,
                expiredApproval.ApprovalId,
                MutationContext.User("alice", "Alice", "Approve expired requirement")));

        Assert.Equal(request.RequestId, exception.RequestId);
        Assert.Equal(expiredApproval.ApprovalId, exception.ApprovalId);
    }
}
