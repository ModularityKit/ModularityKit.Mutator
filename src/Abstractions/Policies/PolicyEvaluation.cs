using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Captures one evaluated policy and its decision.
/// </summary>
/// <typeparam name="TState">The state type used by the evaluated policy.</typeparam>
/// <param name="Policy">The policy that was evaluated.</param>
/// <param name="Decision">The decision produced by the policy.</param>
internal readonly record struct PolicyEvaluation<TState>(
    IMutationPolicy<TState> Policy,
    PolicyDecision Decision);
