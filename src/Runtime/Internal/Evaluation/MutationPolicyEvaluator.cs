using System.Collections.Concurrent;
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
/// Sorted policies are cached per state type to avoid repeated sorting and registry lookups.
/// Synchronous policies complete without allocating async state machines.
/// </remarks>
internal sealed class MutationPolicyEvaluator(
    IPolicyRegistry policyRegistry,
    MutationEngineOptions options)
{
    private readonly IPolicyRegistry _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
    private readonly MutationEngineOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ConcurrentDictionary<Type, object> _sortedPolicies = new();

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
    public ValueTask<PolicyDecision> EvaluateAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        var policies = GetSortedPolicies<TState>();

        if (policies.Length == 0)
            return new ValueTask<PolicyDecision>(PolicyDecision.Allow());

        if (!_options.PolicyEvaluationTimeout.HasValue)
            return EvaluateNoTimeoutAsync(policies, mutation, state, cancellationToken);

        return new ValueTask<PolicyDecision>(
            EvaluateWithTimeoutAsync(policies, mutation, state, cancellationToken));
    }

    /// <summary>
    /// Retrieves sorted policies for the given state type from the cache or builds and caches them.
    /// </summary>
    /// <remarks>
    /// The first access per state type queries the registry and applies <c>OrderByDescending</c> /
    /// <c>ThenBy</c> sorting. Subsequent calls return the cached array without any registry or sort overhead.
    /// Thread safety relies on <c>ConcurrentDictionary</c> — duplicate builds are harmless since the array is immutable.
    /// </remarks>
    /// <typeparam name="TState">The state type whose policies are being retrieved.</typeparam>
    /// <returns>Cached array of policies sorted by descending priority, then by name, then by full type name.</returns>
    private IMutationPolicy<TState>[] GetSortedPolicies<TState>()
    {
        var type = typeof(TState);
        if (_sortedPolicies.TryGetValue(type, out var cached))
            return (IMutationPolicy<TState>[])cached;

        var policies = _policyRegistry.GetPolicies<TState>()
            .OrderByDescending(static p => p.Priority)
            .ThenBy(static p => p.Name, StringComparer.Ordinal)
            .ThenBy(static p => p.GetType().FullName ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        _sortedPolicies[type] = policies;
        return policies;
    }

    /// <summary>
    /// Evaluates the policy set without per policy timeouts.
    /// </summary>
    /// <returns>
    /// The first blocking or modifying <see cref="PolicyDecision"/> wrapped in a <see cref="ValueTask{TResult}"/>,
    /// or an allow decision when all policies pass.
    /// When all policies in the array complete synchronously the method returns a synchronously
    /// completed <see cref="ValueTask{TResult}"/> without allocating an async state machine.
    /// </returns>
    /// <typeparam name="TState">The state type handled by the policies.</typeparam>
    /// <param name="policies">The sorted policy array to evaluate.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel policy evaluation.</param>
    /// <exception cref="PolicyEvaluationException">Thrown when policy call throws non cancellation exception synchronously.</exception>
    private static ValueTask<PolicyDecision> EvaluateNoTimeoutAsync<TState>(
        IMutationPolicy<TState>[] policies,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < policies.Length; i++)
        {
            Task<PolicyDecision> task;
            try
            {
                task = policies[i].EvaluateAsync(mutation, state, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new PolicyEvaluationException(
                    policies[i].Name,
                    $"Policy '{policies[i].Name}' evaluation failed: {ex.Message}",
                    ex);
            }

            if (task.IsCompletedSuccessfully)
            {
                var decision = task.Result;
                if (!decision.IsAllowed || decision.Modifications is not null)
                    return new ValueTask<PolicyDecision>(decision);
            }
            else
            {
                return AwaitRemainingAsync(policies, i, task, mutation, state, cancellationToken);
            }
        }

        return new ValueTask<PolicyDecision>(PolicyDecision.Allow());
    }

    /// <summary>
    /// Continues evaluation asynchronously after the first non synchronously-completing policy is encountered.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the policies.</typeparam>
    /// <param name="policies">The sorted policy array to evaluate.</param>
    /// <param name="startIndex">The index of the policy whose task is being awaited.</param>
    /// <param name="currentTask">The task of the current policy that did not complete synchronously.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel policy evaluation.</param>
    /// <returns>
    /// The first blocking or modifying <see cref="PolicyDecision"/>, or an allow decision when all policies pass.
    /// </returns>
    /// <exception cref="PolicyEvaluationException">Thrown when policy evaluation fails with non cancellation exception.</exception>
    private static async ValueTask<PolicyDecision> AwaitRemainingAsync<TState>(
        IMutationPolicy<TState>[] policies,
        int startIndex,
        Task<PolicyDecision> currentTask,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        var decision = await CatchPolicyErrorAsync(currentTask, policies[startIndex].Name)
            .ConfigureAwait(false);

        if (!decision.IsAllowed || decision.Modifications is not null)
            return decision;

        for (var i = startIndex + 1; i < policies.Length; i++)
        {
            Task<PolicyDecision> task;
            try
            {
                task = policies[i].EvaluateAsync(mutation, state, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new PolicyEvaluationException(
                    policies[i].Name,
                    $"Policy '{policies[i].Name}' evaluation failed: {ex.Message}",
                    ex);
            }

            if (task.IsCompletedSuccessfully)
            {
                decision = task.Result;
            }
            else
            {
                decision = await CatchPolicyErrorAsync(task, policies[i].Name)
                    .ConfigureAwait(false);
            }

            if (!decision.IsAllowed || decision.Modifications is not null)
                return decision;
        }

        return PolicyDecision.Allow();
    }

    /// <summary>
    /// Awaits policy task and wraps non-cancellation failures into <see cref="PolicyEvaluationException"/>.
    /// </summary>
    /// <param name="task">The task returned by <c>policy.EvaluateAsync</c>.</param>
    /// <param name="policyName">The name of the policy, used as the exception policy name.</param>
    /// <returns>The <see cref="PolicyDecision"/> produced by the policy.</returns>
    /// <exception cref="OperationCanceledException">Rethrown without wrapping.</exception>
    /// <exception cref="PolicyEvaluationException">
    /// Thrown when the task faults with any exception other than <see cref="OperationCanceledException"/>.
    /// The original exception is set as the inner exception.
    /// </exception>
    private static async Task<PolicyDecision> CatchPolicyErrorAsync(
        Task<PolicyDecision> task,
        string policyName)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PolicyEvaluationException(
                policyName,
                $"Policy '{policyName}' evaluation failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Evaluates all policies with per-policy timeout enforcement.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the policies.</typeparam>
    /// <param name="policies">The sorted policy array to evaluate.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel policy evaluation.</param>
    /// <returns>
    /// The first blocking or modifying <see cref="PolicyDecision"/>, or an allow decision when all policies pass.
    /// </returns>
    /// <exception cref="PolicyEvaluationTimeoutException">
    /// Thrown when any policy evaluation exceeds <c>PolicyEvaluationTimeout</c>.
    /// </exception>
    /// <exception cref="PolicyEvaluationException">Thrown when policy evaluation fails with a non-cancellation exception.</exception>
    private async Task<PolicyDecision> EvaluateWithTimeoutAsync<TState>(
        IMutationPolicy<TState>[] policies,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < policies.Length; i++)
        {
            var decision = await EvaluateSingleWithTimeoutAsync(
                policies[i], mutation, state, cancellationToken).ConfigureAwait(false);

            if (!decision.IsAllowed || decision.Modifications is not null)
                return decision;
        }

        return PolicyDecision.Allow();
    }

    /// <summary>
    /// Evaluates single policy with dedicated cancellation timeout.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the policy.</typeparam>
    /// <param name="policy">The policy to evaluate.</param>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current state snapshot.</param>
    /// <param name="cancellationToken">Token used to cancel policy evaluation.</param>
    /// <returns>The <see cref="PolicyDecision"/> produced by the policy.</returns>
    /// <exception cref="PolicyEvaluationTimeoutException">
    /// Thrown when the policy evaluation exceeds <c>PolicyEvaluationTimeout</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">Rethrown when the caller's cancellation token triggers before the timeout.</exception>
    /// <exception cref="PolicyEvaluationException">
    /// Thrown when the policy evaluation fails with non cancellation exception.
    /// The original exception is set as the inner exception.
    /// </exception>
    private async Task<PolicyDecision> EvaluateSingleWithTimeoutAsync<TState>(
        IMutationPolicy<TState> policy,
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(_options.PolicyEvaluationTimeout!.Value);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

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
}
