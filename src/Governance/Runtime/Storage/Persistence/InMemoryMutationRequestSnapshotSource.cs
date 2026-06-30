using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Runtime.Storage.Persistence;

/// <summary>
/// Owns the in-memory governed request collection and its synchronization boundary.
/// </summary>
internal sealed class InMemoryMutationRequestSnapshotSource
{
    private readonly Dictionary<string, MutationRequest> _requests = new();
    private readonly Lock _lock = new();

    /// <summary>
    /// Executes read operation against the in-memory request collection under the storage lock.
    /// </summary>
    /// <typeparam name="T">The result type returned by the read operation.</typeparam>
    /// <param name="read">The read operation to execute.</param>
    /// <returns>The result produced by the read operation.</returns>
    public T Read<T>(Func<IReadOnlyDictionary<string, MutationRequest>, T> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        lock (_lock)
        {
            return read(_requests);
        }
    }

    /// <summary>
    /// Executes write operation against the in-memory request collection under the storage lock.
    /// </summary>
    /// <typeparam name="T">The result type returned by the write operation.</typeparam>
    /// <param name="write">The write operation to execute.</param>
    /// <returns>The result produced by the write operation.</returns>
    public T Write<T>(Func<IDictionary<string, MutationRequest>, T> write)
    {
        ArgumentNullException.ThrowIfNull(write);

        lock (_lock)
        {
            return write(_requests);
        }
    }
}
