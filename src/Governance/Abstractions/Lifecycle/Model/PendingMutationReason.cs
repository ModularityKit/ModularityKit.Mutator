namespace ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;

/// <summary>
/// Describes why a mutation request cannot execute immediately.
/// </summary>
public enum PendingMutationReason
{
    /// <summary>
    /// The request is waiting for approval.
    /// </summary>
    Approval = 0,

    /// <summary>
    /// The request is waiting for an external check or integration response.
    /// </summary>
    ExternalCheck = 1,

    /// <summary>
    /// The request is waiting for a scheduled execution window.
    /// </summary>
    Schedule = 2,

    /// <summary>
    /// The request is waiting for a dependency to become ready.
    /// </summary>
    Dependency = 3,

    /// <summary>
    /// The request is waiting because of quota constraints.
    /// </summary>
    Quota = 4,

    /// <summary>
    /// The request is waiting for manual review.
    /// </summary>
    ManualReview = 5,
    Revalidation = 6
}
