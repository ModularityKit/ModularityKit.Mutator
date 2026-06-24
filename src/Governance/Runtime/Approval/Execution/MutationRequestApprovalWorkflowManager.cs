using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Runtime.Approval.State;

namespace ModularityKit.Mutator.Governance.Runtime.Approval.Execution;

/// <summary>
/// Executes explicit approval and rejection actions for governed mutation requests.
/// </summary>
public sealed class MutationRequestApprovalWorkflowManager(IMutationRequestStore requestStore)
    : IMutationRequestApprovalWorkflowManager
{
    private readonly MutationRequestApprovalDecisionExecutor _decisionExecutor =
        new(requestStore ?? throw new ArgumentNullException(nameof(requestStore)));

    private readonly MutationRequestApprovalExpirationExecutor _expirationExecutor =
        new(requestStore ?? throw new ArgumentNullException(nameof(requestStore)));

    /// <summary>
    /// Approves single request level approval requirement and advances the request when all approvals are satisfied.
    /// </summary>
    /// <param name="requestId">Governed request identifier.</param>
    /// <param name="approvalId">Approval requirement identifier.</param>
    /// <param name="decisionContext">Actor context performing the approval.</param>
    /// <param name="reason">Optional free-form approval reason.</param>
    /// <param name="metadata">Optional extra decision metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated governed request.</returns>
    public Task<MutationRequest> ApproveRequirement(
        string requestId,
        string approvalId,
        MutationContext decisionContext,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _decisionExecutor.ApplyDecision(
            requestId,
            approvalId,
            decisionContext,
            reason,
            null,
            metadata,
            MutationRequestApprovalWorkflowState.ApplyApproval,
            MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Granted),
            finalizeApprovedRequest: true,
            cancellationToken);
    }

    /// <summary>
    /// Rejects single request level approval requirement and terminates the request lifecycle.
    /// </summary>
    /// <param name="requestId">Governed request identifier.</param>
    /// <param name="approvalId">Approval requirement identifier.</param>
    /// <param name="decisionContext">Actor context performing the rejection.</param>
    /// <param name="reason">Optional free-form rejection reason.</param>
    /// <param name="rejection">Optional structured rejection payload.</param>
    /// <param name="metadata">Optional extra decision metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated governed request.</returns>
    public Task<MutationRequest> RejectRequirement(
        string requestId,
        string approvalId,
        MutationContext decisionContext,
        string? reason = null,
        MutationApprovalRejectionReason? rejection = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _decisionExecutor.ApplyDecision(
            requestId,
            approvalId,
            decisionContext,
            reason,
            rejection,
            metadata,
            MutationRequestApprovalWorkflowState.ApplyRejection,
            MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Rejected),
            finalizeApprovedRequest: false,
            cancellationToken);
    }

    /// <summary>
    /// Expires pending approval requests whose approval specific deadlines have elapsed.
    /// </summary>
    /// <param name="now">The timestamp used to evaluate approval expiration.</param>
    /// <param name="decisionContext">Actor context recording the expiration sweep.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requests that were expired during the sweep.</returns>
    public async Task<IReadOnlyList<MutationRequest>> ExpirePendingApprovals(
        DateTimeOffset now,
        MutationContext decisionContext,
        CancellationToken cancellationToken = default) =>
        await _expirationExecutor.ExpirePendingApprovals(now, decisionContext, cancellationToken).ConfigureAwait(false);
}
