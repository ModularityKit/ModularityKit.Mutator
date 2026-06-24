namespace ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;

/// <summary>
/// Raised when approval actions are attempted against a request that is not in an approval workflow state.
/// </summary>
public sealed class InvalidMutationApprovalWorkflowStateException(
    string requestId,
    string message) : InvalidOperationException(message)
{
    /// <summary>
    /// Request identifier against which the invalid approval workflow action was attempted.
    /// </summary>
    public string RequestId { get; } = requestId;
}
