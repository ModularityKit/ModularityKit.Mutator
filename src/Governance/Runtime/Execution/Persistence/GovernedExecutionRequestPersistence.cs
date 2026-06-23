using ModularityKit.Mutator.Governance.Abstractions.Exceptions.Storage;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Storage;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Persistence;

/// <summary>
/// Persists request transitions for governed execution with optimistic concurrency checks.
/// </summary>
internal sealed class GovernedExecutionRequestPersistence(IMutationRequestStore requestStore)
{
    private readonly IMutationRequestStore _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));

    public async Task<MutationRequest> Persist(
        MutationRequest previousRequest,
        MutationRequest nextRequest,
        CancellationToken cancellationToken)
    {
        var persistedRequest = await _requestStore
            .TryStore(nextRequest, previousRequest.Revision, cancellationToken)
            .ConfigureAwait(false);

        if (persistedRequest is null)
            throw new MutationRequestConcurrencyException(previousRequest.RequestId, previousRequest.Revision);

        return persistedRequest;
    }
}
