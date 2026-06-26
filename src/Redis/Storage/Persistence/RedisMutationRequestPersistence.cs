using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Reading;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Writing;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Persistence;

/// <summary>
/// Coordinates Redis persistence operations for governed mutation requests.
/// </summary>
internal sealed class RedisMutationRequestPersistence(
    IDatabase database,
    RedisMutationRequestPersistenceRecordFactory recordFactory,
    RedisMutationRequestPersistenceDocumentReader documentReader,
    RedisMutationRequestTransactionWriter transactionWriter)
{
    private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));
    private readonly RedisMutationRequestPersistenceRecordFactory _recordFactory = recordFactory ?? throw new ArgumentNullException(nameof(recordFactory));
    private readonly RedisMutationRequestPersistenceDocumentReader _documentReader = documentReader ?? throw new ArgumentNullException(nameof(documentReader));
    private readonly RedisMutationRequestTransactionWriter _transactionWriter = transactionWriter ?? throw new ArgumentNullException(nameof(transactionWriter));

    /// <summary>
    /// Creates a new governed mutation request in Redis with an initial revision.
    /// </summary>
    /// <param name="request">The request to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted request with provider-managed revision values applied.</returns>
    public async Task<MutationRequest> Create(MutationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var persistedRequest = request with { Revision = 0 };
        var record = _recordFactory.Create(persistedRequest);
        var transaction = _database.CreateTransaction();

        _transactionWriter.WriteCreate(transaction, record, persistedRequest);

        var committed = await transaction.ExecuteAsync().ConfigureAwait(false);
        return !committed
            ? throw new InvalidOperationException($"Mutation request '{request.RequestId}' already exists in Redis.")
            : persistedRequest;
    }

    /// <summary>
    /// Attempts to store an updated governed mutation request using optimistic concurrency.
    /// </summary>
    /// <param name="request">The request to persist.</param>
    /// <param name="expectedRevision">The expected current revision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted request if the update succeeds; otherwise <see langword="null" />.</returns>
    public async Task<MutationRequest?> TryStore(MutationRequest request, long expectedRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var currentRequest = await Get(request.RequestId, cancellationToken).ConfigureAwait(false);
        if (currentRequest is null || currentRequest.Revision != expectedRevision)
            return null;

        var persistedRequest = request with { Revision = expectedRevision + 1 };
        var record = _recordFactory.Create(persistedRequest);
        var transaction = _database.CreateTransaction();

        _transactionWriter.WriteUpdate(transaction, record, expectedRevision, currentRequest, persistedRequest);

        var committed = await transaction.ExecuteAsync().ConfigureAwait(false);
        return committed ? persistedRequest : null;
    }

    /// <summary>
    /// Reads a governed mutation request by identifier.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request if it exists; otherwise <see langword="null" />.</returns>
    public async Task<MutationRequest?> Get(string requestId, CancellationToken cancellationToken = default) =>
        await _documentReader.GetAsync(requestId, cancellationToken).ConfigureAwait(false);
}
