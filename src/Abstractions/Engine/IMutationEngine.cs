using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Interception;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Abstractions.Engine;

/// <summary>
/// Orchestrates mutation execution using a fully governed, end-to-end pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IMutationEngine"/> is the central coordination component responsible for
/// executing mutations in a deterministic and auditable manner.
/// </para>
/// <para>
/// The execution pipeline includes, in order:
/// </para>
/// <list type="bullet">
///   <item><description>Policy evaluation</description></item>
///   <item><description>Validation</description></item>
///   <item><description>Interception</description></item>
///   <item><description>Execution</description></item>
///   <item><description>Audit logging</description></item>
///   <item><description>History persistence</description></item>
/// </list>
/// <para>
/// Core runtime concurrency is governed by <see cref="ModularityKit.Mutator.Abstractions.MutationEngineOptions.MaxConcurrentMutations"/>.
/// Mutations that target the same <see cref="MutationContext.StateId"/> are serialized by the runtime so shared-state workloads
/// remain deterministic. This is separate from governance request storage concurrency, which protects request lifecycle writes
/// in the governance package.
/// </para>
/// <para>
/// The engine acts as the primary governance boundary for all state mutations.
/// </para>
/// </remarks>
public interface IMutationEngine
{
    /// <summary>
    /// Executes a single mutation using the full governance pipeline.
    /// </summary>
    /// <typeparam name="TState">The type of the state being mutated.</typeparam>
    /// <param name="mutation">The mutation to execute.</param>
    /// <param name="state">The current state.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>
    /// A <see cref="MutationResult{TState}"/> containing the execution outcome,
    /// produced changes, and resulting state.
    /// </returns>
    Task<MutationResult<TState>> ExecuteAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a batch of mutations as a single logical transaction.
    /// </summary>
    /// <typeparam name="TState">The type of the state being mutated.</typeparam>
    /// <param name="mutations">The sequence of mutations to execute.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>
    /// A <see cref="BatchMutationResult{TState}"/> describing the outcome of the batch execution.
    /// </returns>
    /// <remarks>
    /// Batch execution is ordered and sequential. Each batch step passes through the same core concurrency controls as a
    /// single execution, including the maximum concurrent execution limit and any state-specific serialization.
    /// Fail-fast vs best-effort behavior is controlled by <see cref="MutationEngineOptions"/>.
    /// </remarks>
    Task<BatchMutationResult<TState>> ExecuteBatchAsync<TState>(
        IEnumerable<IMutation<TState>> mutations,
        TState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a global mutation policy.
    /// </summary>
    /// <typeparam name="TState">The state type the policy applies to.</typeparam>
    /// <param name="policy">The policy to register.</param>
    /// <remarks>
    /// Global policies participate in evaluation for every compatible mutation
    /// and represent the primary governance mechanism.
    /// </remarks>
    void RegisterPolicy<TState>(IMutationPolicy<TState> policy);

    /// <summary>
    /// Registers a global mutation interceptor.
    /// </summary>
    /// <param name="interceptor">The interceptor to register.</param>
    /// <remarks>
    /// Interceptors observe and react to mutation lifecycle events but must not
    /// directly alter mutation semantics.
    /// </remarks>
    void RegisterInterceptor(IMutationInterceptor interceptor);

    /// <summary>
    /// Retrieves the mutation history for a given state identifier.
    /// </summary>
    /// <param name="stateId">The identifier of the state.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="MutationHistory"/> containing all recorded mutations for the state.
    /// </returns>
    Task<MutationHistory> GetHistoryAsync(
        string stateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated mutation execution statistics.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="MutationStatistics"/> snapshot representing engine-level metrics.
    /// </returns>
    Task<MutationStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}
