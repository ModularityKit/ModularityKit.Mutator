using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Represents one composed policy built from multiple child policies.
/// </summary>
/// <remarks>
/// The type is intentionally internal because callers are expected to use the
/// factory methods on <see cref="PolicyComposition"/> instead of constructing
/// composed policies directly. That keeps the composition surface explicit and
/// allows the implementation to enforce validation and ordering rules in one
/// place.
/// </remarks>
internal sealed class ComposedMutationPolicy<TState> : IMutationPolicy<TState>
{
    private readonly IReadOnlyList<IMutationPolicy<TState>> _policies;

    /// <summary>
    /// Creates a composed policy from a validated set of child policies.
    /// </summary>
    /// <param name="name">The composed policy name.</param>
    /// <param name="priority">The priority of the composed policy.</param>
    /// <param name="description">An optional human-readable description.</param>
    /// <param name="mode">The composition mode that controls decision merging.</param>
    /// <param name="policies">The child policies to evaluate.</param>
    public ComposedMutationPolicy(
        string name,
        int priority,
        string? description,
        PolicyCompositionMode mode,
        IEnumerable<IMutationPolicy<TState>> policies)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A composed policy name is required.", nameof(name));

        ArgumentNullException.ThrowIfNull(policies);

        Name = name;
        Priority = priority;
        Description = description;
        Mode = mode;
        _policies = ValidatePolicies(policies);
    }

    public string Name { get; }

    public int Priority { get; }

    public string? Description { get; }

    private PolicyCompositionMode Mode { get; }

    public PolicyDecision Evaluate(IMutation<TState> mutation, TState state)
        => EvaluateAsync(mutation, state, CancellationToken.None).GetAwaiter().GetResult();

    public Task<PolicyDecision> EvaluateAsync(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        return PolicyDecisionComposer.ComposeAsync(
            Name,
            Mode,
            _policies,
            mutation,
            state,
            cancellationToken);
    }

    /// <summary>
    /// Validates that the composed policy contains at least one non-null child policy.
    /// </summary>
    /// <param name="policies">The candidate child policies.</param>
    /// <returns>The validated list of child policies.</returns>
    private static IReadOnlyList<IMutationPolicy<TState>> ValidatePolicies(IEnumerable<IMutationPolicy<TState>> policies)
    {
        var validated = new List<IMutationPolicy<TState>>();
        var index = 0;

        foreach (var policy in policies)
        {
            if (policy is null)
                throw new ArgumentException($"Child policy at index {index} is null.", nameof(policies));

            validated.Add(policy);
            index++;
        }

        if (validated.Count == 0)
            throw new ArgumentException("At least one child policy is required.", nameof(policies));

        return validated;
    }
}
