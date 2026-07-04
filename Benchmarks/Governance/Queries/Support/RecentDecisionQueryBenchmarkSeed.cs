using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Queries.Support;

/// <summary>
/// Seeds recent decision read scenarios.
/// </summary>
internal static class RecentDecisionQueryBenchmarkSeed
{
    private static readonly DateTimeOffset BaseTime = new(2026, 6, 1, 16, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates a store preloaded with recent decision and noise data.
    /// </summary>
    public static InMemoryMutationRequestStore CreateStore()
    {
        var store = new InMemoryMutationRequestStore();

        for (var index = 0; index < 72; index++)
        {
            var request = CreateDecisionRequest(
                requestId: $"decision-request-{index:000}",
                stateId: "governance-benchmark:query-decision",
                decisionType: (index % 4) switch
                {
                    0 => MutationRequestLifecycleDecisionType.Executed,
                    1 => MutationRequestLifecycleDecisionType.Rejected,
                    2 => MutationRequestLifecycleDecisionType.Canceled,
                    _ => MutationRequestLifecycleDecisionType.Expired
                },
                createdAt: BaseTime.AddMinutes(index),
                decisionOffsetMinutes: index);

            store.Create(request).GetAwaiter().GetResult();
        }

        for (var index = 0; index < 32; index++)
        {
            var request = CreateDecisionRequest(
                requestId: $"decision-noise-{index:000}",
                stateId: "governance-benchmark:query-decision",
                decisionType: MutationRequestLifecycleDecisionType.Submitted,
                createdAt: BaseTime.AddHours(1).AddMinutes(index),
                decisionOffsetMinutes: index,
                includeExecutionOutcome: false);

            store.Create(request).GetAwaiter().GetResult();
        }

        return store;
    }

    private static MutationRequest CreateDecisionRequest(
        string requestId,
        string stateId,
        MutationRequestLifecycleDecisionType decisionType,
        DateTimeOffset createdAt,
        int decisionOffsetMinutes,
        bool includeExecutionOutcome = true)
    {
        var decisions = new List<MutationRequestDecision>
        {
            MutationRequestDecision.Create(
                MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                MutationContext.User("requester", "Requester", "Submitted"))
            with
            {
                Timestamp = createdAt
            }
        };

        if (includeExecutionOutcome)
        {
            decisions.Add(
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
                    MutationContext.User("requester", "Requester", "Pending"))
                with
                {
                    Timestamp = createdAt.AddMinutes(1)
                });

            decisions.Add(
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(decisionType),
                    MutationContext.System("Execution outcome"))
                with
                {
                    Timestamp = createdAt.AddMinutes(2 + decisionOffsetMinutes)
                });
        }

        return new MutationRequest
        {
            RequestId = requestId,
            Scope = new MutationRequestScopeDetails
            {
                StateId = stateId,
                StateType = "GovernanceState",
                MutationType = "GovernanceBenchmarkDecisionMutation"
            },
            Payload = new MutationRequestPayloadDetails
            {
                Intent = CreateIntent("RecentExecutionOutcomes", "Recent governance execution outcomes"),
                Context = MutationContext.User("requester", "Requester", "Read recent outcomes")
            },
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = includeExecutionOutcome ? MutationRequestStatus.Executed : MutationRequestStatus.Pending,
                PendingReason = includeExecutionOutcome ? null : PendingMutationReason.ExternalCheck,
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddMinutes(includeExecutionOutcome ? 3 + decisionOffsetMinutes : 1)
            },
            Decisions = decisions
        };
    }

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
