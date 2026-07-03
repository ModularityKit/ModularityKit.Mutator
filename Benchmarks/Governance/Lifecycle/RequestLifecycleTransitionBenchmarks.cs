using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Benchmarks.Governance.Lifecycle.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Lifecycle;

/// <summary>
/// Benchmarks lifecycle transitions on governed requests.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class RequestLifecycleTransitionBenchmarks : RequestLifecycleBenchmarkBase
{
    /// <summary>
    /// Measures a pending request being approved through the lifecycle manager.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<MutationRequest> Approve_PendingRequest()
    {
        var (manager, request) = CreatePendingWorkflow();

        return await manager.Approve(
            request.RequestId,
            RequestLifecycleBenchmarkSupport.CreateDecisionContext("approver", "Approver", "Approve lifecycle request"))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Measures a pending request being rejected through the lifecycle manager.
    /// </summary>
    [Benchmark]
    public async Task<MutationRequest> Reject_PendingRequest()
    {
        var (manager, request) = CreatePendingWorkflow();

        return await manager.Reject(
            request.RequestId,
            RequestLifecycleBenchmarkSupport.CreateDecisionContext("reviewer", "Reviewer", "Reject lifecycle request"))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Measures a pending request being cancelled through the lifecycle manager.
    /// </summary>
    [Benchmark]
    public async Task<MutationRequest> Cancel_PendingRequest()
    {
        var (manager, request) = CreatePendingWorkflow();

        return await manager.Cancel(
            request.RequestId,
            RequestLifecycleBenchmarkSupport.CreateDecisionContext("operator", "Operator", "Cancel lifecycle request"))
            .ConfigureAwait(false);
    }
}
