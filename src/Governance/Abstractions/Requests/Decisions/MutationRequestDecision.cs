using ModularityKit.Mutator.Abstractions.Context;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

/// <summary>
/// Captures single decision or lifecycle transition applied to mutation request.
/// </summary>
public sealed record MutationRequestDecision
{
    /// <summary>
    /// Type of the decision that was taken.
    /// </summary>
    public MutationRequestDecisionType Type { get; init; }

    /// <summary>
    /// Context of the actor or system that recorded the decision.
    /// </summary>
    public MutationContext Context { get; init; } = null!;

    /// <summary>
    /// Optional human-readable reason for the decision.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Timestamp at which the decision was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional metadata for governance integrations or diagnostics.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Creates lifecycle decision entry.
    /// </summary>
    /// <param name="type">Lifecycle decision type.</param>
    /// <param name="context">Actor or system context that records the decision.</param>
    /// <param name="reason">Optional human-readable explanation for the decision.</param>
    /// <param name="metadata">Optional governance metadata attached to the decision.</param>
    /// <returns>A lifecycle decision entry.</returns>
    public static MutationRequestDecision Lifecycle(
        MutationRequestLifecycleDecisionType type,
        MutationContext context,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        => Create(
            MutationRequestDecisionType.Lifecycle(type),
            context,
            reason,
            metadata);

    /// <summary>
    /// Creates an approval decision entry.
    /// </summary>
    /// <param name="type">Approval decision type.</param>
    /// <param name="context">Actor or system context that records the decision.</param>
    /// <param name="reason">Optional human-readable explanation for the decision.</param>
    /// <param name="metadata">Optional governance metadata attached to the decision.</param>
    /// <returns>An approval decision entry.</returns>
    public static MutationRequestDecision Approval(
        MutationRequestApprovalDecisionType type,
        MutationContext context,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        => Create(
            MutationRequestDecisionType.Approval(type),
            context,
            reason,
            metadata);

    /// <summary>
    /// Creates version resolution decision entry.
    /// </summary>
    /// <param name="type">Version-resolution decision type.</param>
    /// <param name="context">Actor or system context that records the decision.</param>
    /// <param name="reason">Optional human-readable explanation for the decision.</param>
    /// <param name="metadata">Optional governance metadata attached to the decision.</param>
    /// <returns>A version-resolution decision entry.</returns>
    public static MutationRequestDecision VersionResolution(
        MutationRequestVersionResolutionDecisionType type,
        MutationContext context,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        => Create(
            MutationRequestDecisionType.VersionResolution(type),
            context,
            reason,
            metadata);

    /// <summary>
    /// Creates new request decision entry.
    /// </summary>
    /// <param name="type">Decision type wrapper including category and stable code.</param>
    /// <param name="context">Actor or system context that records the decision.</param>
    /// <param name="reason">Optional human-readable explanation for the decision.</param>
    /// <param name="metadata">Optional governance metadata attached to the decision.</param>
    /// <returns>A new request decision entry.</returns>
    public static MutationRequestDecision Create(
        MutationRequestDecisionType type,
        MutationContext context,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new()
        {
            Type = type,
            Context = context,
            Reason = reason,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
}
