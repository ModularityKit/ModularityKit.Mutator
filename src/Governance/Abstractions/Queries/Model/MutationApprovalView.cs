using ModularityKit.Mutator.Governance.Abstractions.Approval.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Abstractions.Queries.Model;

/// <summary>
/// Represents one approval oriented projection from governed mutation request.
/// </summary>
public sealed record MutationApprovalView
{
    /// <summary>
    /// Parent mutation request.
    /// </summary>
    public MutationRequest Request { get; init; } = null!;

    /// <summary>
    /// Approval requirement projected from the parent request.
    /// </summary>
    public MutationApprovalRequirement Approval { get; init; } = null!;
}
