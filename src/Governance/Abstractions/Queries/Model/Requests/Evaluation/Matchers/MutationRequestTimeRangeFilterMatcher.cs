using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;

/// <summary>
/// Evaluates creation and update time filters on governed requests.
/// </summary>
internal static class MutationRequestTimeRangeFilterMatcher
{
    /// <summary>
    /// Determines whether a request matches the supplied time range filter.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="filter">The time range filter.</param>
    /// <returns><see langword="true"/> when the request matches; otherwise <see langword="false"/>.</returns>
    public static bool Matches(MutationRequest request, MutationRequestTimeRangeFilter filter)
        => MatchesCreatedAt(request, filter) &&
           MatchesUpdatedAt(request, filter);

    private static bool MatchesCreatedAt(MutationRequest request, MutationRequestTimeRangeFilter filter)
    {
        if (filter.CreatedFrom.HasValue && request.CreatedAt < filter.CreatedFrom.Value)
            return false;

        if (filter.CreatedTo.HasValue && request.CreatedAt > filter.CreatedTo.Value)
            return false;

        return true;
    }

    private static bool MatchesUpdatedAt(MutationRequest request, MutationRequestTimeRangeFilter filter)
    {
        if (filter.UpdatedFrom.HasValue && request.UpdatedAt < filter.UpdatedFrom.Value)
            return false;

        if (filter.UpdatedTo.HasValue && request.UpdatedAt > filter.UpdatedTo.Value)
            return false;

        return true;
    }
}
