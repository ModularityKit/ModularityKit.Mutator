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
/// Rolls a role back to Reader for compensation scenarios.
/// </summary>
internal sealed class RollbackRoleMutation(MutationContext context, string nextVersion) : IMutation<RoleState>
{
    /// <summary>
    /// Gets the intent associated with the rollback mutation.
    /// </summary>
    public MutationIntent Intent { get; } = new()
    {
        OperationName = "RollbackRole",
        Category = "Security",
        Description = "Rollback tenant role to Reader",
        IsReversible = false
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
            Role = "Reader",
            Version = nextVersion
        };

        return MutationResult<RoleState>.Success(
            newState,
            ChangeSet.Single(StateChange.Modified("Role", state.Role, newState.Role)),
            [
                SideEffect.Create(
                    type: "RoleRollback",
                    description: "Governed compensation restored the previous role",
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
        return state.Role == "Reader"
            ? ValidationResult.WithError("Role", "Role is already Reader.")
            : ValidationResult.Success();
    }

    /// <inheritdoc />
    public MutationResult<RoleState> Simulate(RoleState state) => Apply(state);
}
