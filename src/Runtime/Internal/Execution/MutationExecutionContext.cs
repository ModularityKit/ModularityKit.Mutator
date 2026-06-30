using System.Diagnostics;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Metrics;

namespace ModularityKit.Mutator.Runtime.Internal.Execution;

/// <summary>
/// Carries shared execution state across the runtime mutation pipeline.
/// </summary>
/// <typeparam name="TState">The state type handled by the mutation.</typeparam>
internal sealed record MutationExecutionContext<TState>
{
    /// <summary>
    /// The mutation being executed.
    /// </summary>
    public IMutation<TState> Mutation { get; init; } = null!;

    /// <summary>
    /// The current state snapshot being mutated.
    /// </summary>
    public TState State { get; init; } = default!;

    /// <summary>
    /// The unique identifier for this execution run.
    /// </summary>
    public string ExecutionId { get; init; } = string.Empty;

    /// <summary>
    /// The shared stopwatch tracking total execution time.
    /// </summary>
    public Stopwatch Stopwatch { get; init; } = null!;

    /// <summary>
    /// The optional metrics scope for detailed runtime metrics.
    /// </summary>
    public IMetricsScope? MetricsScope { get; init; }

    /// <summary>
    /// The cancellation token for the current execution.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
