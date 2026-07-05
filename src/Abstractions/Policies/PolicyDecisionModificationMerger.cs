using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Exceptions;

namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Merges modification payloads for composed policy decisions.
/// </summary>
/// <remarks>
/// The merger folds child policy modification payloads into single composed
/// modification dictionary. Regular modification keys must either be unique or
/// contain equal values across contributing policies. Side effects are treated
/// specially and are collected into a single side effects collection.
/// </remarks>
internal static class PolicyDecisionModificationMerger
{
    /// <summary>
    /// The modification key used for single side effect.
    /// </summary>
    private const string SideEffectKey = "SideEffect";

    /// <summary>
    /// The modification key used for collection of side effects.
    /// </summary>
    private const string SideEffectsKey = "SideEffects";

    /// <summary>
    /// Merges modification payloads produced by child policy decisions.
    /// </summary>
    /// <typeparam name="TState">The state type used by the evaluated policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="evaluations">The child policy evaluations that contribute modifications.</param>
    /// <returns>
    /// The merged modification dictionary, or <c>null</c> when no modifications were produced.
    /// </returns>
    /// <exception cref="PolicyCompositionConflictException">
    /// Thrown when multiple policies provide the same modification key with different values,
    /// or when the side effects payload has an unsupported shape.
    /// </exception>
    public static IReadOnlyDictionary<string, object>? Merge<TState>(
        string compositionName,
        IReadOnlyList<PolicyEvaluation<TState>> evaluations)
    {
        var merged = new Dictionary<string, object>(StringComparer.Ordinal);
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        var sideEffects = new List<SideEffect>();

        foreach (var evaluation in evaluations)
        {
            if (evaluation.Decision.Modifications is null)
                continue;

            foreach (var (key, value) in evaluation.Decision.Modifications)
            {
                if (key == SideEffectKey)
                {
                    if (value is SideEffect sideEffect)
                    {
                        sideEffects.Add(sideEffect);
                    }

                    continue;
                }

                if (key == SideEffectsKey)
                {
                    if (value is IEnumerable<SideEffect> effects)
                    {
                        sideEffects.AddRange(effects);
                        continue;
                    }

                    throw new PolicyCompositionConflictException(
                        compositionName,
                        SideEffectsKey,
                        [evaluation.Policy.Name]);
                }

                if (!merged.TryGetValue(key, out var existing))
                {
                    merged[key] = value;
                    sources[key] = evaluation.Policy.Name;
                    continue;
                }

                if (Equals(existing, value))
                    continue;

                throw new PolicyCompositionConflictException(
                    compositionName,
                    key,
                    [sources[key], evaluation.Policy.Name]);
            }
        }

        if (sideEffects.Count > 0)
            merged[SideEffectsKey] = sideEffects;

        return merged.Count == 0 ? null : merged;
    }
}
