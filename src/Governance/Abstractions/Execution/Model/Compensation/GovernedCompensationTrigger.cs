namespace ModularityKit.Mutator.Governance.Abstractions.Execution.Model.Compensation;

/// <summary>
/// Describes what initiated a governed compensation plan.
/// </summary>
public enum GovernedCompensationTrigger
{
    /// <summary>
    /// A human operator explicitly initiated rollback or correction.
    /// </summary>
    OperatorRollback = 0,

    /// <summary>
    /// A batch workflow initiated compensation after one or more failures.
    /// </summary>
    BatchFailure = 1,

    /// <summary>
    /// A failed execution path initiated a compensating follow-up action.
    /// </summary>
    FailedExecution = 2
}
