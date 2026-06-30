using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Interception;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime.Internal.Evaluation;

namespace ModularityKit.Mutator.Runtime.Internal.Execution;

/// <summary>
/// Orchestrates the high level mutation pipeline after concurrency admission succeeds.
/// </summary>
internal sealed class MutationExecutionPipeline(
    MutationPolicyEvaluator policyEvaluator,
    IInterceptorPipeline interceptorPipeline,
    MutationExecutionModeRunner modeRunner,
    MutationExecutionOutcomeProcessor outcomeProcessor,
    MutationEngineOptions options)
{
    private readonly MutationPolicyEvaluator _policyEvaluator = policyEvaluator ?? throw new ArgumentNullException(nameof(policyEvaluator));
    private readonly IInterceptorPipeline _interceptorPipeline = interceptorPipeline ?? throw new ArgumentNullException(nameof(interceptorPipeline));
    private readonly MutationExecutionModeRunner _modeRunner = modeRunner ?? throw new ArgumentNullException(nameof(modeRunner));
    private readonly MutationExecutionOutcomeProcessor _outcomeProcessor = outcomeProcessor ?? throw new ArgumentNullException(nameof(outcomeProcessor));
    private readonly MutationEngineOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Runs policy evaluation, validation, execution, and outcome processing for a mutation execution context.
    /// </summary>
    public async Task<MutationResult<TState>> ExecuteAsync<TState>(MutationExecutionContext<TState> executionContext)
    {
        await _interceptorPipeline.OnBeforeMutationAsync(
            executionContext.Mutation.Intent,
            executionContext.Mutation.Context,
            executionContext.State!,
            executionContext.ExecutionId,
            executionContext.CancellationToken).ConfigureAwait(false);

        var policyDecision = await EvaluatePolicyDecisionAsync(executionContext).ConfigureAwait(false);
        if (!policyDecision.IsAllowed)
            return await _outcomeProcessor
                .HandleBlockedPolicyAsync(executionContext, policyDecision)
                .ConfigureAwait(false);

        var validationFailureResult = ValidateIfRequired(executionContext);
        if (validationFailureResult is not null)
            return await _outcomeProcessor
                .HandleValidationFailureAsync(executionContext, validationFailureResult)
                .ConfigureAwait(false);

        var mutationResult = await _modeRunner.ExecuteAsync(executionContext).ConfigureAwait(false);
        return await _outcomeProcessor
            .CompleteMutationAsync(executionContext, mutationResult, policyDecision)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Evaluates policies and records the elapsed policy evaluation time in metrics.
    /// </summary>
    private async Task<PolicyDecision> EvaluatePolicyDecisionAsync<TState>(
        MutationExecutionContext<TState> executionContext)
    {
        var policyEvaluationStart = executionContext.Stopwatch.Elapsed;
        var policyDecision = await _policyEvaluator
            .EvaluateAsync(
                executionContext.Mutation,
                executionContext.State,
                executionContext.CancellationToken)
            .ConfigureAwait(false);

        executionContext.MetricsScope?.RecordPolicyEvaluationTime(
            executionContext.Stopwatch.Elapsed - policyEvaluationStart);

        return policyDecision;
    }

    /// <summary>
    /// Validates the mutation when the current mode and engine options require it.
    /// </summary>
    private MutationResult<TState>? ValidateIfRequired<TState>(
        MutationExecutionContext<TState> executionContext)
    {
        if (executionContext.Mutation.Context.Mode == MutationMode.Commit && !_options.AlwaysValidate)
            return null;

        var validationStart = executionContext.Stopwatch.Elapsed;
        var validation = executionContext.Mutation.Validate(executionContext.State);
        executionContext.MetricsScope?.RecordValidationTime(
            executionContext.Stopwatch.Elapsed - validationStart);

        return validation.IsValid
            ? null
            : MutationResult<TState>.Failure(validation);
    }
}
