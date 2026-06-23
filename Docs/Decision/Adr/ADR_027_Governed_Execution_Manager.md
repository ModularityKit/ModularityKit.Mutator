# ADR-027: Governed Execution Manager

## Tag
#adr_027

## Status
Accepted

## Date
2026-06-24

## Scope
ModularityKit.Mutator.Governance

## Context

The governance package already models:

- durable mutation requests
- pending request lifecycle
- approval workflow
- version-aware request resolution

What remained open was the final governed execution loop:

- load an approved request
- resolve it against the current state version
- execute the underlying core mutation only when resolution permits it
- persist the terminal governance outcome
- record the resulting state version for future audit and replay

Without an explicit runtime service for that flow, callers would have to manually compose:

- request loading
- resolution
- mutation engine invocation
- request status persistence
- execution decision history

That would make governed execution easy to implement inconsistently across applications.

## Decision

The governance package introduces a dedicated governed execution runtime centered on `IGovernanceExecutionManager`.

The governed execution flow is:

1. load and resolve an approved request through governance version resolution
2. stop early when resolution yields a non-executable outcome such as:
   - `RejectedAsStale`
   - `RequiresRenewedApproval`
3. execute the wrapped core mutation through `IMutationEngine` only when resolution allows execution
4. persist the terminal governance request outcome:
   - `Executed` on successful mutation execution
   - `Rejected` on failed execution or execution exception
5. record governance metadata such as:
   - resulting state version
   - executed timestamp
   - request correlation metadata flowing into core audit/history

The runtime is intentionally split by responsibility:

- `GovernanceExecutionManager` orchestrates the end-to-end flow
- `GovernedMutation` carries governance request identifiers into core execution metadata
- `GovernedExecutionOutcomeHandler` maps execution outcomes into terminal request state
- `GovernedExecutionDecisionFactory` creates execution-related governance decisions
- `GovernedExecutionRequestPersistence` performs guarded request persistence with optimistic concurrency

## Design Rationale

- Governed execution is governance concern, not responsibility of the base mutation engine.
- Version resolution must always happen before governed execution proceeds.
- Terminal request persistence must be handled consistently and with optimistic concurrency checks.
- The execution runtime should remain decomposed so orchestration, persistence, and decision mapping can evolve independently.

## Consequences

### Positive

- Governance now closes the loop from approved request to terminal execution outcome.
- Version drift handling is enforced before core execution starts.
- Core audit/history can be correlated with governance request identifiers.
- Execution state is recorded explicitly through `ExecutedAt` and `ResultingStateVersion`.

### Negative

- Governance runtime now owns a larger orchestration surface.
- Execution failures currently collapse into terminal rejection and may require richer taxonomy later.
- Future work is still needed for:
  - governed execution retries
  - compensation semantics
  - persistent queryable execution history across providers

## Related ADRs

- ADR-020: Governance MutationRequest Model
- ADR-023: Governance Versioned Request Resolution
- ADR-024: Governance Runtime Pending Request Handling
- ADR-025: Governance Approval Workflow
