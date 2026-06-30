using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;

/// <summary>
/// Asynchronous allow policy that records evaluation order.
/// </summary>
internal sealed class ObservedAsyncAllowPolicy(List<string> observed) : IMutationPolicy<PolicySampleState>
{
    public string Name => "ObservedAsyncAllow";
    public int Priority => 100;
    public string? Description => "Records asynchronous policy evaluation order.";

    public async Task<PolicyDecision> EvaluateAsync(
        IMutation<PolicySampleState> mutation,
        PolicySampleState state,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        observed.Add("async");
        return PolicyDecision.Allow(Name, "Asynchronous policy allowed the mutation.");
    }
}
