using ModularityKit.Mutator.Abstractions.Exceptions;

namespace ModularityKit.Mutator.Abstractions;

/// <summary>
/// Configuration options controlling the behavior of the mutation engine.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MutationEngineOptions"/> defines execution semantics such as validation strategy,
/// timeout handling, batching behavior, and performance characteristics.
/// </para>
/// <para>
/// These options are typically configured once per engine instance and treated as immutable
/// during execution.
/// </para>
/// </remarks>
public sealed class MutationEngineOptions
{
    /// <summary>
    /// Determines whether mutations should always be validated,
    /// even when executed in Commit mode.
    /// </summary>
    /// <remarks>
    /// When enabled, all validation rules are enforced regardless of execution mode.
    /// Disabling this may improve performance but can allow invalid mutations to execute.
    /// </remarks>
    public bool AlwaysValidate { get; set; } = true;

    /// <summary>
    /// The maximum allowed execution time for a single mutation.
    /// </summary>
    /// <remarks>
    /// When specified, exceeding this duration results in an
    /// <see cref="ExecutionTimeoutException"/>.
    /// </remarks>
    public TimeSpan? ExecutionTimeout { get; set; }

    /// <summary>
    /// The maximum allowed evaluation time for a single policy.
    /// </summary>
    /// <remarks>
    /// When specified, each registered policy evaluation gets its own timeout window.
    /// This is primarily intended for async policies that call external identity, ticketing,
    /// quota, or compliance systems.
    /// </remarks>
    public TimeSpan? PolicyEvaluationTimeout { get; set; }

    /// <summary>
    /// Indicates whether batch execution should stop after the first failure.
    /// </summary>
    /// <remarks>
    /// When enabled, subsequent mutations in the batch will not be executed
    /// once a failure occurs.
    /// </remarks>
    public bool StopBatchOnFirstFailure { get; set; } = true;

    /// <summary>
    /// Enables collection of detailed execution metrics.
    /// </summary>
    /// <remarks>
    /// Detailed metrics provide deep observability but may have measurable
    /// performance impact in high-throughput scenarios.
    /// </remarks>
    public bool EnableDetailedMetrics { get; set; } = false;

    /// <summary>
    /// The maximum number of mutations that may be executed concurrently by the core runtime.
    /// </summary>
    /// <remarks>
    /// This setting limits concurrent core execution across the engine.
    /// Mutations that carry the same <see cref="Context.MutationContext.StateId"/>
    /// are serialized so shared-state workloads remain deterministic.
    /// Batch execution remains ordered; the limit applies to each batch step as it
    /// passes through the runtime.
    /// </remarks>
    public int MaxConcurrentMutations { get; set; } = 10;

    /// <summary>
    /// Default engine configuration with balanced safety and performance.
    /// </summary>
    public static MutationEngineOptions Default => new();

    /// <summary>
    /// Strict configuration emphasizing correctness, validation, and observability.
    /// </summary>
    public static MutationEngineOptions Strict => new()
    {
        AlwaysValidate = true,
        StopBatchOnFirstFailure = true,
        EnableDetailedMetrics = true
    };

    /// <summary>
    /// Performance oriented configuration minimizing overhead.
    /// </summary>
    /// <remarks>
    /// Intended for trusted environments where validation and detailed metrics
    /// can be safely reduced.
    /// </remarks>
    public static MutationEngineOptions Performance => new()
    {
        AlwaysValidate = false,
        EnableDetailedMetrics = false
    };
}
