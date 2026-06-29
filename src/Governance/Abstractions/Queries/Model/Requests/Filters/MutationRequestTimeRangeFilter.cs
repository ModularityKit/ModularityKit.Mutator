namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;

/// <summary>
/// Groups time window request query filters.
/// </summary>
public sealed record MutationRequestTimeRangeFilter
{
    /// <summary>
    /// Inclusive lower bound for request creation time.
    /// </summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>
    /// Inclusive upper bound for request creation time.
    /// </summary>
    public DateTimeOffset? CreatedTo { get; init; }

    /// <summary>
    /// Inclusive lower bound for request update time.
    /// </summary>
    public DateTimeOffset? UpdatedFrom { get; init; }

    /// <summary>
    /// Inclusive upper bound for request update time.
    /// </summary>
    public DateTimeOffset? UpdatedTo { get; init; }
}
