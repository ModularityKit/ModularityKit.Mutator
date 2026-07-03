using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Lifecycle.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Lifecycle.Support;

/// <summary>
/// Shared setup helpers for governance request lifecycle benchmarks.
/// </summary>
public abstract class RequestLifecycleBenchmarkBase
{
    protected const string RequestId = "governance-lifecycle-request";

    protected static (IMutationRequestLifecycleManager Manager, MutationRequest Request) CreateSubmissionWorkflow()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestLifecycleManager(store);
        var request = RequestLifecycleBenchmarkSupport.CreatePendingRequest(RequestId);

        return (manager, request);
    }

    protected static (IMutationRequestLifecycleManager Manager, MutationRequest Request) CreatePendingWorkflow()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestLifecycleManager(store);
        var request = store.Create(RequestLifecycleBenchmarkSupport.CreatePendingRequest(RequestId))
            .GetAwaiter()
            .GetResult();

        return (manager, request);
    }

    protected static (IMutationRequestLifecycleManager Manager, MutationRequest Request) CreateApprovedWorkflow()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestLifecycleManager(store);
        var request = store.Create(RequestLifecycleBenchmarkSupport.CreateApprovedRequest(RequestId))
            .GetAwaiter()
            .GetResult();

        return (manager, request);
    }

    protected static IMutationRequestLifecycleManager CreateExpirationWorkflow()
    {
        var store = new InMemoryMutationRequestStore();
        var manager = new MutationRequestLifecycleManager(store);

        store.Create(RequestLifecycleBenchmarkSupport.CreateExpiredRequest($"{RequestId}-expired"))
            .GetAwaiter()
            .GetResult();
        store.Create(RequestLifecycleBenchmarkSupport.CreatePendingRequest($"{RequestId}-active"))
            .GetAwaiter()
            .GetResult();

        return manager;
    }
}
