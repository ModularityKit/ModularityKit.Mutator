using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Runtime.Resolution.Evaluation;

/// <summary>
/// Evaluates whether governed request still matches the currently observed state version.
/// </summary>
internal static class MutationRequestVersionEvaluator
{
    /// <summary>
    /// Compares the request expected version with the current state version and returns normalized evaluation model.
    /// </summary>
    /// <param name="request">Governed request whose expected version should be evaluated.</param>
    /// <param name="currentStateVersion">Currently observed state version.</param>
    /// <returns>Normalized version evaluation used by governance resolution.</returns>
    public static MutationRequestVersionEvaluation Evaluate(
        MutationRequest request,
        string currentStateVersion)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(currentStateVersion))
            throw new ArgumentException("Current state version is required.", nameof(currentStateVersion));

        var expectedStateVersion = request.Versioning.ExpectedStateVersion;
        var isStale = !string.IsNullOrWhiteSpace(expectedStateVersion)
                      && !string.Equals(expectedStateVersion, currentStateVersion, StringComparison.Ordinal);

        return new MutationRequestVersionEvaluation
        {
            ExpectedStateVersion = expectedStateVersion,
            CurrentStateVersion = currentStateVersion,
            IsStale = isStale
        };
    }
}
