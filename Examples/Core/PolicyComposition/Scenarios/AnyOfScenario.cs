using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.Mutations;
using PolicyComposition.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Scenarios;

/// <summary>
/// Demonstrates the <c>AnyOf</c> composition mode for alternate governance branches.
/// </summary>
/// <remarks>
/// This scenario evaluates the emergency gate against a sample release submission.
/// The setup is deliberately asymmetric: the emergency flag is enabled, so the
/// composed gate can show how one allowed branch wins while the blocked branch is
/// still visible in the output metadata.
/// </remarks>
internal static class AnyOfScenario
{
    /// <summary>
    /// Evaluates the emergency gate and prints the winning and blocking branches.
    /// </summary>
    /// <remarks>
    /// The sample mutation carries governance metadata through the execution
    /// context. That allows the emergency override policy to detect the explicit
    /// override flag while the fallback branch remains available as a reusable
    /// alternative path.
    /// </remarks>
    public static async Task Run()
    {
        var state = new ReleaseGateState("release-42", "Draft", "platform");
        var mutation = new SubmitReleaseMutation(
            releaseName: state.ReleaseName,
            approvals: 0,
            emergency: true,
            environment: "staging");

        var decision = await ReleaseGovernancePolicies.EmergencyGate().EvaluateAsync(mutation, state);

        WriteDecision(decision);
    }

    /// <summary>
    /// Writes the merged result of the emergency gate to the console.
    /// </summary>
    /// <param name="decision">The composed policy decision returned by the gate.</param>
    private static void WriteDecision(PolicyDecision decision)
    {
        Console.WriteLine("[AnyOf] Emergency override gate");
        Console.WriteLine($"  allowed: {decision.IsAllowed}");
        Console.WriteLine($"  reason: {decision.Reason}");
        Console.WriteLine($"  winning: {string.Join(", ", (string[])decision.Metadata!["PolicyComposition.WinningPolicies"])}");
        Console.WriteLine($"  blocking: {string.Join(", ", (string[])decision.Metadata!["PolicyComposition.BlockingPolicies"])}");

        if (decision.Modifications is not null && decision.Modifications.TryGetValue("State", out var value) && value is ReleaseGateState state)
        {
            Console.WriteLine($"  stage: {state.Stage}");
        }

        Console.WriteLine();
    }
}
