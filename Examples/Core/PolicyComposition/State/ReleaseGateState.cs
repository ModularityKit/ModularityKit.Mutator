namespace PolicyComposition.State;

/// <summary>
/// Minimal release state used to demonstrate governance policy composition.
/// </summary>
/// <remarks>
/// The example keeps the state intentionally small so the composition behavior
/// is easy to follow. Each field maps directly to a visible part of the policy
/// output:
/// <list type="bullet">
/// <item><description><see cref="ReleaseName"/> identifies the release flow.</description></item>
/// <item><description><see cref="Stage"/> shows which policy branch updated the release.</description></item>
/// <item><description><see cref="Owner"/> is used by the conflict example.</description></item>
/// </list>
/// </remarks>
public sealed record ReleaseGateState
{
    /// <summary>
    /// Creates a new release state.
    /// </summary>
    /// <param name="releaseName">The release identifier.</param>
    /// <param name="stage">The initial release stage.</param>
    /// <param name="owner">The initial release owner.</param>
    public ReleaseGateState(string releaseName, string stage, string owner)
    {
        ReleaseName = releaseName;
        Stage = stage;
        Owner = owner;
    }

    /// <summary>
    /// The release identifier used to correlate the example flow.
    /// </summary>
    public string ReleaseName { get; init; }

    /// <summary>
    /// The current release stage.
    /// </summary>
    public string Stage { get; init; }

    /// <summary>
    /// The current release owner.
    /// </summary>
    public string Owner { get; init; }
}
