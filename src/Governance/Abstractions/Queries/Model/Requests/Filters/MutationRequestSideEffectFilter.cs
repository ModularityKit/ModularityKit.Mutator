using ModularityKit.Mutator.Abstractions.Effects;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Filters governed requests by persisted side effect characteristics.
/// </summary>
public sealed record MutationRequestSideEffectFilter
{
    /// <summary>
    /// Side effect types that may appear on the request.
    /// </summary>
    public IReadOnlySet<string> Types { get; init; } = new HashSet<string>();

    /// <summary>
    /// Stable side effect payload contract identifiers that may appear on the request.
    /// </summary>
    public IReadOnlySet<string> DataContractTypes { get; init; } = new HashSet<string>();

    /// <summary>
    /// Side effect severity levels that may appear on the request.
    /// </summary>
    public IReadOnlySet<SideEffectSeverity> Severities { get; init; } = new HashSet<SideEffectSeverity>();

    /// <summary>
    /// Optional flag indicating whether the request must contain a side effect that requires action.
    /// </summary>
    public bool? RequiresAction { get; init; }
}
