using ModularityKit.Mutator.Governance.Abstractions.Execution.Contracts;

namespace ModularityKit.Mutator.Benchmarks.Governance.Materialization.Support;

/// <summary>
/// Minimal versioned state used by governance materialization benchmarks.
/// </summary>
/// <param name="StateId">Stable state identifier.</param>
/// <param name="Value">Benchmark counter value.</param>
/// <param name="Version">Current state version.</param>
internal sealed record GovernanceMaterializationState(string StateId, int Value, string Version) : IVersionedState;
