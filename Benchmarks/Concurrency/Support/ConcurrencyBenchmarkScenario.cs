using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Runtime;

namespace ModularityKit.Mutator.Benchmarks.Concurrency.Support;

/// <summary>
/// Creates repeatable concurrency benchmark scenarios and engine instances.
/// </summary>
internal static class ConcurrencyBenchmarkScenario
{
    /// <summary>
    /// Builds a performance-oriented mutation engine for concurrency benchmark scenarios.
    /// </summary>
    /// <param name="maxConcurrentMutations">The engine-wide concurrency limit.</param>
    /// <param name="configureServices">Optional service registrations overriding default runtime services.</param>
    /// <returns>A configured mutation engine instance.</returns>
    public static IMutationEngine BuildEngine(
        int maxConcurrentMutations,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options =>
        {
            options.AlwaysValidate = MutationEngineOptions.Performance.AlwaysValidate;
            options.EnableDetailedMetrics = MutationEngineOptions.Performance.EnableDetailedMetrics;
            options.StopBatchOnFirstFailure = MutationEngineOptions.Performance.StopBatchOnFirstFailure;
            options.MaxConcurrentMutations = maxConcurrentMutations;
        });

        configureServices?.Invoke(services);

        return services
            .BuildServiceProvider()
            .GetRequiredService<IMutationEngine>();
    }

    /// <summary>
    /// Creates a minimal commit mutation bound to the supplied benchmark state identifier.
    /// </summary>
    /// <param name="stateId">The state identifier used by the runtime gate.</param>
    /// <param name="operationSuffix">The operation suffix used to build a stable correlation identifier.</param>
    /// <returns>A concurrency benchmark mutation configured for commit execution.</returns>
    public static IncrementConcurrencyMutation CreateCommitMutation(string stateId, string operationSuffix)
    {
        var context = MutationContext.System("benchmark-concurrency")
            with
            {
                StateId = stateId,
                Mode = MutationMode.Commit,
                CorrelationId = $"{stateId}:{operationSuffix}"
            };

        return new IncrementConcurrencyMutation(context);
    }

    /// <summary>
    /// Creates a blocking commit mutation bound to the supplied benchmark state identifier.
    /// </summary>
    /// <param name="gate">The shared gate coordinating blocked execution.</param>
    /// <param name="stateId">The state identifier used by the runtime gate.</param>
    /// <param name="operationSuffix">The operation suffix used to build a stable correlation identifier.</param>
    /// <returns>A blocking concurrency benchmark mutation configured for commit execution.</returns>
    public static BlockingGateMutation CreateBlockingMutation(
        BlockingMutationGate gate,
        string stateId,
        string operationSuffix)
    {
        var context = MutationContext.System("benchmark-concurrency")
            with
            {
                StateId = stateId,
                Mode = MutationMode.Commit,
                CorrelationId = $"{stateId}:{operationSuffix}"
            };

        return new BlockingGateMutation(context, gate);
    }
}
