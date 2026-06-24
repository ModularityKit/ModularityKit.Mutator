using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Approval.Contracts;

/// <summary>
/// Resolves approval requirements attached to governed mutation requests.
/// </summary>
public interface IMutationRequestApprovalWorkflowManager
{
    /// <summary>
    /// Approves one pending approval requirement on a request.
    /// </summary>
    Task<MutationRequest> ApproveRequirement(
        string requestId,
        string approvalId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects one pending approval requirement on a request and terminates the request.
    /// </summary>
    Task<MutationRequest> RejectRequirement(
        string requestId,
        string approvalId,
        MutationContext decisionContext,
        string? reason = null,
        MutationApprovalRejectionReason? rejection = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires requests whose pending approval requirements have passed their approval-specific expiration time.
    /// </summary>
    Task<IReadOnlyList<MutationRequest>> ExpirePendingApprovals(
        DateTimeOffset now,
        MutationContext decisionContext,
        CancellationToken cancellationToken = default);
}
