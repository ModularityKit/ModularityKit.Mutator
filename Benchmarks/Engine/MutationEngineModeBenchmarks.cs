using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Benchmarks.Engine;

/// <summary>
/// Benchmarks non-commit engine modes to isolate simulate and validate-only execution overhead.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class MutationEngineModeBenchmarks
{
    private IMutationEngine _strictEngine = null!;
    private MutationEngineBenchmarkSupport.CounterState _state = null!;
    private MutationEngineBenchmarkSupport.IncrementCounterMutation _simulateMutation = null!;
    private MutationEngineBenchmarkSupport.IncrementCounterMutation _validateMutation = null!;

    /// <summary>
    /// Prepares the strict engine and mode-specific mutations used in this suite.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _strictEngine = MutationEngineBenchmarkSupport.BuildEngine(
            MutationEngineOptions.Strict,
            engine => engine.RegisterPolicy(new MutationEngineBenchmarkSupport.AllowAllCounterPolicy()));

        _state = new MutationEngineBenchmarkSupport.CounterState(42);
        _simulateMutation = MutationEngineBenchmarkSupport.CreateCounterMutation(MutationMode.Simulate, "simulate-one");
        _validateMutation = MutationEngineBenchmarkSupport.CreateCounterMutation(MutationMode.Validate, "validate-one");
    }

    /// <summary>
    /// Measures the simulate path with strict engine behavior and one allow policy.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task Simulate_Strict_WithPolicy()
    {
        var result = await _strictEngine.ExecuteAsync(_simulateMutation, _state);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures the validate-only path with strict engine behavior and one allow policy.
    /// </summary>
    [Benchmark]
    public async Task ValidateOnly_Strict_WithPolicy()
    {
        var result = await _strictEngine.ExecuteAsync(_validateMutation, _state);
        GC.KeepAlive(result);
    }
}
