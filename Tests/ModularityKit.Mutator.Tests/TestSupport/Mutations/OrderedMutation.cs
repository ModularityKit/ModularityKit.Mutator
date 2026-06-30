using System.Collections.Concurrent;
using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Tests.TestSupport.Engine;

namespace ModularityKit.Mutator.Tests.TestSupport.Mutations;

/// <summary>
/// Records execution order for batch execution tests.
/// </summary>
internal sealed class OrderedMutation(string stateId, string value, ConcurrentQueue<string> observed)
    : IMutation<OrderedState>
{
    public MutationIntent Intent { get; } = new()
    {
        OperationName = "Order",
        Category = "Test",
        Description = "Observe execution order"
    };

    public MutationContext Context { get; } = MutationContext.User("tester", "Tester", "Order test")
        with
    { StateId = stateId };

    public MutationResult<OrderedState> Apply(OrderedState state)
    {
        observed.Enqueue(value);
        return MutationResult<OrderedState>.Success(state with { Value = value }, ChangeSet.Empty);
    }

    public ValidationResult Validate(OrderedState state) => ValidationResult.Success();

    public MutationResult<OrderedState> Simulate(OrderedState state) => Apply(state);
}
