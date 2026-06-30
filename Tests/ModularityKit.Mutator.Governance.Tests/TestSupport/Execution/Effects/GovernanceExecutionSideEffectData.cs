using ModularityKit.Mutator.Abstractions.Effects;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Effects;

/// <summary>
/// Side-effect payload used by governed execution tests.
/// </summary>
[SideEffectDataContract("governance.execution-effect")]
internal sealed record GovernanceExecutionSideEffectData
{
    /// <summary>
    /// Gets the state identifier associated with the governed request.
    /// </summary>
    public required string RequestStateId { get; init; }

    /// <summary>
    /// Gets the resulting role value produced by the test mutation.
    /// </summary>
    public required string NewRole { get; init; }
}
