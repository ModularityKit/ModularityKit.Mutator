using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime;

public sealed class MutationBaseTests
{
    [Fact]
    public void MutationBase_provides_successful_validation_by_default()
    {
        var mutation = new SampleMutation();

        Assert.True(mutation.Validate(new SampleState("initial")).IsValid);
    }

    [Fact]
    public void MutationBase_defaults_simulate_to_apply()
    {
        var mutation = new SampleMutation();
        var state = new SampleState("initial");

        var simulated = mutation.Simulate(state);

        Assert.True(simulated.IsSuccess);
        Assert.Equal("applied", simulated.NewState!.Value);
        Assert.Equal("Sample", mutation.Intent.OperationName);
        Assert.Equal("tester", mutation.Context.ActorId);
    }

    [Fact]
    public void MutationResult_success_can_be_created_from_single_change()
    {
        var result = MutationResult<SampleState>.Success(
            new SampleState("next"),
            StateChange.Modified("Value", "initial", "next"));

        Assert.True(result.IsSuccess);
        Assert.Equal("next", result.NewState!.Value);
        Assert.Single(result.Changes.Changes);
        Assert.Equal("Value", result.Changes.Changes[0].Path);
    }

    [Fact]
    public void MutationBase_provides_success_helper_for_single_change()
    {
        var mutation = new SampleMutation();

        var result = mutation.Apply(new SampleState("initial"));

        Assert.True(result.IsSuccess);
        Assert.Equal("applied", result.NewState!.Value);
        Assert.Single(result.Changes.Changes);
    }

    [Fact]
    public async Task ExecuteBatchAsync_supports_params_overload()
    {
        var services = new ServiceCollection();
        services.AddMutators();

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        var state = new SampleState("initial");

        var result = await engine.ExecuteBatchAsync(
            state,
            new SampleMutation("first"),
            new SampleMutation("second"));

        Assert.True(result.IsSuccess);
        Assert.Equal("second", result.FinalState!.Value);
        Assert.Equal(2, result.Results.Count);
    }

    private sealed record SampleState(string Value);

    private sealed class SampleMutation : MutationBase<SampleState>
    {
        public SampleMutation(string? nextValue = null)
            : base(
                CreateIntent(
                    operationName: "Sample",
                    category: "Test",
                    description: "Sample mutation"),
                MutationContext.User("tester", "Tester", "Sample run"))
        {
            NextValue = nextValue ?? "applied";
        }

        private string NextValue { get; }

        public override MutationResult<SampleState> Apply(SampleState state)
            => Success(
                state with { Value = NextValue },
                StateChange.Modified("Value", state.Value, NextValue));
    }
}
