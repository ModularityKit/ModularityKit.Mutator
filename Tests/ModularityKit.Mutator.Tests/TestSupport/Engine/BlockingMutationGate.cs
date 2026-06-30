namespace ModularityKit.Mutator.Tests.TestSupport.Engine;

/// <summary>
/// Coordinates blocking test mutations and tracks observed concurrency.
/// </summary>
internal sealed class BlockingMutationGate : IDisposable
{
    private readonly ManualResetEventSlim _release = new(false);
    private int _entered;
    private int _active;
    private int _peak;

    public int PeakConcurrency => Volatile.Read(ref _peak);

    /// <summary>
    /// Waits until the expected number of mutations have entered the gate.
    /// </summary>
    /// <param name="expectedEntries">Number of entries required before returning success.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <returns><see langword="true"/> when the expected number of entries arrived before timeout; otherwise <see langword="false"/>.</returns>
    public async Task<bool> WaitForEntriesAsync(int expectedEntries, TimeSpan timeout)
    {
        var started = DateTimeOffset.UtcNow;

        while (Volatile.Read(ref _entered) < expectedEntries)
        {
            if (DateTimeOffset.UtcNow - started > timeout)
                return false;

            await Task.Delay(10);
        }

        return true;
    }

    public void Enter()
    {
        Interlocked.Increment(ref _entered);
        var active = Interlocked.Increment(ref _active);

        while (true)
        {
            var peak = Volatile.Read(ref _peak);
            if (active <= peak || Interlocked.CompareExchange(ref _peak, active, peak) == peak)
                break;
        }

        _release.Wait();
        Interlocked.Decrement(ref _active);
    }

    public void Release() => _release.Set();

    public void Dispose() => _release.Dispose();
}
