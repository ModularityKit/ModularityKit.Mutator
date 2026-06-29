
# API Reference

Complete API documentation for **ModularityKit.Mutators**.

---

## Table of Contents

- [Interfaces](#interfaces)
  - [IMutation](#imutation)
  - [IMutationPolicy](#imutationpolicy)
- [Base Classes](#base-classes)
  - [MutationInterceptorBase](#mutationinterceptorbase)
- [Related Types](#related-types)
- [DI Extension Methods](#di-extension-methods)
  - [AddMutators](#addmutators)
- [Governance API](#governance-api)
- [Execution Semantics](#execution-semantics)
- [Best Practices](#best-practices)
- [Complete Examples](#complete-examples)

---

## Interfaces

### IMutation

Represents a mutation operation on a state of type `TState`.

**Namespace:** `ModularityKit.Mutators.Abstractions.Engine`

```csharp
public interface IMutation<TState>
{
    MutationIntent Intent { get; }
    MutationContext Context { get; }

    MutationResult<TState> Apply(TState state);
    ValidationResult Validate(TState state);
    MutationResult<TState> Simulate(TState state);
}
```

**Type Parameters**

| Parameter | Description                                |
| --------- | ------------------------------------------ |
| `TState`  | Type of the state the mutation operates on |

**Properties**

| Property  | Type              | Description                                             |
| --------- | ----------------- | ------------------------------------------------------- |
| `Intent`  | `MutationIntent`  | Declarative description of the change (what & why)      |
| `Context` | `MutationContext` | Metadata about execution (who, when, correlation, mode) |

**Methods**

|Method|Returns|Description|
|---|---|---|
|`Apply`|`MutationResult<TState>`|Applies the mutation, producing new state|
|`Validate`|`ValidationResult`|Checks preconditions without mutating|
|`Simulate`|`MutationResult<TState>`|Dry-run, behaves like `Apply` but without persistence|

**Semantics**

- `Apply` must be deterministic given `(state, intent, context)`
- `Validate` must **never** mutate state or cause side-effects
- `Simulate` must match `Apply` behavior, except no persistence

---

### IMutationPolicy

Represents governance rule that can allow or block mutation.

**Namespace:** `ModularityKit.Mutators.Abstractions.Policies`

```csharp
public interface IMutationPolicy<TState>
{
    string Name { get; }
    int Priority { get; }
    string? Description { get; }

    PolicyDecision Evaluate(IMutation<TState> mutation, TState state);
}
```

**Type Parameters**

|Parameter|Description|
|---|---|
|`TState`|State type the policy evaluates|

**Properties**

|Property|Type|Description|
|---|---|---|
|`Name`|`string`|Logical policy name|
|`Priority`|`int`|Evaluation order (higher runs first)|
|`Description`|`string?`|Optional description|

**Method**

|Method|Returns|Description|
|---|---|---|
|`Evaluate`|`PolicyDecision`|Determines if mutation is allowed|

**Semantics**

- Policies **must be deterministic and side effect free**
- Evaluated before mutation application
- Higher `Priority` runs earlier
- Any blocking decision cancels mutation

---

## Base Classes

### MutationInterceptorBase

Base class for interceptors with default no-op implementations.

**Namespace:** `ModularityKit.Mutators.Runtime.Interception`

```csharp
public abstract class MutationInterceptorBase : IMutationInterceptor
{
    public virtual string Name { get; }
    public virtual int Order { get; }

    protected internal virtual bool ShouldRun(MutationIntent intent, MutationContext context);

    public virtual Task OnBeforeMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        string executionId,
        CancellationToken cancellationToken = default);

    public virtual Task OnAfterMutationAsync(
        MutationIntent intent,
        MutationContext context,
        object? oldState,
        object? newState,
        ChangeSet changes,
        string executionId,
        CancellationToken cancellationToken = default);

    public virtual Task OnMutationFailedAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        Exception exception,
        string executionId,
        CancellationToken cancellationToken = default);

    public virtual Task OnPolicyBlockedAsync(
        MutationIntent intent,
        MutationContext context,
        object state,
        PolicyDecision decision,
        string executionId,
        CancellationToken cancellationToken = default);
}
```

**Properties & Methods**

| Member                  | Description                                           |
| ----------------------- | ----------------------------------------------------- |
| `Name`                  | Logical interceptor name (default: type name)         |
| `Order`                 | Execution order (low → high)                          |
| `ShouldRun`             | Determines if interceptor executes for given mutation |
| `OnBeforeMutationAsync` | Hook before mutation                                  |
| `OnAfterMutationAsync`  | Hook after successful mutation                        |
| `OnMutationFailedAsync` | Hook on exception                                     |
| `OnPolicyBlockedAsync`  | Hook when mutation is blocked by policy               |

**Ordering Example**
```
Order -100 → Infrastructure
Order 0    → Default
Order 100  → Diagnostics / Logging
```

---

## Related Types

- `MutationIntent` — Declarative description of the change
- `MutationContext` — Metadata about execution
- `MutationResult<TState>` — Result of mutation application
- `ValidationResult` — Result of `Validate`
- `ChangeSet` — Detailed changes applied
- `PolicyDecision` — Allow/Block decision from policy

---

## DI Extension Methods

### AddMutators

Registers Mutators framework in DI.

**Namespace:** `ModularityKit.Mutators.Extensions`
```csharp
void AddMutators(
    this IServiceCollection services,
    MutationEngineOptions? presetOptions = null,
    Action<MutationEngineOptions>? configure = null,
    bool addDefaultLoggingInterceptor = false)
```

**Parameters**

|Parameter|Description|
|---|---|
|`services`|Target `IServiceCollection`|
|`presetOptions`|Optional base configuration|
|`configure`|Optional delegate for further configuration|
|`addDefaultLoggingInterceptor`|If `true`, registers default logging interceptor|

**Registered Services**

|Service|Implementation|Lifetime|
|---|---|---|
|`IMutationExecutor`|`MutationExecutor`|Singleton|
|`IPolicyRegistry`|`PolicyRegistry`|Singleton|
|`IInterceptorPipeline`|`InterceptorPipeline`|Singleton|
|`IMutationAuditor`|`InMemoryAuditor`|Singleton|
|`IMutationHistoryStore`|`InMemoryHistoryStore`|Singleton|
|`IMetricsCollector`|`MetricsCollectorImpl`|Singleton|
|`IMutationEngine`|`MutationEngine`|Singleton|

**Example**
```csharp
services.AddMutators(MutationEngineOptions.Performance, addDefaultLoggingInterceptor: true);
```

**or**
```csharp
services.AddMutators(MutationEngineOptions.Strict, addDefaultLoggingInterceptor: true);
```

---

## Governance API

Governance is documented separately so the core API reference can stay focused on direct mutation execution.

- [Governance package overview](../../src/Governance/README.md)
- [Governance API usage](API.md)
- [Redis provider API](Redis.md)

Governance-specific types such as `MutationRequestFactory`, `MutationRequestDecision`, and
`IGovernanceExecutionManager` live in the governance package, not the core mutation runtime.

---

## Execution Semantics

1. Call `Validate(state)`
2. Evaluate policies (`IMutationPolicy`)
3. Call interceptors `OnBeforeMutationAsync`
4. Apply mutation via `Apply(state)`
5. Call interceptors `OnAfterMutationAsync`
6. Emit results, metrics

**Failure Paths**

- Policy blocks → `OnPolicyBlockedAsync`
- Exception during apply → `OnMutationFailedAsync`

---

## Best Practices

- Mutations are **pure domain operations** (no I/O or side-effects)    
- Policies are **side-effect free**    
- Interceptors are **cross-cutting only**, never mutate state
- Use `Order` to explicitly control interceptor execution
- Prefer **many small interceptors** rather than a single large one

---

> **Built with ❤️ for .NET developers**
