using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModularityKit.Mutator.Governance.Abstractions.Queries.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Storage;
using ModularityKit.Mutator.Governance.Redis.Configuration;
using ModularityKit.Mutator.Governance.Redis.Keys;
using ModularityKit.Mutator.Governance.Redis.Storage;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Execution;
using ModularityKit.Mutator.Governance.Redis.Storage.Candidates.Planning;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents.Keys;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents.Payloads;
using ModularityKit.Mutator.Governance.Redis.Storage.Documents.Reading;
using ModularityKit.Mutator.Governance.Redis.Storage.Identifiers;
using ModularityKit.Mutator.Governance.Redis.Storage.Identifiers.Loading;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Reading;
using ModularityKit.Mutator.Governance.Redis.Storage.Persistence.Writing;
using ModularityKit.Mutator.Governance.Redis.Storage.Queries;
using ModularityKit.Mutator.Governance.Redis.Storage.Queries.Reading;
using StackExchange.Redis;

namespace ModularityKit.Mutator.Governance.Redis;

/// <summary>
/// Dependency injection registration for the Redis governance provider.
/// </summary>
public static class RedisGovernanceServiceCollectionExtensions
{
    /// <summary>
    /// Registers Redis-backed governance request storage and query services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer used by the provider.</param>
    /// <param name="configure">An optional callback for configuring provider options.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddRedisGovernanceStore(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        Action<RedisMutationRequestStoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        var options = new RedisMutationRequestStoreOptions();
        configure?.Invoke(options);

        services.AddSingleton(connectionMultiplexer);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
        services.TryAddSingleton<RedisMutationRequestPersistence>();
        services.TryAddSingleton<RedisMutationRequestQueryReader>();
        services.TryAddSingleton(sp =>
        {
            var resolvedOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisMutationRequestStoreOptions>>();
            return new RedisMutationRequestKeyspace(resolvedOptions.Value);
        });
        services.TryAddSingleton(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        services.TryAddSingleton<RedisMutationRequestPersistenceRecordFactory>();
        services.TryAddSingleton<RedisMutationRequestPersistenceDocumentReader>();
        services.TryAddSingleton<RedisMutationRequestIndexWriter>();
        services.TryAddSingleton<RedisMutationRequestTransactionWriter>();
        services.TryAddSingleton<RedisMutationRequestIdentifierSetLoader>();
        services.TryAddSingleton<RedisMutationRequestIdSetReader>();
        services.TryAddSingleton<RedisMutationRequestDocumentKeyFactory>();
        services.TryAddSingleton<RedisMutationRequestPayloadReader>();
        services.TryAddSingleton<RedisMutationRequestDocumentReader>();
        services.TryAddSingleton<RedisMutationRequestCandidatePlanBuilder>();
        services.TryAddSingleton<RedisMutationRequestCandidateExecutor>();
        services.TryAddSingleton<RedisMutationRequestQueryCandidateSelector>();
        services.TryAddSingleton<RedisMutationRequestQueryDocumentLoader>();
        services.TryAddSingleton(sp => new RedisMutationRequestStore(
            sp.GetRequiredService<RedisMutationRequestPersistence>(),
            sp.GetRequiredService<RedisMutationRequestQueryReader>()));
        services.TryAddSingleton<IMutationRequestStore>(sp => sp.GetRequiredService<RedisMutationRequestStore>());
        services.TryAddSingleton<IMutationRequestQueryStore>(sp => sp.GetRequiredService<RedisMutationRequestStore>());

        return services;
    }
}
