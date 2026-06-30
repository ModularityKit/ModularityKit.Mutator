using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Exceptions;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Host;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Policy;
using ModularityKit.Mutator.Tests.TestSupport.Engine.Samples;
using ModularityKit.Mutator.Tests.TestSupport.Mutations;
using Xunit;

namespace ModularityKit.Mutator.Tests.Runtime.Engine;

public sealed class MutationEnginePolicyTests
{
    [Fact]
    public async Task ExecuteAsync_supports_async_policy_evaluation()
    {
        var engine = MutationEngineTestHost.CreateEngine();
        engine.RegisterPolicy(new AsyncBlockingPolicy());

        var result = await engine.ExecuteAsync(new PolicySampleMutation(), new PolicySampleState("initial"));

        Assert.False(result.IsSuccess);
        Assert.Single(result.PolicyDecisions);
        Assert.Equal("AsyncBlocking", result.PolicyDecisions[0].PolicyName);
        Assert.Equal("External compliance check rejected the mutation.", result.PolicyDecisions[0].Reason);
    }

    [Fact]
    public async Task ExecuteAsync_throws_policy_evaluation_timeout_exception_for_slow_policy()
    {
        var engine = MutationEngineTestHost.CreateEngine(options => options.PolicyEvaluationTimeout = TimeSpan.FromMilliseconds(50));
        engine.RegisterPolicy(new SlowAsyncPolicy());

        var exception = await Assert.ThrowsAsync<PolicyEvaluationTimeoutException>(() =>
            engine.ExecuteAsync(new PolicySampleMutation(), new PolicySampleState("initial")));

        Assert.Equal("SlowExternalCheck", exception.PolicyName);
    }

    [Fact]
    public async Task ExecuteAsync_wraps_policy_evaluation_failures()
    {
        var engine = MutationEngineTestHost.CreateEngine();
        engine.RegisterPolicy(new FailingAsyncPolicy());

        var exception = await Assert.ThrowsAsync<PolicyEvaluationException>(() =>
            engine.ExecuteAsync(new PolicySampleMutation(), new PolicySampleState("initial")));

        Assert.Equal("FailingExternalCheck", exception.PolicyName);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task ExecuteAsync_preserves_caller_cancellation_during_policy_evaluation()
    {
        var engine = MutationEngineTestHost.CreateEngine();
        engine.RegisterPolicy(new CancelAwareAsyncPolicy());
        using var cancellationSource = new CancellationTokenSource(millisecondsDelay: 50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.ExecuteAsync(new PolicySampleMutation(), new PolicySampleState("initial"), cancellationSource.Token));
    }

    [Fact]
    public async Task ExecuteAsync_uses_sync_policy_path_without_async_override()
    {
        var engine = MutationEngineTestHost.CreateEngine();
        engine.RegisterPolicy(new SyncAllowPolicy());

        var result = await engine.ExecuteAsync(new PolicySampleMutation(), new PolicySampleState("initial"));

        Assert.True(result.IsSuccess);
        Assert.Equal("updated", result.NewState!.Value);
    }

    [Fact]
    public async Task ExecuteAsync_allows_sync_and_async_policies_to_coexist_without_ambiguous_ordering()
    {
        var engine = MutationEngineTestHost.CreateEngine();
        var observed = new List<string>();

        engine.RegisterPolicy(new ObservedSyncAllowPolicy(observed));
        engine.RegisterPolicy(new ObservedAsyncAllowPolicy(observed));

        var result = await engine.ExecuteAsync(new PolicySampleMutation(), new PolicySampleState("initial"));

        Assert.True(result.IsSuccess);
        Assert.Equal(["async", "sync"], observed);
    }
}
