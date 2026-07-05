using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.Policies.Approval;
using PolicyComposition.Policies.Deployment;
using PolicyComposition.Policies.Emergency;
using PolicyComposition.Policies.Shared;
using PolicyComposition.State;

namespace PolicyComposition.Policies;

/// <summary>
/// Named composed policy sets used by the release governance example.
/// </summary>
/// <remarks>
/// This class acts as the composition root for the example. It keeps the concrete
/// child policies isolated from the scenarios and exposes reusable policy sets that
/// demonstrate the three composition modes:
/// <list type="bullet">
/// <item><description><c>AllOf</c> for mandatory approval gates.</description></item>
/// <item><description><c>AnyOf</c> for emergency fallback flows.</description></item>
/// <item><description><c>Priority</c> for deterministic ordered selection.</description></item>
/// </list>
/// </remarks>
internal static class ReleaseGovernancePolicies
{
    /// <summary>
    /// Builds the standard approval gate used for release promotion.
    /// </summary>
    /// <remarks>
    /// The gate combines:
    /// <list type="bullet">
    /// <item><description><see cref="RequireApprovalsPolicy"/> to enforce the approval threshold.</description></item>
    /// <item><description><see cref="AddAuditTrailPolicy"/> to add traceability once the gate succeeds.</description></item>
    /// </list>
    /// The composed policy only succeeds when both child policies can contribute
    /// without a conflict and the approval requirement is satisfied.
    /// </remarks>
    public static IMutationPolicy<ReleaseGateState> ApprovalGate() =>
        ModularityKit.Mutator.Abstractions.Policies.PolicyComposition.AllOf(
            "ReleaseApprovalGate",
            [
                new RequireApprovalsPolicy(),
                new AddAuditTrailPolicy()
            ],
            priority: 500,
            description: "Requires two approvals and adds audit metadata.");

    /// <summary>
    /// Builds the emergency gate that prefers an explicit override and falls back to approvals.
    /// </summary>
    /// <remarks>
    /// The gate combines:
    /// <list type="bullet">
    /// <item><description><see cref="EmergencyOverridePolicy"/> for explicit emergency approval.</description></item>
    /// <item><description><see cref="ApprovalFallbackPolicy"/> for the approval-based fallback path.</description></item>
    /// </list>
    /// The first allowed branch wins, which makes the result deterministic while
    /// still allowing a reusable emergency path to be expressed in one place.
    /// </remarks>
    public static IMutationPolicy<ReleaseGateState> EmergencyGate() =>
        ModularityKit.Mutator.Abstractions.Policies.PolicyComposition.AnyOf(
            "ReleaseEmergencyGate",
            [
                new EmergencyOverridePolicy(),
                new ApprovalFallbackPolicy()
            ],
            priority: 400,
            description: "Chooses the emergency override branch when available.");

    /// <summary>
    /// Builds the deployment gate that evaluates production checks before the default path.
    /// </summary>
    /// <remarks>
    /// The gate combines:
    /// <list type="bullet">
    /// <item><description><see cref="ProductionGuardPolicy"/> to block production releases early.</description></item>
    /// <item><description><see cref="DefaultDeploymentPolicy"/> as the fallback branch for non-production releases.</description></item>
    /// </list>
    /// Because this is a priority composition, the first decisive policy short-circuits
    /// the rest of the chain.
    /// </remarks>
    public static IMutationPolicy<ReleaseGateState> DeploymentGate() =>
        ModularityKit.Mutator.Abstractions.Policies.PolicyComposition.Priority(
            "ReleaseDeploymentGate",
            [
                new ProductionGuardPolicy(),
                new DefaultDeploymentPolicy()
            ],
            priority: 300,
            description: "Uses a production guard first, then falls back to the default deployment path.");

    /// <summary>
    /// Builds a composed gate that intentionally conflicts on the owner field.
    /// </summary>
    /// <remarks>
    /// The gate composes two <see cref="SetOwnerPolicy"/> instances that target the
    /// same state field with different values. The example uses this to show that
    /// the composition layer detects conflicting mutation results explicitly instead
    /// of silently picking one branch.
    /// </remarks>
    public static IMutationPolicy<ReleaseGateState> ConflictingOwnerGate() =>
        ModularityKit.Mutator.Abstractions.Policies.PolicyComposition.AllOf(
            "ReleaseOwnerConflictGate",
            [
                new SetOwnerPolicy("platform"),
                new SetOwnerPolicy("security")
            ],
            priority: 200,
            description: "Demonstrates explicit conflict handling when two policies set the same field differently.");
}
