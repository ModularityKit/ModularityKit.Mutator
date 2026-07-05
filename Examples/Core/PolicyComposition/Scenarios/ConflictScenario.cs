using ModularityKit.Mutator.Abstractions.Exceptions;
using PolicyComposition.Mutations;
using PolicyComposition.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Scenarios;

/// <summary>
/// Demonstrates explicit conflict detection during composition.
/// </summary>
/// <remarks>
/// This scenario intentionally composes two policies that both write the owner
/// field with different values. The example is useful because it shows that the
/// composition layer does not silently pick one result. Instead, it fails fast
/// with a conflict exception that names the field and the policies involved.
/// </remarks>
internal static class ConflictScenario
{
    /// <summary>
    /// Evaluates a conflicting policy set and prints the exception details.
    /// </summary>
    /// <remarks>
    /// The exception is caught locally so the example can show the conflict
    /// diagnostics without stopping the rest of the console run.
    /// </remarks>
    public static async Task Run()
    {
        var state = new ReleaseGateState("release-42", "Draft", "platform");
        var mutation = new SubmitReleaseMutation(
            releaseName: state.ReleaseName,
            approvals: 2,
            emergency: false,
            environment: "staging");
        
        try
        {
            await ReleaseGovernancePolicies.ConflictingOwnerGate().EvaluateAsync(mutation, state);
        }
        catch (PolicyCompositionConflictException exception)
        {
            Console.WriteLine($"  conflict key: {exception.ConflictKey}");
            Console.WriteLine($"  policies: {string.Join(", ", exception.PolicyNames)}");
            Console.WriteLine($"  message: {exception.Message}");
            Console.WriteLine();
        }
    }
}
