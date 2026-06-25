using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Loading;

/// <summary>
/// Normalizes raw Redis set members into stable request-id lists.
/// </summary>
internal static class RedisMutationRequestIdentifierValueNormalizer
{
    /// <summary>
    /// Converts Redis values into distinct, non-empty request identifiers.
    /// </summary>
    /// <param name="values">The Redis values to normalize.</param>
    /// <returns>The normalized request identifiers.</returns>
    public static IReadOnlyList<string> Normalize(RedisValue[] values) => 
        values.Where(value => value.HasValue)
            .Select(value => value.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
