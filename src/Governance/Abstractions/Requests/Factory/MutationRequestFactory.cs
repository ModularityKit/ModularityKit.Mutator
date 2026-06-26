using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Mapping;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;

/// <summary>
/// Creates governed mutation requests for common governance entry paths.
/// </summary>
public static class MutationRequestFactory
{
    /// <summary>
    /// Creates a request that should enter the pending lifecycle using type inference for the target state and mutation.
    /// </summary>
    public static MutationRequest Pending<TState, TMutation>(
        string stateId,
        MutationIntent intent,
        MutationContext context,
        PendingMutationReason pendingReason,
        IReadOnlyList<PolicyRequirement>? requirements = null,
        string? expectedStateVersion = null,
        DateTimeOffset? expiresAt = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        where TMutation : IMutation<TState>
        => Pending(
            stateId,
            typeof(TState).Name,
            typeof(TMutation).Name,
            intent,
            context,
            pendingReason,
            requirements,
            expectedStateVersion,
            expiresAt,
            metadata);

    /// <summary>
    /// Creates a request that should enter the pending lifecycle.
    /// </summary>
    public static MutationRequest Pending(
        string stateId,
        string stateType,
        string mutationType,
        MutationIntent intent,
        MutationContext context,
        PendingMutationReason pendingReason,
        IReadOnlyList<PolicyRequirement>? requirements = null,
        string? expectedStateVersion = null,
        DateTimeOffset? expiresAt = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new MutationRequest
        {
            StateId = stateId,
            StateType = stateType,
            MutationType = mutationType,
            Intent = intent,
            Context = context,
            Status = MutationRequestStatus.Pending,
            PendingReason = pendingReason,
            Requirements = requirements ?? [],
            ExpectedStateVersion = expectedStateVersion,
            ExpiresAt = expiresAt,
            Metadata = metadata ?? new Dictionary<string, object>(),
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    context,
                    reason: context.Reason),
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
                    context,
                    reason: $"Request entered pending lifecycle for reason '{pendingReason}'.")
            ]
        };
    }

    /// <summary>
    /// Creates a request that enters pending approval using type inference for the target state and mutation.
    /// </summary>
    public static MutationRequest PendingApproval<TState, TMutation>(
        string stateId,
        MutationIntent intent,
        MutationContext context,
        IReadOnlyList<PolicyRequirement> requirements,
        string? expectedStateVersion = null,
        DateTimeOffset? expiresAt = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        where TMutation : IMutation<TState>
        => PendingApproval(
            stateId,
            typeof(TState).Name,
            typeof(TMutation).Name,
            intent,
            context,
            requirements,
            expectedStateVersion,
            expiresAt,
            metadata);

    /// <summary>
    /// Creates a request that enters pending approval with concrete request-level approval requirements.
    /// </summary>
    public static MutationRequest PendingApproval(
        string stateId,
        string stateType,
        string mutationType,
        MutationIntent intent,
        MutationContext context,
        IReadOnlyList<PolicyRequirement> requirements,
        string? expectedStateVersion = null,
        DateTimeOffset? expiresAt = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        var approvalRequirements = MutationApprovalRequirementMapper.Map(requirements);
        if (approvalRequirements.Count == 0)
            throw new InvalidOperationException("Pending approval requests require at least one approval requirement.");

        return new MutationRequest
        {
            StateId = stateId,
            StateType = stateType,
            MutationType = mutationType,
            Intent = intent,
            Context = context,
            Status = MutationRequestStatus.Pending,
            PendingReason = PendingMutationReason.Approval,
            Requirements = requirements,
            ApprovalRequirements = approvalRequirements,
            ExpectedStateVersion = expectedStateVersion,
            ExpiresAt = expiresAt,
            Metadata = metadata ?? new Dictionary<string, object>(),
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    context,
                    reason: context.Reason),
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Pending),
                    context,
                    reason: "Request entered pending approval."),
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Approval(MutationRequestApprovalDecisionType.Requested),
                    context,
                    reason: $"Request requires {approvalRequirements.Count} approval action(s).",
                    metadata: new Dictionary<string, object>
                    {
                        ["ApprovalRequirementCount"] = approvalRequirements.Count
                    })
            ]
        };
    }

    /// <summary>
    /// Creates a request that is immediately approved for execution using type inference for the target state and mutation.
    /// </summary>
    public static MutationRequest Approved<TState, TMutation>(
        string stateId,
        MutationIntent intent,
        MutationContext context,
        string? expectedStateVersion = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        where TMutation : IMutation<TState>
        => Approved(
            stateId,
            typeof(TState).Name,
            typeof(TMutation).Name,
            intent,
            context,
            expectedStateVersion,
            metadata);

    /// <summary>
    /// Creates a request that is immediately approved for execution.
    /// </summary>
    public static MutationRequest Approved(
        string stateId,
        string stateType,
        string mutationType,
        MutationIntent intent,
        MutationContext context,
        string? expectedStateVersion = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new MutationRequest
        {
            StateId = stateId,
            StateType = stateType,
            MutationType = mutationType,
            Intent = intent,
            Context = context,
            Status = MutationRequestStatus.Approved,
            ExpectedStateVersion = expectedStateVersion,
            Metadata = metadata ?? new Dictionary<string, object>(),
            Decisions =
            [
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Submitted),
                    context,
                    reason: context.Reason),
                MutationRequestDecision.Create(
                    MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Approved),
                    context,
                    reason: "Approved at submission time")
            ]
        };
    }
}
