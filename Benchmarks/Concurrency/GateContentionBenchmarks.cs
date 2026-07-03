using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Benchmarks.Concurrency.Support;
using ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

namespace ModularityKit.Mutator.Benchmarks.Concurrency;

/// <summary>
/// Benchmarks state-level gate contention in the core mutation runtime.
/// </summary>
[BenchmarkCategory("Concurrency")]
[MemoryDiagnoser]
[InProcess]
public class GateContentionBenchmarks
{
    private const string SharedStateId = "shared-concurrency-state";

    private IMutationEngine _engine = null!;
    private ConcurrencyState _state = null!;

    /// <summary>
    /// Prepares an engine with a concurrency limit high enough to isolate state-gate contention.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _engine = ConcurrencyBenchmarkScenario.BuildEngine(
            maxConcurrentMutations: 4,
            configureServices: services =>
            {
                services.AddSingleton<IMutationAuditor, NoOpAuditor>();
                services.AddSingleton<IMutationHistoryStore, NoOpHistoryStore>();
            });

        _state = new ConcurrencyState(42, 0);
    }

    /// <summary>
    /// Measures two concurrent executions targeting the same state identifier while one execution blocks the gate.
    /// </summary>
    [Benchmark]
    public async Task SharedStateGate_TwoConcurrentExecutions()
    {
        using var gate = new BlockingMutationGate();
        var firstMutation = ConcurrencyBenchmarkScenario.CreateBlockingMutation(gate, SharedStateId, "first");
        var secondMutation = ConcurrencyBenchmarkScenario.CreateCommitMutation(SharedStateId, "second");

        var firstTask = _engine.ExecuteAsync(firstMutation, _state);

        if (!gate.WaitForEntries(expectedEntries: 1, timeout: TimeSpan.FromSeconds(5)))
            throw new InvalidOperationException("Blocking benchmark mutation did not enter the gate in time.");

        var secondTask = _engine.ExecuteAsync(secondMutation, _state);

        Thread.SpinWait(100_000);
        gate.Release();

        var results = await Task.WhenAll(firstTask, secondTask);
        GC.KeepAlive(results);
    }
}
