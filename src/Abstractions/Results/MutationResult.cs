using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Metrics;
using ModularityKit.Mutator.Abstractions.Policies;

namespace ModularityKit.Mutator.Abstractions.Results;

/// <summary>
/// Represents the outcome of mutation operation for a given state type.
/// </summary>
/// <typeparam name="TState">The type of the state object being mutated.</typeparam>
public readonly record struct MutationResult<TState>
{
    public MutationResult() { }

    /// <summary>
    /// Indicates whether the mutation completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The new state after a successful mutation, or default if unsuccessful.
    /// </summary>
    public TState? NewState { get; init; }

    /// <summary>
    /// The set of changes applied by the mutation.
    /// </summary>
    public ChangeSet Changes { get; init; }

    /// <summary>
    /// Validation result indicating any validation errors or warnings.
    /// </summary>
    public ValidationResult ValidationResult { get; init; }

    /// <summary>
    /// Policy decisions that were made during mutation evaluation.
    /// </summary>
    public IReadOnlyList<PolicyDecision> PolicyDecisions { get; init; } = [];

    /// <summary>
    /// Side effects produced during the mutation.
    /// </summary>
    public IReadOnlyList<SideEffect> SideEffects { get; init; } = [];

    /// <summary>
    /// Metrics collected during the mutation execution.
    /// </summary>
    public MutationMetrics Metrics { get; init; }

    /// <summary>
    /// Exception thrown during the mutation, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Timestamp when the mutation completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>
    /// Creates successful mutation result with the given state and changes.
    /// </summary>
    /// <param name="newState">The new state after mutation.</param>
    /// <param name="changes">The set of changes applied.</param>
    /// <param name="sideEffects">Optional list of side effects.</param>
    /// <returns>A successful <see cref="MutationResult{TState}"/>.</returns>
    public static MutationResult<TState> Success(
        TState newState,
        ChangeSet changes,
        IReadOnlyList<SideEffect>? sideEffects = null)
        => new()
        {
            IsSuccess = true,
            NewState = newState,
            Changes = changes,
            SideEffects = sideEffects ?? [],
            ValidationResult = ValidationResult.Success(),
            Metrics = MutationMetrics.Empty,
            CompletedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Creates successful mutation result with single state change.
    /// </summary>
    /// <param name="newState">The new state after mutation.</param>
    /// <param name="change">The single state change applied.</param>
    /// <param name="sideEffects">Optional list of side effects.</param>
    /// <returns>A successful <see cref="MutationResult{TState}"/>.</returns>
    public static MutationResult<TState> Success(
        TState newState,
        StateChange change,
        IReadOnlyList<SideEffect>? sideEffects = null)
        => Success(newState, ChangeSet.Single(change), sideEffects);

    /// <summary>
    /// Creates failed mutation result with validation errors.
    /// </summary>
    /// <param name="validation">The validation result describing the failure.</param>
    /// <returns>A failed <see cref="MutationResult{TState}"/>.</returns>
    public static MutationResult<TState> Failure(ValidationResult validation)
        => new()
        {
            IsSuccess = false,
            ValidationResult = validation,
            Metrics = MutationMetrics.Empty,
            CompletedAt = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Creates mutation result blocked by policy decision.
    /// </summary>
    /// <param name="decision">The policy decision that blocked the mutation.</param>
    /// <returns>A blocked <see cref="MutationResult{TState}"/>.</returns>
    public static MutationResult<TState> PolicyBlocked(PolicyDecision decision)
        => new()
        {
            IsSuccess = false,
            PolicyDecisions = [decision],
            ValidationResult = ValidationResult.WithError(
                "Policy",
                decision.Reason ?? "Blocked by policy"),
            Metrics = MutationMetrics.Empty,
            CompletedAt = DateTimeOffset.UtcNow
        };
}
