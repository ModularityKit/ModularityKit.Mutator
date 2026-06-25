using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

/// <summary>
/// Defines storage-agnostic filters for governed mutation request queries.
/// </summary>
public sealed record MutationRequestQuery
{
    /// <summary>
    /// Specific request identifiers to include.
    /// </summary>
    public IReadOnlySet<string> RequestIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// State identifiers to include.
    /// </summary>
    public IReadOnlySet<string> StateIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// State types to include.
    /// </summary>
    public IReadOnlySet<string> StateTypes { get; init; } = new HashSet<string>();

    /// <summary>
    /// Mutation types to include.
    /// </summary>
    public IReadOnlySet<string> MutationTypes { get; init; } = new HashSet<string>();

    /// <summary>
    /// Actor identifiers to include.
    /// </summary>
    public IReadOnlySet<string> ActorIds { get; init; } = new HashSet<string>();

    /// <summary>
    /// Actor names to include.
    /// </summary>
    public IReadOnlySet<string> ActorNames { get; init; } = new HashSet<string>();

    /// <summary>
    /// Mutation categories to include.
    /// </summary>
    public IReadOnlySet<string> Categories { get; init; } = new HashSet<string>();

    /// <summary>
    /// Request statuses to include.
    /// </summary>
    public IReadOnlySet<MutationRequestStatus> Statuses { get; init; } = new HashSet<MutationRequestStatus>();

    /// <summary>
    /// Pending reasons to include.
    /// </summary>
    public IReadOnlySet<PendingMutationReason> PendingReasons { get; init; } = new HashSet<PendingMutationReason>();

    /// <summary>
    /// Tags to include from the request intent.
    /// </summary>
    public IReadOnlySet<string> Tags { get; init; } = new HashSet<string>();

    /// <summary>
    /// Tag matching strategy.
    /// </summary>
    public MutationRequestTagMatchMode TagMatchMode { get; init; } = MutationRequestTagMatchMode.Any;

    /// <summary>
    /// Exact metadata key/value pairs to match against request metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Minimum estimated blast radius scope to include.
    /// </summary>
    public BlastRadiusScope? MinimumBlastRadiusScope { get; init; }

    /// <summary>
    /// Maximum estimated blast radius scope to include.
    /// </summary>
    public BlastRadiusScope? MaximumBlastRadiusScope { get; init; }

    /// <summary>
    /// Inclusive lower bound for request creation time.
    /// </summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>
    /// Inclusive upper bound for request creation time.
    /// </summary>
    public DateTimeOffset? CreatedTo { get; init; }

    /// <summary>
    /// Inclusive lower bound for request update time.
    /// </summary>
    public DateTimeOffset? UpdatedFrom { get; init; }

    /// <summary>
    /// Inclusive upper bound for request update time.
    /// </summary>
    public DateTimeOffset? UpdatedTo { get; init; }

    /// <summary>
    /// Decision categories that must appear in the request history.
    /// </summary>
    public IReadOnlySet<MutationRequestDecisionCategory> DecisionCategories { get; init; }
        = new HashSet<MutationRequestDecisionCategory>();

    /// <summary>
    /// Creates a query that targets pending requests.
    /// </summary>
    public static MutationRequestQuery Pending()
        => new()
        {
            Statuses = new HashSet<MutationRequestStatus> { MutationRequestStatus.Pending }
        };

    /// <summary>
    /// Creates a query that targets the pending approval queue.
    /// </summary>
    public static MutationRequestQuery PendingApprovalQueue()
        => new()
        {
            Statuses = new HashSet<MutationRequestStatus> { MutationRequestStatus.Pending },
            PendingReasons = new HashSet<PendingMutationReason> { PendingMutationReason.Approval },
            DecisionCategories = new HashSet<MutationRequestDecisionCategory>
            {
                MutationRequestDecisionCategory.Approval
            }
        };

    /// <summary>
    /// Creates a query that targets approval-driven requests that recently moved through approval.
    /// </summary>
    public static MutationRequestQuery RecentApprovals()
        => new()
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
        };
}

/// <summary>
/// Controls how tag filters are evaluated.
/// </summary>
public enum MutationRequestTagMatchMode
{
    /// <summary>
    /// Match requests that contain at least one of the requested tags.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Match requests that contain all requested tags.
    /// </summary>
    All = 1
}
