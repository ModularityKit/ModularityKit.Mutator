using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Runtime.Approval.Persistence;
using ModularityKit.Mutator.Governance.Runtime.Approval.State;

namespace ModularityKit.Mutator.Governance.Runtime.Approval.Execution;

/// <summary>
/// Applies approval specific expiration semantics to pending approval requests.
/// </summary>
internal sealed class MutationRequestApprovalExpirationExecutor(IMutationRequestStore requestStore)
{
    private readonly IMutationRequestStore _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
    private readonly MutationRequestApprovalPersistence _persistence = new(requestStore);

    /// <summary>
    /// Expires approval requirements and rejects requests when their approval deadlines have elapsed.
    /// </summary>
    public async Task<IReadOnlyList<MutationRequest>> ExpirePendingApprovals(
        DateTimeOffset now,
        MutationContext decisionContext,
        CancellationToken cancellationToken)
    {
        var pendingRequests = await _requestStore.GetPending(PendingMutationReason.Approval, cancellationToken).ConfigureAwait(false);
        var expiredRequests = new List<MutationRequest>();

        foreach (var request in pendingRequests)
        {
            var expiredApprovals = request.ApprovalRequirements
                .Where(requirement =>
                    requirement.Status == MutationApprovalRequirementStatus.Pending &&
                    requirement.ExpiresAt is not null &&
                    requirement.ExpiresAt <= now)
                .ToList();

            if (expiredApprovals.Count == 0)
                continue;

            var updatedRequirements = request.ApprovalRequirements
                .Select(requirement =>
                {
                    var expired = expiredApprovals.Any(candidate => candidate.ApprovalId == requirement.ApprovalId);
                    return expired
                        ? MutationRequestApprovalWorkflowState.ApplyExpiration(requirement, decisionContext)
                        : requirement;
                })
                .ToList();

            var decisions = new List<MutationRequestDecision>(request.Decisions);
            decisions.AddRange(expiredApprovals.Select(requirement =>
                MutationRequestApprovalWorkflowState.CreateApprovalDecision(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Expired),
                    requirement,
                    decisionContext,
                    reason: requirement.ExpiresAt is null
                        ? "Approval requirement expired."
                        : $"Approval requirement expired at '{requirement.ExpiresAt:O}'.")));

            decisions.Add(MutationRequestDecision.Create(
                MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Rejected),
                decisionContext,
                reason: "Request was rejected because one or more approval requirements expired."));

            var updatedRequest = request with
            {
                Status = MutationRequestStatus.Rejected,
                PendingReason = null,
                ApprovalRequirements = updatedRequirements,
                Decisions = decisions,
                UpdatedAt = decisions[^1].Timestamp
            };

            expiredRequests.Add(await _persistence.Persist(request, updatedRequest, cancellationToken).ConfigureAwait(false));
        }

        return expiredRequests;
    }
}
