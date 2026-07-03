namespace ModularityKit.Mutator.Benchmarks.Concurrency.Support;

/// <summary>
/// Minimal state used by concurrency benchmark scenarios.
/// </summary>
/// <param name="Counter">The mutable numeric field exercised by the benchmark mutation.</param>
/// <param name="Revision">The revision counter advanced on each benchmark mutation.</param>
public sealed record ConcurrencyState(int Counter, int Revision);
