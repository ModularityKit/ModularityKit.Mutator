using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

/// <summary>
/// Represents a governed mutation request that may execute immediately or enter a pending lifecycle.
/// </summary>
public sealed record MutationRequest
{
    /// <summary>
    /// Stable identifier for the mutation request.
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Identifier of the state targeted by this request.
    /// </summary>
    public string StateId { get; init; } = string.Empty;

    /// <summary>
    /// Logical state type targeted by the request.
    /// </summary>
    public string StateType { get; init; } = string.Empty;

    /// <summary>
    /// CLR type name of the underlying mutation.
    /// </summary>
    public string MutationType { get; init; } = string.Empty;

    /// <summary>
    /// Intent associated with the requested mutation.
    /// </summary>
    public MutationIntent Intent { get; init; } = null!;

    /// <summary>
    /// Request context describing who requested the mutation and why.
    /// </summary>
    public MutationContext Context { get; init; } = null!;

    /// <summary>
    /// Current lifecycle status of the request.
    /// </summary>
    public MutationRequestStatus Status { get; init; } = MutationRequestStatus.Created;

    /// <summary>
    /// Reason why the request is pending, if it has not executed yet.
    /// </summary>
    public PendingMutationReason? PendingReason { get; init; }

    /// <summary>
    /// Requirements that must be fulfilled before execution may proceed.
    /// </summary>
    public IReadOnlyList<PolicyRequirement> Requirements { get; init; } = [];

    /// <summary>
    /// Concrete request-level approval requirements derived from governance policy requirements.
    /// </summary>
    public IReadOnlyList<MutationApprovalRequirement> ApprovalRequirements { get; init; } = [];

    /// <summary>
    /// Governance decisions recorded against this request over time.
    /// </summary>
    public IReadOnlyList<MutationRequestDecision> Decisions { get; init; } = [];

    /// <summary>
    /// Side effects captured from governed execution results for this request.
    /// </summary>
    public IReadOnlyList<SideEffect> SideEffects { get; init; } = [];

    /// <summary>
    /// Optimistic concurrency revision for the governed request.
    /// </summary>
    public long Revision { get; init; }

    /// <summary>
    /// Expected version or concurrency token for the target state.
    /// </summary>
    public string? ExpectedStateVersion { get; init; }

    /// <summary>
    /// Resulting version of the target state after successful governed execution.
    /// </summary>
    public string? ResultingStateVersion { get; init; }

    /// <summary>
    /// Optional expiration time for pending requests.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Timestamp when governed execution completed successfully.
    /// </summary>
    public DateTimeOffset? ExecutedAt { get; init; }

    /// <summary>
    /// Timestamp when the request was first created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp of the last lifecycle update applied to the request.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional governance metadata carried by the request.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
