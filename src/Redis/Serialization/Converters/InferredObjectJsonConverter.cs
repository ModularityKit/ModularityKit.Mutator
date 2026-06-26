using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModularityKit.Mutator.Governance.Redis.Serialization.Converters;

/// <summary>
/// Deserializes flexible metadata values into inferred CLR object graphs.
/// </summary>
internal sealed class InferredObjectJsonConverter : JsonConverter<object?>
{
    /// <summary>
    /// Reads a JSON value into an inferred CLR object graph.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The inferred CLR value.</returns>
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadValue(ref reader);

    /// <summary>
    /// Writes an inferred CLR value back to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The CLR value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    private static object? ReadValue(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var longValue))
                    return longValue;

                if (reader.TryGetDecimal(out var decimalValue))
                    return decimalValue;

                return reader.GetDouble();
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.StartArray:
            {
                var list = new List<object?>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return list;

                    list.Add(ReadValue(ref reader));
                }

                throw new JsonException("Unexpected end of JSON while reading array.");
            }
            case JsonTokenType.StartObject:
            {
                var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return dictionary;

                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException($"Unexpected token '{reader.TokenType}' while reading object.");

                    var propertyName = reader.GetString() ?? string.Empty;

                    if (!reader.Read())
                        throw new JsonException("Unexpected end of JSON after property name.");

                    dictionary[propertyName] = ReadValue(ref reader);
                }

                throw new JsonException("Unexpected end of JSON while reading object.");
            }
            default:
                throw new JsonException($"Unsupported JSON token '{reader.TokenType}' for inferred object conversion.");
        }
    }
}
