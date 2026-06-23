using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Model;

/// <summary>
/// Captures the outcome of executing a governed mutation request.
/// </summary>
public sealed record GovernedExecutionResult<TState>
{
    /// <summary>
    /// Latest persisted request snapshot after resolution and optional execution.
    /// </summary>
    public MutationRequest Request { get; init; } = null!;

    /// <summary>
    /// Version-resolution outcome that gated execution.
    /// </summary>
    public MutationRequestVersionResolution Resolution { get; init; } = null!;

    /// <summary>
    /// Core mutation result when execution actually ran.
    /// </summary>
    public MutationResult<TState>? MutationResult { get; init; }

    /// <summary>
    /// Indicates whether the core mutation engine executed the request.
    /// </summary>
    public bool WasExecuted { get; init; }

    /// <summary>
    /// Resulting state version recorded after a successful execution.
    /// </summary>
    public string? ResultingStateVersion { get; init; }
}
