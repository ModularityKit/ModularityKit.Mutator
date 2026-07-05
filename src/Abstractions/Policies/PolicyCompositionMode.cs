namespace ModularityKit.Mutator.Abstractions.Policies;

/// <summary>
/// Defines how composed policy set combines child policy decisions.
/// </summary>
public enum PolicyCompositionMode
{
    /// <summary>
    /// All composed policies are evaluated and their outputs are merged.
    /// Any blocking decision blocks the composition.
    /// </summary>
    AllOf = 0,

    /// <summary>
    /// All composed policies are evaluated and at least one allowed branch must succeed.
    /// Outputs from the allowed branches are merged.
    /// </summary>
    AnyOf = 1,

    /// <summary>
    /// Policies are evaluated in priority order and the first decisive policy wins.
    /// </summary>
    Priority = 2
}
