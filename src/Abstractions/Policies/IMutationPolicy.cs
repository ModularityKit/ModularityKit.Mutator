using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Represents policy that decides whether a mutation can be applied.
/// Policies are FIRST CLASS governance mechanism in the mutation framework.
/// </summary>
/// <typeparam name="TState">Type of the state the mutation operates on.</typeparam>
public interface IMutationPolicy<TState>
{
    /// <summary>
    /// The name of the policy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Priority of the policy evaluation.
    /// Higher values are evaluated earlier.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Optional description of the policy.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Evaluates whether the given mutation is allowed on the current state.
    /// </summary>
    /// <param name="mutation">The mutation to evaluate.</param>
    /// <param name="state">The current state before applying the mutation.</param>
    /// <returns>A <see cref="PolicyDecision"/> representing the result of the evaluation.</returns>
    PolicyDecision Evaluate(IMutation<TState> mutation, TState state)
        => throw new NotSupportedException("This policy does not implement synchronous evaluation.");

    /// <summary>
    /// Evaluates whether the given mutation is allowed on the current state asynchronously.
    /// </summary>
    /// <param name="mutation">The mutation to evaluate.</param>
    /// <param name="state">The current state before applying the mutation.</param>
    /// <param name="cancellationToken">Token used to cancel the policy evaluation.</param>
    /// <returns>A <see cref="PolicyDecision"/> representing the result of the evaluation.</returns>
    Task<PolicyDecision> EvaluateAsync(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Evaluate(mutation, state));
}
