using ModularityKit.Mutator.Abstractions.Audit;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

/// <summary>
/// No-op auditor used to remove audit persistence noise from selected benchmark cases.
/// </summary>
internal sealed class NoOpAuditor : IMutationAuditor
{
    /// <summary>
    /// Ignores the supplied audit entry.
    /// </summary>
    /// <param name="entry">The audit entry.</param>
    /// <returns>A completed task.</returns>
    public Task AuditAsync(MutationAuditEntry entry) => Task.CompletedTask;

    /// <summary>
    /// Ignores the supplied audit entry.
    /// </summary>
    /// <param name="entry">The audit entry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task AuditAsync(MutationAuditEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Returns an empty audit log for the requested state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <returns>An empty audit log.</returns>
    public Task<IReadOnlyList<MutationAuditEntry>> GetAuditLogAsync(string stateId)
        => Task.FromResult<IReadOnlyList<MutationAuditEntry>>([]);

    /// <summary>
    /// Returns an empty audit log for the requested state and time range.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="from">The lower bound of the time range.</param>
    /// <param name="to">The upper bound of the time range.</param>
    /// <returns>An empty audit log.</returns>
    public Task<IReadOnlyList<MutationAuditEntry>> GetAuditLogAsync(
        string stateId,
        DateTimeOffset? from,
        DateTimeOffset? to)
        => Task.FromResult<IReadOnlyList<MutationAuditEntry>>([]);

    /// <summary>
    /// Returns an empty audit log for the requested state and time range.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="from">The lower bound of the time range.</param>
    /// <param name="to">The upper bound of the time range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An empty audit log.</returns>
    public Task<IReadOnlyList<MutationAuditEntry>> GetAuditLogAsync(
        string stateId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<MutationAuditEntry>>([]);
}
