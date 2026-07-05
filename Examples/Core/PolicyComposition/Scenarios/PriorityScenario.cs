using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.Mutations;
using PolicyComposition.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Scenarios;

/// <summary>
/// Demonstrates the priority-based composition mode for deterministic policy selection.
/// </summary>
/// <remarks>
/// This scenario evaluates the deployment gate twice: once for a staging release
/// and once for a production release. The first path shows the fallback branch
/// being selected, while the second path shows the guard policy short-circuiting
/// the composition with a decisive denial.
/// </remarks>
internal static class PriorityScenario
{
    /// <summary>
    /// Evaluates the deployment gate for staging and production inputs.
    /// </summary>
    /// <remarks>
    /// The two inputs only differ by environment, which keeps the example focused
    /// on priority ordering rather than on unrelated mutation state.
    /// </remarks>
    public static async Task Run()
    {
        var state = new ReleaseGateState("release-42", "Draft", "platform");

        var stagingMutation = new SubmitReleaseMutation(
            releaseName: state.ReleaseName,
            approvals: 1,
            emergency: false,
            environment: "staging");

        var productionMutation = new SubmitReleaseMutation(
            releaseName: state.ReleaseName,
            approvals: 1,
            emergency: false,
            environment: "production");

        Console.WriteLine("  staging path:");
        WriteDecision(await ReleaseGovernancePolicies.DeploymentGate().EvaluateAsync(stagingMutation, state));

        Console.WriteLine("  production path:");
        WriteDecision(await ReleaseGovernancePolicies.DeploymentGate().EvaluateAsync(productionMutation, state));
    }

    /// <summary>
    /// Writes the outcome of the priority-based composition.
    /// </summary>
    /// <param name="decision">The composed policy decision returned by the gate.</param>
    private static void WriteDecision(PolicyDecision decision)
    {
        Console.WriteLine($"    allowed: {decision.IsAllowed}");
        Console.WriteLine($"    reason: {decision.Reason}");
        Console.WriteLine($"    selected: {string.Join(", ", (string[])decision.Metadata!["PolicyComposition.WinningPolicies"])}");

        if (decision.Modifications is not null && decision.Modifications.TryGetValue("State", out var value) && value is ReleaseGateState state)
        {
            Console.WriteLine($"    stage: {state.Stage}");
        }
    }
}
