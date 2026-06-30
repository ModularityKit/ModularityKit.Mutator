namespace ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;

/// <summary>
/// Resolves and validates the resulting state version for governed execution.
/// </summary>
internal static class GovernedExecutionResultingStateVersionResolver
{
    public static string Resolve<TState>(
        GovernedExecutionContext<TState> execution,
        TState newState)
    {
        var resultingStateVersion = execution.ResultingStateVersionProvider(newState);
        if (string.IsNullOrWhiteSpace(resultingStateVersion))
            throw new InvalidOperationException("Governed execution requires non empty resulting state version.");

        return resultingStateVersion;
    }
}
