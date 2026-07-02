using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Runtime.Interception;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

/// <summary>
/// Logging style interceptor that formats lifecycle messages and writes them to a sink.
/// </summary>
internal sealed class FormattingLoggingInterceptor : MutationInterceptorBase
{
    private readonly TextWriter _sink = TextWriter.Null;

    /// <summary>
    /// Gets the interceptor name used in benchmark scenarios.
    /// </summary>
    public override string Name => nameof(FormattingLoggingInterceptor);

    /// <summary>
    /// Formats and writes pre mutation log line to the configured sink.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation context.</param>
    /// <param name="state">The current state.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public override Task OnBeforeMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        string executionId,
        CancellationToken cancellationToken = default)
    {
        _sink.WriteLine($"[Before] {intent.OperationName} by {context.ActorId} (ExecutionId: {executionId})");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Formats and writes post mutation log lines, including the generated change set, to the configured sink.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The mutation context.</param>
    /// <param name="oldState">The state before mutation execution.</param>
    /// <param name="newState">The state after mutation execution.</param>
    /// <param name="changes">The generated change set.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public override Task OnAfterMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object? oldState,
        object? newState,
        ChangeSet changes,
        string executionId,
        CancellationToken cancellationToken = default)
    {
        _sink.WriteLine($"[After] {intent.OperationName}, changes: {changes.Changes.Count} (ExecutionId: {executionId})");

        foreach (var change in changes.Changes)
            _sink.WriteLine($"  - {change.Path}: {change.OldValue} -> {change.NewValue}");

        return Task.CompletedTask;
    }
}
