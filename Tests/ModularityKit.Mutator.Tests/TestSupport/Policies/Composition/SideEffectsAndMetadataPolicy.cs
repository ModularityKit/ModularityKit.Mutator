using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that contributes side effects and governance metadata.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify side effect aggregation and
/// metadata propagation across composed policy decisions.
/// </remarks>
internal sealed class SideEffectsAndMetadataPolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "SideEffectsAndMetadataPolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 200;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Adds an audit side effect and governance metadata.";

    /// <summary>
    /// Produces an allowed decision containing side effects and metadata.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>An allowed decision with side effects and metadata.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["SideEffects"] = new[]
                {
                    SideEffect.Create("notification", "Composed policy emitted a notification.")
                }
            },
            Metadata = new Dictionary<string, object>
            {
                ["owner"] = "state-policy"
            }
        };
}