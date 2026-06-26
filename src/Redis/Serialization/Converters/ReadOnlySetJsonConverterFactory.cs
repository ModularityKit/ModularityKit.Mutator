using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModularityKit.Mutator.Governance.Redis.Serialization.Converters;

/// <summary>
/// Creates converters for <see cref="IReadOnlySet{T}" /> payload members.
/// </summary>
internal sealed class ReadOnlySetJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the supplied type is a supported read-only set type.
    /// </summary>
    /// <param name="typeToConvert">The type to inspect.</param>
    /// <returns><see langword="true" /> when the type is supported; otherwise <see langword="false" />.</returns>
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType &&
           typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);

    /// <summary>
    /// Creates a converter instance for the supplied read-only set type.
    /// </summary>
    /// <param name="typeToConvert">The set type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The created JSON converter.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ReadOnlySetJsonConverter<>).MakeGenericType(itemType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    /// <summary>
    /// Converts a concrete read-only set payload for a specific item type.
    /// </summary>
    /// <typeparam name="T">The item type contained in the set.</typeparam>
    private sealed class ReadOnlySetJsonConverter<T> : JsonConverter<IReadOnlySet<T>>
    {
        /// <summary>
        /// Reads a JSON array into a read-only set.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The target type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The materialized read-only set.</returns>
        public override IReadOnlySet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var values = JsonSerializer.Deserialize<HashSet<T>>(ref reader, options);
            return values ?? new HashSet<T>();
        }

        /// <summary>
        /// Writes a read-only set as a JSON array.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The set value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value.ToArray(), options);
    }
}
