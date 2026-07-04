using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Queries.Support;

/// <summary>
/// Seeds pending request read scenarios.
/// </summary>
internal static class PendingRequestQueryBenchmarkSeed
{
    private static readonly DateTimeOffset BaseTime = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates a store preloaded with pending request and noise data.
    /// </summary>
    public static InMemoryMutationRequestStore CreateStore()
    {
        var store = new InMemoryMutationRequestStore();

        for (var index = 0; index < 96; index++)
        {
            var request = MutationRequestFactory.Pending(
                stateId: "governance-benchmark:query-pending",
                stateType: "GovernanceState",
                mutationType: "GovernanceBenchmarkMutation",
                intent: CreateIntent(),
                context: MutationContext.User("requester", "Requester", "List pending governance requests"),
                pendingReason: index % 2 == 0
                    ? PendingMutationReason.Approval
                    : PendingMutationReason.ExternalCheck)
            with
            {
                RequestId = $"pending-request-{index:000}",
                Lifecycle = new MutationRequestLifecycleDetails
                {
                    Status = MutationRequestStatus.Pending,
                    PendingReason = index % 2 == 0
                        ? PendingMutationReason.Approval
                        : PendingMutationReason.ExternalCheck,
                    CreatedAt = BaseTime.AddMinutes(index),
                    UpdatedAt = BaseTime.AddMinutes(index)
                }
            };

            store.Create(request).GetAwaiter().GetResult();
        }

        for (var index = 0; index < 48; index++)
        {
            var request = new MutationRequest
            {
                RequestId = $"completed-request-{index:000}",
                Scope = new MutationRequestScopeDetails
                {
                    StateId = "governance-benchmark:query-pending",
                    StateType = "GovernanceState",
                    MutationType = "GovernanceBenchmarkCompletedMutation"
                },
                Payload = new MutationRequestPayloadDetails
                {
                    Intent = CreateIntent("CompletedRequest", "Completed governance request"),
                    Context = MutationContext.User("requester", "Requester", "Completed request")
                },
                Lifecycle = new MutationRequestLifecycleDetails
                {
                    Status = index % 3 == 0
                        ? MutationRequestStatus.Approved
                        : MutationRequestStatus.Executed,
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
            OperationName = "ListPendingRequests",
            Category = "Governance",
            Description = "List pending governance requests",
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
