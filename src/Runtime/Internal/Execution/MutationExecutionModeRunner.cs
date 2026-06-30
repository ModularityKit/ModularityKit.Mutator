using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityExecutionContext = ModularityKit.Mutator.Abstractions.Context.ExecutionContext;

namespace ModularityKit.Mutator.Runtime.Internal.Execution;

/// <summary>
/// Executes mutation behavior according to the current mutation mode.
/// </summary>
internal sealed class MutationExecutionModeRunner(
    IMutationExecutor executor,
    MutationEngineOptions options)
{
    private readonly IMutationExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly MutationEngineOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Runs the mutation using simulate, validate, or commit execution semantics.
    /// </summary>
    public Task<MutationResult<TState>> ExecuteAsync<TState>(MutationExecutionContext<TState> executionContext)
    {
        var executorContext = new ModularityExecutionContext
        {
            ExecutionId = executionContext.ExecutionId,
            Timeout = _options.ExecutionTimeout,
            CancellationToken = executionContext.CancellationToken
        };

        return executionContext.Mutation.Context.Mode switch
        {
            MutationMode.Simulate => Task.FromResult(executionContext.Mutation.Simulate(executionContext.State)),
            MutationMode.Validate => Task.FromResult(BuildValidationOnlyResult(
                executionContext.Mutation,
                executionContext.State)),
            _ => _executor.ExecuteAsync(
                executionContext.Mutation,
                executionContext.State,
                executorContext,
                executionContext.CancellationToken)
        };
    }

    private static MutationResult<TState> BuildValidationOnlyResult<TState>(
        IMutation<TState> mutation,
        TState state)
    {
        var validation = mutation.Validate(state);
        return validation.IsValid
            ? MutationResult<TState>.Success(state, ChangeSet.Empty)
            : MutationResult<TState>.Failure(validation);
    }
}
