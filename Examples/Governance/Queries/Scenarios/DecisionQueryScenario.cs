using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

namespace Queries.Scenarios;

internal static class DecisionQueryScenario
{
    public static async Task Run(IMutationRequestQueryStore queryStore)
    {
        GovernanceQueriesSampleData.PrintSection("Recent Version Resolution Decisions");
        GovernanceQueriesSampleData.PrintDecisions(await queryStore.GetRecentDecisionsAsync(
            MutationRequestDecisionQuery.RecentVersionResolutions(),
            take: 5));

        GovernanceQueriesSampleData.PrintSection("Recent Execution Outcomes");
        GovernanceQueriesSampleData.PrintDecisions(await queryStore.GetRecentDecisionsAsync(
            MutationRequestDecisionQuery.RecentExecutionOutcomes(),
            take: 5));
    }
}
