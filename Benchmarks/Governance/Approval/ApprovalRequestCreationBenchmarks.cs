using BenchmarkDotNet.Attributes;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Benchmarks.Governance.Approval.Support;

namespace ModularityKit.Mutator.Benchmarks.Governance.Approval;

/// <summary>
/// Benchmarks approval request creation and assignment overhead.
/// </summary>
[BenchmarkCategory("Governance")]
[MemoryDiagnoser]
[InProcess]
public class ApprovalRequestCreationBenchmarks : ApprovalWorkflowBenchmarkBase
{
    /// <summary>
    /// Measures approval request creation and single approval assignment.
    /// </summary>
    [Benchmark(Baseline = true)]
    public MutationRequest PendingApproval_SingleRequest()
        => ApprovalWorkflowBenchmarkSupport.CreatePendingApprovalRequest(RequestId);

    /// <summary>
    /// Measures approval request creation with two approval steps.
    /// </summary>
    [Benchmark]
    public MutationRequest PendingApproval_TwoStepRequest()
        => ApprovalWorkflowBenchmarkSupport.CreateTwoStepApprovalRequest(RequestId);

    /// <summary>
    /// Measures approval request creation driven by role metadata.
    /// </summary>
    [Benchmark]
    public MutationRequest PendingApproval_RoleBasedRequest()
        => ApprovalWorkflowBenchmarkSupport.CreateRoleBasedApprovalRequest(RequestId);
}
