using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace Queries.Scenarios;

internal static class GovernanceQueriesSampleData
{
    public static async Task<InMemoryMutationRequestStore> CreateStoreAsync()
    {
        var store = new InMemoryMutationRequestStore();

        await store.Create(CreatePendingApprovalRequest(
            requestId: "req-security-1",
            stateId: "tenant-42:roles",
            category: "Security",
            approverId: "security-lead",
            createdAt: new DateTimeOffset(2026, 6, 25, 8, 0, 0, TimeSpan.Zero)));

        await store.Create(CreatePendingApprovalRequest(
            requestId: "req-billing-1",
            stateId: "tenant-42:quota",
            category: "Billing",
            approverId: "billing-owner",
            createdAt: new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero)));

        await store.Create(CreateResolvedRequest(
            requestId: "req-resolution-1",
            stateId: "tenant-42:flags",
            category: "Configuration",
            decisionTimestamp: new DateTimeOffset(2026, 6, 25, 10, 30, 0, TimeSpan.Zero)));

        await store.Create(CreateExternalCheckRequest(
            requestId: "req-check-1",
            stateId: "tenant-99:release",
            category: "Configuration",
            createdAt: new DateTimeOffset(2026, 6, 25, 11, 0, 0, TimeSpan.Zero)));

        await store.Create(CreateRecentlyApprovedRequest(
            requestId: "req-approved-1",
            stateId: "tenant-42:roles",
            category: "Security",
            approverId: "security-lead",
            approvedAt: new DateTimeOffset(2026, 6, 25, 11, 30, 0, TimeSpan.Zero)));

        await store.Create(CreateExecutedRequest(
            requestId: "req-executed-1",
            stateId: "tenant-42:quota",
            category: "Billing",
            executedAt: new DateTimeOffset(2026, 6, 25, 12, 0, 0, TimeSpan.Zero)));

        return store;
    }

    public static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    public static void PrintRequests(IReadOnlyList<MutationRequest> requests)
    {
        foreach (var request in requests)
        {
            Console.WriteLine(
                $"- {request.RequestId} | {request.StateId} | {request.Intent.Category} | {request.Status} | pending: {request.PendingReason?.ToString() ?? "-"}");
        }

        if (requests.Count == 0)
            Console.WriteLine("- none");
    }

    public static void PrintApprovals(IReadOnlyList<ModularityKit.Mutator.Governance.Abstractions.Queries.Model.MutationApprovalView> approvals)
    {
        foreach (var approval in approvals)
        {
            Console.WriteLine(
                $"- {approval.Request.RequestId} | {approval.Request.Intent.Category} | approver: {approval.Approval.ApproverId} | status: {approval.Approval.Status}");
        }

        if (approvals.Count == 0)
            Console.WriteLine("- none");
    }

    public static void PrintDecisions(IReadOnlyList<ModularityKit.Mutator.Governance.Abstractions.Queries.Model.MutationRequestDecisionView> decisions)
    {
        foreach (var decision in decisions)
        {
            Console.WriteLine(
                $"- {decision.Request.RequestId} | {decision.Decision.Type.Category}:{decision.Decision.Type.Code} | {decision.Decision.Timestamp:O}");
        }

        if (decisions.Count == 0)
            Console.WriteLine("- none");
    }

    private static MutationRequest CreatePendingApprovalRequest(
        string requestId,
        string stateId,
        string category,
        string approverId,
        DateTimeOffset createdAt)
        => MutationRequestFactory.PendingApproval(
            stateId: stateId,
            stateType: "ExampleState",
            mutationType: "ExampleMutation",
            intent: new MutationIntent
            {
                OperationName = "ExampleOperation",
                Category = category,
                Description = $"Governed request for {category.ToLowerInvariant()} flow"
            },
            context: MutationContext.User("requester", "Requester", "Need governed change"),
            requirements:
            [
                PolicyRequirement.Approval(approverId, $"Approval required from {approverId}")
            ])
        with
        {
            RequestId = requestId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            ApprovalRequirements =
            [
                new MutationApprovalRequirement
                {
                    ApproverId = approverId,
                    Status = MutationApprovalRequirementStatus.Pending,
                    StepOrder = 1
                }
            ]
        };

    private static MutationRequest CreateExternalCheckRequest(
        string requestId,
        string stateId,
        string category,
        DateTimeOffset createdAt)
        => MutationRequestFactory.Pending(
            stateId: stateId,
            stateType: "ExampleState",
            mutationType: "ExampleMutation",
            intent: new MutationIntent
            {
                OperationName = "ExampleOperation",
                Category = category,
                Description = "Waiting for dependency validation"
            },
            context: MutationContext.Service("release-orchestrator", "Waiting for external dependency"),
            pendingReason: PendingMutationReason.ExternalCheck)
        with
        {
            RequestId = requestId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static MutationRequest CreateRecentlyApprovedRequest(
        string requestId,
        string stateId,
        string category,
        string approverId,
        DateTimeOffset approvedAt)
        => MutationRequestFactory.PendingApproval(
            stateId: stateId,
            stateType: "ExampleState",
            mutationType: "ExampleMutation",
            intent: new MutationIntent
            {
                OperationName = "ExampleOperation",
                Category = category,
                Description = "Recently approved governed request"
            },
            context: MutationContext.User("requester", "Requester", "Need privileged change"),
            requirements:
            [
                PolicyRequirement.Approval(approverId, $"Approval required from {approverId}")
            ])
        with
        {
            RequestId = requestId,
            Status = MutationRequestStatus.Approved,
            PendingReason = null,
            CreatedAt = approvedAt.AddMinutes(-20),
            UpdatedAt = approvedAt,
            ApprovalRequirements =
            [
                new MutationApprovalRequirement
                {
                    ApproverId = approverId,
                    Status = MutationApprovalRequirementStatus.Approved,
                    StepOrder = 1,
                    DecidedAt = approvedAt
                }
            ],
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    MutationContext.User("requester", "Requester", "Submitted"))
                with
                {
                    Timestamp = approvedAt.AddMinutes(-20)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
                    MutationContext.User("requester", "Requester", "Pending approval"))
                with
                {
                    Timestamp = approvedAt.AddMinutes(-19)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Requested),
                    MutationContext.User("requester", "Requester", "Approval requested"))
                with
                {
                    Timestamp = approvedAt.AddMinutes(-18)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Granted),
                    MutationContext.User(approverId, approverId, "Approved"))
                with
                {
                    Timestamp = approvedAt
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
                    MutationContext.User(approverId, approverId, "Approved"))
                with
                {
                    Timestamp = approvedAt.AddMinutes(1)
                }
            ]
        };

    private static MutationRequest CreateResolvedRequest(
        string requestId,
        string stateId,
        string category,
        DateTimeOffset decisionTimestamp)
        => MutationRequestFactory.Approved(
            stateId: stateId,
            stateType: "ExampleState",
            mutationType: "ExampleMutation",
            intent: new MutationIntent
            {
                OperationName = "ExampleOperation",
                Category = category,
                Description = "Resolved governed request"
            },
            context: MutationContext.Service("governance-runtime", "Resolve stale request"))
        with
        {
            RequestId = requestId,
            Status = MutationRequestStatus.Approved,
            CreatedAt = decisionTimestamp.AddMinutes(-30),
            UpdatedAt = decisionTimestamp,
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    MutationContext.Service("governance-runtime", "Submitted"))
                with
                {
                    Timestamp = decisionTimestamp.AddMinutes(-30)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.VersionResolution(
                        MutationRequestVersionResolutionDecisionType.Validated),
                    MutationContext.Service("governance-runtime", "Validated current version"))
                with
                {
                    Timestamp = decisionTimestamp
                }
            ]
        };

    private static MutationRequest CreateExecutedRequest(
        string requestId,
        string stateId,
        string category,
        DateTimeOffset executedAt)
        => MutationRequestFactory.Approved(
            stateId: stateId,
            stateType: "ExampleState",
            mutationType: "ExampleMutation",
            intent: new MutationIntent
            {
                OperationName = "ExampleOperation",
                Category = category,
                Description = "Executed governed request"
            },
            context: MutationContext.Service("governance-runtime", "Execute approved request"))
        with
        {
            RequestId = requestId,
            Status = MutationRequestStatus.Executed,
            CreatedAt = executedAt.AddMinutes(-15),
            UpdatedAt = executedAt,
            ExecutedAt = executedAt,
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    MutationContext.Service("governance-runtime", "Submitted"))
                with
                {
                    Timestamp = executedAt.AddMinutes(-15)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
                    MutationContext.Service("governance-runtime", "Approved"))
                with
                {
                    Timestamp = executedAt.AddMinutes(-5)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
                    MutationContext.Service("governance-runtime", "Executed"))
                with
                {
                    Timestamp = executedAt
                }
            ]
        };
}
