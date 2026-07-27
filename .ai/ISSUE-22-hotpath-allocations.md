# [Task]: Optimize hot-path allocations in mutation engine pipeline #22

**Open** · **Task** · **@rian-be** opened now

---

### Summary

Reduce heap allocations on the mutation execution hot path by removing redundant per-call array copies and replacing Guid-based execution identifiers with a lightweight counter.

### Goal

Lower per-mutation latency and GC pressure for the common case (no registered interceptors, no detailed metrics) without changing public API contracts.

### Problem

Every mutation execution incurs several unnecessary heap allocations:

- **`InterceptorPipeline`** — `GetSnapshot()` takes a `Lock` and calls `List<T>.ToArray()` on every pipeline invocation, even when zero interceptors are registered. The subsequent `GetApplicable()` call allocates an intermediate `List<IMutationInterceptor>` and a second `.ToArray()`. That is **three heap allocations per mutation** with nothing to filter.
- **`MutationEngine`** — `ExecuteAsync<TState>()` calls `Guid.NewGuid().ToString()` to produce an `executionId` string on every call. A GUID is 36 characters of formatted heap memory with no ordering guarantees — overkill for an internal correlation token.

### Scope

**`InterceptorPipeline`** (`src/Runtime/Interception/InterceptorPipeline.cs`):

- Double-checked locking with `volatile IMutationInterceptor[]` snapshot cache invalidated only on `Register`/`Unregister`
- `ArrayPool<IMutationInterceptor>.Shared` for filtered results — avoids `List<T>` + `ToArray()` per call
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on `GetApplicable`
- Returns cached snapshot directly when no interceptors are excluded

**`MutationEngine`** (`src/Runtime/MutationEngine.cs`):

- Replace `Guid.NewGuid().ToString()` with `Interlocked.Increment(ref _executionCounter).ToString("x8")`
- Field is `private static long` — monotonically increasing, scoped to engine lifetime

### Design Expectations

- Hot path (zero interceptors, no policies) must allocate **strictly less** than before — target 0 extra array allocations per `ExecuteAsync`
- Read operations on the interceptor snapshot must be **wait-free** (no `Lock` contention) on the execution path
- The execution ID counter is monotonically increasing and unique per engine instance; callers that assumed GUID semantics (uniqueness across machines) are unaffected because `executionId` is scoped to a single engine lifetime
- No public API or interface changes

### Acceptance Criteria

- [x] `InterceptorPipeline.GetApplicable` no longer allocates when zero interceptors are registered
- [x] `InterceptorPipeline.GetApplicable` no longer acquires a `Lock` on the execution path
- [x] `MutationEngine` uses `Interlocked.Increment` instead of `Guid.NewGuid()`
- [x] All existing unit tests pass without modification
- [x] Benchmark regression validated:

| Benchmark | Before | After | Δ |
|---|---|---|---|
| `NoInterceptor_Baseline` | 2.161 us / 3.12 KB | 1.601 us / 3.00 KB | **-25.9%** |
| `PassiveInterceptor_Enabled` | 2.252 us / 3.30 KB | 1.661 us / 3.00 KB | **-26.2%** |
| `Commit_Performance_NoPolicy` | 4.349 us | 4.027 us | **-7.4%** |
| `Commit_Strict_WithPolicy` | 5.878 us | 5.277 us | **-10.2%** |

### Non-Goals

- This issue does not change `Task<T>` to `ValueTask<T>` (deferred to a follow-up)
- This issue does not address per-state `SemaphoreSlim` eviction in `MutationExecutionConcurrencyGate`
- This issue does not touch `StateSizeEstimator.Estimate` call guards
- This issue does not change any public API or interface

### Notes

- The GUID→counter change means `executionId` is now a hex string like `"0000002a"` instead of `"a7f3c912-b41e-4d8f-9c23-6e1a0b5d8f03"`. Callers that log or expose `executionId` externally should be aware of the format change.
- Full benchmark details recorded in `Benchmarks/RESULTS-OPTIMIZED.md`
