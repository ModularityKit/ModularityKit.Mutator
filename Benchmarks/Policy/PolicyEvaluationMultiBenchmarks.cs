using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Engine;

namespace ModularityKit.Mutator.Benchmarks.Policy;

/// <summary>
/// Benchmarks aggregate overhead for multiple policies evaluated in a single runtime pass.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class PolicyEvaluationMultiBenchmarks
{
    private IMutationEngine _multiPolicyEngine = null!;
    private PolicyBenchmarkSupport.PolicyBenchmarkState _state = null!;
    private PolicyBenchmarkSupport.MinimalPolicyMutation _mutation = null!;

    /// <summary>
    /// Prepares an engine with mixed synchronous and asynchronous allow policies.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _multiPolicyEngine = PolicyBenchmarkSupport.BuildEngine(
            engine =>
            {
                engine.RegisterPolicy(new PolicyBenchmarkSupport.SyncAllowBenchmarkPolicy(priority: 300));
                engine.RegisterPolicy(new PolicyBenchmarkSupport.AsyncAllowBenchmarkPolicy(priority: 200));
                engine.RegisterPolicy(new PolicyBenchmarkSupport.SyncAllowBenchmarkPolicy(priority: 100));
                engine.RegisterPolicy(new PolicyBenchmarkSupport.AsyncAllowBenchmarkPolicy(priority: 0));
            });

        _state = new PolicyBenchmarkSupport.PolicyBenchmarkState("alpha", 42);
        _mutation = PolicyBenchmarkSupport.CreateMutation();
    }

    /// <summary>
    /// Measures the overhead of evaluating several allow policies in priority order.
    /// </summary>
    [Benchmark]
    public async Task MultipleMixedPolicies_Allow()
    {
        var result = await _multiPolicyEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }
}
