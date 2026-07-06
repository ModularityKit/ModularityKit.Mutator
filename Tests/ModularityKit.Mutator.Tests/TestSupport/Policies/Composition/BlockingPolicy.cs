using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that unconditionally blocks mutation execution.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify blocking behavior,
/// precedence rules, and denial propagation.
/// </remarks>
internal sealed class BlockingPolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "BlockingPolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 400;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Rejects the mutation.";

    /// <summary>
    /// Produces a blocking policy decision.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>A denial decision.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => PolicyDecision.Deny("Blocked by policy.", Name);
}