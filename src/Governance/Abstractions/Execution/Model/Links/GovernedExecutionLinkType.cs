namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Links;

/// <summary>
/// Describes how one governed request relates to another.
/// </summary>
public enum GovernedExecutionLinkType
{
    /// <summary>
    /// The current request compensates for the linked request.
    /// </summary>
    Compensates = 0,

    /// <summary>
    /// The current request was compensated by the linked request.
    /// </summary>
    CompensatedBy = 1
}
