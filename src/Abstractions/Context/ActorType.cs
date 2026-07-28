namespace ModularityKit.Mutator.Abstractions.Context;

/// <summary>
/// Defines the type of actor performing a mutation.
/// Provides context for auditing, tracking, and governance of system changes.
/// </summary>
public enum ActorType
{
    /// <summary>
    /// Unknown actor type.
    /// Use when the actor cannot be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Human user performing the mutation.
    /// </summary>
    User = 1,

    /// <summary>
    /// System-initiated operation (automatic or internal process).
    /// </summary>
    System = 2,

    /// <summary>
    /// Service or microservice performing the mutation.
    /// </summary>
    Service = 3,

    /// <summary>
    /// Policy engine or automated governance rule.
    /// </summary>
    Policy = 4,

    /// <summary>
    /// Scheduled operation executed by a scheduler.
    /// </summary>
    Scheduler = 5,

    /// <summary>
    /// Administrator performing the mutation.
    /// Typically, a privileged user with elevated rights.
    /// </summary>
    Administrator = 6
}
