using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime;

namespace ModularityKit.Mutator.Benchmarks.Engine;

/// <summary>
/// Shared support types for core engine benchmark scenarios.
/// </summary>
internal static class MutationEngineBenchmarkSupport
{
    public const string CounterStateId = "benchmark-counter";

    public static IMutationEngine BuildEngine(
        MutationEngineOptions options,
        Action<IMutationEngine>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddMutators(options);

        var engine = services
            .BuildServiceProvider()
            .GetRequiredService<IMutationEngine>();

        configure?.Invoke(engine);
        return engine;
    }

    public static IncrementCounterMutation CreateCounterMutation(MutationMode mode, string operationSuffix)
    {
        var context = MutationContext.System("benchmark")
            with
            {
                StateId = CounterStateId,
                Mode = mode,
                CorrelationId = $"{CounterStateId}:{operationSuffix}"
            };

        return new IncrementCounterMutation(context);
    }

    /// <summary>
    /// Minimal counter state used by engine benchmark scenarios.
    /// </summary>
    /// <param name="Value">The current counter value.</param>
    public sealed record CounterState(int Value);

    /// <summary>
    /// Minimal counter mutation shared by core engine benchmark scenarios.
    /// </summary>
    public sealed class IncrementCounterMutation(MutationContext context) : IMutation<CounterState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "IncrementCounter",
            Category = "Benchmark",
            Description = "Increment the benchmark counter by one",
            RiskLevel = MutationRiskLevel.Low,
            IsReversible = true
        };

        public MutationContext Context { get; } = context;

        public MutationResult<CounterState> Apply(CounterState state)
        {
            var next = state with { Value = state.Value + 1 };

            return MutationResult<CounterState>.Success(
                next,
                ChangeSet.Single(StateChange.Modified(nameof(CounterState.Value), state.Value, next.Value)));
        }

        public ValidationResult Validate(CounterState state)
        {
            var result = ValidationResult.Success();

            if (state.Value < 0)
                result.AddError(nameof(CounterState.Value), "Counter value must be non-negative.");

            return result;
        }

        public MutationResult<CounterState> Simulate(CounterState state)
        {
            var next = state with { Value = state.Value + 1 };

            return MutationResult<CounterState>.Success(
                next,
                ChangeSet.Single(StateChange.Modified(nameof(CounterState.Value), state.Value, next.Value)));
        }
    }

    /// <summary>
    /// Trivial allow policy used to measure policy-aware engine paths.
    /// </summary>
    public sealed class AllowAllCounterPolicy : IMutationPolicy<CounterState>
    {
        public string Name => nameof(AllowAllCounterPolicy);

        public int Priority => 0;

        public string? Description => "Always allows the benchmark counter mutation.";

        public PolicyDecision Evaluate(IMutation<CounterState> mutation, CounterState state)
            => PolicyDecision.Allow(Name, "Benchmark policy allows all mutations.");
    }
}
