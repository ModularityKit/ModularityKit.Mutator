namespace ModularityKit.Mutator.Examples.SmokeTests.Support;

/// <summary>
/// Captures the observable outcome of running an example process.
/// </summary>
public sealed record ExampleRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);
