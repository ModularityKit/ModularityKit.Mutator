using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Runtime;

namespace ModularityKit.Mutator.Tests.TestSupport.Engine.Host;

/// <summary>
/// Creates configured mutation engine instances for runtime engine tests.
/// </summary>
internal static class MutationEngineTestHost
{
    /// <summary>
    /// Builds mutation engine with optional test-specific runtime configuration.
    /// </summary>
    /// <param name="configure">Optional runtime configuration callback.</param>
    /// <returns>Configured mutation engine instance.</returns>
    public static IMutationEngine CreateEngine(Action<MutationEngineOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: configure);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMutationEngine>();
    }
}
