using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

/// <summary>
/// Minimal commit mutation used to measure diagnostics and interception overhead.
/// </summary>
internal sealed class DiagnosticsMutation(MutationContext context) : IMutation<DiagnosticsState>
{
    /// <summary>
    /// Gets the benchmark mutation intent metadata.
    /// </summary>
    public MutationIntent Intent { get; } = new()
    {
        OperationName = "DiagnosticsMutation",
        Category = "Benchmark",
        Description = "Minimal commit mutation used to measure interception and diagnostics overhead.",
        RiskLevel = MutationRiskLevel.Low,
        IsReversible = true
    };

    /// <summary>
    /// Gets the execution context bound to the benchmark mutation instance.
    /// </summary>
    public MutationContext Context { get; } = context;

    /// <summary>
    /// Applies the benchmark mutation to the provided state.
    /// </summary>
    /// <param name="state">The input state.</param>
    /// <returns>The successful mutation result containing the updated state and changes.</returns>
    public MutationResult<DiagnosticsState> Apply(DiagnosticsState state)
    {
        var nextState = state with
        {
            Counter = state.Counter + 1,
            LastOperation = Context.CorrelationId ?? string.Empty
        };

        return MutationResult<DiagnosticsState>.Success(
            nextState,
            ChangeSet.FromChanges(
                StateChange.Modified(nameof(DiagnosticsState.Counter), state.Counter, nextState.Counter),
                StateChange.Modified(nameof(DiagnosticsState.LastOperation), state.LastOperation, nextState.LastOperation)
            ));
    }

    /// <summary>
    /// Validates the provided state before mutation execution.
    /// </summary>
    /// <param name="state">The input state.</param>
    /// <returns>A successful validation result.</returns>
    public ValidationResult Validate(DiagnosticsState state) => ValidationResult.Success();

    /// <summary>
    /// Simulates the benchmark mutation using the same state transition as commit execution.
    /// </summary>
    /// <param name="state">The input state.</param>
    /// <returns>The simulated mutation result.</returns>
    public MutationResult<DiagnosticsState> Simulate(DiagnosticsState state) => Apply(state);
}
