using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Runtime.Approval.Persistence;
using ModularityKit.Mutator.Governance.Runtime.Approval.State;
using ModularityKit.Mutator.Governance.Runtime.Approval.Validation;

namespace ModularityKit.Mutator.Governance.Runtime.Approval.Execution;

/// <summary>
/// Applies approval and rejection decisions to governed requests and persists the resulting request state.
/// </summary>
internal sealed class MutationRequestApprovalDecisionExecutor(IMutationRequestStore requestStore)
{
    private readonly IMutationRequestStore _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
    private readonly MutationRequestApprovalPersistence _persistence = new(requestStore);

    /// <summary>
    /// Applies single approval decision to request level approval requirement.
    /// </summary>
    public async Task<MutationRequest> ApplyDecision(
        string requestId,
        string approvalId,
        MutationContext decisionContext,
        string? reason,
        MutationApprovalRejectionReason? rejection,
        IReadOnlyDictionary<string, object>? metadata,
        Func<MutationApprovalRequirement, MutationContext, string?, MutationApprovalRejectionReason?, MutationApprovalRequirement> applyResolution,
        MutationRequestDecisionType decisionType,
        bool finalizeApprovedRequest,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID is required.", nameof(requestId));

        if (string.IsNullOrWhiteSpace(approvalId))
            throw new ArgumentException("Approval ID is required.", nameof(approvalId));

        ArgumentNullException.ThrowIfNull(decisionContext);
        ArgumentNullException.ThrowIfNull(applyResolution);

        var request = await GetRequired(requestId, cancellationToken).ConfigureAwait(false);
        MutationRequestApprovalWorkflowValidator.ValidateWorkflowRequest(request);

        var approvalRequirement = request.ApprovalRequirements.FirstOrDefault(requirement => requirement.ApprovalId == approvalId);
        if (approvalRequirement is null)
            throw new MutationApprovalRequirementNotFoundException(request.RequestId, approvalId);

        MutationRequestApprovalWorkflowValidator.ValidateApprovalAction(request, approvalRequirement, decisionContext);

        var resolvedRequirement = applyResolution(approvalRequirement, decisionContext, reason, rejection);
        var updatedRequirements = MutationRequestApprovalWorkflowState.Replace(request.ApprovalRequirements, resolvedRequirement);
        updatedRequirements = MutationRequestApprovalWorkflowState.ApplyQuorumSatisfaction(updatedRequirements, resolvedRequirement, decisionContext);

        var decisions = BuildDecisionHistory(
            request,
            updatedRequirements,
            resolvedRequirement,
            decisionContext,
            reason,
            rejection,
            metadata,
            decisionType,
            finalizeApprovedRequest);

        var updatedRequest = BuildUpdatedRequest(
            request,
            updatedRequirements,
            decisions,
            finalizeApprovedRequest);

        return await _persistence.Persist(request, updatedRequest, cancellationToken).ConfigureAwait(false);
    }

    private static List<MutationRequestDecision> BuildDecisionHistory(
        MutationRequest request,
        IReadOnlyList<MutationApprovalRequirement> updatedRequirements,
        MutationApprovalRequirement resolvedRequirement,
        MutationContext decisionContext,
        string? reason,
        MutationApprovalRejectionReason? rejection,
        IReadOnlyDictionary<string, object>? metadata,
        MutationRequestDecisionType decisionType,
        bool finalizeApprovedRequest)
    {
        var decisions = new List<MutationRequestDecision>(request.Decisions)
        {
            MutationRequestApprovalWorkflowState.CreateApprovalDecision(
                decisionType,
                resolvedRequirement,
                decisionContext,
                reason,
                rejection,
                metadata)
        };

        if (finalizeApprovedRequest &&
            !string.IsNullOrWhiteSpace(resolvedRequirement.ApprovalGroupId) &&
            updatedRequirements.Any(requirement =>
                requirement.StepOrder == resolvedRequirement.StepOrder &&
                string.Equals(requirement.ApprovalGroupId, resolvedRequirement.ApprovalGroupId, StringComparison.Ordinal) &&
                requirement.Status == MutationApprovalRequirementStatus.Satisfied))
        {
            decisions.Add(MutationRequestDecision.Approval(
                MutationRequestApprovalDecisionType.QuorumSatisfied,
                decisionContext,
                reason: $"Approval quorum satisfied for group '{resolvedRequirement.ApprovalGroupId}'.",
                metadata: new Dictionary<string, object>
                {
                    ["ApprovalGroupId"] = resolvedRequirement.ApprovalGroupId,
                    ["RequiredApprovals"] = resolvedRequirement.RequiredApprovals
                }));
        }

        var isFullyApproved = updatedRequirements.All(requirement =>
            requirement.Status is MutationApprovalRequirementStatus.Approved or MutationApprovalRequirementStatus.Satisfied);

        if (finalizeApprovedRequest && isFullyApproved)
        {
            decisions.Add(MutationRequestDecision.Lifecycle(
                MutationRequestLifecycleDecisionType.Approved,
                decisionContext,
                reason: "All approval requirements were fulfilled."));
        }
        else if (!finalizeApprovedRequest)
        {
            decisions.Add(MutationRequestDecision.Lifecycle(
                MutationRequestLifecycleDecisionType.Rejected,
                decisionContext,
                reason: reason ?? rejection?.Message ?? decisionContext.Reason ?? "Request was rejected during approval workflow."));
        }

        return decisions;
    }

    private static MutationRequest BuildUpdatedRequest(
        MutationRequest request,
        IReadOnlyList<MutationApprovalRequirement> updatedRequirements,
        IReadOnlyList<MutationRequestDecision> decisions,
        bool finalizeApprovedRequest)
    {
        var updatedRequest = request with
        {
            ApprovalRequirements = updatedRequirements
        };

        if (finalizeApprovedRequest)
        {
            var isFullyApproved = updatedRequirements.All(requirement =>
                requirement.Status is MutationApprovalRequirementStatus.Approved or MutationApprovalRequirementStatus.Satisfied);

            updatedRequest = updatedRequest with
            {
                Lifecycle = request.Lifecycle with
                {
                    Status = isFullyApproved ? MutationRequestStatus.Approved : MutationRequestStatus.Pending,
                    PendingReason = isFullyApproved ? null : PendingMutationReason.Approval
                }
            };
        }
        else
        {
            updatedRequest = updatedRequest with
            {
                Lifecycle = request.Lifecycle with
                {
                    Status = MutationRequestStatus.Rejected,
                    PendingReason = null
                }
            };
        }

        return updatedRequest with
        {
            Decisions = decisions,
            Lifecycle = updatedRequest.Lifecycle with
            {
                UpdatedAt = decisions[^1].Timestamp
            }
        };
    }

    private async Task<MutationRequest> GetRequired(
        string requestId,
        CancellationToken cancellationToken)
    {
        var request = await _requestStore.Get(requestId, cancellationToken).ConfigureAwait(false);

        if (request is null)
            throw new MutationRequestNotFoundException(requestId);

        return request;
    }
}
