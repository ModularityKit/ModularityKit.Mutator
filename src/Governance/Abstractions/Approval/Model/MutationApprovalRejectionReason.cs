namespace ModularityKit.Mutator.Governance.Abstractions.Approval.Model;

/// <summary>
/// Represents a structured business reason for rejecting an approval requirement.
/// </summary>
public sealed record MutationApprovalRejectionReason
{
    /// <summary>
    /// Stable machine-readable rejection code.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Optional higher-level rejection category.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Human-readable rejection message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Additional structured metadata attached to the rejection.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
