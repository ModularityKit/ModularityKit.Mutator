using BillingQuotas.State;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace BillingQuotas.Mutations;

/// <summary>
/// Mutation that resets user's quota to zero.
/// </summary>
internal sealed class ResetQuotaMutation(
    string userId,
    MutationContext context
) : MutationBase<QuotaState>(
    CreateIntent(
        operationName: "ResetQuota",
        category: "Billing",
        description: "Reset user quota to zero",
        riskLevel: MutationRiskLevel.High),
    context)
{
    public string UserId { get; } = userId;

    public override ValidationResult Validate(QuotaState state)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(UserId))
            result.AddError("UserId", "UserId cannot be empty");

        return result;
    }

    public override MutationResult<QuotaState> Apply(QuotaState state)
    {
        var quotas = state.UserQuotas.ToDictionary(kv => kv.Key, kv => kv.Value);
        quotas[UserId] = 0;

        var newState = state with { UserQuotas = quotas };

        return Success(
            newState,
            StateChange.Modified($"UserQuotas.{UserId}", null, 0));
    }
}
