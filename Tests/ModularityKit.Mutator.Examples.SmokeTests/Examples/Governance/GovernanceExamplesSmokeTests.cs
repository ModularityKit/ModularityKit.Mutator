using ModularityKit.Mutator.Examples.SmokeTests.Support;

namespace ModularityKit.Mutator.Examples.SmokeTests.Examples.Governance;

/// <summary>
/// Smoke coverage for the executable samples shipped under <c>Examples/Governance</c>.
/// </summary>
public sealed class GovernanceExamplesSmokeTests
{
    [Fact]
    public Task ApprovalWorkflow_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("ApprovalWorkflow", "Examples/Governance/ApprovalWorkflow/ApprovalWorkflow.csproj"));

    [Fact]
    public Task GovernedExecution_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("GovernedExecution", "Examples/Governance/GovernedExecution/GovernedExecution.csproj"));

    [Fact]
    public Task DecisionTaxonomy_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("DecisionTaxonomy", "Examples/Governance/DecisionTaxonomy/DecisionTaxonomy.csproj"));

    [Fact]
    public Task Queries_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("Queries", "Examples/Governance/Queries/Queries.csproj"));

    [Fact]
    public Task RedisQueries_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(CreateRedis());

    [Fact]
    public Task RequestLifecycle_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("RequestLifecycle", "Examples/Governance/RequestLifecycle/RequestLifecycle.csproj"));

    [Fact]
    public Task VersionedResolution_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("VersionedResolution", "Examples/Governance/VersionedResolution/VersionedResolution.csproj"));

    private static ExampleSmokeCase Create(string name, string projectPath)
        => new(
            name,
            projectPath,
            result =>
            {
                if (result.TimedOut)
                    return "process timed out";

                if (result.ExitCode != 0)
                    return $"expected exit code 0 but got {result.ExitCode}";

                if (string.IsNullOrWhiteSpace(result.StandardOutput) && string.IsNullOrWhiteSpace(result.StandardError))
                    return "example did not produce any output";

                if (result.StandardError.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase))
                    return "stderr contains unhandled exception output";

                return null;
            });

    private static ExampleSmokeCase CreateRedis()
        => new(
            "RedisQueries",
            "Examples/Governance/RedisQueries/RedisQueries.csproj",
            result =>
            {
                if (result.TimedOut)
                    return "process timed out";

                if (result.ExitCode != 0)
                    return $"expected exit code 0 but got {result.ExitCode}";

                var hasRedisOutput = result.StandardOutput.Contains("Pending Approval Queue", StringComparison.Ordinal)
                    && result.StandardOutput.Contains("Recent Execution Outcomes", StringComparison.Ordinal);

                var hasExpectedDependencyWarning =
                    result.StandardError.Contains("Could not connect to Redis", StringComparison.Ordinal)
                    && result.StandardError.Contains("Start Redis locally or set MODULARITYKIT_REDIS to a reachable endpoint.", StringComparison.Ordinal);

                if (!hasRedisOutput && !hasExpectedDependencyWarning)
                    return "expected either Redis query output or the documented Redis prerequisite warning";

                if (result.StandardError.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase))
                    return "stderr contains unhandled exception output";

                return null;
            },
            new Dictionary<string, string?>
            {
                ["MODULARITYKIT_REDIS"] = "localhost:6379,connectTimeout=1000,abortConnect=false,syncTimeout=1000"
            });
}
