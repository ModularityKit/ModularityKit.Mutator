namespace ModularityKit.Mutator.Abstractions.Context;

/// <summary>
/// Represents the execution context of a mutation.
/// Answers the questions: WHO, WHEN, WHY, and HOW the mutation is performed.
/// </summary>
/// <remarks>
/// <para>
/// This context provides metadata for auditing, tracking, and governance of mutations
/// within the system. It captures information about the actor (user, system, or service),
/// the reason for the mutation, timestamps, correlation identifiers, and additional metadata.
/// </para>
/// <para>
/// Common usage:
/// <list type="bullet">
/// <item>Audit trails</item>
/// <item>Correlation and causation in distributed systems</item>
/// <item>Simulation and validation of mutations</item>
/// </list>
/// </para>
/// </remarks>
public sealed record MutationContext
{
    /// <summary>
    /// Identifier of the state being mutated (if applicable).
    /// </summary>
    public string? StateId { get; init; }

    /// <summary>
    /// Identifier of the actor performing the mutation.
    /// </summary>
    public string? ActorId { get; init; }

    /// <summary>
    /// Human-readable name of the actor.
    /// </summary>
    public string? ActorName { get; init; }

    /// <summary>
    /// Type of actor performing the mutation.
    /// </summary>
    public ActorType ActorType { get; internal init; } = ActorType.Unknown;

    /// <summary>
    /// Reason for performing the mutation.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Mode of mutation execution (commit, dry-run, etc.).
    /// </summary>
    public MutationMode Mode { get; init; } = MutationMode.Commit;

    /// <summary>
    /// Correlation ID for distributed system tracking.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// ID of the command or event that caused this mutation.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// User session identifier.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Source IP address (if applicable).
    /// </summary>
    public string? SourceIpAddress { get; init; }

    /// <summary>
    /// User agent string (if applicable).
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Timestamp when the mutation was initiated.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Time zone of the actor.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; init; }

    /// <summary>
    /// Culture of the actor (e.g., "en-US").
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// Additional contextual metadata for the mutation.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Creates a system-level mutation context.
    /// </summary>
    /// <param name="reason">Reason for the system mutation.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>A <see cref="MutationContext"/> representing a system actor.</returns>
    public static MutationContext System(string reason, string? correlationId = null)
        => new()
        {
            ActorType = ActorType.System,
            Reason = reason,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };

    /// <summary>
    /// Creates a user-level mutation context.
    /// </summary>
    /// <param name="userId">User ID performing the mutation.</param>
    /// <param name="userName">Optional username.</param>
    /// <param name="reason">Optional reason for mutation.</param>
    /// <returns>A <see cref="MutationContext"/> representing a user actor.</returns>
    public static MutationContext User(string userId, string? userName = null, string? reason = null)
        => new()
        {
            ActorId = userId,
            ActorName = userName,
            ActorType = ActorType.User,
            Reason = reason
        };

    /// <summary>
    /// Creates a service-level mutation context.
    /// </summary>
    /// <param name="serviceId">Service ID performing the mutation.</param>
    /// <param name="reason">Reason for the service mutation.</param>
    /// <returns>A <see cref="MutationContext"/> representing a service actor.</returns>
    public static MutationContext Service(string serviceId, string reason)
        => new()
        {
            ActorId = serviceId,
            ActorType = ActorType.Service,
            Reason = reason
        };
}
