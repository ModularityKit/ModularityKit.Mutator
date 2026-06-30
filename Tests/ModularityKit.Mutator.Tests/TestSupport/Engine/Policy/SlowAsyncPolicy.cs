using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;

/// <summary>
/// Asynchronous policy that exceeds the configured timeout window.
/// </summary>
internal sealed class SlowAsyncPolicy : IMutationPolicy<PolicySampleState>
{
    public string Name => "SlowExternalCheck";
    public int Priority => 100;
    public string? Description => "Simulates a slow external dependency.";

    public async Task<PolicyDecision> EvaluateAsync(
        IMutation<PolicySampleState> mutation,
        PolicySampleState state,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        return PolicyDecision.Allow(Name, "Finished too late.");
    }
}
