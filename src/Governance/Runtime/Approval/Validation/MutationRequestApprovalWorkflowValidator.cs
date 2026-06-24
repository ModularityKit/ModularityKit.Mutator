using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Approval.State;

namespace ModularityKit.Mutator.Governance.Runtime.Approval.Validation;

/// <summary>
/// Validates approval workflow state and approval actions before the runtime mutates a governed request.
/// </summary>
internal static class MutationRequestApprovalWorkflowValidator
{
    /// <summary>
    /// Ensures the request is currently in pending approval state.
    /// </summary>
    public static void ValidateWorkflowRequest(MutationRequest request)
    {
        if (request.Status != MutationRequestStatus.Pending || request.PendingReason != PendingMutationReason.Approval)
            throw new InvalidMutationApprovalWorkflowStateException(
                request.RequestId,
                $"Request '{request.RequestId}' is not in pending approval state.");

        if (request.ApprovalRequirements.Count == 0)
            throw new InvalidMutationApprovalWorkflowStateException(
                request.RequestId,
                $"Request '{request.RequestId}' does not define approval requirements.");
    }

    /// <summary>
    /// Ensures an approval action is valid for the current request, approval target, and active step.
    /// </summary>
    public static void ValidateApprovalAction(
        MutationRequest request,
        MutationApprovalRequirement approvalRequirement,
        MutationContext decisionContext)
    {
        if (approvalRequirement.Status != MutationApprovalRequirementStatus.Pending)
            throw new InvalidMutationApprovalActionException(
                request.RequestId,
                approvalRequirement.ApprovalId,
                $"Approval requirement '{approvalRequirement.ApprovalId}' is already {approvalRequirement.Status}.");

        if (approvalRequirement.ExpiresAt is not null && approvalRequirement.ExpiresAt <= decisionContext.Timestamp)
            throw new MutationApprovalRequirementExpiredException(
                request.RequestId,
                approvalRequirement.ApprovalId,
                approvalRequirement.ExpiresAt.Value);

        if (string.IsNullOrWhiteSpace(decisionContext.ActorId))
            throw new InvalidMutationApprovalActionException(
                request.RequestId,
                approvalRequirement.ApprovalId,
                "Approval actions require a user or service actor ID.");

        if (!MutationRequestApprovalWorkflowState.MatchesApprovalTarget(approvalRequirement, decisionContext))
            throw new InvalidMutationApprovalActionException(
                request.RequestId,
                approvalRequirement.ApprovalId,
                $"Actor '{decisionContext.ActorId}' does not satisfy the approval target for '{approvalRequirement.ApprovalId}'.");

        var currentStep = request.ApprovalRequirements
            .Where(requirement => requirement.Status == MutationApprovalRequirementStatus.Pending)
            .Min(requirement => requirement.StepOrder);

        if (approvalRequirement.StepOrder != currentStep)
            throw new InvalidMutationApprovalActionException(
                request.RequestId,
                approvalRequirement.ApprovalId,
                $"Approval requirement '{approvalRequirement.ApprovalId}' is in step {approvalRequirement.StepOrder}, but current active step is {currentStep}.");
    }
}
