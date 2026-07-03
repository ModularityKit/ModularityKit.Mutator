using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Benchmarks.Results.Support;

namespace ModularityKit.Mutator.Benchmarks.Results;

/// <summary>
/// Benchmarks materialization of history and audit output from an executed mutation result.
/// </summary>
[BenchmarkCategory("Results")]
[MemoryDiagnoser]
[InProcess]
public class MutationOutputMaterializationBenchmarks
{
    private MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState> _result = null!;
    private string _executionId = string.Empty;
    private TimeSpan _duration;

    /// <summary>
    /// Prepares a representative executed mutation result for output materialization benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _result = ResultsBenchmarkSupport.CreateExecutedResult(sideEffectCount: 3, changeCount: 4);
        _executionId = "results-benchmark-execution";
        _duration = TimeSpan.FromMilliseconds(2);
    }

    /// <summary>
    /// Measures materialization of the mutation history entry, including change and side effect copying.
    /// </summary>
    [Benchmark(Baseline = true)]
    public MutationHistoryEntry HistoryEntry_Materialization()
    {
        return new MutationHistoryEntry
        {
            ExecutionId = _executionId,
            StateId = ResultsBenchmarkSupport.StateId,
            Intent = ResultsBenchmarkSupport.CreateIntent(
                "ResultHistoryMaterialization",
                "Materialize history output for benchmark results."),
            Context = ResultsBenchmarkSupport.CreateContext("history"),
            Changes = _result.Changes,
            SideEffects = _result.SideEffects.ToList(),
            Timestamp = DateTimeOffset.UtcNow,
            ExecutionTime = _duration
        };
    }

    /// <summary>
    /// Measures materialization of the audit entry produced from the same executed mutation result.
    /// </summary>
    [Benchmark]
    public MutationAuditEntry AuditEntry_Materialization()
    {
        return new MutationAuditEntry
        {
            ExecutionId = _executionId,
            StateId = ResultsBenchmarkSupport.StateId,
            StateType = nameof(ResultsBenchmarkSupport.ResultBenchmarkState),
            MutationIntent = ResultsBenchmarkSupport.CreateIntent(
                "ResultAuditMaterialization",
                "Materialize audit output for benchmark results."),
            Context = ResultsBenchmarkSupport.CreateContext("audit"),
            Changes = _result.Changes,
            IsSuccess = _result.IsSuccess,
            ErrorMessage = null,
            PolicyDecisions = _result.PolicyDecisions,
            SideEffects = _result.SideEffects,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = _duration
        };
    }
}
