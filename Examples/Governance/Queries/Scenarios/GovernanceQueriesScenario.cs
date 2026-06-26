namespace Queries.Scenarios;

internal static class GovernanceQueriesScenario
{
    public static async Task Run()
    {
        var store = await GovernanceQueriesSampleData.CreateStoreAsync();

        await RequestQueryScenario.Run(store);
        await ApprovalQueryScenario.Run(store);
        await DecisionQueryScenario.Run(store);
    }
}
