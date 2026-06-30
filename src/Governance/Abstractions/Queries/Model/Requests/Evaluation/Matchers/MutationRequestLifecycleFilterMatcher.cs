using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;

/// <summary>
/// Evaluates lifecycle-oriented request filters.
/// </summary>
internal static class MutationRequestLifecycleFilterMatcher
{
    /// <summary>
    /// Determines whether a request matches the supplied lifecycle filter.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="filter">The lifecycle filter.</param>
    /// <returns><see langword="true"/> when the request matches; otherwise <see langword="false"/>.</returns>
    public static bool Matches(MutationRequest request, MutationRequestLifecycleFilter filter)
        => MatchesStatus(request, filter) &&
           MatchesPendingReason(request, filter) &&
           MatchesDecisionCategories(request, filter);

    private static bool MatchesStatus(MutationRequest request, MutationRequestLifecycleFilter filter)
        => filter.Statuses.Count == 0 || filter.Statuses.Contains(request.Status);

    private static bool MatchesPendingReason(MutationRequest request, MutationRequestLifecycleFilter filter)
        => filter.PendingReasons.Count == 0 ||
           (request.PendingReason is not null && filter.PendingReasons.Contains(request.PendingReason.Value));

    private static bool MatchesDecisionCategories(MutationRequest request, MutationRequestLifecycleFilter filter)
        => filter.DecisionCategories.Count == 0 ||
           request.Decisions.Any(decision => filter.DecisionCategories.Contains(decision.Type.Category));
}
