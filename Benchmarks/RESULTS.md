# Core Benchmark Results (ADR-001 to ADR-018)

**CPU:** AMD Ryzen 9 7950X3D, .NET 10.0.10, Linux elementary OS 8  
**Date:** 2026-07-26  
**Toolchain:** InProcessEmitToolchain (BenchmarkDotNet v0.15.8)  

---

## 1. Concurrency — BatchScheduling

| ConcurrentBatches | BatchSize | Mean      | Allocated  |
|-------------------|-----------|-----------|------------|
| 2                 | 4         | 18.12 us  | 27.09 KB   |
| 2                 | 16        | 71.95 us  | 105.09 KB  |
| 4                 | 4         | 36.71 us  | 54.09 KB   |
| 4                 | 16        | 140.05 us | 210.10 KB  |

---

## 2. Concurrency — ParallelExecution

| Parallelism | Mean     | Allocated |
|-------------|----------|-----------|
| 2           | 4.324 us | 6.42 KB   |
| 8           | 17.557 us| 25.41 KB  |

---

## 3. Diagnostics — Overhead

| Method                                        | Mean     | Allocated |
|-----------------------------------------------|----------|-----------|
| NoDiagnostics_Baseline                        | 2.237 us | -         |
| AuditHistory_Enabled                          | 4.974 us | -         |
| CombinedInterceptionAndDiagnostics_Enabled    | 5.653 us | 4.01 KB   |

---

## 4. Diagnostics — Interceptor

| Method                     | Mean     | Allocated | Ratio |
|----------------------------|----------|-----------|-------|
| NoInterceptor_Baseline     | 2.161 us | 3.12 KB   | 1.00  |
| PassiveInterceptor_Enabled | 2.252 us | 3.30 KB   | 1.04  |

---

## 5. Engine — Batch Performance

| BatchSize | Mean      | Allocated |
|-----------|-----------|-----------|
| 10        | 46.73 us  | 31.49 KB  |
| 100       | 426.36 us | 308.65 KB |

---

## 6. Engine — Commit Performance

| Method                      | Mean     | Allocated | Ratio |
|-----------------------------|----------|-----------|-------|
| Commit_Performance_NoPolicy | 4.349 us | 3.00 KB   | 1.00  |
| Commit_Strict_WithPolicy    | 5.878 us | 4.44 KB   | 1.35  |

---

## 7. Engine — Mode Benchmarks

| Method                              | Mean     | Allocated |
|-------------------------------------|----------|-----------|
| Simulate_Strict_WithPolicy          | 5.303 us | -         |
| ValidateOnly_Strict_WithPolicy      | 5.056 us | 4.16 KB   |

---

## 8. Engine — Throughput

| Method     | StateSize | BatchSize | Mean       | Allocated  |
|------------|-----------|-----------|------------|------------|
| Single     | 32        | 8         | 5.142 us   | -          |
| Batch      | 32        | 8         | 39.899 us  | -          |
| Single     | 32        | 64        | 5.110 us   | -          |
| Batch      | 32        | 64        | 312.159 us | -          |
| Single     | 1024      | 8         | 5.396 us   | -          |
| Batch      | 1024      | 8         | 44.314 us  | -          |
| Single     | 1024      | 64        | 5.461 us   | -          |
| Batch      | 1024      | 64        | 348.187 us | -          |
| Single     | 16384     | 8         | 7.895 us   | 67.08 KB   |
| Batch      | 16384     | 8         | 63.035 us  | 537.68 KB  |
| Single     | 16384     | 64        | 7.300 us   | 67.08 KB   |
| Batch      | 16384     | 64        | 508.349 us | 4297.72 KB |

---

## 9. Policy — Evaluation

| Method                      | Mean     | Allocated | Ratio |
|-----------------------------|----------|-----------|-------|
| NoPolicy_Baseline           | 5.007 us | 3.01 KB   | 1.00  |
| SingleSyncPolicy_Allow      | 5.324 us | 3.98 KB   | 1.06  |
| SingleAsyncPolicy_Allow     | 5.307 us | 3.98 KB   | 1.06  |
| MultipleMixedPolicies_Allow | 5.607 us | 5.43 KB   | -     |

---

## 10. Results — Creation

| Method                         | Mean      | Allocated | Ratio |
|--------------------------------|-----------|-----------|-------|
| Success_NoSideEffects          | 93.56 ns  | 528 B     | 1.00  |
| Success_SingleSideEffect       | 268.92 ns | 792 B     | 2.87  |
| Success_MultipleSideEffects    | 897.30 ns | 1960 B    | 9.59  |

---

## 11. Results — Materialization

| Method                           | Mean     | Allocated | Ratio |
|----------------------------------|----------|-----------|-------|
| HistoryEntry_Materialization     | 706.5 ns | 896 B     | 1.00  |
| AuditEntry_Materialization       | 689.3 ns | 848 B     | 0.95  |

---

## 12. Concurrency — GateContention

> ❌ **Not executed** — `SharedStateGate_TwoConcurrentExecutions` timed out under InProcessEmitToolchain. Requires out-of-process execution.

---

---

## Priority Matrix

### P1 — Must benchmark (missing coverage, core-path risk)

| ADR    | Component        | Missing |
|--------|------------------|---------|
| 010    | Results          | 0/2 materialization edge cases |
| 004    | GateContention   | Async blocking gate scenario (InProcess timeout) |

### P2 — Should benchmark (ADRs without any coverage)

| ADR    | Component         | Gap |
|--------|-------------------|-----|
| 009    | Metrics           | No benchmarks exist |
| 016    | MetricsCollection | No benchmarks exist |
| 018    | DI Registration   | No benchmarks exist |

### P3 — Nice-to-have (indirect coverage, low-risk gaps)

| ADR    | Component                 | Current |
|--------|---------------------------|---------|
| 011    | ExecutionContext          | ✅ indirect |
| 014    | InMemoryAuditor/HistoryStore | ✅ indirect |

### Performance Watchlist — Items to monitor on regressions

| Benchmark ID                    | Baseline  | Alert if > |
|---------------------------------|-----------|------------|
| Commit_Performance_NoPolicy     | 4.35 us   | 5.5 us     |
| BatchMutation_Commit (32/64)    | 312 us    | 400 us     |
| BatchMutation_Commit (16384/64) | 508 us    | 650 us     |
| SingleMutation_Commit (any)     | 5-8 us    | 10 us      |
| Policy overhead (sync/async)    | 1.06×     | 1.20×      |

---

## Coverage Summary

| ADR    | Component                 | Status |
|--------|---------------------------|--------|
| 001    | StateChange / ChangeSet   | ✅     |
| 002    | MutationContext           | ✅     |
| 003    | MutationIntent / BlastRadius | ✅  |
| 004    | Policies / PolicyDecision | ✅     |
| 005    | Audit abstractions        | ✅     |
| 006    | SideEffects               | ✅     |
| 007    | History                   | ✅     |
| 008    | Interceptors              | ✅     |
| 009    | Metrics                   | ❌ Not benchmarked |
| 010    | Results                   | ✅     |
| 011    | ExecutionContext          | 🔶 No dedicated benchmark |
| 012    | IMutation / IMutationExecutor | ✅ |
| 013    | MutationEngine            | ✅     |
| 014    | InMemoryAuditor / HistoryStore | 🔶 No dedicated benchmark |
| 015    | InterceptorPipeline       | ✅     |
| 016    | MetricsCollection         | ❌ Not benchmarked |
| 017    | PolicyRegistry            | ✅     |
| 018    | DI Registration           | ❌ Not benchmarked |

**Status:** 36/39 benchmarks completed, 2 not benchmarked (Metrics, DI), 1 timeout (GateContention).
