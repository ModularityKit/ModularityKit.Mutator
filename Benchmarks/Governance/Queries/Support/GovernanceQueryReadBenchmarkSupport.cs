using ModularityKit.Mutator.Governance.Runtime.Storage;

namespace ModularityKit.Mutator.Benchmarks.Governance.Queries.Support;

/// <summary>
/// Builds repeatable governance query benchmark fixtures.
/// </summary>
internal static class GovernanceQueryReadBenchmarkSupport
{
    /// <summary>
    /// Creates a seeded fixture for query and read benchmarks.
    /// </summary>
    public static GovernanceQueryReadBenchmarkFixture CreateFixture()
        => new(
            RequestStore: PendingRequestQueryBenchmarkSeed.CreateStore(),
            ApprovalStore: PendingApprovalQueryBenchmarkSeed.CreateStore(),
            DecisionStore: RecentDecisionQueryBenchmarkSeed.CreateStore());

    /// <summary>
    /// Shared fixture for governance query and read scenarios.
    /// </summary>
    internal sealed record GovernanceQueryReadBenchmarkFixture(
        InMemoryMutationRequestStore RequestStore,
        InMemoryMutationRequestStore ApprovalStore,
        InMemoryMutationRequestStore DecisionStore);
}
