# Governance RequestLifecycle

This example shows the first real runtime flow built on `ModularityKit.Mutator.Governance`.

It focuses on `MutationRequest`, `IMutationRequestStore`, and `MutationRequestLifecycleManager` rather than the core mutation engine itself.

## What it demonstrates

- creating governed requests with `MutationRequestFactory.Pending(...)` and `MutationRequestFactory.Approved(...)`
- storing requests in `InMemoryMutationRequestStore`
- moving requests through the lifecycle with `MutationRequestLifecycleManager`
- listing pending requests globally and by `StateId`
- explicit runtime paths for:
  - approval
  - cancellation
  - expiration sweep
  - external-check pending state
- inspecting `MutationRequestDecision` history after transitions

## Key files

- [`Program.cs`](Program.cs)
- [`Scenarios/GovernanceRequestLifecycleScenario.cs`](Scenarios/GovernanceRequestLifecycleScenario.cs)
- [`src/Governance/Abstractions/Requests/Model/MutationRequest.cs`](../../../src/Governance/Abstractions/Requests/Model/MutationRequest.cs)
- [`src/Governance/Abstractions/Lifecycle/IMutationRequestLifecycleManager.cs`](../../../src/Governance/Abstractions/Lifecycle/IMutationRequestLifecycleManager.cs)
- [`src/Governance/Runtime/MutationRequestLifecycleManager.cs`](../../../src/Governance/Runtime/MutationRequestLifecycleManager.cs)
- [`src/Governance/Runtime/InMemoryMutationRequestStore.cs`](../../../src/Governance/Runtime/InMemoryMutationRequestStore.cs)

## Run

```bash
dotnet run --project Examples/Governance/RequestLifecycle/RequestLifecycle.csproj
```

## Expected output

The sample prints:

- pending requests after submission
- pending requests filtered by state
- requests expired during the expiration sweep
- final lifecycle state and decision history for each request
