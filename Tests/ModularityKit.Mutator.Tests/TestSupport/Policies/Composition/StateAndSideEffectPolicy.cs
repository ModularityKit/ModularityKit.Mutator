using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that contributes state modification and single side effect.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify state modification merging,
/// side effect aggregation, and metadata propagation.
/// </remarks>
internal sealed class StateAndSideEffectPolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "StateAndSideEffectPolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 300;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Moves state and records one side effect.";

    /// <summary>
    /// Produces an allowed decision containing a state modification,
    /// a side effect, and metadata.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>An allowed decision with state, side effect, and metadata.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["State"] = new PolicySampleState(Value: "governed"),
                ["SideEffect"] = SideEffect.Create("audit", "State changed by the composed policy set.")
            },
            Metadata = new Dictionary<string, object>
            {
                ["source"] = "state-policy"
            }
        };
}