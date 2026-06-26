using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Queries.Materialization;

/// <summary>
/// Projects governed request documents into query specific view models.
/// </summary>
internal static class RedisMutationRequestViewProjector
{
    public static IEnumerable<MutationApprovalView> ToApprovalViews(IEnumerable<MutationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests.SelectMany(request => request.ApprovalRequirements.Select(approval => new MutationApprovalView
        {
            Request = request,
            Approval = approval
        }));
    }

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
