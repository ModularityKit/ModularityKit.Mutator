using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Decisions;

/// <summary>
/// Represents one decision-oriented projection from a governed mutation request.
/// </summary>
public sealed record MutationRequestDecisionView
{
    /// <summary>
    /// Parent mutation request.
    /// </summary>
    public MutationRequest Request { get; init; } = null!;

    /// <summary>
    /// Decision projected from the parent request.
    /// </summary>
    public MutationRequestDecision Decision { get; init; } = null!;
}
