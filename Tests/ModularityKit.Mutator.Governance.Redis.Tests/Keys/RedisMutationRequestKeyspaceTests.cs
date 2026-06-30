using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis;
using ModularityKit.Mutator.Governance.Redis.Configuration;
using ModularityKit.Mutator.Governance.Redis.Keys;
using Xunit;

namespace ModularityKit.Mutator.Governance.Redis.Tests.Keys;

public sealed class RedisMutationRequestKeyspaceTests
{
    [Fact]
    public void Builds_expected_request_keys_from_prefix()
    {
        var keyspace = new RedisMutationRequestKeyspace(new RedisMutationRequestStoreOptions
        {
            KeyPrefix = "mk:gov"
        });

        Assert.Equal("mk:gov:requests:ids", keyspace.RequestIds().ToString());
        Assert.Equal("mk:gov:requests:req-42:data", keyspace.RequestData("req-42").ToString());
        Assert.Equal("mk:gov:requests:req-42:revision", keyspace.RequestRevision("req-42").ToString());
    }

    [Fact]
    public void Builds_expected_index_keys_for_state_status_and_pending_reason()
    {
        var keyspace = new RedisMutationRequestKeyspace(new RedisMutationRequestStoreOptions
        {
            KeyPrefix = "mk:gov"
        });

        Assert.Equal("mk:gov:states:tenant-42:requests", keyspace.RequestsByStateId("tenant-42").ToString());
        Assert.Equal("mk:gov:status:pending:requests", keyspace.RequestsByStatus(MutationRequestStatus.Pending).ToString());
        Assert.Equal("mk:gov:pending:requests", keyspace.PendingRequestIds().ToString());
        Assert.Equal(
            "mk:gov:pending:approval:requests",
            keyspace.PendingRequestIds(PendingMutationReason.Approval).ToString());
    }

    [Fact]
    public void Enumerate_indexes_includes_pending_indexes_only_for_pending_requests()
    {
        var keyspace = new RedisMutationRequestKeyspace(new RedisMutationRequestStoreOptions
        {
            KeyPrefix = "mk:gov"
        });

        var request = new MutationRequest
        {
            RequestId = "req-42",
            Scope = new MutationRequestScopeDetails
            {
                StateId = "tenant-42",
                StateType = "IamRoleState",
                MutationType = "GrantRoleMutation"
            },
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Approval
            }
        };

        var keys = keyspace.EnumerateIndexes(request).Select(key => key.ToString()).ToArray();

        Assert.Contains("mk:gov:requests:ids", keys);
        Assert.Contains("mk:gov:states:tenant-42:requests", keys);
        Assert.Contains("mk:gov:status:pending:requests", keys);
        Assert.Contains("mk:gov:pending:requests", keys);
        Assert.Contains("mk:gov:pending:approval:requests", keys);
    }
}
