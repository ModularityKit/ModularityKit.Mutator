using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;

namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Links;

/// <summary>
/// Represents an explicit relation between governed execution records.
/// </summary>
public sealed record GovernedExecutionLink
{
    /// <summary>
    /// Linked request identifier.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Relationship to the linked request.
    /// </summary>
    public GovernedExecutionLinkType Type { get; init; }

    /// <summary>
    /// Execution kind of the linked request.
    /// </summary>
    public GovernedExecutionKind ExecutionKind { get; init; } = GovernedExecutionKind.Standard;

    /// <summary>
    /// Compensation style associated with the link when applicable.
    /// </summary>
    public GovernedCompensationKind? CompensationKind { get; init; }

    /// <summary>
    /// Trigger that led to the compensating execution when applicable.
    /// </summary>
    public GovernedCompensationTrigger? Trigger { get; init; }

    /// <summary>
    /// Optional batch identifier associated with the compensation plan.
    /// </summary>
    public string? BatchId { get; init; }

    /// <summary>
    /// Time when the relation was recorded.
    /// </summary>
    public DateTimeOffset LinkedAt { get; init; } = DateTimeOffset.UtcNow;
}
