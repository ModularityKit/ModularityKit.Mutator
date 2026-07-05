using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Policies.Deployment;

/// <summary>
/// Supplies the fallback deployment path for non production releases.
/// </summary>
/// <remarks>
/// The policy does not inspect environment-specific risk beyond the fact that it
/// is the default branch in the composed deployment gate. It moves the release to
/// a deploy-ready stage and contributes both a side effect and metadata so the
/// composition result stays auditable.
/// </remarks>
internal sealed class DefaultDeploymentPolicy : IMutationPolicy<ReleaseGateState>
{
    /// <summary>
    /// Policy identifier used in composition metadata.
    /// </summary>
    public string Name => "DefaultDeployment";

    /// <summary>
    /// Lowest priority in the deployment composition, so it acts as the fallback branch.
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Describes the fallback deployment behavior.
    /// </summary>
    public string Description => "Default non-production deployment path.";

    /// <summary>
    /// Moves the release into the ready for deployment stage and emits an audit trace.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current release state.</param>
    /// <returns>An allowed decision that marks the release ready for deployment.</returns>
    public PolicyDecision Evaluate(IMutation<ReleaseGateState> mutation, ReleaseGateState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["State"] = state with { Stage = "ReadyForDeploy" },
                ["SideEffect"] = SideEffect.Create("audit", "Default deployment path selected.")
            },
            Metadata = new Dictionary<string, object>
            {
                ["deploymentPath"] = "default"
            }
        };
}
