namespace ModularityKit.Mutator.Abstractions.History;

/// <summary>
/// Represents store for persisting and retrieving mutation history entries.
/// </summary>
/// <remarks>
/// Mutation history stores provide durable records of all mutations applied to a given state object.
/// This interface allows storing, querying by state ID, retrieving recent mutations, or querying a time range.
/// Typical implementations include database-backed stores, event-sourced logs, or in-memory caches for testing.
/// </remarks>
/// <example>
/// <code>
/// var historyStore: IMutationHistoryStore = new SqlMutationHistoryStore(connectionString);
/// 
/// // Storing a history entry
/// await historyStore.StoreAsync(historyEntry);
/// 
/// // Retrieving full history
/// var fullHistory = await historyStore.GetHistoryAsync("user-123");
/// 
/// // Retrieving recent 10 mutations
/// var recent = await historyStore.GetRecentAsync("user-123", 10);
/// 
/// // Retrieving history in a specific time range
/// var range = await historyStore.GetHistoryRangeAsync("user-123", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);
/// </code>
/// </example>
public interface IMutationHistoryStore
{
    /// <summary>
    /// When <see langword="false" />, the runtime may skip creating history entries for this store.
    /// </summary>
    bool IsEnabled => true;

    /// <summary>
    /// Persists a mutation history entry.
    /// </summary>
    /// <param name="entry">The mutation history entry to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StoreAsync(
        MutationHistoryEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete mutation history for the specified state ID.
    /// </summary>
    /// <param name="stateId">The unique identifier of the state object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="MutationHistory"/> containing all entries.</returns>
    Task<MutationHistory> GetHistoryAsync(
        string stateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves mutation history for a given state ID within a specific time range.
    /// </summary>
    /// <param name="stateId">The unique identifier of the state object.</param>
    /// <param name="from">Start of the time range (inclusive).</param>
    /// <param name="to">End of the time range (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="MutationHistory"/> containing entries within the specified range.</returns>
    Task<MutationHistory> GetHistoryRangeAsync(
        string stateId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent N mutation entries for a given state ID.
    /// </summary>
    /// <param name="stateId">The unique identifier of the state object.</param>
    /// <param name="count">Number of recent entries to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of the most recent mutation history entries.</returns>
    Task<IReadOnlyList<MutationHistoryEntry>> GetRecentAsync(
        string stateId,
        int count,
        CancellationToken cancellationToken = default);
}
