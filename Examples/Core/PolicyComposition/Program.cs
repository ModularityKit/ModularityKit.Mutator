using PolicyComposition.Scenarios;

namespace PolicyCompositionExample;

/// <summary>
/// Entry point for the policy composition example.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs the four sample scenarios for composed governance policies.
    /// </summary>
    private static async Task Main()
    {
        Console.WriteLine("=== Policy Composition Example ===\n");

        await AllOfScenario.Run();
        await AnyOfScenario.Run();
        await PriorityScenario.Run();
        await ConflictScenario.Run();
    }
}
