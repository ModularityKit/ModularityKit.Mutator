using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Abstractions.Engine;

/// <summary>
/// Base class for mutation implementations that want sensible defaults for common behavior.
/// </summary>
/// <remarks>
/// <para>
/// Inheriting from <see cref="MutationBase{TState}"/> removes repeated boilerplate for:
/// </para>
/// <list type="bullet">
/// <item><description>storing <see cref="Intent"/> and <see cref="Context"/></description></item>
/// <item><description>defaulting <see cref="Validate(TState)"/> to success</description></item>
/// <item><description>defaulting <see cref="Simulate(TState)"/> to <see cref="Apply(TState)"/></description></item>
/// <item><description>building common <see cref="MutationIntent"/> instances</description></item>
/// </list>
/// <para>
/// The base class is optional. Mutations can still implement <see cref="IMutation{TState}"/> directly
/// when they need a different shape.
/// </para>
/// </remarks>
/// <typeparam name="TState">The type of state the mutation operates on.</typeparam>
public abstract class MutationBase<TState> : IMutation<TState>
{
    /// <summary>
    /// Initializes a new mutation base with the provided intent and context.
    /// </summary>
    /// <param name="intent">The mutation intent.</param>
    /// <param name="context">The execution context.</param>
    protected MutationBase(MutationIntent intent, MutationContext context)
    {
        Intent = intent ?? throw new ArgumentNullException(nameof(intent));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public MutationIntent Intent { get; }

    /// <inheritdoc />
    public MutationContext Context { get; }

    /// <inheritdoc />
    public abstract MutationResult<TState> Apply(TState state);

    /// <inheritdoc />
    public virtual ValidationResult Validate(TState state) => ValidationResult.Success();

    /// <inheritdoc />
    public virtual MutationResult<TState> Simulate(TState state) => Apply(state);

    /// <summary>
    /// Creates a successful mutation result from a single state change.
    /// </summary>
    /// <param name="newState">The new state after the mutation.</param>
    /// <param name="change">The single change applied.</param>
    /// <param name="sideEffects">Optional list of side effects.</param>
    /// <returns>A <see cref="MutationResult{TState}"/> representing success.</returns>
    protected static MutationResult<TState> Success(
        TState newState,
        StateChange change,
        IReadOnlyList<SideEffect>? sideEffects = null)
        => MutationResult<TState>.Success(newState, change, sideEffects);

    /// <summary>
    /// Creates a common <see cref="MutationIntent"/> instance.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="category">The mutation category.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="riskLevel">The mutation risk level.</param>
    /// <param name="isReversible">Whether the mutation can be reversed.</param>
    /// <param name="estimatedBlastRadius">Optional blast radius estimate.</param>
    /// <param name="tags">Optional classification tags.</param>
    /// <param name="metadata">Optional metadata attached to the intent.</param>
    /// <returns>A configured <see cref="MutationIntent"/>.</returns>
    protected static MutationIntent CreateIntent(
        string operationName,
        string category,
        string? description = null,
        MutationRiskLevel riskLevel = MutationRiskLevel.Low,
        bool isReversible = true,
        BlastRadius? estimatedBlastRadius = null,
        IReadOnlySet<string>? tags = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        => new()
        {
            OperationName = operationName,
            Category = category,
            Description = description,
            RiskLevel = riskLevel,
            IsReversible = isReversible,
            EstimatedBlastRadius = estimatedBlastRadius,
            Tags = tags ?? new HashSet<string>(),
            Metadata = metadata ?? new Dictionary<string, object>()
        };
}
