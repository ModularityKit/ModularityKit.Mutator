using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using WorkflowApprovals.Contracts;
using WorkflowApprovals.State;

namespace WorkflowApprovals.Mutations;

/// <summary>
/// Mutation that rejects the entire workflow in an <see cref="ApprovalWorkflowState"/>.
/// </summary>
internal sealed class RejectWorkflowMutation(
    string rejector,
    MutationContext context
) : MutationBase<ApprovalWorkflowState>(
    CreateIntent(
        operationName: "RejectWorkflow",
        category: "Workflow",
        description: "Rejects the entire workflow",
        riskLevel: MutationRiskLevel.Critical),
    context)
{
    public string Rejector { get; } = rejector;

    public override ValidationResult Validate(ApprovalWorkflowState state)
    {
        var result = new ValidationResult();
        if (string.IsNullOrEmpty(Rejector))
            result.AddError("Reject", "Reject cannot be empty");
        return result;
    }

    public override MutationResult<ApprovalWorkflowState> Apply(ApprovalWorkflowState state)
    {
        var steps = state.Steps.Select(s => s with
        {
            Status = StepStatus.Rejected,
            RejectedBy = Rejector
        }).ToList();

        var newState = state with { Steps = steps };
        return Success(
            newState,
            StateChange.Modified("Workflow", null, "Rejected"),
            [
                SideEffect.Critical(
                    type: "WorkflowRejected",
                    description: "Workflow rejection requires manual follow-up",
                    data: new WorkflowRejectedSideEffectData
                    {
                        Rejector = Rejector,
                        StepCount = steps.Count,
                        State = "Rejected"
                    })
            ]);
    }
}
