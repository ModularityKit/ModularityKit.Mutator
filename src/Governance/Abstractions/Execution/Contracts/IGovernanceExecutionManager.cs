using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;

namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;

/// <summary>
/// Executes approved governance requests through version resolution and the core mutation engine.
/// </summary>
public interface IGovernanceExecutionManager
{
    /// <summary>
    /// Executes an approved governed mutation request against the provided state snapshot.
    /// </summary>
    Task<GovernedExecutionResult<TState>> ExecuteApproved<TState>(
        string requestId,
        IMutation<TState> mutation,
        TState currentState,
        string currentStateVersion,
        Func<TState, string> resultingStateVersionProvider,
        MutationContext governanceContext,
        VersionedRequestResolutionStrategy strategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an approved governed mutation request against a versioned state snapshot.
    /// </summary>
    Task<GovernedExecutionResult<TState>> ExecuteApproved<TState>(
        string requestId,
        IMutation<TState> mutation,
        TState currentState,
        MutationContext governanceContext,
        VersionedRequestResolutionStrategy strategy,
        CancellationToken cancellationToken = default)
        where TState : IVersionedState;
}
