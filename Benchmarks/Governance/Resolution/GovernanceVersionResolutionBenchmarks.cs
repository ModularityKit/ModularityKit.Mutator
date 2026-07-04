using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Benchmarks.Governance.Resolution.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Resolution;

/// <summary>
/// Benchmarks governance version resolution paths in the governance runtime.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class GovernanceVersionResolutionBenchmarks
{
    private const string MatchingRequestId = "governance-resolution-matching";
    private const string StaleRequestId = "governance-resolution-stale";
    private const string RevalidateRequestId = "governance-resolution-revalidate";

    private GovernanceVersionResolutionBenchmarkSupport.GovernanceVersionResolutionBenchmarkFixture _matchingFixture = null!;
    private GovernanceVersionResolutionBenchmarkSupport.GovernanceVersionResolutionBenchmarkFixture _staleFixture = null!;
    private GovernanceVersionResolutionBenchmarkSupport.GovernanceVersionResolutionBenchmarkFixture _revalidateFixture = null!;

    /// <summary>
    /// Prepares fresh fixtures for each benchmark iteration.
    /// </summary>
    [IterationSetup]
    public void Setup()
    {
        _matchingFixture = GovernanceVersionResolutionBenchmarkSupport.CreateFixture(
            MatchingRequestId,
            expectedStateVersion: "v10",
            currentStateVersion: "v10");

        _staleFixture = GovernanceVersionResolutionBenchmarkSupport.CreateFixture(
            StaleRequestId,
            expectedStateVersion: "v10",
            currentStateVersion: "v15");

        _revalidateFixture = GovernanceVersionResolutionBenchmarkSupport.CreateFixture(
            RevalidateRequestId,
            expectedStateVersion: "v10",
            currentStateVersion: "v15");
    }

    /// <summary>
    /// Measures version comparison when approved request matches the current state version.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task ResolveApproved_MatchingVersion()
    {
        var result = await _matchingFixture.ResolutionManager.ResolveAndStore(
            _matchingFixture.Request.RequestId,
            currentStateVersion: _matchingFixture.CurrentStateVersion,
            resolutionContext: _matchingFixture.ResolutionContext,
            strategy: VersionedRequestResolutionStrategy.RejectStale).ConfigureAwait(false);

        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures stale request detection and classification during version resolution.
    /// </summary>
    [Benchmark]
    public async Task ResolveApproved_RejectStale()
    {
        var result = await _staleFixture.ResolutionManager.ResolveAndStore(
            _staleFixture.Request.RequestId,
            currentStateVersion: _staleFixture.CurrentStateVersion,
            resolutionContext: _staleFixture.ResolutionContext,
            strategy: VersionedRequestResolutionStrategy.RejectStale).ConfigureAwait(false);

        GC.KeepAlive(result);
    }

    /// <summary>
    /// Measures revalidation-driven request resolution against the latest state version.
    /// </summary>
    [Benchmark]
    public async Task ResolveApproved_Revalidate()
    {
        var result = await _revalidateFixture.ResolutionManager.ResolveAndStore(
            _revalidateFixture.Request.RequestId,
            currentStateVersion: _revalidateFixture.CurrentStateVersion,
            resolutionContext: _revalidateFixture.ResolutionContext,
            strategy: VersionedRequestResolutionStrategy.RevalidateOnLatestState).ConfigureAwait(false);

        GC.KeepAlive(result);
    }
}
