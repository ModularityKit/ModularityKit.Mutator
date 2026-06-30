using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Results;
using System.Diagnostics;

namespace ModularityKit.Mutator.Runtime.Internal.Execution;

/// <summary>
/// Executes a sequence of mutations against an evolving state, accumulating results and changes.
/// </summary>
internal static class MutationBatchExecutor
{
    /// <summary>
    /// Iterates over <paramref name="mutations" /> in order, executing each against the current state.
    /// Successful mutations advance the state; failed mutations are recorded and may halt the batch.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutations.</typeparam>
    /// <param name="mutations">The ordered sequence of mutations to execute.</param>
    /// <param name="initialState">The starting state before any mutation is applied.</param>
    /// <param name="stopOnFirstFailure">When <see langword="true" />, halts execution after the first unsuccessful mutation.</param>
    /// <param name="executeAsync">The delegate used to execute a single mutation against the current state.</param>
    /// <param name="cancellationToken">Token used to cancel batch execution.</param>
    /// <returns>
    /// A <see cref="BatchMutationResult{TState}" /> containing all individual results, the aggregated
    /// change set, the final state (when all mutations succeeded), and the total execution time.
    /// </returns>
    public static async Task<BatchMutationResult<TState>> ExecuteAsync<TState>(
        IEnumerable<IMutation<TState>> mutations,
        TState initialState,
        bool stopOnFirstFailure,
        Func<IMutation<TState>, TState, CancellationToken, Task<MutationResult<TState>>> executeAsync,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<MutationResult<TState>>();
        var allChanges = new ChangeSet();
        var currentState = initialState;

        foreach (var mutation in mutations)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await executeAsync(mutation, currentState, cancellationToken);
            results.Add(result);

            if (result.IsSuccess)
            {
                currentState = result.NewState!;
                foreach (var change in result.Changes.Changes)
                    allChanges.Add(change);

                continue;
            }

            if (stopOnFirstFailure)
                break;
        }

        stopwatch.Stop();
        var allSucceeded = results.Count > 0 && results.All(r => r.IsSuccess);

        return new BatchMutationResult<TState>
        {
            IsSuccess = allSucceeded,
            FinalState = allSucceeded ? currentState : default,
            Results = results,
            AggregatedChanges = allChanges,
            TotalExecutionTime = stopwatch.Elapsed
        };
    }
}
