using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Benchmarks.Engine;

/// <summary>
/// Benchmarks batch commit execution overhead for the performance-oriented engine path.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class MutationEngineBatchBenchmarks
{
    private IMutationEngine _performanceEngine = null!;
    private MutationEngineBenchmarkSupport.CounterState _state = null!;
    private IReadOnlyList<IMutation<MutationEngineBenchmarkSupport.CounterState>> _batchMutations = null!;

    /// <summary>
    /// Controls how many commit mutations are executed in a single batch benchmark iteration.
    /// </summary>
    [Params(10, 100)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Prepares the engine, base state, and batch mutation list for the selected batch size.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _performanceEngine = MutationEngineBenchmarkSupport.BuildEngine(MutationEngineOptions.Performance);
        _state = new MutationEngineBenchmarkSupport.CounterState(42);
        _batchMutations = [.. Enumerable.Range(0, BatchSize)
            .Select(i => MutationEngineBenchmarkSupport.CreateCounterMutation(MutationMode.Commit, $"batch-{i}"))];
    }

    /// <summary>
    /// Measures sequential batch commit execution without policy pressure.
    /// </summary>
    [Benchmark]
    public async Task Batch_Commit_Performance_NoPolicy()
    {
        var result = await _performanceEngine.ExecuteBatchAsync(_batchMutations, _state);
        GC.KeepAlive(result);
    }
}
