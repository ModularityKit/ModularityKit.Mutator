using ModularityKit.Mutator.Abstractions.Effects;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Queries.Model;

/// <summary>
/// Query-side side-effect payload used by governance query scenarios.
/// </summary>
[SideEffectDataContract("governance.side-effect")]
internal sealed record GovernanceSideEffectData
{
    /// <summary>
    /// Gets the external reference carried by the side effect.
    /// </summary>
    public required string Reference { get; init; }
}
