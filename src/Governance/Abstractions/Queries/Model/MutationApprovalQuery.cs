using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

/// <summary>
/// Defines storage-agnostic filters for approval-oriented governance queries.
/// </summary>
public sealed record MutationApprovalQuery
{
    /// <summary>
    /// Request-level filters applied before approval requirements are projected.
    /// </summary>
    public MutationRequestQuery RequestQuery { get; init; } = new();

    /// <summary>
    /// Request categories to include.
    /// </summary>
    public IReadOnlySet<string> Categories { get; init; } = new HashSet<string>();

    /// <summary>
    /// Allowed approver identifiers.
    /// </summary>
    public IReadOnlySet<string> ApproverIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// Allowed approver roles.
    /// </summary>
    public IReadOnlySet<string> ApproverRoles { get; init; } = new HashSet<string>();

    /// <summary>
    /// Allowed approver groups.
    /// </summary>
    public IReadOnlySet<string> ApproverGroups { get; init; } = new HashSet<string>();

    /// <summary>
    /// Allowed approval requirement statuses.
    /// </summary>
    public IReadOnlySet<MutationApprovalRequirementStatus> ApprovalStatuses { get; init; }
        = new HashSet<MutationApprovalRequirementStatus>();

    /// <summary>
    /// Allowed pending reasons for the parent request.
    /// </summary>
    public IReadOnlySet<PendingMutationReason> PendingReasons { get; init; } = new HashSet<PendingMutationReason>();

    /// <summary>
    /// Allowed request statuses for the parent request.
    /// </summary>
    public IReadOnlySet<MutationRequestStatus> RequestStatuses { get; init; } = new HashSet<MutationRequestStatus>();

    /// <summary>
    /// Creates a query for pending approval work.
    /// </summary>
    public static MutationApprovalQuery Pending()
        => new()
        {
            ApprovalStatuses = new HashSet<MutationApprovalRequirementStatus>
            {
                MutationApprovalRequirementStatus.Pending
            },
            RequestStatuses = new HashSet<MutationRequestStatus>
            {
                MutationRequestStatus.Pending
            },
            PendingReasons = new HashSet<PendingMutationReason>
            {
                PendingMutationReason.Approval
            }
        };
}
