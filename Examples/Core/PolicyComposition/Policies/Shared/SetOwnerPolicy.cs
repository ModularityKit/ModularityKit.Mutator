using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Policies;
using PolicyComposition.State;

namespace PolicyComposition.Policies.Shared;

/// <summary>
/// Forces the release owner to fixed value, so conflict handling is easy to observe.
/// </summary>
/// <remarks>
/// The policy only touches one field. That makes it useful for the conflict
/// example because two instances of the same class can be composed and produce a
/// deterministic clash when they disagree on the owner.
/// </remarks>
internal sealed class SetOwnerPolicy(string owner) : IMutationPolicy<ReleaseGateState>
{
    /// <summary>
    /// Policy identifier that includes the target owner value.
    /// </summary>
    public string Name => $"SetOwner[{owner}]";

    /// <summary>
    /// Middle priority because the policy is only used as a composed leaf rule.
    /// </summary>
    public int Priority => 150;

    /// <summary>
    /// Describes the owner assignment performed by the policy.
    /// </summary>
    public string Description => "Sets the release owner.";

    /// <summary>
    /// Updates only the owner field and leaves the rest of the state unchanged.
    /// </summary>
    /// <param name="mutation">The mutation being evaluated.</param>
    /// <param name="state">The current release state.</param>
    /// <returns>An allowed decision that changes only the owner field.</returns>
    public PolicyDecision Evaluate(IMutation<ReleaseGateState> mutation, ReleaseGateState state)
        => new()
        {
            IsAllowed = true,
            PolicyName = Name,
            Modifications = new Dictionary<string, object>
            {
                ["State"] = state with { Owner = owner }
            }
        };
}
