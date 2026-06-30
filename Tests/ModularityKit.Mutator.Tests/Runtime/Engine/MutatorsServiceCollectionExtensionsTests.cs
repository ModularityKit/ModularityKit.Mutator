using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Runtime;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime.Engine;

public sealed class MutatorsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMutators_rejects_non_positive_max_concurrent_mutations()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options => options.MaxConcurrentMutations = 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => services.BuildServiceProvider().GetRequiredService<IMutationEngine>());
    }
}
