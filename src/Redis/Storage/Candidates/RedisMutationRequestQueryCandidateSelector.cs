using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Execution;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Models;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Planning;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Candidates;

/// <summary>
/// Selects Redis request-id candidates for higher-level request queries.
/// </summary>
internal sealed class RedisMutationRequestQueryCandidateSelector(
    RedisMutationRequestCandidatePlanBuilder planBuilder,
    RedisMutationRequestCandidateExecutor candidateExecutor)
{
    private readonly RedisMutationRequestCandidatePlanBuilder _planBuilder =
        planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
   
    private readonly RedisMutationRequestCandidateExecutor _candidateExecutor =
        candidateExecutor ?? throw new ArgumentNullException(nameof(candidateExecutor));

    /// <summary>
    /// Loads all known request identifiers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public Task<IReadOnlyList<string>> LoadAllRequestIdsAsync(CancellationToken cancellationToken) =>
        LoadAsync(_planBuilder.BuildAllRequestsPlan(), cancellationToken);

    /// <summary>
    /// Loads request identifiers for a specific state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public Task<IReadOnlyList<string>> LoadByStateIdAsync(string stateId, CancellationToken cancellationToken) =>
        LoadAsync(_planBuilder.BuildByStateIdPlan(stateId), cancellationToken);

    /// <summary>
    /// Loads pending request identifiers, optionally narrowed by reason.
    /// </summary>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public Task<IReadOnlyList<string>> LoadPendingAsync(PendingMutationReason? reason, CancellationToken cancellationToken) =>
        LoadAsync(_planBuilder.BuildPendingPlan(reason), cancellationToken);

    /// <summary>
    /// Loads pending request identifiers for a specific state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public Task<IReadOnlyList<string>> LoadPendingByStateIdAsync(string stateId, PendingMutationReason? reason, CancellationToken cancellationToken) =>
        LoadAsync(_planBuilder.BuildPendingByStateIdPlan(stateId, reason), cancellationToken);

    /// <summary>
    /// Loads request identifiers for a general request query using Redis-side candidate narrowing.
    /// </summary>
    /// <param name="query">The query to analyze.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public Task<IReadOnlyList<string>> LoadQueryCandidatesAsync(MutationRequestQuery query, CancellationToken cancellationToken) =>
        LoadAsync(_planBuilder.BuildQueryPlan(query), cancellationToken);

    private Task<IReadOnlyList<string>> LoadAsync(RedisMutationRequestCandidatePlan plan, CancellationToken cancellationToken) =>
        _candidateExecutor.ExecuteAsync(plan, cancellationToken);
}
