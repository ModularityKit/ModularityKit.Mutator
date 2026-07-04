using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Benchmarks.Engine;
using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Factory;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;
using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Resolution.Support;

/// <summary>
/// Builds repeatable governance version resolution benchmark fixtures.
/// </summary>
internal static class GovernanceVersionResolutionBenchmarkSupport
{
    public const string StateId = "governance-benchmark:resolution";

    /// <summary>
    /// Creates a fresh resolution fixture for a single scenario run.
    /// </summary>
    public static GovernanceVersionResolutionBenchmarkFixture CreateFixture(
        string requestId,
        string expectedStateVersion,
        string currentStateVersion)
    {
        _ = MutationEngineBenchmarkSupport.BuildEngine(MutationEngineOptions.Strict);

        var store = new InMemoryMutationRequestStore();
        var resolver = new MutationRequestVersionResolver();
        var resolutionManager = new MutationRequestVersionResolutionManager(store, resolver);
        var request = store.Create(CreateApprovedRequest<GovernanceVersionResolutionState, NoOpGovernanceResolutionMutation>(
                requestId,
                expectedStateVersion))
            .GetAwaiter()
            .GetResult();

        return new GovernanceVersionResolutionBenchmarkFixture(
            ResolutionManager: resolutionManager,
            Request: request,
            CurrentStateVersion: currentStateVersion,
            ResolutionContext: MutationContext.System("governance-resolution-benchmark"));
    }

    /// <summary>
    /// Creates the approved request used by governance version resolution benchmarks.
    /// </summary>
    private static MutationRequest CreateApprovedRequest<TState, TMutation>(
        string requestId,
        string expectedStateVersion)
        where TMutation : IMutation<TState>
        => MutationRequestFactory.Approved<TState, TMutation>(
            stateId: StateId,
            intent: CreateIntent(),
            context: MutationContext.User("requester", "Requester", "Need governed version resolution"),
            expectedStateVersion: expectedStateVersion)
        with
        {
            RequestId = requestId
        };

    /// <summary>
    /// Creates the intent used by governance version resolution scenarios.
    /// </summary>
    private static MutationIntent CreateIntent()
        => new()
        {
            OperationName = "ResolveGovernedVersion",
            Category = "Governance",
            Description = "Resolve governance request version against the current state snapshot",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

    /// <summary>
    /// Minimal versioned state used by governance version resolution benchmarks.
    /// </summary>
    /// <param name="StateId">Stable state identifier.</param>
    /// <param name="Version">Current state version.</param>
    internal sealed record GovernanceVersionResolutionState(string StateId, string Version) : IVersionedState;

    /// <summary>
    /// Minimal mutation type used to anchor governed request metadata for version resolution benchmarks.
    /// </summary>
    internal sealed class NoOpGovernanceResolutionMutation : IMutation<GovernanceVersionResolutionState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "NoOpGovernanceResolution",
            Category = "Governance",
            Description = "Anchor request metadata for governed version resolution benchmarks",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

        public MutationContext Context { get; } = MutationContext.System("governance-resolution-benchmark");

        public MutationResult<GovernanceVersionResolutionState> Apply(GovernanceVersionResolutionState state)
            => MutationResult<GovernanceVersionResolutionState>.Success(
                state,
                ChangeSet.Empty);

        public ValidationResult Validate(GovernanceVersionResolutionState state) => ValidationResult.Success();

        public MutationResult<GovernanceVersionResolutionState> Simulate(GovernanceVersionResolutionState state) => Apply(state);
    }

    /// <summary>
    /// Shared fixture for a governance version resolution scenario.
    /// </summary>
    internal sealed record GovernanceVersionResolutionBenchmarkFixture(
        MutationRequestVersionResolutionManager ResolutionManager,
        MutationRequest Request,
        string CurrentStateVersion,
        MutationContext ResolutionContext);
}
