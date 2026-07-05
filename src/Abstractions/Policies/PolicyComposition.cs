namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Factory methods for composing multiple mutation policies into one deterministic policy.
/// </summary>
public static class PolicyComposition
{
    /// <summary>
    /// Creates composed policy that requires every child policy to allow the mutation.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the composed policy.</typeparam>
    /// <param name="name">The composed policy name.</param>
    /// <param name="policies">The child policies to evaluate.</param>
    /// <param name="priority">The priority of the composed policy.</param>
    /// <param name="description">An optional human readable description.</param>
    /// <returns>A composed policy that merges all child decisions.</returns>
    public static IMutationPolicy<TState> AllOf<TState>(
        string name,
        IEnumerable<IMutationPolicy<TState>> policies,
        int priority = 0,
        string? description = null)
        => new ComposedMutationPolicy<TState>(
            name,
            priority,
            description,
            PolicyCompositionMode.AllOf,
            policies);

    /// <summary>
    /// Creates composed policy that succeeds when at least one child policy allows the mutation.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the composed policy.</typeparam>
    /// <param name="name">The composed policy name.</param>
    /// <param name="policies">The child policies to evaluate.</param>
    /// <param name="priority">The priority of the composed policy.</param>
    /// <param name="description">An optional human readable description.</param>
    /// <returns>A composed policy that selects the allowed branch when one exists.</returns>
    public static IMutationPolicy<TState> AnyOf<TState>(
        string name,
        IEnumerable<IMutationPolicy<TState>> policies,
        int priority = 0,
        string? description = null)
        => new ComposedMutationPolicy<TState>(
            name,
            priority,
            description,
            PolicyCompositionMode.AnyOf,
            policies);

    /// <summary>
    /// Creates a composed policy that evaluates children in deterministic priority order and
    /// returns the first decisive result.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the composed policy.</typeparam>
    /// <param name="name">The composed policy name.</param>
    /// <param name="policies">The child policies to evaluate.</param>
    /// <param name="priority">The priority of the composed policy.</param>
    /// <param name="description">An optional human readable description.</param>
    /// <returns>A composed policy that stops at the first decisive child decision.</returns>
    public static IMutationPolicy<TState> Priority<TState>(
        string name,
        IEnumerable<IMutationPolicy<TState>> policies,
        int priority = 0,
        string? description = null)
        => new ComposedMutationPolicy<TState>(
            name,
            priority,
            description,
            PolicyCompositionMode.Priority,
            policies);
}
