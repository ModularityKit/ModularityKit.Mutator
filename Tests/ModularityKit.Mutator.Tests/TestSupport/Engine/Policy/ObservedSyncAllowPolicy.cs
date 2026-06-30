using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;

/// <summary>
/// Synchronous allow policy that records evaluation order.
/// </summary>
internal sealed class ObservedSyncAllowPolicy(List<string> observed) : IMutationPolicy<PolicySampleState>
{
    public string Name => "ObservedSyncAllow";
    public int Priority => 10;
    public string? Description => "Records synchronous policy evaluation order.";

    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
    {
        observed.Add("sync");
        return PolicyDecision.Allow(Name, "Synchronous policy allowed the mutation.");
    }
}
