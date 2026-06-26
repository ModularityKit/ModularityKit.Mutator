using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents.Keys;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents.Materialization;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents.Payloads;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Documents.Reading;

/// <summary>
/// Coordinates Redis document key creation, payload reads, and request materialization.
/// </summary>
internal sealed class RedisMutationRequestDocumentReader(
    RedisMutationRequestDocumentKeyFactory keyFactory,
    RedisMutationRequestPayloadReader payloadReader)
{
    private readonly RedisMutationRequestDocumentKeyFactory _keyFactory =
        keyFactory ?? throw new ArgumentNullException(nameof(keyFactory));
    private readonly RedisMutationRequestPayloadReader _payloadReader =
        payloadReader ?? throw new ArgumentNullException(nameof(payloadReader));

    /// <summary>
    /// Loads request documents ordered by request creation time.
    /// </summary>
    /// <param name="requestIds">The request identifiers to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The materialized and ordered mutation requests.</returns>
    public Task<IReadOnlyList<MutationRequest>> LoadOrderedByCreatedAsync(IEnumerable<string> requestIds, CancellationToken cancellationToken) =>
        LoadAsync(requestIds, cancellationToken, orderByCreated: true);

    /// <summary>
    /// Loads request documents for the supplied identifiers.
    /// </summary>
    /// <param name="requestIds">The request identifiers to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="orderByCreated">Whether to order results by request creation time.</param>
    /// <returns>The materialized mutation requests.</returns>
    public async Task<IReadOnlyList<MutationRequest>> LoadAsync(IEnumerable<string> requestIds, CancellationToken cancellationToken, bool orderByCreated)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = _keyFactory.CreateKeys(requestIds);
        if (keys.Count == 0)
            return [];

        var values = await _payloadReader.LoadAsync(keys, cancellationToken).ConfigureAwait(false);
        return RedisMutationRequestDocumentMaterializer.Materialize(values, cancellationToken, orderByCreated);
    }
}
