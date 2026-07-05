using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that contributes high priority state modification.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify priority selection,
/// metadata propagation, and side effect aggregation.
/// </remarks>
internal sealed class HighPriorityStatePolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "HighPriorityStatePolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 500;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Higher priority branch.";

    /// <summary>
    /// Produces an allowed decision with a high-priority state modification,
    /// metadata, and an audit side effect.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>An allowed decision with state, metadata, and side effects.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["State"] = new PolicySampleState(Value: "high"),
                ["SideEffect"] = SideEffect.Create("audit", "High priority branch selected.")
            },
            Metadata = new Dictionary<string, object>
            {
                ["selectedPolicy"] = "high"
            }
        };
}