using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;

/// <summary>
/// Asynchronous policy that waits long enough for caller cancellation to trigger.
/// </summary>
internal sealed class CancelAwareAsyncPolicy : IMutationPolicy<PolicySampleState>
{
    public string Name => "CancelAware";
    public int Priority => 100;
    public string? Description => "Waits for cancellation.";

    public async Task<PolicyDecision> EvaluateAsync(
        IMutation<PolicySampleState> mutation,
        PolicySampleState state,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        return PolicyDecision.Allow(Name, "Completed.");
    }
}
