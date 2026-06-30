using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

/// <summary>
/// Groups lifecycle state and lifecycle timestamps associated with a governed request.
/// </summary>
public sealed record MutationRequestLifecycleDetails
{
    /// <summary>
    /// Current lifecycle status of the request.
    /// </summary>
    public MutationRequestStatus Status { get; init; } = MutationRequestStatus.Created;

    /// <summary>
    /// Reason why the request is pending, if it has not executed yet.
    /// </summary>
    public PendingMutationReason? PendingReason { get; init; }

    /// <summary>
    /// Optional expiration time for pending requests.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Timestamp when the request was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp of the last lifecycle update applied to the request.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
