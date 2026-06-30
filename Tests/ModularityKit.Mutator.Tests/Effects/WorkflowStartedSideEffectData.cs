using ModularityKit.Mutator.Abstractions.Effects;

namespace ModularityKit.Mutator.Tests.Effects;

[SideEffectDataContract("workflow.started", 1)]
public sealed class WorkflowStartedSideEffectData
{
    public required string Initiator { get; init; }

    public required int StepCount { get; init; }

    public required string WorkflowId { get; init; }
}
