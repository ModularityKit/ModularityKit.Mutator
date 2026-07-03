using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Benchmarks.Governance.Approval.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Approval;

/// <summary>
/// Benchmarks expiration handling for pending governance approvals.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class ApprovalWorkflowExpirationBenchmarks : ApprovalWorkflowBenchmarkBase
{
    /// <summary>
    /// Measures expiration of pending approval request and the resulting rejection bookkeeping.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<IReadOnlyList<MutationRequest>> ExpirePendingApprovals_Sweep()
    {
        var (manager, _) = CreateExpiredWorkflow();

        return await manager.ExpirePendingApprovals(
            DateTimeOffset.UtcNow,
            MutationContext.Service("approval-sweeper", "Expire pending governance approvals"))
            .ConfigureAwait(false);
    }
}
