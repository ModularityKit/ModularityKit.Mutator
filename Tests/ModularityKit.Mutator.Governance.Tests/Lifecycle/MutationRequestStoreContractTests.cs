using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Requests;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Lifecycle;

public sealed class MutationRequestStoreContractTests
{
    [Fact]
    public async Task Create_contract_rejects_duplicate_request_ids()
    {
        var store = new InMemoryMutationRequestStore();
        var request = MutationRequestTestFactory.CreatePendingRequest();

        var created = await store.Create(request);

        var exception = await Assert.ThrowsAsync<MutationRequestAlreadyExistsException>(() =>
            store.Create(created));

        Assert.Equal(request.RequestId, exception.RequestId);
    }

    [Fact]
    public async Task TryStore_rejects_stale_revision_and_preserves_current_state()
    {
        var store = new InMemoryMutationRequestStore();
        var request = MutationRequestTestFactory.CreatePendingRequest();
        var created = await store.Create(request);

        var firstUpdate = created with
        {
            Lifecycle = created.Lifecycle with
            {
                Status = MutationRequestStatus.Approved,
                PendingReason = null
            },
            Decisions =
            [
                .. created.Decisions,
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
                    MutationContext.User("approver", "Approver", "Approve request"))
            ]
        };

        var persisted = await store.TryStore(firstUpdate, created.Revision);
        Assert.NotNull(persisted);

        var staleUpdate = created with
        {
            Lifecycle = created.Lifecycle with
            {
                Status = MutationRequestStatus.Canceled,
                PendingReason = null
            },
            Decisions =
            [
                .. created.Decisions,
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Canceled),
                    MutationContext.User("operator", "Operator", "Cancel request"))
            ]
        };

        var rejected = await store.TryStore(staleUpdate, created.Revision);

        var loaded = await store.Get(request.RequestId);

        Assert.Null(rejected);
        Assert.NotNull(loaded);
        Assert.Equal(MutationRequestStatus.Approved, loaded.Status);
        Assert.Equal(1, loaded.Revision);
    }
}
