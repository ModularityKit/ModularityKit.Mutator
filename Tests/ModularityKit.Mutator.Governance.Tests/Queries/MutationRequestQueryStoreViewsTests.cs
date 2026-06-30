using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Queries.Builders;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Queries.Model;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Queries;

public sealed partial class MutationRequestQueryStoreTests
{
    [Fact]
    public async Task QueryAsync_can_filter_by_persisted_side_effect_dimensions()
    {
        var store = new InMemoryMutationRequestStore();

        await store.Create(MutationRequestQueryStoreRequestBuilders.CreateGovernedRequest(
            requestId: "req-side-effect",
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            actorId: "alice",
            actorName: "Alice",
            category: "Security",
            tags: new HashSet<string> { "security" },
            intentMetadata: new Dictionary<string, object> { ["risk-owner"] = "platform" },
            requestMetadata: new Dictionary<string, object> { ["ticket"] = "INC-42" },
            blastRadius: BlastRadius.Module,
            createdAt: new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            updatedAt: new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero),
            status: MutationRequestStatus.Executed,
            pendingReason: null,
            decisions: [],
            sideEffects:
            [
                SideEffect.Critical(
                    type: "WorkflowRejected",
                    description: "Manual review required",
                    data: new GovernanceSideEffectData
                    {
                        Reference = "INC-42"
                    })
            ]));

        await store.Create(MutationRequestQueryStoreRequestBuilders.CreateGovernedRequest(
            requestId: "req-other-effect",
            stateId: "tenant-42:quota",
            stateType: "QuotaState",
            mutationType: "IncreaseQuotaMutation",
            actorId: "bob",
            actorName: "Bob",
            category: "Billing",
            tags: new HashSet<string> { "billing" },
            intentMetadata: new Dictionary<string, object> { ["risk-owner"] = "finance" },
            requestMetadata: new Dictionary<string, object> { ["ticket"] = "FIN-9" },
            blastRadius: BlastRadius.Single,
            createdAt: new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
            updatedAt: new DateTimeOffset(2026, 6, 1, 12, 30, 0, TimeSpan.Zero),
            status: MutationRequestStatus.Executed,
            pendingReason: null,
            decisions: [],
            sideEffects:
            [
                SideEffect.Create(
                    type: "QuotaRaised",
                    description: "Quota updated")
            ]));

        var results = await store.QueryAsync(new MutationRequestQuery
        {
            SideEffects = new MutationRequestSideEffectFilter
            {
                Types = new HashSet<string> { "WorkflowRejected" },
                DataContractTypes = new HashSet<string> { "governance.side-effect" },
                Severities = new HashSet<SideEffectSeverity> { SideEffectSeverity.Critical },
                RequiresAction = true
            }
        });

        Assert.Single(results);
        Assert.Equal("req-side-effect", results[0].RequestId);
    }

    [Fact]
    public async Task GetPendingApprovalQueueAsync_and_GetRecentApprovalsAsync_return_approval_oriented_views()
    {
        var store = new InMemoryMutationRequestStore();

        var pendingApproval = await store.Create(MutationRequestQueryStoreRequestBuilders.CreateSimpleRequest(
            "req-pending-approval",
            MutationRequestStatus.Pending,
            PendingMutationReason.Approval,
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero)));

        var recentApproval = await store.Create(MutationRequestQueryStoreRequestBuilders.CreateApprovedRequest(
            "req-recent-approval",
            new DateTimeOffset(2026, 6, 2, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 9, 15, 0, TimeSpan.Zero)));

        await store.Create(MutationRequestQueryStoreRequestBuilders.CreateApprovedRequest(
            "req-older-approval",
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero)));

        var pendingQueue = await store.GetPendingApprovalQueueAsync();
        var recentApprovals = await store.GetRecentApprovalsAsync(take: 1);

        Assert.Single(pendingQueue);
        Assert.Equal(pendingApproval.RequestId, pendingQueue[0].RequestId);

        Assert.Single(recentApprovals);
        Assert.Equal(recentApproval.RequestId, recentApprovals[0].RequestId);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_filters_approval_views_by_approver_dimensions()
    {
        var store = new InMemoryMutationRequestStore();

        await store.Create(MutationRequestQueryStoreViewBuilders.CreateApprovalViewRequest(
            requestId: "req-security",
            approverId: "security-lead",
            approverRole: "SecurityLead",
            approverGroup: "security",
            category: "Security",
            approvalStatus: MutationApprovalRequirementStatus.Pending));

        await store.Create(MutationRequestQueryStoreViewBuilders.CreateApprovalViewRequest(
            requestId: "req-platform",
            approverId: "platform-owner",
            approverRole: "PlatformOwner",
            approverGroup: "platform",
            category: "Platform",
            approvalStatus: MutationApprovalRequirementStatus.Pending));

        var approvals = await store.GetPendingApprovalsAsync(new MutationApprovalQuery
        {
            ApproverIds = new HashSet<string> { "security-lead" },
            ApproverRoles = new HashSet<string> { "SecurityLead" },
            ApproverGroups = new HashSet<string> { "security" },
            Categories = new HashSet<string> { "Security" }
        });

        Assert.Single(approvals);
        Assert.Equal("req-security", approvals[0].Request.RequestId);
        Assert.Equal("security-lead", approvals[0].Approval.ApproverId);
    }

    [Fact]
    public async Task GetRecentDecisionsAsync_returns_filtered_decision_views()
    {
        var store = new InMemoryMutationRequestStore();

        await store.Create(MutationRequestQueryStoreViewBuilders.CreateDecisionViewRequest(
            requestId: "req-resolution",
            decisions:
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.VersionResolution(
                        MutationRequestVersionResolutionDecisionType.RejectedAsStale),
                    MutationContext.User("resolver", "Resolver", "Rejected as stale"))
                with
                {
                    Timestamp = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero)
                }
            ]));

        await store.Create(MutationRequestQueryStoreViewBuilders.CreateDecisionViewRequest(
            requestId: "req-executed",
            decisions:
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
                    MutationContext.System("Executed"))
                with
                {
                    Timestamp = new DateTimeOffset(2026, 6, 3, 13, 0, 0, TimeSpan.Zero)
                }
            ]));

        var decisions = await store.GetRecentDecisionsAsync(
            MutationRequestDecisionQuery.RecentVersionResolutions() with
            {
                ActorIds = new HashSet<string> { "resolver" }
            },
            take: 5);

        Assert.Single(decisions);
        Assert.Equal("req-resolution", decisions[0].Request.RequestId);
        Assert.Equal(MutationRequestDecisionCategory.VersionResolution, decisions[0].Decision.Type.Category);
    }
}
