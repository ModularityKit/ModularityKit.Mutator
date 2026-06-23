# Governance ApprovalWorkflow

This example shows the governance approval workflow built on top of `MutationRequestFactory.PendingApproval(...)` and `MutationRequestApprovalWorkflowManager`.

It demonstrates:

- mapping `PolicyRequirement` into request-level approval requirements
- multi-actor approvals in the same step
- ordered approval steps
- transition from `Pending` to `Approved` after the final approval

## Key files

- [`Program.cs`](Program.cs)
- [`Scenarios/GovernanceApprovalWorkflowScenario.cs`](Scenarios/GovernanceApprovalWorkflowScenario.cs)
- [`src/Governance/Runtime/Approval/MutationRequestApprovalWorkflowManager.cs`](../../../src/Governance/Runtime/Approval/MutationRequestApprovalWorkflowManager.cs)
- [`src/Governance/Abstractions/Approval/MutationApprovalRequirement.cs`](../../../src/Governance/Abstractions/Approval/MutationApprovalRequirement.cs)

## Run

```bash
dotnet run --project Examples/Governance/ApprovalWorkflow/ApprovalWorkflow.csproj
```
