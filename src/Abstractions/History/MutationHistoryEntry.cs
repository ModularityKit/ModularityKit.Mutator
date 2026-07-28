using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;

namespace ModularityKit.Mutator.Abstractions.History;

/// <summary>
/// Represents a single mutation event in the history of a state object.
/// </summary>
/// <remarks>
/// Each <see cref="MutationHistoryEntry"/> captures the details of a single mutation,
/// including its intent, context, state changes, side effects, execution timing, and integrity hashes.
/// This type is used by <see cref="MutationHistory"/> to store a chronological sequence of mutations.
/// </remarks>
public readonly record struct MutationHistoryEntry
{
    /// <summary>
    /// Unique identifier for the execution of this mutation.
    /// </summary>
    public string ExecutionId { get; init; }

    /// <summary>
    /// Identifier of the state this mutation was applied to.
    /// </summary>
    public string StateId { get; init; }

    /// <summary>
    /// The intent behind the mutation.
    /// </summary>
    public MutationIntent Intent { get; init; }

    /// <summary>
    /// Contextual information about the mutation execution.
    /// </summary>
    public MutationContext Context { get; init; }

    /// <summary>
    /// Set of changes applied by this mutation.
    /// </summary>
    public ChangeSet Changes { get; init; }

    /// <summary>
    /// Side effects produced by this mutation.
    /// </summary>
    public IReadOnlyList<SideEffect> SideEffects { get; init; }

    /// <summary>
    /// Timestamp indicating when the mutation occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Duration of the mutation execution.
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Hash of the state before the mutation (used for integrity checks).
    /// </summary>
    public string? PreviousStateHash { get; init; }

    /// <summary>
    /// Hash of the state after the mutation (used for integrity checks).
    /// </summary>
    public string? NewStateHash { get; init; }
}
