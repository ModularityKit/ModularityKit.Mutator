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

    /// <summary>
    /// Persists next governed request snapshot when the previous revision still matches storage.
    /// </summary>
    /// <param name="previousRequest">Previously persisted request snapshot that provides the expected revision.</param>
    /// <param name="nextRequest">Next request snapshot to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted request snapshot with updated revision.</returns>
    /// <exception cref="MutationRequestConcurrencyException">
    /// Thrown when the persisted request revision no longer matches the expected revision.
    /// </exception>
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
