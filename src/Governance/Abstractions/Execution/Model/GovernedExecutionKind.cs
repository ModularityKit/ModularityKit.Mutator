namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Model;

/// <summary>
/// Classifies governed execution as a primary mutation or compensating execution.
/// </summary>
public enum GovernedExecutionKind
{
    /// <summary>
    /// Standard governed execution for the originally requested mutation.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Governed execution intended to compensate for a prior execution.
    /// </summary>
    Compensation = 1
}
