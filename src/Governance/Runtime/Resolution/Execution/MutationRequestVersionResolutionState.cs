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
    /// Appends a decision to the request decision history.
    /// </summary>
    public static MutationRequest AppendDecision(
        MutationRequest request,
        MutationRequestDecision decision)
    {
        return request with
        {
            Decisions = [.. request.Decisions, decision]
        };
    }

    /// <summary>
    /// Applies the rejected-as-stale state transition.
    /// </summary>
    public static MutationRequest ApplyRejectedAsStale(
        MutationRequest request,
        string currentStateVersion,
        MutationRequestDecision decision)
    {
        return AppendDecision(
            request with
            {
                Status = MutationRequestStatus.Rejected,
                PendingReason = null,
                UpdatedAt = decision.Timestamp
            },
            decision);
    }

    /// <summary>
    /// Applies the renewed-approval-required state transition.
    /// </summary>
    public static MutationRequest ApplyRenewedApprovalRequired(
        MutationRequest request,
        string currentStateVersion,
        MutationRequestDecision decision)
    {
        return AppendDecision(
            request with
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Approval,
                ExpectedStateVersion = currentStateVersion,
                UpdatedAt = decision.Timestamp
            },
            decision);
    }

    /// <summary>
    /// Applies the revalidation-required state transition.
    /// </summary>
    public static MutationRequest ApplyRevalidationRequired(
        MutationRequest request,
        string currentStateVersion,
        MutationRequestDecision decision)
    {
        return AppendDecision(
            request with
            {
                Status = MutationRequestStatus.Pending,
                PendingReason = PendingMutationReason.Revalidation,
                ExpectedStateVersion = currentStateVersion,
                UpdatedAt = decision.Timestamp
            },
            decision);
    }

    /// <summary>
    /// Builds metadata describing the expected and current state versions used during resolution.
    /// </summary>
    public static IReadOnlyDictionary<string, object> CreateVersionMetadata(
        string? expectedStateVersion,
        string currentStateVersion)
    {
        return new Dictionary<string, object>
        {
            ["ExpectedStateVersion"] = expectedStateVersion ?? string.Empty,
            ["CurrentStateVersion"] = currentStateVersion
        };
    }

    /// <summary>
    /// Builds the success reason for a request whose expected and current versions match.
    /// </summary>
    public static string BuildValidatedReason(
        string? expectedStateVersion,
        string currentStateVersion)
    {
        return string.IsNullOrWhiteSpace(expectedStateVersion)
            ? "No expected state version was provided. Request can proceed."
            : $"State version '{currentStateVersion}' matches the expected version.";
    }

    /// <summary>
    /// Builds the stale-version explanation used by stale resolution decisions.
    /// </summary>
    public static string BuildStaleReason(
        string expectedStateVersion,
        string currentStateVersion)
    {
        return $"Request expected state version '{expectedStateVersion}' but current version is '{currentStateVersion}'.";
    }
}
