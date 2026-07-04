using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Benchmarks.Governance.Queries.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Queries;

/// <summary>
/// Benchmarks governance query and read paths in the governance runtime.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class GovernanceQueryReadBenchmarks
{
    private GovernanceQueryReadBenchmarkSupport.GovernanceQueryReadBenchmarkFixture _fixture = null!;

    /// <summary>
    /// Prepares a seeded query store for the read benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _fixture = GovernanceQueryReadBenchmarkSupport.CreateFixture();
    }

    /// <summary>
    /// Measures listing pending governance requests.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task GetPendingRequests()
    {
        var result = await _fixture.RequestStore.GetPendingRequestsAsync(
            MutationRequestQueries.Pending()).ConfigureAwait(false);

        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures listing pending approval queue entries.
    /// </summary>
    [Benchmark]
    public async Task GetPendingApprovalQueue()
    {
        var result = await _fixture.ApprovalStore.GetPendingApprovalQueueAsync(
            MutationRequestQueries.PendingApprovalQueue()).ConfigureAwait(false);

        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures reading recent execution outcome decisions.
    /// </summary>
    [Benchmark]
    public async Task GetRecentExecutionOutcomes()
    {
        var result = await _fixture.DecisionStore.GetRecentDecisionsAsync(
            MutationRequestDecisionQuery.RecentExecutionOutcomes(),
            take: 64).ConfigureAwait(false);

        GC.KeepAlive(result);
    }
}
