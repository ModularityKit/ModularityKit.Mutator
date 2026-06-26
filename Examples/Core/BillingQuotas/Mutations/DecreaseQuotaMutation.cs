using BillingQuotas.State;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;

namespace BillingQuotas.Mutations;

/// <summary>
/// Mutation that decreases the quota for specific user by specified amount.
/// </summary>
internal sealed class DecreaseQuotaMutation(
    string userId,
    int amount,
    MutationContext context
) : MutationBase<QuotaState>(
    CreateIntent(
        operationName: "DecreaseQuota",
        category: "Billing",
        description: "Decrease user quota by given amount",
        riskLevel: MutationRiskLevel.High),
    context)
{
    public string UserId { get; } = userId;

    public int Amount { get; } = amount;

    public override ValidationResult Validate(QuotaState state)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(UserId))
            result.AddError("UserId", "UserId cannot be empty");

        if (Amount <= 0)
            result.AddError("Amount", "Amount must be positive");

        if (state.UserQuotas.GetValueOrDefault(UserId) < Amount)
            result.AddError("Amount", "Cannot decrease below zero");

        return result;
    }

    public override MutationResult<QuotaState> Apply(QuotaState state)
    {
        var quotas = state.UserQuotas.ToDictionary(kv => kv.Key, kv => kv.Value);
        quotas[UserId] = quotas.GetValueOrDefault(UserId) - Amount;

        var newState = state with { UserQuotas = quotas };

        return Success(
            newState,
            StateChange.Modified($"UserQuotas.{UserId}", null, quotas[UserId]));
    }
}
