using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;

/// <summary>
/// Common request query presets for governance workflows.
/// </summary>
public static class MutationRequestQueries
{
    /// <summary>
    /// Creates query that targets pending requests.
    /// </summary>
    public static MutationRequestQuery Pending()
        => new()
        {
            Lifecycle = new MutationRequestLifecycleFilter
            {
                Statuses = new HashSet<MutationRequestStatus> { MutationRequestStatus.Pending }
            }
        };

    /// <summary>
    /// Creates query that targets the pending approval queue.
    /// </summary>
    public static MutationRequestQuery PendingApprovalQueue()
        => new()
        {
            Lifecycle = new MutationRequestLifecycleFilter
            {
                Statuses = new HashSet<MutationRequestStatus> { MutationRequestStatus.Pending },
                PendingReasons = new HashSet<PendingMutationReason> { PendingMutationReason.Approval },
                DecisionCategories = new HashSet<MutationRequestDecisionCategory>
                {
                    MutationRequestDecisionCategory.Approval
                }
            }
        };

    /// <summary>
    /// Creates query that targets approval driven requests that recently moved through approval.
    /// </summary>
    public static MutationRequestQuery RecentApprovals()
        => new()
        {
            Lifecycle = new MutationRequestLifecycleFilter
            {
                Statuses = new HashSet<MutationRequestStatus>
                {
                    MutationRequestStatus.Approved,
                    MutationRequestStatus.Executed
                },
                DecisionCategories = new HashSet<MutationRequestDecisionCategory>
                {
                    MutationRequestDecisionCategory.Approval
                }
            }
        };
}
