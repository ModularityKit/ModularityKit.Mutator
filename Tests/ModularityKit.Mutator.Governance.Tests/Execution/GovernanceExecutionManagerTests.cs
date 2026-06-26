using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Runtime;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Execution;

public sealed class GovernanceExecutionManagerTests
{
    [Fact]
    public async Task ExecuteApproved_executes_request_persists_resulting_version_and_correlates_audit_history()
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Strict);
        await using var provider = services.BuildServiceProvider();

        var engine = provider.GetRequiredService<IMutationEngine>();
        var auditor = provider.GetRequiredService<IMutationAuditor>();
        var historyStore = provider.GetRequiredService<IMutationHistoryStore>();
        var requestStore = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(requestStore, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(requestStore, resolutionManager, engine);

        var request = await requestStore.Create(MutationRequestFactory.Approved<RoleState, PromoteRoleMutation>(
            stateId: "tenant-42:roles",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access"
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            expectedStateVersion: "v10"));
        var mutation = new PromoteRoleMutation(
            MutationContext.User("operator-1", "Operator One", "Execute approved role promotion"),
            nextVersion: "v11");
        var state = RoleState.Create("tenant-42:roles", role: "Reader", version: "v10");

        var result = await executionManager.ExecuteApproved(
            request.RequestId,
            mutation,
            state,
            governanceContext: MutationContext.Service("governance-runtime", "Execute approved request"),
            strategy: VersionedRequestResolutionStrategy.RejectStale);

        Assert.True(result.WasExecuted);
        Assert.NotNull(result.MutationResult);
        Assert.Equal("v11", result.ResultingStateVersion);
        Assert.Equal(MutationRequestStatus.Executed, result.Request.Status);
        Assert.Equal("v11", result.Request.ResultingStateVersion);
        Assert.Equal("v11", result.Request.ExpectedStateVersion);
        Assert.NotNull(result.Request.ExecutedAt);
        Assert.Equal(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
            result.Request.Decisions[^1].Type);

        var auditEntries = await auditor.GetAuditLogAsync(request.StateId);
        var history = await historyStore.GetHistoryAsync(request.StateId);

        Assert.Single(auditEntries);
        Assert.Single(history.Entries);
        Assert.Equal(request.RequestId, auditEntries[0].Context.Metadata["GovernanceRequestId"]);
        Assert.Equal(request.RequestId, history.Entries[0].Context.Metadata["GovernanceRequestId"]);
    }

    [Fact]
    public async Task ExecuteApproved_does_not_execute_when_stale_resolution_rejects_request()
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Strict);
        await using var provider = services.BuildServiceProvider();

        var engine = provider.GetRequiredService<IMutationEngine>();
        var requestStore = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(requestStore, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(requestStore, resolutionManager, engine);

        var request = await requestStore.Create(MutationRequestFactory.Approved<RoleState, PromoteRoleMutation>(
            stateId: "tenant-42:roles",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access"
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            expectedStateVersion: "v10"));
        var mutation = new PromoteRoleMutation(
            MutationContext.User("operator-1", "Operator One", "Execute approved role promotion"),
            nextVersion: "v11");
        var state = RoleState.Create("tenant-42:roles", role: "Reader", version: "v15");

        var result = await executionManager.ExecuteApproved(
            request.RequestId,
            mutation,
            state,
            governanceContext: MutationContext.Service("governance-runtime", "Reject stale request"),
            strategy: VersionedRequestResolutionStrategy.RejectStale);

        Assert.False(result.WasExecuted);
        Assert.Null(result.MutationResult);
        Assert.Equal(MutationRequestStatus.Rejected, result.Request.Status);
        Assert.Equal(MutationRequestVersionResolutionOutcome.RejectedAsStale, result.Resolution.Outcome);
        Assert.Equal(
            MutationRequestDecisionType.VersionResolution(MutationRequestVersionResolutionDecisionType.RejectedAsStale),
            result.Request.Decisions[^1].Type);
    }

    [Fact]
    public async Task ExecuteApproved_requires_renewed_approval_before_execution_when_strategy_demands_it()
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Strict);
        await using var provider = services.BuildServiceProvider();

        var engine = provider.GetRequiredService<IMutationEngine>();
        var requestStore = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(requestStore, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(requestStore, resolutionManager, engine);

        var request = await requestStore.Create(MutationRequestFactory.Approved<RoleState, PromoteRoleMutation>(
            stateId: "tenant-42:roles",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access"
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            expectedStateVersion: "v10"));
        var mutation = new PromoteRoleMutation(
            MutationContext.User("operator-1", "Operator One", "Execute approved role promotion"),
            nextVersion: "v11");
        var state = RoleState.Create("tenant-42:roles", role: "Reader", version: "v15");

        var result = await executionManager.ExecuteApproved(
            request.RequestId,
            mutation,
            state,
            governanceContext: MutationContext.Service("governance-runtime", "Require renewed approval"),
            strategy: VersionedRequestResolutionStrategy.RequireRenewedApproval);

        Assert.False(result.WasExecuted);
        Assert.Null(result.MutationResult);
        Assert.Equal(MutationRequestStatus.Pending, result.Request.Status);
        Assert.Equal(PendingMutationReason.Approval, result.Request.PendingReason);
        Assert.Equal("v15", result.Request.ExpectedStateVersion);
        Assert.Equal(MutationRequestVersionResolutionOutcome.RequiresRenewedApproval, result.Resolution.Outcome);
    }

    [Fact]
    public async Task ExecuteApproved_revalidates_and_executes_against_latest_state_when_strategy_demands_it()
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Strict);
        await using var provider = services.BuildServiceProvider();

        var engine = provider.GetRequiredService<IMutationEngine>();
        var requestStore = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(requestStore, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(requestStore, resolutionManager, engine);

        var request = await requestStore.Create(MutationRequestFactory.Approved<RoleState, PromoteRoleMutation>(
            stateId: "tenant-42:roles",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access"
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            expectedStateVersion: "v10"));
        var mutation = new PromoteRoleMutation(
            MutationContext.User("operator-1", "Operator One", "Execute approved role promotion"),
            nextVersion: "v16");
        var state = RoleState.Create("tenant-42:roles", role: "Reader", version: "v15");

        var result = await executionManager.ExecuteApproved(
            request.RequestId,
            mutation,
            state,
            governanceContext: MutationContext.Service("governance-runtime", "Revalidate and execute"),
            strategy: VersionedRequestResolutionStrategy.RevalidateOnLatestState);

        Assert.True(result.WasExecuted);
        Assert.NotNull(result.MutationResult);
        Assert.Equal(MutationRequestVersionResolutionOutcome.RevalidateOnLatestState, result.Resolution.Outcome);
        Assert.Equal(MutationRequestStatus.Executed, result.Request.Status);
        Assert.Equal("v16", result.ResultingStateVersion);
        Assert.Equal("v16", result.Request.ResultingStateVersion);
        Assert.Equal("v16", result.Request.ExpectedStateVersion);
        Assert.Equal(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
            result.Request.Decisions[^1].Type);
        Assert.Contains(
            result.Request.Decisions,
            decision => decision.Type == MutationRequestDecisionType.VersionResolution(MutationRequestVersionResolutionDecisionType.RevalidationRequired));
    }

    private sealed record RoleState(string StateId, string Role, string Version) : IVersionedState
    {
        public static RoleState Create(string stateId, string role, string version) => new(stateId, role, version);
    }

    private sealed class PromoteRoleMutation(MutationContext context, string nextVersion) : IMutation<RoleState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "PromoteRole",
            Category = "Security",
            Description = "Promote tenant role after governance approval"
        };

        public MutationContext Context { get; } = context;

        public MutationResult<RoleState> Apply(RoleState state)
        {
            var newState = state with
            {
                Role = "Admin",
                Version = nextVersion
            };

            return MutationResult<RoleState>.Success(
                newState,
                ChangeSet.Single(StateChange.Modified("Role", state.Role, newState.Role)));
        }

        public ValidationResult Validate(RoleState state)
        {
            return state.Role == "Admin"
                ? ValidationResult.WithError("Role", "Role is already Admin.")
                : ValidationResult.Success();
        }

        public MutationResult<RoleState> Simulate(RoleState state) => Apply(state);
    }
}
