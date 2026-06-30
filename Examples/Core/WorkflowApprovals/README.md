# WorkflowApprovals

This example models multi step workflow where ordering matters and approvals can be blocked by policy.

It is the best sample to read if you want to understand how the engine behaves when one mutation must happen before the next one can safely proceed.

## Domain

The domain state contains:

- a workflow ID
- an initiator
- an ordered list of workflow steps

Each step has status and may be approved or rejected by user.

The example includes two scenarios:

- a happy path where the steps are approved in sequence
- a rejection path where one or more approvals are blocked
- a side effects path where workflow start and rejection emit observable side effects

## What this example demonstrates

- ordered workflow mutation
- approval and rejection transitions
- policy checks that depend on prior state
- mutation context usage for audit and traceability
- failure handling when step is out of order or unauthorized
- side effect emission for monitoring, alerting, and follow-up workflows

## Project structure

- [`Program.cs`](Program.cs)
- [`WorkflowApprovals.csproj`](WorkflowApprovals.csproj)
- [`State/ApprovalWorkflowState.cs`](State/ApprovalWorkflowState.cs)
- [`State/WorkflowStep.cs`](State/WorkflowStep.cs)
- [`State/StepStatus.cs`](State/StepStatus.cs)
- [`Mutations/StartApprovalMutation.cs`](Mutations/StartApprovalMutation.cs)
- [`Mutations/ApproveStepMutation.cs`](Mutations/ApproveStepMutation.cs)
- [`Mutations/RejectWorkflowMutation.cs`](Mutations/RejectWorkflowMutation.cs)
- [`Policies/EnforceOrderPolicy.cs`](Policies/EnforceOrderPolicy.cs)
- [`Policies/RequireManagerApprovalPolicy.cs`](Policies/RequireManagerApprovalPolicy.cs)
- [`Scenarios/HappyPathScenario.cs`](Scenarios/HappyPathScenario.cs)
- [`Scenarios/RejectedScenario.cs`](Scenarios/RejectedScenario.cs)
- [`Scenarios/SideEffectsScenario.cs`](Scenarios/SideEffectsScenario.cs)

## How it works

`Program.cs`:

1. registers the engine with strict options
2. resolves `IMutationEngine`
3. registers workflow policies
4. runs both scenarios
5. prints engine statistics

The sample is intentionally sequential. It shows how stateful process can be advanced step by step while the engine keeps the mutation logic and policy logic separate.

## Mutation flow

### Start workflow

[`StartApprovalMutation`](Mutations/StartApprovalMutation.cs) creates workflow from list of step names.

- validates initiator and step list
- creates workflow steps from names
- initializes a new workflow ID
- emits change entry for the created step list
- emits a `WorkflowStarted` side effect through `SideEffect.Create(...)` with a typed contract payload

### Approve step

[`ApproveStepMutation`](Mutations/ApproveStepMutation.cs) approves a single workflow step.

- validates the step index
- validates approver input
- updates the targeted step
- emits change entry for the step status

### Reject workflow

[`RejectWorkflowMutation`](Mutations/RejectWorkflowMutation.cs) marks the whole workflow as rejected.

- applies rejection to every step
- records the actor who rejected the workflow
- emits workflow level change
- emits a critical `WorkflowRejected` side effect through `SideEffect.Critical(...)` with a typed contract payload

## Policies

### Enforce order

[`EnforceOrderPolicy`](Policies/EnforceOrderPolicy.cs) prevents a step from being approved before the previous step has been approved.

This is the main rule that makes the example a proper ordered workflow.

### Require manager approval

[`RequireManagerApprovalPolicy`](Policies/RequireManagerApprovalPolicy.cs) limits approvals to designated managers.

This policy demonstrates:

- checking the specific mutation type
- validating approver identity
- rejecting unauthorized actors

## Scenarios

### Happy path

[`HappyPathScenario`](Scenarios/HappyPathScenario.cs) starts workflow and approves each step in order.

It shows:

- successful workflow initialization
- sequential approvals
- final state printing

### Rejected path

[`RejectedScenario`](Scenarios/RejectedScenario.cs) starts workflow and then attempts approvals that may be blocked.

It shows:

- policy driven rejection
- per step logging
- final workflow state inspection

### Side effects path

[`SideEffectsScenario`](Scenarios/SideEffectsScenario.cs) starts a workflow and then rejects it to show side effects in the result object.

It shows:

- a standard side effect created with `SideEffect.Create(...)`
- a critical side effect created with `SideEffect.Critical(...)`
- how typed payload contracts are registered through `SideEffectDataContractRegistry`
- how `Severity`, `RequiresAction`, `DataContractType`, and typed `Data` can be read from `MutationResult.SideEffects`

## What to read first

1. [`State/ApprovalWorkflowState.cs`](State/ApprovalWorkflowState.cs)
2. [`State/WorkflowStep.cs`](State/WorkflowStep.cs)
3. [`State/StepStatus.cs`](State/StepStatus.cs)
4. [`Mutations/StartApprovalMutation.cs`](Mutations/StartApprovalMutation.cs)
5. [`Mutations/ApproveStepMutation.cs`](Mutations/ApproveStepMutation.cs)
6. [`Policies/EnforceOrderPolicy.cs`](Policies/EnforceOrderPolicy.cs)
7. [`Policies/RequireManagerApprovalPolicy.cs`](Policies/RequireManagerApprovalPolicy.cs)
8. [`Scenarios/SideEffectsScenario.cs`](Scenarios/SideEffectsScenario.cs)

## Run

```bash
dotnet run --project Examples/Core/WorkflowApprovals/WorkflowApprovals.csproj
```

## Expected output

You should see:

- workflow start output
- approvals or policy rejections for individual steps
- the final workflow state
- engine statistics at the end

This sample is the cleanest one to study for staged business processes and ordered execution rules.
