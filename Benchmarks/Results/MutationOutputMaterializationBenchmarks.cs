using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Benchmarks.Results.Support;

namespace ModularityKit.Mutator.Benchmarks.Results;

[BenchmarkCategory("Results")]
[MemoryDiagnoser]
[InProcess]
public class MutationOutputMaterializationBenchmarks
{
    private MutationResult<ResultsBenchmarkSupport.ResultBenchmarkState> _result = default!;
    private string _executionId = string.Empty;
    private TimeSpan _duration;
    private IReadOnlyList<SideEffect> _sideEffectList = null!;
    private MutationIntent _historyIntent = null!;
    private MutationIntent _auditIntent = null!;
    private MutationContext _historyContext = null!;
    private MutationContext _auditContext = null!;

    [GlobalSetup]
    public void Setup()
    {
        _result = ResultsBenchmarkSupport.CreateExecutedResult(sideEffectCount: 3, changeCount: 4);
        _executionId = "results-benchmark-execution";
        _duration = TimeSpan.FromMilliseconds(2);
        _sideEffectList = _result.SideEffects.ToList();
        _historyIntent = ResultsBenchmarkSupport.CreateIntent(
            "ResultHistoryMaterialization",
            "Materialize history output for benchmark results.");
        _auditIntent = ResultsBenchmarkSupport.CreateIntent(
            "ResultAuditMaterialization",
            "Materialize audit output for benchmark results.");
        _historyContext = ResultsBenchmarkSupport.CreateContext("history");
        _auditContext = ResultsBenchmarkSupport.CreateContext("audit");
    }

    [Benchmark(Baseline = true)]
    public MutationHistoryEntry HistoryEntry_Materialization()
    {
        return new MutationHistoryEntry
        {
            ExecutionId = _executionId,
            StateId = ResultsBenchmarkSupport.StateId,
            Intent = _historyIntent,
            Context = _historyContext,
            Changes = _result.Changes,
            SideEffects = _sideEffectList,
            Timestamp = DateTimeOffset.UtcNow,
            ExecutionTime = _duration
        };
    }

    [Benchmark]
    public MutationAuditEntry AuditEntry_Materialization()
    {
        return new MutationAuditEntry
        {
            ExecutionId = _executionId,
            StateId = ResultsBenchmarkSupport.StateId,
            StateType = "ResultBenchmarkState",
            MutationIntent = _auditIntent,
            Context = _auditContext,
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
