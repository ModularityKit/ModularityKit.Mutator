using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;

/// <summary>
/// Test policy that blocks execution and requires explicit approval.
/// </summary>
/// <remarks>
/// Used by policy composition tests to verify approval requirements,
/// blocking decisions, and requirement aggregation.
/// </remarks>
internal sealed class ApprovalPolicy : IMutationPolicy<PolicySampleState>
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name => "ApprovalPolicy";

    /// <summary>
    /// Gets the evaluation priority.
    /// </summary>
    public int Priority => 200;

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    public string Description => "Requires signoff for sensitive work.";

    /// <summary>
    /// Produces a blocking decision that requires explicit approval.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current sample state.</param>
    /// <returns>A blocking decision with an approval requirement.</returns>
    public PolicyDecision Evaluate(IMutation<PolicySampleState> mutation, PolicySampleState state)
        => new()
        {
            IsAllowed = false,
            PolicyName = Name,
            Severity = PolicyDecisionSeverity.Error,
            Reason = "Sensitive change requires approval.",
            Requirements = [PolicyRequirement.Approval("approver-a", "Sensitive change requires approval.")]
        };
}