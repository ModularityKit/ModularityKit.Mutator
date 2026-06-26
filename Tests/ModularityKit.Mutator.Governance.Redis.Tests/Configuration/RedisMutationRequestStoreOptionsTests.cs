using ModularityKit.Mutator.Governance.Redis;
using ModularityKit.Mutator.Governance.Redis.Configuration;
using Xunit;

namespace ModularityKit.Mutator.Governance.Redis.Tests.Configuration;

public sealed class RedisMutationRequestStoreOptionsTests
{
    [Fact]
    public void Uses_expected_default_key_prefix()
    {
        var options = new RedisMutationRequestStoreOptions();

        Assert.Equal("modularitykit:governance", options.KeyPrefix);
    }
}
