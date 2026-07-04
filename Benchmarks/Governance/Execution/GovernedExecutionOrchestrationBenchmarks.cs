using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Benchmarks.Governance.Execution.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Execution;

/// <summary>
/// Benchmarks governed execution orchestration paths in the governance runtime.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class GovernedExecutionOrchestrationBenchmarks
{
    private const string ApprovedRequestId = "governance-execution-approved";
    private const string StaleRequestId = "governance-execution-stale";
    private const string RevalidateRequestId = "governance-execution-revalidate";

    private GovernedExecutionBenchmarkSupport.GovernedExecutionBenchmarkFixture _approvedFixture = null!;
    private GovernedExecutionBenchmarkSupport.GovernedExecutionBenchmarkFixture _staleFixture = null!;
    private GovernedExecutionBenchmarkSupport.GovernedExecutionBenchmarkFixture _revalidateFixture = null!;

    /// <summary>
    /// Prepares fresh fixtures for each benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void Setup()
    {
        _approvedFixture = GovernedExecutionBenchmarkSupport.CreateFixture(
            ApprovedRequestId,
            currentStateVersion: "v10",
            nextStateVersion: "v11");

        _staleFixture = GovernedExecutionBenchmarkSupport.CreateFixture(
            StaleRequestId,
            currentStateVersion: "v15",
            nextStateVersion: "v16");

        _revalidateFixture = GovernedExecutionBenchmarkSupport.CreateFixture(
            RevalidateRequestId,
            currentStateVersion: "v15",
            nextStateVersion: "v16");
    }

    /// <summary>
    /// Measures an approved request executing through governed orchestration with matching state version.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task ExecuteApproved_MatchingVersion()
    {
        var result = await _approvedFixture.ExecutionManager.ExecuteApproved(
            _approvedFixture.Request.RequestId,
            _approvedFixture.Mutation,
            _approvedFixture.State,
            governanceContext: _approvedFixture.Mutation.Context,
            strategy: VersionedRequestResolutionStrategy.RejectStale).ConfigureAwait(false);

        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures an approved request being rejected as stale before the core engine executes.
    /// </summary>
    [Benchmark]
    public async Task ExecuteApproved_RejectStale()
    {
        var result = await _staleFixture.ExecutionManager.ExecuteApproved(
            _staleFixture.Request.RequestId,
            _staleFixture.Mutation,
            _staleFixture.State,
            governanceContext: _staleFixture.Mutation.Context,
            strategy: VersionedRequestResolutionStrategy.RejectStale).ConfigureAwait(false);

        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures an approved request being revalidated against the latest state and then executed.
    /// </summary>
    [Benchmark]
    public async Task ExecuteApproved_RevalidateAndExecute()
    {
        var result = await _revalidateFixture.ExecutionManager.ExecuteApproved(
            _revalidateFixture.Request.RequestId,
            _revalidateFixture.Mutation,
            _revalidateFixture.State,
            governanceContext: _revalidateFixture.Mutation.Context,
            strategy: VersionedRequestResolutionStrategy.RevalidateOnLatestState).ConfigureAwait(false);

        GC.KeepAlive(result);
    }
}
