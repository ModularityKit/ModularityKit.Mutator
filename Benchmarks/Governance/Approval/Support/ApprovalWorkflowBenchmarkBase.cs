using ModularityKit.Mutator.Governance.Abstractions.Approval.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Approval.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Approval.Support;

/// <summary>
/// Shared setup helpers for governance approval workflow benchmarks.
/// </summary>
public abstract class ApprovalWorkflowBenchmarkBase
{
    protected const string RequestId = "governance-approval-request";

    protected static (IMutationRequestApprovalWorkflowManager Manager, MutationRequest Request) CreateSingleApprovalWorkflow()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = store.Create(ApprovalWorkflowBenchmarkSupport.CreatePendingApprovalRequest(RequestId))
            .GetAwaiter()
            .GetResult();

        return (manager, request);
    }

    protected static (IMutationRequestApprovalWorkflowManager Manager, MutationRequest Request) CreateTwoStepApprovalWorkflow()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = store.Create(ApprovalWorkflowBenchmarkSupport.CreateTwoStepApprovalRequest(RequestId))
            .GetAwaiter()
            .GetResult();

        return (manager, request);
    }

    protected static (IMutationRequestApprovalWorkflowManager Manager, MutationRequest Request) CreateExpiredWorkflow()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestApprovalWorkflowManager(store);
        var request = store.Create(ApprovalWorkflowBenchmarkSupport.CreateExpiredApprovalRequest(RequestId))
            .GetAwaiter()
            .GetResult();

        return (manager, request);
    }
}
