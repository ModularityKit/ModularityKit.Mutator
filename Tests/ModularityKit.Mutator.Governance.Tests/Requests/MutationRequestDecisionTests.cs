using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using Xunit;

namespace ModularityKit.Mutator.Governance.Tests.Requests;

public sealed class MutationRequestDecisionTests
{
    [Fact]
    public void Lifecycle_factory_wraps_lifecycle_type()
    {
        var decision = MutationRequestDecision.Lifecycle(
            MutationRequestLifecycleDecisionType.Approved,
            MutationContext.User("alice", "Alice", "Approved"),
            reason: "Approved by operator");

        Assert.Equal(MutationRequestDecisionCategory.Lifecycle, decision.Type.Category);
        Assert.Equal(nameof(MutationRequestLifecycleDecisionType.Approved), decision.Type.Code);
        Assert.Equal("Approved by operator", decision.Reason);
    }

    [Fact]
    public void Approval_factory_wraps_approval_type()
    {
        var decision = MutationRequestDecision.Approval(
            MutationRequestApprovalDecisionType.QuorumSatisfied,
            MutationContext.User("bob", "Bob", "Quorum satisfied"));

        Assert.Equal(MutationRequestDecisionCategory.Approval, decision.Type.Category);
        Assert.Equal(nameof(MutationRequestApprovalDecisionType.QuorumSatisfied), decision.Type.Code);
    }

    [Fact]
    public void VersionResolution_factory_wraps_version_resolution_type()
    {
        var decision = MutationRequestDecision.VersionResolution(
            MutationRequestVersionResolutionDecisionType.RejectedAsStale,
            MutationContext.User("carol", "Carol", "Stale"));

        Assert.Equal(MutationRequestDecisionCategory.VersionResolution, decision.Type.Category);
        Assert.Equal(nameof(MutationRequestVersionResolutionDecisionType.RejectedAsStale), decision.Type.Code);
    }
}
