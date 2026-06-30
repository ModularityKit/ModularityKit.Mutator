using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Effects;
using ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Model;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Mutations;

/// <summary>
/// Promotes a role to Admin for execution tests.
/// </summary>
internal sealed class PromoteRoleMutation(MutationContext context, string nextVersion) : IMutation<RoleState>
{
    /// <summary>
    /// Gets the intent associated with the promotion mutation.
    /// </summary>
    public MutationIntent Intent { get; } = new()
    {
        OperationName = "PromoteRole",
        Category = "Security",
        Description = "Promote tenant role after governance approval"
    };

    /// <summary>
    /// Gets the invocation context for the mutation.
    /// </summary>
    public MutationContext Context { get; } = context;

    /// <inheritdoc />
    public MutationResult<RoleState> Apply(RoleState state)
    {
        var newState = state with
        {
            Role = "Admin",
            Version = nextVersion
        };

        return MutationResult<RoleState>.Success(
            newState,
            ChangeSet.Single(StateChange.Modified("Role", state.Role, newState.Role)),
            [
                SideEffect.Create(
                    type: "RoleElevated",
                    description: "Governed execution elevated the role",
                    data: new GovernanceExecutionSideEffectData
                    {
                        RequestStateId = state.StateId,
                        NewRole = newState.Role
                    })
            ]);
    }

    /// <inheritdoc />
    public ValidationResult Validate(RoleState state)
    {
        return state.Role == "Admin"
            ? ValidationResult.WithError("Role", "Role is already Admin.")
            : ValidationResult.Success();
    }

    /// <inheritdoc />
    public MutationResult<RoleState> Simulate(RoleState state) => Apply(state);
}
