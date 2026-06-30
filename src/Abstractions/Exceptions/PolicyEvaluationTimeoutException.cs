namespace ModularityKit.Mutator.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when policy evaluation exceeds the configured timeout.
/// </summary>
/// <remarks>
/// Initializes new instance of <see cref="PolicyEvaluationTimeoutException"/>.
/// </remarks>
/// <param name="policyName">The policy name.</param>
/// <param name="timeout">The configured timeout.</param>
public sealed class PolicyEvaluationTimeoutException(string policyName, TimeSpan timeout) : PolicyEvaluationException(
        policyName,
        $"Policy '{policyName}' evaluation timed out after {timeout.TotalSeconds:0.###}s.")
{
    /// <summary>
    /// The configured timeout for policy evaluation.
    /// </summary>
    public TimeSpan Timeout { get; } = timeout;
}
