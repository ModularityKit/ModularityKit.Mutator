# Example Smoke Tests

This project provides lightweight smoke coverage for the executable samples under:

- `Examples/Core`
- `Examples/Governance`

The goal is to catch sample drift against the current public API and runtime behavior without turning the examples into a second full test suite.

## Structure

- `Examples/Core` contains smoke tests for core runtime samples.
- `Examples/Governance` contains smoke tests for governance and Redis-backed samples.
- `Support` contains the shared runner and result models used by the smoke layer.

## How it works

Each smoke test:

1. builds the target example project in `Release`
2. executes the built assembly with `dotnet exec`
3. captures `stdout` and `stderr`
4. validates a small set of expected signals for that sample

The runner keeps the checks focused on sample viability:

- non-zero exit codes fail the test
- hung processes are terminated by timeout
- failures include captured output for fast diagnosis

## Redis example behavior

`RedisQueries` accepts either:

- normal Redis-backed query output, or
- the documented "could not connect to Redis" message

That keeps the sample smoke testable even when Redis is not running locally.

## Run

Build:

```bash
dotnet build Tests/ModularityKit.Mutator.Examples.SmokeTests/ModularityKit.Mutator.Examples.SmokeTests.csproj -c Debug
```

Run:

```bash
dotnet test Tests/ModularityKit.Mutator.Examples.SmokeTests/ModularityKit.Mutator.Examples.SmokeTests.csproj -c Debug --no-build
```

Run a single smoke test:

```bash
dotnet test Tests/ModularityKit.Mutator.Examples.SmokeTests/ModularityKit.Mutator.Examples.SmokeTests.csproj -c Debug --no-build --filter FullyQualifiedName=ModularityKit.Mutator.Examples.SmokeTests.GovernanceExamplesSmokeTests.RedisQueries_runs_successfully
```

## Environment note

`vstest` uses a local socket. In restricted sandboxes that block local bind operations, the test host may need to run outside the sandbox even though the examples themselves are local processes.
