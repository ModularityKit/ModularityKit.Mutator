using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Runtime;

namespace ModularityKit.Mutator.Governance.Tests.TestSupport.Execution.Host;

/// <summary>
/// Creates governed execution fixtures for execution-oriented tests.
/// </summary>
internal static class GovernanceExecutionManagerTestSupport
{
    /// <summary>
    /// Builds the service provider and execution manager used by governed execution tests.
    /// </summary>
    public static async Task<(ServiceProvider Provider, IMutationEngine Engine, IMutationAuditor Auditor, IMutationHistoryStore HistoryStore, InMemoryMutationRequestStore RequestStore, MutationRequestVersionResolutionManager ResolutionManager, GovernanceExecutionManager ExecutionManager)> CreateAsync()
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Strict);

        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        var auditor = provider.GetRequiredService<IMutationAuditor>();
        var historyStore = provider.GetRequiredService<IMutationHistoryStore>();
        var requestStore = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(requestStore, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(requestStore, resolutionManager, engine);

        return (provider, engine, auditor, historyStore, requestStore, resolutionManager, executionManager);
    }
}
