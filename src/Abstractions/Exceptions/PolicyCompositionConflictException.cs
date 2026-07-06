namespace ModularityKit.Mutator.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when composed policies attempt to produce incompatible outputs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PolicyCompositionConflictException"/> class.
/// </remarks>
/// <param name="compositionName">Name of the composed policy set.</param>
/// <param name="conflictKey">Key or field that conflicted.</param>
/// <param name="policyNames">Policy names that contributed to the conflict.</param>
public sealed class PolicyCompositionConflictException(
    string compositionName,
    string conflictKey,
    IReadOnlyList<string> policyNames) : MutationException(
        $"Policy composition '{compositionName}' has a conflict for '{conflictKey}' between policies {string.Join(", ", policyNames)}.")
{
    /// <summary>
    /// Name of the composed policy set that detected the conflict.
    /// </summary>
    public string CompositionName { get; } = compositionName;

    /// <summary>
    /// Key or field that conflicted during composition.
    /// </summary>
    public string ConflictKey { get; } = conflictKey;

    /// <summary>
    /// Policy names that contributed to the conflicting value.
    /// </summary>
    public IReadOnlyList<string> PolicyNames { get; } = policyNames;
}
