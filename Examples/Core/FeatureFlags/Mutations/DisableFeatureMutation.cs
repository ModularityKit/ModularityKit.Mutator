using FeatureFlags.State;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace FeatureFlags.Mutations;

/// <summary>
/// Mutation that disables a feature flag in the current <see cref="FeatureFlagsState"/>.
/// </summary>
internal sealed class DisableFeatureMutation(string featureName, MutationContext context)
    : MutationBase<FeatureFlagsState>(
        CreateIntent(
            operationName: "DisableFeature",
            category: "Configuration",
            description: "Disables a feature flag.",
            riskLevel: MutationRiskLevel.High),
        context)
{
    public string FeatureName { get; } = featureName;

    public override MutationResult<FeatureFlagsState> Apply(FeatureFlagsState state)
    {
        var newFlags = new Dictionary<string, bool>(state.Flags);
        if (newFlags.ContainsKey(FeatureName))
            newFlags[FeatureName] = false;
        
        var newState = state with { Flags = newFlags };
        return Success(
            newState,
            StateChange.Modified($"Flags.{FeatureName}", true, false));
    }

    public override ValidationResult Validate(FeatureFlagsState state)
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrEmpty(FeatureName))
            result.AddError("FeatureName", "Feature name cannot be empty");
        if (!state.Flags.ContainsKey(FeatureName))
            result.AddError("FeatureName", $"Feature '{FeatureName}' does not exist");
        return result;
    }
}
