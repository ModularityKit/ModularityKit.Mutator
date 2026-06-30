using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Abstractions.Intent;

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
            Lifecycle = new MutationRequestLifecycleFilter
            {
                PendingReasons = new HashSet<PendingMutationReason> { PendingMutationReason.ExternalCheck }
            }
        }));

        GovernanceQueriesSampleData.PrintSection("Billing Requests");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.QueryAsync(new MutationRequestQuery
        {
            Intent = new MutationRequestIntentFilter
            {
                Categories = new HashSet<string> { "Billing" }
            }
        }));

        GovernanceQueriesSampleData.PrintSection("Requests For tenant-42:roles");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.QueryAsync(new MutationRequestQuery
        {
            Scope = new MutationRequestScopeFilter
            {
                StateIds = new HashSet<string> { "tenant-42:roles" }
            }
        }));

        GovernanceQueriesSampleData.PrintSection("Metadata-classified Security Requests");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.QueryAsync(new MutationRequestQuery
        {
            Intent = new MutationRequestIntentFilter
            {
                Tags = new HashSet<string> { "security" },
                Metadata = new Dictionary<string, object?> { ["risk-owner"] = "platform" },
                MinimumBlastRadiusScope = BlastRadiusScope.Module
            },
            Metadata = new MutationRequestMetadataFilter
            {
                Values = new Dictionary<string, object?> { ["ticket"] = "INC-42" }
            }
        }));

        GovernanceQueriesSampleData.PrintSection("Requests With Actionable Execution Side Effects");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.QueryAsync(new MutationRequestQuery
        {
            SideEffects = new MutationRequestSideEffectFilter
            {
                DataContractTypes = new HashSet<string> { "examples.governance.execution-side-effect" },
                RequiresAction = true
            }
        }));

        GovernanceQueriesSampleData.PrintSection("Recent Approval Driven Requests");
        GovernanceQueriesSampleData.PrintRequests(await queryStore.GetRecentApprovalsAsync(take: 3));
    }
}
