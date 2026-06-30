using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Links;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

/// <summary>
/// Groups governed execution-specific details carried by mutation request.
/// </summary>
public sealed record GovernedExecutionDetails
{
    /// <summary>
    /// Classifies this request as standard governed execution or compensating execution.
    /// </summary>
    public GovernedExecutionKind Kind { get; init; } = GovernedExecutionKind.Standard;

    /// <summary>
    /// Compensation plan carried by this request when it compensates for prior execution.
    /// </summary>
    public GovernedCompensationPlan? Compensation { get; init; }

    /// <summary>
    /// Explicit links to related governed execution records.
    /// </summary>
    public IReadOnlyList<GovernedExecutionLink> RelatedExecutions { get; init; } = [];
}
