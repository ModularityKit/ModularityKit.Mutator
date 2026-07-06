using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that contributes governance metadata while blocking execution.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify metadata aggregation,
/// propagation, and conflict detection across composed decisions.
/// </remarks>
internal sealed class MetadataPolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "MetadataPolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 150;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Adds governance metadata.";

    /// <summary>
    /// Produces blocking decision containing governance metadata.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>A blocking decision with metadata.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = false,
            PolicyName = Name,
            Reason = "Metadata policy requires approval.",
            Severity = PolicyDecisionSeverity.Warning,
            Metadata = new Dictionary<string, object>
            {
                ["team"] = "compliance",
                ["owner"] = "platform"
            }
        };
}