using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Approvals;

namespace Queries.Scenarios;

internal static class ApprovalQueryScenario
{
    public static async Task Run(IMutationRequestQueryStore queryStore)
    {
        GovernanceQueriesSampleData.PrintSection("Pending Approvals For security-lead");
        GovernanceQueriesSampleData.PrintApprovals(await queryStore.GetPendingApprovalsAsync(new MutationApprovalQuery
        {
            ApproverIds = new HashSet<string> { "security-lead" }
        }));
    }
}
