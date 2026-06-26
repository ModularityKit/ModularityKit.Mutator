using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Metrics;
using ModularityKit.Mutator.Abstractions.Policies;

namespace ModularityKit.Mutator.Abstractions.Results;

/// <summary>
/// Represents the outcome of applying a mutation to a state.
/// Always contains a trace of changes, even if the mutation fails.
/// </summary>
/// <typeparam name="TState">The type of the state being mutated.</typeparam>
public sealed record MutationResult<TState>
{
    /// <summary>
    /// Indicates whether the mutation was successfully applied.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The new state after mutation, if successful; otherwise null.
    /// </summary>
    public TState? NewState { get; init; }

    /// <summary>
    /// The set of changes describing what exactly was modified during the mutation.
    /// Always populated, even on failure.
    /// </summary>
    public ChangeSet Changes { get; init; } = ChangeSet.Empty;

    /// <summary>
    /// Result of validation checks performed during mutation.
    /// </summary>
    public ValidationResult ValidationResult { get; init; } = ValidationResult.Success();

    /// <summary>
    /// Decisions from policy evaluation that influenced the mutation outcome.
    /// </summary>
    public IReadOnlyList<PolicyDecision> PolicyDecisions { get; init; } = [];

    /// <summary>
    /// Side effects produced during the mutation.
    /// </summary>
    public IReadOnlyList<SideEffect> SideEffects { get; init; } = [];

    /// <summary>
    /// Metrics related to the execution of the mutation (e.g., duration, performance counters).
    /// </summary>
    public MutationMetrics Metrics { get; init; } = new();

    /// <summary>
    /// Exception thrown during mutation, if any, for diagnostic purposes.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Timestamp indicating when the mutation completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a successful mutation result.
    /// </summary>
    /// <param name="newState">The new state after the mutation.</param>
    /// <param name="changes">The set of changes applied.</param>
    /// <param name="sideEffects">Optional list of side effects.</param>
    /// <returns>A <see cref="MutationResult{TState}"/> representing success.</returns>
    public static MutationResult<TState> Success(
        TState newState,
        ChangeSet changes,
        IReadOnlyList<SideEffect>? sideEffects = null)
        => new()
        {
            IsSuccess = true,
            NewState = newState,
            Changes = changes,
            SideEffects = sideEffects ?? []
        };

    /// <summary>
    /// Creates a successful mutation result from a single state change.
    /// </summary>
    /// <param name="newState">The new state after the mutation.</param>
    /// <param name="change">The single change applied.</param>
    /// <param name="sideEffects">Optional list of side effects.</param>
    /// <returns>A <see cref="MutationResult{TState}"/> representing success.</returns>
    public static MutationResult<TState> Success(
        TState newState,
        StateChange change,
        IReadOnlyList<SideEffect>? sideEffects = null)
        => Success(newState, ChangeSet.Single(change), sideEffects);

    /// <summary>
    /// Creates a failed mutation result due to validation errors.
    /// </summary>
    /// <param name="validation">The validation result explaining the failure.</param>
    /// <returns>A <see cref="MutationResult{TState}"/> representing failure.</returns>
    public static MutationResult<TState> Failure(ValidationResult validation)
        => new()
        {
            IsSuccess = false,
            ValidationResult = validation
        };

    /// <summary>
    /// Creates a failed mutation result due to policy enforcement.
    /// </summary>
    /// <param name="decision">The policy decision that blocked the mutation.</param>
    /// <returns>A <see cref="MutationResult{TState}"/> representing a policy-blocked mutation.</returns>
    public static MutationResult<TState> PolicyBlocked(PolicyDecision decision)
        => new()
        {
            IsSuccess = false,
            PolicyDecisions = [decision],
            ValidationResult = ValidationResult.WithError(
                "Policy",
                decision.Reason ?? "Blocked by policy")
        };
}
