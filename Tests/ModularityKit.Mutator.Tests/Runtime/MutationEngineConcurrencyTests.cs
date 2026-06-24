using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime;

public sealed class MutationEngineConcurrencyTests
{
    [Fact]
    public async Task ExecuteAsync_serializes_mutations_that_target_the_same_state_id()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options =>
        {
            options.MaxConcurrentMutations = 4;
            options.EnableDetailedMetrics = false;
        });

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        using var gate = new BlockingMutationGate();
        var state = new OrderedState("initial");

        var first = new BlockingMutation(gate, "shared-state", "first");
        var second = new BlockingMutation(gate, "shared-state", "second");

        var firstTask = Task.Run(() => engine.ExecuteAsync(first, state));
        var secondTask = Task.Run(() => engine.ExecuteAsync(second, state));

        Assert.True(await gate.WaitForEntriesAsync(1, TimeSpan.FromSeconds(5)));
        Assert.Equal(1, gate.PeakConcurrency);

        gate.Release();

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, gate.PeakConcurrency);
    }

    [Fact]
    public async Task ExecuteAsync_honors_max_concurrent_mutations_for_different_states()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options =>
        {
            options.MaxConcurrentMutations = 2;
            options.EnableDetailedMetrics = false;
        });

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        using var gate = new BlockingMutationGate();
        var states = new[]
        {
            new OrderedState("one"),
            new OrderedState("two"),
            new OrderedState("three"),
            new OrderedState("four")
        };

        var tasks = new[]
        {
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-1", "one"), states[0])),
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-2", "two"), states[1])),
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-3", "three"), states[2])),
            Task.Run(() => engine.ExecuteAsync(new BlockingMutation(gate, "state-4", "four"), states[3]))
        };

        Assert.True(await gate.WaitForEntriesAsync(2, TimeSpan.FromSeconds(5)));
        Assert.Equal(2, gate.PeakConcurrency);

        gate.Release();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(2, gate.PeakConcurrency);
    }

    [Fact]
    public async Task ExecuteBatchAsync_remains_ordered_while_respecting_runtime_concurrency_gates()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options =>
        {
            options.MaxConcurrentMutations = 2;
            options.EnableDetailedMetrics = false;
        });

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IMutationEngine>();
        var observed = new ConcurrentQueue<string>();

        var batch = new[]
        {
            new OrderedMutation("state-1", "first", observed),
            new OrderedMutation("state-2", "second", observed),
            new OrderedMutation("state-1", "third", observed)
        };

        var result = await engine.ExecuteBatchAsync(batch, new OrderedState("initial"));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Results.Count);
        Assert.Equal(new[] { "first", "second", "third" }, observed);
    }

    [Fact]
    public void AddMutators_rejects_non_positive_max_concurrent_mutations()
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: options => options.MaxConcurrentMutations = 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => services.BuildServiceProvider().GetRequiredService<IMutationEngine>());
    }

    private sealed record OrderedState(string Value);

    private sealed class OrderedMutation(string stateId, string value, ConcurrentQueue<string> observed)
        : IMutation<OrderedState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "Order",
            Category = "Test",
            Description = "Observe execution order"
        };

        public MutationContext Context { get; } = MutationContext.User("tester", "Tester", "Order test")
            with { StateId = stateId };

        public MutationResult<OrderedState> Apply(OrderedState state)
        {
            observed.Enqueue(value);
            return MutationResult<OrderedState>.Success(state with { Value = value }, ChangeSet.Empty);
        }

        public ValidationResult Validate(OrderedState state) => ValidationResult.Success();

        public MutationResult<OrderedState> Simulate(OrderedState state) => Apply(state);
    }

    private sealed class BlockingMutationGate : IDisposable
    {
        private readonly ManualResetEventSlim _release = new(false);
        private int _entered;
        private int _active;
        private int _peak;

        public int PeakConcurrency => Volatile.Read(ref _peak);

        public async Task<bool> WaitForEntriesAsync(int expectedEntries, TimeSpan timeout)
        {
            var started = DateTimeOffset.UtcNow;

            while (Volatile.Read(ref _entered) < expectedEntries)
            {
                if (DateTimeOffset.UtcNow - started > timeout)
                    return false;

                await Task.Delay(10);
            }

            return true;
        }

        public void Enter()
        {
            Interlocked.Increment(ref _entered);
            var active = Interlocked.Increment(ref _active);

            while (true)
            {
                var peak = Volatile.Read(ref _peak);
                if (active <= peak || Interlocked.CompareExchange(ref _peak, active, peak) == peak)
                    break;
            }

            _release.Wait();
            Interlocked.Decrement(ref _active);
        }

        public void Release() => _release.Set();

        public void Dispose() => _release.Dispose();
    }

    private sealed class BlockingMutation(
        BlockingMutationGate gate,
        string stateId,
        string value) : IMutation<OrderedState>
    {
        public MutationIntent Intent { get; } = new()
        {
            OperationName = "Block",
            Category = "Test",
            Description = "Block until released"
        };

        public MutationContext Context { get; } = MutationContext.User($"{stateId}-actor", $"{stateId}-actor", "Concurrency test")
            with { StateId = stateId };

        public MutationResult<OrderedState> Apply(OrderedState state)
        {
            gate.Enter();
            return MutationResult<OrderedState>.Success(state with { Value = value }, ChangeSet.Empty);
        }

        public ValidationResult Validate(OrderedState state) => ValidationResult.Success();

        public MutationResult<OrderedState> Simulate(OrderedState state) => Apply(state);
    }
}
