using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;

namespace Queries.Scenarios;

internal static class RequestQueryScenario
{
    public static async Task Run(IMutationRequestQueryStore queryStore)
    {
        GovernanceQueriesSampleData.PrintSection("Pending Approval Queue");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.GetPendingApprovalQueueAsync());

        GovernanceQueriesSampleData.PrintSection("Pending External Check Requests");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.GetPendingRequestsAsync(new MutationRequestQuery
        {
            PendingReasons = new HashSet<PendingMutationReason> { PendingMutationReason.ExternalCheck }
        }));

        GovernanceQueriesSampleData.PrintSection("Billing Requests");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.QueryAsync(new MutationRequestQuery
        {
            Categories = new HashSet<string> { "Billing" }
        }));

        GovernanceQueriesSampleData.PrintSection("Requests For tenant-42:roles");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.QueryAsync(new MutationRequestQuery
        {
            StateIds = new HashSet<string> { "tenant-42:roles" }
        }));

        GovernanceQueriesSampleData.PrintSection("Recent Approval Driven Requests");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.GetRecentApprovalsAsync(take: 3));
    }
}
