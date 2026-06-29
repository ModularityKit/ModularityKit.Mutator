using BillingQuotas.Mutations;
using BillingQuotas.State;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;

namespace BillingQuotas.Scenarios;

/// <summary>
/// MonthlyResetScenario
/// ====================
/// 
/// Demonstrates resetting all user quotas to their default value as part of a monthly maintenance operation.
/// This scenario exercises the following capabilities of the Mutators framework:
/// 
/// - <see cref="IMutation{TState}"/> execution in batch mode via <see cref="IMutationEngine.ExecuteBatchAsync"/>
/// - System-level <see cref="MutationContext"/> usage
/// - Policy evaluation and enforcement
/// - Aggregation of mutation results and state updates
/// 
/// The scenario simulates a recurring operation that resets quotas for all users in a <see cref="QuotaState"/>.
/// It ensures that system-level mutations bypass user-specific restrictions where appropriate, and demonstrates
/// how to handle batch results including successes and policy-denied mutations.
/// 
/// Key Steps:
/// 1. Initialize <see cref="QuotaState"/> with example user quotas.
/// 2. Create a system-level <see cref="MutationContext"/> with a reason describing the reset operation.
/// 3. Generate a <see cref="ResetQuotaMutation"/> for each user in the current state.
/// 4. Execute all mutations in a batch via <see cref="IMutationEngine.ExecuteBatchAsync"/>.
/// 5. Aggregate successful results to form the new state.
/// 6. Output the final state and report the number of executed and successful mutations.
/// 
/// Example Use Case:
/// - Monthly quota reset for SaaS billing systems
/// - Automated cleanup or maintenance operations where system-level authority is required
/// 
/// Notes:
/// - Mutations that are blocked by policies (e.g., max/min constraints, two-man approval) are reported but do not prevent
///   the execution of other independent mutations in the batch.
/// - The scenario assumes that <see cref="ResetQuotaMutation"/> sets user quota to a predefined default (e.g., 0 or monthly limit).
/// </summary>
internal static class MonthlyResetScenario
{
    internal static async Task Run(IMutationEngine engine)
    {
        Console.WriteLine("\n=== Monthly Reset Scenario ===");

        var state = new QuotaState
        {
            UserQuotas = new Dictionary<string, int>
            {
                ["alice"] = 50,
                ["bob"] = 75,
                ["carol"] = 20
            }
        };

        var ctx = MutationContext.System(reason: "Monthly reset");

        var mutations = state.UserQuotas.Keys
            .Select(user => new ResetQuotaMutation(user, ctx))
            .ToArray();

        var result = await engine.ExecuteBatchAsync(state, mutations);

        Console.WriteLine($"Executed: {result.Results.Count}");
        Console.WriteLine($"Success: {result.Results.Count(r => r.IsSuccess)}");

        state = result.Results
            .Where(r => r.IsSuccess)
            .Aggregate(state, (s, r) => r.NewState!);

        foreach (var (user, quota) in state.UserQuotas)
            Console.WriteLine($"  {user}: {quota}");
    }
}
