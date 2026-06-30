using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Runtime;
using ModularityKit.Mutator.Tests.TestSupport.Engine;
using ModularityKit.Mutator.Tests.TestSupport.Mutations;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime.Engine;

public sealed class MutationEngineConcurrencyGateTests
{
    [Fact]
    public async Task ExecuteAsync_serializes_mutations_that_target_the_same_state_id()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options =>
        {
            options.MaxConcurrentMutations = 4;
            options.EnableDetailedMetrics = false;
        });

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        using var gate = new BlockingMutationGate();
        var state = new OrderedState("initial");

        var first = new BlockingMutation(gate, "shared-state", "first");
        var second = new BlockingMutation(gate, "shared-state", "second");

        var firstTask = Task.Run(() => engine.ExecuteAsync(first, state));
        var secondTask = Task.Run(() => engine.ExecuteAsync(second, state));

        Assert.True(await gate.WaitForEntriesAsync(1, TimeSpan.FromSeconds(5)));
        Assert.Equal(1, gate.PeakConcurrency);

        gate.Release();

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, gate.PeakConcurrency);
    }

    [Fact]
    public async Task ExecuteAsync_honors_max_concurrent_mutations_for_different_states()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options =>
        {
            options.MaxConcurrentMutations = 2;
            options.EnableDetailedMetrics = false;
        });

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        using var gate = new BlockingMutationGate();
        var states = new[]
        {
            new OrderedState("one"),
            new OrderedState("two"),
            new OrderedState("three"),
            new OrderedState("four")
        };

        var tasks = new[]
        {
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-1", "one"), states[0])),
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-2", "two"), states[1])),
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-3", "three"), states[2])),
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-4", "four"), states[3]))
        };

        Assert.True(await gate.WaitForEntriesAsync(2, TimeSpan.FromSeconds(5)));
        Assert.Equal(2, gate.PeakConcurrency);

        gate.Release();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(2, gate.PeakConcurrency);
    }
}
