using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that allows the mutation and replaces the sample state value.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify that state modifications from
/// allowed policies are propagated correctly through composed decisions.
/// </remarks>
internal sealed class AllowedStatePolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "AllowedStatePolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Applies the allowed branch.";

    /// <summary>
    /// Produces an allowed decision containing a modified sample state.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>An allowed decision with replacement state modification.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["State"] = new PolicySampleState(Value: "allowed")
            }
        };
}
