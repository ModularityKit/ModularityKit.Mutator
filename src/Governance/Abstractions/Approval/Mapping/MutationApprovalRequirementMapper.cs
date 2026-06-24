using System.Collections;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Approval.Mapping;

internal static class MutationApprovalRequirementMapper
{
    public static IReadOnlyList<MutationApprovalRequirement> Map(
        IReadOnlyList<PolicyRequirement>? requirements)
    {
        if (requirements is null || requirements.Count == 0)
            return [];

        var mapped = new List<MutationApprovalRequirement>();
        var approvalIndex = 0;

        foreach (var requirement in requirements)
        {
            if (!string.Equals(requirement.Type, "Approval", StringComparison.Ordinal))
                continue;

            var approvalDefinitions = ExtractApprovalDefinitions(requirement, approvalIndex);
            mapped.AddRange(approvalDefinitions);
            approvalIndex++;
        }

        return mapped;
    }

    private static IReadOnlyList<MutationApprovalRequirement> ExtractApprovalDefinitions(
        PolicyRequirement requirement,
        int defaultStepOrder)
    {
        var stepOrder = ReadIntProperty(requirement.Data, "StepOrder") ?? defaultStepOrder + 1;
        var approverName = ReadStringProperty(requirement.Data, "ApproverName");
        var reason = ReadStringProperty(requirement.Data, "Reason");
        var approvalGroupId = ReadStringProperty(requirement.Data, "ApprovalGroupId")
                              ?? ReadStringProperty(requirement.Data, "GroupId");
        var approverRole = ReadStringProperty(requirement.Data, "ApproverRole");
        var approverGroup = ReadStringProperty(requirement.Data, "ApproverGroup");
        var requiredApprovals = ReadIntProperty(requirement.Data, "RequiredApprovals")
                                ?? ReadIntProperty(requirement.Data, "Quorum")
                                ?? 1;
        var expiresAt = ReadDateTimeOffsetProperty(requirement.Data, "ExpiresAt");

        var approvers = ReadStringSequenceProperty(requirement.Data, "Approvers");
        if (approvers.Count == 0)
        {
            var approver = ReadStringProperty(requirement.Data, "Approver");
            if (!string.IsNullOrWhiteSpace(approver))
                approvers = [approver];
        }

        var targetCount = approvers.Count
                          + (!string.IsNullOrWhiteSpace(approverRole) ? 1 : 0)
                          + (!string.IsNullOrWhiteSpace(approverGroup) ? 1 : 0);

        if (targetCount == 0)
            throw new InvalidMutationApprovalConfigurationException(
                $"Approval requirement '{requirement.Description}' does not define an approver, approver role, or approver group.");

        if (requiredApprovals <= 0)
            throw new InvalidMutationApprovalConfigurationException(
                $"Approval requirement '{requirement.Description}' must require at least one approval.");

        if (requiredApprovals > targetCount)
            throw new InvalidMutationApprovalConfigurationException(
                $"Approval requirement '{requirement.Description}' requires {requiredApprovals} approval(s) but only defines {targetCount} target(s).");

        if (targetCount > 1 && string.IsNullOrWhiteSpace(approvalGroupId))
            approvalGroupId = $"approval-group-{defaultStepOrder + 1}-{defaultStepOrder}";

        var mapped = approvers
            .Select(approverId => new MutationApprovalRequirement
            {
                Type = requirement.Type,
                Description = requirement.Description,
                ApproverId = approverId,
                ApproverName = approverName,
                StepOrder = stepOrder,
                ApprovalGroupId = approvalGroupId,
                RequiredApprovals = requiredApprovals,
                ExpiresAt = expiresAt,
                Metadata = new Dictionary<string, object>
                {
                    ["RequirementDescription"] = requirement.Description,
                    ["RequirementReason"] = reason ?? string.Empty
                }
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(approverRole))
        {
            mapped.Add(new MutationApprovalRequirement
            {
                Type = requirement.Type,
                Description = requirement.Description,
                ApproverRole = approverRole,
                ApproverName = approverName,
                StepOrder = stepOrder,
                ApprovalGroupId = approvalGroupId,
                RequiredApprovals = requiredApprovals,
                ExpiresAt = expiresAt,
                Metadata = new Dictionary<string, object>
                {
                    ["RequirementDescription"] = requirement.Description,
                    ["RequirementReason"] = reason ?? string.Empty
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(approverGroup))
        {
            mapped.Add(new MutationApprovalRequirement
            {
                Type = requirement.Type,
                Description = requirement.Description,
                ApproverGroup = approverGroup,
                ApproverName = approverName,
                StepOrder = stepOrder,
                ApprovalGroupId = approvalGroupId,
                RequiredApprovals = requiredApprovals,
                ExpiresAt = expiresAt,
                Metadata = new Dictionary<string, object>
                {
                    ["RequirementDescription"] = requirement.Description,
                    ["RequirementReason"] = reason ?? string.Empty
                }
            });
        }

        return mapped;
    }

    private static string? ReadStringProperty(object? source, string propertyName)
    {
        if (source is null)
            return null;

        var property = source.GetType().GetProperty(propertyName);
        var value = property?.GetValue(source);
        return value as string;
    }

    private static int? ReadIntProperty(object? source, string propertyName)
    {
        if (source is null)
            return null;

        var property = source.GetType().GetProperty(propertyName);
        var value = property?.GetValue(source);
        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            short shortValue => shortValue,
            _ => null
        };
    }

    private static DateTimeOffset? ReadDateTimeOffsetProperty(object? source, string propertyName)
    {
        if (source is null)
            return null;

        var property = source.GetType().GetProperty(propertyName);
        var value = property?.GetValue(source);

        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt),
            string text when DateTimeOffset.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }

    private static List<string> ReadStringSequenceProperty(object? source, string propertyName)
    {
        if (source is null)
            return [];

        var property = source.GetType().GetProperty(propertyName);
        var value = property?.GetValue(source);

        return value switch
        {
            IEnumerable<string> typedStrings => typedStrings.Where(static x => !string.IsNullOrWhiteSpace(x)).ToList(),
            IEnumerable sequence => sequence.Cast<object?>()
                .OfType<string>()
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .ToList(),
            _ => []
        };
    }
}
