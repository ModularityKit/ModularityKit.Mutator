using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents.Reading;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Queries.Reading;

/// <summary>
/// Loads governed request documents for Redis backed query flows.
/// </summary>
internal sealed class RedisMutationRequestQueryDocumentLoader(
    RedisMutationRequestQueryCandidateSelector candidateSelector,
    RedisMutationRequestDocumentReader documentReader)
{
    private readonly RedisMutationRequestQueryCandidateSelector _candidateSelector =
        candidateSelector ?? throw new ArgumentNullException(nameof(candidateSelector));
    private readonly RedisMutationRequestDocumentReader _documentReader =
        documentReader ?? throw new ArgumentNullException(nameof(documentReader));

    /// <summary>
    /// Loads governed request documents for a specific state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded request documents.</returns>
    public async Task<IReadOnlyList<MutationRequest>> LoadByStateIdAsync(
        string stateId,
        CancellationToken cancellationToken)
    {
        var requestIds = await _candidateSelector.LoadByStateIdAsync(stateId, cancellationToken).ConfigureAwait(false);
        return await _documentReader.LoadOrderedByCreatedAsync(requestIds, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads pending governed request documents, optionally narrowed by pending reason.
    /// </summary>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded request documents.</returns>
    public async Task<IReadOnlyList<MutationRequest>> LoadPendingAsync(
        PendingMutationReason? reason,
        CancellationToken cancellationToken)
    {
        var requestIds = await _candidateSelector.LoadPendingAsync(reason, cancellationToken).ConfigureAwait(false);
        return await _documentReader.LoadOrderedByCreatedAsync(requestIds, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads pending governed request documents for a specific state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="reason">The optional pending reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded request documents.</returns>
    public async Task<IReadOnlyList<MutationRequest>> LoadPendingByStateIdAsync(
        string stateId,
        PendingMutationReason? reason,
        CancellationToken cancellationToken)
    {
        var requestIds = await _candidateSelector.LoadPendingByStateIdAsync(stateId, reason, cancellationToken)
            .ConfigureAwait(false);
        return await _documentReader.LoadOrderedByCreatedAsync(requestIds, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads governed request documents for a general request query.
    /// </summary>
    /// <param name="query">The query to narrow through Redis candidates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded request documents.</returns>
    public async Task<IReadOnlyList<MutationRequest>> LoadByRequestQueryAsync(
        MutationRequestQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var requestIds = await _candidateSelector.LoadQueryCandidatesAsync(query, cancellationToken).ConfigureAwait(false);
        return await _documentReader.LoadOrderedByCreatedAsync(requestIds, cancellationToken).ConfigureAwait(false);
    }
}
