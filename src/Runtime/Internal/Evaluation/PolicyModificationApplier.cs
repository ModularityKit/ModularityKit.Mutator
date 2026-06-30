using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Runtime.Internal.Evaluation;

/// <summary>
/// Applies policy-level state and side-effect modifications to a mutation result.
/// </summary>
internal static class PolicyModificationApplier
{
    /// <summary>
    /// Applies the given policy modifications to <paramref name="result" />, returning an updated result.
    /// </summary>
    /// <typeparam name="TState">The state type handled by the mutation.</typeparam>
    /// <param name="result">The original mutation result to apply modifications to.</param>
    /// <param name="modifications">
    /// A dictionary of modifications. Recognised keys are <c>"State"</c> (overrides the new state),
    /// <c>"SideEffect"</c> (appends a single <see cref="SideEffect" />), and
    /// <c>"SideEffects"</c> (appends a collection of <see cref="SideEffect" />).
    /// </param>
    /// <returns>
    /// The original <paramref name="result" /> unchanged when no applicable modifications exist or the result is not successful;
    /// otherwise a new result record with the modifications applied.
    /// </returns>
    public static MutationResult<TState> Apply<TState>(
        MutationResult<TState> result,
        IReadOnlyDictionary<string, object>? modifications)
    {
        if (modifications is null || modifications.Count == 0 || !result.IsSuccess)
            return result;

        var newState = result.NewState;
        var sideEffects = result.SideEffects.ToList();

        foreach (var modification in modifications)
        {
            switch (modification.Key)
            {
                case "State" when modification.Value is TState stateValue:
                    newState = stateValue;
                    break;
                case "SideEffect" when modification.Value is SideEffect effect:
                    sideEffects.Add(effect);
                    break;
                case "SideEffects" when modification.Value is IEnumerable<SideEffect> effects:
                    sideEffects.AddRange(effects);
                    break;
            }
        }

        return new MutationResult<TState>
        {
            IsSuccess = result.IsSuccess,
            NewState = newState,
            Changes = result.Changes,
            ValidationResult = result.ValidationResult,
            PolicyDecisions = result.PolicyDecisions,
            SideEffects = sideEffects,
            Metrics = result.Metrics,
            Exception = result.Exception,
            CompletedAt = result.CompletedAt
        };
    }
}
