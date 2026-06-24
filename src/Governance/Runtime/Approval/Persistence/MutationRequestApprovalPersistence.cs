using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;

namespace ModularityKit.Mutator.Governance.Runtime.Approval.Persistence;

/// <summary>
/// Persists approval related request transitions with optimistic concurrency checks.
/// </summary>
internal sealed class MutationRequestApprovalPersistence(IMutationRequestStore requestStore)
{
    private readonly IMutationRequestStore _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));

    /// <summary>
    /// Persists an approval related request transition using guarded optimistic concurrency.
    /// </summary>
    public async Task<MutationRequest> Persist(
        MutationRequest previousRequest,
        MutationRequest nextRequest,
        CancellationToken cancellationToken)
    {
        var persistedRequest = await _requestStore
            .TryStore(nextRequest, previousRequest.Revision, cancellationToken)
            .ConfigureAwait(false);

        return persistedRequest is null
            ? throw new MutationRequestConcurrencyException(previousRequest.RequestId, previousRequest.Revision)
            : persistedRequest;
    }
}
