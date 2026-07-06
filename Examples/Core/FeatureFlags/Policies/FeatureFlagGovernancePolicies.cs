using FeatureFlags.State;
using ModularityKit.Mutator.Abstractions.Policies;

namespace FeatureFlags.Policies;

/// <summary>
/// Reusable policy compositions for sensitive feature flag changes.
/// </summary>
internal static class FeatureFlagGovernancePolicies
{
    /// <summary>
    /// Composed governance policy set for critical feature flag changes.
    /// </summary>
    public static IMutationPolicy<FeatureFlagsState> CriticalChanges() =>
        PolicyComposition.AllOf(
            name: "CriticalFeatureFlagGovernance",
            policies:
            [
                new BusinessHoursPolicy(),
                new RequireTwoManApprovalPolicy()
            ],
            priority: 200,
            description: "Requires business-hours execution and two-man approval for critical feature flag changes.");
}
