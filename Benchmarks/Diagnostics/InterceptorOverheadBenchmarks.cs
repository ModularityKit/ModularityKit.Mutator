using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics;

/// <summary>
/// Benchmarks interceptor overhead in the core mutation pipeline independently from audit and history storage.
/// </summary>
[BenchmarkCategory("Diagnostics")]
[MemoryDiagnoser]
[InProcess]
public class InterceptorOverheadBenchmarks
{
    private IMutationEngine _baselineEngine = null!;
    private IMutationEngine _interceptorEngine = null!;
    private DiagnosticsState _state = null!;
    private DiagnosticsMutation _mutation = null!;

    /// <summary>
    /// Prepares engines with and without a passive interceptor while disabling audit and history storage noise.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _baselineEngine = DiagnosticsBenchmarkScenario.BuildEngine(
            services =>
            {
                services.AddSingleton<IMutationAuditor, NoOpAuditor>();
                services.AddSingleton<IMutationHistoryStore, NoOpHistoryStore>();
            });

        _interceptorEngine = DiagnosticsBenchmarkScenario.BuildEngine(
            services =>
            {
                services.AddSingleton<IMutationAuditor, NoOpAuditor>();
                services.AddSingleton<IMutationHistoryStore, NoOpHistoryStore>();
            },
            engine => engine.RegisterInterceptor(new PassiveBenchmarkInterceptor()));

        _state = new DiagnosticsState(42, "baseline");
        _mutation = DiagnosticsBenchmarkScenario.CreateCommitMutation("interceptor");
    }

    /// <summary>
    /// Measures the commit pipeline without interceptors, audit persistence, or history persistence.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task NoInterceptor_Baseline()
    {
        var result = await _baselineEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures the same commit pipeline with one passive interceptor enabled.
    /// </summary>
    [Benchmark]
    public async Task PassiveInterceptor_Enabled()
    {
        var result = await _interceptorEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }
}
