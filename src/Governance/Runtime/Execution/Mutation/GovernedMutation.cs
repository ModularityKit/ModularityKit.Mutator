using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Mutation;

/// <summary>
/// Wraps mutation so governance request identifiers flow into core execution audit and history metadata.
/// </summary>
internal sealed class GovernedMutation<TState> : IMutation<TState>
{
    private const string GovernanceRequestIdMetadataKey = "GovernanceRequestId";
    private const string GovernanceRequestMetadataKey = "GovernanceRequestMetadata";
    private const string GovernanceIntentMetadataKey = "GovernanceIntentMetadata";
    private const string GovernanceEstimatedBlastRadiusMetadataKey = "GovernanceEstimatedBlastRadius";
    private const string GovernanceExecutionKindMetadataKey = "GovernanceExecutionKind";
    private const string GovernanceCompensationMetadataKey = "GovernanceCompensation";

    private readonly IMutation<TState> _inner;

    public GovernedMutation(IMutation<TState> inner, MutationRequest request)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        ArgumentNullException.ThrowIfNull(request);

        Intent = new MutationIntent
        {
            OperationName = _inner.Intent.OperationName,
            Category = _inner.Intent.Category,
            Description = _inner.Intent.Description,
            RiskLevel = _inner.Intent.RiskLevel,
            IsReversible = _inner.Intent.IsReversible,
            EstimatedBlastRadius = request.Intent.EstimatedBlastRadius ?? _inner.Intent.EstimatedBlastRadius,
            Tags = MergeTags(request.Intent.Tags, _inner.Intent.Tags),
            CreatedAt = _inner.Intent.CreatedAt,
            Metadata = MergeIntentMetadata(request)
        };

        Context = _inner.Context with
        {
            StateId = string.IsNullOrWhiteSpace(_inner.Context.StateId) ? request.StateId : _inner.Context.StateId,
            CorrelationId = string.IsNullOrWhiteSpace(_inner.Context.CorrelationId) ? request.RequestId : _inner.Context.CorrelationId,
            Metadata = MergeContextMetadata(request)
        };
    }

    public MutationIntent Intent { get; }

    public MutationContext Context { get; }

    public MutationResult<TState> Apply(TState state) => _inner.Apply(state);

    public ValidationResult Validate(TState state) => _inner.Validate(state);

    public MutationResult<TState> Simulate(TState state) => _inner.Simulate(state);

    private IReadOnlyDictionary<string, object> MergeContextMetadata(MutationRequest request)
    {
        var metadata = new Dictionary<string, object>(_inner.Context.Metadata)
        {
            [GovernanceRequestIdMetadataKey] = request.RequestId,
            [GovernanceExecutionKindMetadataKey] = request.Execution.Kind.ToString()
        };

        if (request.Metadata.Count > 0)
            metadata[GovernanceRequestMetadataKey] = request.Metadata;

        if (request.Intent.Metadata.Count > 0)
            metadata[GovernanceIntentMetadataKey] = request.Intent.Metadata;

        if (request.Intent.EstimatedBlastRadius is not null)
            metadata[GovernanceEstimatedBlastRadiusMetadataKey] = request.Intent.EstimatedBlastRadius;

        if (request.Execution.Compensation is not null)
            metadata[GovernanceCompensationMetadataKey] = CreateCompensationMetadata(request.Execution.Compensation);

        return metadata;
    }

    private IReadOnlyDictionary<string, object> MergeIntentMetadata(MutationRequest request)
    {
        var metadata = new Dictionary<string, object>(request.Intent.Metadata)
        {
            [GovernanceRequestIdMetadataKey] = request.RequestId,
            [GovernanceExecutionKindMetadataKey] = request.Execution.Kind.ToString()
        };

        if (_inner.Intent.Metadata.Count > 0)
            metadata["ExecutionIntentMetadata"] = _inner.Intent.Metadata;

        if (request.Execution.Compensation is not null)
            metadata[GovernanceCompensationMetadataKey] = CreateCompensationMetadata(request.Execution.Compensation);

        return metadata;
    }

    private static IReadOnlyDictionary<string, object> CreateCompensationMetadata(
        GovernedCompensationPlan compensation)
    {
        var metadata = new Dictionary<string, object>
        {
            ["OriginalRequestId"] = compensation.OriginalRequestId,
            ["Kind"] = compensation.Kind.ToString(),
            ["Trigger"] = compensation.Trigger.ToString()
        };

        if (!string.IsNullOrWhiteSpace(compensation.BatchId))
            metadata["BatchId"] = compensation.BatchId;

        if (compensation.RelatedRequestIds.Count > 0)
            metadata["RelatedRequestIds"] = compensation.RelatedRequestIds;

        if (!string.IsNullOrWhiteSpace(compensation.Reason))
            metadata["Reason"] = compensation.Reason;

        return metadata;
    }

    private static IReadOnlySet<string> MergeTags(
        IReadOnlySet<string> requestTags,
        IReadOnlySet<string> executionTags)
        => new HashSet<string>(requestTags.Concat(executionTags), StringComparer.Ordinal);
}
