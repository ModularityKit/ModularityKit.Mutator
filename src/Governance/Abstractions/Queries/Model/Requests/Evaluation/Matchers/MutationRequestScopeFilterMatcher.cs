using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;

/// <summary>
/// Evaluates identifier and type-oriented request filters.
/// </summary>
internal static class MutationRequestScopeFilterMatcher
{
    /// <summary>
    /// Determines whether a request matches the supplied scope filter.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="filter">The scope filter.</param>
    /// <returns><see langword="true"/> when the request matches; otherwise <see langword="false"/>.</returns>
    public static bool Matches(MutationRequest request, MutationRequestScopeFilter filter)
        => MatchesRequestId(request, filter) &&
           MatchesStateId(request, filter) &&
           MatchesStateType(request, filter) &&
           MatchesMutationType(request, filter);

    private static bool MatchesRequestId(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.RequestIds.Count == 0 || filter.RequestIds.Contains(request.RequestId);

    private static bool MatchesStateId(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.StateIds.Count == 0 || filter.StateIds.Contains(request.StateId);

    private static bool MatchesStateType(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.StateTypes.Count == 0 || filter.StateTypes.Contains(request.StateType);

    private static bool MatchesMutationType(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.MutationTypes.Count == 0 || filter.MutationTypes.Contains(request.MutationType);
}
