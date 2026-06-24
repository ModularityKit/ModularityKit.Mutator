namespace ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;

/// <summary>
/// Raised when a pending approval requirement has already expired.
/// </summary>
public sealed class MutationApprovalRequirementExpiredException(
    string requestId,
    string approvalId,
    DateTimeOffset expiresAt) : InvalidOperationException(
    $"Approval requirement '{approvalId}' on request '{requestId}' expired at '{expiresAt:O}'.")
{
    /// <summary>
    /// Request identifier on which the expired approval requirement exists.
    /// </summary>
    public string RequestId { get; } = requestId;

    /// <summary>
    /// Expired approval identifier.
    /// </summary>
    public string ApprovalId { get; } = approvalId;

    /// <summary>
    /// Timestamp at which the approval requirement expired.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; } = expiresAt;
}
