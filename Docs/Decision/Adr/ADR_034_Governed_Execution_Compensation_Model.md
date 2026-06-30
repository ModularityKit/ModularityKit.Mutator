# ADR-034: Governed Execution Compensation Model

## Tag
#adr_034

## Status
Accepted

## Date
2026-06-30

## Scope
ModularityKit.Mutator.Governance

## Context

ADR-027 established governed execution as the point where approved governance requests enter the core mutation engine.

That execution path already persisted terminal request outcomes and already carried audit and history metadata, but it still lacked a first-class way to represent compensation:

- `MutationIntent.IsReversible` existed only as intent metadata
- rollback or corrective compensation had no durable request-level model
- original and compensating executions were not explicitly linked
- audit and history could show that a mutation happened, but not that a later governed execution compensated for it

Without an explicit compensation model, governed execution would treat rollback and forward correction as informal follow-up work. That is not sufficient once governance requests, request history, and durable execution outcomes become part of the public platform model.

## Decision

Governed compensation should be modeled as a first-class governed execution concern.

The model should introduce:

- explicit compensation plan contracts
- explicit execution classification for standard and compensating governed executions
- explicit links between original and compensating request records
- explicit request decision history for compensation behavior
- explicit audit and history metadata that preserves compensation semantics

Implemented shape:

- `GovernedCompensationPlan` describes the original request being compensated together with compensation kind, trigger, optional batch identity, and related request identifiers
- `GovernedCompensationKind` distinguishes restoration-style rollback from forward corrective compensation
- `GovernedCompensationTrigger` distinguishes operator-driven rollback from batch or failed-execution initiated compensation
- `GovernedExecutionKind` classifies governed execution as standard execution or compensation
- `GovernedExecutionLink` and `GovernedExecutionLinkType` link governed requests through `Compensates` and `CompensatedBy`
- request-level execution metadata is grouped under `GovernedExecutionDetails`
- compensation requests are created through dedicated request factory flow rather than being mixed into the baseline request factory path
- original requests receive a `Compensated` lifecycle decision when a compensating request executes successfully

Runtime behavior:

- compensation remains governed execution, not a utility outside the governance lifecycle
- successful compensating execution updates the compensating request and then links it back to the original request
- audit and history metadata must carry both execution kind and compensation metadata so the compensating behavior remains visible after persistence

## Design Rationale

- Compensation is a governance concern because it changes how durable requests, decisions, and execution outcomes are interpreted over time.
- Explicit links are preferable to inference from tags or metadata because they survive storage, querying, and future provider implementations more cleanly.
- Rollback and forward correction are not the same operational action, so the model should distinguish them even if the first tested path focuses on rollback.
- A dedicated compensation request factory keeps baseline request creation simpler while allowing compensation-specific semantics to remain explicit.
- Audit and history already form part of the governed execution contract, so compensation semantics should extend those flows rather than introducing a parallel reporting mechanism.

## Consequences

### Positive

- Governed execution now has durable compensation semantics instead of relying on convention.
- Original and compensating requests can be traversed explicitly.
- Audit, history, and request decision flows preserve why and how compensation happened.
- The model supports operator-driven rollback immediately and leaves room for broader compensation planning later.

### Negative

- The governance request model becomes more complex because execution metadata now carries compensation and linking semantics.
- Runtime execution must perform an additional persistence step to link successful compensation back to the original request.
- Future storage providers and query models must preserve the new compensation and linking fields consistently.

## Non-Goals

- automatic generation of reverse mutations
- distributed saga orchestration
- full batch compensation engine
- replacing explicit domain modeling for inherently irreversible mutations

## Related ADRs

- ADR-020: Governance MutationRequest Model
- ADR-022: Governance Request Decisions and Storage
- ADR-023: Governance Versioned Request Resolution
- ADR-027: Governed Execution Manager
