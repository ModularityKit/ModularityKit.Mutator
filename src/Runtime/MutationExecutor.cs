using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityExecutionContext = ModularityKit.Mutator.Abstractions.Context.ExecutionContext;

namespace ModularityKit.Mutator.Runtime;

/// <summary>
/// Responsible for executing a single mutation against a given state.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MutationExecutor"/> applies mutations in synchronous fashion (the <see cref="IMutation{TState}.Apply"/>
/// method is executed inline) while respecting timeouts and cancellation tokens provided via the execution context and
/// the caller's cancellation token.
/// </para>
/// <para>
/// This class does not perform policy checks, auditing, or interceptor pipelines — it is a low level executor
/// used by <see cref="MutationEngine"/> to actually run mutations.
/// </para>
/// </remarks>
internal sealed class MutationExecutor : IMutationExecutor
{
    /// <summary>
    /// Executes a mutation against the provided state.
    /// </summary>
    /// <typeparam name="TState">The type of the state being mutated.</typeparam>
    /// <param name="mutation">The mutation instance to execute.</param>
    /// <param name="state">The current state on which the mutation will be applied.</param>
    /// <param name="context">
    /// Execution context containing metadata, cancellation, and timeout information.
    /// </param>
    /// <param name="cancellationToken">Optional token to observe cancellation requests.</param>
    /// <returns>The result of the mutation execution, including the new state and applied changes.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the mutation execution is canceled via <paramref name="cancellationToken" />.
    /// </exception>
    /// <exception cref="ExecutionTimeoutException">
    /// Thrown if the configured execution timeout elapses before or during execution.
    /// </exception>
    public Task<MutationResult<TState>> ExecuteAsync<TState>(
        IMutation<TState> mutation,
        TState state,
        ModularityExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.IsTimedOut())
        {
            return Task.FromException<MutationResult<TState>>(new ExecutionTimeoutException(
                context.Timeout!.Value,
                DateTimeOffset.UtcNow - context.StartedAt));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<MutationResult<TState>>(cancellationToken);
        }

        try
        {
            var result = mutation.Apply(state);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromException<MutationResult<TState>>(ex);
        }
    }
}
