using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

/// <summary>
/// Evaluates approval oriented query criteria against governed mutation requests.
/// </summary>
public static class MutationApprovalQueryEvaluator
{
    public static bool Matches(MutationRequest request, MutationApprovalRequirement approval, MutationApprovalQuery query)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(query);

        return MutationRequestQueryEvaluator.Matches(request, query.RequestQuery) &&
               MatchesCategory(request, query) &&
               MatchesApproverId(approval, query) &&
               MatchesApproverRole(approval, query) &&
               MatchesApproverGroup(approval, query) &&
               MatchesApprovalStatus(approval, query) &&
               MatchesPendingReason(request, query) &&
               MatchesRequestStatus(request, query);
    }

    private static bool MatchesCategory(MutationRequest request, MutationApprovalQuery query)
        => query.Categories.Count == 0 || query.Categories.Contains(request.Intent.Category);

    private static bool MatchesApproverId(MutationApprovalRequirement approval, MutationApprovalQuery query)
        => query.ApproverIds.Count == 0 || query.ApproverIds.Contains(approval.ApproverId);

    private static bool MatchesApproverRole(MutationApprovalRequirement approval, MutationApprovalQuery query)
        => query.ApproverRoles.Count == 0 ||
           (approval.ApproverRole is not null && query.ApproverRoles.Contains(approval.ApproverRole));

    private static bool MatchesApproverGroup(MutationApprovalRequirement approval, MutationApprovalQuery query)
        => query.ApproverGroups.Count == 0 ||
           (approval.ApproverGroup is not null && query.ApproverGroups.Contains(approval.ApproverGroup));

    private static bool MatchesApprovalStatus(MutationApprovalRequirement approval, MutationApprovalQuery query)
        => query.ApprovalStatuses.Count == 0 || query.ApprovalStatuses.Contains(approval.Status);

    private static bool MatchesPendingReason(MutationRequest request, MutationApprovalQuery query)
        => query.PendingReasons.Count == 0 ||
           (request.PendingReason is not null && query.PendingReasons.Contains(request.PendingReason.Value));

    private static bool MatchesRequestStatus(MutationRequest request, MutationApprovalQuery query)
        => query.RequestStatuses.Count == 0 || query.RequestStatuses.Contains(request.Status);
}
