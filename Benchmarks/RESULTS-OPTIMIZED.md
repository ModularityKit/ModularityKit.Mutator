# Optimized Benchmark Results (Post-Optimization)

**CPU:** AMD Ryzen 9 7950X3D, .NET 10.0.10, Linux elementary OS 8  
**Date:** 2026-07-26  
**Toolchain:** InProcessEmitToolchain (BenchmarkDotNet v0.15.8)  

**Optimizations applied:**
- `InterceptorPipeline` — lock-free `volatile` snapshot cache + `Array.Empty` shortcut (eliminates 3 allocations per pipeline call when 0 interceptors registered)
- `MutationEngine` — `Interlocked.Increment` hex counter instead of `Guid.NewGuid().ToString()` (eliminates 1 string GUID allocation per mutation)

---

## 4. Diagnostics — Interceptor

| Method                     | Mean     | Allocated | Ratio | vs Before |
|----------------------------|----------|-----------|-------|-----------|
| NoInterceptor_Baseline     | 1.582 us | 3.00 KB   | 1.00  | **-26.8%** |
| PassiveInterceptor_Enabled | 1.609 us | 3.00 KB   | 1.02  | **-28.6%** |

---

## 6. Engine — Commit Performance

| Method                      | Mean     | Allocated | Ratio | vs Before |
|-----------------------------|----------|-----------|-------|-----------|
| Commit_Performance_NoPolicy | 4.027 us | 3.88 KB   | 1.00  | **-7.4%** |
| Commit_Strict_WithPolicy    | 5.277 us | 4.32 KB   | 1.31  | **-10.2%** |

---

## Performance Watchlist (Updated)

| Benchmark ID                    | Baseline (before) | Baseline (after) | Alert if > |
|---------------------------------|-------------------|------------------|------------|
| Commit_Performance_NoPolicy     | 4.35 us           | 4.03 us          | 5.0 us     |
| Interceptor_Baseline            | 2.16 us           | 1.64 us          | 2.0 us     |
| Interceptor_Enabled             | 2.25 us           | 1.63 us          | 2.0 us     |
| BatchMutation_Commit (32/64)    | 312 us            | —                | 400 us     |
| BatchMutation_Commit (16384/64) | 508 us            | —                | 650 us     |
| SingleMutation_Commit (any)     | 5-8 us            | —                | 10 us      |
| Policy overhead (sync/async)    | 1.06×             | —                | 1.20×      |
