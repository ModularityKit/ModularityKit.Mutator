using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Links;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Host;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Model;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Mutations;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Execution;

public sealed partial class GovernanceExecutionManagerTests
{
    [Fact]
    public async Task ExecuteApproved_executes_operator_rollback_compensation_and_links_execution_history()
    {
        var (provider, _, auditor, historyStore, requestStore, _, executionManager) =
            await GovernanceExecutionManagerTestSupport.CreateAsync();
        await using var _ = provider;

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
            originalResult.MutationResult!.Value.NewState!,
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
            compensatedOriginalRequest.Execution.RelatedExecutions,
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
}
