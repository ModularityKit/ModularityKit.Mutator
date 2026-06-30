using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Queries.Builders;

/// <summary>
/// Builds request fixtures used by query store tests.
/// </summary>
internal static class MutationRequestQueryStoreRequestBuilders
{
    /// <summary>
    /// Creates a simple request fixture with configurable lifecycle state.
    /// </summary>
    public static MutationRequest CreateSimpleRequest(
        string requestId,
        MutationRequestStatus status,
        PendingMutationReason? pendingReason,
        DateTimeOffset createdAt)
        => MutationRequestFactory.Pending(
            stateId: "tenant-42:quota",
            stateType: "QuotaState",
            mutationType: "IncreaseQuotaMutation",
            intent: new MutationIntent
            {
                OperationName = "IncreaseQuota",
                Category = "Billing",
                Description = "Raise quota",
                Tags = new HashSet<string> { "billing" },
                EstimatedBlastRadius = BlastRadius.Single
            },
            context: MutationContext.User("alice", "Alice", "Need more quota"),
            pendingReason: pendingReason ?? PendingMutationReason.Approval)
        with
        {
            RequestId = requestId,
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = status,
                PendingReason = pendingReason,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            }
        };

    /// <summary>
    /// Creates an approved request fixture with a decision timeline.
    /// </summary>
    public static MutationRequest CreateApprovedRequest(
        string requestId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access",
                Tags = new HashSet<string> { "security" },
                EstimatedBlastRadius = BlastRadius.Module
            },
            context: MutationContext.User("requester", "Requester", "Need access"),
            requirements:
            [
                PolicyRequirement.Approval("approver", "Review elevated access")
            ])
        with
        {
            RequestId = requestId,
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Approved,
                PendingReason = null,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            },
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    MutationContext.User("requester", "Requester", "Submitted"))
                with
                {
                    Timestamp = createdAt
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
                    MutationContext.User("requester", "Requester", "Pending approval"))
                with
                {
                    Timestamp = createdAt.AddMinutes(5)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Requested),
                    MutationContext.User("requester", "Requester", "Approval requested"),
                    metadata: new Dictionary<string, object> { ["Queue"] = "security" })
                with
                {
                    Timestamp = createdAt.AddMinutes(10)
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Granted),
                    MutationContext.User("approver", "Approver", "Approved"),
                    metadata: new Dictionary<string, object> { ["Queue"] = "security" })
                with
                {
                    Timestamp = updatedAt
                },
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
                    MutationContext.User("approver", "Approver", "Approved"),
                    metadata: new Dictionary<string, object> { ["Queue"] = "security" })
                with
                {
                    Timestamp = updatedAt.AddMinutes(1)
                }
            ]
        };

    /// <summary>
    /// Creates a governed request fixture with fully populated request data.
    /// </summary>
    public static MutationRequest CreateGovernedRequest(
        string requestId,
        string stateId,
        string stateType,
        string mutationType,
        string actorId,
        string actorName,
        string category,
        IReadOnlySet<string> tags,
        IReadOnlyDictionary<string, object> intentMetadata,
        IReadOnlyDictionary<string, object> requestMetadata,
        BlastRadius blastRadius,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        MutationRequestStatus status,
        PendingMutationReason? pendingReason,
        IReadOnlyList<MutationRequestDecision> decisions,
        IReadOnlyList<SideEffect>? sideEffects = null)
        => new MutationRequest
        {
            RequestId = requestId,
            Scope = new MutationRequestScopeDetails
            {
                StateId = stateId,
                StateType = stateType,
                MutationType = mutationType
            },
            Payload = new MutationRequestPayloadDetails
            {
                Intent = new MutationIntent
                {
                    OperationName = mutationType,
                    Category = category,
                    Tags = tags,
                    Metadata = intentMetadata,
                    EstimatedBlastRadius = blastRadius
                },
                Context = MutationContext.User(actorId, actorName, "Query test")
            },
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = status,
                PendingReason = pendingReason,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            },
            Decisions = decisions,
            Metadata = requestMetadata,
            SideEffects = sideEffects ?? []
        };
}
