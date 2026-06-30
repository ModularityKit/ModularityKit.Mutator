using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Abstractions.Interception;
using ModularityKit.Mutator.Runtime.Diagnostics;

namespace ModularityKit.Mutator.Runtime.Internal.Execution;

/// <summary>
/// Centralizes interceptor notification and audit persistence for mutation execution failures.
/// </summary>
internal sealed class MutationExecutionFailureHandler(
    IInterceptorPipeline interceptorPipeline,
    IMutationAuditor auditor)
{
    private readonly IInterceptorPipeline _interceptorPipeline =
        interceptorPipeline ?? throw new ArgumentNullException(nameof(interceptorPipeline));

    private readonly IMutationAuditor _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));

    /// <summary>
    /// Processes known mutation exception without wrapping it again.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="executionContext">The shared execution context for the failed mutation.</param>
    /// <param name="exception">The known mutation exception.</param>
    /// <param name="duration">The elapsed execution time before failure.</param>
    public async Task HandleKnownExceptionAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        MutationException exception,
        TimeSpan duration)
    {
        await NotifyFailureAsync(
            executionContext,
            exception,
            executionContext.CancellationToken).ConfigureAwait(false);

        await AuditExceptionAsync(
            executionContext.Mutation,
            exception,
            executionContext.ExecutionId,
            duration).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes an unexpected exception and converts it into runtime level mutation exception.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="executionContext">The shared execution context for the failed mutation.</param>
    /// <param name="exception">The unexpected exception.</param>
    /// <param name="duration">The elapsed execution time before failure.</param>
    /// <returns>The wrapped runtime exception to rethrow.</returns>
    public async Task<MutationException> HandleUnexpectedExceptionAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        Exception exception,
        TimeSpan duration)
    {
        await NotifyFailureAsync(
            executionContext,
            exception,
            executionContext.CancellationToken).ConfigureAwait(false);

        await AuditExceptionAsync(
            executionContext.Mutation,
            exception,
            executionContext.ExecutionId,
            duration).ConfigureAwait(false);

        return new MutationException(
            $"Mutation execution failed: {exception.Message}",
            exception)
        {
            ExecutionId = executionContext.ExecutionId
        };
    }

    private async Task NotifyFailureAsync<TState>(
        MutationExecutionContext<TState> executionContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await _interceptorPipeline.OnMutationFailedAsync(
            executionContext.Mutation.Intent,
            executionContext.Mutation.Context,
            executionContext.State!,
            exception,
            executionContext.ExecutionId,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AuditExceptionAsync<TState>(
        IMutation<TState> mutation,
        Exception exception,
        string executionId,
        TimeSpan duration)
    {
        var entry = MutationAuditEntryFactory.CreateException(
            mutation,
            exception,
            executionId,
            duration);

        await _auditor.AuditAsync(entry).ConfigureAwait(false);
    }
}
