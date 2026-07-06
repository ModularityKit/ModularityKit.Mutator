using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Abstractions.Policies;

namespace ModularityKit.Mutator.Runtime.Internal.Evaluation;

/// <summary>
/// Evaluates registered mutation policies in runtime priority order.
/// </summary>
/// <remarks>
/// The evaluator resolves policies from the registry, applies deterministic
/// priority ordering, invokes each policy with optional timeout handling, and
/// returns the first blocking or modifying decision.
/// </remarks>
internal sealed class MutationPolicyEvaluator(
    IPolicyRegistry policyRegistry,
    MutationEngineOptions options)
{
    private readonly IPolicyRegistry _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
    private readonly MutationEngineOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Evaluates all registered policies for the supplied mutation and state.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel policy evaluation.</param>
    /// <returns>
    /// The first blocking or modifying <see cref="PolicyDecision"/>, or an allow decision when all policies pass.
    /// </returns>
    public async Task<PolicyDecision> EvaluateAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        var policies = _policyRegistry.GetPolicies<TState>();

        foreach (var policy in policies
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .ThenBy(p => p.GetType().FullName ?? string.Empty, StringComparer.Ordinal))
        {
            var decision = await EvaluatePolicyAsync(
                policy,
                mutation,
                state,
                cancellationToken).ConfigureAwait(false);

            if (!decision.IsAllowed || decision.Modifications != null)
                return decision;
        }

        return PolicyDecision.Allow();
    }

    /// <summary>
    /// Evaluates a single policy with optional runtime timeout handling.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the policy.</typeparam>
    /// <param name="policy">The policy to evaluate.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel policy evaluation.</param>
    /// <returns>The decision produced by the policy.</returns>
    /// <exception cref="PolicyEvaluationTimeoutException">
    /// Thrown when policy evaluation exceeds the configured timeout.
    /// </exception>
    /// <exception cref="PolicyEvaluationException">
    /// Thrown when the policy evaluation fails with a non-cancellation exception.
    /// </exception>
    private async Task<PolicyDecision> EvaluatePolicyAsync<TState>(
        IMutationPolicy<TState> policy,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        if (!_options.PolicyEvaluationTimeout.HasValue)
            return await InvokePolicyAsync(policy, mutation, state, cancellationToken).ConfigureAwait(false);

        using var timeoutSource = new CancellationTokenSource(_options.PolicyEvaluationTimeout.Value);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            return await policy.EvaluateAsync(mutation, state, linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            throw new PolicyEvaluationTimeoutException(policy.Name, _options.PolicyEvaluationTimeout.Value);
        }
        catch (Exception ex)
        {
            throw new PolicyEvaluationException(
                policy.Name,
                $"Policy '{policy.Name}' evaluation failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Invokes a policy and normalizes unexpected failures into policy evaluation exceptions.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the policy.</typeparam>
    /// <param name="policy">The policy to invoke.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel policy evaluation.</param>
    /// <returns>The decision produced by the policy.</returns>
    /// <exception cref="PolicyEvaluationException">
    /// Thrown when the policy evaluation fails with a non-cancellation exception.
    /// </exception>
    private static async Task<PolicyDecision> InvokePolicyAsync<TState>(
        IMutationPolicy<TState> policy,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        try
        {
            return await policy.EvaluateAsync(mutation, state, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PolicyEvaluationException(
                policy.Name,
                $"Policy '{policy.Name}' evaluation failed: {ex.Message}",
                ex);
        }
    }
}
