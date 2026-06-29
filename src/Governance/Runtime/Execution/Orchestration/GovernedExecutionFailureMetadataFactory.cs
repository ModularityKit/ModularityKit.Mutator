using ModularityKit.Mutator.Abstractions.Results;

namespace ModularityKit.Mutator.Governance.Runtime.Execution.Orchestration;

/// <summary>
/// Builds consistent governance metadata payloads for execution failures.
/// </summary>
internal static class GovernedExecutionFailureMetadataFactory
{
    public static IReadOnlyDictionary<string, object> CreateExceptionMetadata(
        string currentStateVersion,
        Exception exception)
        => new Dictionary<string, object>
        {
            ["CurrentStateVersion"] = currentStateVersion,
            ["ExecutionFailureType"] = exception.GetType().Name
        };

    public static IReadOnlyDictionary<string, object> CreateRejectedExecutionMetadata<TState>(
        string currentStateVersion,
        MutationResult<TState> mutationResult)
        => new Dictionary<string, object>
        {
            ["CurrentStateVersion"] = currentStateVersion,
            ["HasPolicyDecisions"] = mutationResult.PolicyDecisions.Count > 0,
            ["HasValidationErrors"] = !mutationResult.ValidationResult.IsValid
        };
}
