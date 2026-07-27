using System.Buffers;
using System.Runtime.CompilerServices;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Interception;
using ModularityKit.Mutator.Abstractions.Policies;

namespace ModularityKit.Mutator.Runtime.Interception;

/// <summary>
/// Pipeline responsible for sequential execution of mutation interceptors.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="InterceptorPipeline"/> manages registration, unregistration, and execution of interceptors
/// implementing <see cref="IMutationInterceptor"/>. Interceptors are sorted by their <c>Order</c> property
/// to ensure deterministic execution order.
/// </para>
/// <para>
/// The pipeline also filters interceptors via <see cref="MutationInterceptorBase.ShouldRun"/>.
/// Method calls are executed asynchronously to integrate with the ModularityKit mutation pipeline.
/// </para>
/// <para>
/// The execution path reads from a lock-free volatile snapshot updated atomically on register/unregister,
/// avoiding any lock acquisition or array allocation on the hot path when no interceptors are registered.
/// </para>
/// </remarks>
internal sealed class InterceptorPipeline : IInterceptorPipeline
{
    private readonly List<IMutationInterceptor> _interceptors = [];
    private readonly Lock _lock = new();

    // Cached immutable snapshot of the registered interceptors. Invalidated (set to null)
    // on every Register/Unregister and lazily rebuilt on next read. `volatile` ensures
    // readers on other threads observe fully published array or null, never a partial write.
    private volatile IMutationInterceptor[]? _snapshotCache = [];

    /// <summary>
    /// Registers new interceptor in the pipeline.
    /// </summary>
    /// <param name="interceptor">The interceptor to register. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is <c>null</c>.</exception>
    /// <remarks>
    /// After adding, the interceptors list is sorted by <c>Order</c> to guarantee deterministic execution order.
    /// The snapshot cache is invalidated so the next read lazily rebuilds it.
    /// </remarks>
    public void Register(IMutationInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        lock (_lock)
        {
            _interceptors.Add(interceptor);
            _interceptors.Sort((a, b) => a.Order.CompareTo(b.Order));
            _snapshotCache = null;
        }
    }

    /// <summary>
    /// Removes an interceptor from the pipeline by its name.
    /// </summary>
    /// <param name="name">The name of the interceptor to remove.</param>
    public void Unregister(string name)
    {
        lock (_lock)
        {
            _interceptors.RemoveAll(i => i.Name == name);
            _snapshotCache = null;
        }
    }

    /// <summary>
    /// Gets snapshot of the currently registered interceptors.
    /// </summary>
    /// <returns>An array of interceptors. Returns <see cref="Array.Empty{T}"/> without taking
    /// the lock when the cached snapshot is still valid and empty.</returns>
    /// <remarks>
    /// <c>volatile</c> on <see cref="_snapshotCache"/> guarantees an acquire fence on every read,
    /// so the calling thread observe fully published array or null — never torn reference.
    /// Because the array is immutable after publication, contents are implicitly safe.
    /// </remarks>
    private IMutationInterceptor[] GetSnapshot()
    {
        var cached = _snapshotCache;
        if (cached is not null)
            return cached;

        lock (_lock)
        {
            // Re check under the lock in case another thread already rebuilt it.
            cached = _snapshotCache;
            if (cached is not null)
                return cached;

            cached = _interceptors.Count == 0
                ? []
                : [.. _interceptors];

            _snapshotCache = cached;
            return cached;
        }
    }

    /// <summary>
    /// Filters interceptors to determine which ones should run for a given mutation intent and context.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation context.</param>
    /// <returns>An array of interceptors that should be executed.</returns>
    /// <remarks>
    /// Uses the cached snapshot. When no interceptors are registered, returns the cached empty array
    /// without any filtering overhead. When all interceptors are applicable, the snapshot is reused
    /// directly without allocating a filtered copy.
    /// A pooled buffer is used during filtering to avoid per-call <c>List&lt;T&gt;</c> allocation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IMutationInterceptor[] GetApplicable(
        MutationIntent intent,
        MutationContext context)
    {
        var snapshot = GetSnapshot();
        if (snapshot.Length == 0)
            return snapshot;

        var pool = ArrayPool<IMutationInterceptor>.Shared;
        var buffer = pool.Rent(snapshot.Length);
        var count = 0;
        var excluded = false;

        for (var i = 0; i < snapshot.Length; i++)
        {
            var t = snapshot[i];
            var shouldRun = t is not MutationInterceptorBase baseInt || baseInt.ShouldRun(intent, context);

            if (shouldRun)
            {
                buffer[count++] = t;
            }
            else
            {
                excluded = true;
            }
        }

        if (!excluded)
        {
            pool.Return(buffer);
            return snapshot;
        }

        var result = new IMutationInterceptor[count];
        Array.Copy(buffer, result, count);
        pool.Return(buffer);
        return result;
    }

    /// <summary>
    /// Executes the given action on all applicable interceptors in the pipeline.
    /// </summary>
    /// <param name="action">The action to execute for each interceptor.</param>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation context.</param>
    private async Task ExecuteAsync(
        Func<IMutationInterceptor, Task> action,
        MutationIntent intent,
        MutationContext context)
    {
        var interceptors = GetApplicable(intent, context);
        foreach (var t in interceptors)
        {
            await action(t);
        }
    }

    /// <summary>
    /// Executes registered interceptors before a mutation is evaluated.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation execution context.</param>
    /// <param name="state">The current mutation state.</param>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task OnBeforeMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        string executionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            interceptor => interceptor.OnBeforeMutationAsync(
                intent,
                context,
                state,
                executionId,
                cancellationToken),
            intent,
            context);

    /// <summary>
    /// Executes registered interceptors after mutation has completed.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation execution context.</param>
    /// <param name="oldState">The state before the mutation.</param>
    /// <param name="newState">The state after the mutation.</param>
    /// <param name="changes">The changes produced by the mutation.</param>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task OnAfterMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object? oldState,
        object? newState,
        ChangeSet changes,
        string executionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            interceptor => interceptor.OnAfterMutationAsync(
                intent,
                context,
                oldState,
                newState,
                changes,
                executionId,
                cancellationToken),
            intent,
            context);

    /// <summary>
    /// Executes registered interceptors when mutation fails.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation execution context.</param>
    /// <param name="state">The current mutation state.</param>
    /// <param name="exception">The exception that caused the mutation to fail.</param>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task OnMutationFailedAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        Exception exception,
        string executionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            interceptor => interceptor.OnMutationFailedAsync(
                intent,
                context,
                state,
                exception,
                executionId,
                cancellationToken),
            intent,
            context);

    /// <summary>
    /// Executes registered interceptors when mutation is blocked by policy.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation execution context.</param>
    /// <param name="state">The current mutation state.</param>
    /// <param name="decision">The policy decision that blocked the mutation.</param>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task OnPolicyBlockedAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        PolicyDecision decision,
        string executionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            interceptor => interceptor.OnPolicyBlockedAsync(
                intent,
                context,
                state,
                decision,
                executionId,
                cancellationToken),
            intent,
            context);
}
