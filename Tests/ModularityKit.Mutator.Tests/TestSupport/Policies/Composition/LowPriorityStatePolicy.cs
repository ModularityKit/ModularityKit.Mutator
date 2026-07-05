using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that contributes low priority state modification.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify deterministic priority ordering
/// and metadata conflict handling.
/// </remarks>
internal sealed class LowPriorityStatePolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "LowPriorityStatePolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Lower priority branch.";

    /// <summary>
    /// Produces an allowed decision with a low-priority state modification.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>An allowed decision with state and metadata.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["State"] = new PolicySampleState(Value: "low")
            },
            Metadata = new Dictionary<string, object>
            {
                ["selectedPolicy"] = Name
            }
        };
}