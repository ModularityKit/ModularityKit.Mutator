using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModularityKit.Mutator.Abstractions.Effects;

/// <summary>
/// Serializes side effects while preserving typed payload contracts when registered.
/// When a payload contract is unknown at read time, the converter falls back to inferred
/// dictionary and list materialization so side effect meaning is not lost.
/// </summary>
public sealed class SideEffectJsonConverter : JsonConverter<SideEffect>
{
    /// <inheritdoc />
    public override SideEffect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var contractType = TryReadString(root, nameof(SideEffect.DataContractType));
        var contractVersion = TryReadInt(root, nameof(SideEffect.DataContractVersion));

        return new SideEffect
        {
            Type = root.GetProperty(nameof(SideEffect.Type)).GetString() ?? string.Empty,
            Description = root.GetProperty(nameof(SideEffect.Description)).GetString() ?? string.Empty,
            Severity = root.GetProperty(nameof(SideEffect.Severity)).Deserialize<SideEffectSeverity>(options),
            Data = TryReadData(root, contractType, contractVersion, options),
            Timestamp = root.GetProperty(nameof(SideEffect.Timestamp)).GetDateTimeOffset(),
            RequiresAction = root.GetProperty(nameof(SideEffect.RequiresAction)).GetBoolean(),
            DataContractType = contractType,
            DataContractVersion = contractVersion
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, SideEffect value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(SideEffect.Type), value.Type);
        writer.WriteString(nameof(SideEffect.Description), value.Description);
        writer.WritePropertyName(nameof(SideEffect.Severity));
        JsonSerializer.Serialize(writer, value.Severity, options);
        writer.WritePropertyName(nameof(SideEffect.Data));
        if (value.Data is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, value.Data, value.Data.GetType(), options);
        }

        writer.WriteString(nameof(SideEffect.Timestamp), value.Timestamp);
        writer.WriteBoolean(nameof(SideEffect.RequiresAction), value.RequiresAction);

        if (value.DataContractType is null)
            writer.WriteNull(nameof(SideEffect.DataContractType));
        else
            writer.WriteString(nameof(SideEffect.DataContractType), value.DataContractType);

        if (value.DataContractVersion is null)
            writer.WriteNull(nameof(SideEffect.DataContractVersion));
        else
            writer.WriteNumber(nameof(SideEffect.DataContractVersion), value.DataContractVersion.Value);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the side effect payload, preferring a registered typed contract when available.
    /// </summary>
    /// <param name="root">The serialized side effect object.</param>
    /// <param name="contractType">The optional payload contract identifier.</param>
    /// <param name="contractVersion">The optional payload contract version.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The typed payload when registered; otherwise an inferred object graph.</returns>
    private static object? TryReadData(
        JsonElement root,
        string? contractType,
        int? contractVersion,
        JsonSerializerOptions options)
    {
        if (!root.TryGetProperty(nameof(SideEffect.Data), out var dataElement))
            return null;

        if (dataElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        if (contractType is not null &&
            contractVersion is not null &&
            SideEffectDataContractRegistry.TryResolve(contractType, contractVersion.Value, out var dataType))
        {
            return JsonSerializer.Deserialize(dataElement.GetRawText(), dataType!, options);
        }

        return ReadInferredValue(dataElement);
    }

    /// <summary>
    /// Reads an optional string property from the serialized side effect object.
    /// </summary>
    /// <param name="root">The serialized side effect object.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The string value when present; otherwise <see langword="null"/>.</returns>
    private static string? TryReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;

    /// <summary>
    /// Reads an optional integer property from the serialized side effect object.
    /// </summary>
    /// <param name="root">The serialized side effect object.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The integer value when present; otherwise <see langword="null"/>.</returns>
    private static int? TryReadInt(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetInt32()
            : null;

    /// <summary>
    /// Materializes JSON into a best-effort inferred CLR object graph.
    /// </summary>
    /// <param name="value">The JSON value to materialize.</param>
    /// <returns>A dictionary, list, scalar, or <see langword="null"/> depending on the JSON token.</returns>
    private static object? ReadInferredValue(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject()
                .ToDictionary(property => property.Name, property => ReadInferredValue(property.Value), StringComparer.Ordinal),
            JsonValueKind.Array => value.EnumerateArray().Select(ReadInferredValue).ToList(),
            JsonValueKind.String when value.TryGetDateTimeOffset(out var dateTimeOffset) => dateTimeOffset,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var int64) => int64,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.GetRawText()
        };
}
