using ModularityKit.Mutator.Abstractions.History;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

/// <summary>
/// Noop history store used to remove history persistence noise from selected benchmark cases.
/// </summary>
internal sealed class NoOpHistoryStore : IMutationHistoryStore
{
    public bool IsEnabled => false;
    /// <summary>
    /// Ignores the supplied history entry.
    /// </summary>
    /// <param name="entry">The history entry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StoreAsync(MutationHistoryEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Returns an empty mutation history for the requested state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty mutation history.</returns>
    public Task<MutationHistory> GetHistoryAsync(string stateId, CancellationToken cancellationToken = default)
        => Task.FromResult(new MutationHistory { StateId = stateId, Entries = [] });

    /// <summary>
    /// Returns an empty mutation history for the requested state and time range.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="from">The lower bound of the time range.</param>
    /// <param name="to">The upper bound of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty mutation history.</returns>
    public Task<MutationHistory> GetHistoryRangeAsync(
        string stateId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new MutationHistory { StateId = stateId, Entries = [] });

    /// <summary>
    /// Returns an empty list of recent mutation history entries.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="count">The requested number of entries.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty list of mutation history entries.</returns>
    public Task<IReadOnlyList<MutationHistoryEntry>> GetRecentAsync(
        string stateId,
        int count,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MutationHistoryEntry>>([]);
}
