using IamRoles.Mutations;
using IamRoles.State;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;

namespace IamRoles.Scenarios;

/// <summary>
/// BatchRoleMigrationScenario
/// 
/// Demonstrates batch migration of IAM roles for multiple users using <see cref="IMutation{TState}"/> 
/// and <see cref="GrantUserRoleMutation"/> in a single operation.
/// 
/// Scenario Details:
/// - Executes multiple <see cref="IMutationEngine.ExecuteBatchAsync{TState}"/> instances in a batch via <see cref="IMutationEngine"/>.
/// - Provides a <see cref="UserPermissionsState"/> with system metadata, including approval info for auditability.
/// - Logs success and failure statistics for each mutation.
/// - Handles policy decisions for each mutation, such as restrictions on role assignments.
/// - Works with <see cref="IMutation{TState}"/> which tracks roles assigned to users.
/// 
/// Key Steps:
/// 1. Initialize <see cref="IMutationEngine.ExecuteBatchAsync{TState}"/> with initial roles for each user.
/// 2. Construct an array of <see cref="IMutationEngine"/> representing role grants.
/// 3. Execute the batch with <see cref="IMutationEngine"/>.
/// 4. Count successes and failures, and display any policy decisions that blocked mutations.
/// 
/// Example Use Case:
/// - Performing organizational role restructuring
/// - Batch granting of elevated permissions to multiple users
/// - Ensuring compliance and auditability via system metadata and policy evaluation
/// 
/// Notes:
/// - Individual mutations in the batch may fail if any policy denies the role change.
/// - Correlation ID or metadata can be used to trace this batch operation in logs.
/// </summary>
internal static class BatchRoleMigrationScenario
{
    internal static async Task Run(IMutationEngine engine)
    {
        Console.WriteLine("\n=== Batch Role Migration Scenario ===");

        var state = new UserPermissionsState
        {
            RolesByUser = new Dictionary<string, HashSet<string>>
            {
                ["alice"] = ["User"],
                ["bob"]   = ["User"],
                ["carol"] = ["User"]
            }
        };

        var ctx = MutationContext.System(reason: "Org restructure")
            with
            {
                Metadata = new Dictionary<string, object>
                {
                    ["approvedBy"] = "security-team"
                }
            };

        var mutations = new IMutation<UserPermissionsState>[]
        {
            new GrantUserRoleMutation("alice", "Manager", ctx),
            new GrantUserRoleMutation("bob", "Admin", ctx),
            new GrantUserRoleMutation("carol", "Manager", ctx)
        };

        var result = await engine.ExecuteBatchAsync(state, mutations);

        Console.WriteLine($"Executed: {result.Results.Count}");
        Console.WriteLine($"Success: {result.Results.Count(r => r.IsSuccess)}");
        Console.WriteLine($"Failed:  {result.Results.Count(r => !r.IsSuccess)}");

        foreach (var failure in result.Results.Where(r => !r.IsSuccess))
        {
            Console.WriteLine("✗ Mutation failed:");
            foreach (var decision in failure.PolicyDecisions)
                Console.WriteLine($"  Policy: {decision.PolicyName} – {decision.Reason}");
        }
    }
}
