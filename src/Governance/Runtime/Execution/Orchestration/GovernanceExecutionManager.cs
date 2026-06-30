using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Runtime.Execution.Mutation;
using ModularityKit.Mutator.Governance.Runtime.Execution.Outcome;
using ModularityKit.Mutator.Governance.Runtime.Execution.Persistence;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;

/// <summary>
/// Closes the loop from approved governance request to core mutation execution and terminal request state.
/// </summary>
public sealed class GovernanceExecutionManager(
    IMutationRequestStore requestStore,
    IMutationRequestVersionResolutionManager resolutionManager,
    IMutationEngine mutationEngine) : IGovernanceExecutionManager
{
    private readonly IMutationRequestVersionResolutionManager _resolutionManager = resolutionManager ?? throw new ArgumentNullException(nameof(resolutionManager));
    private readonly IMutationEngine _mutationEngine = mutationEngine ?? throw new ArgumentNullException(nameof(mutationEngine));
    private readonly GovernedExecutionOutcomeHandler _outcomeHandler =
        new(new GovernedExecutionRequestPersistence(requestStore ?? throw new ArgumentNullException(nameof(requestStore))));

    public async Task<GovernedExecutionResult<TState>> ExecuteApproved<TState>(
        string requestId,
        IMutation<TState> mutation,
        TState currentState,
        string currentStateVersion,
        Func<TState, string> resultingStateVersionProvider,
        MutationContext governanceContext,
        VersionedRequestResolutionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID is required.", nameof(requestId));

        if (string.IsNullOrWhiteSpace(currentStateVersion))
            throw new ArgumentException("Current state version is required.", nameof(currentStateVersion));

        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(resultingStateVersionProvider);
        ArgumentNullException.ThrowIfNull(governanceContext);

        var execution = await ResolveExecutionContext(
            requestId,
            mutation,
            currentState,
            currentStateVersion,
            resultingStateVersionProvider,
            governanceContext,
            strategy,
            cancellationToken).ConfigureAwait(false);

        if (execution.Resolution.Outcome is MutationRequestVersionResolutionOutcome.RejectedAsStale or
            MutationRequestVersionResolutionOutcome.RequiresRenewedApproval)
        {
            return _outcomeHandler.BuildNonExecutedResult<TState>(execution.Resolution);
        }

        MutationResult<TState> mutationResult;

        try
        {
            mutationResult = await ExecuteMutation(execution, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _outcomeHandler.PersistException(execution, ex, cancellationToken).ConfigureAwait(false);
            throw;
        }

        return await _outcomeHandler
            .HandleMutationResult(execution, mutationResult, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<GovernedExecutionResult<TState>> ExecuteApproved<TState>(
        string requestId,
        IMutation<TState> mutation,
        TState currentState,
        MutationContext governanceContext,
        VersionedRequestResolutionStrategy strategy,
        CancellationToken cancellationToken = default)
        where TState : IVersionedState
        => ExecuteApproved(
            requestId,
            mutation,
            currentState,
            currentState.Version,
            state => state.Version,
            governanceContext,
            strategy,
            cancellationToken);

    private async Task<GovernedExecutionContext<TState>> ResolveExecutionContext<TState>(
        string requestId,
        IMutation<TState> mutation,
        TState currentState,
        string currentStateVersion,
        Func<TState, string> resultingStateVersionProvider,
        MutationContext governanceContext,
        VersionedRequestResolutionStrategy strategy,
        CancellationToken cancellationToken)
    {
        var resolution = await _resolutionManager.ResolveAndStore(
            requestId,
            currentStateVersion,
            governanceContext,
            strategy,
            cancellationToken).ConfigureAwait(false);

        return new GovernedExecutionContext<TState>(
            resolution,
            new GovernedMutation<TState>(mutation, resolution.Request),
            currentState,
            currentStateVersion,
            resultingStateVersionProvider,
            governanceContext);
    }

    private Task<MutationResult<TState>> ExecuteMutation<TState>(
        GovernedExecutionContext<TState> execution,
        CancellationToken cancellationToken)
        => _mutationEngine.ExecuteAsync(
            execution.Mutation,
            execution.CurrentState,
            cancellationToken);
}
