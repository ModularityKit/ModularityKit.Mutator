using System.Text.Json.Serialization;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

/// <summary>
/// Represents governed mutation request that may execute immediately or enter a pending lifecycle.
/// </summary>
public sealed record MutationRequest
{
    /// <summary>
    /// Stable identifier for the mutation request.
    /// </summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Target scope details for the governed request.
    /// </summary>
    public MutationRequestScopeDetails Scope { get; init; } = new();

    /// <summary>
    /// Submitted mutation payload details for the governed request.
    /// </summary>
    public MutationRequestPayloadDetails Payload { get; init; } = new();

    /// <summary>
    /// Lifecycle state and lifecycle timestamps associated with the request.
    /// </summary>
    public MutationRequestLifecycleDetails Lifecycle { get; init; } = new();

    /// <summary>
    /// Governed execution-specific details associated with this request.
    /// </summary>
    public GovernedExecutionDetails Execution { get; init; } = new();

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
    /// Versioning and execution completion details associated with the request.
    /// </summary>
    public MutationRequestVersioningDetails Versioning { get; init; } = new();

    /// <summary>
    /// Additional governance metadata carried by the request.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    [JsonIgnore]
    public string StateId => Scope.StateId;

    [JsonIgnore]
    public string StateType => Scope.StateType;

    [JsonIgnore]
    public string MutationType => Scope.MutationType;

    [JsonIgnore]
    public MutationIntent Intent => Payload.Intent;

    [JsonIgnore]
    public MutationContext Context => Payload.Context;

    [JsonIgnore]
    public MutationRequestStatus Status => Lifecycle.Status;

    [JsonIgnore]
    public PendingMutationReason? PendingReason => Lifecycle.PendingReason;

    [JsonIgnore]
    public DateTimeOffset? ExpiresAt => Lifecycle.ExpiresAt;

    [JsonIgnore]
    public DateTimeOffset CreatedAt => Lifecycle.CreatedAt;

    [JsonIgnore]
    public DateTimeOffset UpdatedAt => Lifecycle.UpdatedAt;
}
