using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Benchmarks.Engine;

/// <summary>
/// Benchmarks single commit execution for the core mutation engine with and without policy evaluation.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class MutationEngineCommitBenchmarks
{
    private IMutationEngine _performanceEngine = null!;
    private IMutationEngine _strictEngine = null!;
    private MutationEngineBenchmarkSupport.CounterState _state = null!;
    private MutationEngineBenchmarkSupport.IncrementCounterMutation _commitMutation = null!;

    /// <summary>
    /// Prepares the benchmark engines, state snapshot, and commit mutation instance.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _performanceEngine = MutationEngineBenchmarkSupport.BuildEngine(MutationEngineOptions.Performance);
        _strictEngine = MutationEngineBenchmarkSupport.BuildEngine(
            MutationEngineOptions.Strict,
            engine => engine.RegisterPolicy(new MutationEngineBenchmarkSupport.AllowAllCounterPolicy()));

        _state = new MutationEngineBenchmarkSupport.CounterState(42);
        _commitMutation = MutationEngineBenchmarkSupport.CreateCounterMutation(Abstractions.Context.MutationMode.Commit, "commit-one");
    }

    /// <summary>
    /// Measures a commit execution through the performance-oriented runtime path without policies.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task Commit_Performance_NoPolicy()
    {
        var result = await _performanceEngine.ExecuteAsync(_commitMutation, _state);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures a commit execution through the strict runtime path with one allow policy.
    /// </summary>
    [Benchmark]
    public async Task Commit_Strict_WithPolicy()
    {
        var result = await _strictEngine.ExecuteAsync(_commitMutation, _state);
        GC.KeepAlive(result);
    }
}
