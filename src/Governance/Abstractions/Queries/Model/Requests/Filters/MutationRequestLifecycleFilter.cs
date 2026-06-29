using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Groups lifecycle oriented request query filters.
/// </summary>
public sealed record MutationRequestLifecycleFilter
{
    /// <summary>
    /// Request statuses to include.
    /// </summary>
    public IReadOnlySet<MutationRequestStatus> Statuses { get; init; } = new HashSet<MutationRequestStatus>();

    /// <summary>
    /// Pending reasons to include.
    /// </summary>
    public IReadOnlySet<PendingMutationReason> PendingReasons { get; init; } = new HashSet<PendingMutationReason>();

    /// <summary>
    /// Decision categories that must appear in the request history.
    /// </summary>
    public IReadOnlySet<MutationRequestDecisionCategory> DecisionCategories { get; init; }
        = new HashSet<MutationRequestDecisionCategory>();
}
