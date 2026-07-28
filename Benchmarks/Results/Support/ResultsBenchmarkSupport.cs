using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Benchmarks.Results.Support;

/// <summary>
/// Shared support types for result materialization benchmark scenarios.
/// </summary>
public static class ResultsBenchmarkSupport
{
    /// <summary>
    /// Gets the shared state identifier used by result benchmark cases.
    /// </summary>
    public const string StateId = "results-benchmark-state";

    /// <summary>
    /// Creates a reusable mutation context for result benchmarks.
    /// </summary>
    /// <param name="operationSuffix">The suffix used to distinguish benchmark cases.</param>
    /// <returns>A system mutation context bound to the shared benchmark state.</returns>
    public static MutationContext CreateContext(string operationSuffix)
    {
        return MutationContext.System("results-benchmark")
            with
            {
                StateId = StateId,
                Mode = MutationMode.Commit,
                CorrelationId = $"{StateId}:{operationSuffix}"
            };
    }

    /// <summary>
    /// Creates a reusable mutation intent for result benchmarks.
    /// </summary>
    /// <param name="operationName">The operation name reported by the mutation.</param>
    /// <param name="description">The human-readable description.</param>
    /// <returns>A benchmark mutation intent.</returns>
    public static MutationIntent CreateIntent(string operationName, string description)
        => new()
        {
            OperationName = operationName,
            Category = "Benchmark",
            Description = description,
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

    /// <summary>
    /// Creates a change set with a stable revision marker and a configurable number of appended slot updates.
    /// </summary>
    /// <param name="revision">The revision number before mutation.</param>
    /// <param name="updates">The number of slot updates to include.</param>
    /// <returns>A populated change set.</returns>
    public static ChangeSet CreateChangeSet(int revision, int updates)
    {
        var changes = new List<StateChange>(updates + 1)
        {
            StateChange.Modified(nameof(ResultBenchmarkState.Revision), revision, revision + 1)
        };

        for (var index = 0; index < updates; index++)
        {
            changes.Add(StateChange.Modified(
                $"Slots[{index}]",
                index,
                index + 1));
        }

        return ChangeSet.FromChanges([.. changes]);
    }

    /// <summary>
    /// Creates a fixed list of side effects with predictable payload shape.
    /// </summary>
    /// <param name="count">The number of side effects to create.</param>
    /// <returns>A read-only side effect list.</returns>
    public static IReadOnlyList<SideEffect> CreateSideEffects(int count)
    {
        var sideEffects = new List<SideEffect>(count);

        for (var index = 0; index < count; index++)
        {
            sideEffects.Add(SideEffect.Create(
                "ResultMaterialization",
                $"Side effect #{index}",
                new SideEffectPayload(index, $"payload-{index}"),
                SideEffectSeverity.Info));
        }

        return sideEffects;
    }

    /// <summary>
    /// Creates a reusable mutation result used as the input for output materialization benchmarks.
    /// </summary>
    /// <param name="sideEffectCount">The number of side effects to attach to the result.</param>
    /// <param name="changeCount">The number of slot changes to include in the result.</param>
    /// <returns>A mutation result prepopulated with changes and side effects.</returns>
    public static MutationResult<ResultBenchmarkState> CreateExecutedResult(
        int sideEffectCount,
        int changeCount)
    {
        var state = new ResultBenchmarkState(42, 0);
        var nextState = state with { Revision = state.Revision + 1 };

        return MutationResult<ResultBenchmarkState>.Success(
            nextState,
            CreateChangeSet(state.Revision, changeCount),
            CreateSideEffects(sideEffectCount));
    }

    /// <summary>
    /// Minimal state used by result benchmark scenarios.
    /// </summary>
    /// <param name="Revision">The revision counter advanced on each benchmark mutation.</param>
    /// <param name="Value">The mutable numeric value exercised by the benchmark mutation.</param>
    public readonly record struct ResultBenchmarkState(int Revision, int Value);

    /// <summary>
    /// Typed payload used to give side effects realistic materialization shape.
    /// </summary>
    /// <param name="Index">The ordinal of the side effect.</param>
    /// <param name="Token">A stable payload token.</param>
    public readonly record struct SideEffectPayload(int Index, string Token);
}
