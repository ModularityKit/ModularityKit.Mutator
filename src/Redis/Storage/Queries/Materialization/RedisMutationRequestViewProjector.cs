using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Queries.Materialization;

/// <summary>
/// Projects governed request documents into query specific view models.
/// </summary>
internal static class RedisMutationRequestViewProjector
{
    /// <summary>
    /// Projects governed requests into approval-centric query views.
    /// </summary>
    /// <param name="requests">Requests to project.</param>
    /// <returns>Approval views produced from request approval requirements.</returns>
    public static IEnumerable<MutationApprovalView> ToApprovalViews(IEnumerable<MutationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests.SelectMany(request => request.ApprovalRequirements.Select(approval => new MutationApprovalView
        {
            Request = request,
            Approval = approval
        }));
    }

    /// <summary>
    /// Projects governed requests into decision-centric query views.
    /// </summary>
    /// <param name="requests">Requests to project.</param>
    /// <returns>Decision views produced from request history entries.</returns>
    public static IEnumerable<MutationRequestDecisionView> ToDecisionViews(IEnumerable<MutationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests.SelectMany(request => request.Decisions.Select(decision => new MutationRequestDecisionView
        {
            Request = request,
            Decision = decision
        }));
    }
}
