using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Benchmarks.Concurrency.Support;

/// <summary>
/// Minimal commit mutation used to measure core runtime concurrency overhead.
/// </summary>
internal sealed class IncrementConcurrencyMutation(MutationContext context) : IMutation<ConcurrencyState>
{
    /// <summary>
    /// Gets the benchmark mutation intent metadata.
    /// </summary>
    public MutationIntent Intent { get; } = new()
    {
        OperationName = "IncrementConcurrencyState",
        Category = "Benchmark",
        Description = "Increment benchmark state to measure core runtime concurrency overhead.",
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
    public MutationResult<ConcurrencyState> Apply(ConcurrencyState state)
    {
        var nextState = state with
        {
            Counter = state.Counter + 1,
            Revision = state.Revision + 1
        };

        return MutationResult<ConcurrencyState>.Success(
            nextState,
            ChangeSet.FromChanges(
                StateChange.Modified(nameof(ConcurrencyState.Counter), state.Counter, nextState.Counter),
                StateChange.Modified(nameof(ConcurrencyState.Revision), state.Revision, nextState.Revision)
            ));
    }

    /// <summary>
    /// Validates the provided state before mutation execution.
    /// </summary>
    /// <param name="state">The input state.</param>
    /// <returns>A successful validation result.</returns>
    public ValidationResult Validate(ConcurrencyState state) => ValidationResult.Success();

    /// <summary>
    /// Simulates the benchmark mutation using the same state transition as commit execution.
    /// </summary>
    /// <param name="state">The input state.</param>
    /// <returns>The simulated mutation result.</returns>
    public MutationResult<ConcurrencyState> Simulate(ConcurrencyState state) => Apply(state);
}
