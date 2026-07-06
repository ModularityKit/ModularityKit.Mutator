using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Merges policy decisions using explicit composition rules.
/// </summary>
/// <remarks>
/// The composer is the implementation behind <see cref="PolicyComposition"/>.
/// It evaluates child policies in deterministic order, applies the composition
/// mode semantics, and delegates metadata and modification merging to dedicated
/// helpers so the resulting decision stays auditable.
/// </remarks>
internal static class PolicyDecisionComposer
{
    /// <summary>
    /// Evaluates policy set and returns composed decision.
    /// </summary>
    /// <typeparam name="TState">The state type used by the policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="mode">The composition mode.</param>
    /// <param name="policies">The child policies to evaluate.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A composed policy decision.</returns>
    public static Task<PolicyDecision> ComposeAsync<TState>(
        string compositionName,
        PolicyCompositionMode mode,
        IReadOnlyList<IMutationPolicy<TState>> policies,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        var orderedPolicies = policies
            .OrderByDescending(policy => policy.Priority)
            .ThenBy(policy => policy.Name, StringComparer.Ordinal)
            .ThenBy(policy => policy.GetType().FullName ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        return mode switch
        {
            PolicyCompositionMode.AllOf => ComposeFullDecisionAsync(
                compositionName,
                orderedPolicies,
                mutation,
                state,
                cancellationToken,
                ComposeAllOfDecision),
            PolicyCompositionMode.AnyOf => ComposeFullDecisionAsync(
                compositionName,
                orderedPolicies,
                mutation,
                state,
                cancellationToken,
                ComposeAnyOfDecision),
            PolicyCompositionMode.Priority => ComposePriorityDecisionAsync(
                compositionName,
                orderedPolicies,
                mutation,
                state,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported policy composition mode.")
        };
    }

    /// <summary>
    /// Returns whether decision is decisive in priority mode.
    /// </summary>
    /// <param name="decision">The decision to inspect.</param>
    /// <returns><c>true</c> when the decision should stop priority evaluation.</returns>
    private static bool IsDecisive(PolicyDecision decision)
        => !decision.IsAllowed || decision.Modifications is not null || decision.Requirements is not null;

    /// <summary>
    /// Evaluates all child policies and composes their results using selected strategy.
    /// </summary>
    /// <typeparam name="TState">The state type used by the policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="orderedPolicies">The policies in deterministic evaluation order.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="compose">The composition strategy to apply after evaluation.</param>
    /// <returns>A composed policy decision.</returns>
    private static async Task<PolicyDecision> ComposeFullDecisionAsync<TState>(
        string compositionName,
        IReadOnlyList<IMutationPolicy<TState>> orderedPolicies,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken,
        Func<string, IReadOnlyList<PolicyEvaluation<TState>>, PolicyDecision> compose)
    {
        var evaluations = await EvaluatePoliciesAsync(orderedPolicies, mutation, state, cancellationToken)
            .ConfigureAwait(false);

        return compose(compositionName, evaluations);
    }

    /// <summary>
    /// Evaluates policies in priority order and stops at the first decisive result.
    /// </summary>
    /// <typeparam name="TState">The state type used by the policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="orderedPolicies">The policies in deterministic evaluation order.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A composed policy decision.</returns>
    private static async Task<PolicyDecision> ComposePriorityDecisionAsync<TState>(
        string compositionName,
        IReadOnlyList<IMutationPolicy<TState>> orderedPolicies,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(compositionName);
        ArgumentNullException.ThrowIfNull(orderedPolicies);
        ArgumentNullException.ThrowIfNull(mutation);
        var evaluations = new List<PolicyEvaluation<TState>>(orderedPolicies.Count);

        foreach (var policy in orderedPolicies)
        {
            var decision = await policy.EvaluateAsync(mutation, state, cancellationToken).ConfigureAwait(false);
            var evaluation = new PolicyEvaluation<TState>(policy, decision);
            evaluations.Add(evaluation);

            if (IsDecisive(decision))
            {
                return ComposeDecision(
                    compositionName,
                    PolicyCompositionMode.Priority,
                    evaluations,
                    [evaluation],
                    allow: decision.IsAllowed,
                    winningPolicyNames: [policy.Name],
                    blockingPolicyNames: decision.IsAllowed ? [] : [policy.Name]);
            }
        }

        return ComposeDecision(
            compositionName,
            PolicyCompositionMode.Priority,
            evaluations,
            evaluations,
            allow: true,
            winningPolicyNames: evaluations.Select(evaluation => evaluation.Policy.Name),
            blockingPolicyNames: []);
    }

    /// <summary>
    /// Evaluates all policies without short-circuiting.
    /// </summary>
    /// <typeparam name="TState">The state type used by the policies.</typeparam>
    /// <param name="orderedPolicies">The policies in deterministic evaluation order.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ordered policy evaluations.</returns>
    private static async Task<List<PolicyEvaluation<TState>>> EvaluatePoliciesAsync<TState>(
        IReadOnlyList<IMutationPolicy<TState>> orderedPolicies,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        var evaluations = new List<PolicyEvaluation<TState>>(orderedPolicies.Count);

        foreach (var policy in orderedPolicies)
        {
            var decision = await policy.EvaluateAsync(mutation, state, cancellationToken).ConfigureAwait(false);
            evaluations.Add(new PolicyEvaluation<TState>(policy, decision));
        }

        return evaluations;
    }

    /// <summary>
    /// Composes the final decision for an AllOf policy set.
    /// </summary>
    /// <typeparam name="TState">The state type used by the policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="evaluations">All evaluations performed for the composition.</param>
    /// <returns>A composed policy decision.</returns>
    private static PolicyDecision ComposeAllOfDecision<TState>(
        string compositionName,
        IReadOnlyList<PolicyEvaluation<TState>> evaluations)
    {
        var blockedPolicies = evaluations
            .Where(evaluation => !evaluation.Decision.IsAllowed)
            .Select(evaluation => evaluation.Policy.Name)
            .ToArray();

        var allow = blockedPolicies.Length == 0;

        return ComposeDecision(
            compositionName,
            PolicyCompositionMode.AllOf,
            evaluations,
            evaluations,
            allow,
            winningPolicyNames: allow
                ? evaluations.Select(evaluation => evaluation.Policy.Name)
                : blockedPolicies,
            blockingPolicyNames: blockedPolicies);
    }

    /// <summary>
    /// Composes the final decision for an AnyOf policy set.
    /// </summary>
    /// <typeparam name="TState">The state type used by the policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="evaluations">All evaluations performed for the composition.</param>
    /// <returns>A composed policy decision.</returns>
    private static PolicyDecision ComposeAnyOfDecision<TState>(
        string compositionName,
        IReadOnlyList<PolicyEvaluation<TState>> evaluations)
    {
        var allowedEvaluations = evaluations.Where(evaluation => evaluation.Decision.IsAllowed).ToArray();
        var selectedEvaluations = allowedEvaluations.Length > 0 ? allowedEvaluations : evaluations;
        var allow = allowedEvaluations.Length > 0;

        return ComposeDecision(
            compositionName,
            PolicyCompositionMode.AnyOf,
            evaluations,
            selectedEvaluations,
            allow,
            winningPolicyNames: selectedEvaluations.Select(evaluation => evaluation.Policy.Name),
            blockingPolicyNames: allow
                ? evaluations.Where(evaluation => !evaluation.Decision.IsAllowed).Select(evaluation => evaluation.Policy.Name)
                : evaluations.Select(evaluation => evaluation.Policy.Name));
    }

    /// <summary>
    /// Builds the final composed decision payload.
    /// </summary>
    /// <typeparam name="TState">The state type used by the policies.</typeparam>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="mode">The composition mode.</param>
    /// <param name="allEvaluations">All evaluations performed for the composition.</param>
    /// <param name="selectedEvaluations">The evaluations that contribute to the final result.</param>
    /// <param name="allow">Whether the composition is allowed.</param>
    /// <param name="winningPolicyNames">The policies that contributed to the result.</param>
    /// <param name="blockingPolicyNames">The policies that blocked the result.</param>
    /// <returns>A composed policy decision.</returns>
    private static PolicyDecision ComposeDecision<TState>(
        string compositionName,
        PolicyCompositionMode mode,
        IReadOnlyList<PolicyEvaluation<TState>> allEvaluations,
        IReadOnlyList<PolicyEvaluation<TState>> selectedEvaluations,
        bool allow,
        IEnumerable<string> winningPolicyNames,
        IEnumerable<string> blockingPolicyNames)
    {
        var winningNames = winningPolicyNames.ToArray();
        var blockingNames = blockingPolicyNames.ToArray();
        var selectedDecisions = selectedEvaluations.Select(evaluation => evaluation.Decision).ToArray();
        var severity = selectedDecisions.Max(decision => decision.Severity);
        var metadata = PolicyDecisionMetadataMerger.Merge(
            compositionName,
            mode,
            allEvaluations,
            winningNames,
            blockingNames);
        var requirements = selectedDecisions
            .SelectMany(decision => decision.Requirements ?? [])
            .ToArray();
        var modifications = PolicyDecisionModificationMerger.Merge(compositionName, selectedEvaluations);
        var reason = BuildReason(compositionName, allow, winningNames, blockingNames, selectedDecisions);

        return new PolicyDecision
        {
            IsAllowed = allow,
            PolicyName = compositionName,
            Reason = reason,
            Severity = severity,
            Requirements = requirements,
            Modifications = modifications,
            Metadata = metadata,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Builds reason for composed decision.
    /// </summary>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="allow">Whether the composition is allowed.</param>
    /// <param name="winningNames">The policies that contributed to the result.</param>
    /// <param name="blockingNames">The policies that blocked the result.</param>
    /// <param name="decisions">The selected decisions used to derive the reason.</param>
    /// <returns>The formatted reason string.</returns>
    private static string BuildReason(string compositionName, bool allow,
        IReadOnlyList<string> winningNames,
        IReadOnlyList<string> blockingNames,
        IReadOnlyList<PolicyDecision> decisions) => allow
            ? BuildAllowReason(compositionName, winningNames)
            : BuildBlockReason(compositionName, blockingNames, decisions);

    /// <summary>
    /// Builds the allow reason for composed decision.
    /// </summary>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="winningNames">The policies that contributed to the result.</param>
    /// <returns>The formatted allow reason.</returns>
    private static string BuildAllowReason(string compositionName, IReadOnlyList<string> winningNames)
        => $"Policy composition '{compositionName}' allowed by {string.Join(", ", winningNames)}.";

    /// <summary>
    /// Builds the block reason for composed decision.
    /// </summary>
    /// <param name="compositionName">The composed policy name.</param>
    /// <param name="blockingNames">The policies that blocked the result.</param>
    /// <param name="decisions">The selected decisions used to derive the reason.</param>
    /// <returns>The formatted block reason.</returns>
    private static string BuildBlockReason(
        string compositionName,
        IReadOnlyList<string> blockingNames,
        IReadOnlyList<PolicyDecision> decisions)
    {
        var reasons = decisions
            .Select(decision => decision.Reason)
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return reasons.Length == 0
            ? $"Policy composition '{compositionName}' blocked by {string.Join(", ", blockingNames)}."
            : $"Policy composition '{compositionName}' blocked by {string.Join(", ", blockingNames)}: {string.Join(" | ", reasons)}.";
    }
}
