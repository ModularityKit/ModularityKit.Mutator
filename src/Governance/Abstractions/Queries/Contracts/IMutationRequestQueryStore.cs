using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;

/// <summary>
/// Query oriented access to governed mutation requests.
/// </summary>
public interface IMutationRequestQueryStore
{
    /// <summary>
    /// Queries governed requests using the supplied criteria.
    /// </summary>
    Task<IReadOnlyList<MutationRequest>> QueryAsync(
        MutationRequestQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns pending requests, optionally narrowed by additional criteria.
    /// </summary>
    Task<IReadOnlyList<MutationRequest>> GetPendingRequestsAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the pending approval queue, optionally narrowed by additional criteria.
    /// </summary>
    Task<IReadOnlyList<MutationRequest>> GetPendingApprovalQueueAsync(
        MutationRequestQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns recent approval driven requests, optionally narrowed by additional criteria.
    /// </summary>
    Task<IReadOnlyList<MutationRequest>> GetRecentApprovalsAsync(
        MutationRequestQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns approval oriented projections for governed requests.
    /// </summary>
    Task<IReadOnlyList<MutationApprovalView>> GetPendingApprovalsAsync(
        MutationApprovalQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns recent decision oriented projections across governed requests.
    /// </summary>
    Task<IReadOnlyList<MutationRequestDecisionView>> GetRecentDecisionsAsync(
        MutationRequestDecisionQuery? query = null,
        int? take = null,
        CancellationToken cancellationToken = default);
}
