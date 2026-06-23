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

        var resolution = await _resolutionManager.ResolveAndStore(
            requestId,
            currentStateVersion,
            governanceContext,
            strategy,
            cancellationToken).ConfigureAwait(false);

        if (resolution.Outcome is MutationRequestVersionResolutionOutcome.RejectedAsStale or
            MutationRequestVersionResolutionOutcome.RequiresRenewedApproval)
        {
            return _outcomeHandler.BuildNonExecutedResult<TState>(resolution);
        }

        var governedMutation = new GovernedMutation<TState>(mutation, requestId, resolution.Request.StateId);
        MutationResult<TState> mutationResult;

        try
        {
            mutationResult = await _mutationEngine
                .ExecuteAsync(governedMutation, currentState, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _outcomeHandler.PersistRejectedExecution(
                resolution.Request,
                governanceContext,
                $"Governed execution threw '{ex.GetType().Name}': {ex.Message}",
                new Dictionary<string, object>
                {
                    ["CurrentStateVersion"] = currentStateVersion,
                    ["ExecutionFailureType"] = ex.GetType().Name
                },
                cancellationToken).ConfigureAwait(false);

            throw;
        }

        if (!mutationResult.IsSuccess || mutationResult.NewState is null)
        {
            var rejectedRequest = await _outcomeHandler.PersistRejectedExecution(
                resolution.Request,
                governanceContext,
                GovernedExecutionDecisionFactory.BuildRejectedExecutionReason(mutationResult),
                new Dictionary<string, object>
                {
                    ["CurrentStateVersion"] = currentStateVersion,
                    ["HasPolicyDecisions"] = mutationResult.PolicyDecisions.Count > 0,
                    ["HasValidationErrors"] = !mutationResult.ValidationResult.IsValid
                },
                cancellationToken).ConfigureAwait(false);

            return _outcomeHandler.BuildNonExecutedResult(
                resolution with { Request = rejectedRequest },
                mutationResult);
        }

        var resultingStateVersion = resultingStateVersionProvider(mutationResult.NewState);
        if (string.IsNullOrWhiteSpace(resultingStateVersion))
            throw new InvalidOperationException("Governed execution requires a non-empty resulting state version.");

        var executedRequest = await _outcomeHandler.PersistExecutedRequest(
            resolution.Request,
            resultingStateVersion,
            governanceContext,
            mutationResult,
            cancellationToken).ConfigureAwait(false);

        return _outcomeHandler.BuildExecutedResult(
            resolution,
            mutationResult,
            executedRequest,
            resultingStateVersion);
    }
}
