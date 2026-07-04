using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Benchmarks.Governance.Materialization.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Materialization;

/// <summary>
/// Benchmarks governance output materialization paths in the governance runtime.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class GovernanceMaterializationOutputBenchmarks
{
    private GovernanceMaterializationBenchmarkSupport.GovernanceMaterializationBenchmarkFixture _fixture = null!;

    /// <summary>
    /// Prepares a representative governed execution result for output materialization benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _fixture = GovernanceMaterializationBenchmarkSupport.CreateFixture();
    }

    /// <summary>
    /// Measures materialization of the history entry, including copied side effects from governed execution.
    /// </summary>
    [Benchmark(Baseline = true)]
    public MutationHistoryEntry HistoryEntry_FromGovernedExecution()
    {
        return new MutationHistoryEntry
        {
            ExecutionId = _fixture.Result.Request.RequestId,
            StateId = _fixture.Result.Request.StateId,
            Intent = _fixture.Mutation.Intent,
            Context = _fixture.Mutation.Context,
            Changes = _fixture.Result.MutationResult!.Changes,
            SideEffects = _fixture.Result.Request.SideEffects.ToList(),
            Timestamp = _fixture.Result.Request.Versioning.ExecutedAt ?? DateTimeOffset.UtcNow,
            ExecutionTime = TimeSpan.FromMilliseconds(2)
        };
    }

    /// <summary>
    /// Measures materialization of the audit entry produced from the same governed execution output.
    /// </summary>
    [Benchmark]
    public MutationAuditEntry AuditEntry_FromGovernedExecution()
    {
        return new MutationAuditEntry
        {
            ExecutionId = _fixture.Result.Request.RequestId,
            StateId = _fixture.Result.Request.StateId,
            StateType = _fixture.Result.Request.StateType,
            MutationIntent = _fixture.Mutation.Intent,
            Context = _fixture.Mutation.Context,
            Changes = _fixture.Result.MutationResult!.Changes,
            IsSuccess = _fixture.Result.MutationResult.IsSuccess,
            ErrorMessage = null,
            PolicyDecisions = _fixture.Result.MutationResult.PolicyDecisions,
            SideEffects = _fixture.Result.Request.SideEffects.ToList(),
            Timestamp = _fixture.Result.Request.Versioning.ExecutedAt ?? DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(2),
            Metadata = new Dictionary<string, object>
            {
                ["ExecutionKind"] = _fixture.Result.ExecutionKind.ToString(),
                ["RequestStatus"] = _fixture.Result.Request.Status.ToString()
            }
        };
    }

    /// <summary>
    /// Measures request decision materialization for downstream consumers.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<MutationRequestDecisionView> DecisionViews_FromGovernedRequest()
    {
        var request = _fixture.Result.Request;
        var views = request.Decisions.Select(decision => new MutationRequestDecisionView
        {
            Request = request,
            Decision = decision
        }).ToList();

        GC.KeepAlive(views);
        return views;
    }
}
