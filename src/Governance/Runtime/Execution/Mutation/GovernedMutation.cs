using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Mutation;

/// <summary>
/// Wraps a mutation so governance request identifiers flow into core execution audit and history metadata.
/// </summary>
internal sealed class GovernedMutation<TState> : IMutation<TState>
{
    private readonly IMutation<TState> _inner;

    public GovernedMutation(IMutation<TState> inner, string requestId, string stateId)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        Intent = new MutationIntent
        {
            OperationName = _inner.Intent.OperationName,
            Category = _inner.Intent.Category,
            Description = _inner.Intent.Description,
            RiskLevel = _inner.Intent.RiskLevel,
            IsReversible = _inner.Intent.IsReversible,
            EstimatedBlastRadius = _inner.Intent.EstimatedBlastRadius,
            Tags = _inner.Intent.Tags,
            CreatedAt = _inner.Intent.CreatedAt,
            Metadata = MergeMetadata(_inner.Intent.Metadata, requestId)
        };

        Context = _inner.Context with
        {
            StateId = string.IsNullOrWhiteSpace(_inner.Context.StateId) ? stateId : _inner.Context.StateId,
            CorrelationId = string.IsNullOrWhiteSpace(_inner.Context.CorrelationId) ? requestId : _inner.Context.CorrelationId,
            Metadata = MergeMetadata(_inner.Context.Metadata, requestId)
        };
    }

    public MutationIntent Intent { get; }

    public MutationContext Context { get; }

    public MutationResult<TState> Apply(TState state) => _inner.Apply(state);

    public ValidationResult Validate(TState state) => _inner.Validate(state);

    public MutationResult<TState> Simulate(TState state) => _inner.Simulate(state);

    private static IReadOnlyDictionary<string, object> MergeMetadata(
        IReadOnlyDictionary<string, object> source,
        string requestId)
    {
        var metadata = new Dictionary<string, object>(source)
        {
            ["GovernanceRequestId"] = requestId
        };

        return metadata;
    }
}
