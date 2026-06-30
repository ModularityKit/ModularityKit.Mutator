namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;

/// <summary>
/// Distinguishes rollback-style restoration from forward corrective compensation.
/// </summary>
public enum GovernedCompensationKind
{
    /// <summary>
    /// Attempts to restore the prior state or its equivalent.
    /// </summary>
    Rollback = 0,

    /// <summary>
    /// Applies a forward corrective action instead of restoring prior state.
    /// </summary>
    ForwardCorrection = 1
}
