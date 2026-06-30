using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Requests;

public sealed class MutationRequestFactoryTests
{
    [Fact]
    public void Approved_generic_overload_infers_state_and_mutation_type_names()
    {
        var request = MutationRequestFactory.Approved<TestState, TestMutation>(
            stateId: "tenant-42:test",
            intent: new MutationIntent
            {
                OperationName = "Test",
                Category = "Test"
            },
            context: MutationContext.User("requester", "Requester", "Generic helper"),
            expectedStateVersion: "v1");

        Assert.Equal(nameof(TestState), request.StateType);
        Assert.Equal(nameof(TestMutation), request.MutationType);
        Assert.Equal(MutationRequestStatus.Approved, request.Status);
        Assert.Equal("v1", request.Versioning.ExpectedStateVersion);
    }

    [Fact]
    public void PendingApproval_generic_overload_infers_state_and_mutation_type_names()
    {
        var request = MutationRequestFactory.PendingApproval<TestState, TestMutation>(
            stateId: "tenant-42:test",
            intent: new MutationIntent
            {
                OperationName = "Test",
                Category = "Test"
            },
            context: MutationContext.User("requester", "Requester", "Generic helper"),
            requirements:
            [
                PolicyRequirement.Approval("alice", "Approval required")
            ],
            expectedStateVersion: "v1");

        Assert.Equal(nameof(TestState), request.StateType);
        Assert.Equal(nameof(TestMutation), request.MutationType);
        Assert.Equal(MutationRequestStatus.Pending, request.Status);
        Assert.Equal(PendingMutationReason.Approval, request.PendingReason);
        Assert.Single(request.ApprovalRequirements);
    }

    private sealed record TestState;

    private sealed class TestMutation : IMutation<TestState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "Test",
            Category = "Test"
        };

        public MutationContext Context { get; } = MutationContext.System("Test");

        public MutationResult<TestState> Apply(TestState state) => throw new NotImplementedException();

        public ValidationResult Validate(TestState state) => ValidationResult.Success();

        public MutationResult<TestState> Simulate(TestState state) => Apply(state);
    }
}
