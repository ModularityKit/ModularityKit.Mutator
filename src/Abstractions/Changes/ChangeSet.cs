namespace ModularityKit.Mutator.Abstractions.Changes;

/// <summary>
/// Represents collection of state changes introduced by a mutation.
/// This is primary feature of mutation and not an optional addition.
/// </summary>
public sealed class ChangeSet
{
    private static readonly ChangeSet _empty = new();
    private readonly List<StateChange> _changes = [];

    /// <summary>
    /// Gets the list of all recorded state changes.
    /// </summary>
    public IReadOnlyList<StateChange> Changes => _changes;

    /// <summary>
    /// Indicates whether there are any recorded changes.
    /// </summary>
    public bool HasChanges => _changes.Count > 0;

    /// <summary>
    /// Gets the total number of changes in the set.
    /// </summary>
    public int Count => _changes.Count;

    /// <summary>
    /// Timestamp of the changeset creation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional checksum of the changeset for integrity verification.
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Adds new state change to the changeset.
    /// </summary>
    /// <param name="change">The state change to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="change"/> is null.</exception>
    public void Add(StateChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        _changes.Add(change);
    }

    /// <summary>
    /// Retrieves all changes corresponding to specific path.
    /// </summary>
    /// <param name="path">The path to filter changes.</param>
    /// <returns>Enumerable of <see cref="StateChange"/> matching the path.</returns>
    public IEnumerable<StateChange> GetChanges(string path)
        => _changes.Where(c => c.Path == path);

    /// <summary>
    /// Determines whether specific path has been changed.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path has been changed; otherwise, false.</returns>
    public bool IsChanged(string path)
        => _changes.Any(c => c.Path == path);

    /// <summary>
    /// Retrieves all unique paths that have undergone changes.
    /// </summary>
    /// <returns>Enumerable of unique changed paths.</returns>
    public IEnumerable<string> GetChangedPaths()
        => _changes.Select(c => c.Path).Distinct();

    /// <summary>
    /// Returns an empty changeset.
    /// </summary>
    public static ChangeSet Empty => _empty;

    /// <summary>
    /// Creates changeset containing single state change.
    /// </summary>
    /// <param name="change">The state change to include.</param>
    /// <returns>A <see cref="ChangeSet"/> containing the single change.</returns>
    public static ChangeSet Single(StateChange change)
    {
        var set = new ChangeSet();
        set.Add(change);
        return set;
    }

    /// <summary>
    /// Creates changeset from multiple state changes.
    /// </summary>
    /// <param name="changes">Array of state changes to include.</param>
    /// <returns>A <see cref="ChangeSet"/> containing all provided changes.</returns>
    public static ChangeSet FromChanges(params StateChange[] changes)
    {
        var set = new ChangeSet();
        foreach (var change in changes)
            set.Add(change);
        return set;
    }
}
