namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

/// <summary>
/// Represents high-level lifecycle decisions taken against a mutation request.
/// </summary>
public enum MutationRequestLifecycleDecisionType
{
    /// <summary>
    /// The request was submitted into the governance system.
    /// </summary>
    Submitted = 0,
    /// <summary>
    /// The request entered a pending lifecycle state.
    /// </summary>
    Pending = 1,
    /// <summary>
    /// The request was approved.
    /// </summary>
    Approved = 2,
    /// <summary>
    /// The request was rejected.
    /// </summary>
    Rejected = 3,
    /// <summary>
    /// The request was canceled.
    /// </summary>
    Canceled = 4,
    /// <summary>
    /// The request expired before completion.
    /// </summary>
    Expired = 5,
    /// <summary>
    /// The request was superseded by another request.
    /// </summary>
    Superseded = 6,
    /// <summary>
    /// The request executed successfully.
    /// </summary>
    Executed = 7
}
