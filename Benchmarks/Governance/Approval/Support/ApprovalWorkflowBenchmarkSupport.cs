using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Benchmarks.Governance.Approval.Support;

/// <summary>
/// Builds repeatable approval workflow benchmark fixtures.
/// </summary>
internal static class ApprovalWorkflowBenchmarkSupport
{
    /// <summary>
    /// Creates a pending approval request with one approval requirement.
    /// </summary>
    public static MutationRequest CreatePendingApprovalRequest(
        string requestId,
        DateTimeOffset? expiresAt = null)
        => MutationRequestFactory.PendingApproval(
            stateId: "governance-benchmark:approval",
            stateType: "GovernanceState",
            mutationType: "ApproveGovernanceMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need approval"),
            requirements:
            [
                PolicyRequirement.Approval("alice", "Manager approval")
            ],
            expectedStateVersion: "v10",
            expiresAt: expiresAt)
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Creates a pending approval request with two sequential approval requirements.
    /// </summary>
    public static MutationRequest CreateTwoStepApprovalRequest(string requestId)
        => MutationRequestFactory.PendingApproval(
            stateId: "governance-benchmark:approval",
            stateType: "GovernanceState",
            mutationType: "ApproveGovernanceMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need approval"),
            requirements:
            [
                PolicyRequirement.Approval("alice", "Manager approval"),
                PolicyRequirement.Approval("bob", "Security approval")
            ],
            expectedStateVersion: "v10")
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Creates a request that already has an expired approval requirement.
    /// </summary>
    public static MutationRequest CreateExpiredApprovalRequest(string requestId)
        => CreatePendingApprovalRequest(
            requestId,
            DateTimeOffset.UtcNow.AddMinutes(-5));

    /// <summary>
    /// Creates a pending approval request driven by role metadata.
    /// </summary>
    public static MutationRequest CreateRoleBasedApprovalRequest(string requestId)
        => MutationRequestFactory.PendingApproval(
            stateId: "governance-benchmark:approval-role",
            stateType: "GovernanceState",
            mutationType: "ApproveGovernanceMutation",
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need role-based approval"),
            requirements:
            [
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Role approval",
                    Data = new
                    {
                        ApproverRole = "security-approver",
                        StepOrder = 1,
                        Reason = "Role-based sign-off"
                    }
                }
            ],
            expectedStateVersion: "v10")
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Creates the intent used by approval workflow benchmark scenarios.
    /// </summary>
    public static MutationIntent CreateIntent()
        => new()
        {
            OperationName = "ApproveGovernanceChange",
            Category = "Governance",
            Description = "Approve governance request in benchmark workflow"
        };

    /// <summary>
    /// Creates a user context used for benchmark approval decisions.
    /// </summary>
    public static MutationContext CreateDecisionContext(string actorId, string reason)
        => MutationContext.User(actorId, actorId, reason);

    /// <summary>
    /// Creates structured rejection payload for benchmark scenarios.
    /// </summary>
    public static MutationApprovalRejectionReason CreateRejectionReason()
        => new()
        {
            Code = "benchmark-rejection",
            Category = "policy",
            Message = "Benchmark rejection path"
        };

    /// <summary>
    /// Creates a mutation context that carries approval roles for role-based resolution.
    /// </summary>
    public static MutationContext CreateRoleDecisionContext(
        string actorId,
        string actorName,
        string reason,
        params string[] roles)
        => MutationContext.User(actorId, actorName, reason) with
        {
            Metadata = new Dictionary<string, object>
            {
                ["ActorRoles"] = roles
            }
        };
}
