using System.Text.Json;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Serialization.Converters;

namespace ModularityKit.Mutator.Governance.Redis.Serialization;

/// <summary>
/// Serializes governed mutation requests to and from Redis JSON payloads.
/// </summary>
internal static class RedisMutationRequestSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    /// <summary>
    /// Serializes a governed mutation request into a Redis JSON payload.
    /// </summary>
    /// <param name="request">The request to serialize.</param>
    /// <returns>The serialized JSON payload.</returns>
    public static string Serialize(MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return JsonSerializer.Serialize(request, SerializerOptions);
    }

    /// <summary>
    /// Deserializes a Redis JSON payload into a governed mutation request.
    /// </summary>
    /// <param name="json">The JSON payload to deserialize.</param>
    /// <returns>The deserialized mutation request.</returns>
    public static MutationRequest Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var request = JsonSerializer.Deserialize<MutationRequest>(json, SerializerOptions);
        if (request is null)
            throw new InvalidOperationException("Redis mutation request payload deserialized to null.");

        return request;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        options.Converters.Add(new InferredObjectJsonConverter());
        options.Converters.Add(new ReadOnlySetJsonConverterFactory());
        return options;
    }
}
