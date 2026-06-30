using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Requests;

/// <summary>
/// Creates baseline mutation requests used across governance tests.
/// </summary>
internal static class MutationRequestTestFactory
{
    /// <summary>
    /// Creates a pending request with stable defaults for lifecycle tests.
    /// </summary>
    public static MutationRequest CreatePendingRequest()
    {
        return MutationRequestFactory.Pending(
            stateId: "tenant-42:quota",
            stateType: "QuotaPolicy",
            mutationType: "IncreaseQuotaMutation",
            intent: new MutationIntent
            {
                OperationName = "IncreaseQuota",
                Category = "Billing",
                Description = "Raise quota"
            },
            context: MutationContext.User("alice", "Alice", "Need more quota"),
            pendingReason: PendingMutationReason.Approval,
            expectedStateVersion: "v12");
    }

    /// <summary>
    /// Creates an approved request with security-oriented defaults.
    /// </summary>
    public static MutationRequest CreateApprovedSecurityRequest(string expectedStateVersion)
    {
        return MutationRequestFactory.Approved(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access"
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            expectedStateVersion: expectedStateVersion);
    }
}
