# Execution Model

This document describes the execution model that `ModularityKit.Mutator` is moving toward as governance features become first-class runtime capabilities.

It complements the roadmap by explaining the lifecycle of a mutation now that governance already has request lifecycle, approval workflow, and version-resolution primitives, but still needs a fully closed governed execution loop.

## Current Model

Today, the engine is centered around direct mutation execution:

1. Receive a mutation
2. Evaluate policies
3. Validate
4. Execute or block
5. Audit
6. Persist history

This model is strong for immediate execution flows, but it is intentionally narrow:

- blocked mutations are terminal outcomes
- direct mutation execution is still the dominant runtime path
- concurrency is not yet part of a first-class execution contract
- compensation and re-execution are not yet modeled as governed transitions

## Target Model

As governance features expand, execution becomes a lifecycle rather than a single pass.

The target shape is:

1. Create a mutation request
2. Evaluate policies and requirements
3. Enter pending state when execution is deferred
4. Resolve requirements through approvals or external checks
5. Resolve against the current state version
6. Execute through a governed execution manager
7. Emit side effects
8. Audit and persist history
9. Record executed or terminal governance decision
10. Optionally compensate or reverse

This is the key conceptual shift in the project:

- from direct mutation execution
- to governed mutation request execution

## Core Runtime Concepts

### Mutation Request

Represents a request to execute a mutation under governance.

Expected responsibilities:

- carry the original mutation intent and context
- keep a stable request identifier
- track creation time, current status, and owning state
- record required approvals or checks
- retain the state version or snapshot contract used for re-evaluation

### Pending Mutation

Represents a mutation request that cannot execute immediately.

Possible pending reasons:

- `PendingApproval`
- `PendingExternalCheck`
- `PendingSchedule`
- `PendingDependency`
- `PendingQuota`

Pending state is already a first-class governance state. The remaining work is to ensure every pending path has a clear re-entry route into governed execution.

### Resolution

A pending mutation must eventually transition through an explicit resolution path.

Possible outcomes:

- approved and executed
- rejected
- canceled
- expired
- superseded by a newer request or state version

Resolution is already modeled in the governance runtime. The remaining gap is making it part of one mandatory execution path rather than a helper that callers compose manually.

### Versioned Execution

Approval and deferred execution require explicit version handling.

When a request is approved against a later state than the one originally evaluated, the runtime must define one of the following behaviors:

- re-execute against the latest state after re-validation
- reject as stale
- require re-approval

This behavior must be explicit and consistent across all pending mutation types.

The main hardening point is no longer whether version resolution exists, but whether approved requests always pass through it before execution and whether revalidation has its own explicit runtime semantics.

### Governed Execution Manager

Once approvals and version resolution exist, the runtime needs one orchestrator that closes the loop.

Expected responsibilities:

- load an approved request
- perform mandatory version resolution
- choose execute / reject / re-approve / revalidate paths
- invoke the core mutation engine when execution is still valid
- record resulting state version and execution outcome
- persist terminal governance decisions such as `Executed`

Without this step, governance remains a set of useful runtime pieces rather than one coherent execution contract.

### Compensation

Once the engine supports governed execution over time, compensation becomes part of the execution model rather than a simple utility.

Compensation should describe:

- what original execution it is linked to
- whether it is automatic or operator-triggered
- whether it restores prior state or applies a forward corrective mutation

## Why This Needs Its Own Document

The roadmap explains priorities and release grouping.

The execution model explains semantics.

That distinction matters because several features are tightly coupled:

- pending mutation lifecycle
- approval workflow
- versioned execution
- concurrency control
- undo and compensation

Without an explicit execution model, these features risk being implemented as isolated additions instead of one coherent runtime contract.

## Design Pressure Points

These are the architectural questions that should stay visible as implementation continues:

- Is the primary unit of governance a mutation or a mutation request?
- What state version contract is required for deferred execution?
- When does a pending mutation become stale?
- Can approvals survive state drift, or must they be renewed?
- What is the exact contract of revalidation before execution?
- Are side effects emitted on request creation, on execution, or both?
- How are compensation flows represented in audit and history?
- Where does core mutation execution concurrency end and governance request concurrency begin?

## Relationship to the Roadmap

- `v1.1 Governance Runtime Hardening` closes the governed execution loop and hardens approval, version-resolution, and core concurrency semantics.
- `v1.2 Governance Data` adds persistence, queryability, metadata handling, and typed side effects around that runtime model.
- `v1.3 Integration` expands the model to async policy evaluation and external governance dependencies.
- `v2.0 Governance Platform` extends the lifecycle with compensation and richer policy composition.
