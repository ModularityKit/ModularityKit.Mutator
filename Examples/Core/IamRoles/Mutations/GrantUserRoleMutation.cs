using IamRoles.State;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace IamRoles.Mutations;

/// <summary>
/// Mutation that grants role to user in current <see cref="UserPermissionsState"/>.
/// </summary>
internal sealed class GrantUserRoleMutation(
    string userId,
    string role,
    MutationContext context
) : MutationBase<UserPermissionsState>(
    CreateIntent(
        operationName: "GrantUserRole",
        category: "Security",
        description: "Grants role to a user",
        riskLevel: MutationRiskLevel.Critical),
    context)
{
    public string UserId { get; } = userId;

    public string Role { get; } = role;

    public override ValidationResult Validate(UserPermissionsState state)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(UserId))
            result.AddError("UserId", "UserId cannot be empty");

        if (string.IsNullOrWhiteSpace(Role))
            result.AddError("Role", "Role cannot be empty");

        if (state.RolesByUser.TryGetValue(UserId, out var roles) &&
            roles.Contains(Role))
            result.AddError("Role", "User already has this role");

        return result;
    }

    public override MutationResult<UserPermissionsState> Apply(UserPermissionsState state)
    {
        var rolesByUser = state.RolesByUser
            .ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value));

        if (!rolesByUser.TryGetValue(UserId, out var roles))
        {
            roles = [];
            rolesByUser[UserId] = roles;
        }

        roles.Add(Role);

        var newState = state with { RolesByUser = rolesByUser };

        return Success(
            newState,
            StateChange.Added($"RolesByUser.{UserId}", Role));
    }
}
