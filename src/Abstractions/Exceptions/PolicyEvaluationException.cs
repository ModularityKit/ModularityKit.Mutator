namespace ModularityKit.Mutator.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when a policy evaluation fails before producing a decision.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="PolicyEvaluationException"/>.
/// </remarks>
/// <param name="policyName">The policy name.</param>
/// <param name="message">Human-readable description of the failure.</param>
/// <param name="innerException">The underlying failure.</param>
public class PolicyEvaluationException(string policyName, string message, Exception? innerException = null)
    : MutationException(message, innerException ?? new Exception(message))
{

    /// <summary>
    /// The name of the policy that failed.
    /// </summary>
    public string PolicyName { get; } = policyName;
}
