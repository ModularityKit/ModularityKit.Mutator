using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Runtime.Storage.Persistence;

/// <summary>
/// Handles write-side and direct lookup operations for in-memory governed requests.
/// </summary>
internal sealed class InMemoryMutationRequestPersistence(InMemoryMutationRequestSnapshotSource snapshotSource)
{
    private readonly InMemoryMutationRequestSnapshotSource _snapshotSource =
        snapshotSource ?? throw new ArgumentNullException(nameof(snapshotSource));

    /// <summary>
    /// Creates governed request in in-memory store.
    /// </summary>
    /// <param name="request">The request to create.</param>
    /// <returns>The persisted request snapshot.</returns>
    public MutationRequest Create(MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _snapshotSource.Write(requests =>
        {
            if (requests.ContainsKey(request.RequestId))
                throw new MutationRequestAlreadyExistsException(request.RequestId);

            var persistedRequest = request with
            {
                Revision = 0
            };

            requests[request.RequestId] = persistedRequest;
            return persistedRequest;
        });
    }

    /// <summary>
    /// Stores governed request when the expected revision matches the current revision.
    /// </summary>
    /// <param name="request">The request to store.</param>
    /// <param name="expectedRevision">The expected current revision.</param>
    /// <returns>The persisted request snapshot on success, or <see langword="null"/> on conflict.</returns>
    public MutationRequest? TryStore(MutationRequest request, long expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _snapshotSource.Write(requests =>
        {
            if (!requests.TryGetValue(request.RequestId, out var currentRequest))
                return null;

            if (currentRequest.Revision != expectedRevision)
                return null;

            var persistedRequest = request with
            {
                Revision = expectedRevision + 1
            };

            requests[request.RequestId] = persistedRequest;
            return persistedRequest;
        });
    }

    /// <summary>
    /// Gets request by stable identifier.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <returns>The request snapshot when present; otherwise <see langword="null"/>.</returns>
    public MutationRequest? Get(string requestId)
        => _snapshotSource.Read(requests =>
        {
            requests.TryGetValue(requestId, out var request);
            return request;
        });

    /// <summary>
    /// Gets requests targeting specific state.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <returns>Requests ordered by creation time.</returns>
    public IReadOnlyList<MutationRequest> GetByStateId(string stateId)
        => _snapshotSource.Read<IReadOnlyList<MutationRequest>>(requests => [.. requests.Values
            .Where(request => request.StateId == stateId)
            .OrderBy(request => request.CreatedAt)]);

    /// <summary>
    /// Gets pending requests, optionally narrowed by pending reason.
    /// </summary>
    /// <param name="reason">The optional pending reason.</param>
    /// <returns>Pending requests ordered by creation time.</returns>
    public IReadOnlyList<MutationRequest> GetPending(PendingMutationReason? reason = null)
        => _snapshotSource.Read<IReadOnlyList<MutationRequest>>(requests => [.. requests.Values
            .Where(request =>
                request.Status == MutationRequestStatus.Pending &&
                (reason is null || request.PendingReason == reason))
            .OrderBy(request => request.CreatedAt)]);

    /// <summary>
    /// Gets pending requests for specific state, optionally narrowed by pending reason.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="reason">The optional pending reason.</param>
    /// <returns>Pending requests ordered by creation time.</returns>
    public IReadOnlyList<MutationRequest> GetPendingByStateId(
        string stateId,
        PendingMutationReason? reason = null)
        => _snapshotSource.Read<IReadOnlyList<MutationRequest>>(requests => [.. requests.Values
            .Where(request =>
                request.StateId == stateId &&
                request.Status == MutationRequestStatus.Pending &&
                (reason is null || request.PendingReason == reason))
            .OrderBy(request => request.CreatedAt)]);
}
