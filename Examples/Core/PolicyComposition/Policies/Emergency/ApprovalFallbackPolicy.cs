using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Policies.Emergency;

/// <summary>
/// Supplies the approval-based fallback branch for the emergency gate.
/// </summary>
/// <remarks>
/// This policy is only intended to make the emergency composition reusable when
/// the explicit override is unavailable. It checks the same approval count used by
/// the standard approval policy, but it maps the successful outcome to a different
/// release stage so the example can show branch selection in the merged decision.
/// </remarks>
internal sealed class ApprovalFallbackPolicy : IMutationPolicy<ReleaseGateState>
{
    /// <summary>
    /// Policy identifier used in composed diagnostics.
    /// </summary>
    public string Name => "ApprovalFallback";

    /// <summary>
    /// Lower than the override branch but high enough to participate in emergency gating.
    /// </summary>
    public int Priority => 200;

    /// <summary>
    /// Describes the approval-based fallback path.
    /// </summary>
    public string Description => "Fallback branch that allows the release once approvals exist.";

    /// <summary>
    /// Uses the approval count to decide whether the emergency fallback can proceed.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current release state.</param>
    /// <returns>
    /// An allowed decision that advances the release through the fallback stage
    /// or a blocking decision when approvals are missing.
    /// </returns>
    public PolicyDecision Evaluate(IMutation<ReleaseGateState> mutation, ReleaseGateState state)
    {
        var approvals = GetInt32(mutation.Context.Metadata, "approvals");

        return approvals >= 2
            ? new PolicyDecision
            {
                IsAllowed = true,
                PolicyName = Name,
                Modifications = new Dictionary<string, object>
                {
                    ["State"] = state with { Stage = "ApprovedViaFallback" },
                    ["SideEffect"] = SideEffect.Create("audit", "Fallback approval branch selected.")
                }
            }
            : PolicyDecision.Deny($"Fallback approval branch rejected the release; found {approvals} approvals.", Name);
    }

    private static int GetInt32(IReadOnlyDictionary<string, object> metadata, string key)
        => metadata.TryGetValue(key, out var value) && value is int number ? number : 0;
}
