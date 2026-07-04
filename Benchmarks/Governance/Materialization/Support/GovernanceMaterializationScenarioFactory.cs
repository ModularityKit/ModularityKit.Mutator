using ModularityKit.Mutator.Abstractions.Intent;

namespace ModularityKit.Mutator.Benchmarks.Governance.Materialization.Support;

/// <summary>
/// Creates governance materialization benchmark intent metadata.
/// </summary>
internal static class GovernanceMaterializationScenarioFactory
{
    /// <summary>
    /// Creates the intent used by governance materialization scenarios.
    /// </summary>
    public static MutationIntent CreateIntent()
        => new()
        {
            OperationName = "MaterializeGovernanceOutput",
            Category = "Governance",
            Description = "Materialize governance output for audit, history, and downstream consumers",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };
}
