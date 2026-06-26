using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Abstractions.Engine;

/// <summary>
/// Represents a mutation operation that can be applied to a given state of type <typeparamref name="TState"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IMutation{TState}"/> interface defines the contract for a mutation:
/// what change is being performed (<see cref="Intent"/>), the execution context (<see cref="Context"/>),
/// and the operations to apply, validate, or simulate the mutation.
/// </para>
/// <para>
/// For simpler implementations, <see cref="MutationBase{TState}"/> provides default behavior for
/// validation and simulation.
/// </para>
/// <para>
/// Typical usage involves:
/// <list type="bullet">
/// <item>Creating a mutation instance with a descriptive <see cref="Intent"/>.</item>
/// <item>Validating the mutation against a current state using <see cref="Validate"/>.</item>
/// <item>Applying the mutation via <see cref="Apply"/> to obtain a new state and results.</item>
/// <item>Optionally performing a dry-run simulation using <see cref="Simulate"/>.</item>
/// </list>
/// </para>
/// </remarks>
/// <typeparam name="TState">The type of state the mutation operates on.</typeparam>
public interface IMutation<TState>
{
    /// <summary>
    /// Gets the intent describing this mutation — what change is being performed and why.
    /// </summary>
    MutationIntent Intent { get; }

    /// <summary>
    /// Gets the execution context containing metadata about who, when, why, and how the mutation is executed.
    /// </summary>
    MutationContext Context { get; }

    /// <summary>
    /// Applies the mutation to the given <paramref name="state"/> and returns the result.
    /// </summary>
    /// <param name="state">The current state to mutate.</param>
    /// <returns>A <see cref="MutationResult{TState}"/> containing the new state and any side-effects or logs.</returns>
    MutationResult<TState> Apply(TState state);

    /// <summary>
    /// Validates the mutation without applying it to ensure legality and preconditions.
    /// </summary>
    /// <param name="state">The state against which validation should be performed.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the mutation is valid and any violations.</returns>
    ValidationResult Validate(TState state);

    /// <summary>
    /// Simulates the mutation without changing the actual state (dry-run).
    /// Useful for testing, previewing effects, or computing potential side-effects.
    /// </summary>
    /// <param name="state">The current state to simulate the mutation on.</param>
    /// <returns>A <see cref="MutationResult{TState}"/> reflecting the hypothetical outcome.</returns>
    MutationResult<TState> Simulate(TState state);
}
