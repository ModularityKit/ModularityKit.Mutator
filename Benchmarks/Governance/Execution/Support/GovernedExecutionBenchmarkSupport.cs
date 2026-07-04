using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Benchmarks.Engine;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Strategies;
using ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Execution.Support;

/// <summary>
/// Builds repeatable governed execution benchmark fixtures.
/// </summary>
internal static class GovernedExecutionBenchmarkSupport
{
    public const string StateId = "governance-benchmark:execution";

    /// <summary>
    /// Creates a fresh execution fixture for a single scenario run.
    /// </summary>
    public static GovernedExecutionBenchmarkFixture CreateFixture(
        string requestId,
        string currentStateVersion,
        string nextStateVersion)
    {
        var engine = MutationEngineBenchmarkSupport.BuildEngine(MutationEngineOptions.Strict);
        var store = new InMemoryMutationRequestStore();
        var resolutionManager = new MutationRequestVersionResolutionManager(store, new MutationRequestVersionResolver());
        var executionManager = new GovernanceExecutionManager(store, resolutionManager, engine);
        var request = store.Create(CreateApprovedRequest<GovernedExecutionState, IncrementValueMutation>(requestId))
            .GetAwaiter()
            .GetResult();

        return new GovernedExecutionBenchmarkFixture(
            ExecutionManager: executionManager,
            Request: request,
            State: new GovernedExecutionState(StateId, 42, currentStateVersion),
            Mutation: new IncrementValueMutation(
                MutationContext.User("operator", "Operator", "Execute governed request"),
                nextStateVersion));
    }

    /// <summary>
    /// Creates the approved request used by governed execution benchmarks.
    /// </summary>
    private static MutationRequest CreateApprovedRequest<TState, TMutation>(string requestId)
        where TMutation : IMutation<TState>
        => MutationRequestFactory.Approved<TState, TMutation>(
            stateId: StateId,
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need governed execution"),
            expectedStateVersion: "v10")
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Creates the intent used by governed execution scenarios.
    /// </summary>
    private static MutationIntent CreateIntent()
        => new()
        {
            OperationName = "ExecuteGovernedRequest",
            Category = "Governance",
            Description = "Execute a governed request through orchestration",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

    /// <summary>
    /// Minimal versioned state used by governed execution benchmarks.
    /// </summary>
    /// <param name="StateId">Stable state identifier.</param>
    /// <param name="Value">Benchmark counter value.</param>
    /// <param name="Version">Current state version.</param>
    internal sealed record GovernedExecutionState(string StateId, int Value, string Version) : IVersionedState;

    /// <summary>
    /// Minimal mutation used to measure governed execution orchestration.
    /// </summary>
    internal sealed class IncrementValueMutation(MutationContext context, string nextVersion)
        : IMutation<GovernedExecutionState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "IncrementGovernedValue",
            Category = "Governance",
            Description = "Increment a governed benchmark value",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

        public MutationContext Context { get; } = context;

        public MutationResult<GovernedExecutionState> Apply(GovernedExecutionState state)
        {
            var next = state with
            {
                Value = state.Value + 1,
                Version = nextVersion
            };

            return MutationResult<GovernedExecutionState>.Success(
                next,
                ChangeSet.Single(StateChange.Modified(nameof(GovernedExecutionState.Value), state.Value, next.Value)));
        }

        public ValidationResult Validate(GovernedExecutionState state)
        {
            return state.Value < 0
                ? ValidationResult.WithError(nameof(GovernedExecutionState.Value), "Value must be non-negative.")
                : ValidationResult.Success();
        }

        public MutationResult<GovernedExecutionState> Simulate(GovernedExecutionState state) => Apply(state);
    }

    /// <summary>
    /// Shared execution fixture for a benchmark scenario.
    /// </summary>
    internal sealed record GovernedExecutionBenchmarkFixture(
        GovernanceExecutionManager ExecutionManager,
        MutationRequest Request,
        GovernedExecutionState State,
        IncrementValueMutation Mutation);
}
