using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Keys;
using ModularityKit.Mutator.Governance.Redis.Serialization;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Models;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Writing;

/// <summary>
/// Creates Redis persistence records from governed mutation requests.
/// </summary>
internal sealed class RedisMutationRequestPersistenceRecordFactory(
    RedisMutationRequestKeyspace keyspace)
{
    private readonly RedisMutationRequestKeyspace _keyspace = keyspace ?? throw new ArgumentNullException(nameof(keyspace));

    /// <summary>
    /// Creates a persistence record for the supplied request.
    /// </summary>
    /// <param name="request">The request to convert.</param>
    /// <returns>The Redis persistence record.</returns>
    public RedisMutationRequestPersistenceRecord Create(MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RedisMutationRequestPersistenceRecord(
            request.RequestId,
            _keyspace.RequestData(request.RequestId),
            _keyspace.RequestRevision(request.RequestId),
            RedisMutationRequestSerializer.Serialize(request),
            request.Revision);
    }
}
