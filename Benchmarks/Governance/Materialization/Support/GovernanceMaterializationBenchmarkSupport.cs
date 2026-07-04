using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Benchmarks.Engine;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Materialization.Support;

/// <summary>
/// Builds repeatable governance materialization benchmark fixtures.
/// </summary>
internal static class GovernanceMaterializationBenchmarkSupport
{
    public const string StateId = "governance-benchmark:materialization";

    /// <summary>
    /// Creates a fresh execution result fixture for a single scenario run.
    /// </summary>
    public static GovernanceMaterializationBenchmarkFixture CreateFixture()
    {
        var engine = MutationEngineBenchmarkSupport.BuildEngine(MutationEngineOptions.Strict);
        var store = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(store, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(store, resolutionManager, engine);
        var request = store.Create(CreateApprovedRequest<GovernanceMaterializationState, MaterializeGovernanceOutputMutation>("governance-materialization-request"))
            .GetAwaiter()
            .GetResult();
        var mutation = new MaterializeGovernanceOutputMutation(
            MutationContext.User("operator", "Operator", "Execute governance materialization benchmark"),
            nextVersion: "v11");

        var result = executionManager.ExecuteApproved(
                request.RequestId,
                mutation,
                new GovernanceMaterializationState(StateId, 10, "v10"),
                governanceContext: mutation.Context,
                strategy: VersionedRequestResolutionStrategy.RejectStale)
            .GetAwaiter()
            .GetResult();

        return new GovernanceMaterializationBenchmarkFixture(result, mutation);
    }

    private static MutationRequest CreateApprovedRequest<TState, TMutation>(string requestId)
        where TMutation : IMutation<TState>
        => MutationRequestFactory.Approved<TState, TMutation>(
            stateId: StateId,
            intent: GovernanceMaterializationScenarioFactory.CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need governance output materialization"),
            expectedStateVersion: "v10")
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Shared execution result fixture for a governance materialization scenario.
    /// </summary>
    internal sealed record GovernanceMaterializationBenchmarkFixture(
        GovernedExecutionResult<GovernanceMaterializationState> Result,
        MaterializeGovernanceOutputMutation Mutation);
}
