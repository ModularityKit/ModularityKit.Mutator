using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Queries.Builders;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Queries;

public sealed partial class MutationRequestQueryStoreTests
{
    [Fact]
    public async Task QueryAsync_filters_requests_by_governance_dimensions()
    {
        var store = new InMemoryMutationRequestStore();
        var approvalRequest = await store.Create(MutationRequestQueryStoreRequestBuilders.CreateGovernedRequest(
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

        await store.Create(MutationRequestQueryStoreRequestBuilders.CreateGovernedRequest(
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
        var pending = await store.Create(MutationRequestQueryStoreRequestBuilders.CreateSimpleRequest(
            "req-pending",
            MutationRequestStatus.Pending,
            PendingMutationReason.ExternalCheck,
            new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero)));

        await store.Create(MutationRequestQueryStoreRequestBuilders.CreateSimpleRequest(
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

        await store.Create(MutationRequestQueryStoreRequestBuilders.CreateGovernedRequest(
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

        await store.Create(MutationRequestQueryStoreRequestBuilders.CreateGovernedRequest(
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
}
