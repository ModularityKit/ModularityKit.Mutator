using System.Diagnostics;
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
/// <remarks>
/// <para>
/// The pipeline runs policy evaluation, validation, mode-specific execution, and
/// outcome processing in fixed order. Each stage can short circuit:
/// a blocking policy decision skips execution; a validation failure skips execution;
/// otherwise the mutation runs through the configured mode runner and the result
/// is finalized by the outcome processor.
/// </para>
/// <para>
/// Policy evaluation timing is recorded on the metrics scope when available.
/// Validation is skipped for <c>Commit</c> mode when <c>AlwaysValidate</c> is false.
/// </para>
/// </remarks>
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
    /// Runs policy evaluation, validation, execution, and outcome processing for mutation execution context.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="executionContext">
    /// The context carrying the mutation, current state, execution metadata,
    /// cancellation token, and optional metrics scope.
    /// </param>
    /// <returns>
    /// A <see cref="MutationResult{TState}"/> containing the new state, applied changes,
    /// and final execution metrics.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The execution order is:
    /// <list type="number">
    ///   <item>Before mutation interceptor notification</item>
    ///   <item>Policy evaluation</item>
    ///   <item>Validation (when required by mode and options)</item>
    ///   <item>Mode-specific execution (simulate / validate only / commit)</item>
    ///   <item>After mutation interceptor notification, auditing, history, and metrics finalization</item>
    /// </list>
    /// </para>
    /// <para>
    /// A blocking policy decision or validation failure stops the pipeline immediately
    /// and returns the corresponding result without executing the mutation.
    /// </para>
    /// </remarks>
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
    /// Evaluates all registered policies and records the elapsed policy evaluation time in metrics.
    /// </summary>
    /// <remarks>
    /// Policy evaluation timing is measured via <c>Stopwatch.GetElapsedTime</c> and recorded on
    /// <c>MetricsScope</c> when available. The measurement excludes interceptor and validation time.
    /// </remarks>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="executionContext">The context carrying the mutation, state, execution metadata, and optional metrics scope.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing the <see cref="PolicyDecision"/> produced by policy evaluation.
    /// When the evaluator completes synchronously the result is wrapped without allocating a <c>Task</c>.
    /// </returns>
    private ValueTask<PolicyDecision> EvaluatePolicyDecisionAsync<TState>(
        MutationExecutionContext<TState> executionContext)
    {
        var policyEvaluationStart = Stopwatch.GetElapsedTime(executionContext.StartTimestamp);
        var evaluateTask = _policyEvaluator.EvaluateAsync(
            executionContext.Mutation,
            executionContext.State,
            executionContext.CancellationToken);

        if (evaluateTask.IsCompletedSuccessfully)
        {
            var policyDecision = evaluateTask.Result;
            executionContext.MetricsScope?.RecordPolicyEvaluationTime(
                Stopwatch.GetElapsedTime(executionContext.StartTimestamp) - policyEvaluationStart);
            return new ValueTask<PolicyDecision>(policyDecision);
        }

        return AwaitPolicyAndRecordAsync(evaluateTask, executionContext, policyEvaluationStart);
    }

    /// <summary>
    /// Awaits the policy evaluation task and records timing once the result is available.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="evaluateTask">The in flight policy evaluation task.</param>
    /// <param name="executionContext">The context carrying the start timestamp and optional metrics scope.</param>
    /// <param name="policyEvaluationStart">The <c>GetElapsedTime</c> value captured before the evaluator was invoked.</param>
    /// <returns>The <see cref="PolicyDecision"/> produced by policy evaluation.</returns>
    private static async ValueTask<PolicyDecision> AwaitPolicyAndRecordAsync<TState>(
        ValueTask<PolicyDecision> evaluateTask,
        MutationExecutionContext<TState> executionContext,
        TimeSpan policyEvaluationStart)
    {
        var policyDecision = await evaluateTask.ConfigureAwait(false);
        executionContext.MetricsScope?.RecordPolicyEvaluationTime(
            Stopwatch.GetElapsedTime(executionContext.StartTimestamp) - policyEvaluationStart);
        return policyDecision;
    }

    /// <summary>
    /// Validates the mutation when the current execution mode and engine options require it.
    /// </summary>
    /// <remarks>
    /// Validation is skipped entirely in <c>Commit</c> mode when <c>AlwaysValidate</c> is
    /// <c>false</c> (the performance-optimized configuration). Validation time is recorded
    /// on the metrics scope when available.
    /// </remarks>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="executionContext">The context carrying the mutation and current state. </param>
    /// <returns>
    /// A <see cref="MutationResult{TState}"/> with validation errors when validation fails;
    /// otherwise <c>null</c>.
    /// </returns>
    private MutationResult<TState>? ValidateIfRequired<TState>(
        MutationExecutionContext<TState> executionContext)
    {
        if (executionContext.Mutation.Context.Mode == MutationMode.Commit && !_options.AlwaysValidate)
            return null;

        var validationStart = Stopwatch.GetElapsedTime(executionContext.StartTimestamp);
        var validation = executionContext.Mutation.Validate(executionContext.State);
        executionContext.MetricsScope?.RecordValidationTime(
            Stopwatch.GetElapsedTime(executionContext.StartTimestamp) - validationStart);

        return validation.IsValid
            ? null
            : MutationResult<TState>.Failure(validation);
    }
}
