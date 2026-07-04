using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Benchmarks.Governance.Materialization.Support;

/// <summary>
/// Minimal mutation used to produce executed governance output with side effects.
/// </summary>
internal sealed class MaterializeGovernanceOutputMutation(MutationContext context, string nextVersion)
    : IMutation<GovernanceMaterializationState>
{
    /// <summary>
    /// Gets the intent associated with the materialization mutation.
    /// </summary>
    public MutationIntent Intent { get; } = new()
    {
        OperationName = "MaterializeGovernanceOutputMutation",
        Category = "Governance",
        Description = "Produce governed execution output for materialization benchmarks",
        RiskLevel = MutationRiskLevel.Low,
        IsReversible = true
    };

    /// <summary>
    /// Gets the invocation context for the mutation.
    /// </summary>
    public MutationContext Context { get; } = context;

    /// <summary>
    /// Applies the mutation and emits governance side effects.
    /// </summary>
    public MutationResult<GovernanceMaterializationState> Apply(GovernanceMaterializationState state)
    {
        var next = state with
        {
            Value = state.Value + 1,
            Version = nextVersion
        };

        return MutationResult<GovernanceMaterializationState>.Success(
            next,
            ChangeSet.FromChanges(
                StateChange.Modified(nameof(GovernanceMaterializationState.Value), state.Value, next.Value),
                StateChange.Modified(nameof(GovernanceMaterializationState.Version), state.Version, next.Version)),
            CreateSideEffects());
    }

    /// <summary>
    /// Validates the provided state before mutation execution.
    /// </summary>
    public ValidationResult Validate(GovernanceMaterializationState state)
    {
        return state.Value < 0
            ? ValidationResult.WithError(nameof(GovernanceMaterializationState.Value), "Value must be non-negative.")
            : ValidationResult.Success();
    }

    /// <summary>
    /// Simulates the mutation using the same state transition as execution.
    /// </summary>
    public MutationResult<GovernanceMaterializationState> Simulate(GovernanceMaterializationState state) => Apply(state);

    private static IReadOnlyList<SideEffect> CreateSideEffects()
        => [
            SideEffect.Create(
                "GovernanceOutputMaterialized",
                "Governed execution output materialized for history and audit consumers",
                new GovernanceMaterializationSideEffectData("history-audit", 1),
                SideEffectSeverity.Info),
            SideEffect.Critical(
                "GovernanceOutputLinked",
                "Governed execution output linked for downstream request consumers",
                new GovernanceMaterializationSideEffectData("request-link", 2))
        ];
}
