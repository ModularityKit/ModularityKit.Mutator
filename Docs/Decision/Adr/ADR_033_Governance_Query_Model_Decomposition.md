# ADR-033: Governance Query Model Decomposition

## Tag
#adr_033

## Status
Accepted

## Date
2026-06-30

## Scope
ModularityKit.Mutator.Governance.Abstractions.Queries

## Context

ADR-026 established that governance needs storage agnostic query API, but it intentionally left the exact query object model open.

As the query surface grew, `MutationRequestQuery` started to accumulate several distinct concerns:

- request and state identity filters
- actor filters
- intent classification filters such as category, tags, metadata, and blast radius
- lifecycle and decision-history filters
- approval oriented and decision oriented projections adjacent to the same flat namespace

Without more explicit structure, the query model would keep growing as one wide record with loosely related fields. That creates several problems:

- the request query type becomes harder to read and evolve
- approval and decision query types remain mixed into the same flat model namespace
- future additions such as risk level or execution specific filters would further flatten unrelated concepts into one type

## Decision

The governance query abstractions should be decomposed into grouped models and organized by query concern.

Request query shape:

- `MutationRequestQuery` remains the root request query contract
- request query filters are grouped into explicit components:
  - `Scope`
  - `Actor`
  - `Intent`
  - `Metadata`
  - `Lifecycle`
  - `TimeRange`
- request query presets move into `MutationRequestQueries`

Namespace and folder organization:

- request query abstractions live under `...Queries.Model.Requests`
- request query filter types live under `...Queries.Model.Requests.Filters`
- approval query types live under `...Queries.Model.Approvals`
- decision query types live under `...Queries.Model.Decisions`

Evaluation semantics:

- query evaluators compose groupspecific matches rather than expanding one flat request query type indefinitely
- approval and decision query types continue to depend on the request query contract where needed, but remain organized as distinct concerns

## Design Rationale

- The request query contract stays explicit without becoming one ever growing bag of filters.
- Filter grouping makes new additions easier to place without weakening the model.
- Approval and decision queries are part of the same read side, but not the same concern as request filters, so they should not share one flat namespace.
- Moving presets out of `MutationRequestQuery` keeps the model closer to a DTO and avoids mixing data shape with convenience factories.

## Consequences

### Positive

- The query model is easier to scan and extend.
- Filter placement becomes more disciplined as the governance read side grows.
- Namespace organization better reflects actual query responsibilities.
- The query API stays storage agnostic while becoming more explicit internally.

### Negative

- Consumers need a small amount of additional object construction when building request queries.
- Existing code that referenced the old flat model or flat namespaces must be updated.
- Some changes that are semantically query related now span several small types instead of one file.

## Related ADRs

- ADR-026: Governance Request Query API
- ADR-030: Governance Redis Request Storage and Query Strategy
