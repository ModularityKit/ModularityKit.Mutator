using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Keys;
using Xunit;

namespace ModularityKit.Mutator.Governance.Redis.Tests.Keys;

public sealed partial class RedisMutationRequestKeyspaceTests
{
    [Fact]
    public void Enumerate_indexes_includes_pending_indexes_only_for_pending_requests()
    {
        var keyspace = RedisMutationRequestKeyspaceTestSupport.CreateKeyspace();

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
