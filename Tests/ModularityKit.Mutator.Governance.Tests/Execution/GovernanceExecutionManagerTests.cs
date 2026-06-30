using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Links;
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
                Description = "Grant elevated access",
                Tags = new HashSet<string> { "security", "incident" },
                EstimatedBlastRadius = BlastRadius.Module,
                Metadata = new Dictionary<string, object>
                {
                    ["risk-owner"] = "platform"
                }
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            expectedStateVersion: "v10",
            metadata: new Dictionary<string, object>
            {
                ["ticket"] = "INC-42"
            }));
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
        Assert.Equal("v11", result.Request.Versioning.ResultingStateVersion);
        Assert.Equal("v11", result.Request.Versioning.ExpectedStateVersion);
        Assert.NotNull(result.Request.Versioning.ExecutedAt);
        Assert.Single(result.Request.SideEffects);
        Assert.Equal("RoleElevated", result.Request.SideEffects[0].Type);
        Assert.Equal("governance.execution-effect", result.Request.SideEffects[0].DataContractType);
        Assert.Equal(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
            result.Request.Decisions[^1].Type);

        var auditEntries = await auditor.GetAuditLogAsync(request.StateId);
        var history = await historyStore.GetHistoryAsync(request.StateId);

        Assert.Single(auditEntries);
        Assert.Single(history.Entries);
        Assert.Equal(request.RequestId, auditEntries[0].Context.Metadata["GovernanceRequestId"]);
        Assert.Equal(request.RequestId, history.Entries[0].Context.Metadata["GovernanceRequestId"]);
        Assert.Contains("security", auditEntries[0].MutationIntent.Tags);
        Assert.Equal(BlastRadiusScope.Module, auditEntries[0].MutationIntent.EstimatedBlastRadius?.Scope);
        Assert.Equal("platform", auditEntries[0].MutationIntent.Metadata["risk-owner"]);
        Assert.Equal("platform", history.Entries[0].Intent.Metadata["risk-owner"]);
        Assert.Equal("INC-42", ((IReadOnlyDictionary<string, object>)auditEntries[0].Context.Metadata["GovernanceRequestMetadata"])["ticket"]);
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
        Assert.Equal("v15", result.Request.Versioning.ExpectedStateVersion);
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
        Assert.Equal("v16", result.Request.Versioning.ResultingStateVersion);
        Assert.Equal("v16", result.Request.Versioning.ExpectedStateVersion);
        Assert.Equal(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
            result.Request.Decisions[^1].Type);
        Assert.Contains(
            result.Request.Decisions,
            decision => decision.Type == MutationRequestDecisionType.VersionResolution(MutationRequestVersionResolutionDecisionType.RevalidationRequired));
    }

    [Fact]
    public async Task ExecuteApproved_executes_operator_rollback_compensation_and_links_execution_history()
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

        var originalRequest = await requestStore.Create(MutationRequestFactory.Approved<RoleState, PromoteRoleMutation>(
            stateId: "tenant-42:roles",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access",
                IsReversible = true
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            expectedStateVersion: "v10"));

        var originalState = RoleState.Create("tenant-42:roles", role: "Reader", version: "v10");
        var originalMutation = new PromoteRoleMutation(
            MutationContext.User("operator-1", "Operator One", "Execute approved role promotion"),
            nextVersion: "v11");

        var originalResult = await executionManager.ExecuteApproved(
            originalRequest.RequestId,
            originalMutation,
            originalState,
            governanceContext: MutationContext.Service("governance-runtime", "Execute approved request"),
            strategy: VersionedRequestResolutionStrategy.RejectStale);

        var compensationPlan = new GovernedCompensationPlan
        {
            OriginalRequestId = originalRequest.RequestId,
            Kind = GovernedCompensationKind.Rollback,
            Trigger = GovernedCompensationTrigger.OperatorRollback,
            Reason = "Operator reverted the elevated role after incident review."
        };

        var compensationRequest = await requestStore.Create(CompensationMutationRequestFactory.Approved<RoleState, RollbackRoleMutation>(
            stateId: "tenant-42:roles",
            intent: new MutationIntent
            {
                OperationName = "RollbackRole",
                Category = "Security",
                Description = "Restore the previous tenant role"
            },
            context: MutationContext.User("operator-2", "Operator Two", "Rollback approved role mutation"),
            compensation: compensationPlan,
            expectedStateVersion: "v11"));

        var compensationMutation = new RollbackRoleMutation(
            MutationContext.User("operator-2", "Operator Two", "Rollback approved role mutation"),
            nextVersion: "v12");

        var compensationResult = await executionManager.ExecuteApproved(
            compensationRequest.RequestId,
            compensationMutation,
            originalResult.MutationResult!.NewState!,
            governanceContext: MutationContext.Service("governance-runtime", "Execute operator rollback"),
            strategy: VersionedRequestResolutionStrategy.RejectStale);

        Assert.True(compensationResult.WasExecuted);
        Assert.Equal(GovernedExecutionKind.Compensation, compensationResult.ExecutionKind);
        Assert.NotNull(compensationResult.Compensation);
        Assert.Equal(originalRequest.RequestId, compensationResult.Compensation!.OriginalRequestId);
        Assert.Equal(GovernedCompensationKind.Rollback, compensationResult.Compensation.Kind);
        Assert.Contains(
            compensationResult.Request.Execution.RelatedExecutions,
            link => link.RequestId == originalRequest.RequestId &&
                    link.Type == GovernedExecutionLinkType.Compensates);

        var compensatedOriginalRequest = await requestStore.Get(originalRequest.RequestId);
        Assert.NotNull(compensatedOriginalRequest);
        Assert.Contains(
            compensatedOriginalRequest!.Execution.RelatedExecutions,
            link => link.RequestId == compensationRequest.RequestId &&
                    link.Type == GovernedExecutionLinkType.CompensatedBy &&
                    link.ExecutionKind == GovernedExecutionKind.Compensation);
        Assert.Contains(
            compensatedOriginalRequest.Decisions,
            decision => decision.Type == MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Compensated));

        var auditEntries = await auditor.GetAuditLogAsync(originalRequest.StateId);
        var history = await historyStore.GetHistoryAsync(originalRequest.StateId);

        Assert.Equal(2, auditEntries.Count);
        Assert.Equal(2, history.Entries.Count);
        Assert.Equal("Compensation", auditEntries[1].Context.Metadata["GovernanceExecutionKind"]);
        Assert.Equal("Compensation", history.Entries[1].Context.Metadata["GovernanceExecutionKind"]);

        var auditCompensation = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            auditEntries[1].Context.Metadata["GovernanceCompensation"]);
        Assert.Equal(originalRequest.RequestId, auditCompensation["OriginalRequestId"]);
        Assert.Equal("Rollback", auditCompensation["Kind"]);
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
                ChangeSet.Single(StateChange.Modified("Role", state.Role, newState.Role)),
                [
                    SideEffect.Create(
                        type: "RoleElevated",
                        description: "Governed execution elevated the role",
                        data: new GovernanceExecutionSideEffectData
                        {
                            RequestStateId = state.StateId,
                            NewRole = newState.Role
                        })
                ]);
        }

        public ValidationResult Validate(RoleState state)
        {
            return state.Role == "Admin"
                ? ValidationResult.WithError("Role", "Role is already Admin.")
                : ValidationResult.Success();
        }

        public MutationResult<RoleState> Simulate(RoleState state) => Apply(state);
    }

    private sealed class RollbackRoleMutation(MutationContext context, string nextVersion) : IMutation<RoleState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "RollbackRole",
            Category = "Security",
            Description = "Rollback tenant role to Reader",
            IsReversible = false
        };

        public MutationContext Context { get; } = context;

        public MutationResult<RoleState> Apply(RoleState state)
        {
            var newState = state with
            {
                Role = "Reader",
                Version = nextVersion
            };

            return MutationResult<RoleState>.Success(
                newState,
                ChangeSet.Single(StateChange.Modified("Role", state.Role, newState.Role)),
                [
                    SideEffect.Create(
                        type: "RoleRollback",
                        description: "Governed compensation restored the previous role",
                        data: new GovernanceExecutionSideEffectData
                        {
                            RequestStateId = state.StateId,
                            NewRole = newState.Role
                        })
                ]);
        }

        public ValidationResult Validate(RoleState state)
        {
            return state.Role == "Reader"
                ? ValidationResult.WithError("Role", "Role is already Reader.")
                : ValidationResult.Success();
        }

        public MutationResult<RoleState> Simulate(RoleState state) => Apply(state);
    }

    [SideEffectDataContract("governance.execution-effect", 1)]
    private sealed record GovernanceExecutionSideEffectData
    {
        public required string RequestStateId { get; init; }

        public required string NewRole { get; init; }
    }
}
