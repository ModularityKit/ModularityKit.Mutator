namespace ModularityKit.Mutator.Governance.Redis.Configuration;

/// <summary>
/// Configuration for Redis-backed governance request storage.
/// </summary>
public sealed class RedisMutationRequestStoreOptions
{
    /// <summary>
    /// Key prefix used by the provider.
    /// </summary>
    public string KeyPrefix { get; set; } = "modularitykit:governance";
}
