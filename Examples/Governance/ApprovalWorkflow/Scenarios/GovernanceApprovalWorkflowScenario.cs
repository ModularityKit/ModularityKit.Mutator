using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Approval.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ApprovalWorkflow.Scenarios;

internal static class GovernanceApprovalWorkflowScenario
{
    public static async Task Run()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);

        PrintSection("Submit Pending Approval Request");
        var request = await store.Create(CreateApprovalRequest());
        PrintRequest(request);

        PrintSection("Approve Step 1");
        var aliceApproval = request.ApprovalRequirements.Single(requirement => requirement.ApproverId == "alice");
        var afterAlice = await manager.ApproveRequirement(
            request.RequestId,
            aliceApproval.ApprovalId,
            MutationContext.User("alice", "Alice", "Manager approved"));
        PrintRequest(afterAlice);

        PrintSection("Approve Step 2 - Quorum Approval 1/2");
        var bobApproval = afterAlice.ApprovalRequirements.Single(requirement => requirement.ApproverId == "bob");
        var afterBob = await manager.ApproveRequirement(
            request.RequestId,
            bobApproval.ApprovalId,
            MutationContext.User("bob", "Bob", "Security approved"));
        PrintRequest(afterBob);

        PrintSection("Approve Step 2 - Quorum Approval 2/2");
        var carolApproval = afterBob.ApprovalRequirements.Single(requirement => requirement.ApproverId == "carol");
        var afterCarol = await manager.ApproveRequirement(
            request.RequestId,
            carolApproval.ApprovalId,
            MutationContext.User("carol", "Carol", "Finance approved"));
        PrintRequest(afterCarol);

        PrintSection("Approve Step 3 - Role Target");
        var financeApproval = afterCarol.ApprovalRequirements.Single(requirement => requirement.ApproverRole == "finance-approver");
        var afterFinance = await manager.ApproveRequirement(
            request.RequestId,
            financeApproval.ApprovalId,
            MutationContext.User("frank", "Frank", "Finance role approved") with
            {
                Metadata = new Dictionary<string, object>
                {
                    ["ActorRoles"] = new[] { "finance-approver" }
                }
            });
        PrintRequest(afterFinance);

        PrintSection("Expire A Separate Pending Approval Request");
        var expiringRequest = await store.Create(CreateExpiringApprovalRequest());
        var expired = await manager.ExpirePendingApprovals(
            DateTimeOffset.UtcNow,
            MutationContext.Service("approval-timeout-monitor", "Expire stale approvals"));

        var expiredRequest = expired.Single(candidate => candidate.RequestId == expiringRequest.RequestId);
        PrintRequest(expiredRequest);
    }

    private static MutationRequest CreateApprovalRequest()
    {
        return MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated role to tenant operator"
            },
            context: MutationContext.User("requester", "Requester", "Need elevated access for incident"),
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
                    Description = "Finance role review",
                    Data = new
                    {
                        ApproverRole = "finance-approver",
                        StepOrder = 3,
                        Reason = "Budget sign-off"
                    }
                }
            ],
            expectedStateVersion: "v10");
    }

    private static MutationRequest CreateExpiringApprovalRequest()
    {
        return MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:deploy",
            stateType: "DeploymentState",
            mutationType: "ApproveDeploymentMutation",
            intent: new MutationIntent
            {
                OperationName = "ApproveDeployment",
                Category = "Operations",
                Description = "Approval request that will expire"
            },
            context: MutationContext.User("requester", "Requester", "Need emergency deployment approval"),
            requirements:
            [
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Operations group approval",
                    Data = new
                    {
                        ApproverGroup = "ops-oncall",
                        StepOrder = 1,
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                        Reason = "Operational readiness"
                    }
                }
            ],
            expectedStateVersion: "v11");
    }

    private static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    private static void PrintRequest(MutationRequest request)
    {
        Console.WriteLine($"Request status: {request.Status}");
        Console.WriteLine($"Pending reason: {request.PendingReason?.ToString() ?? "-"}");
        Console.WriteLine($"Revision: {request.Revision}");
        Console.WriteLine("Approval requirements:");

        foreach (var requirement in request.ApprovalRequirements.OrderBy(requirement => requirement.StepOrder).ThenBy(requirement => requirement.ApproverId))
        {
            Console.WriteLine(
                $"  - Step {requirement.StepOrder}: {DescribeTarget(requirement)} => {requirement.Status} (group: {requirement.ApprovalGroupId ?? "-" }, quorum: {requirement.RequiredApprovals}, expires: {requirement.ExpiresAt?.ToString("O") ?? "-"})");
        }

        var lastDecision = request.Decisions[^1];
        Console.WriteLine($"Last decision: {lastDecision.Type} by {lastDecision.Context.ActorId ?? "system"}");
        Console.WriteLine($"Reason: {lastDecision.Reason ?? "-"}");
    }

    private static string DescribeTarget(MutationApprovalRequirement requirement)
    {
        if (!string.IsNullOrWhiteSpace(requirement.ApproverId))
            return requirement.ApproverId;

        if (!string.IsNullOrWhiteSpace(requirement.ApproverRole))
            return $"role:{requirement.ApproverRole}";

        if (!string.IsNullOrWhiteSpace(requirement.ApproverGroup))
            return $"group:{requirement.ApproverGroup}";

        return "unknown";
    }
}
