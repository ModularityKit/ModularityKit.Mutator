using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation;

/// <summary>
/// Evaluates query criteria against governed mutation requests.
/// </summary>
public static class MutationRequestQueryEvaluator
{
    /// <summary>
    /// Determines whether a governed request matches the supplied query.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="query">The query criteria.</param>
    /// <returns><see langword="true"/> when the request matches; otherwise <see langword="false"/>.</returns>
    public static bool Matches(MutationRequest request, MutationRequestQuery query)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(query);

        return MutationRequestScopeFilterMatcher.Matches(request, query.Scope) &&
               MutationRequestActorFilterMatcher.Matches(request, query.Actor) &&
               MutationRequestIntentFilterMatcher.Matches(request, query.Intent) &&
               MatchesMetadata(request, query) &&
               MutationRequestLifecycleFilterMatcher.Matches(request, query.Lifecycle) &&
               MutationRequestTimeRangeFilterMatcher.Matches(request, query.TimeRange) &&
               MutationRequestSideEffectFilterMatcher.Matches(request, query.SideEffects);
    }

    /// <summary>
    /// Determines whether a request has approval-oriented activity.
    /// </summary>
    /// <param name="request">The request to inspect.</param>
    /// <returns><see langword="true"/> when approval requirements or approval decisions are present.</returns>
    public static bool HasApprovalActivity(MutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.ApprovalRequirements.Count > 0 ||
            request.Decisions.Any(decision => decision.Type.Category == MutationRequestDecisionCategory.Approval);
    }

    /// <summary>
    /// Gets the most recent approval activity timestamp for ordering purposes.
    /// </summary>
    /// <param name="request">The request to inspect.</param>
    /// <returns>The latest approval decision timestamp, or the request update time when no terminal approval decision exists.</returns>
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
        => query.Metadata.Values.Count == 0 ||
           MutationRequestMetadataMatcher.Matches(request.Metadata, query.Metadata.Values);
}
