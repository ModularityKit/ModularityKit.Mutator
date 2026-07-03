using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Benchmarks.Governance.Lifecycle.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Lifecycle;

/// <summary>
/// Benchmarks submission of governed requests into the pending lifecycle.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class RequestSubmissionBenchmarks : RequestLifecycleBenchmarkBase
{
    /// <summary>
    /// Measures a pending governed request being submitted and stored.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<MutationRequest> Submit_PendingRequest()
    {
        var (manager, request) = CreateSubmissionWorkflow();

        return await manager.Submit(request).ConfigureAwait(false);
    }
}
