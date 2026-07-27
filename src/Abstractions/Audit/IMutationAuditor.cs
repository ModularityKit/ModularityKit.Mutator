namespace ModularityKit.Mutator.Abstractions.Audit;

/// <summary>
/// Audits mutation operations. Responsible for recording, storing, and retrieving
/// mutation audit entries for traceability and compliance.
/// </summary>
/// <remarks>
/// Mutation auditors track changes applied to state objects via mutations. They can be used
/// for compliance, debugging, analytics, or security reviews. Implementations may persist
/// audit entries in databases, event stores, or logging systems.
/// </remarks>
public interface IMutationAuditor
{
    /// <summary>
    /// When <see langword="false" />, the runtime may skip creating audit entries for this auditor.
    /// </summary>
    bool IsEnabled => true;

    /// <summary>
    /// Records an audit entry for a mutation.
    /// </summary>
    /// <param name="entry">The mutation audit entry to store.</param>
    Task AuditAsync(MutationAuditEntry entry);

    /// <summary>
    /// Records an audit entry for a mutation.
    /// </summary>
    /// <param name="entry">The mutation audit entry to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AuditAsync(
        MutationAuditEntry entry,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves audit log entries for a specific state object.
    /// </summary>
    /// <param name="stateId">Identifier of the state object.</param>
    /// <returns>Read-only list of mutation audit entries.</returns>
    Task<IReadOnlyList<MutationAuditEntry>> GetAuditLogAsync(string stateId);

    /// <summary>
    /// Retrieves audit log entries for a specific state object.
    /// </summary>
    /// <param name="stateId">Identifier of the state object.</param>
    /// <param name="from">Start timestamp for filtering entries.</param>
    /// <param name="to">End timestamp for filtering entries.</param>
    /// <returns>Read-only list of mutation audit entries.</returns>
    Task<IReadOnlyList<MutationAuditEntry>> GetAuditLogAsync(
        string stateId,
        DateTimeOffset? from,
        DateTimeOffset? to);

    /// <summary>
    /// Retrieves audit log entries for a specific state object.
    /// </summary>
    /// <param name="stateId">Identifier of the state object.</param>
    /// <param name="from">Start timestamp for filtering entries.</param>
    /// <param name="to">End timestamp for filtering entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<MutationAuditEntry>> GetAuditLogAsync(
        string stateId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken);
}