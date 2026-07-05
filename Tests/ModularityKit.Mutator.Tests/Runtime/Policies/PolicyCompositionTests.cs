using ModularityKit.Mutator.Abstractions.Effects;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;
using ModularityKit.Mutator.Tests.TestSupport.Mutations;
using ModularityKit.Mutator.Tests.TestSupport.Policies.Composition;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime.Policies;

public sealed class PolicyCompositionTests
{
    [Fact]
    public async Task AllOf_merges_modifications_side_effects_and_metadata()
    {
        var composed = PolicyComposition.AllOf(
            "FeatureFlagGovernance",
            [
                new StateAndSideEffectPolicy(),
                new SideEffectsAndMetadataPolicy()
            ],
            priority: 500,
            description: "Composed governance rules for feature flag changes.");

        var decision = await composed.EvaluateAsync(new PolicySampleMutation(), new PolicySampleState("initial"));

        Assert.True(decision.IsAllowed);
        Assert.Equal("governed", GetState(decision).Value);
        Assert.Equal(2, GetSideEffects(decision).Count());
        Assert.Equal("FeatureFlagGovernance", decision.PolicyName);
        Assert.Equal("AllOf", decision.Metadata!["PolicyComposition.Mode"]);
        Assert.Equal("state-policy", decision.Metadata!["owner"]);
        Assert.Equal("state-policy", decision.Metadata!["source"]);
    }

    [Fact]
    public async Task AllOf_merges_requirements_and_metadata()
    {
        var composed = PolicyComposition.AllOf(
            "ApprovalGate",
            [
                new ApprovalPolicy(),
                new MetadataPolicy()
            ],
            priority: 100);

        var decision = await composed.EvaluateAsync(
            new PolicySampleMutation(),
            new PolicySampleState("initial")
        );

        Assert.False(decision.IsAllowed);
        Assert.Single(decision.Requirements!);
        Assert.Equal("Approval", decision.Requirements![0].Type);
        Assert.Equal(PolicyDecisionSeverity.Error, decision.Severity);
        Assert.Equal("ApprovalGate", decision.PolicyName);
        Assert.Equal("AllOf", decision.Metadata!["PolicyComposition.Mode"]);
        Assert.Equal("compliance", decision.Metadata!["team"]);
        Assert.Equal("platform", decision.Metadata!["owner"]);
        Assert.Equal(["ApprovalPolicy", "MetadataPolicy"], (string[])decision.Metadata["PolicyComposition.BlockingPolicies"]);
    }

    [Fact]
    public async Task AnyOf_uses_only_allowed_branches_when_one_branch_succeeds()
    {
        var composed = PolicyComposition.AnyOf(
            "AlternativeAllow",
            [
                new BlockingPolicy(),
                new AllowedStatePolicy()
            ],
            priority: 100);

        var decision = await composed.EvaluateAsync(new PolicySampleMutation(), new PolicySampleState("initial"));

        Assert.True(decision.IsAllowed);
        Assert.NotNull(decision.Modifications);
        Assert.Equal("AlternativeAllow", decision.PolicyName);
        Assert.Equal("AnyOf", decision.Metadata!["PolicyComposition.Mode"]);
        Assert.Equal(["AllowedStatePolicy"], (string[])decision.Metadata["PolicyComposition.WinningPolicies"]);
        Assert.Equal(["BlockingPolicy"], (string[])decision.Metadata["PolicyComposition.BlockingPolicies"]);
        Assert.Equal("allowed", GetState(decision).Value);
    }

    [Fact]
    public async Task Priority_composition_returns_first_decisive_higher_priority_policy()
    {
        var composed = PolicyComposition.Priority(
            "PriorityGate",
            [
                new LowPriorityStatePolicy(),
                new HighPriorityStatePolicy()
            ],
            priority: 100);

        var decision = await composed.EvaluateAsync(new PolicySampleMutation(), new PolicySampleState("initial"));

        Assert.True(decision.IsAllowed);
        Assert.Equal("PriorityGate", decision.PolicyName);
        Assert.Equal("high", GetState(decision).Value);
        Assert.Single(GetSideEffects(decision));
        Assert.Equal("high", decision.Metadata!["selectedPolicy"]);
    }

    [Fact]
    public async Task AllOf_detects_conflicting_mutation_result_modifications()
    {
        var composed = PolicyComposition.AllOf(
            "ConflictingGate",
            [
                new StatePolicy("First", "one"),
                new StatePolicy("Second", "two")
            ],
            priority: 100);

        var exception = await Assert.ThrowsAsync<PolicyCompositionConflictException>(() =>
            composed.EvaluateAsync(new PolicySampleMutation(), new PolicySampleState("initial")));

        Assert.Equal("ConflictingGate", exception.CompositionName);
        Assert.Equal("State", exception.ConflictKey);
        Assert.Equal(["First", "Second"], exception.PolicyNames);
    }

    private static PolicySampleState GetState(PolicyDecision decision)
        => (PolicySampleState)decision.Modifications!["State"];

    private static IEnumerable<SideEffect> GetSideEffects(PolicyDecision decision)
        => (IEnumerable<SideEffect>)decision.Modifications!["SideEffects"];

}
