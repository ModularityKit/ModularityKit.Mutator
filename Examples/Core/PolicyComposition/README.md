# PolicyComposition

This example shows how to compose multiple governance policies into reusable sets.

It is the sample to read when you want to see `AllOf`, `AnyOf`, and priority-based composition
without pushing the rules into one large handwritten policy class.

## Domain

The sample uses a simple release gate model:

- a release has a name, stage, and owner
- policy metadata carries approvals, environment, and emergency flags
- composed policies decide whether the release stays blocked, becomes approved, or gets an emergency path

## What this example demonstrates

- reusable composed policy sets
- explicit merge rules for severity, requirements, side effects, and metadata
- `AllOf`, `AnyOf`, and priority-based composition
- deterministic conflict handling when two policies modify the same value

## Project structure

- [`Program.cs`](Program.cs)
- [`PolicyComposition.csproj`](PolicyComposition.csproj)
- [`State/ReleaseGateState.cs`](State/ReleaseGateState.cs)
- [`Mutations/SubmitReleaseMutation.cs`](Mutations/SubmitReleaseMutation.cs)
- [`Policies/ReleaseGovernancePolicies.cs`](Policies/ReleaseGovernancePolicies.cs)
- [`Policies/Approval/RequireApprovalsPolicy.cs`](Policies/Approval/RequireApprovalsPolicy.cs)
- [`Policies/Approval/AddAuditTrailPolicy.cs`](Policies/Approval/AddAuditTrailPolicy.cs)
- [`Policies/Emergency/EmergencyOverridePolicy.cs`](Policies/Emergency/EmergencyOverridePolicy.cs)
- [`Policies/Emergency/ApprovalFallbackPolicy.cs`](Policies/Emergency/ApprovalFallbackPolicy.cs)
- [`Policies/Deployment/ProductionGuardPolicy.cs`](Policies/Deployment/ProductionGuardPolicy.cs)
- [`Policies/Deployment/DefaultDeploymentPolicy.cs`](Policies/Deployment/DefaultDeploymentPolicy.cs)
- [`Policies/Shared/SetOwnerPolicy.cs`](Policies/Shared/SetOwnerPolicy.cs)
- [`Scenarios/AllOfScenario.cs`](Scenarios/AllOfScenario.cs)
- [`Scenarios/AnyOfScenario.cs`](Scenarios/AnyOfScenario.cs)
- [`Scenarios/PriorityScenario.cs`](Scenarios/PriorityScenario.cs)
- [`Scenarios/ConflictScenario.cs`](Scenarios/ConflictScenario.cs)

## Run

```bash
dotnet run --project Examples/Core/PolicyComposition/PolicyComposition.csproj
```

## Expected output

You should see:

- an `AllOf` release gate that merges state updates, metadata, and audit side effects
- an `AnyOf` gate that selects the first allowed branch
- a priority-based gate that prefers the first decisive policy
- a conflict example that raises `PolicyCompositionConflictException`
