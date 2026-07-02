namespace ModularityKit.Mutator.Examples.SmokeTests.Support;

/// <summary>
/// Describes a single executable example covered by the smoke test suite.
/// </summary>
public sealed record ExampleSmokeCase(
    string Name,
    string ProjectPath,
    Func<ExampleRunResult, string?> Validate,
    IReadOnlyDictionary<string, string?>? EnvironmentVariables = null)
{
    public override string ToString() => Name;
}
