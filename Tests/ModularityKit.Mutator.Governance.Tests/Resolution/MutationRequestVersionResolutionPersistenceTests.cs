using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Governance.Tests.TestSupport;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Resolution;

public sealed class MutationRequestVersionResolutionPersistenceTests
{
    [Fact]
    public async Task Resolve_does_not_persist_decision_history_unless_caller_saves_the_result()
    {
        var store = new InMemoryMutationRequestStore();
        var resolver = new MutationRequestVersionResolver();
        var request = MutationRequestTestFactory.CreateApprovedSecurityRequest("v10");

        await store.Create(request);

        var resolution = resolver.Resolve(
            request,
            currentStateVersion: "v15",
            resolutionContext: MutationContext.User("approver", "Approver", "Resolve request"),
            strategy: VersionedRequestResolutionStrategy.RejectStale);

        var loaded = await store.Get(request.RequestId);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Decisions.Count);
        Assert.Equal(3, resolution.Request.Decisions.Count);
        Assert.Equal(MutationRequestStatus.Approved, loaded.Status);
        Assert.Equal(MutationRequestStatus.Rejected, resolution.Request.Status);
    }

    [Fact]
    public async Task ResolveAndStore_persists_decision_history_and_state()
    {
        var store = new InMemoryMutationRequestStore();
        var resolver = new MutationRequestVersionResolver();
        var manager = new MutationRequestVersionResolutionManager(store, resolver);
        var request = await store.Create(MutationRequestTestFactory.CreateApprovedSecurityRequest("v10"));

        var resolution = await manager.ResolveAndStore(
            request.RequestId,
            currentStateVersion: "v15",
            resolutionContext: MutationContext.User("approver", "Approver", "Resolve request"),
            strategy: VersionedRequestResolutionStrategy.RejectStale);

        var loaded = await store.Get(request.RequestId);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.Decisions.Count);
        Assert.Equal(
            MutationRequestDecisionType.VersionResolution(MutationRequestVersionResolutionDecisionType.RejectedAsStale),
            loaded.Decisions[^1].Type);
        Assert.Equal(MutationRequestStatus.Rejected, loaded.Status);
        Assert.Equal(1, loaded.Revision);
        Assert.Equal(loaded, resolution.Request);
    }

    [Fact]
    public async Task ResolveAndStore_revalidation_marks_request_pending_for_revalidation()
    {
        var store = new InMemoryMutationRequestStore();
        var resolver = new MutationRequestVersionResolver();
        var manager = new MutationRequestVersionResolutionManager(store, resolver);
        var request = await store.Create(MutationRequestTestFactory.CreateApprovedSecurityRequest("v10"));

        var resolution = await manager.ResolveAndStore(
            request.RequestId,
            currentStateVersion: "v15",
            resolutionContext: MutationContext.User("approver", "Approver", "Revalidate request"),
            strategy: VersionedRequestResolutionStrategy.RevalidateOnLatestState);

        var loaded = await store.Get(request.RequestId);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.Decisions.Count);
        Assert.Equal(
            MutationRequestDecisionType.VersionResolution(MutationRequestVersionResolutionDecisionType.RevalidationRequired),
            loaded.Decisions[^1].Type);
        Assert.Equal(MutationRequestStatus.Pending, loaded.Status);
        Assert.Equal(PendingMutationReason.Revalidation, loaded.PendingReason);
        Assert.Equal("v15", loaded.Versioning.ExpectedStateVersion);
        Assert.Equal(1, loaded.Revision);
        Assert.Equal(MutationRequestVersionResolutionOutcome.RevalidateOnLatestState, resolution.Outcome);
        Assert.Equal(loaded, resolution.Request);
    }

    [Fact]
    public async Task ResolveAndStore_throws_not_found_for_missing_request()
    {
        var store = new InMemoryMutationRequestStore();
        var resolver = new MutationRequestVersionResolver();
        var manager = new MutationRequestVersionResolutionManager(store, resolver);

        var exception = await Assert.ThrowsAsync<MutationRequestNotFoundException>(() =>
            manager.ResolveAndStore(
                "missing-request",
                currentStateVersion: "v15",
                resolutionContext: MutationContext.User("approver", "Approver", "Resolve request"),
                strategy: VersionedRequestResolutionStrategy.RejectStale));

        Assert.Equal("missing-request", exception.RequestId);
    }
}
