using System.Collections;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Approval;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Approval.Mapping;

internal static class MutationApprovalRequirementMapper
{
    private sealed record ApprovalRequirementConfig(
        int StepOrder,
        string? ApproverName,
        string? Reason,
        string? ApprovalGroupId,
        string? ApproverRole,
        string? ApproverGroup,
        int RequiredApprovals,
        DateTimeOffset? ExpiresAt,
        IReadOnlyList<string> Approvers);

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
        var config = ReadApprovalRequirementConfig(requirement, defaultStepOrder);
        var targetCount = CountTargets(config);

        if (targetCount == 0)
            throw new InvalidMutationApprovalConfigurationException(
                $"Approval requirement '{requirement.Description}' does not define an approver, approver role, or approver group.");

        if (config.RequiredApprovals <= 0)
            throw new InvalidMutationApprovalConfigurationException(
                $"Approval requirement '{requirement.Description}' must require at least one approval.");

        if (config.RequiredApprovals > targetCount)
            throw new InvalidMutationApprovalConfigurationException(
                $"Approval requirement '{requirement.Description}' requires {config.RequiredApprovals} approval(s) but only defines {targetCount} target(s).");

        var approvalGroupId = ResolveApprovalGroupId(config.ApprovalGroupId, targetCount, defaultStepOrder);

        return config.Approvers
            .Select(approverId => CreateRequirement(
                requirement,
                config.ApproverName,
                config.Reason,
                config.StepOrder,
                approvalGroupId,
                config.RequiredApprovals,
                config.ExpiresAt,
                approverId: approverId))
            .Concat(CreateOptionalRequirements(
                requirement,
                config.ApproverName,
                config.Reason,
                config.StepOrder,
                approvalGroupId,
                config.RequiredApprovals,
                config.ExpiresAt,
                config.ApproverRole,
                config.ApproverGroup))
            .ToList();
    }

    private static ApprovalRequirementConfig ReadApprovalRequirementConfig(
        PolicyRequirement requirement,
        int defaultStepOrder)
        => new(
            StepOrder: ReadIntProperty(requirement.Data, "StepOrder") ?? defaultStepOrder + 1,
            ApproverName: ReadStringProperty(requirement.Data, "ApproverName"),
            Reason: ReadStringProperty(requirement.Data, "Reason"),
            ApprovalGroupId: ReadStringProperty(requirement.Data, "ApprovalGroupId")
                             ?? ReadStringProperty(requirement.Data, "GroupId"),
            ApproverRole: ReadStringProperty(requirement.Data, "ApproverRole"),
            ApproverGroup: ReadStringProperty(requirement.Data, "ApproverGroup"),
            RequiredApprovals: ReadIntProperty(requirement.Data, "RequiredApprovals")
                               ?? ReadIntProperty(requirement.Data, "Quorum")
                               ?? 1,
            ExpiresAt: ReadDateTimeOffsetProperty(requirement.Data, "ExpiresAt"),
            Approvers: ReadApprovers(requirement.Data));

    private static IReadOnlyList<string> ReadApprovers(object? source)
    {
        var approvers = ReadStringSequenceProperty(source, "Approvers");
        if (approvers.Count > 0)
            return approvers;

        var approver = ReadStringProperty(source, "Approver");
        return string.IsNullOrWhiteSpace(approver) ? [] : [approver];
    }

    private static int CountTargets(ApprovalRequirementConfig config)
        => config.Approvers.Count
           + CountOptionalTarget(config.ApproverRole)
           + CountOptionalTarget(config.ApproverGroup);

    private static int CountOptionalTarget(string? value)
        => string.IsNullOrWhiteSpace(value) ? 0 : 1;

    private static string? ResolveApprovalGroupId(
        string? approvalGroupId,
        int targetCount,
        int defaultStepOrder)
        => targetCount > 1 && string.IsNullOrWhiteSpace(approvalGroupId)
            ? $"approval-group-{defaultStepOrder + 1}-{defaultStepOrder}"
            : approvalGroupId;

    private static IEnumerable<MutationApprovalRequirement> CreateOptionalRequirements(
        PolicyRequirement requirement,
        string? approverName,
        string? reason,
        int stepOrder,
        string? approvalGroupId,
        int requiredApprovals,
        DateTimeOffset? expiresAt,
        string? approverRole,
        string? approverGroup)
    {
        var targets = new[]
        {
            new { ApproverRole = approverRole, ApproverGroup = (string?)null },
            new { ApproverRole = (string?)null, ApproverGroup = approverGroup }
        };

        return targets
            .Where(target =>
                !string.IsNullOrWhiteSpace(target.ApproverRole) ||
                !string.IsNullOrWhiteSpace(target.ApproverGroup))
            .Select(target => CreateRequirement(
                requirement,
                approverName,
                reason,
                stepOrder,
                approvalGroupId,
                requiredApprovals,
                expiresAt,
                approverRole: target.ApproverRole,
                approverGroup: target.ApproverGroup));
    }

    private static MutationApprovalRequirement CreateRequirement(
        PolicyRequirement requirement,
        string? approverName,
        string? reason,
        int stepOrder,
        string? approvalGroupId,
        int requiredApprovals,
        DateTimeOffset? expiresAt,
        string approverId = "",
        string? approverRole = null,
        string? approverGroup = null)
        => new()
        {
            Type = requirement.Type,
            Description = requirement.Description,
            ApproverId = approverId,
            ApproverRole = approverRole,
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
        };

    private static string? ReadStringProperty(object? source, string propertyName)
    {
        var value = ReadPropertyValue(source, propertyName);
        return value as string;
    }

    private static int? ReadIntProperty(object? source, string propertyName)
    {
        var value = ReadPropertyValue(source, propertyName);
        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            short shortValue => shortValue,
            decimal decimalValue => decimal.ToInt32(decimalValue),
            _ => null
        };
    }

    private static DateTimeOffset? ReadDateTimeOffsetProperty(object? source, string propertyName)
    {
        var value = ReadPropertyValue(source, propertyName);

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
        var value = ReadPropertyValue(source, propertyName);

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

    private static object? ReadPropertyValue(object? source, string propertyName)
    {
        if (source is null)
            return null;

        if (source is IReadOnlyDictionary<string, object> readOnlyDictionary &&
            readOnlyDictionary.TryGetValue(propertyName, out var readOnlyValue))
            return readOnlyValue;

        if (source is IDictionary<string, object> dictionary &&
            dictionary.TryGetValue(propertyName, out var dictionaryValue))
            return dictionaryValue;

        if (source is IDictionary nonGenericDictionary && nonGenericDictionary.Contains(propertyName))
            return nonGenericDictionary[propertyName];

        var property = source.GetType().GetProperty(propertyName);
        return property?.GetValue(source);
    }
}
