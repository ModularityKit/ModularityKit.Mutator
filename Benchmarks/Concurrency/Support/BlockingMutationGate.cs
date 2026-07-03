namespace ModularityKit.Mutator.Benchmarks.Concurrency.Support;

/// <summary>
/// Coordinates blocked benchmark mutations and tracks observed entry counts.
/// </summary>
internal sealed class BlockingMutationGate : IDisposable
{
    private readonly ManualResetEventSlim _release = new(false);
    private int _entered;

    /// <summary>
    /// Gets the number of mutations that have entered the blocking region.
    /// </summary>
    public int EnteredCount => Volatile.Read(ref _entered);

    /// <summary>
    /// Waits until the expected number of mutations have entered the gate.
    /// </summary>
    /// <param name="expectedEntries">Number of entries required before returning success.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <returns><see langword="true"/> when the expected number of entries arrived before timeout; otherwise <see langword="false"/>.</returns>
    public bool WaitForEntries(int expectedEntries, TimeSpan timeout)
        => SpinWait.SpinUntil(() => Volatile.Read(ref _entered) >= expectedEntries, timeout);

    /// <summary>
    /// Enters the blocking region and waits until released.
    /// </summary>
    public void Enter()
    {
        Interlocked.Increment(ref _entered);
        _release.Wait();
    }

    /// <summary>
    /// Releases all blocked benchmark mutations.
    /// </summary>
    public void Release() => _release.Set();

    /// <summary>
    /// Disposes the underlying synchronization primitive.
    /// </summary>
    public void Dispose() => _release.Dispose();
}
