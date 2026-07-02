namespace ModularityKit.Mutator.Benchmarks.Diagnostics.Support;

/// <summary>
/// Minimal state used by diagnostics benchmark scenarios.
/// </summary>
/// <param name="Counter">The mutable numeric field exercised by the benchmark mutation.</param>
/// <param name="LastOperation">The last logical operation label written by the mutation.</param>
public sealed record DiagnosticsState(int Counter, string LastOperation);
