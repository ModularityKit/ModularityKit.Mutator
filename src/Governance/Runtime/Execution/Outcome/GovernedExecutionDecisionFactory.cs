using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Outcome;

/// <summary>
/// Creates governance execution decisions and reason text for terminal request transitions.
/// </summary>
internal static class GovernedExecutionDecisionFactory
{
    /// <summary>
    /// Creates lifecycle decision describing rejected governed execution.
    /// </summary>
    /// <param name="request">Governed request whose execution was rejected.</param>
    /// <param name="governanceContext">Context describing the actor or service recording the rejection.</param>
    /// <param name="reason">Human-readable rejection reason.</param>
    /// <param name="metadata">Additional metadata captured for the rejection decision.</param>
    /// <returns>A lifecycle decision representing rejected execution.</returns>
    public static MutationRequestDecision CreateRejectedDecision(
        MutationRequest request,
        MutationContext governanceContext,
        string reason,
        IReadOnlyDictionary<string, object> metadata)
    {
        var mergedMetadata = new Dictionary<string, object>(metadata)
        {
            ["ExecutionKind"] = request.Execution.Kind.ToString()
        };

        AppendCompensationMetadata(mergedMetadata, request.Execution.Compensation);

        return MutationRequestDecision.Lifecycle(
            MutationRequestLifecycleDecisionType.Rejected,
            governanceContext,
            reason,
            mergedMetadata);
    }

    /// <summary>
    /// Creates lifecycle decision describing successful governed execution.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="request">Governed request whose execution completed successfully.</param>
    /// <param name="governanceContext">Context describing the actor or service recording the execution.</param>
    /// <param name="resultingStateVersion">Resulting state version persisted after successful execution.</param>
    /// <param name="mutationResult">Core mutation result used to populate decision metadata.</param>
    /// <returns>A lifecycle decision representing successful execution.</returns>
    public static MutationRequestDecision CreateExecutedDecision<TState>(
        MutationRequest request,
        MutationContext governanceContext,
        string resultingStateVersion,
        MutationResult<TState> mutationResult)
    {
        var metadata = new Dictionary<string, object>
        {
            ["ExecutionKind"] = request.Execution.Kind.ToString(),
            ["ResultingStateVersion"] = resultingStateVersion,
            ["ChangeCount"] = mutationResult.Changes.Count,
            ["SideEffectCount"] = mutationResult.SideEffects.Count
        };

        AppendCompensationMetadata(metadata, request.Execution.Compensation);

        return MutationRequestDecision.Lifecycle(
            MutationRequestLifecycleDecisionType.Executed,
            governanceContext,
            request.Execution.Kind == GovernedExecutionKind.Compensation
                ? "Governed compensation executed successfully."
                : "Governed request executed successfully.",
            metadata);
    }

    /// <summary>
    /// Creates lifecycle decision linking original request to successful compensating execution.
    /// </summary>
    /// <param name="governanceContext">Context describing the actor or service recording the compensation link.</param>
    /// <param name="compensationRequestId">Identifier of the compensating request.</param>
    /// <param name="compensation">Compensation plan associated with the compensating request.</param>
    /// <param name="resultingStateVersion">Resulting state version produced by the compensation.</param>
    /// <param name="timestamp">Timestamp to record on the compensation decision.</param>
    /// <returns>A lifecycle decision representing compensation of the original request.</returns>
    public static MutationRequestDecision CreateCompensatedDecision(
        MutationContext governanceContext,
        string compensationRequestId,
        GovernedCompensationPlan compensation,
        string resultingStateVersion,
        DateTimeOffset timestamp)
    {
        var metadata = new Dictionary<string, object>
        {
            ["CompensationRequestId"] = compensationRequestId,
            ["ResultingStateVersion"] = resultingStateVersion,
            ["CompensationKind"] = compensation.Kind.ToString(),
            ["CompensationTrigger"] = compensation.Trigger.ToString()
        };

        AppendCompensationMetadata(metadata, compensation);

        return MutationRequestDecision.Lifecycle(
            MutationRequestLifecycleDecisionType.Compensated,
            governanceContext,
            $"Compensated by request '{compensationRequestId}'.",
            metadata) with
        {
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Builds rejection reason from failed mutation result.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="mutationResult">Failed mutation result to inspect.</param>
    /// <returns>Human-readable rejection reason derived from policy, validation, or default fallback.</returns>
    public static string BuildRejectedExecutionReason<TState>(MutationResult<TState> mutationResult)
    {
        if (mutationResult.PolicyDecisions.Count > 0)
            return mutationResult.PolicyDecisions[0].Reason ?? "Governed execution was blocked by policy.";

        if (!mutationResult.ValidationResult.IsValid && mutationResult.ValidationResult.Errors.Count > 0)
            return mutationResult.ValidationResult.Errors[0].Message;

        return "Governed execution completed without a successful mutation result.";
    }

    /// <summary>
    /// Appends compensation metadata to decision metadata map when compensation context exists.
    /// </summary>
    /// <param name="metadata">Decision metadata map to enrich.</param>
    /// <param name="compensation">Optional compensation plan providing additional metadata.</param>
    private static void AppendCompensationMetadata(
        IDictionary<string, object> metadata,
        GovernedCompensationPlan? compensation)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (compensation is null)
            return;

        metadata["OriginalRequestId"] = compensation.OriginalRequestId;
        metadata["CompensationKind"] = compensation.Kind.ToString();
        metadata["CompensationTrigger"] = compensation.Trigger.ToString();

        if (!string.IsNullOrWhiteSpace(compensation.BatchId))
            metadata["BatchId"] = compensation.BatchId;

        if (compensation.RelatedRequestIds.Count > 0)
            metadata["RelatedRequestIds"] = compensation.RelatedRequestIds;

        if (!string.IsNullOrWhiteSpace(compensation.Reason))
            metadata["CompensationReason"] = compensation.Reason;
    }
}
