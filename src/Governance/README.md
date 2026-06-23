# ModularityKit.Mutator.Governance

`ModularityKit.Mutator.Governance` is the governance focused extension layer for `ModularityKit.Mutator`.

The core package stays responsible for direct mutation execution. Governance builds on top of that runtime with request based lifecycle concepts such as deferred execution, approvals, and request storage.

## Features

- **Mutation Requests** - model governed mutation submission as a durable request
- **Pending Lifecycle** - represent requests that cannot execute immediately
- **Decision History** - record approvals, rejections, cancellations, and other lifecycle transitions
- **Approval Workflow** - model request-level approval requirements and explicit approver actions
- **Governed Execution** - execute approved requests through resolution and the core mutation engine
- **Request Storage Contracts** - define a persistence seam for governance-oriented stores
- **Runtime Lifecycle Management** - move requests through pending, approval, expiration, and execution transitions
- **In-Memory Runtime Support** - provide lightweight request runtime services for development and tests

## Governance Flow

The package is built around a request-driven governance loop:

1. create `MutationRequest`
2. move it through pending lifecycle states when direct execution is not allowed
3. collect approval decisions when approval is required
4. resolve the request against the current state version before execution
5. execute the underlying mutation through the core engine
6. persist the terminal governance outcome and execution metadata

The important point is that governance owns the request lifecycle around execution. The base `ModularityKit.Mutator` package still owns the mutation engine itself.

## Main Entry Points

Most consumers only need a small set of types.

### Request Model

- `MutationRequest`
- `MutationRequestFactory`
- `MutationRequestDecision`
- `MutationRequestStatus`
- `PendingMutationReason`

Use these to create and inspect governed requests.

### Storage

- `IMutationRequestStore`
- `InMemoryMutationRequestStore`

Use the store to persist requests and load them back into governance runtime services.

### Lifecycle

- `IMutationRequestLifecycleManager`
- `MutationRequestLifecycleManager`

Use lifecycle services to submit, pend, approve, reject, expire, supersede, cancel, and mark requests as executed.

### Approval

- `IMutationRequestApprovalWorkflowManager`
- `MutationRequestApprovalWorkflowManager`
- `MutationApprovalRequirement`

Use approval workflow services when a request must be explicitly approved by one or more actors before execution.

### Version Resolution

- `IMutationRequestVersionResolver`
- `IMutationRequestVersionResolutionManager`
- `MutationRequestVersionResolution`
- `MutationRequestVersionResolutionOutcome`
- `VersionedRequestResolutionStrategy`

Use resolution services to decide what happens when deferred request no longer matches the state version it was created against.

### Governed Execution

- `IGovernanceExecutionManager`
- `GovernanceExecutionManager`
- `GovernedExecutionResult<TState>`

Use governed execution to close the loop from approved request to core mutation execution and terminal governance state.

## Package Areas

The codebase is organized by governance concern rather than by framework layer alone.

### Requests

`Abstractions/Requests` contains the durable request model, decision taxonomy, and request factory methods.

- `Requests/Model`
- `Requests/Decisions`
- `Requests/Factory`

### Lifecycle

`Lifecycle` owns generic request movement between governance states such as pending, approved, rejected, expired, superseded, and executed.

- `Lifecycle/Contracts`
- `Lifecycle/Model`
- `Runtime/Lifecycle/Execution`
- `Runtime/Lifecycle/Validation`
- `Runtime/Lifecycle/State`

### Approval

`Approval` builds request-level approval workflow on top of the generic lifecycle model.

- `Approval/Contracts`
- `Approval/Model`
- `Approval/Mapping`
- `Runtime/Approval/Execution`
- `Runtime/Approval/State`

### Resolution

`Resolution` owns version-aware request handling before governed execution.

- `Resolution/Contracts`
- `Resolution/Model`
- `Resolution/Strategies`
- `Runtime/Resolution/Evaluation`
- `Runtime/Resolution/Execution`

### Execution

`Execution` owns the bridge from governance request semantics into the core mutation engine.

- `Execution/Contracts`
- `Execution/Model`
- `Runtime/Execution/Mutation`
- `Runtime/Execution/Orchestration`
- `Runtime/Execution/Outcome`
- `Runtime/Execution/Persistence`

### Storage and Exceptions

`Storage` defines persistence seams. `Exceptions` contains governance-specific failures grouped by concern.

- `Abstractions/Storage`
- `Abstractions/Exceptions/Approval`
- `Abstractions/Exceptions/Lifecycle`
- `Abstractions/Exceptions/Storage`

## What Exists Today

Today the package already provides:

- durable `MutationRequest` modeling
- request-level approval requirements
- optimistic concurrency in request storage
- explicit lifecycle transitions
- version-aware request resolution
- governed execution through the core mutation engine
- in-memory runtime support for examples and tests

What it does not try to do yet:

- persistence providers such as EF Core or PostgreSQL
- query stores for operational governance reporting
- compensation and retry orchestration
- external async approval or policy integrations

## Relationship to Core

### `ModularityKit.Mutator`

Responsible for:

- mutation execution
- policy evaluation
- audit and history basics
- side effects
- metrics and interception

### `ModularityKit.Mutator.Governance`

Responsible for:

- mutation request lifecycle
- pending execution modeling
- approval oriented governance contracts
- request decision history
- governance specific storage and future query seams

## Direction

This package is the place where broader governance behavior should grow without turning the core mutation engine into a workflow framework.

The near-term direction is:

- harden governed execution semantics
- add governance persistence and query providers
- expose governance metadata operationally
- support richer approval and integration scenarios

The goal is to keep the core runtime small and execution focused while letting governance evolve as an opt-in extension.
