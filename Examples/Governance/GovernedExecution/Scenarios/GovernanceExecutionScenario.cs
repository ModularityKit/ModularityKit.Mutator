using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;
using ModularityKit.Mutator.Runtime;

namespace GovernedExecution.Scenarios;

internal static class GovernanceExecutionScenario
{
    public static async Task Run()
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Strict);
        await using var provider = services.BuildServiceProvider();

        var engine = provider.GetRequiredService<IMutationEngine>();
        var store = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(store, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(store, resolutionManager, engine);

        var request = await store.Create(MutationRequestFactory.Approved(
            stateId: "tenant-42:feature-flags",
            stateType: "FeatureFlagState",
            mutationType: nameof(EnableFeatureMutation),
            intent: new MutationIntent
            {
                OperationName = "EnableFeature",
                Category = "Configuration",
                Description = "Enable a rollout after governance approval"
            },
            context: MutationContext.User("requester-1", "Requester One", "Enable guarded rollout"),
            expectedStateVersion: "v10"));

        var currentState = new FeatureFlagState(
            request.StateId,
            IsEnabled: false,
            Version: "v10");

        var execution = await executionManager.ExecuteApproved(
            request.RequestId,
            new EnableFeatureMutation(MutationContext.Service("release-orchestrator", "Execute approved rollout"), "v11"),
            currentState,
            currentState.Version,
            resultingStateVersionProvider: state => state.Version,
            governanceContext: MutationContext.Service("governance-runtime", "Execute approved governance request"),
            strategy: VersionedRequestResolutionStrategy.RejectStale);

        Console.WriteLine("=== Governed Execution ===");
        Console.WriteLine($"Executed: {execution.WasExecuted}");
        Console.WriteLine($"Resolution: {execution.Resolution.Outcome}");
        Console.WriteLine($"Request status: {execution.Request.Status}");
        Console.WriteLine($"Resulting version: {execution.ResultingStateVersion ?? "-"}");
        Console.WriteLine($"Last decision: {execution.Request.Decisions[^1].Type}");
        Console.WriteLine($"Reason: {execution.Request.Decisions[^1].Reason}");
    }

    private sealed record FeatureFlagState(string StateId, bool IsEnabled, string Version);

    private sealed class EnableFeatureMutation(MutationContext context, string nextVersion) : IMutation<FeatureFlagState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "EnableFeature",
            Category = "Configuration",
            Description = "Enable a feature after governance approval"
        };

        public MutationContext Context { get; } = context;

        public MutationResult<FeatureFlagState> Apply(FeatureFlagState state)
        {
            var newState = state with
            {
                IsEnabled = true,
                Version = nextVersion
            };

            return MutationResult<FeatureFlagState>.Success(
                newState,
                ChangeSet.Single(StateChange.Modified("IsEnabled", state.IsEnabled, newState.IsEnabled)));
        }

        public ValidationResult Validate(FeatureFlagState state)
        {
            return state.IsEnabled
                ? ValidationResult.WithError("IsEnabled", "Feature is already enabled.")
                : ValidationResult.Success();
        }

        public MutationResult<FeatureFlagState> Simulate(FeatureFlagState state) => Apply(state);
    }
}
