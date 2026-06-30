using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;

/// <summary>
/// Asynchronous policy that throws to exercise failure wrapping behavior.
/// </summary>
internal sealed class FailingAsyncPolicy : IMutationPolicy<PolicySampleState>
{
    public string Name => "FailingExternalCheck";
    public int Priority => 100;
    public string? Description => "Simulates an external dependency failure.";

    public Task<PolicyDecision> EvaluateAsync(
        IMutation<PolicySampleState> mutation,
        PolicySampleState state,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Remote ticketing system unavailable.");
}
