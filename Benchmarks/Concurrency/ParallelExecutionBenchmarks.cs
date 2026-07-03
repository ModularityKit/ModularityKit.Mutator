using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Benchmarks.Concurrency.Support;
using ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

namespace ModularityKit.Mutator.Benchmarks.Concurrency;

/// <summary>
/// Benchmarks parallel execution throughput across distinct runtime state identifiers.
/// </summary>
[BenchmarkCategory("Concurrency")]
[MemoryDiagnoser]
[InProcess]
public class ParallelExecutionBenchmarks
{
    private IMutationEngine _engine = null!;
    private ConcurrencyState[] _states = null!;
    private IncrementConcurrencyMutation[] _mutations = null!;

    /// <summary>
    /// Controls how many distinct mutation executions run in parallel during a single benchmark iteration.
    /// </summary>
    [Params(2, 8)]
    public int Parallelism { get; set; }

    /// <summary>
    /// Prepares the engine, state snapshots, and mutation list for the selected parallelism level.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _engine = ConcurrencyBenchmarkScenario.BuildEngine(
            Parallelism,
            services =>
            {
                services.AddSingleton<IMutationAuditor, NoOpAuditor>();
                services.AddSingleton<IMutationHistoryStore, NoOpHistoryStore>();
            });

        _states = Enumerable
            .Range(0, Parallelism)
            .Select(index => new ConcurrencyState(index, 0))
            .ToArray();

        _mutations = Enumerable
            .Range(0, Parallelism)
            .Select(index => ConcurrencyBenchmarkScenario.CreateCommitMutation($"parallel-state-{index}", $"parallel-{index}"))
            .ToArray();
    }

    /// <summary>
    /// Measures concurrent execution across distinct state identifiers without diagnostics storage noise.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task ParallelDistinctStates_ExecuteAsync()
    {
        var tasks = new Task[Parallelism];

        for (var index = 0; index < Parallelism; index++)
            tasks[index] = _engine.ExecuteAsync(_mutations[index], _states[index]);

        await Task.WhenAll(tasks);
        GC.KeepAlive(tasks);
    }
}
