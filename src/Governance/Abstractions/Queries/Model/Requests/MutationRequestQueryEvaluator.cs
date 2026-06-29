using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests;

/// <summary>
/// Evaluates query criteria against governed mutation requests.
/// </summary>
public static class MutationRequestQueryEvaluator
{
    public static bool Matches(MutationRequest request, MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(query);

        return MatchesScope(request, query.Scope) &&
               MatchesActor(request, query.Actor) &&
               MatchesIntent(request, query.Intent) &&
               MatchesMetadata(request, query.Metadata) &&
               MatchesLifecycle(request, query.Lifecycle) &&
               MatchesTimeRange(request, query.TimeRange);
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

    private static bool MatchesScope(MutationRequest request, MutationRequestScopeFilter filter)
        => MatchesRequestId(request, filter) &&
           MatchesStateId(request, filter) &&
           MatchesStateType(request, filter) &&
           MatchesMutationType(request, filter);

    private static bool MatchesActor(MutationRequest request, MutationRequestActorFilter filter)
        => MatchesActorId(request, filter) &&
           MatchesActorName(request, filter);

    private static bool MatchesIntent(MutationRequest request, MutationRequestIntentFilter filter)
        => MatchesCategory(request, filter) &&
           MatchesTags(request, filter) &&
           MatchesIntentMetadata(request, filter) &&
           MatchesBlastRadius(request, filter);

    private static bool MatchesMetadata(MutationRequest request, MutationRequestMetadataFilter filter)
        => filter.Values.Count == 0 || MatchesMetadata(request.Metadata, filter.Values);

    private static bool MatchesLifecycle(MutationRequest request, MutationRequestLifecycleFilter filter)
        => MatchesStatus(request, filter) &&
           MatchesPendingReason(request, filter) &&
           MatchesDecisionCategories(request, filter);

    private static bool MatchesTimeRange(MutationRequest request, MutationRequestTimeRangeFilter filter)
        => MatchesCreatedAt(request, filter) &&
           MatchesUpdatedAt(request, filter);

    private static bool MatchesIntentMetadata(MutationRequest request, MutationRequestIntentFilter filter)
        => filter.Metadata.Count == 0 || MatchesMetadata(request.Intent.Metadata, filter.Metadata);

    private static bool MatchesRequestId(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.RequestIds.Count == 0 || filter.RequestIds.Contains(request.RequestId);

    private static bool MatchesStateId(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.StateIds.Count == 0 || filter.StateIds.Contains(request.StateId);

    private static bool MatchesStateType(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.StateTypes.Count == 0 || filter.StateTypes.Contains(request.StateType);

    private static bool MatchesMutationType(MutationRequest request, MutationRequestScopeFilter filter)
        => filter.MutationTypes.Count == 0 || filter.MutationTypes.Contains(request.MutationType);

    private static bool MatchesActorId(MutationRequest request, MutationRequestActorFilter filter)
        => filter.ActorIds.Count == 0 ||
           (request.Context.ActorId is not null && filter.ActorIds.Contains(request.Context.ActorId));

    private static bool MatchesActorName(MutationRequest request, MutationRequestActorFilter filter)
        => filter.ActorNames.Count == 0 ||
           (request.Context.ActorName is not null && filter.ActorNames.Contains(request.Context.ActorName));

    private static bool MatchesCategory(MutationRequest request, MutationRequestIntentFilter filter)
        => filter.Categories.Count == 0 || filter.Categories.Contains(request.Intent.Category);

    private static bool MatchesStatus(MutationRequest request, MutationRequestLifecycleFilter filter)
        => filter.Statuses.Count == 0 || filter.Statuses.Contains(request.Status);

    private static bool MatchesPendingReason(MutationRequest request, MutationRequestLifecycleFilter filter)
        => filter.PendingReasons.Count == 0 ||
           (request.PendingReason is not null && filter.PendingReasons.Contains(request.PendingReason.Value));

    private static bool MatchesTags(MutationRequest request, MutationRequestIntentFilter filter)
    {
        if (filter.Tags.Count == 0)
            return true;

        var requestTags = request.Intent.Tags;
        return filter.TagMatchMode == MutationRequestTagMatchMode.All
            ? filter.Tags.All(requestTags.Contains)
            : filter.Tags.Any(requestTags.Contains);
    }

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

    private static bool MatchesDecisionCategories(MutationRequest request, MutationRequestLifecycleFilter filter)
        => filter.DecisionCategories.Count == 0 ||
           request.Decisions.Any(decision => filter.DecisionCategories.Contains(decision.Type.Category));

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
