using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Benchmarks.Governance.Approval.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Approval;

/// <summary>
/// Benchmarks approval decision paths in the governance runtime.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class ApprovalWorkflowDecisionBenchmarks : ApprovalWorkflowBenchmarkBase
{
    /// <summary>
    /// Measures single approval requirement being approved and the request being finalized.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<MutationRequest> ApproveRequirement_Granted()
    {
        var (manager, request) = CreateSingleApprovalWorkflow();
        var approvalId = request.ApprovalRequirements[0].ApprovalId;

        return await manager.ApproveRequirement(
            request.RequestId,
            approvalId,
            ApprovalWorkflowBenchmarkSupport.CreateDecisionContext("alice", "Approve governance benchmark request"))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Measures the first approval in two step approval workflow before the request can finalize.
    /// </summary>
    [Benchmark]
    public async Task<MutationRequest> ApproveRequirement_FirstStepPending()
    {
        var (manager, request) = CreateTwoStepApprovalWorkflow();
        var approvalId = request.ApprovalRequirements[0].ApprovalId;

        return await manager.ApproveRequirement(
            request.RequestId,
            approvalId,
            ApprovalWorkflowBenchmarkSupport.CreateDecisionContext("alice", "Approve first step in benchmark workflow"))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Measures the second approval in two step approval workflow that finalizes the request.
    /// </summary>
    [Benchmark]
    public async Task<MutationRequest> ApproveRequirement_FinalizeAfterSecondStep()
    {
        var (manager, request) = CreateTwoStepApprovalWorkflow();
        var firstApprovalId = request.ApprovalRequirements[0].ApprovalId;
        var secondApprovalId = request.ApprovalRequirements[1].ApprovalId;

        var firstApproved = await manager.ApproveRequirement(
            request.RequestId,
            firstApprovalId,
            ApprovalWorkflowBenchmarkSupport.CreateDecisionContext("alice", "Approve first benchmark step"))
            .ConfigureAwait(false);

        return await manager.ApproveRequirement(
            firstApproved.RequestId,
            secondApprovalId,
            ApprovalWorkflowBenchmarkSupport.CreateDecisionContext("bob", "Approve second benchmark step"))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Measures single approval requirement being rejected and the request being terminated.
    /// </summary>
    [Benchmark]
    public async Task<MutationRequest> RejectRequirement_Rejected()
    {
        var (manager, request) = CreateSingleApprovalWorkflow();
        var approvalId = request.ApprovalRequirements[0].ApprovalId;

        return await manager.RejectRequirement(
            request.RequestId,
            approvalId,
            ApprovalWorkflowBenchmarkSupport.CreateDecisionContext("alice", "Reject governance benchmark request"),
            rejection: ApprovalWorkflowBenchmarkSupport.CreateRejectionReason())
            .ConfigureAwait(false);
    }
}
