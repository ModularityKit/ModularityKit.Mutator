using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Runtime.Lifecycle.Execution;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Lifecycle.Storage;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Requests;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Lifecycle;

public sealed class MutationRequestLifecycleAtomicityTests
{
    [Fact]
    public async Task Stale_snapshot_transition_is_rejected_after_a_prior_lifecycle_update()
    {
        var request = MutationRequestTestFactory.CreatePendingRequest();
        var store = new StaleSnapshotMutationRequestStore(request);
        var manager = new MutationRequestLifecycleManager(store);

        var approved = await manager.Approve(
            request.RequestId,
            MutationContext.User("approver", "Approver", "Approve request"));

        var exception = await Assert.ThrowsAsync<MutationRequestConcurrencyException>(() =>
            manager.Cancel(
                request.RequestId,
                MutationContext.User("operator", "Operator", "Cancel request")));

        Assert.Equal(1, store.StoreCount);
        Assert.All(store.GetSnapshots, snapshot => Assert.Equal(MutationRequestStatus.Pending, snapshot.Status));
        Assert.Equal(MutationRequestStatus.Approved, approved.Status);
        Assert.Equal(request.RequestId, exception.RequestId);
        Assert.Equal(0, exception.ExpectedRevision);
        Assert.Equal(MutationRequestStatus.Approved, store.Current.Status);
        Assert.Equal(1, store.Current.Revision);
    }
}
