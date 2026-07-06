using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.Mutations;
using PolicyComposition.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Scenarios;

/// <summary>
/// Demonstrates the <c>AllOf</c> composition mode for reusable governance gates.
/// </summary>
/// <remarks>
/// This scenario is intentionally small: it creates a release state, builds a
/// release submission mutation, evaluates the approval gate, and prints the
/// composed result. The goal is to show how the merged decision contains the
/// final state, audit side effects, and composition metadata.
/// </remarks>
internal static class AllOfScenario
{
    /// <summary>
    /// Evaluates the approval gate against a sample release submission.
    /// </summary>
    /// <remarks>
    /// The mutation carries approval count, emergency flag, and environment data
    /// through its execution context so the composed policies can make their own
    /// decisions without needing extra helper state.
    /// </remarks>
    public static async Task Run()
    {
        var state = new ReleaseGateState("release-42", "Draft", "platform");
        var mutation = new SubmitReleaseMutation(
            releaseName: state.ReleaseName,
            approvals: 2,
            emergency: false,
            environment: "staging");

        var decision = await ReleaseGovernancePolicies.ApprovalGate().EvaluateAsync(mutation, state);

        WriteDecision(decision);
    }

    /// <summary>
    /// Writes the important parts of the composed decision to the console.
    /// </summary>
    /// <param name="decision">The composed policy decision returned by the gate.</param>
    private static void WriteDecision(PolicyDecision decision)
    {
        Console.WriteLine("[AnyOf] Reusable override gate");

        Console.WriteLine($"  allowed: {decision.IsAllowed}");
        Console.WriteLine($"  reason: {decision.Reason}");
        Console.WriteLine($"  mode: {decision.Metadata!["PolicyComposition.Mode"]}");

        if (decision.Modifications is not null &&
            decision.Modifications.TryGetValue("State", out var value) &&
            value is ReleaseGateState state)
        {
            Console.WriteLine($"  stage: {state.Stage}");
            Console.WriteLine($"  owner: {state.Owner}");
        }

        if (decision.Modifications is not null &&
            decision.Modifications.TryGetValue("SideEffects", out var sideEffects) &&
            sideEffects is IEnumerable<object> effects)
        {
            Console.WriteLine($"  sideEffects: {effects.Count()}");
        }

        Console.WriteLine();
    }
}
