using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Queries.Support;

/// <summary>
/// Seeds pending approval queue scenarios.
/// </summary>
internal static class PendingApprovalQueryBenchmarkSeed
{
    private static readonly DateTimeOffset BaseTime = new(2026, 6, 1, 14, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates a store preloaded with pending approval queue and noise data.
    /// </summary>
    public static InMemoryMutationRequestStore CreateStore()
    {
        var store = new InMemoryMutationRequestStore();

        for (var index = 0; index < 64; index++)
        {
            var approverId = index % 2 == 0 ? "security-lead" : "platform-lead";
            var category = index % 2 == 0 ? "Security" : "Platform";

            var request = MutationRequestFactory.PendingApproval(
                stateId: "governance-benchmark:query-approval",
                stateType: "GovernanceState",
                mutationType: "GovernanceBenchmarkApprovalMutation",
                intent: CreateIntent(),
                context: MutationContext.User("requester", "Requester", "List pending approvals"),
                requirements:
                [
                    PolicyRequirement.Approval(approverId, "Benchmark approval")
                ],
                expectedStateVersion: "v10")
            with
            {
                RequestId = $"approval-request-{index:000}",
                Lifecycle = new MutationRequestLifecycleDetails
                {
                    Status = MutationRequestStatus.Pending,
                    PendingReason = PendingMutationReason.Approval,
                    CreatedAt = BaseTime.AddMinutes(index),
                    UpdatedAt = BaseTime.AddMinutes(index)
                },
                ApprovalRequirements =
                [
                    new MutationApprovalRequirement
                    {
                        ApproverId = approverId,
                        ApproverRole = category == "Security" ? "SecurityLead" : "PlatformLead",
                        ApproverGroup = category.ToLowerInvariant(),
                        Status = MutationApprovalRequirementStatus.Pending,
                        StepOrder = 1
                    }
                ]
            };

            store.Create(request).GetAwaiter().GetResult();
        }

        for (var index = 0; index < 48; index++)
        {
            var request = new MutationRequest
            {
                RequestId = $"approval-noise-{index:000}",
                Scope = new MutationRequestScopeDetails
                {
                    StateId = "governance-benchmark:query-approval",
                    StateType = "GovernanceState",
                    MutationType = "GovernanceBenchmarkCompletedMutation"
                },
                Payload = new MutationRequestPayloadDetails
                {
                    Intent = CreateIntent("CompletedApprovalNoise", "Completed governance approval noise"),
                    Context = MutationContext.User("requester", "Requester", "Completed request")
                },
                Lifecycle = new MutationRequestLifecycleDetails
                {
                    Status = MutationRequestStatus.Approved,
                    CreatedAt = BaseTime.AddHours(1).AddMinutes(index),
                    UpdatedAt = BaseTime.AddHours(1).AddMinutes(index + 1)
                }
            };

            store.Create(request).GetAwaiter().GetResult();
        }

        return store;
    }

    private static MutationIntent CreateIntent()
        => new()
        {
            OperationName = "ListPendingApprovals",
            Category = "Governance",
            Description = "List pending approval queue entries",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

    private static MutationIntent CreateIntent(string operationName, string description)
        => new()
        {
            OperationName = operationName,
            Category = "Governance",
            Description = description,
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };
}
