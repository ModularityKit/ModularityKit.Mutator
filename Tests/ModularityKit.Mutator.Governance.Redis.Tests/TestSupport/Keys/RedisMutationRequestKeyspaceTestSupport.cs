using ModularityKit.Mutator.Governance.Redis.Configuration;
using ModularityKit.Mutator.Governance.Redis.Keys;

namespace ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Keys;

/// <summary>
/// Creates Redis mutation request keyspace fixtures for key-centric tests.
/// </summary>
internal static class RedisMutationRequestKeyspaceTestSupport
{
    /// <summary>
    /// Creates a keyspace with the default provider prefix used by tests.
    /// </summary>
    public static RedisMutationRequestKeyspace CreateKeyspace(string keyPrefix = "mk:gov")
        => new(new RedisMutationRequestStoreOptions
        {
            KeyPrefix = keyPrefix
        });
}
