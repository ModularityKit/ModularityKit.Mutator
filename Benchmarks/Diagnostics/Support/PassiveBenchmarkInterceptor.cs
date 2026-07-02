using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Runtime.Interception;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

/// <summary>
/// Minimal interceptor that participates in the full mutation lifecycle without side effects.
/// </summary>
internal sealed class PassiveBenchmarkInterceptor : MutationInterceptorBase
{
    /// <summary>
    /// Gets the interceptor name used in benchmark scenarios.
    /// </summary>
    public override string Name => nameof(PassiveBenchmarkInterceptor);

    /// <summary>
    /// Handles the pre mutation hook without introducing observable side effects.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation context.</param>
    /// <param name="state">The current state.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public override Task OnBeforeMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        string executionId,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Handles the post mutation hook without introducing observable side effects.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation context.</param>
    /// <param name="oldState">The state before mutation execution.</param>
    /// <param name="newState">The state after mutation execution.</param>
    /// <param name="changes">The generated change set.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public override Task OnAfterMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object? oldState,
        object? newState,
        ChangeSet changes,
        string executionId,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
