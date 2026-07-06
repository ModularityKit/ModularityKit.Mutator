using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Policies.Approval;

/// <summary>
/// Appends audit trail information to an already allowed approval gate decision.
/// </summary>
/// <remarks>
/// This policy does not block or change the release state. It exists to show how
/// composed policies can contribute side effects and metadata independently of
/// the policy that made the main business decision.
/// </remarks>
internal sealed class AddAuditTrailPolicy : IMutationPolicy<ReleaseGateState>
{
    /// <summary>
    /// Stable policy identifier used in composition metadata and diagnostics.
    /// </summary>
    public string Name => "AddAuditTrail";

    /// <summary>
    /// Medium priority so this policy can be grouped with the approval gate it decorates.
    /// </summary>
    public int Priority => 200;

    /// <summary>
    /// Describes the audit trail side effect this policy adds to the composed result.
    /// </summary>
    public string Description => "Adds audit metadata and a notification side effect.";

    /// <summary>
    /// Emits one audit side effect and a simple metadata flag.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current release state.</param>
    /// <returns>An allowed decision with audit metadata and a single side effect.</returns>
    public PolicyDecision Evaluate(IMutation<ReleaseGateState> mutation, ReleaseGateState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["SideEffects"] = new[]
                {
                    SideEffect.Create("audit", $"Release {state.ReleaseName} passed the composed approval gate.")
                }
            },
            Metadata = new Dictionary<string, object>
            {
                ["auditTrail"] = "enabled"
            }
        };
}
