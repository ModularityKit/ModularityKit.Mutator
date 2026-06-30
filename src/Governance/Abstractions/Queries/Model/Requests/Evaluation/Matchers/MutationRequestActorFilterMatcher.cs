using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;

/// <summary>
/// Evaluates actor-oriented request filters.
/// </summary>
internal static class MutationRequestActorFilterMatcher
{
    /// <summary>
    /// Determines whether a request matches the supplied actor filter.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="filter">The actor filter.</param>
    /// <returns><see langword="true"/> when the request matches; otherwise <see langword="false"/>.</returns>
    public static bool Matches(MutationRequest request, MutationRequestActorFilter filter)
        => MatchesActorId(request, filter) &&
           MatchesActorName(request, filter);

    private static bool MatchesActorId(MutationRequest request, MutationRequestActorFilter filter)
        => filter.ActorIds.Count == 0 ||
           (request.Context.ActorId is not null && filter.ActorIds.Contains(request.Context.ActorId));

    private static bool MatchesActorName(MutationRequest request, MutationRequestActorFilter filter)
        => filter.ActorNames.Count == 0 ||
           (request.Context.ActorName is not null && filter.ActorNames.Contains(request.Context.ActorName));
}
