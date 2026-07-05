using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Policies.Approval;

/// <summary>
/// Blocks release promotion until the expected approval threshold is present.
/// </summary>
/// <remarks>
/// The policy reads the approval count from mutation metadata, so the example can
/// show how governance input travels alongside the mutation itself.
/// When the threshold is met, the policy marks the release as approved and emits
/// an audit side effect. When it is not met, the policy returns a requirement and
/// error severity so the composed result can surface the missing approval.
/// </remarks>
internal sealed class RequireApprovalsPolicy : IMutationPolicy<ReleaseGateState>
{
    /// <summary>
    /// Stable policy identifier used in diagnostics and composition metadata.
    /// </summary>
    public string Name => "RequireApprovals";

    /// <summary>
    /// Higher than the audit only policy, so approval gating is evaluated first.
    /// </summary>
    public int Priority => 300;

    /// <summary>
    /// Explains the minimum-approval requirement that this policy enforces.
    /// </summary>
    public string Description => "Requires at least two approvals before the release can proceed.";

    /// <summary>
    /// Reads the approval count from mutation metadata and either allows or blocks the release.
    /// </summary>
    /// <param name="mutation">The mutation carrying government metadata.</param>
    /// <param name="state">The current release state.</param>
    /// <returns>
    /// An allowed decision when approvals are enough, otherwise a blocking
    /// decision with an approval requirement and error severity.
    /// </returns>
    public PolicyDecision Evaluate(IMutation<ReleaseGateState> mutation, ReleaseGateState state)
    {
        var approvals = GetInt32(mutation.Context.Metadata, "approvals");

        return approvals >= 2
            ? new PolicyDecision
            {
                IsAllowed = true,
                PolicyName = Name,
                Modifications = new Dictionary<string, object>
                {
                    ["State"] = state with { Stage = "Approved" },
                    ["SideEffect"] = SideEffect.Create("audit", $"Release approved with {approvals} approvals.")
                },
                Metadata = new Dictionary<string, object>
                {
                    ["approvalCount"] = approvals
                }
            }
            : new PolicyDecision
            {
                IsAllowed = false,
                PolicyName = Name,
                Severity = PolicyDecisionSeverity.Error,
                Reason = $"Release requires at least two approvals; found {approvals}.",
                Requirements =
                [
                    PolicyRequirement.Approval("release-manager", "Two approvals are required before promotion.")
                ],
                Metadata = new Dictionary<string, object>
                {
                    ["approvalCount"] = approvals
                }
            };
    }

    private static int GetInt32(IReadOnlyDictionary<string, object> metadata, string key)
        => metadata.TryGetValue(key, out var value) && value is int number ? number : 0;
}
