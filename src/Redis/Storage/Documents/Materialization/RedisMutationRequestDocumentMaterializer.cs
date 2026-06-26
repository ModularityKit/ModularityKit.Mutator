using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Serialization;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Documents.Materialization;

/// <summary>
/// Materializes governed mutation requests from Redis payload values.
/// </summary>
internal static class RedisMutationRequestDocumentMaterializer
{
    /// <summary>
    /// Deserializes request payloads and optionally orders the resulting requests by creation time.
    /// </summary>
    /// <param name="values">The raw Redis payload values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="orderByCreated">Whether to order results by creation timestamp.</param>
    /// <returns>The materialized mutation requests.</returns>
    public static IReadOnlyList<MutationRequest> Materialize(IReadOnlyList<RedisValue> values, CancellationToken cancellationToken, bool orderByCreated)
    {
        var requests = new List<MutationRequest>(values.Count);

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (value.HasValue)
                requests.Add(RedisMutationRequestSerializer.Deserialize(value!));
        }

        if (!orderByCreated)
            return requests;

        return requests.OrderBy(request => request.CreatedAt)
            .ThenBy(request => request.RequestId)
            .ToList();
    }
}
