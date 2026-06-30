using ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Filters;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model.Requests.Evaluation.Matchers;

/// <summary>
/// Evaluates persisted side effect filters on governed requests.
/// </summary>
internal static class MutationRequestSideEffectFilterMatcher
{
    /// <summary>
    /// Determines whether a request matches the supplied side effect filter.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="filter">The side effect filter.</param>
    /// <returns><see langword="true"/> when the request matches; otherwise <see langword="false"/>.</returns>
    public static bool Matches(MutationRequest request, MutationRequestSideEffectFilter filter)
    {
        if (filter.Types.Count == 0 &&
            filter.DataContractTypes.Count == 0 &&
            filter.Severities.Count == 0 &&
            !filter.RequiresAction.HasValue)
        {
            return true;
        }

        return request.SideEffects.Any(effect =>
            (filter.Types.Count == 0 || filter.Types.Contains(effect.Type)) &&
            (filter.DataContractTypes.Count == 0 ||
             (effect.DataContractType is not null && filter.DataContractTypes.Contains(effect.DataContractType))) &&
            (filter.Severities.Count == 0 || filter.Severities.Contains(effect.Severity)) &&
            (!filter.RequiresAction.HasValue || effect.RequiresAction == filter.RequiresAction.Value));
    }
}
