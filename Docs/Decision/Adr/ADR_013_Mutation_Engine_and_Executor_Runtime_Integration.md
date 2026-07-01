# ADR-013: Mutation Engine and Executor Runtime Integration

## Tag
#adr_013 
## Status
Accepted

## Date
2026-01-26

## Scope
ModularityKit.Mutators.Runtime  
ModularityKit.Mutators.Abstractions.Engine  
ModularityKit.Mutators.Abstractions.Context

## Context

In ModularityKit, mutations have two main context layers:
1. **MutationContext (#adr_002)** – semantic and audit context for a mutation.
2. **ExecutionContext (#adr_011)** – runtime context for a single mutation execution.

The `MutationEngine` and `MutationExecutor` must use both contexts consistently and deterministically:

- The engine is responsible for the mutation pipeline, including:
    - policy evaluation
    - interceptor pipeline
    - validation
    - auditing
    - mutation history
    - metrics
- The executor is responsible **only** for deterministic state transformation (`Apply`) and respecting timeouts and cancellation from `ExecutionContext`.

Separating these layers is critical to avoid mixing concerns and to maintain audit consistency and testability.

## Decision

### MutationEngine

- Creates **new ExecutionContext** for each mutation (ephemeral).
- Retrieves and uses the **MutationContext** from the mutation (`IMutation<TState>.Context`) for auditing and interceptors.
- Pipeline flow:
    1. `OnBeforeMutationAsync` (interceptor pipeline)
    2. Policy evaluation
    3. Validation (if mode is `Commit` or `_options.AlwaysValidate`)
    4. Mutation execution (simulate, validate, commit)
    5. `OnAfterMutationAsync` (interceptors)
    6. Audit and history storage
    7. Metrics registration
- Batch execution aggregates results and changes, optionally stopping on first failure if `_options.StopBatchOnFirstFailure` is set.

### MutationExecutor

- Low level executor that executes `IMutation<TState>.Apply` inline.
- Respects timeouts (`ExecutionContext.Timeout`) and cancellation token.
- Does **not** perform policies, auditing, or interceptors.
- Exceptions:
    - `ExecutionTimeoutException` when the timeout is exceeded
    - `OperationCanceledException` when cancelled

### Design Rationale

- Clean separation of the mutation pipeline from deterministic execution.
- Allows multiple, safe, and repeatable executions of the same mutation with different `ExecutionContext` instances.
- Maintains full auditability and compliance with ADR-002 (MutationContext) and ADR-011 (ExecutionContext).
- Minimal executor API with maximum engine flexibility.

### Related ADRs #adr_002 #adr_011

- ADR-002: Mutation Context and Actor Type
- ADR-011: Execution Context for Mutation Runtime

## Consequences

### Positive

- Strongly-typed, deterministic mutation pipeline
- Consistent auditing and validation
- Natural integration points for metrics, history, and interceptors
- Supports simulation and batch execution

### Negative

- Requires passing two contexts at runtime (`MutationContext` + `ExecutionContext`)
- Slight increase in the number of context types