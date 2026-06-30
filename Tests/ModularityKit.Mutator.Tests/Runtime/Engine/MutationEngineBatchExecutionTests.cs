using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Runtime;
using ModularityKit.Mutator.Tests.TestSupport.Engine;
using ModularityKit.Mutator.Tests.TestSupport.Mutations;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime.Engine;

public sealed class MutationEngineBatchExecutionTests
{
    [Fact]
    public async Task ExecuteBatchAsync_remains_ordered_while_respecting_runtime_concurrency_gates()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options =>
        {
            options.MaxConcurrentMutations = 2;
            options.EnableDetailedMetrics = false;
        });

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        var observed = new ConcurrentQueue<string>();

        var batch = new[]
        {
            new OrderedMutation("state-1", "first", observed),
            new OrderedMutation("state-2", "second", observed),
            new OrderedMutation("state-1", "third", observed)
        };

        var result = await engine.ExecuteBatchAsync(batch, new OrderedState("initial"));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Results.Count);
        Assert.Equal(new[] { "first", "second", "third" }, observed);
    }
}
