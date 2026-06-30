using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Keys;
using Xunit;

namespace ModularityKit.Mutator.Governance.Redis.Tests.Keys;

public sealed partial class RedisMutationRequestKeyspaceTests
{
    [Fact]
    public void Builds_expected_index_keys_for_state_status_and_pending_reason()
    {
        var keyspace = RedisMutationRequestKeyspaceTestSupport.CreateKeyspace();

        Assert.Equal("mk:gov:states:tenant-42:requests", keyspace.RequestsByStateId("tenant-42").ToString());
        Assert.Equal("mk:gov:status:pending:requests", keyspace.RequestsByStatus(MutationRequestStatus.Pending).ToString());
        Assert.Equal("mk:gov:pending:requests", keyspace.PendingRequestIds().ToString());
        Assert.Equal(
            "mk:gov:pending:approval:requests",
            keyspace.PendingRequestIds(PendingMutationReason.Approval).ToString());
    }
}
