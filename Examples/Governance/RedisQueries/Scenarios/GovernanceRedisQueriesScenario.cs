using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Redis;
using StackExchange.Redis;

namespace RedisQueries.Scenarios;

internal static class GovernanceRedisQueriesScenario
{
    public static async Task Run()
    {
        var redisConnectionString = BuildRedisConnectionString();
        var keyPrefix = $"modularitykit:examples:governance:redis:{Guid.NewGuid():N}";

        try
        {
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            var services = new ServiceCollection();

            services.AddRedisGovernanceStore(
                multiplexer,
                options => options.KeyPrefix = keyPrefix);

            await using var provider = services.BuildServiceProvider();
            var requestStore = provider.GetRequiredService<IMutationRequestStore>();
            var queryStore = provider.GetRequiredService<IMutationRequestQueryStore>();

            await Seed(requestStore);

            Console.WriteLine($"Redis: {redisConnectionString}");
            Console.WriteLine($"KeyPrefix: {keyPrefix}");

            PrintSection("Pending Approval Queue");
            PrintRequests(await queryStore.GetPendingApprovalQueueAsync());

            PrintSection("Pending Approvals For security-lead");
            PrintApprovals(await queryStore.GetPendingApprovalsAsync(new MutationApprovalQuery
            {
                ApproverIds = new HashSet<string> { "security-lead" }
            }));

            PrintSection("Recent Execution Outcomes");
            PrintDecisions(await queryStore.GetRecentDecisionsAsync(
                MutationRequestDecisionQuery.RecentExecutionOutcomes(),
                take: 5));
        }
        catch (RedisConnectionException exception)
        {
            Console.Error.WriteLine($"Could not connect to Redis at '{redisConnectionString}'.");
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine("Start Redis locally or set MODULARITYKIT_REDIS to a reachable endpoint.");
        }
    }

    private static string BuildRedisConnectionString()
    {
        var explicitConnectionString = Environment.GetEnvironmentVariable("MODULARITYKIT_REDIS");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
            return explicitConnectionString;

        var host = Environment.GetEnvironmentVariable("MODULARITYKIT_REDIS_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("MODULARITYKIT_REDIS_PORT") ?? "6379";
        var password = Environment.GetEnvironmentVariable("MODULARITYKIT_REDIS_PASSWORD");

        return string.IsNullOrWhiteSpace(password)
            ? $"{host}:{port}"
            : $"{host}:{port},password={password}";
    }

    private static async Task Seed(IMutationRequestStore requestStore)
    {
        await requestStore.Create(CreatePendingApprovalRequest(
            requestId: "redis-req-security-1",
            stateId: "tenant-42:roles",
            category: "Security",
            approverId: "security-lead",
            createdAt: new DateTimeOffset(2026, 6, 25, 8, 0, 0, TimeSpan.Zero)));

        await requestStore.Create(CreatePendingApprovalRequest(
            requestId: "redis-req-billing-1",
            stateId: "tenant-42:quota",
            category: "Billing",
            approverId: "billing-owner",
            createdAt: new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero)));

        await requestStore.Create(CreateExecutedRequest(
            requestId: "redis-req-executed-1",
            stateId: "tenant-42:quota",
            category: "Billing",
            executedAt: new DateTimeOffset(2026, 6, 25, 12, 0, 0, TimeSpan.Zero)));
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
                Description = $"Redis-backed governed request for {category.ToLowerInvariant()} flow"
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
            UpdatedAt = createdAt
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
                $"- {request.RequestId} | {request.StateId} | {request.Intent.Category} | {request.Status} | pending: {request.PendingReason?.ToString() ?? "-"}");
        }

        if (requests.Count == 0)
            Console.WriteLine("- none");
    }

    private static void PrintApprovals(IReadOnlyList<MutationApprovalView> approvals)
    {
        foreach (var approval in approvals)
        {
            Console.WriteLine(
                $"- {approval.Request.RequestId} | {approval.Request.Intent.Category} | approver: {approval.Approval.ApproverId} | status: {approval.Approval.Status}");
        }

        if (approvals.Count == 0)
            Console.WriteLine("- none");
    }

    private static void PrintDecisions(IReadOnlyList<MutationRequestDecisionView> decisions)
    {
        foreach (var decision in decisions)
        {
            Console.WriteLine(
                $"- {decision.Request.RequestId} | {decision.Decision.Type.Category}:{decision.Decision.Type.Code} | {decision.Decision.Timestamp:O}");
        }

        if (decisions.Count == 0)
            Console.WriteLine("- none");
    }
}
