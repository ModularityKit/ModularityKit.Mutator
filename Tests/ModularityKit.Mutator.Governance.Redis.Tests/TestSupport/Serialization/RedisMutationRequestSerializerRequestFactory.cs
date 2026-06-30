using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Serialization.Models;

namespace ModularityKit.Mutator.Governance.Redis.Tests.TestSupport.Serialization;

/// <summary>
/// Creates the governed request fixture used by serializer tests.
/// </summary>
internal static class RedisMutationRequestSerializerRequestFactory
{
    /// <summary>
    /// Creates a request that exercises the Redis serializer roundtrip path.
    /// </summary>
    public static MutationRequest CreateRoundtripRequest()
    {
        return MutationRequestFactory.PendingApproval(
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
    }
}
