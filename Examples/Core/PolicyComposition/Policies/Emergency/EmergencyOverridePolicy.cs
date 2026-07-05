using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Policies.Emergency;

/// <summary>
/// Enables a hard emergency override when the request explicitly asks for it.
/// </summary>
/// <remarks>
/// This policy demonstrates a decisive allow-or-deny branch inside an AnyOf
/// composition. It reads the emergency flag from mutation metadata, and when the
/// flag is present, it promotes the release to an emergency-approved stage and
/// adds a critical side effect for audit visibility.
/// </remarks>
internal sealed class EmergencyOverridePolicy : IMutationPolicy<ReleaseGateState>
{
    /// <summary>
    /// Policy identifier used in metadata and logs.
    /// </summary>
    public string Name => "EmergencyOverride";

    /// <summary>
    /// Higher than the fallback branch, so the explicit override wins first.
    /// </summary>
    public int Priority => 400;

    /// <summary>
    /// Describes the emergency override behavior.
    /// </summary>
    public string Description => "Allows an emergency release override when requested explicitly.";

    /// <summary>
    /// Reads the emergency flag from metadata and either approves the override or denies it.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current release state.</param>
    /// <returns>
    /// An allowed override decision when the emergency flag is set, otherwise a
    /// standard denial that allows the AnyOf composition to keep searching.
    /// </returns>
    public PolicyDecision Evaluate(IMutation<ReleaseGateState> mutation, ReleaseGateState state)
    {
        var emergency = GetBool(mutation.Context.Metadata, "emergency");

        return emergency
            ? new PolicyDecision
            {
                IsAllowed = true,
                PolicyName = Name,
                Modifications = new Dictionary<string, object>
                {
                    ["State"] = state with { Stage = "EmergencyApproved" },
                    ["SideEffect"] = SideEffect.Critical("release.override", "Emergency override applied.")
                },
                Metadata = new Dictionary<string, object>
                {
                    ["override"] = true
                }
            }
            : PolicyDecision.Deny("Emergency override not requested.", Name);
    }

    private static bool GetBool(IReadOnlyDictionary<string, object> metadata, string key)
        => metadata.TryGetValue(key, out var value) && value is true;
}
