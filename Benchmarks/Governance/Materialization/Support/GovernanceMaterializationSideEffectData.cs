using ModularityKit.Mutator.Abstractions.Effects;

namespace ModularityKit.Mutator.Benchmarks.Governance.Materialization.Support;

/// <summary>
/// Typed payload used to give governance side effects realistic materialization shape.
/// </summary>
/// <param name="Token">A stable payload token.</param>
/// <param name="Index">The ordinal of the side effect.</param>
[SideEffectDataContract("governance.materialization.side-effect", 1)]
internal sealed record GovernanceMaterializationSideEffectData(string Token, int Index);
