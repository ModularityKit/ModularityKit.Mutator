namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Groups request level governance metadata filters.
/// </summary>
public sealed record MutationRequestMetadataFilter
{
    /// <summary>
    /// Exact metadata key/value pairs to match against request level governance metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Values { get; init; } = new Dictionary<string, object?>();
}
