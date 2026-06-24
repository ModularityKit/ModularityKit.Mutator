using System.Collections.Concurrent;

namespace ModularityKit.Mutator.Runtime.Internal;

/// <summary>
/// Coordinates core mutation execution concurrency across the engine.
/// </summary>
internal sealed class MutationExecutionConcurrencyGate(int maxConcurrentMutations)
{
    private readonly SemaphoreSlim _globalGate = new(maxConcurrentMutations, maxConcurrentMutations);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _stateGates = new(StringComparer.Ordinal);

    public async ValueTask<Lease> EnterAsync(string? stateId, CancellationToken cancellationToken)
    {
        await _globalGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        var stateGate = default(SemaphoreSlim);

        try
        {
            if (!string.IsNullOrWhiteSpace(stateId))
            {
                stateGate = _stateGates.GetOrAdd(stateId, static _ => new SemaphoreSlim(1, 1));
                await stateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            return new Lease(_globalGate, stateGate);
        }
        catch
        {
            _globalGate.Release();
            throw;
        }
    }

    /// <summary>
    /// Represents an acquired execution slot.
    /// </summary>
    internal readonly struct Lease : IAsyncDisposable
    {
        private readonly SemaphoreSlim _globalGate;
        private readonly SemaphoreSlim? _stateGate;

        public Lease(SemaphoreSlim globalGate, SemaphoreSlim? stateGate)
        {
            _globalGate = globalGate;
            _stateGate = stateGate;
        }

        public ValueTask DisposeAsync()
        {
            _stateGate?.Release();
            _globalGate.Release();
            return ValueTask.CompletedTask;
        }
    }
}
