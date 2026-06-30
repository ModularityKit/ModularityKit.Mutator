namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;

/// <summary>
/// Describes how a governed request compensates for a prior execution.
/// </summary>
public sealed record GovernedCompensationPlan
{
    /// <summary>
    /// Request identifier of the original execution being compensated.
    /// </summary>
    public string OriginalRequestId { get; init; } = string.Empty;

    /// <summary>
    /// Compensation style to apply.
    /// </summary>
    public GovernedCompensationKind Kind { get; init; } = GovernedCompensationKind.Rollback;

    /// <summary>
    /// Trigger that initiated the compensation.
    /// </summary>
    public GovernedCompensationTrigger Trigger { get; init; } = GovernedCompensationTrigger.OperatorRollback;

    /// <summary>
    /// Optional batch identifier for batch-oriented compensation plans.
    /// </summary>
    public string? BatchId { get; init; }

    /// <summary>
    /// Optional identifiers of related requests in the same compensation plan.
    /// </summary>
    public IReadOnlyList<string> RelatedRequestIds { get; init; } = [];

    /// <summary>
    /// Optional human-readable rationale for the compensation.
    /// </summary>
    public string? Reason { get; init; }

    internal void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(OriginalRequestId))
            throw new InvalidOperationException("Compensation requests require an original request identifier.");
    }
}
