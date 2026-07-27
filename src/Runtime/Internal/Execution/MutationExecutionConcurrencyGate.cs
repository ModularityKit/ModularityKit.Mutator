using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ModularityKit.Mutator.Runtime.Internal.Execution;

/// <summary>
/// Coordinates concurrent mutation execution using global concurrency limit
/// and per state serialization.
/// </summary>
/// <remarks>
/// When state identifier is provided, an additional per state gate ensures that
/// mutations targeting the same state are executed sequentially, while mutations
/// targeting different states may execute concurrently up to the global limit.
/// </remarks>
/// <param name="maxConcurrentMutations">The maximum number of mutations that may execute concurrently across all states. </param>
internal sealed class MutationExecutionConcurrencyGate(
    int maxConcurrentMutations)
{
    private readonly SemaphoreSlim _globalGate =
        new(maxConcurrentMutations, maxConcurrentMutations);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _stateGates =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Enters the concurrency gate for mutation execution.
    /// </summary>
    /// <param name="stateId">The identifier of the state being mutated.</param>
    /// <param name="cancellationToken">Token that can be used to cancel waiting for the required concurrency gates. </param>
    /// <returns>An asynchronous lease that releases the acquired concurrency gates when disposed. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<Lease> EnterAsync(
        string? stateId,
        CancellationToken cancellationToken)
    {
        if (stateId is null)
            return EnterGlobalOnlyAsync(cancellationToken);

        return EnterWithStateAsync(stateId, cancellationToken);
    }

    /// <summary>
    /// Attempts to acquire the global concurrency gate without asynchronous waiting
    /// when capacity is immediately available.
    /// </summary>
    private ValueTask<Lease> EnterGlobalOnlyAsync(
        CancellationToken cancellationToken)
    {
        if (_globalGate.Wait(0, cancellationToken))
        {
            return new ValueTask<Lease>(
                new Lease(_globalGate, null));
        }

        return SlowEnterGlobalOnlyAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously waits for the global concurrency gate when it could not be
    /// acquired immediately.
    /// </summary>
    private async ValueTask<Lease> SlowEnterGlobalOnlyAsync(
        CancellationToken cancellationToken)
    {
        await _globalGate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return new Lease(_globalGate, null);
    }

    /// <summary>
    /// Attempts to acquire both the global concurrency gate and the gate associated
    /// with the specified state.
    /// </summary>
    /// <remarks>
    /// The global gate is always acquired before the per state gate to maintain
    /// consistent acquisition order and avoid lock order inversion.
    /// </remarks>
    private ValueTask<Lease> EnterWithStateAsync(
        string stateId,
        CancellationToken cancellationToken)
    {
        if (_stateGates.TryGetValue(stateId, out var existing))
        {
            if (_globalGate.Wait(0, cancellationToken))
            {
                if (existing.Wait(0, cancellationToken))
                {
                    return new ValueTask<Lease>(
                        new Lease(_globalGate, existing));
                }

                _globalGate.Release();
            }
        }

        return SlowEnterWithStateAsync(
            stateId,
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously acquires the global and per state gates when the fast path
    /// could not acquire them immediately.
    /// </summary>
    private async ValueTask<Lease> SlowEnterWithStateAsync(
        string stateId,
        CancellationToken cancellationToken)
    {
        await _globalGate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var stateGate = _stateGates.GetOrAdd(
                stateId,
                static _ => new SemaphoreSlim(1, 1));

            await stateGate
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Lease(
                _globalGate,
                stateGate);
        }
        catch
        {
            _globalGate.Release();
            throw;
        }
    }

    /// <summary>
    /// Represents an acquired concurrency lease that releases the associated gates
    /// when disposed.
    /// </summary>
    internal readonly struct Lease(
        SemaphoreSlim globalGate,
        SemaphoreSlim? stateGate) : IAsyncDisposable
    {
        /// <summary>
        /// Releases the per state gate, when present, followed by the global gate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask DisposeAsync()
        {
            stateGate?.Release();
            globalGate.Release();

            return ValueTask.CompletedTask;
        }
    }
}