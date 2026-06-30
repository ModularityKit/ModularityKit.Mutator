using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Host;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Model;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Mutations;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Execution;

public sealed partial class GovernanceExecutionManagerTests
{
    [Fact]
    public async Task ExecuteApproved_executes_request_persists_resulting_version_and_correlates_audit_history()
    {
        var (provider, _, auditor, historyStore, requestStore, _, executionManager) =
            await GovernanceExecutionManagerTestSupport.CreateAsync();
        await using var _ = provider;

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
        var (provider, _, _, _, requestStore, _, executionManager) =
            await GovernanceExecutionManagerTestSupport.CreateAsync();
        await using var _ = provider;

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
        var (provider, _, _, _, requestStore, _, executionManager) =
            await GovernanceExecutionManagerTestSupport.CreateAsync();
        await using var _ = provider;

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
        var (provider, _, _, _, requestStore, _, executionManager) =
            await GovernanceExecutionManagerTestSupport.CreateAsync();
        await using var _ = provider;

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
}
