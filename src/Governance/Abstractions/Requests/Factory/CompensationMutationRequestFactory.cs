using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Links;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;

/// <summary>
/// Creates governed mutation requests for compensation flows.
/// </summary>
public static class CompensationMutationRequestFactory
{
    /// <summary>
    /// Creates an immediately approved compensation request using type inference for the target state and mutation.
    /// </summary>
    /// <typeparam name="TState">The target state type.</typeparam>
    /// <typeparam name="TMutation">The compensation mutation type.</typeparam>
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="intent">Intent associated with the compensating mutation.</param>
    /// <param name="context">Request context describing who initiated the compensation and why.</param>
    /// <param name="compensation">Compensation plan describing the original execution and recovery semantics.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured before compensation execution.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>An approved governed compensation request.</returns>
    public static MutationRequest Approved<TState, TMutation>(
        string stateId,
        MutationIntent intent,
        MutationContext context,
        GovernedCompensationPlan compensation,
        string? expectedStateVersion = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        where TMutation : IMutation<TState>
        => Approved(
            stateId,
            typeof(TState).Name,
            typeof(TMutation).Name,
            intent,
            context,
            compensation,
            expectedStateVersion,
            metadata);

    /// <summary>
    /// Creates an immediately approved compensation request.
    /// </summary>
    /// <param name="stateId">Stable identifier of the target state.</param>
    /// <param name="stateType">Logical state type name.</param>
    /// <param name="mutationType">Compensation mutation type name.</param>
    /// <param name="intent">Intent associated with the compensating mutation.</param>
    /// <param name="context">Request context describing who initiated the compensation and why.</param>
    /// <param name="compensation">Compensation plan describing the original execution and recovery semantics.</param>
    /// <param name="expectedStateVersion">Optional expected state version captured before compensation execution.</param>
    /// <param name="metadata">Optional governance metadata carried by the request.</param>
    /// <returns>An approved governed compensation request.</returns>
    public static MutationRequest Approved(
        string stateId,
        string stateType,
        string mutationType,
        MutationIntent intent,
        MutationContext context,
        GovernedCompensationPlan compensation,
        string? expectedStateVersion = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(compensation);
        compensation.EnsureValid();

        var request = MutationRequestFactory.Approved(
            stateId,
            stateType,
            mutationType,
            intent,
            context,
            expectedStateVersion,
            metadata);

        return request with
        {
            Execution = new GovernedExecutionDetails
            {
                Kind = GovernedExecutionKind.Compensation,
                Compensation = compensation,
                RelatedExecutions =
                [
                    new GovernedExecutionLink
                    {
                        RequestId = compensation.OriginalRequestId,
                        Type = GovernedExecutionLinkType.Compensates,
                        ExecutionKind = GovernedExecutionKind.Standard,
                        CompensationKind = compensation.Kind,
                        Trigger = compensation.Trigger,
                        BatchId = compensation.BatchId
                    }
                ]
            },
            Decisions =
            [
                .. request.Decisions.Take(request.Decisions.Count - 1),
                MutationRequestDecision.Lifecycle(
                    MutationRequestLifecycleDecisionType.Approved,
                    context,
                    reason: $"Compensation request approved at submission time for original request '{compensation.OriginalRequestId}'.",
                    metadata: CreateCompensationMetadata(compensation))
            ]
        };
    }

    private static IReadOnlyDictionary<string, object> CreateCompensationMetadata(GovernedCompensationPlan compensation)
    {
        var metadata = new Dictionary<string, object>
        {
            ["OriginalRequestId"] = compensation.OriginalRequestId,
            ["CompensationKind"] = compensation.Kind.ToString(),
            ["CompensationTrigger"] = compensation.Trigger.ToString()
        };

        if (!string.IsNullOrWhiteSpace(compensation.BatchId))
            metadata["BatchId"] = compensation.BatchId;

        if (compensation.RelatedRequestIds.Count > 0)
            metadata["RelatedRequestIds"] = compensation.RelatedRequestIds;

        if (!string.IsNullOrWhiteSpace(compensation.Reason))
            metadata["CompensationReason"] = compensation.Reason;

        return metadata;
    }
}
