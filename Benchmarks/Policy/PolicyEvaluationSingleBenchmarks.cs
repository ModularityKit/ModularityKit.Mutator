using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Benchmarks.Policy;

/// <summary>
/// Benchmarks single-policy evaluation overhead against a no-policy baseline.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class PolicyEvaluationSingleBenchmarks
{
    private IMutationEngine _baselineEngine = null!;
    private IMutationEngine _syncPolicyEngine = null!;
    private IMutationEngine _asyncPolicyEngine = null!;
    private PolicyBenchmarkSupport.PolicyBenchmarkState _state = null!;
    private PolicyBenchmarkSupport.MinimalPolicyMutation _mutation = null!;

    /// <summary>
    /// Prepares the baseline, synchronous-policy, and asynchronous-policy engines.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _baselineEngine = PolicyBenchmarkSupport.BuildEngine();
        _syncPolicyEngine = PolicyBenchmarkSupport.BuildEngine(
            engine => engine.RegisterPolicy(new PolicyBenchmarkSupport.SyncAllowBenchmarkPolicy(priority: 100)));
        _asyncPolicyEngine = PolicyBenchmarkSupport.BuildEngine(
            engine => engine.RegisterPolicy(new PolicyBenchmarkSupport.AsyncAllowBenchmarkPolicy(priority: 100)));
        _state = new PolicyBenchmarkSupport.PolicyBenchmarkState("alpha", 42);
        _mutation = PolicyBenchmarkSupport.CreateMutation();
    }

    /// <summary>
    /// Measures the execution baseline without any registered policies.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task NoPolicy_Baseline()
    {
        var result = await _baselineEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures the overhead of one synchronous allow policy.
    /// </summary>
    [Benchmark]
    public async Task SingleSyncPolicy_Allow()
    {
        var result = await _syncPolicyEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures the overhead of one asynchronous allow policy.
    /// </summary>
    [Benchmark]
    public async Task SingleAsyncPolicy_Allow()
    {
        var result = await _asyncPolicyEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }
}
