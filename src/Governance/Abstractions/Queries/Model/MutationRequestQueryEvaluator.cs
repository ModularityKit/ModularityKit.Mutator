using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

/// <summary>
/// Evaluates query criteria against governed mutation requests.
/// </summary>
public static class MutationRequestQueryEvaluator
{
    public static bool Matches(MutationRequest request, MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(query);

        return MatchesRequestId(request, query) &&
               MatchesStateId(request, query) &&
               MatchesStateType(request, query) &&
               MatchesMutationType(request, query) &&
               MatchesActorId(request, query) &&
               MatchesActorName(request, query) &&
               MatchesCategory(request, query) &&
               MatchesStatus(request, query) &&
               MatchesPendingReason(request, query) &&
               MatchesTags(request, query) &&
               MatchesMetadata(request, query) &&
               MatchesBlastRadius(request, query) &&
               MatchesCreatedAt(request, query) &&
               MatchesUpdatedAt(request, query) &&
               MatchesDecisionCategories(request, query);
    }

    public static bool HasApprovalActivity(MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.ApprovalRequirements.Count > 0 ||
            request.Decisions.Any(decision => decision.Type.Category == MutationRequestDecisionCategory.Approval);
    }

    public static DateTimeOffset GetRecentApprovalTimestamp(MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var approvalDecision = request.Decisions
            .Where(decision => decision.Type.Category == MutationRequestDecisionCategory.Approval &&
                decision.Type.Code != MutationRequestApprovalDecisionType.Requested.ToString())
            .OrderByDescending(decision => decision.Timestamp)
            .FirstOrDefault();

        return approvalDecision?.Timestamp ?? request.UpdatedAt;
    }

    private static bool MatchesMetadata(MutationRequest request, MutationRequestQuery query)
        => query.Metadata.Count == 0 || MatchesMetadata(request.Metadata, query.Metadata);

    private static bool MatchesRequestId(MutationRequest request, MutationRequestQuery query)
        => query.RequestIds.Count == 0 || query.RequestIds.Contains(request.RequestId);

    private static bool MatchesStateId(MutationRequest request, MutationRequestQuery query)
        => query.StateIds.Count == 0 || query.StateIds.Contains(request.StateId);

    private static bool MatchesStateType(MutationRequest request, MutationRequestQuery query)
        => query.StateTypes.Count == 0 || query.StateTypes.Contains(request.StateType);

    private static bool MatchesMutationType(MutationRequest request, MutationRequestQuery query)
        => query.MutationTypes.Count == 0 || query.MutationTypes.Contains(request.MutationType);

    private static bool MatchesActorId(MutationRequest request, MutationRequestQuery query)
        => query.ActorIds.Count == 0 ||
           (request.Context.ActorId is not null && query.ActorIds.Contains(request.Context.ActorId));

    private static bool MatchesActorName(MutationRequest request, MutationRequestQuery query)
        => query.ActorNames.Count == 0 ||
           (request.Context.ActorName is not null && query.ActorNames.Contains(request.Context.ActorName));

    private static bool MatchesCategory(MutationRequest request, MutationRequestQuery query)
        => query.Categories.Count == 0 || query.Categories.Contains(request.Intent.Category);

    private static bool MatchesStatus(MutationRequest request, MutationRequestQuery query)
        => query.Statuses.Count == 0 || query.Statuses.Contains(request.Status);

    private static bool MatchesPendingReason(MutationRequest request, MutationRequestQuery query)
        => query.PendingReasons.Count == 0 ||
           (request.PendingReason is not null && query.PendingReasons.Contains(request.PendingReason.Value));

    private static bool MatchesTags(MutationRequest request, MutationRequestQuery query)
    {
        if (query.Tags.Count == 0)
            return true;

        var requestTags = request.Intent.Tags;
        return query.TagMatchMode == MutationRequestTagMatchMode.All
            ? query.Tags.All(requestTags.Contains)
            : query.Tags.Any(requestTags.Contains);
    }

    private static bool MatchesBlastRadius(MutationRequest request, MutationRequestQuery query)
    {
        if (!query.MinimumBlastRadiusScope.HasValue && !query.MaximumBlastRadiusScope.HasValue)
            return true;

        var scope = request.Intent.EstimatedBlastRadius?.Scope;
        if (scope is null)
            return false;

        if (query.MinimumBlastRadiusScope.HasValue && scope.Value < query.MinimumBlastRadiusScope.Value)
            return false;

        if (query.MaximumBlastRadiusScope.HasValue && scope.Value > query.MaximumBlastRadiusScope.Value)
            return false;

        return true;
    }

    private static bool MatchesCreatedAt(MutationRequest request, MutationRequestQuery query)
    {
        if (query.CreatedFrom.HasValue && request.CreatedAt < query.CreatedFrom.Value)
            return false;

        if (query.CreatedTo.HasValue && request.CreatedAt > query.CreatedTo.Value)
            return false;

        return true;
    }

    private static bool MatchesUpdatedAt(MutationRequest request, MutationRequestQuery query)
    {
        if (query.UpdatedFrom.HasValue && request.UpdatedAt < query.UpdatedFrom.Value)
            return false;

        if (query.UpdatedTo.HasValue && request.UpdatedAt > query.UpdatedTo.Value)
            return false;

        return true;
    }

    private static bool MatchesDecisionCategories(MutationRequest request, MutationRequestQuery query)
        => query.DecisionCategories.Count == 0 ||
           request.Decisions.Any(decision => query.DecisionCategories.Contains(decision.Type.Category));

    private static bool MatchesMetadata(
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
