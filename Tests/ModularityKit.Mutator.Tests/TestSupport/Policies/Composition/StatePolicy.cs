using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that assigns a predefined state value.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify deterministic state merging
/// and modification conflict detection.
/// </remarks>
internal sealed class StatePolicy(string name, string value) : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Sets a fixed state value.";

    /// <summary>
    /// Produces an allowed decision containing a predefined state value.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>An allowed decision with a fixed state modification.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["State"] = new PolicySampleState(Value: value)
            }
        };
}