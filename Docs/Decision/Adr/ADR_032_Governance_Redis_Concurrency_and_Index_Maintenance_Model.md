# ADR-032: Governance Redis Concurrency and Index Maintenance Model

## Tag
#adr_032

## Status
Accepted

## Date
2026-06-25

## Scope
ModularityKit.Mutator.Governance.Redis

## Context

The Redis provider must preserve governance request semantics during updates, especially:

- optimistic concurrency by request revision
- consistent request replacement on update
- index maintenance for queue and lookup reads

Redis does not provide relational constraints or a built-in document update model. That means the provider must explicitly define how request documents, revision values, and secondary indexes are updated together.

Without an explicit model, the provider risks:

- lost updates when two actors write the same request concurrently
- stale indexes that no longer match the latest request state
- inconsistent pending queues after status or reason changes

## Decision

The Redis provider uses optimistic concurrency around a per-request revision value and treats document persistence plus secondary-index maintenance as one provider-level update operation.

The provider should:

- require expected revision matching on request updates
- reject conflicting writes rather than silently overwrite newer request state
- maintain Redis secondary indexes whenever request status, state, or pending reason changes
- keep concurrency and index maintenance inside the write path, not in external repair code
- model indexes as derivations of the canonical request document, not as independent sources of truth

## Design Rationale

- Governance request updates already assume optimistic concurrency semantics, so Redis should preserve that contract.
- A canonical request document plus derived indexes is simpler than splitting state ownership across many Redis structures.
- Explicit index maintenance keeps common reads fast enough without changing governance query semantics.
- Rejecting conflicting writes is safer than last-write-wins for approval and lifecycle state.

## Consequences

### Positive

- Redis updates preserve governance revision semantics.
- Secondary indexes remain tied to the latest canonical request state.
- Pending queue views and status-based reads stay operationally useful.
- Future providers can compare their concurrency model against a documented Redis baseline.

### Negative

- Write paths are more complex than pure document replacement.
- Bugs in index maintenance can affect operational reads even when the canonical document is correct.
- Higher write volumes may require further batching or index strategy refinement later.

## Related ADRs

- ADR-021: Governance Pending Mutation Lifecycle
- ADR-022: Governance Request Decisions and Storage
- ADR-029: Governance Redis Provider Package
- ADR-030: Governance Redis Request Storage and Query Strategy
