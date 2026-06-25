using ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Models;
using ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Loading;

namespace ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Execution;

/// <summary>
/// Executes candidate id lookup plans against Redis set data.
/// </summary>
internal sealed class RedisMutationRequestCandidateExecutor(RedisMutationRequestIdSetReader idSetReader)
{
    private readonly RedisMutationRequestIdSetReader _idSetReader = idSetReader ?? throw new ArgumentNullException(nameof(idSetReader));

    /// <summary>
    /// Executes a candidate plan and returns the resulting request identifiers.
    /// </summary>
    /// <param name="plan">The candidate plan to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved request identifiers.</returns>
    public async Task<IReadOnlyList<string>> ExecuteAsync(RedisMutationRequestCandidatePlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return plan.Operation switch
        {
            RedisMutationRequestCandidateOperation.ExplicitIds => plan.ExplicitRequestIds ?? [],
            RedisMutationRequestCandidateOperation.SingleSet => await LoadSingleSetAsync(plan, cancellationToken).ConfigureAwait(false),
            RedisMutationRequestCandidateOperation.Union => await LoadUnionAsync(plan, cancellationToken).ConfigureAwait(false),
            RedisMutationRequestCandidateOperation.Intersection => await LoadIntersectionAsync(plan, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported candidate operation '{plan.Operation}'.")
        };
    }

    private Task<IReadOnlyList<string>> LoadSingleSetAsync(RedisMutationRequestCandidatePlan plan, CancellationToken cancellationToken) =>
        _idSetReader.LoadIdsAsync(plan.Keys[0], cancellationToken);

    private Task<IReadOnlyList<string>> LoadUnionAsync(RedisMutationRequestCandidatePlan plan, CancellationToken cancellationToken) => 
        _idSetReader.LoadUnionedIdsAsync(plan.Keys, cancellationToken);

    private Task<IReadOnlyList<string>> LoadIntersectionAsync(RedisMutationRequestCandidatePlan plan, CancellationToken cancellationToken) =>
        _idSetReader.LoadIntersectedIdsAsync(plan.Keys[0], plan.Keys[1], cancellationToken);
}
