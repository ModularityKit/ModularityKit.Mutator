# ADR-029: Governance Redis Provider Package

## Tag
#adr_029

## Status
Accepted

## Date
2026-06-25

## Scope
ModularityKit.Mutator.Governance.Redis

## Context

The governance package already defines:

- durable mutation requests
- optimistic concurrency around request revisions
- request, approval, and decision query contracts
- an in-memory implementation suitable for tests and examples

What remained open was a first persistence provider that can move governance state beyond in-memory storage without coupling `ModularityKit.Mutator.Governance` to a database-specific implementation.

The first provider needs to support:

- durable request document storage
- optimistic concurrency for request updates
- query-oriented reads over governed request data
- simple application integration through DI

At the same time, this should remain separate from the governance abstractions package so provider-specific concerns do not leak into the base runtime API.

## Decision

Introduce a dedicated package:

- `ModularityKit.Mutator.Governance.Redis`

The provider should:

- implement `IMutationRequestStore`
- implement `IMutationRequestQueryStore`
- register through dedicated DI extensions
- keep all Redis-specific logic inside the provider package
- reuse governance query contracts instead of inventing a Redis-specific query surface

The provider should remain additive:

- `ModularityKit.Mutator.Governance` owns contracts and runtime semantics
- `ModularityKit.Mutator.Governance.Redis` owns Redis persistence and Redis-specific internal read/write mechanics

## Design Rationale

- Governance contracts should stay provider-neutral.
- Redis is a pragmatic first persistence backend for request-oriented workflows with simple document state and indexable queue views.
- Package separation allows future providers such as EF Core or PostgreSQL without changing governance abstractions.
- DI registration gives applications a low-friction way to switch from in-memory storage to Redis-backed storage.

## Consequences

### Positive

- Governance gets a real persistence provider beyond in-memory storage.
- Redis-backed request and query implementations can evolve independently from governance contracts.
- Future providers now have a clear packaging precedent.
- Examples and tests can exercise a provider-backed read side without changing core governance APIs.

### Negative

- Provider package maintenance adds another surface to version and test.
- Redis-specific internal design decisions must now be documented and kept coherent over time.
- Query behavior remains contract-compatible but may have different performance characteristics than future relational providers.

## Related ADRs

- ADR-019: Governance Package Separation
- ADR-022: Governance Request Decisions and Storage
- ADR-026: Governance Request Query API
