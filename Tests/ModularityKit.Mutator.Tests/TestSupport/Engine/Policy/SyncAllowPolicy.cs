using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;

/// <summary>
/// Synchronous allow policy used by policy evaluation tests.
/// </summary>
internal sealed class SyncAllowPolicy : IMutationPolicy<PolicySampleState>
{
    public string Name => "SyncAllow";
    public int Priority => 10;
    public string? Description => "Simple synchronous allow policy.";

    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => PolicyDecision.Allow(Name, "Synchronous policy allowed the mutation.");
}
