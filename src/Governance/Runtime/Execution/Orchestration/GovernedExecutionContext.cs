using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Governance.Abstractions.Resolution.Model;
using ModularityKit.Mutator.Governance.Runtime.Execution.Mutation;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;

/// <summary>
/// Carries the resolved runtime inputs for one governed execution attempt.
/// </summary>
internal sealed record GovernedExecutionContext<TState>(
    MutationRequestVersionResolution Resolution,
    GovernedMutation<TState> Mutation,
    TState CurrentState,
    string CurrentStateVersion,
    Func<TState, string> ResultingStateVersionProvider,
    MutationContext GovernanceContext);
