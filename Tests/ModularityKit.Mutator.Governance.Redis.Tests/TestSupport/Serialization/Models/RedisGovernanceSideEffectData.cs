using ModularityKit.Mutator.Abstractions.Effects;

namespace ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Serialization.Models;

/// <summary>
/// Side-effect payload used by the serializer roundtrip fixture.
/// </summary>
[SideEffectDataContract("redis.governance.side-effect", 1)]
internal sealed record RedisGovernanceSideEffectData
{
    /// <summary>
    /// Gets the external ticket reference carried by the side effect.
    /// </summary>
    public required string Ticket { get; init; }
}
