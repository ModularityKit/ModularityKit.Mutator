using IamRoles.State;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace IamRoles.Mutations;

/// <summary>
/// Mutation that revokes role from user in the current <see cref="UserPermissionsState"/>.
/// </summary>
internal sealed class RevokeUserRoleMutation(
    string userId,
    string role,
    MutationContext context
) : MutationBase<UserPermissionsState>(
    CreateIntent(
        operationName: "RevokeUserRole",
        category: "Security",
        description: "Revokes a role from a user",
        riskLevel: MutationRiskLevel.High),
    context)
{
    public string UserId { get; } = userId;

    public string Role { get; } = role;

    public override ValidationResult Validate(UserPermissionsState state)
    {
        var result = new ValidationResult();

        if (!state.RolesByUser.TryGetValue(UserId, out var roles) ||
            !roles.Contains(Role))
            result.AddError("Role", "User does not have this role");

        return result;
    }

    public override MutationResult<UserPermissionsState> Apply(UserPermissionsState state)
    {
        var rolesByUser = state.RolesByUser
            .ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value));

        rolesByUser[UserId].Remove(Role);

        var newState = state with { RolesByUser = rolesByUser };

        return Success(
            newState,
            StateChange.Removed($"RolesByUser.{UserId}", Role));
    }
}
