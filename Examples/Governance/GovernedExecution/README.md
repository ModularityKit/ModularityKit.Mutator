# GovernedExecution

This example shows the full governed execution loop:

- approved request
- version resolution
- mutation execution through the core engine
- terminal `Executed` request decision

## Key Files

- [`Program.cs`](Program.cs)
- [`Scenarios/GovernanceExecutionScenario.cs`](Scenarios/GovernanceExecutionScenario.cs)
- [`src/Governance/Runtime/Execution/Orchestration/GovernanceExecutionManager.cs`](../../../src/Governance/Runtime/Execution/Orchestration/GovernanceExecutionManager.cs)
- [`src/Governance/Abstractions/Execution/Contracts/IGovernanceExecutionManager.cs`](../../../src/Governance/Abstractions/Execution/Contracts/IGovernanceExecutionManager.cs)

## Run

```bash
dotnet run --project Examples/Governance/GovernedExecution/GovernedExecution.csproj
```
