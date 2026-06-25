# ADR-031: Governance Redis Serialization and Document Compatibility

## Tag
#adr_031

## Status
Accepted

## Date
2026-06-25

## Scope
ModularityKit.Mutator.Governance.Redis

## Context

The Redis provider persists governed mutation requests as serialized documents.

That creates a compatibility boundary:

- request data must round-trip without losing governance semantics
- read models must tolerate heterogeneous metadata values
- provider internals must avoid a Redis-specific domain model fork
- future package versions should evolve the serialized shape deliberately, not accidentally

The governance request model already contains nested structures such as:

- mutation intent
- request metadata
- approval requirements
- decision history
- version resolution state

Without an explicit serialization decision, the provider could drift into:

- inconsistent JSON shapes across components
- fragile metadata handling for object-valued entries
- hidden coupling between runtime classes and Redis payload format

## Decision

The Redis provider serializes the existing governance request model directly as JSON documents and treats that JSON shape as the persisted document contract for the provider.

The provider should:

- serialize full `MutationRequest` documents rather than introduce a parallel storage DTO graph
- keep provider serialization centralized in dedicated Redis serialization components
- support heterogeneous metadata values through explicit converter handling
- keep document materialization inside provider internals, not spread across query code paths
- evolve document shape through deliberate package changes backed by ADRs when compatibility semantics change

## Design Rationale

- Reusing the governance model avoids translation layers that would duplicate request, approval, and decision semantics.
- Centralized serialization keeps Redis persistence mechanics consistent across reads and writes.
- Explicit converter support is necessary because governance metadata is intentionally flexible and may contain inferred object values.
- A single persisted document contract makes provider behavior easier to reason about in tests, examples, and future migrations.

## Consequences

### Positive

- Redis persistence stays aligned with governance runtime semantics.
- Serialization behavior is easier to test because all provider paths use the same document contract.
- Metadata and nested governance structures can round-trip without ad hoc per-query parsing.
- Future compatibility changes now have an explicit decision boundary.

### Negative

- The provider is coupled to the serialized shape of the governance request model.
- Backward-compatible evolution requires discipline when changing serialized request members.
- JSON document size grows with request history and approval detail.

## Related ADRs

- ADR-022: Governance Request Decisions and Storage
- ADR-029: Governance Redis Provider Package
- ADR-030: Governance Redis Request Storage and Query Strategy
