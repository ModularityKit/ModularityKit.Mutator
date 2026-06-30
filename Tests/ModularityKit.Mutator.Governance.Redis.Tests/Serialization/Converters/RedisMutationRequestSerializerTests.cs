using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Serialization;
using ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Serialization;
using ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Serialization.Models;
using Xunit;

namespace ModularityKit.Mutator.Governance.Redis.Tests.Serialization;

public sealed class RedisMutationRequestSerializerTests
{
    [Fact]
    public void Roundtrip_preserves_request_shape_needed_by_governance_runtime()
    {
        var request = RedisMutationRequestSerializerRequestFactory.CreateRoundtripRequest();

        var json = RedisMutationRequestSerializer.Serialize(request);
        var roundtrip = RedisMutationRequestSerializer.Deserialize(json);

        Assert.Equal(request.RequestId, roundtrip.RequestId);
        Assert.Equal(request.Status, roundtrip.Status);
        Assert.Equal(request.PendingReason, roundtrip.PendingReason);
        Assert.Equal(request.Intent.Category, roundtrip.Intent.Category);
        Assert.Contains("security", roundtrip.Intent.Tags);
        Assert.Equal(BlastRadiusScope.Module, roundtrip.Intent.EstimatedBlastRadius?.Scope);
        Assert.Equal("platform", roundtrip.Intent.Metadata["risk-owner"]);
        Assert.Equal("security", roundtrip.Metadata["team"]);
        Assert.Single(roundtrip.SideEffects);
        Assert.Equal("WorkflowRejected", roundtrip.SideEffects[0].Type);
        Assert.Equal("redis.governance.side-effect", roundtrip.SideEffects[0].DataContractType);
        Assert.True(roundtrip.SideEffects[0].TryGetData<RedisGovernanceSideEffectData>(out var sideEffectData));
        Assert.Equal("INC-42", sideEffectData!.Ticket);
        Assert.Single(roundtrip.Requirements);
        Assert.Single(roundtrip.ApprovalRequirements);
        Assert.Equal("security-lead", roundtrip.ApprovalRequirements[0].ApproverId);
        Assert.Equal(3, roundtrip.Decisions.Count);
        Assert.Equal(request.Lifecycle.CreatedAt, roundtrip.Lifecycle.CreatedAt);
        Assert.Equal(request.Lifecycle.UpdatedAt, roundtrip.Lifecycle.UpdatedAt);
    }
}
