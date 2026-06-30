using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Lifecycle.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace RequestLifecycle.Scenarios;

internal static class GovernanceRequestLifecycleScenario
{
    public static async Task Run()
    {
        var store = new InMemoryMutationRequestStore();
        var lifecycle = new MutationRequestLifecycleManager(store);

        var submittedAt = DateTimeOffset.UtcNow;

        var quotaApprovalRequest = MutationRequestFactory.Pending(
            stateId: "tenant-42:quota",
            stateType: "QuotaPolicy",
            mutationType: "IncreaseQuotaMutation",
            intent: new MutationIntent
            {
                OperationName = "IncreaseQuota",
                Category = "Billing",
                Description = "Increase tenant quota for month-end processing"
            },
            context: MutationContext.User("alice", "Alice", "Need temporary quota uplift"),
            pendingReason: PendingMutationReason.Approval,
            requirements:
            [
                PolicyRequirement.Approval("billing-owner", "Quota increase exceeds standard threshold")
            ],
            expectedStateVersion: "v12",
            expiresAt: submittedAt.AddHours(6),
            metadata: new Dictionary<string, object>
            {
                ["TenantId"] = "tenant-42"
            });

        var scheduledRequest = MutationRequestFactory.Pending(
            stateId: "tenant-42:quota",
            stateType: "QuotaPolicy",
            mutationType: "ResetQuotaMutation",
            intent: new MutationIntent
            {
                OperationName = "ResetQuota",
                Category = "Billing",
                Description = "Scheduled monthly reset"
            },
            context: MutationContext.System("Queued for month-end reset"),
            pendingReason: PendingMutationReason.Schedule,
            expectedStateVersion: "v12",
            expiresAt: submittedAt.AddMinutes(-5));

        var externalCheckRequest = MutationRequestFactory.Approved(
            stateId: "tenant-99:flags",
            stateType: "FeatureFlagState",
            mutationType: "EnableFeatureMutation",
            intent: new MutationIntent
            {
                OperationName = "EnableFeature",
                Category = "Configuration",
                Description = "Enable guarded rollout after dependency check"
            },
            context: MutationContext.Service("release-orchestrator", "Create rollout request"),
            expectedStateVersion: "v3");

        quotaApprovalRequest = await lifecycle.Submit(quotaApprovalRequest);
        scheduledRequest = await lifecycle.Submit(scheduledRequest);
        externalCheckRequest = await lifecycle.Submit(externalCheckRequest);

        externalCheckRequest = await lifecycle.MoveToPending(
            externalCheckRequest.RequestId,
            PendingMutationReason.ExternalCheck,
            MutationContext.Service("release-orchestrator", "Waiting for dependency health signal"),
            reason: "Dependency health check has not completed yet.");

        PrintSection("Pending After Submission");
        PrintRequests(await lifecycle.GetPending());

        PrintSection("Pending For tenant-42:quota");
        PrintRequests(await lifecycle.GetPendingByStateId("tenant-42:quota"));

        quotaApprovalRequest = await lifecycle.Approve(
            quotaApprovalRequest.RequestId,
            MutationContext.User("billing-owner", "Billing Owner", "Quota change approved"),
            reason: "Temporary uplift approved for this billing cycle.");

        externalCheckRequest = await lifecycle.Cancel(
            externalCheckRequest.RequestId,
            MutationContext.Service("release-orchestrator", "Deployment window closed"),
            reason: "Rollout canceled because the deployment window ended.");

        var expiredRequests = await lifecycle.ExpireDueRequests(
            submittedAt,
            MutationContext.System("Expire overdue pending requests"));

        PrintSection("Expired During Sweep");
        PrintRequests(expiredRequests);

        PrintSection("Final Request States");
        PrintRequestDetails(quotaApprovalRequest);
        PrintRequestDetails(externalCheckRequest);
        PrintRequestDetails(await store.Get(scheduledRequest.RequestId) ?? scheduledRequest);
    }

    private static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    private static void PrintRequests(IReadOnlyList<MutationRequest> requests)
    {
        foreach (var request in requests)
        {
            Console.WriteLine(
                $"- {request.RequestId} | {request.Scope.StateId} | {request.Lifecycle.Status} | pending: {request.Lifecycle.PendingReason?.ToString() ?? "-"}");
        }

        if (requests.Count == 0)
            Console.WriteLine("- none");
    }

    private static void PrintRequestDetails(MutationRequest request)
    {
        Console.WriteLine($"{request.RequestId}");
        Console.WriteLine($"  state: {request.Scope.StateId}");
        Console.WriteLine($"  status: {request.Lifecycle.Status}");
        Console.WriteLine($"  pending: {request.Lifecycle.PendingReason?.ToString() ?? "-"}");
        Console.WriteLine($"  expires: {request.Lifecycle.ExpiresAt?.ToString("O") ?? "-"}");
        Console.WriteLine("  decisions:");

        foreach (var decision in request.Decisions)
        {
            Console.WriteLine(
                $"    - {decision.Type} by {decision.Context.ActorId ?? "system"} at {decision.Timestamp:O}");

            if (!string.IsNullOrWhiteSpace(decision.Reason))
                Console.WriteLine($"      reason: {decision.Reason}");
        }
    }
}
