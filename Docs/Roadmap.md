# Roadmap

This document captures the most valuable next steps for `ModularityKit.Mutator` based on the current state of the API, runtime, and examples.

The goal is not to add features for their own sake. The goal is to close gaps between the public model and the runtime behavior, then complete the governed execution loop and extend the engine into a more operational governance platform for state mutations.

## Principles

- Prefer features that complete existing abstractions over adding parallel APIs.
- Prioritize runtime behavior that the public API already implies.
- Keep new capabilities observable through audit, history, and examples.
- Add focused tests alongside each roadmap item before broadening the surface further.

## Current Gaps

These areas already exist in the model but are only partially realized in the runtime:

- Governance now has request lifecycle, approval workflow, and version resolution primitives, but the end-to-end execution loop is still incomplete.
- `MutationIntent.IsReversible` exists, but there is no undo or compensation mechanism.
- `MutationEngineOptions.MaxConcurrentMutations` exists, but concurrency control is not enforced in core mutation execution.
- `MutationIntent.EstimatedBlastRadius`, `Tags`, and `Metadata` exist, but the engine does not yet use them for governance or query workflows.
- Approved governance requests do not yet flow through a single runtime path that performs version resolution, mutation execution, audit/history persistence, and terminal executed-state recording.

## v1.1 Governance Runtime Hardening

Focus: close the governed execution loop and harden the current governance runtime rather than introducing the first lifecycle concepts.

### 1. Governed Execution Manager

Add a first-class runtime path that executes approved governance requests end to end.

Scope:

- Introduce a `GovernanceExecutionManager` or equivalent orchestration runtime for approved requests.
- Always perform version resolution before executing an approved request.
- Execute the underlying mutation through the core runtime once the request is still valid.
- Record execution outcome, resulting state version, and terminal request decision.
- Mark requests as `Executed`, rejected as stale, or sent back into a renewed approval / revalidation path.
- Ensure audit and history flows capture both request-level and execution-level outcomes.

Why this matters:

- Governance runtime already knows how to hold, approve, reject, expire, and version-check requests.
- The biggest missing behavior is the actual transition from approved request to governed mutation execution.

### 2. Approval Workflow Hardening

Harden the approval model now that first-class approval workflow exists.

Scope:

- Replace generic approval failure paths with explicit domain exceptions and outcomes where they still leak through.
- Add approval groups, role-oriented approver targeting, and richer requirement assignment semantics.
- Support quorum or `N-of-M` approvals rather than only linear step completion.
- Add timeout / expiration policies that are specific to approval requirements, not only request-level pending expiration.
- Introduce a richer rejection reason model for auditability and later query support.

Why this matters:

- Approval workflow is no longer hypothetical.
- The next investment is making it operationally expressive rather than merely present.

### 3. Version Resolution Semantics Hardening

Harden the stale-request and revalidation semantics that now exist in the governance runtime.

Scope:

- Clarify what `RevalidateOnLatestState` means operationally before execution starts.
- Consider an explicit pending reason or status path for revalidation-driven deferral.
- Add end-to-end scenarios such as:
  - approve stale request
  - require renewed approval
  - approve again
  - execute
- Ensure stale handling semantics are visible through request decisions, examples, and tests.

Why this matters:

- Version resolution exists, but its branch semantics still need to be tightened before governed execution becomes a durable contract.

### 4. Core Runtime Concurrency

Close the remaining concurrency gap in the core mutation engine itself.

Scope:

- Enforce `MutationEngineOptions.MaxConcurrentMutations` in core execution rather than leaving it as a passive option.
- Introduce explicit concurrency handling around direct state mutation execution.
- Decide whether per-state locking, optimistic execution, or both are part of the core runtime contract.
- Define how direct batch execution behaves when concurrency conflicts appear mid-flight.

Why this matters:

- Recent work hardened governance request storage concurrency.
- The underlying core execution runtime still has its own unresolved concurrency contract.

## v1.2 Governance Data

Focus: make governed execution queryable, persistent, and classification-aware.

### 1. Governance Query Store

Expose operational queries over requests, approvals, and decisions.

Scope:

- Introduce an `IMutationRequestQueryStore` or equivalent query contract.
- Query pending approvals by actor, group, role, or request category.
- Query requests by `stateId`, request category, tags, governance metadata, and blast radius.
- Query recent request decisions, approval actions, stale resolutions, and execution outcomes.
- Define storage-agnostic query contracts before binding them to a specific provider.

Why this matters:

- Governance data becomes operational once users can ask workflow questions, not just load a single request by id.

### 2. Persistent Governance and Audit Providers

Add production-ready persistence adapters beyond in-memory implementations.

Scope:

- Entity Framework Core provider for governance request storage and query flows.
- PostgreSQL-oriented governance provider package.
- Persistent audit/history provider where governance execution outcomes can be correlated with request data.
- Optional Redis-backed recent-decision cache for hot operational paths if it proves necessary.

Why this matters:

- Governance runtime without persistence remains development-friendly but operationally shallow.

### 3. Governance Metadata

Turn intent classification fields into runtime-usable governance data.

Scope:

- Persist `Tags`, `Metadata`, and `EstimatedBlastRadius` into audit and history records.
- Expose them through query APIs.
- Enable policy decisions and reporting based on governance metadata.

Why this matters:

- These fields already exist in the public model.
- The roadmap should explicitly close the gap instead of only calling it out in `Current Gaps`.

### 4. Typed Side Effects

Evolve side effects beyond `object? Data`.

Scope:

- Add a typed side effect variant such as `SideEffect<TData>`, or
- Add serialization and registration contracts for side effect payloads.

Why this matters:

- Persistence and queryability make side effect payload contracts much more important.
- This is the point where side effects stop being just runtime output and become integration data.

## v1.3 Integration

Focus: connect governance runtime behaviors to external systems and asynchronous policy sources.

### 1. Async Policies and External Governance Checks

Support policy evaluation that depends on external systems.

Scope:

- Introduce `EvaluateAsync(...)` policy support.
- Preserve a sync path for lightweight policies.
- Define ordering and timeout semantics when multiple async policies are involved.
- Allow approval and governance checks to rely on external identity, ticketing, quota, or compliance systems.
- Support ticket-driven approval flows, external quota checks, and identity-provider backed approver resolution.
- Define timeout and cancellation semantics for external governance dependencies.

Why this matters:

- Real approval and compliance checks often depend on external identity, ticketing, quota, or feature control systems.

## v2.0 Governance Platform

Focus: make the engine a complete platform for governed mutations.

### 1. Undo and Compensation

Add explicit reversal and compensation support for reversible mutations.

Scope:

- Introduce a reversible mutation contract or compensation contract.
- Record links between original execution and compensating execution.
- Support compensation plans for failed batches and operator-driven rollback flows.

Why this matters:

- `MutationIntent.IsReversible` already exists.
- This is one of the biggest jumps in real operational value for change governance.

### 2. Governance-Aware Policy Composition

Composition primitives for complex policy sets are now available in the core policy abstractions.

Scope:

- `AllOf`, `AnyOf`, and priority-based composition.
- Merging rules for severity, requirements, side effects, and metadata.
- Clear conflict rules when multiple policies modify the same mutation result.

Why this matters:

- Today, policy complexity is pushed into handwritten policy classes.
- Composition will make complex governance easier to express and reuse.

## Cross-Cutting Work

These tasks should accompany every milestone:

- Improve README accuracy where examples still drift from the current package and namespace layout.
- Keep `Examples/` aligned with newly added runtime features.
- Extend `Benchmarks/` when new runtime behavior could affect hot paths.
- Maintain comprehensive test coverage for all runtime behaviors.
- Add regression tests for every bug fix.
- Require tests for all new governance features.
- Add ADRs for every public or behavioral change that alters execution semantics.

## Execution Model

The longer-term lifecycle model behind these milestones is documented in [`Docs/ExecutionModel.md`](ExecutionModel.md).

## Recommended Build Order

1. Add a governed execution manager that resolves and executes approved requests.
2. Harden approval workflow semantics around quorum, roles, and rejection modeling.
3. Tighten version-resolution semantics and revalidation paths.
4. Close remaining core runtime concurrency gaps.
5. Add governance query contracts for requests, approvals, and decisions.
6. Add persistent governance and audit/history providers.
7. Persist and expose governance metadata.
8. Add typed side effects once persistence and query contracts are stable.
9. Add async policy support, unless external approval integrations force it earlier.
10. Add undo/compensation support once governed execution and persistence semantics are stable.

## Not Recommended Yet

These ideas may be useful later, but they are not the best next investment:

- Distributed execution features before concurrency semantics are explicit.
- More examples before the governed execution loop is complete.
