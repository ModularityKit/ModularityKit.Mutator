using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;

/// <summary>
/// Asynchronous deny policy that simulates external compliance rejection.
/// </summary>
internal sealed class AsyncBlockingPolicy : IMutationPolicy<PolicySampleState>
{
    public string Name => "AsyncBlocking";
    public int Priority => 100;
    public string? Description => "Simulates an external compliance check.";

    public async Task<PolicyDecision> EvaluateAsync(
        IMutation<PolicySampleState> mutation,
        PolicySampleState state,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        return PolicyDecision.Deny("External compliance check rejected the mutation.", Name);
    }
}
