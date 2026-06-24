# ADR-028: Governance Approval Workflow Hardening

## Tag
#adr_028

## Status
Accepted

## Date
2026-06-24

## Scope
ModularityKit.Mutator.Governance

## Context

The governance package already implements the first approval workflow:

- request-level approval requirements
- ordered step execution
- explicit approve and reject actions
- approval history recorded through `MutationRequestDecision`

That first slice is functional, but it is too narrow for operational governance scenarios.

The main gaps are:

- approval targets are mostly modeled as individual actors
- grouped approvals and quorum semantics are not first-class
- approval expiration is not modeled independently from generic request expiration
- rejection is recorded, but the business reason model is too thin
- some failure paths still collapse into generic invalid-operation behavior

Without hardening, governance approval would remain present but not expressive enough for real multi-actor approval workflows.

## Decision

The governance package hardens approval workflow semantics in the following way:

- approval requirements may target:
  - a concrete approver id
  - an approver role
  - an approver group
- approval requirements may participate in approval groups with explicit quorum semantics such as `N-of-M`
- approval requirements may expire independently through approval-specific expiration timestamps
- rejection may carry a structured `MutationApprovalRejectionReason`
- approval-specific invalid states and invalid configurations should raise domain-specific approval exceptions

The request-centric governance model remains unchanged:

- approvals are still attached to a `MutationRequest`
- approval actions still resolve request-level approval requirements
- approval outcomes still become request decision history

Quorum behavior is modeled explicitly:

- once a grouped approval reaches the required quorum
- remaining pending approvals in that group become `Satisfied`
- request history records a `QuorumSatisfied` approval decision

Expiration behavior is also explicit:

- expired approval requirements become `Expired`
- expired approval sets reject the request through governance runtime
- request history records approval expiration and terminal request rejection

## Design Rationale

- Governance approvals must support real organizational approval patterns, not only linear actor-by-actor sign-off.
- Role and group targeting fits the existing `MutationContext` model without introducing a separate identity subsystem.
- Quorum semantics belong in governance approval, not in the core mutation engine.
- Structured rejection reasons make approval denial auditable and machine-readable.
- Approval expiration should be explicit because an approval timeout is not the same business event as generic request expiration.

## Consequences

### Positive

- Approval workflow now supports richer operational behavior without leaving the request-centric governance model.
- Multi-actor and grouped approvals become first-class.
- Approval denial and expiration become more auditable.
- Governance runtime can represent approval completion without requiring every approver in a quorum set to act.

### Negative

- Approval model complexity increases.
- Approval targeting now depends on agreed metadata conventions for actor roles and groups.
- Future persistence providers will need to store richer approval state and rejection metadata.

## Related ADRs

- ADR-025: Governance Approval Workflow
- ADR-027: Governed Execution Manager
