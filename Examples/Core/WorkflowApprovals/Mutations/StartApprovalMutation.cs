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
/// Mutation that starts new approval workflow in an <see cref="ApprovalWorkflowState"/>.
/// </summary>
internal sealed class StartApprovalMutation(
    string initiator,
    string[] stepNames,
    MutationContext context
) : MutationBase<ApprovalWorkflowState>(
    CreateIntent(
        operationName: "StartWorkflow",
        category: "Workflow",
        description: "Starts a new approval workflow",
        riskLevel: MutationRiskLevel.Medium),
    context)
{
    public string Initiator { get; } = initiator;

    public string[] StepNames { get; } = stepNames;

    public override ValidationResult Validate(ApprovalWorkflowState state)
    {
        var result = new ValidationResult();
        if (string.IsNullOrEmpty(Initiator))
            result.AddError("Initiator", "Initiator cannot be empty");
        if (StepNames.Length == 0)
            result.AddError("Steps", "Workflow must have at least one step");
        return result;
    }

    public override MutationResult<ApprovalWorkflowState> Apply(ApprovalWorkflowState state)
    {
        var steps = StepNames.Select(name => new WorkflowStep(name)).ToList();
        var newState = state with
        {
            WorkflowId = Guid.NewGuid().ToString(),
            Steps = steps,
            Initiator = Initiator
        };

        return Success(
            newState,
            StateChange.Added("Steps", steps),
            [
                SideEffect.Create(
                    type: "WorkflowStarted",
                    description: "Approval workflow started and ready for first review",
                    data: new WorkflowStartedSideEffectData
                    {
                        Initiator = Initiator,
                        StepCount = steps.Count,
                        WorkflowId = newState.WorkflowId
                    })
            ]);
    }
}
