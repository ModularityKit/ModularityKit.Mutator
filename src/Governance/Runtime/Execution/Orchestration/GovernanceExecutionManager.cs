using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Links;
using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
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
    private readonly IMutationRequestStore _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
    private readonly IMutationRequestVersionResolutionManager _resolutionManager = resolutionManager ?? throw new ArgumentNullException(nameof(resolutionManager));
    private readonly IMutationEngine _mutationEngine = mutationEngine ?? throw new ArgumentNullException(nameof(mutationEngine));
    private readonly GovernedExecutionOutcomeHandler _outcomeHandler =
        new(new GovernedExecutionRequestPersistence(requestStore ?? throw new ArgumentNullException(nameof(requestStore))));

    /// <summary>
    /// Executes approved governed mutation request against provided state snapshot.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="requestId">Stable identifier of the governed mutation request.</param>
    /// <param name="mutation">Mutation instance to execute after governance resolution succeeds.</param>
    /// <param name="currentState">Current state snapshot used for execution.</param>
    /// <param name="currentStateVersion">Current version or concurrency token of the state snapshot.</param>
    /// <param name="resultingStateVersionProvider">Delegate that resolves the resulting state version from the post-mutation state.</param>
    /// <param name="governanceContext">Context describing the governance actor or service performing execution.</param>
    /// <param name="strategy">Version-resolution strategy applied before execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The governed execution result, including persisted request state and optional mutation outcome.</returns>
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

        var result = await _outcomeHandler
            .HandleMutationResult(execution, mutationResult, cancellationToken)
            .ConfigureAwait(false);

        if (result.WasExecuted)
            await LinkCompensationExecution(result.Request, governanceContext, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Executes approved governed mutation request against versioned state snapshot.
    /// </summary>
    /// <typeparam name="TState">The versioned state type handled by the governed mutation.</typeparam>
    /// <param name="requestId">Stable identifier of the governed mutation request.</param>
    /// <param name="mutation">Mutation instance to execute after governance resolution succeeds.</param>
    /// <param name="currentState">Current versioned state snapshot used for execution.</param>
    /// <param name="governanceContext">Context describing the governance actor or service performing execution.</param>
    /// <param name="strategy">Version-resolution strategy applied before execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The governed execution result, including persisted request state and optional mutation outcome.</returns>
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

    /// <summary>
    /// Resolves the governed request and builds execution context for the core mutation engine.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="requestId">Stable identifier of the governed mutation request.</param>
    /// <param name="mutation">Mutation instance to wrap for governed execution.</param>
    /// <param name="currentState">Current state snapshot used for execution.</param>
    /// <param name="currentStateVersion">Current version or concurrency token of the state snapshot.</param>
    /// <param name="resultingStateVersionProvider">Delegate that resolves the resulting state version from the post-mutation state.</param>
    /// <param name="governanceContext">Context describing the governance actor or service performing execution.</param>
    /// <param name="strategy">Version-resolution strategy applied before execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved execution context passed to the core mutation engine.</returns>
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

    /// <summary>
    /// Executes the wrapped governed mutation through the core mutation engine.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the governed mutation.</typeparam>
    /// <param name="execution">Resolved governed execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The core mutation result.</returns>
    private Task<MutationResult<TState>> ExecuteMutation<TState>(
        GovernedExecutionContext<TState> execution,
        CancellationToken cancellationToken)
        => _mutationEngine.ExecuteAsync(
            execution.Mutation,
            execution.CurrentState,
            cancellationToken);

    /// <summary>
    /// Links successful compensating execution back to the original governed request.
    /// </summary>
    /// <param name="executedRequest">Persisted compensating request after successful execution.</param>
    /// <param name="governanceContext">Context describing the governance actor or service recording the compensation link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task LinkCompensationExecution(
        MutationRequest executedRequest,
        MutationContext governanceContext,
        CancellationToken cancellationToken)
    {
        if (executedRequest.Execution.Kind != GovernedExecutionKind.Compensation || executedRequest.Execution.Compensation is null)
            return;

        executedRequest.Execution.Compensation.EnsureValid();

        var originalRequest = await _requestStore
            .Get(executedRequest.Execution.Compensation.OriginalRequestId, cancellationToken)
            .ConfigureAwait(false)
                ?? throw new MutationRequestNotFoundException(executedRequest.Execution.Compensation.OriginalRequestId);

        if (originalRequest.Execution.RelatedExecutions.Any(link => link.RequestId == executedRequest.RequestId &&
        link.Type == GovernedExecutionLinkType.CompensatedBy))
            return;

        var linkedAt = executedRequest.Versioning.ExecutedAt ?? DateTimeOffset.UtcNow;
        var decision = GovernedExecutionDecisionFactory.CreateCompensatedDecision(
            governanceContext,
            executedRequest.RequestId,
            executedRequest.Execution.Compensation,
            executedRequest.Versioning.ResultingStateVersion ?? string.Empty,
            linkedAt);

        var updatedOriginalRequest = originalRequest with
        {
            Lifecycle = originalRequest.Lifecycle with
            {
                UpdatedAt = linkedAt
            },
            Execution = originalRequest.Execution with
            {
                RelatedExecutions =
                [
                    .. originalRequest.Execution.RelatedExecutions,
                    new GovernedExecutionLink
                    {
                        RequestId = executedRequest.RequestId,
                        Type = GovernedExecutionLinkType.CompensatedBy,
                        ExecutionKind = GovernedExecutionKind.Compensation,
                        CompensationKind = executedRequest.Execution.Compensation.Kind,
                        Trigger = executedRequest.Execution.Compensation.Trigger,
                        BatchId = executedRequest.Execution.Compensation.BatchId,
                        LinkedAt = linkedAt
                    }
                ]
            },
            Decisions = [.. originalRequest.Decisions, decision]
        };

        var persistedRequest = await _requestStore
            .TryStore(updatedOriginalRequest, originalRequest.Revision, cancellationToken)
            .ConfigureAwait(false)
                ?? throw new MutationRequestConcurrencyException(originalRequest.RequestId, originalRequest.Revision);
    }
}
