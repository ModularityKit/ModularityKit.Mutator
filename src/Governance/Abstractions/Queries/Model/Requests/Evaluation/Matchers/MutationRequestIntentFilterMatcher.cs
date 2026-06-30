using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;

/// <summary>
/// Evaluates intent-oriented request filters.
/// </summary>
internal static class MutationRequestIntentFilterMatcher
{
    /// <summary>
    /// Determines whether a request matches the supplied intent filter.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="filter">The intent filter.</param>
    /// <returns><see langword="true"/> when the request matches; otherwise <see langword="false"/>.</returns>
    public static bool Matches(MutationRequest request, MutationRequestIntentFilter filter)
        => MatchesCategory(request, filter) &&
           MatchesTags(request, filter) &&
           MatchesMetadata(request, filter) &&
           MatchesBlastRadius(request, filter);

    private static bool MatchesCategory(MutationRequest request, MutationRequestIntentFilter filter)
        => filter.Categories.Count == 0 || filter.Categories.Contains(request.Intent.Category);

    private static bool MatchesTags(MutationRequest request, MutationRequestIntentFilter filter)
    {
        if (filter.Tags.Count == 0)
            return true;

        var requestTags = request.Intent.Tags;
        return filter.TagMatchMode == MutationRequestTagMatchMode.All
            ? filter.Tags.All(requestTags.Contains)
            : filter.Tags.Any(requestTags.Contains);
    }

    private static bool MatchesMetadata(MutationRequest request, MutationRequestIntentFilter filter)
        => filter.Metadata.Count == 0 || MutationRequestMetadataMatcher.Matches(request.Intent.Metadata, filter.Metadata);

    private static bool MatchesBlastRadius(MutationRequest request, MutationRequestIntentFilter filter)
    {
        if (!filter.MinimumBlastRadiusScope.HasValue && !filter.MaximumBlastRadiusScope.HasValue)
            return true;

        var scope = request.Intent.EstimatedBlastRadius?.Scope;
        if (scope is null)
            return false;

        if (filter.MinimumBlastRadiusScope.HasValue && scope.Value < filter.MinimumBlastRadiusScope.Value)
            return false;

        if (filter.MaximumBlastRadiusScope.HasValue && scope.Value > filter.MaximumBlastRadiusScope.Value)
            return false;

        return true;
    }
}
