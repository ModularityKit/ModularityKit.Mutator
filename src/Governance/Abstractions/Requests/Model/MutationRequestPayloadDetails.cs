using ModularityKit.Mutator.Abstractions.Context;
using ModularityKit.Mutator.Abstractions.Intent;

namespace ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

/// <summary>
/// Groups the submitted mutation intent and requester context carried by a governed request.
/// </summary>
public sealed record MutationRequestPayloadDetails
{
    /// <summary>
    /// Intent associated with the requested mutation.
    /// </summary>
    public MutationIntent Intent { get; init; } = null!;

    /// <summary>
    /// Request context describing who requested the mutation and why.
    /// </summary>
    public MutationContext Context { get; init; } = null!;
}
