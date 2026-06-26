using FeatureFlags.State;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace FeatureFlags.Mutations;

/// <summary>
/// Mutation that enables feature flag in the current <see cref="FeatureFlagsState"/>.
/// </summary>
internal sealed class EnableFeatureMutation(string featureName, MutationContext context)
    : MutationBase<FeatureFlagsState>(
        CreateIntent(
            operationName: "EnableFeature",
            category: "Security",
            description: "Enables a feature flag.",
            riskLevel: MutationRiskLevel.High,
            tags: new HashSet<string> { "auth" }),
        context)
{
    public string FeatureName { get; } = featureName;

    public override MutationResult<FeatureFlagsState> Apply(FeatureFlagsState state)
    {
        if (state.Flags.TryGetValue(FeatureName, out var oldValue) && oldValue)
            return MutationResult<FeatureFlagsState>.Success(state, ChangeSet.Empty);

        var newFlags = new Dictionary<string, bool>(state.Flags)
        {
            [FeatureName] = true
        };
        var newState = state with { Flags = newFlags };
        return Success(
            newState,
            StateChange.Modified($"Flags.{FeatureName}", oldValue, true));
    }

    public override ValidationResult Validate(FeatureFlagsState state)
    {
        var result = new ValidationResult();
        if (string.IsNullOrEmpty(FeatureName))
        {
            result.AddError("FeatureName", "Feature name cannot be empty");
        }
        else if (!state.Flags.ContainsKey(FeatureName))
        {
            result.AddError("FeatureName", $"Feature '{FeatureName}' does not exist");
        }
        return result;
    }
}
