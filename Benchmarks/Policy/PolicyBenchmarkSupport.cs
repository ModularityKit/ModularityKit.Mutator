using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Audit;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.History;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Benchmarks.Diagnostics.Support;
using ModularityKit.Mutator.Runtime;

namespace ModularityKit.Mutator.Benchmarks.Policy;

/// <summary>
/// Shared support types for policy evaluation benchmark scenarios.
/// </summary>
internal static class PolicyBenchmarkSupport
{
    public const string StateId = "policy-benchmark-state";

    public static IMutationEngine BuildEngine(Action<IMutationEngine>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddMutators(MutationEngineOptions.Performance);
        services.AddSingleton<IMutationAuditor>(new NoOpAuditor());
        services.AddSingleton<IMutationHistoryStore>(new NoOpHistoryStore());

        var engine = services
            .BuildServiceProvider()
            .GetRequiredService<IMutationEngine>();

        configure?.Invoke(engine);
        return engine;
    }

    public static MinimalPolicyMutation CreateMutation()
    {
        var context = MutationContext.System("policy-benchmark")
            with
            {
                StateId = StateId,
                Mode = MutationMode.Commit,
                CorrelationId = $"{StateId}:policy-pass"
            };

        return new MinimalPolicyMutation(context);
    }

    /// <summary>
    /// Minimal state used by policy evaluation benchmarks.
    /// </summary>
    /// <param name="Name">The logical state name.</param>
    /// <param name="Counter">The mutable numeric field exercised by the benchmark mutation.</param>
    public sealed record PolicyBenchmarkState(string Name, int Counter);

    /// <summary>
    /// Minimal mutation used to isolate policy pipeline overhead from unrelated runtime work.
    /// </summary>
    public sealed class MinimalPolicyMutation(MutationContext context) : IMutation<PolicyBenchmarkState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "MinimalPolicyMutation",
            Category = "Benchmark",
            Description = "Minimal mutation used to isolate policy evaluation overhead.",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

        public MutationContext Context { get; } = context;

        public MutationResult<PolicyBenchmarkState> Apply(PolicyBenchmarkState state)
        {
            var nextState = state with { Counter = state.Counter + 1 };

            return MutationResult<PolicyBenchmarkState>.Success(
                nextState,
                ChangeSet.Single(StateChange.Modified(nameof(PolicyBenchmarkState.Counter), state.Counter, nextState.Counter)));
        }

        public ValidationResult Validate(PolicyBenchmarkState state) => ValidationResult.Success();

        public MutationResult<PolicyBenchmarkState> Simulate(PolicyBenchmarkState state) => Apply(state);
    }

    /// <summary>
    /// Synchronous allow policy used in benchmark scenarios.
    /// </summary>
    public sealed class SyncAllowBenchmarkPolicy : IMutationPolicy<PolicyBenchmarkState>
    {
        private readonly PolicyDecision _decision;
        private readonly Task<PolicyDecision> _decisionTask;

        public SyncAllowBenchmarkPolicy(int priority)
        {
            Priority = priority;
            _decision = PolicyDecision.Allow(Name, "Synchronous benchmark policy allowed the mutation.");
            _decisionTask = Task.FromResult(_decision);
        }

        public string Name => $"{nameof(SyncAllowBenchmarkPolicy)}_{Priority}";

        public int Priority { get; }

        public string? Description => "Synchronous allow policy for benchmark measurements.";

        public PolicyDecision Evaluate(IMutation<PolicyBenchmarkState> mutation, PolicyBenchmarkState state)
            => _decision;

        public Task<PolicyDecision> EvaluateAsync(
            IMutation<PolicyBenchmarkState> mutation,
            PolicyBenchmarkState state,
            CancellationToken cancellationToken = default)
            => _decisionTask;
    }

    /// <summary>
    /// Asynchronous allow policy used in benchmark scenarios.
    /// </summary>
    public sealed class AsyncAllowBenchmarkPolicy : IMutationPolicy<PolicyBenchmarkState>
    {
        private readonly PolicyDecision _decision;
        private readonly Task<PolicyDecision> _decisionTask;

        public AsyncAllowBenchmarkPolicy(int priority)
        {
            Priority = priority;
            _decision = PolicyDecision.Allow(Name, "Asynchronous benchmark policy allowed the mutation.");
            _decisionTask = Task.FromResult(_decision);
        }

        public string Name => $"{nameof(AsyncAllowBenchmarkPolicy)}_{Priority}";

        public int Priority { get; }

        public string? Description => "Asynchronous allow policy for benchmark measurements.";

        public Task<PolicyDecision> EvaluateAsync(
            IMutation<PolicyBenchmarkState> mutation,
            PolicyBenchmarkState state,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _decisionTask;
        }
    }
}
