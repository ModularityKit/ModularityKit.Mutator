# Governance Queries

This example shows the query-oriented read side of `ModularityKit.Mutator.Governance`.

It focuses on listing governed requests, approval work, and decision history without reconstructing those views manually from raw stored records.

## What it demonstrates

- querying governed requests with `MutationRequestQuery`
- filtering by intent tags, intent metadata, request metadata, and blast radius
- listing the pending approval queue through `IMutationRequestQueryStore`
- listing pending requests by `PendingMutationReason`
- querying requests by `StateId` and request category
- listing recent approval-driven requests
- projecting pending approval work with `MutationApprovalQuery`
- projecting recent decision history with `MutationRequestDecisionQuery`
- projecting recent execution outcomes separately from version resolution history
- using the in-memory governance store as both write-side storage and query-side read model

## Key files

- [`Program.cs`](Program.cs)
- [`Scenarios/GovernanceQueriesScenario.cs`](Scenarios/GovernanceQueriesScenario.cs)
- [`Scenarios/GovernanceQueriesSampleData.cs`](Scenarios/GovernanceQueriesSampleData.cs)
- [`Scenarios/RequestQueryScenario.cs`](Scenarios/RequestQueryScenario.cs)
- [`Scenarios/ApprovalQueryScenario.cs`](Scenarios/ApprovalQueryScenario.cs)
- [`Scenarios/DecisionQueryScenario.cs`](Scenarios/DecisionQueryScenario.cs)
- [`src/Governance/Abstractions/Queries/Contracts/IMutationRequestQueryStore.cs`](../../../src/Governance/Abstractions/Queries/Contracts/IMutationRequestQueryStore.cs)
- [`src/Governance/Abstractions/Queries/Model/Requests/MutationRequestQuery.cs`](../../../src/Governance/Abstractions/Queries/Model/Requests/MutationRequestQuery.cs)
- [`src/Governance/Abstractions/Queries/Model/Approvals/MutationApprovalQuery.cs`](../../../src/Governance/Abstractions/Queries/Model/Approvals/MutationApprovalQuery.cs)
- [`src/Governance/Abstractions/Queries/Model/Decisions/MutationRequestDecisionQuery.cs`](../../../src/Governance/Abstractions/Queries/Model/Decisions/MutationRequestDecisionQuery.cs)

## Run

```bash
dotnet run --project Examples/Governance/Queries/Queries.csproj
```

## Expected output

The sample prints:

- pending approval requests
- pending external-check requests
- requests filtered by request category
- requests filtered by state
- requests filtered by governance metadata
- recent approval-driven requests
- approval views filtered by approver
- recent version-resolution decisions
- recent execution outcomes
