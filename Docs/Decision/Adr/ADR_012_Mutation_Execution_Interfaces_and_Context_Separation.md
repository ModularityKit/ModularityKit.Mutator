# ADR-012: Mutation Execution Interfaces and Context Separation

## Tag
#adr_012
## Status
Accepted

## Date
2026-01-22 / 2026-01-26

## Scope
ModularityKit.Mutators.Abstractions.Engine  
ModularityKit.Mutators.Abstractions.Context

## Context

In ModularityKit, mutations are the central mechanism for state changes. During the design of mutation engine interfaces, two main needs emerged:

1. **Separate contexts for audit and runtime**:
    - **MutationContext (ADR-002)** – semantic and audit context for mutations, storing information about the actor, reason, mode, and correlation.
    - **ExecutionContext (ADR-011)** – runtime context for a single mutation execution, handling timeouts, cancellation, and temporary runtime data.
2. **Minimal, deterministic mutation execution**:
    - The executor interface should be free of policies, validation, and logging.
    - `IMutation<TState>` defines the contract for a mutation (intent, validation, simulation, commit).

Separating these responsibilities enables:
- Repeatable, deterministic execution of the same mutation in different modes (`Simulate`, `Validate`, `Commit`)
- Clean separation of audit logic from runtime logic
- Easy testing and mocking of mutation execution

## Decision

### `IMutation<TState>`

- Represents mutation operating on state `TState`.
    
- Contains:
    - `MutationIntent Intent` – describes the change and its reason
    - `MutationContext Context` – linked to ADR-002, the audit context
    - Methods:
        - `MutationResult<TState> Apply(TState state)` – applies the mutation
        - `ValidationResult Validate(TState state)` – checks validity before execution
        - `MutationResult<TState> Simulate(TState state)` – simulates the mutation without changing state

### `IMutationExecutor`

- Low level executor responsible only for deterministic state transformation
- Uses `ExecutionContext` (ADR-011) for:
    - Unique execution identification (`ExecutionId`)
    - Timeout handling (`Timeout`, `IsTimedOut()`)
    - Cancellation (`CancellationToken`)
    - Temporary runtime data storage (`Data`)
- Does **not** enforce policies, validation, or auditing – these are delegated to the mutation engine (`IMutationEngine`)
- Defines the method:
```csharp
Task<MutationResult<TState>> ExecuteAsync<TState>(
    IMutation<TState> mutation,
    TState state,
    ExecutionContext context,
    CancellationToken cancellationToken = default);
```

### Design Rationale

- Clear separation of mutation semantics (`MutationContext`) from execution (`ExecutionContext`) improves consistency and testability
- Interfaces remain minimal and flexible – the executor is deterministic, and the mutation defines intent and validation
- Each mutation can be executed multiple times with different `ExecutionContext` instances without changing its semantics

### ## Related ADRs #adr_002 #adr_011 
- ADR-002: Mutation Context and Actor Type
- ADR-011: Execution Context for Mutation Runtime

## Consequences

### Positive

- Deterministic and safe mutation execution
- Clear separation of responsibilities between audit, business logic, and runtime
- Easy simulation and batch execution
- Minimal, readable API for executors

### Negative

- Requires passing two contexts (`MutationContext` + `ExecutionContext`)
- Slight increase in the number of context types in the system