using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

/// <summary>
/// Evaluates decision oriented query criteria against governed mutation requests.
/// </summary>
public static class MutationRequestDecisionQueryEvaluator
{
    public static bool Matches(
        MutationRequest request,
        MutationRequestDecision decision,
        MutationRequestDecisionQuery query)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(query);

        return MutationRequestQueryEvaluator.Matches(request, query.RequestQuery) &&
               MatchesCategory(decision, query) &&
               MatchesCode(decision, query) &&
               MatchesActorId(decision, query) &&
               MatchesActorName(decision, query) &&
               MatchesTimestamp(decision, query);
    }

    private static bool MatchesCategory(
        MutationRequestDecision decision,
        MutationRequestDecisionQuery query)
        => query.Categories.Count == 0 || query.Categories.Contains(decision.Type.Category);

    private static bool MatchesCode(
        MutationRequestDecision decision,
        MutationRequestDecisionQuery query)
        => query.Codes.Count == 0 || query.Codes.Contains(decision.Type.Code);

    private static bool MatchesActorId(
        MutationRequestDecision decision,
        MutationRequestDecisionQuery query)
        => query.ActorIds.Count == 0 ||
           (decision.Context.ActorId is not null && query.ActorIds.Contains(decision.Context.ActorId));

    private static bool MatchesActorName(
        MutationRequestDecision decision,
        MutationRequestDecisionQuery query)
        => query.ActorNames.Count == 0 ||
           (decision.Context.ActorName is not null && query.ActorNames.Contains(decision.Context.ActorName));

    private static bool MatchesTimestamp(
        MutationRequestDecision decision,
        MutationRequestDecisionQuery query)
    {
        if (query.From.HasValue && decision.Timestamp < query.From.Value)
            return false;

        if (query.To.HasValue && decision.Timestamp > query.To.Value)
            return false;

        return true;
    }
}
