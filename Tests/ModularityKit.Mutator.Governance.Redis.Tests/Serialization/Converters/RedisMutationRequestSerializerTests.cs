using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Serialization;
using Xunit;

namespace ModularityKit.Mutator.Governance.Redis.Tests.Serialization;

public sealed class RedisMutationRequestSerializerTests
{
    [Fact]
    public void Roundtrip_preserves_request_shape_needed_by_governance_runtime()
    {
        var request = MutationRequestFactory.PendingApproval(
            stateId: "tenant-42:roles",
            stateType: "IamRoleState",
            mutationType: "GrantRoleMutation",
            intent: new MutationIntent
            {
                OperationName = "GrantRole",
                Category = "Security",
                Description = "Grant elevated access",
                Tags = new HashSet<string> { "security", "urgent" },
                EstimatedBlastRadius = BlastRadius.Module,
                Metadata = new Dictionary<string, object>
                {
                    ["risk-owner"] = "platform"
                }
            },
            context: MutationContext.User("requester-1", "Requester One", "Need emergency access") with
            {
                StateId = "tenant-42:roles",
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "tests"
                }
            },
            requirements:
            [
                new PolicyRequirement
                {
                    Type = "Approval",
                    Description = "Requires security approval",
                    Data = new Dictionary<string, object>
                    {
                        ["Approver"] = "security-lead",
                        ["Reason"] = "Elevated role",
                        ["StepOrder"] = 1L,
                        ["RequiredApprovals"] = 1L
                    }
                }
            ],
            expectedStateVersion: "v10",
            metadata: new Dictionary<string, object>
            {
                ["team"] = "security",
                ["priority"] = "high"
            })
        with
        {
            Lifecycle = new MutationRequestLifecycleDetails
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Approval,
                CreatedAt = new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 6, 25, 9, 5, 0, TimeSpan.Zero)
            },
            SideEffects =
            [
                SideEffect.Critical(
                    type: "WorkflowRejected",
                    description: "Workflow rejection requires action",
                    data: new RedisGovernanceSideEffectData
                    {
                        Ticket = "INC-42"
                    })
            ]
        };

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

    [SideEffectDataContract("redis.governance.side-effect", 1)]
    private sealed record RedisGovernanceSideEffectData
    {
        public required string Ticket { get; init; }
    }
}
