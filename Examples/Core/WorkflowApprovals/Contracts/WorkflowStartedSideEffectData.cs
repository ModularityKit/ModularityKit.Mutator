using ModularityKit.Mutator.Abstractions.Effects;

namespace WorkflowApprovals.Contracts;

[SideEffectDataContract("workflow.started", 1)]
internal sealed record WorkflowStartedSideEffectData
{
    public required string Initiator { get; init; }

    public required int StepCount { get; init; }

    public required string WorkflowId { get; init; }
}
