using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Benchmarks.Governance.Lifecycle.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Lifecycle;

/// <summary>
/// Benchmarks expiration sweeps for governed requests.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class RequestLifecycleExpirationBenchmarks : RequestLifecycleBenchmarkBase
{
    /// <summary>
    /// Measures expiration of due requests in the pending lifecycle.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<IReadOnlyList<MutationRequest>> ExpireDueRequests_Sweep()
    {
        var manager = CreateExpirationWorkflow();

        return await manager.ExpireDueRequests(
            DateTimeOffset.UtcNow,
            RequestLifecycleBenchmarkSupport.CreateSweepContext())
            .ConfigureAwait(false);
    }
}
