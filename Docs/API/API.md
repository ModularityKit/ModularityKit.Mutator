# Governance API

This page shows the practical entry points used by the governance examples.

## What this API is for

Use the governance API when execution is not immediate and you need one of these steps first:

- request creation
- approval workflow
- stale-version resolution
- governed execution
- request and decision history queries

The core package owns direct mutation execution. Governance wraps that execution with a request
model and stateful decision history.

## Request creation

Use `MutationRequestFactory` when you want to create a request from explicit strings and values:

```csharp
var request = MutationRequestFactory.PendingApproval(
    stateId: "tenant-42:roles",
    stateType: "IamRoleState",
    mutationType: "GrantRoleMutation",
    intent: new MutationIntent
    {
        OperationName = "GrantRole",
        Category = "Security",
        Description = "Grant elevated role to tenant operator"
    },
    context: MutationContext.User("requester-1", "Requester One", "Incident escalation"),
    expectedStateVersion: "v10",
    approvalRequirements:
    [
        MutationApprovalRequirement.SingleActorStep("security-lead"),
        MutationApprovalRequirement.SingleActorStep("platform-owner")
    ]);
```

If you already have concrete CLR types, prefer the generic overloads such as:

- `MutationRequestFactory.Approved<TState, TMutation>()`
- `MutationRequestFactory.PendingApproval<TState, TMutation>()`

They remove repeated `stateType` and `mutationType` strings at the call site.

### Factory choices

- `Pending(...)` for a request that should enter the lifecycle without approval requirements
- `PendingApproval(...)` for a request that must collect one or more approval steps
- `Approved(...)` for a request that is terminally approved at creation time

Use the generic overloads when the CLR types already exist. Use the string-based overloads when the
request metadata comes from an external system or serialized payload.

## Decision history

Use `MutationRequestDecision` when you need to record a lifecycle, approval, or version-resolution decision and the category is already known:

```csharp
var decision = MutationRequestDecision.Lifecycle(
    MutationRequestLifecycleDecisionType.Approved,
    MutationContext.System("governance-runtime", "Approved by governance"));
```

The category-specific helpers are:

- `MutationRequestDecision.Lifecycle(...)`
- `MutationRequestDecision.Approval(...)`
- `MutationRequestDecision.VersionResolution(...)`

Use the low-level `MutationRequestDecision.Create(...)` only when the decision type is already dynamic and you do not know the category up front.

### Decision categories

- lifecycle decisions describe request state transitions such as submitted, pending, approved, rejected, executed, canceled, or expired
- approval decisions describe workflow-level approval actions such as requested, approved, rejected, or quorum satisfied
- version-resolution decisions describe how the runtime handled stale or matching versions before execution

The helper methods exist so call sites stay readable when the category is already known.

## Governed execution

Use `IGovernanceExecutionManager.ExecuteApproved(...)` when the runtime should resolve an approved request and execute the mutation:

```csharp
var execution = await executionManager.ExecuteApproved(
    request.RequestId,
    mutation,
    currentState,
    governanceContext: MutationContext.Service("governance-runtime", "Execute approved governance request"),
    strategy: VersionedRequestResolutionStrategy.RejectStale);
```

If your state implements `IVersionedState`, use the overload that accepts `currentState` directly and reads `state.Version` for both version selectors.

### Typical execution flow

1. create or load a request
2. approve the request if needed
3. resolve the request against the current state version
4. execute the underlying mutation through the core engine
5. record the terminal request decision

That flow is what the `GovernedExecution` example demonstrates end to end.

## Common operations

### Create a pending approval request

```csharp
var request = MutationRequestFactory.PendingApproval(
    stateId: "tenant-42:roles",
    stateType: "IamRoleState",
    mutationType: "GrantRoleMutation",
    intent: new MutationIntent
    {
        OperationName = "GrantRole",
        Category = "Security",
        Description = "Grant elevated role to tenant operator"
    },
    context: MutationContext.User("requester-1", "Requester One", "Incident escalation"),
    expectedStateVersion: "v10",
    approvalRequirements:
    [
        MutationApprovalRequirement.SingleActorStep("security-lead"),
        MutationApprovalRequirement.SingleActorStep("platform-owner")
    ]);
```

### Record an approval decision

```csharp
var decision = MutationRequestDecision.Approval(
    MutationRequestApprovalDecisionType.Approved,
    MutationContext.User("approver-1", "Approver One", "Approved after review"));
```

### Execute an already approved request

```csharp
var result = await executionManager.ExecuteApproved(
    request.RequestId,
    mutation,
    currentState,
    governanceContext: MutationContext.Service("governance-runtime", "Execute approved governance request"),
    strategy: VersionedRequestResolutionStrategy.RejectStale);
```

## Where to look next

- `README.md` for the package overview
- `Examples/Governance/*/README.md` for runnable scenarios
- `src/Governance/Abstractions/Requests/Factory/MutationRequestFactory.cs` for request creation
- `src/Governance/Abstractions/Requests/Decisions/MutationRequestDecision.cs` for decision helpers
- `src/Governance/Runtime/Execution/Orchestration/GovernanceExecutionManager.cs` for governed execution
