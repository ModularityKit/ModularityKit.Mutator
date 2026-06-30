using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;

namespace ModularityKit.Mutator.Tests.TestSupport.Mutations;

/// <summary>
/// Sample mutation used to exercise policy evaluation paths.
/// </summary>
internal sealed class PolicySampleMutation : MutationBase<PolicySampleState>
{
    public PolicySampleMutation()
        : base(
            CreateIntent(
                operationName: "UpdateSample",
                category: "Test",
                description: "Exercise policy evaluation"),
            MutationContext.System("Policy test") with { StateId = "sample-1" })
    {
    }

    public override MutationResult<PolicySampleState> Apply(PolicySampleState state)
        => Success(state with { Value = "updated" },
            StateChange.Modified("Value", state.Value, "updated"));
}
