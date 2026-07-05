using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Results;
using PolicyComposition.State;

namespace PolicyComposition.Mutations;

/// <summary>
/// Submits a release and carries the governance metadata consumed by the policies.
/// </summary>
/// <remarks>
/// The mutation itself only moves the release into the submitted stage. The
/// interesting governance behavior lives in the composed policies, which read the
/// approval count, emergency flag, and target environment from the mutation
/// context metadata.
/// </remarks>
internal sealed class SubmitReleaseMutation(
    string releaseName,
    int approvals,
    bool emergency,
    string environment) : MutationBase<ReleaseGateState>(
        CreateIntent(
            operationName: "SubmitRelease",
            category: "ReleaseGovernance",
            description: "Submit a release through composed governance policies"),
        MutationContext.User("release-manager", "Release Manager", "Release composition example")
        with
        {
            StateId = releaseName,
            Metadata = new Dictionary<string, object>
            {
                ["approvals"] = approvals,
                ["emergency"] = emergency,
                ["environment"] = environment
            }
        })
{
    /// <summary>
    /// Marks the release as submitted before policy composition evaluates it.
    /// </summary>
    /// <param name="state">The current release state.</param>
    /// <returns>A mutation result that moves the release into the submitted stage.</returns>
    public override MutationResult<ReleaseGateState> Apply(ReleaseGateState state)
        => Success(
            state with { Stage = "Submitted" },
            StateChange.Modified("Stage", state.Stage, "Submitted"));
}
