using System.Text.Json;
using ModularityKit.Mutator.Abstractions.Effects;
using Xunit;

namespace ModularityKit.Mutator.Tests.Effects;

public sealed partial class SideEffectTypedDataTests
{
    [Fact]
    public void Create_with_typed_payload_populates_contract_metadata()
    {
        var effect = SideEffect.Create(
            type: "WorkflowStarted",
            description: "Workflow started",
            data: new WorkflowStartedSideEffectData
            {
                Initiator = "alice",
                StepCount = 2,
                WorkflowId = "wf-42"
            });

        Assert.Equal("workflow.started", effect.DataContractType);
        Assert.Equal(1, effect.DataContractVersion);
        Assert.True(effect.TryGetData<WorkflowStartedSideEffectData>(out var data));
        Assert.Equal("wf-42", data!.WorkflowId);
    }

    [Fact]
    public void Json_roundtrip_rehydrates_registered_typed_payload()
    {
        SideEffectDataContractRegistry.Register<WorkflowStartedSideEffectData>();

        var effect = SideEffect.Create(
            type: "WorkflowStarted",
            description: "Workflow started",
            data: new WorkflowStartedSideEffectData
            {
                Initiator = "alice",
                StepCount = 2,
                WorkflowId = "wf-42"
            });

        var roundtrip = JsonSerializer.Deserialize<SideEffect>(JsonSerializer.Serialize(effect));

        Assert.NotNull(roundtrip);
        Assert.Equal("workflow.started", roundtrip!.DataContractType);
        Assert.True(roundtrip.TryGetData<WorkflowStartedSideEffectData>(out var data));
        Assert.Equal("alice", data!.Initiator);
        Assert.Equal(2, data.StepCount);
    }

    [Fact]
    public void Json_roundtrip_without_registration_preserves_contract_and_payload_shape()
    {
        var effect = new SideEffect
        {
            Type = "WorkflowStarted",
            Description = "Workflow started",
            Data = new WorkflowStartedSideEffectData
            {
                Initiator = "alice",
                StepCount = 2,
                WorkflowId = "wf-42"
            },
            DataContractType = "workflow.started.unregistered",
            DataContractVersion = 1
        };

        var roundtrip = JsonSerializer.Deserialize<SideEffect>(JsonSerializer.Serialize(effect));

        Assert.NotNull(roundtrip);
        Assert.Equal("workflow.started.unregistered", roundtrip!.DataContractType);
        Assert.Equal(1, roundtrip.DataContractVersion);

        var payload = Assert.IsType<IReadOnlyDictionary<string, object?>>(roundtrip.Data, exactMatch: false);
        Assert.Equal("alice", payload["Initiator"]);
        Assert.Equal(2L, payload["StepCount"]);
        Assert.Equal("wf-42", payload["WorkflowId"]);
    }
}
