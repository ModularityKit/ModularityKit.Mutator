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
    /// <typeparam name="TState">The target state type.</typeparam>
    /// <typeparam name="TMutation">The mutation type.</typeparam>
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="intent">Intent associated with the requested mutation.</param>
    /// <param name="context">Request context describing who submitted the mutation and why.</param>
    /// <param name="pendingReason">Lifecycle reason that keeps the request pending.</param>
    /// <param name="requirements">Optional policy requirements attached to the request.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured at submission time.</param>
    /// <param name="expiresAt">Optional expiration time for the pending request.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>A pending governed mutation request.</returns>
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
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="stateType">Logical state type name.</param>
    /// <param name="mutationType">Mutation type name.</param>
    /// <param name="intent">Intent associated with the requested mutation.</param>
    /// <param name="context">Request context describing who submitted the mutation and why.</param>
    /// <param name="pendingReason">Lifecycle reason that keeps the request pending.</param>
    /// <param name="requirements">Optional policy requirements attached to the request.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured at submission time.</param>
    /// <param name="expiresAt">Optional expiration time for the pending request.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>A pending governed mutation request.</returns>
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
            Scope = new MutationRequestScopeDetails
            {
                StateId = stateId,
                StateType = stateType,
                MutationType = mutationType
            },
            Payload = new MutationRequestPayloadDetails
            {
                Intent = intent,
                Context = context
            },
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = pendingReason,
                ExpiresAt = expiresAt
            },
            Requirements = requirements ?? [],
            Versioning = new MutationRequestVersioningDetails
            {
                ExpectedStateVersion = expectedStateVersion
            },
            Metadata = metadata ?? new Dictionary<string, object>(),
            Decisions =
            [
                MutationRequestDecision.Lifecycle(
                    MutationRequestLifecycleDecisionType.Submitted,
                    context,
                    reason: context.Reason),
                MutationRequestDecision.Lifecycle(
                    MutationRequestLifecycleDecisionType.Pending,
                    context,
                    reason: $"Request entered pending lifecycle for reason '{pendingReason}'.")
            ]
        };
    }

    /// <summary>
    /// Creates a request that enters pending approval using type inference for the target state and mutation.
    /// </summary>
    /// <typeparam name="TState">The target state type.</typeparam>
    /// <typeparam name="TMutation">The mutation type.</typeparam>
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="intent">Intent associated with the requested mutation.</param>
    /// <param name="context">Request context describing who submitted the mutation and why.</param>
    /// <param name="requirements">Policy requirements that will be translated into approval requirements.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured at submission time.</param>
    /// <param name="expiresAt">Optional expiration time for the pending request.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>A governed mutation request pending approval.</returns>
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
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="stateType">Logical state type name.</param>
    /// <param name="mutationType">Mutation type name.</param>
    /// <param name="intent">Intent associated with the requested mutation.</param>
    /// <param name="context">Request context describing who submitted the mutation and why.</param>
    /// <param name="requirements">Policy requirements that will be translated into approval requirements.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured at submission time.</param>
    /// <param name="expiresAt">Optional expiration time for the pending request.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>A governed mutation request pending approval.</returns>
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
            Scope = new MutationRequestScopeDetails
            {
                StateId = stateId,
                StateType = stateType,
                MutationType = mutationType
            },
            Payload = new MutationRequestPayloadDetails
            {
                Intent = intent,
                Context = context
            },
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Approval,
                ExpiresAt = expiresAt
            },
            Requirements = requirements,
            ApprovalRequirements = approvalRequirements,
            Versioning = new MutationRequestVersioningDetails
            {
                ExpectedStateVersion = expectedStateVersion
            },
            Metadata = metadata ?? new Dictionary<string, object>(),
            Decisions =
            [
                MutationRequestDecision.Lifecycle(
                    MutationRequestLifecycleDecisionType.Submitted,
                    context,
                    reason: context.Reason),
                MutationRequestDecision.Lifecycle(
                    MutationRequestLifecycleDecisionType.Pending,
                    context,
                    reason: "Request entered pending approval."),
                MutationRequestDecision.Approval(
                    MutationRequestApprovalDecisionType.Requested,
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
    /// <typeparam name="TState">The target state type.</typeparam>
    /// <typeparam name="TMutation">The mutation type.</typeparam>
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="intent">Intent associated with the requested mutation.</param>
    /// <param name="context">Request context describing who submitted the mutation and why.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured at submission time.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>An approved governed mutation request.</returns>
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
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="stateType">Logical state type name.</param>
    /// <param name="mutationType">Mutation type name.</param>
    /// <param name="intent">Intent associated with the requested mutation.</param>
    /// <param name="context">Request context describing who submitted the mutation and why.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured at submission time.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>An approved governed mutation request.</returns>
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
            Scope = new MutationRequestScopeDetails
            {
                StateId = stateId,
                StateType = stateType,
                MutationType = mutationType
            },
            Payload = new MutationRequestPayloadDetails
            {
                Intent = intent,
                Context = context
            },
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Approved
            },
            Versioning = new MutationRequestVersioningDetails
            {
                ExpectedStateVersion = expectedStateVersion
            },
            Metadata = metadata ?? new Dictionary<string, object>(),
            Decisions =
            [
                MutationRequestDecision.Lifecycle(
                    MutationRequestLifecycleDecisionType.Submitted,
                    context,
                    reason: context.Reason),
                MutationRequestDecision.Lifecycle(
                    MutationRequestLifecycleDecisionType.Approved,
                    context,
                    reason: "Approved at submission time")
            ]
        };
    }
}
