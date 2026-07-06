using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Policies.Deployment;

/// <summary>
/// Stops production releases before fallback policies can be applied.
/// </summary>
/// <remarks>
/// This policy exists to demonstrate the priority composition mode. It evaluates
/// first, looks at the environment metadata, and returns a decisive denial when
/// the release targets production. For any other environment, it allows the
/// composition to continue to the next policy.
/// </remarks>
internal sealed class ProductionGuardPolicy : IMutationPolicy<ReleaseGateState>
{
    /// <summary>
    /// Policy identifier used in decision metadata.
    /// </summary>
    public string Name => "ProductionGuard";

    /// <summary>
    /// Highest priority in the deployment gate, so the production check runs first.
    /// </summary>
    public int Priority => 500;

    /// <summary>
    /// Summarizes the production protection rule enforced by this policy.
    /// </summary>
    public string Description => "Blocks production releases before lower priority policies can run.";

    /// <summary>
    /// Checks the environment metadata and either denies production or allows fallback.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current release state.</param>
    /// <returns>
    /// A critical denial for production deployments, or an allowed decision that
    /// hands control to lower-priority policies.
    /// </returns>
    public PolicyDecision Evaluate(IMutation<ReleaseGateState> mutation, ReleaseGateState state)
    {
        var environment = GetString(mutation.Context.Metadata, "environment");

        return string.Equals(environment, "production", StringComparison.OrdinalIgnoreCase)
            ? PolicyDecision.DenyCritical("Production releases require a dedicated change window.", Name)
            : PolicyDecision.Allow(Name, $"Environment '{environment}' falls through to the next policy.");
    }

    private static string GetString(IReadOnlyDictionary<string, object> metadata, string key)
        => metadata.TryGetValue(key, out var value) && value is string text ? text : string.Empty;
}