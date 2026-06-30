using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Queries;

public sealed class MutationRequestQueryStoreTests
{
    [Fact]
    public async Task QueryAsync_filters_requests_by_governance_dimensions()
    {
        var store = new InMemoryMutationRequestStore();
        var approvalRequest = await store.Create(CreateGovernedRequest(
            requestId: "req-approval",
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            actorId: "alice",
            actorName: "Alice",
            category: "Security",
            tags: new HashSet<string> { "security", "urgent" },
            intentMetadata: new Dictionary<string, object> { ["team"] = "platform" },
            requestMetadata: new Dictionary<string, object> { ["team"] = "platform" },
            blastRadius: BlastRadius.Module,
            createdAt: new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            updatedAt: new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero),
            status: MutationRequestStatus.Pending,
            pendingReason: PendingMutationReason.Approval,
            decisions:
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    MutationContext.User("alice", "Alice", "Need review")),
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
                    MutationContext.User("alice", "Alice", "Pending approval")),
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Requested),
                    MutationContext.User("alice", "Alice", "Approval requested"))
            ]));

        await store.Create(CreateGovernedRequest(
            requestId: "req-other",
            stateId: "tenant-42:billing",
            stateType: "QuotaState",
            mutationType: "IncreaseQuotaMutation",
            actorId: "bob",
            actorName: "Bob",
            category: "Billing",
            tags: new HashSet<string> { "billing" },
            intentMetadata: new Dictionary<string, object> { ["team"] = "finance" },
            requestMetadata: new Dictionary<string, object> { ["team"] = "finance" },
            blastRadius: BlastRadius.System,
            createdAt: new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero),
            updatedAt: new DateTimeOffset(2026, 6, 2, 11, 0, 0, TimeSpan.Zero),
            status: MutationRequestStatus.Approved,
            pendingReason: null,
            decisions:
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    MutationContext.User("bob", "Bob", "Request submitted")),
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
                    MutationContext.User("bob", "Bob", "Approved"))
            ]));

        var results = await store.QueryAsync(new MutationRequestQuery
        {
            Lifecycle = new MutationRequestLifecycleFilter
            {
                Statuses = new HashSet<MutationRequestStatus> { MutationRequestStatus.Pending },
                PendingReasons = new HashSet<PendingMutationReason> { PendingMutationReason.Approval }
            },
            Actor = new MutationRequestActorFilter
            {
                ActorIds = new HashSet<string> { "alice" }
            },
            Intent = new MutationRequestIntentFilter
            {
                Categories = new HashSet<string> { "Security" },
                Tags = new HashSet<string> { "security", "urgent" },
                TagMatchMode = MutationRequestTagMatchMode.All,
                Metadata = new Dictionary<string, object?> { ["team"] = "platform" },
                MinimumBlastRadiusScope = BlastRadiusScope.Module
            },
            Metadata = new MutationRequestMetadataFilter
            {
                Values = new Dictionary<string, object?> { ["team"] = "platform" }
            },
            TimeRange = new MutationRequestTimeRangeFilter
            {
                CreatedFrom = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                CreatedTo = new DateTimeOffset(2026, 6, 1, 23, 59, 59, TimeSpan.Zero)
            }
        });

        Assert.Single(results);
        Assert.Equal(approvalRequest.RequestId, results[0].RequestId);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_returns_only_pending_requests()
    {
        var store = new InMemoryMutationRequestStore();
        var pending = await store.Create(CreateSimpleRequest(
            "req-pending",
            MutationRequestStatus.Pending,
            PendingMutationReason.ExternalCheck,
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero)));

        await store.Create(CreateSimpleRequest(
            "req-approved",
            MutationRequestStatus.Approved,
            null,
            new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero)));

        var results = await store.GetPendingRequestsAsync();

        Assert.Single(results);
        Assert.Equal(pending.RequestId, results[0].RequestId);
    }

    [Fact]
    public async Task QueryAsync_can_filter_by_intent_metadata_independently_from_request_metadata()
    {
        var store = new InMemoryMutationRequestStore();

        await store.Create(CreateGovernedRequest(
            requestId: "req-platform",
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
            status: MutationRequestStatus.Pending,
            pendingReason: PendingMutationReason.Approval,
            decisions: []));

        await store.Create(CreateGovernedRequest(
            requestId: "req-finance",
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
            status: MutationRequestStatus.Approved,
            pendingReason: null,
            decisions: []));

        var results = await store.QueryAsync(new MutationRequestQuery
        {
            Intent = new MutationRequestIntentFilter
            {
                Metadata = new Dictionary<string, object?> { ["risk-owner"] = "platform" }
            },
            Metadata = new MutationRequestMetadataFilter
            {
                Values = new Dictionary<string, object?> { ["ticket"] = "INC-42" }
            }
        });

        Assert.Single(results);
        Assert.Equal("req-platform", results[0].RequestId);
    }

    [Fact]
    public async Task QueryAsync_can_filter_by_persisted_side_effect_dimensions()
    {
        var store = new InMemoryMutationRequestStore();

        await store.Create(CreateGovernedRequest(
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

        await store.Create(CreateGovernedRequest(
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

        var pendingApproval = await store.Create(CreateSimpleRequest(
            "req-pending-approval",
            MutationRequestStatus.Pending,
            PendingMutationReason.Approval,
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero)));

        var recentApproval = await store.Create(CreateApprovedRequest(
            "req-recent-approval",
            new DateTimeOffset(2026, 6, 2, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 9, 15, 0, TimeSpan.Zero)));

        await store.Create(CreateApprovedRequest(
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

        await store.Create(CreateApprovalViewRequest(
            requestId: "req-security",
            approverId: "security-lead",
            approverRole: "SecurityLead",
            approverGroup: "security",
            category: "Security",
            approvalStatus: MutationApprovalRequirementStatus.Pending));

        await store.Create(CreateApprovalViewRequest(
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

        await store.Create(CreateDecisionViewRequest(
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

        await store.Create(CreateDecisionViewRequest(
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

    private static MutationRequest CreateSimpleRequest(
        string requestId,
        MutationRequestStatus status,
        PendingMutationReason? pendingReason,
        DateTimeOffset createdAt)
        => MutationRequestFactory.Pending(
            stateId: "tenant-42:quota",
            stateType: "QuotaState",
            mutationType: "IncreaseQuotaMutation",
            intent: new MutationIntent
            {
                OperationName = "IncreaseQuota",
                Category = "Billing",
                Description = "Raise quota",
                Tags = new HashSet<string> { "billing" },
                EstimatedBlastRadius = BlastRadius.Single
            },
            context: MutationContext.User("alice", "Alice", "Need more quota"),
            pendingReason: pendingReason ?? PendingMutationReason.Approval)
        with
        {
            RequestId = requestId,
            Status = status,
            PendingReason = pendingReason,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static MutationRequest CreateApprovedRequest(
        string requestId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access",
                Tags = new HashSet<string> { "security" },
                EstimatedBlastRadius = BlastRadius.Module
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            requirements:
            [
                PolicyRequirement.Approval("approver", "Review elevated access")
            ])
        with
        {
            RequestId = requestId,
            Status = MutationRequestStatus.Approved,
            PendingReason = null,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    MutationContext.User("requester", "Requester", "Submitted"))
                with
                {
                    Timestamp = createdAt
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
                    MutationContext.User("requester", "Requester", "Pending approval"))
                with
                {
                    Timestamp = createdAt.AddMinutes(5)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Requested),
                    MutationContext.User("requester", "Requester", "Approval requested"),
                    metadata: new Dictionary<string, object> { ["Queue"] = "security" })
                with
                {
                    Timestamp = createdAt.AddMinutes(10)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Granted),
                    MutationContext.User("approver", "Approver", "Approved"),
                    metadata: new Dictionary<string, object> { ["Queue"] = "security" })
                with
                {
                    Timestamp = updatedAt
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
                    MutationContext.User("approver", "Approver", "Approved"),
                    metadata: new Dictionary<string, object> { ["Queue"] = "security" })
                with
                {
                    Timestamp = updatedAt.AddMinutes(1)
                }
            ]
        };

    private static MutationRequest CreateApprovalViewRequest(
        string requestId,
        string approverId,
        string approverRole,
        string approverGroup,
        string category,
        MutationApprovalRequirementStatus approvalStatus)
        => MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = category,
                Tags = new HashSet<string> { "approval" }
            },
            context: MutationContext.User("requester", "Requester", "Need approval"),
            requirements:
            [
                PolicyRequirement.Approval(approverId, "Need review")
            ])
        with
        {
            RequestId = requestId,
            Status = MutationRequestStatus.Pending,
            PendingReason = PendingMutationReason.Approval,
            ApprovalRequirements =
            [
                new MutationApprovalRequirement
                {
                    ApproverId = approverId,
                    ApproverRole = approverRole,
                    ApproverGroup = approverGroup,
                    Status = approvalStatus,
                    StepOrder = 1
                }
            ]
        };

    private static MutationRequest CreateDecisionViewRequest(
        string requestId,
        IReadOnlyList<MutationRequestDecision> decisions)
        => MutationRequestFactory.Pending(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security"
            },
            context: MutationContext.User("requester", "Requester", "Need execution"),
            pendingReason: PendingMutationReason.ExternalCheck)
        with
        {
            RequestId = requestId,
            Decisions = decisions,
            UpdatedAt = decisions.Max(decision => decision.Timestamp)
        };

    private static MutationRequest CreateGovernedRequest(
        string requestId,
        string stateId,
        string stateType,
        string mutationType,
        string actorId,
        string actorName,
        string category,
        IReadOnlySet<string> tags,
        IReadOnlyDictionary<string, object> intentMetadata,
        IReadOnlyDictionary<string, object> requestMetadata,
        BlastRadius blastRadius,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        MutationRequestStatus status,
        PendingMutationReason? pendingReason,
        IReadOnlyList<MutationRequestDecision> decisions,
        IReadOnlyList<SideEffect>? sideEffects = null)
        => new MutationRequest
        {
            RequestId = requestId,
            StateId = stateId,
            StateType = stateType,
            MutationType = mutationType,
            Intent = new MutationIntent
            {
                OperationName = mutationType,
                Category = category,
                Tags = tags,
                Metadata = intentMetadata,
                EstimatedBlastRadius = blastRadius
            },
            Context = MutationContext.User(actorId, actorName, "Query test"),
            Status = status,
            PendingReason = pendingReason,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Decisions = decisions,
            Metadata = requestMetadata,
            SideEffects = sideEffects ?? []
        };

    [SideEffectDataContract("governance.side-effect", 1)]
    private sealed record GovernanceSideEffectData
    {
        public required string Reference { get; init; }
    }
}
