using ModularityKit.Mutator.Abstractions.Changes;
using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Engine;
using ModularityKit.Mutator.Abstractions.Intent;
using ModularityKit.Mutator.Abstractions.Results;
using ModularityKit.Mutator.Tests.TestSupport.Engine;

namespace ModularityKit.Mutator.Tests.TestSupport.Mutations;

/// <summary>
/// Blocks mutation execution until the shared test gate is released.
/// </summary>
internal sealed class BlockingMutation(
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
