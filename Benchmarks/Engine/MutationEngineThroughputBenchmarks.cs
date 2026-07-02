using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime;

namespace ModularityKit.Mutator.Benchmarks.Engine;

/// <summary>
/// Benchmarks mutation engine throughput across varying state sizes and batch sizes.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class MutationEngineThroughputBenchmarks
{
    private const string StateId = "throughput-state";

    private IMutationEngine _engine = null!;
    private ThroughputState _singleState = null!;
    private ThroughputState _batchState = null!;
    private ReplaceSlotMutation _singleMutation = null!;
    private IReadOnlyList<IMutation<ThroughputState>> _batchMutations = null!;

    /// <summary>
    /// Controls the number of slots cloned and updated by each benchmarked mutation.
    /// </summary>
    [Params(32, 1024, 16384)]
    public int StateSize { get; set; }

    /// <summary>
    /// Controls how many mutations are executed in the batch throughput scenario.
    /// </summary>
    [Params(8, 64)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Prepares the engine, state snapshots, and mutation lists for the selected benchmark parameters.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _engine = BuildEngine();
        _singleState = CreateState(StateSize);
        _batchState = CreateState(StateSize);
        _singleMutation = CreateMutation(StateSize / 2, 1, "single");
        _batchMutations = [.. Enumerable.Range(0, BatchSize).Select(i => CreateMutation(i % StateSize, (i % 4) + 1, $"batch-{i}"))];
    }

    /// <summary>
    /// Measures single-mutation throughput for the configured state size.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task SingleMutation_Commit_Throughput()
    {
        var result = await _engine.ExecuteAsync(_singleMutation, _singleState);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures batch-mutation throughput for the configured state size and batch size.
    /// </summary>
    [Benchmark]
    public async Task BatchMutation_Commit_Throughput()
    {
        var result = await _engine.ExecuteBatchAsync(_batchMutations, _batchState);
        GC.KeepAlive(result);
    }

    private static IMutationEngine BuildEngine()
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Performance);

        return services
            .BuildServiceProvider()
            .GetRequiredService<IMutationEngine>();
    }

    private static ThroughputState CreateState(int size)
    {
        var values = new int[size];

        for (var index = 0; index < values.Length; index++)
            values[index] = index;

        return new ThroughputState(values, 0);
    }

    private static ReplaceSlotMutation CreateMutation(int slot, int delta, string operationSuffix)
    {
        var context = MutationContext.System("benchmark-throughput")
            with
            {
                StateId = StateId,
                Mode = MutationMode.Commit,
                CorrelationId = $"{StateId}:{operationSuffix}"
            };

        return new ReplaceSlotMutation(context, slot, delta);
    }

    private sealed record ThroughputState(int[] Slots, int Revision);

    private sealed class ReplaceSlotMutation(
        MutationContext context,
        int slot,
        int delta)
        : IMutation<ThroughputState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "ReplaceSlot",
            Category = "Benchmark",
            Description = "Clone state and update a single slot to measure throughput-sensitive execution paths.",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

        public MutationContext Context { get; } = context;

        public MutationResult<ThroughputState> Apply(ThroughputState state)
        {
            var nextSlots = (int[])state.Slots.Clone();
            var before = nextSlots[slot];
            nextSlots[slot] = before + delta;

            var nextState = state with
            {
                Slots = nextSlots,
                Revision = state.Revision + 1
            };

            return MutationResult<ThroughputState>.Success(
                nextState,
                ChangeSet.Single(StateChange.Modified($"Slots[{slot}]", before, nextSlots[slot])));
        }

        public ValidationResult Validate(ThroughputState state)
        {
            var result = ValidationResult.Success();

            if (slot < 0 || slot >= state.Slots.Length)
                result.AddError(nameof(state.Slots), $"Slot index {slot} is outside the benchmark state.");

            return result;
        }

        public MutationResult<ThroughputState> Simulate(ThroughputState state)
            => Apply(state);
    }
}
