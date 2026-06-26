using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using WorkflowApprovals.State;

namespace WorkflowApprovals.Mutations;

/// <summary>
/// Mutation that approves specific step in an <see cref="ApprovalWorkflowState"/>.
/// </summary>
internal sealed class ApproveStepMutation(
    int stepIndex,
    string approver,
    MutationContext context
) : MutationBase<ApprovalWorkflowState>(
    CreateIntent(
        operationName: "ApproveStep",
        category: "Workflow",
        description: "Approve a workflow step",
        riskLevel: MutationRiskLevel.High),
    context)
{
    public int StepIndex { get; } = stepIndex;

    public string Approver { get; } = approver;

    public override ValidationResult Validate(ApprovalWorkflowState state)
    {
        var result = new ValidationResult();
        if (StepIndex < 0 || StepIndex >= state.Steps.Count)
            result.AddError("StepIndex", "Invalid step index");
        if (string.IsNullOrEmpty(Approver))
            result.AddError("Approver", "Approver cannot be empty");
        return result;
    }

    public override MutationResult<ApprovalWorkflowState> Apply(ApprovalWorkflowState state)
    {
        var steps = state.Steps.ToList();
        var oldStep = steps[StepIndex];
        var newStep = oldStep with
        {
            Status = StepStatus.Approved,
            ApprovedBy = Approver
        };
        steps[StepIndex] = newStep;

        var newState = state with { Steps = steps };

        return Success(
            newState,
            StateChange.Modified($"Steps[{StepIndex}]", oldStep.Status, newStep.Status));
    }
}
