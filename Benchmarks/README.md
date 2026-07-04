# Benchmarks

This folder contains BenchmarkDotNet measurements for `ModularityKit.Mutator`.

## What is benchmarked

- commit execution without policy pressure
- strict engine execution with policy checks
- simulate and validate only paths
- batch execution overhead
- throughput for single mutation execution across multiple state sizes
- throughput for batch mutation execution across multiple state sizes and batch sizes
- policy evaluation overhead for no policy, synchronous policy, asynchronous policy, and mixed multi policy runs
- interception, audit/history, and logging diagnostics overhead in the core runtime
- parallel execution, state gate contention, and concurrent batch scheduling pressure in the core runtime
- mutation result creation and history/audit output materialization in the core runtime
- governance approval workflow overhead in the governance runtime
- governance request lifecycle overhead in the governance runtime
- governance execution orchestration overhead in the governance runtime
- governance version resolution overhead in the governance runtime
- governance query and read performance overhead in the governance runtime

The throughput benchmarks use cloned array backed state so state size effects remain visible in the
actual mutation path rather than being hidden behind an artificial inner loop.

## Run

Build first:

```bash
dotnet build Benchmarks/ModularityKit.Mutator.Benchmarks.csproj -c Release
```

Run a specific benchmark:

```bash
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --filter '*MutationEngineCommitBenchmarks*'
```

Run the throughput-focused suite:

```bash
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --filter '*MutationEngineThroughputBenchmarks*'
```

Run the policy evaluation suite:

```bash
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --filter '*PolicyEvaluationSingleBenchmarks*'
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --filter '*PolicyEvaluationMultiBenchmarks*'
```

Run the diagnostics suite:

```bash
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --anyCategories Diagnostics
```

Run the concurrency suite:

```bash
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --anyCategories Concurrency
```

Run the results suite:

```bash
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --anyCategories Results
```

Run the governance suite:

```bash
dotnet Benchmarks/bin/Release/net10.0/ModularityKit.Mutator.Benchmarks.dll --anyCategories Governance
```

Key parameters reported by BenchmarkDotNet:

- `StateSize` controls the size of the cloned mutation state
- `BatchSize` controls the number of mutations executed in the batch benchmark

## Notes

- The benchmark harness is configured for the current environment.
- Results are emitted by BenchmarkDotNet when the runner can write artifacts.
