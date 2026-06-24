namespace ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;

/// <summary>
/// Represents the lifecycle status of governed mutation request.
/// </summary>
public enum MutationRequestStatus
{
    /// <summary>
    /// The request has been created but not yet processed.
    /// </summary>
    Created = 0,
    /// <summary>
    /// The request is waiting for an external governance condition.
    /// </summary>
    Pending = 1,
    /// <summary>
    /// The request has been approved and may proceed to execution.
    /// </summary>
    Approved = 2,
    /// <summary>
    /// The request has been rejected and will not proceed.
    /// </summary>
    Rejected = 3,
    /// <summary>
    /// The request has been canceled by an explicit action.
    /// </summary>
    Canceled = 4,
    /// <summary>
    /// The request expired before it could be completed.
    /// </summary>
    Expired = 5,
    /// <summary>
    /// The request has been superseded by another request.
    /// </summary>
    Superseded = 6,
    /// <summary>
    /// The request has been executed successfully.
    /// </summary>
    Executed = 7
}
