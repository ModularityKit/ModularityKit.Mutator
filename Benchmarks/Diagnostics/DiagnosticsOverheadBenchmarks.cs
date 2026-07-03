using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics;

/// <summary>
/// Benchmarks audit, history, and logging-style overhead in the core mutation pipeline.
/// </summary>
[BenchmarkCategory("Diagnostics")]
[MemoryDiagnoser]
[InProcess]
public class DiagnosticsOverheadBenchmarks
{
    private IMutationEngine _noDiagnosticsEngine = null!;
    private IMutationEngine _auditHistoryEngine = null!;
    private IMutationEngine _combinedDiagnosticsEngine = null!;
    private DiagnosticsState _state = null!;
    private DiagnosticsMutation _mutation = null!;

    /// <summary>
    /// Prepares baseline, audit-history, and combined observability benchmark engines.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _noDiagnosticsEngine = DiagnosticsBenchmarkScenario.BuildEngine(
            services =>
            {
                services.AddSingleton<IMutationAuditor, NoOpAuditor>();
                services.AddSingleton<IMutationHistoryStore, NoOpHistoryStore>();
            });

        _auditHistoryEngine = DiagnosticsBenchmarkScenario.BuildEngine();

        _combinedDiagnosticsEngine = DiagnosticsBenchmarkScenario.BuildEngine(
            configureEngine: engine =>
            {
                engine.RegisterInterceptor(new PassiveBenchmarkInterceptor());
                engine.RegisterInterceptor(new FormattingLoggingInterceptor());
            });

        _state = new DiagnosticsState(42, "baseline");
        _mutation = DiagnosticsBenchmarkScenario.CreateCommitMutation("diagnostics");
    }

    /// <summary>
    /// Measures commit execution with observability paths disabled via no-op audit and history services.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task NoDiagnostics_Baseline()
    {
        var result = await _noDiagnosticsEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures commit execution with the default audit and history capture path enabled.
    /// </summary>
    [Benchmark]
    public async Task AuditHistory_Enabled()
    {
        var result = await _auditHistoryEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures commit execution with audit/history capture plus interceptor and logging-style formatting enabled.
    /// </summary>
    [Benchmark]
    public async Task CombinedInterceptionAndDiagnostics_Enabled()
    {
        var result = await _combinedDiagnosticsEngine.ExecuteAsync(_mutation, _state);
        GC.KeepAlive(result);
    }
}
