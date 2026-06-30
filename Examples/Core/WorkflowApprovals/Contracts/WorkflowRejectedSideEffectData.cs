using ModularityKit.Mutator.Abstractions.Effects;

namespace WorkflowApprovals.Contracts;

[SideEffectDataContract("workflow.rejected", 1)]
internal sealed record WorkflowRejectedSideEffectData
{
    public required string Rejector { get; init; }

    public required int StepCount { get; init; }

    public required string State { get; init; }
}
