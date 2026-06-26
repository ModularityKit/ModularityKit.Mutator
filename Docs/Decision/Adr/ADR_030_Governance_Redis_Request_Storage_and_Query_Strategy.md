# ADR-030: Governance Redis Request Storage and Query Strategy

## Tag
#adr_030

## Status
Accepted

## Date
2026-06-25

## Scope
ModularityKit.Mutator.Governance.Redis

## Context

Once the Redis provider package exists, it still needs a concrete storage and read strategy.

Governed request data has two competing needs:

- writes should stay simple and durable
- queue-oriented operational reads should avoid a full scan of every request whenever possible

The provider also needs to preserve governance runtime semantics such as:

- optimistic concurrency by request revision
- storage-agnostic request query filtering
- approval and decision projections built from parent request state

Without an explicit strategy, the Redis provider could drift into ad hoc key naming, inconsistent indexing, or duplicated query behavior that no longer matches the governance abstractions.

## Decision

The Redis provider stores one serialized request document per request and maintains a small set of Redis secondary indexes for common request-oriented reads.

Storage shape:

- one request JSON document per `MutationRequest`
- one revision key per request for optimistic concurrency
- set indexes for:
  - all request ids
  - requests by `StateId`
  - requests by `MutationRequestStatus`
  - all pending requests
  - pending requests by `PendingMutationReason`

Query shape:

- Redis index selection happens first through candidate-planning internals
- matching request documents are then loaded in bulk
- final filtering is applied through governance query evaluators, not Redis-specific ad hoc logic
- approval views and decision views are projected from loaded parent requests after candidate selection

Internal provider structure should remain decomposed:

- candidate planning and execution
- document key creation and payload loading
- document materialization
- read-side query orchestration

## Design Rationale

- Document-per-request storage maps naturally to the governance request model.
- Separate revision keys give a simple optimistic concurrency mechanism in Redis transactions.
- A small set of explicit secondary indexes improves the common queue and status reads without forcing the provider into a large custom indexing subsystem.
- Reusing governance evaluators keeps provider behavior aligned with in-memory and future providers.
- Internal decomposition makes Redis-specific read mechanics easier to evolve without turning one class into the entire provider.

## Consequences

### Positive

- Request writes stay simple and explicit.
- Common operational views such as pending queues and state/status slices can be narrowed through Redis sets.
- Query semantics remain aligned with governance abstractions because the final filter pass is evaluator-driven.
- Internal provider responsibilities are easier to test and evolve independently.

### Negative

- Broad ad hoc queries still fall back to loading candidate request documents and filtering in memory.
- Index maintenance increases write-path complexity compared to pure document storage.
- Additional indexes may be needed later for higher-volume provider scenarios.

## Related ADRs

- ADR-022: Governance Request Decisions and Storage
- ADR-026: Governance Request Query API
- ADR-029: Governance Redis Provider Package
