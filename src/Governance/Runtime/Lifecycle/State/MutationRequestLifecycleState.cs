using ModularityKit.Mutator.Governance.Abstractions.Requests.Model;

namespace ModularityKit.Mutator.Governance.Runtime.Lifecycle.State;

/// <summary>
/// Provides shared state transformations and metadata helpers for lifecycle transitions.
/// </summary>
internal static class MutationRequestLifecycleState
{
    /// <summary>
    /// Clears pending-only fields once a request leaves the pending lifecycle.
    /// </summary>
    public static MutationRequest ClearPendingState(MutationRequest request)
    {
        return request with
        {
            Lifecycle = request.Lifecycle with
            {
                PendingReason = null,
                ExpiresAt = null
            }
        };
    }

    /// <summary>
    /// Merges transition metadata with appended runtime metadata values.
    /// </summary>
    public static IReadOnlyDictionary<string, object> MergeMetadata(
        IReadOnlyDictionary<string, object>? metadata,
        IReadOnlyDictionary<string, object> appended)
    {
        var merged = new Dictionary<string, object>();

        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        foreach (var pair in appended)
        {
            merged[pair.Key] = pair.Value;
        }

        return merged;
    }
}
