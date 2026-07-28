using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;

namespace ModularityKit.Mutator.Abstractions.Audit;

/// <summary>
/// Represents single audit record for a mutation operation.
/// Captures the intent, context, changes, policy decisions, and metadata of the mutation.
/// </summary>
/// <remarks>
/// Mutation audit entries are used for compliance, traceability, debugging, and monitoring.
/// Each entry is immutable once created. Typical consumers include auditors, logging systems,
/// or analytics pipelines.
/// </remarks>
public readonly record struct MutationAuditEntry
{
    /// <summary>
    /// Unique execution identifier for this mutation.
    /// </summary>
    public string ExecutionId { get; init; }

    /// <summary>
    /// Identifier of the state object that was mutated.
    /// </summary>
    public string? StateId { get; init; }

    /// <summary>
    /// Type of the state object.
    /// </summary>
    public string? StateType { get; init; }

    /// <summary>
    /// The intent describing what the mutation is trying to achieve.
    /// </summary>
    public MutationIntent MutationIntent { get; init; }

    /// <summary>
    /// Context of the mutation (e.g., correlation data, user context).
    /// </summary>
    public MutationContext Context { get; init; }

    /// <summary>
    /// Changes applied by the mutation.
    /// </summary>
    public ChangeSet Changes { get; init; }

    /// <summary>
    /// Indicates whether the mutation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the mutation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Decisions made by policies during the mutation evaluation.
    /// </summary>
    public IReadOnlyList<PolicyDecision> PolicyDecisions { get; init; }

    /// <summary>
    /// Side effects produced during the mutation.
    /// </summary>
    public IReadOnlyList<SideEffect> SideEffects { get; init; }

    /// <summary>
    /// Timestamp when the mutation started.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Duration of the mutation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// IP address of the client or source that initiated the mutation.
    /// </summary>
    public string? SourceIpAddress { get; init; }

    /// <summary>
    /// User agent of the client or service that initiated the mutation.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Additional metadata for auditing, tracing, or custom extensions.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
