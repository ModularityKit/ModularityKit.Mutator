namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

/// <summary>
/// Groups target scope identifiers for a governed mutation request.
/// </summary>
public sealed record MutationRequestScopeDetails
{
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
}
