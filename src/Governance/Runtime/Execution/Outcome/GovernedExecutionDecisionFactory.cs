using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Outcome;

/// <summary>
/// Creates governance execution decisions and reason text for terminal request transitions.
/// </summary>
internal static class GovernedExecutionDecisionFactory
{
    public static MutationRequestDecision CreateRejectedDecision(
        MutationContext governanceContext,
        string reason,
        IReadOnlyDictionary<string, object> metadata)
    {
        return MutationRequestDecision.Create(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Rejected),
            governanceContext,
            reason,
            metadata);
    }

    public static MutationRequestDecision CreateExecutedDecision<TState>(
        MutationContext governanceContext,
        string resultingStateVersion,
        MutationResult<TState> mutationResult)
    {
        return MutationRequestDecision.Create(
            MutationRequestDecisionType.Lifecycle(MutationRequestLifecycleDecisionType.Executed),
            governanceContext,
            "Governed request executed successfully.",
            new Dictionary<string, object>
            {
                ["ResultingStateVersion"] = resultingStateVersion,
                ["ChangeCount"] = mutationResult.Changes.Count,
                ["SideEffectCount"] = mutationResult.SideEffects.Count
            });
    }

    public static string BuildRejectedExecutionReason<TState>(MutationResult<TState> mutationResult)
    {
        if (mutationResult.PolicyDecisions.Count > 0)
            return mutationResult.PolicyDecisions[0].Reason ?? "Governed execution was blocked by policy.";

        if (!mutationResult.ValidationResult.IsValid && mutationResult.ValidationResult.Errors.Count > 0)
            return mutationResult.ValidationResult.Errors[0].Message;

        return "Governed execution completed without a successful mutation result.";
    }
}
