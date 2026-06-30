using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Model;

/// <summary>
/// Versioned role state used by governed execution scenarios.
/// </summary>
internal sealed record RoleState(string StateId, string Role, string Version) : IVersionedState
{
    /// <summary>
    /// Creates a role state with explicit identifiers and version.
    /// </summary>
    public static RoleState Create(string stateId, string role, string version) => new(stateId, role, version);
}
