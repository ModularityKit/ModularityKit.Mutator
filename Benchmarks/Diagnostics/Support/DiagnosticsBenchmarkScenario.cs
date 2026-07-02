using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Runtime;

namespace ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

/// <summary>
/// Creates repeatable diagnostics benchmark scenarios and engine instances.
/// </summary>
internal static class DiagnosticsBenchmarkScenario
{
    /// <summary>
    /// Gets the shared state identifier used by diagnostics benchmark cases.
    /// </summary>
    public const string StateId = "diagnostics-benchmark-state";

    /// <summary>
    /// Builds performance oriented mutation engine for diagnostics benchmark scenarios.
    /// </summary>
    /// <param name="configureServices">Optional service registrations overriding default runtime services.</param>
    /// <param name="configureEngine">Optional engine configuration executed after engine resolution.</param>
    /// <returns>A configured mutation engine instance.</returns>
    public static IMutationEngine BuildEngine(
        Action<IServiceCollection>? configureServices = null,
        Action<IMutationEngine>? configureEngine = null)
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Performance);

        configureServices?.Invoke(services);

        var engine = services
            .BuildServiceProvider()
            .GetRequiredService<IMutationEngine>();

        configureEngine?.Invoke(engine);
        return engine;
    }

    /// <summary>
    /// Creates a minimal commit mutation instance bound to the shared diagnostics benchmark state.
    /// </summary>
    /// <param name="operationSuffix">The operation suffix used to build a stable correlation identifier.</param>
    /// <returns>A diagnostics mutation configured for commit execution.</returns>
    public static DiagnosticsMutation CreateCommitMutation(string operationSuffix)
    {
        var context = MutationContext.System("diagnostics-benchmark") with
        {
            StateId = StateId,
            Mode = MutationMode.Commit,
            CorrelationId = $"{StateId}:{operationSuffix}"
        };

        return new DiagnosticsMutation(context);
    }
}
