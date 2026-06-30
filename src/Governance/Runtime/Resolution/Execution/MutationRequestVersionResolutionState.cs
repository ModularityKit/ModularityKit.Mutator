using ModularityKit.Mutator.Governance.Abstractions.Lifecycle.Model;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Decisions;
using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;
using ModularityKit.Mutator.Governance.Runtime.Resolution.Evaluation;

namespace ModularityKit.Mutator.Governance.Runtime.Resolution.Execution;

/// <summary>
/// Provides shared state transformations and metadata helpers for version-aware governance resolution.
/// </summary>
internal static class MutationRequestVersionResolutionState
{
    /// <summary>
    /// Appends decision to the request decision history.
    /// </summary>
    /// <param name="request">Request snapshot to update.</param>
    /// <param name="decision">Decision to append to the request history.</param>
    /// <returns>Updated request snapshot with appended decision.</returns>
    public static MutationRequest AppendDecision(MutationRequest request, MutationRequestDecision decision) =>
        request with
        {
            Decisions = [.. request.Decisions, decision]
        };

    /// <summary>
    /// Applies the rejected as stale state transition.
    /// </summary>
    /// <param name="request">Request snapshot to update.</param>
    /// <param name="currentStateVersion">Currently observed state version.</param>
    /// <param name="decision">Version-resolution decision to append.</param>
    /// <returns>Updated request snapshot marked as rejected.</returns>
    public static MutationRequest ApplyRejectedAsStale(MutationRequest request, string currentStateVersion, MutationRequestDecision decision) => AppendDecision(
        request with
        {
            Status = MutationRequestStatus.Rejected,
            PendingReason = null,
            UpdatedAt = decision.Timestamp
        }, decision);

    /// <summary>
    /// Applies the renewed approval required state transition.
    /// </summary>
    /// <param name="request">Request snapshot to update.</param>
    /// <param name="currentStateVersion">Currently observed state version.</param>
    /// <param name="decision">Version-resolution decision to append.</param>
    /// <returns>Updated request snapshot moved back to pending approval.</returns>
    public static MutationRequest ApplyRenewedApprovalRequired(
        MutationRequest request,
        string currentStateVersion,
        MutationRequestDecision decision) => AppendDecision(
            request with
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Approval,
                Versioning = request.Versioning with
                {
                    ExpectedStateVersion = currentStateVersion
                },
                UpdatedAt = decision.Timestamp
            },
            decision);

    /// <summary>
    /// Applies the revalidation required state transition.
    /// </summary>
    /// <param name="request">Request snapshot to update.</param>
    /// <param name="currentStateVersion">Currently observed state version.</param>
    /// <param name="decision">Version-resolution decision to append.</param>
    /// <returns>Updated request snapshot moved to pending revalidation.</returns>
    public static MutationRequest ApplyRevalidationRequired(
        MutationRequest request,
        string currentStateVersion,
        MutationRequestDecision decision) => AppendDecision(
            request with
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Revalidation,
                Versioning = request.Versioning with
                {
                    ExpectedStateVersion = currentStateVersion
                },
                UpdatedAt = decision.Timestamp
            },
            decision);


    /// <summary>
    /// Builds metadata describing the expected and current state versions used during resolution.
    /// </summary>
    /// <param name="expectedStateVersion">Expected request state version captured before resolution.</param>
    /// <param name="currentStateVersion">Currently observed state version.</param>
    /// <returns>Metadata map describing the compared versions.</returns>
    public static IReadOnlyDictionary<string, object> CreateVersionMetadata(string? expectedStateVersion, string currentStateVersion) =>
        new Dictionary<string, object>
        {
            ["ExpectedStateVersion"] = expectedStateVersion ?? string.Empty,
            ["CurrentStateVersion"] = currentStateVersion
        };

    /// <summary>
    /// Builds the success reason for request whose expected and current versions match.
    /// </summary>
    /// <param name="expectedStateVersion">Expected request state version captured before resolution.</param>
    /// <param name="currentStateVersion">Currently observed state version.</param>
    public static string BuildValidatedReason(
        string? expectedStateVersion,
        string currentStateVersion) => string.IsNullOrWhiteSpace(expectedStateVersion)
            ? "No expected state version was provided. Request can proceed."
            : $"State version '{currentStateVersion}' matches the expected version.";

    /// <summary>
    /// Builds the stale version explanation used by stale resolution decisions.
    /// </summary>
    /// <param name="expectedStateVersion">Expected request state version captured before resolution.</param>
    /// <param name="currentStateVersion">Currently observed state version.</param>
    public static string BuildStaleReason(string expectedStateVersion, string currentStateVersion) =>
         $"Request expected state version '{expectedStateVersion}' but current version is '{currentStateVersion}'.";
}
