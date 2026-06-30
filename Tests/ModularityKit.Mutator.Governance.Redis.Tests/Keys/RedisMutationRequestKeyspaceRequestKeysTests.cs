using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis;
using ModularityKit.Mutator.Governance.Redis.Keys;
using ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Keys;
using Xunit;

namespace ModularityKit.Mutator.Governance.Redis.Tests.Keys;

/// <summary>
/// Verifies Redis mutation request key construction for provider scenarios.
/// </summary>
public sealed partial class RedisMutationRequestKeyspaceTests
{
    /// <summary>
    /// Verifies request data and identity keys derived from the configured prefix.
    /// </summary>
    [Fact]
    public void Builds_expected_request_keys_from_prefix()
    {
        var keyspace = RedisMutationRequestKeyspaceTestSupport.CreateKeyspace();

        Assert.Equal("mk:gov:requests:ids", keyspace.RequestIds().ToString());
        Assert.Equal("mk:gov:requests:req-42:data", keyspace.RequestData("req-42").ToString());
        Assert.Equal("mk:gov:requests:req-42:revision", keyspace.RequestRevision("req-42").ToString());
    }
}
