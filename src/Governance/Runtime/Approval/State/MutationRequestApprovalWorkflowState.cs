using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Runtime.Approval.State;

/// <summary>
/// Provides approval specific state transformations and decision metadata helpers.
/// </summary>
internal static class MutationRequestApprovalWorkflowState
{
    private const string ActorRolesMetadataKey = "ActorRoles";
    private const string ActorGroupsMetadataKey = "ActorGroups";

    /// <summary>
    /// Marks an approval requirement as explicitly approved.
    /// </summary>
    public static MutationApprovalRequirement ApplyApproval(
        MutationApprovalRequirement requirement,
        MutationContext decisionContext,
        string? reason,
        MutationApprovalRejectionReason? rejection = null)
    {
        return requirement with
        {
            Status = MutationApprovalRequirementStatus.Approved,
            DecidedAt = decisionContext.Timestamp,
            DecisionContext = decisionContext,
            DecisionReason = reason ?? decisionContext.Reason
        };
    }

    /// <summary>
    /// Marks an approval requirement as explicitly rejected.
    /// </summary>
    public static MutationApprovalRequirement ApplyRejection(
        MutationApprovalRequirement requirement,
        MutationContext decisionContext,
        string? reason,
        MutationApprovalRejectionReason? rejection)
    {
        return requirement with
        {
            Status = MutationApprovalRequirementStatus.Rejected,
            DecidedAt = decisionContext.Timestamp,
            DecisionContext = decisionContext,
            DecisionReason = reason ?? rejection?.Message ?? decisionContext.Reason,
            Rejection = rejection
        };
    }

    /// <summary>
    /// Mark pending approval requirement as satisfied indirectly by group quorum.
    /// </summary>
    public static MutationApprovalRequirement ApplySatisfiedByQuorum(
        MutationApprovalRequirement requirement,
        MutationContext decisionContext)
    {
        return requirement with
        {
            Status = MutationApprovalRequirementStatus.Satisfied,
            DecidedAt = decisionContext.Timestamp,
            DecisionContext = decisionContext,
            DecisionReason = "Requirement was satisfied by approval quorum."
        };
    }

    /// <summary>
    /// Marks pending approval requirement as expired.
    /// </summary>
    public static MutationApprovalRequirement ApplyExpiration(
        MutationApprovalRequirement requirement,
        MutationContext decisionContext)
    {
        return requirement with
        {
            Status = MutationApprovalRequirementStatus.Expired,
            DecidedAt = decisionContext.Timestamp,
            DecisionContext = decisionContext,
            DecisionReason = requirement.ExpiresAt is null
                ? "Approval requirement expired."
                : $"Approval requirement expired at '{requirement.ExpiresAt:O}'."
        };
    }

    /// <summary>
    /// Replaces one approval requirement inside request level requirement collection.
    /// </summary>
    public static IReadOnlyList<MutationApprovalRequirement> Replace(
        IReadOnlyList<MutationApprovalRequirement> requirements,
        MutationApprovalRequirement updated) =>
            [.. requirements.Select(requirement => requirement.ApprovalId == updated.ApprovalId ? updated : requirement)];

    /// <summary>
    /// Creates request decision for an approval related action and enriches it with approval metadata.
    /// </summary>
    public static MutationRequestDecision CreateApprovalDecision(
        MutationRequestDecisionType decisionType,
        MutationApprovalRequirement requirement,
        MutationContext decisionContext,
        string? reason,
        MutationApprovalRejectionReason? rejection = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var mergedMetadata = new Dictionary<string, object>
        {
            ["ApprovalId"] = requirement.ApprovalId,
            ["ApproverId"] = requirement.ApproverId,
            ["ApproverRole"] = requirement.ApproverRole ?? string.Empty,
            ["ApproverGroup"] = requirement.ApproverGroup ?? string.Empty,
            ["StepOrder"] = requirement.StepOrder,
            ["ApprovalGroupId"] = requirement.ApprovalGroupId ?? string.Empty,
            ["RequiredApprovals"] = requirement.RequiredApprovals
        };

        if (requirement.ExpiresAt is not null)
            mergedMetadata["ApprovalExpiresAt"] = requirement.ExpiresAt.Value;

        if (rejection is not null)
        {
            mergedMetadata["RejectionCode"] = rejection.Code;
            mergedMetadata["RejectionCategory"] = rejection.Category ?? string.Empty;

            foreach (var pair in rejection.Metadata)
            {
                mergedMetadata[pair.Key] = pair.Value;
            }
        }

        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                mergedMetadata[pair.Key] = pair.Value;
            }
        }

        return MutationRequestDecision.Create(
            decisionType,
            decisionContext,
            reason ?? rejection?.Message ?? decisionContext.Reason,
            mergedMetadata);
    }

    /// <summary>
    /// Marks remaining pending approvals in quorum group as satisfied once the quorum threshold is reached.
    /// </summary>
    public static IReadOnlyList<MutationApprovalRequirement> ApplyQuorumSatisfaction(
        IReadOnlyList<MutationApprovalRequirement> requirements,
        MutationApprovalRequirement resolvedRequirement,
        MutationContext decisionContext)
    {
        if (string.IsNullOrWhiteSpace(resolvedRequirement.ApprovalGroupId))
            return requirements;

        var groupRequirements = requirements
            .Where(requirement =>
                requirement.StepOrder == resolvedRequirement.StepOrder &&
                string.Equals(requirement.ApprovalGroupId, resolvedRequirement.ApprovalGroupId, StringComparison.Ordinal))
            .ToList();

        if (groupRequirements.Count <= 1)
            return requirements;

        var approvedCount = groupRequirements.Count(requirement => requirement.Status == MutationApprovalRequirementStatus.Approved);
        if (approvedCount < resolvedRequirement.RequiredApprovals)
            return requirements;

        return [.. requirements
            .Select(requirement =>
            {
                var sameGroup = requirement.StepOrder == resolvedRequirement.StepOrder &&
                                string.Equals(requirement.ApprovalGroupId, resolvedRequirement.ApprovalGroupId, StringComparison.Ordinal);

                if (!sameGroup || requirement.Status != MutationApprovalRequirementStatus.Pending)
                    return requirement;

                return ApplySatisfiedByQuorum(requirement, decisionContext);
            })];
    }

    /// <summary>
    /// Determines whether the actor in the decision context satisfies the approval target definition.
    /// </summary>
    public static bool MatchesApprovalTarget(
        MutationApprovalRequirement requirement,
        MutationContext decisionContext)
    {
        if (!string.IsNullOrWhiteSpace(requirement.ApproverId) &&
            string.Equals(decisionContext.ActorId, requirement.ApproverId, StringComparison.Ordinal))
            return true;

        if (!string.IsNullOrWhiteSpace(requirement.ApproverRole) &&
            ReadStringSet(decisionContext.Metadata, ActorRolesMetadataKey).Contains(requirement.ApproverRole, StringComparer.Ordinal))
            return true;

        if (!string.IsNullOrWhiteSpace(requirement.ApproverGroup) &&
            ReadStringSet(decisionContext.Metadata, ActorGroupsMetadataKey).Contains(requirement.ApproverGroup, StringComparer.Ordinal))
            return true;

        return false;
    }

    private static IReadOnlyCollection<string> ReadStringSet(
        IReadOnlyDictionary<string, object> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value))
            return [];

        return value switch
        {
            IEnumerable<string> typed => typed.Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
            IEnumerable<object> objects => objects.OfType<string>().Where(static item => !string.IsNullOrWhiteSpace(item)).ToArray(),
            _ => []
        };
    }
}
