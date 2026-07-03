using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Benchmarks.Concurrency.Support;
using ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

namespace ModularityKit.Mutator.Benchmarks.Concurrency;

/// <summary>
/// Benchmarks concurrent batch executions competing for limited runtime availability.
/// </summary>
[BenchmarkCategory("Concurrency")]
[MemoryDiagnoser]
[InProcess]
public class BatchSchedulingBenchmarks
{
    private const int RuntimeSlots = 2;

    private IMutationEngine _engine = null!;
    private BatchScenario[] _scenarios = null!;

    /// <summary>
    /// Controls how many batch executions compete for runtime slots during a single benchmark iteration.
    /// </summary>
    [Params(2, 4)]
    public int ConcurrentBatches { get; set; }

    /// <summary>
    /// Controls how many mutations each competing batch executes.
    /// </summary>
    [Params(4, 16)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Prepares the engine and precomputed batch scenarios for the selected parameters.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _engine = ConcurrencyBenchmarkScenario.BuildEngine(
            RuntimeSlots,
            services =>
            {
                services.AddSingleton<IMutationAuditor, NoOpAuditor>();
                services.AddSingleton<IMutationHistoryStore, NoOpHistoryStore>();
            });

        _scenarios = Enumerable
            .Range(0, ConcurrentBatches)
            .Select(CreateBatchScenario)
            .ToArray();
    }

    /// <summary>
    /// Measures scheduler pressure when several ordered batches compete for a limited number of engine slots.
    /// </summary>
    [Benchmark]
    public async Task ConcurrentBatches_LimitedRuntimeAvailability()
    {
        var tasks = new Task[ConcurrentBatches];

        for (var index = 0; index < ConcurrentBatches; index++)
        {
            var scenario = _scenarios[index];
            tasks[index] = _engine.ExecuteBatchAsync(scenario.Mutations, scenario.State);
        }

        await Task.WhenAll(tasks);
        GC.KeepAlive(tasks);
    }

    private BatchScenario CreateBatchScenario(int batchIndex)
    {
        var stateId = $"batch-state-{batchIndex}";
        var mutations = Enumerable
            .Range(0, BatchSize)
            .Select(step => (IMutation<ConcurrencyState>)ConcurrencyBenchmarkScenario.CreateCommitMutation(stateId, $"batch-{batchIndex}-{step}"))
            .ToArray();

        return new BatchScenario(new ConcurrencyState(batchIndex, 0), mutations);
    }

    private sealed record BatchScenario(
        ConcurrencyState State,
        IReadOnlyList<IMutation<ConcurrencyState>> Mutations);
}
