namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

/// <summary>
/// Groups request-level versioning and execution completion details.
/// </summary>
public sealed record MutationRequestVersioningDetails
{
    /// <summary>
    /// Expected version or concurrency token for the target state.
    /// </summary>
    public string? ExpectedStateVersion { get; init; }

    /// <summary>
    /// Resulting version of the target state after successful governed execution.
    /// </summary>
    public string? ResultingStateVersion { get; init; }

    /// <summary>
    /// Timestamp when governed execution completed successfully.
    /// </summary>
    public DateTimeOffset? ExecutedAt { get; init; }
}
