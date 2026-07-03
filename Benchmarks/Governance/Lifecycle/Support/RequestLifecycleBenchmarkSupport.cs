using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Benchmarks.Governance.Lifecycle.Support;

/// <summary>
/// Builds repeatable request lifecycle benchmark fixtures.
/// </summary>
internal static class RequestLifecycleBenchmarkSupport
{
    /// <summary>
    /// Creates a pending request used for lifecycle submission and terminal transition benchmarks.
    /// </summary>
    public static MutationRequest CreatePendingRequest(
        string requestId,
        DateTimeOffset? expiresAt = null)
        => MutationRequestFactory.Pending(
            stateId: "governance-benchmark:lifecycle",
            stateType: "GovernanceState",
            mutationType: "ManageGovernanceMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need lifecycle processing"),
            pendingReason: PendingMutationReason.Approval,
            expectedStateVersion: "v12",
            expiresAt: expiresAt)
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Creates a pending request that is already due for expiration sweeps.
    /// </summary>
    public static MutationRequest CreateExpiredRequest(string requestId)
        => CreatePendingRequest(
            requestId,
            DateTimeOffset.UtcNow.AddMinutes(-5));

    /// <summary>
    /// Creates an approved request used to exercise the lifecycle manager approval path.
    /// </summary>
    public static MutationRequest CreateApprovedRequest(string requestId)
        => MutationRequestFactory.Approved(
            stateId: "governance-benchmark:lifecycle",
            stateType: "GovernanceState",
            mutationType: "ManageGovernanceMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need lifecycle processing"),
            expectedStateVersion: "v12")
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Creates the intent used by request lifecycle benchmark scenarios.
    /// </summary>
    public static MutationIntent CreateIntent()
        => new()
        {
            OperationName = "ManageGovernanceRequest",
            Category = "Governance",
            Description = "Manage governance request lifecycle in benchmark"
        };

    /// <summary>
    /// Creates a decision context used for lifecycle transitions.
    /// </summary>
    public static MutationContext CreateDecisionContext(
        string actorId,
        string actorName,
        string reason)
        => MutationContext.User(actorId, actorName, reason);

    /// <summary>
    /// Creates a sweep context used for expiration benchmarks.
    /// </summary>
    public static MutationContext CreateSweepContext()
        => MutationContext.Service("lifecycle-sweeper", "Expire pending governance requests");
}
