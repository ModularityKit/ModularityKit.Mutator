namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;

/// <summary>
/// Represents a state snapshot that carries a stable version token.
/// </summary>
public interface IVersionedState
{
    /// <summary>
    /// Gets the current version token for the state snapshot.
    /// </summary>
    string Version { get; }
}
