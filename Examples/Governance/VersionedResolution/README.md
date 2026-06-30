# Governance VersionedResolution

This example shows how `MutationRequestVersionResolver` handles requests that were approved against an older state version and how the persisted runtime path stores the resulting decision.

It is the direct runnable example for the semantics introduced around `ExpectedStateVersion` and stale request handling.

## What it demonstrates

- resolving a request when the current state version still matches
- resolving stale requests with `RejectStale`
- resolving stale requests with `RequireRenewedApproval`
- resolving stale requests with `RevalidateOnLatestState`
- persisting a resolved outcome through `MutationRequestVersionResolutionManager`
- inspecting the resulting lifecycle state and appended decision history
- observing that revalidation is represented as `Pending` with `PendingMutationReason.Revalidation`

## Key files

- [`Program.cs`](Program.cs)
- [`Scenarios/GovernanceVersionedResolutionScenario.cs`](Scenarios/GovernanceVersionedResolutionScenario.cs)
- [`src/Governance/Runtime/Resolution/Execution/MutationRequestVersionResolver.cs`](../../../src/Governance/Runtime/Resolution/Execution/MutationRequestVersionResolver.cs)
- [`src/Governance/Runtime/Resolution/Execution/MutationRequestVersionResolutionManager.cs`](../../../src/Governance/Runtime/Resolution/Execution/MutationRequestVersionResolutionManager.cs)
- [`src/Governance/Abstractions/Resolution/Strategies/VersionedRequestResolutionStrategy.cs`](../../../src/Governance/Abstractions/Resolution/Strategies/VersionedRequestResolutionStrategy.cs)
- [`src/Governance/Abstractions/Resolution/Model/MutationRequestVersionResolution.cs`](../../../src/Governance/Abstractions/Resolution/Model/MutationRequestVersionResolution.cs)

## Run

```bash
dotnet run --project Examples/Governance/VersionedResolution/VersionedResolution.csproj
```

## Example usage

```csharp
var store = new InMemoryMutationRequestStore();
var resolver = new MutationRequestVersionResolver();
var manager = new MutationRequestVersionResolutionManager(store, resolver);

var request = await store.Create(
    MutationRequestFactory.Approved(
        stateId: "tenant-42:roles",
        stateType: "IamRoleState",
        mutationType: "GrantRoleMutation",
        intent: new MutationIntent
        {
            OperationName = "GrantRole",
            Category = "Security",
            Description = "Grant elevated role to tenant operator"
        },
        context: MutationContext.User("requester-1", "Requester One", "Need elevated access for incident"),
        expectedStateVersion: "v10"));

var resolution = await manager.ResolveAndStore(
    request.RequestId,
    currentStateVersion: "v15",
    resolutionContext: MutationContext.User("approver-5", "Approver Five", "Persist resolved request"),
    strategy: VersionedRequestResolutionStrategy.RejectStale);

Console.WriteLine(resolution.Outcome);
Console.WriteLine(resolution.Request.Lifecycle.Status);
Console.WriteLine(resolution.Request.Decisions[^1].Type);
```

## Expected output

The sample prints one block per resolution strategy and one persisted-resolution block. It shows:

- selected outcome
- whether the request was stale
- resulting request status
- the revalidation pending reason when the latest-state branch is selected
- updated expected version
- last decision recorded during resolution
- persisted request revision for the runtime path
