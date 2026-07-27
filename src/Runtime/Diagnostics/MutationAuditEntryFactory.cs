using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Runtime.Diagnostics;

/// <summary>
/// Factory for creating audit and history entries for mutations.
/// </summary>
internal static class MutationAuditEntryFactory
{
    /// <summary>
    /// Creates a successful mutation audit entry.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="mutation">The mutation that was executed.</param>
    /// <param name="result">The result of the mutation execution.</param>
    /// <param name="policyDecision">The policy decision applied to the mutation.</param>
    /// <param name="executionId">The unique identifier of the execution.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>A configured <see cref="MutationAuditEntry"/> representing success.</returns>
    public static MutationAuditEntry CreateSuccess<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        PolicyDecision policyDecision,
        string executionId,
        TimeSpan duration)
    {
        return Create(
            mutation,
            executionId,
            duration,
            isSuccess: true,
            changes: result.Changes,
            policyDecisions: result.PolicyDecisions.Count > 0 ? result.PolicyDecisions : [policyDecision],
            sideEffects: result.SideEffects,
            sourceIpAddress: mutation.Context.SourceIpAddress,
            userAgent: mutation.Context.UserAgent);
    }

    /// <summary>
    /// Creates a failed mutation audit entry.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="mutation">The mutation that was executed.</param>
    /// <param name="result">The result of the mutation execution.</param>
    /// <param name="executionId">The unique identifier of the execution.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>A configured <see cref="MutationAuditEntry"/> representing failure.</returns>
    public static MutationAuditEntry CreateFailure<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        string executionId,
        TimeSpan duration)
    {
        return Create(
            mutation,
            executionId,
            duration,
            isSuccess: false,
            changes: result.Changes,
            errorMessage: string.Join("; ", result.ValidationResult.Errors.Select(e => e.Message)),
            policyDecisions: result.PolicyDecisions,
            sideEffects: result.SideEffects);
    }

    /// <summary>
    /// Creates a failed mutation audit entry due to an exception.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="mutation">The mutation that was executed.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionId">The unique identifier of the execution.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>A configured <see cref="MutationAuditEntry"/> representing an exception failure.</returns>
    public static MutationAuditEntry CreateException<TState>(
        IMutation<TState> mutation,
        Exception exception,
        string executionId,
        TimeSpan duration)
    {
        return Create(
            mutation,
            executionId,
            duration,
            isSuccess: false,
            errorMessage: exception.Message);
    }

    /// <summary>
    /// Creates a mutation history entry for persistence.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="mutation">The mutation that was executed.</param>
    /// <param name="result">The result of the mutation execution.</param>
    /// <param name="executionId">The unique identifier of the execution.</param>
    /// <param name="stateId">The identifier of the target state.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>A configured <see cref="MutationHistoryEntry"/>.</returns>
    public static MutationHistoryEntry CreateHistoryEntry<TState>(
        IMutation<TState> mutation,
        MutationResult<TState> result,
        string executionId,
        string stateId,
        TimeSpan duration)
    {
        return new MutationHistoryEntry
        {
            ExecutionId = executionId,
            StateId = stateId,
            Intent = mutation.Intent,
            Context = mutation.Context,
            Changes = result.Changes,
            SideEffects = result.SideEffects,
            Timestamp = mutation.Context.Timestamp,
            ExecutionTime = duration
        };
    }

    /// <summary>
    /// Resolves the state identifier from the mutation context.
    /// </summary>
    /// <param name="context">The mutation context.</param>
    /// <returns>The resolved state ID or correlation ID.</returns>
    public static string? ResolveStateId(MutationContext context) =>
        context.StateId ?? context.CorrelationId;

    /// <summary>
    /// Helper method to create a mutation audit entry.
    /// </summary>
    private static MutationAuditEntry Create<TState>(
        IMutation<TState> mutation,
        string executionId,
        TimeSpan duration,
        bool isSuccess,
        ChangeSet? changes = null,
        string? errorMessage = null,
        IReadOnlyList<PolicyDecision>? policyDecisions = null,
        IReadOnlyList<SideEffect>? sideEffects = null,
        string? sourceIpAddress = null,
        string? userAgent = null)
    {
        return new MutationAuditEntry
        {
            ExecutionId = executionId,
            StateId = ResolveStateId(mutation.Context),
            StateType = typeof(TState).Name,
            MutationIntent = mutation.Intent,
            Context = mutation.Context,
            Changes = changes ?? ChangeSet.Empty,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            PolicyDecisions = policyDecisions ?? [],
            SideEffects = sideEffects ?? [],
            Timestamp = mutation.Context.Timestamp,
            Duration = duration,
            SourceIpAddress = sourceIpAddress,
            UserAgent = userAgent
        };
    }
}
