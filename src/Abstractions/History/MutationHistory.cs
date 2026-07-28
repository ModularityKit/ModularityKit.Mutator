using ModularityKit.Mutator.Abstractions.Changes;

namespace ModularityKit.Mutator.Abstractions.History;

/// <summary>
/// Represents the full mutation history of specific state object.
/// </summary>
/// <remarks>
/// MutationHistory stores chronological sequence of <see cref="MutationHistoryEntry"/> entries.
/// It allows replaying state changes, querying timelines for specific paths, and computing statistics.
/// This is typically used in combination with <see cref="IMutationHistoryStore"/> to persist and retrieve histories.
/// </remarks>
public sealed class MutationHistory
{
    /// <summary>
    /// Unique identifier of the state object.
    /// </summary>
    public string StateId { get; init; } = string.Empty;

    /// <summary>
    /// Type of the state object.
    /// </summary>
    public string StateType { get; init; } = string.Empty;

    /// <summary>
    /// Chronological list of mutation entries.
    /// </summary>
    public IReadOnlyList<MutationHistoryEntry> Entries { get; init; }
        = Array.Empty<MutationHistoryEntry>();

    /// <summary>
    /// Timestamp of the first mutation in the history.
    /// </summary>
    public DateTimeOffset? FirstMutationAt
        => Entries.Count > 0 ? Entries[0].Timestamp : null;

    /// <summary>
    /// Timestamp of the last mutation in the history.
    /// </summary>
    public DateTimeOffset? LastMutationAt
        => Entries.Count > 0 ? Entries[^1].Timestamp : null;

    /// <summary>
    /// Total number of mutations recorded.
    /// </summary>
    public int TotalMutations => Entries.Count;

    /// <summary>
    /// Replays the full mutation history to reconstruct the current state.
    /// </summary>
    /// <typeparam name="TState">Type of the state object.</typeparam>
    /// <param name="initialState">The initial state before any mutations.</param>
    /// <param name="applyChanges">Function that applies a <see cref="ChangeSet"/> to a state instance.</param>
    /// <returns>The reconstructed state after applying all mutations.</returns>
    public TState Replay<TState>(
        TState initialState,
        Func<TState, ChangeSet, TState> applyChanges)
    {
        var current = initialState;
        foreach (var entry in Entries)
        {
            current = applyChanges(current, entry.Changes);
        }
        return current;
    }

    /// <summary>
    /// Replays the mutation history up to a specific timestamp.
    /// </summary>
    /// <typeparam name="TState">Type of the state object.</typeparam>
    /// <param name="initialState">The initial state before any mutations.</param>
    /// <param name="timestamp">Replay mutations only up to this timestamp (inclusive).</param>
    /// <param name="applyChanges">Function that applies a <see cref="ChangeSet"/> to a state instance.</param>
    /// <returns>The reconstructed state as of the given timestamp.</returns>
    public TState ReplayUntil<TState>(
        TState initialState,
        DateTimeOffset timestamp,
        Func<TState, ChangeSet, TState> applyChanges)
    {
        var relevantEntries = Entries.Where(e => e.Timestamp <= timestamp);
        var current = initialState;

        foreach (var entry in relevantEntries)
        {
            current = applyChanges(current, entry.Changes);
        }

        return current;
    }

    /// <summary>
    /// Retrieves a timeline of changes for a specific property path.
    /// </summary>
    /// <param name="path">The property path (e.g., "Email" or "Address.City").</param>
    /// <returns>A chronological sequence of <see cref="StateChangeTimeline"/> entries for that path.</returns>
    public IEnumerable<StateChangeTimeline> GetTimelineForPath(string path)
    {
        return Entries
            .SelectMany(entry => entry.Changes.GetChanges(path)
                .Select(change => new StateChangeTimeline
                {
                    Timestamp = entry.Timestamp,
                    Change = change,
                    ExecutionId = entry.ExecutionId,
                    ActorId = entry.Context.ActorId,
                    Reason = entry.Context.Reason
                }))
            .OrderBy(t => t.Timestamp);
    }

    /// <summary>
    /// Computes summary statistics of the mutation history.
    /// </summary>
    /// <returns>A <see cref="HistoryStatistics"/> instance containing totals, averages, and mutation distribution.</returns>
    public HistoryStatistics GetStatistics()
    {
        return new HistoryStatistics
        {
            TotalMutations = TotalMutations,
            UniqueActors = Entries.Select(e => e.Context.ActorId).Distinct().Count(),
            MutationsByCategory = Entries
                .GroupBy(e => e.Intent.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            AverageChangesPerMutation = Entries.Any()
                ? Entries.Average(e => e.Changes.Count)
                : 0
        };
    }
}
