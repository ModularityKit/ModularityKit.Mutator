namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;

/// <summary>
/// Evaluates exact metadata matches between governed requests and query filters.
/// </summary>
internal static class MutationRequestMetadataMatcher
{
    /// <summary>
    /// Determines whether request metadata contains all queried key/value pairs.
    /// </summary>
    /// <param name="requestMetadata">The request or intent metadata to inspect.</param>
    /// <param name="queryMetadata">The queried key/value pairs.</param>
    /// <returns><see langword="true"/> when all queried entries match exactly; otherwise <see langword="false"/>.</returns>
    public static bool Matches(
        IReadOnlyDictionary<string, object> requestMetadata,
        IReadOnlyDictionary<string, object?> queryMetadata)
    {
        foreach (var pair in queryMetadata)
        {
            if (!requestMetadata.TryGetValue(pair.Key, out var value))
                return false;

            if (!Equals(value, pair.Value))
                return false;
        }

        return true;
    }
}
