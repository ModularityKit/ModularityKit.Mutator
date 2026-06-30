using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Abstractions;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Policies;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Runtime;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime;

public sealed class MutationEnginePolicyTests
{
    [Fact]
    public async Task ExecuteAsync_supports_async_policy_evaluation()
    {
        var engine = CreateEngine();
        engine.RegisterPolicy(new AsyncBlockingPolicy());

        var result = await engine.ExecuteAsync(new SampleMutation(), new SampleState("initial"));

        Assert.False(result.IsSuccess);
        Assert.Single(result.PolicyDecisions);
        Assert.Equal("AsyncBlocking", result.PolicyDecisions[0].PolicyName);
        Assert.Equal("External compliance check rejected the mutation.", result.PolicyDecisions[0].Reason);
    }

    [Fact]
    public async Task ExecuteAsync_throws_policy_evaluation_timeout_exception_for_slow_policy()
    {
        var engine = CreateEngine(options => options.PolicyEvaluationTimeout = TimeSpan.FromMilliseconds(50));
        engine.RegisterPolicy(new SlowAsyncPolicy());

        var exception = await Assert.ThrowsAsync<PolicyEvaluationTimeoutException>(() =>
            engine.ExecuteAsync(new SampleMutation(), new SampleState("initial")));

        Assert.Equal("SlowExternalCheck", exception.PolicyName);
    }

    [Fact]
    public async Task ExecuteAsync_wraps_policy_evaluation_failures()
    {
        var engine = CreateEngine();
        engine.RegisterPolicy(new FailingAsyncPolicy());

        var exception = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            engine.ExecuteAsync(new SampleMutation(), new SampleState("initial")));

        Assert.Equal("FailingExternalCheck", exception.PolicyName);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task ExecuteAsync_preserves_caller_cancellation_during_policy_evaluation()
    {
        var engine = CreateEngine();
        engine.RegisterPolicy(new CancelAwareAsyncPolicy());
        using var cancellationSource = new CancellationTokenSource(millisecondsDelay: 50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.ExecuteAsync(new SampleMutation(), new SampleState("initial"), cancellationSource.Token));
    }

    [Fact]
    public async Task ExecuteAsync_uses_sync_policy_path_without_async_override()
    {
        var engine = CreateEngine();
        engine.RegisterPolicy(new SyncAllowPolicy());

        var result = await engine.ExecuteAsync(new SampleMutation(), new SampleState("initial"));

        Assert.True(result.IsSuccess);
        Assert.Equal("updated", result.NewState!.Value);
    }

    private static IMutationEngine CreateEngine(Action<MutationEngineOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddMutators(configure: configure);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMutationEngine>();
    }

    private sealed record SampleState(string Value);

    private sealed class SampleMutation : MutationBase<SampleState>
    {
        public SampleMutation()
            : base(
                CreateIntent(
                    operationName: "UpdateSample",
                    category: "Test",
                    description: "Exercise policy evaluation"),
                MutationContext.System("Policy test") with { StateId = "sample-1" })
        {
        }

        public override MutationResult<SampleState> Apply(SampleState state)
            => Success(state with { Value = "updated" }, StateChange.Modified("Value", state.Value, "updated"));
    }

    private sealed class SyncAllowPolicy : IMutationPolicy<SampleState>
    {
        public string Name => "SyncAllow";
        public int Priority => 10;
        public string? Description => "Simple synchronous allow policy.";

        public PolicyDecision Evaluate(IMutation<SampleState> mutation, SampleState state)
            => PolicyDecision.Allow(Name, "Synchronous policy allowed the mutation.");
    }

    private sealed class AsyncBlockingPolicy : IMutationPolicy<SampleState>
    {
        public string Name => "AsyncBlocking";
        public int Priority => 100;
        public string? Description => "Simulates an external compliance check.";

        public async Task<PolicyDecision> EvaluateAsync(
            IMutation<SampleState> mutation,
            SampleState state,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return PolicyDecision.Deny("External compliance check rejected the mutation.", Name);
        }
    }

    private sealed class SlowAsyncPolicy : IMutationPolicy<SampleState>
    {
        public string Name => "SlowExternalCheck";
        public int Priority => 100;
        public string? Description => "Simulates a slow external dependency.";

        public async Task<PolicyDecision> EvaluateAsync(
            IMutation<SampleState> mutation,
            SampleState state,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return PolicyDecision.Allow(Name, "Finished too late.");
        }
    }

    private sealed class FailingAsyncPolicy : IMutationPolicy<SampleState>
    {
        public string Name => "FailingExternalCheck";
        public int Priority => 100;
        public string? Description => "Simulates an external dependency failure.";

        public Task<PolicyDecision> EvaluateAsync(
            IMutation<SampleState> mutation,
            SampleState state,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Remote ticketing system unavailable.");
    }

    private sealed class CancelAwareAsyncPolicy : IMutationPolicy<SampleState>
    {
        public string Name => "CancelAware";
        public int Priority => 100;
        public string? Description => "Waits for cancellation.";

        public async Task<PolicyDecision> EvaluateAsync(
            IMutation<SampleState> mutation,
            SampleState state,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return PolicyDecision.Allow(Name, "Completed.");
        }
    }
}
