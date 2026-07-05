using ModularityKit.Mutator.Abstractions.Exceptions;

namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Merges metadata for composed policy decisions.
/// </summary>
/// <remarks>
/// The merger preserves composition audit metadata and folds child policy
/// metadata into the final decision. Metadata keys are merged deterministically.
/// When two child policies provide the same metadata key with different values,
/// the merge fails with policy composition conflict so ambiguous audit data is
/// never silently overwritten.
/// </remarks>
internal static class PolicyDecisionMetadataMerger
{
    /// <summary>
    /// The metadata key used to store the policy composition mode.
    /// </summary>
    private const string CompositionModeKey = "PolicyComposition.Mode";

    /// <summary>
    /// The metadata key used to store the ordered child policy evaluation summary.
    /// </summary>
    private const string CompositionPoliciesKey = "PolicyComposition.Policies";

    /// <summary>
    /// The metadata key used to store the policies that contributed to the composed result.
    /// </summary>
    private const string CompositionWinningPoliciesKey = "PolicyComposition.WinningPolicies";

    /// <summary>
    /// The metadata key used to store the policies that blocked the composed result.
    /// </summary>
    private const string CompositionBlockingPoliciesKey = "PolicyComposition.BlockingPolicies";

    /// <summary>
    /// Merges composition metadata with metadata produced by child policy decisions.
    /// </summary>
    /// <typeparam name="TState">The state type used by the evaluated policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="mode">The composition mode used to produce the final decision.</param>
    /// <param name="evaluations">The ordered child policy evaluations.</param>
    /// <param name="winningPolicyNames">The policies that contributed to the composed result.</param>
    /// <param name="blockingPolicyNames">The policies that blocked the composed result.</param>
    /// <returns>The merged metadata dictionary for the composed policy decision.</returns>
    /// <exception cref="PolicyCompositionConflictException">
    /// Thrown when multiple child policies provide the same metadata key with different values.
    /// </exception>
    public static IReadOnlyDictionary<string, object> Merge<TState>(
        string compositionName,
        PolicyCompositionMode mode,
        IReadOnlyList<PolicyEvaluation<TState>> evaluations,
        IReadOnlyList<string> winningPolicyNames,
        IReadOnlyList<string> blockingPolicyNames)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [CompositionModeKey] = mode.ToString(),
            [CompositionPoliciesKey] = evaluations
                .Select((evaluation, index) => new
                {
                    index,
                    policy = evaluation.Policy.Name,
                    severity = evaluation.Decision.Severity.ToString(),
                    allowed = evaluation.Decision.IsAllowed
                })
                .ToArray(),
            [CompositionWinningPoliciesKey] = winningPolicyNames.ToArray(),
            [CompositionBlockingPoliciesKey] = blockingPolicyNames.ToArray()
        };

        foreach (var evaluation in evaluations)
        {
            if (evaluation.Decision.Metadata is null)
                continue;

            foreach (var (key, value) in evaluation.Decision.Metadata)
            {
                if (!metadata.TryGetValue(key, out var existing))
                {
                    metadata[key] = value;
                    continue;
                }

                if (Equals(existing, value))
                    continue;

                throw new PolicyCompositionConflictException(
                    compositionName,
                    $"metadata:{key}",
                    [.. evaluations
                        .Where(candidate => candidate.Decision.Metadata is not null && candidate.Decision.Metadata.ContainsKey(key))
                        .Select(candidate => candidate.Policy.Name)
                        .Distinct(StringComparer.Ordinal)]);
            }
        }

        return metadata;
    }
}
