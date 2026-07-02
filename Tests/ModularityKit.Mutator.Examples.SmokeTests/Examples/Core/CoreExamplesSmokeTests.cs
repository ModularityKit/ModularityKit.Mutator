using ModularityKit.Mutator.Examples.SmokeTests.Support;

namespace ModularityKit.Mutator.Examples.SmokeTests.Examples.Core;

/// <summary>
/// Smoke coverage for the executable samples shipped under <c>Examples/Core</c>.
/// </summary>
public sealed class CoreExamplesSmokeTests
{
    [Fact]
    public Task BillingQuotas_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("BillingQuotas", "Examples/Core/BillingQuotas/BillingQuotas.csproj"));

    [Fact]
    public Task FeatureFlags_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("FeatureFlags", "Examples/Core/FeatureFlags/FeatureFlags.csproj"));

    [Fact]
    public Task IamRoles_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("IamRoles", "Examples/Core/IamRoles/IamRoles.csproj"));

    [Fact]
    public Task WorkflowApprovals_runs_successfully()
        => ExampleSmokeRunner.RunAndAssertAsync(Create("WorkflowApprovals", "Examples/Core/WorkflowApprovals/WorkflowApprovals.csproj"));

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

                if (string.IsNullOrWhiteSpace(result.StandardOutput))
                    return "example did not produce any stdout";

                if (!result.StandardOutput.Contains("METRICS & STATISTICS", StringComparison.Ordinal))
                    return "expected metrics section in stdout";

                if (result.StandardError.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase))
                    return "stderr contains unhandled exception output";

                return null;
            });
}
