# Examples

This folder contains runnable console apps that show how to use `ModularityKit.Mutator` in concrete domain flows.

The examples are split into two groups:

- `Core/` for direct mutation engine usage
- `Governance/` for request lifecycle and governance runtime behavior

The projects are intentionally small and focused. Each one demonstrates a different style of mutation workflow, policy enforcement, and engine usage without requiring you to read the whole library first.

## Examples

| Example | Focus | Readme |
| --- | --- | --- |
| `BillingQuotas` | quota changes, validation, and policy limits | [`Examples/Core/BillingQuotas/README.md`](Core/BillingQuotas/README.md) |
| `FeatureFlags` | feature toggles, audit/history, and batch execution | [`Examples/Core/FeatureFlags/README.md`](Core/FeatureFlags/README.md) |
| `IamRoles` | role changes, approval rules, and batch migration | [`Examples/Core/IamRoles/README.md`](Core/IamRoles/README.md) |
| `WorkflowApprovals` | ordered workflow state transitions and approvals | [`Examples/Core/WorkflowApprovals/README.md`](Core/WorkflowApprovals/README.md) |

## Governance examples

| Example | Focus | Readme |
| --- | --- | --- |
| `RequestLifecycle` | pending requests, lifecycle transitions, expiration, and cancellation | [`Examples/Governance/RequestLifecycle/README.md`](Governance/RequestLifecycle/README.md) |
| `GovernedExecution` | approved request -> resolution -> execution -> executed decision | [`Examples/Governance/GovernedExecution/README.md`](Governance/GovernedExecution/README.md) |
| `DecisionTaxonomy` | lifecycle, approval, and version-resolution decision categories | [`Examples/Governance/DecisionTaxonomy/README.md`](Governance/DecisionTaxonomy/README.md) |
| `ApprovalWorkflow` | request-level approvals, multi-step sign-off, and governed approval actions | [`Examples/Governance/ApprovalWorkflow/README.md`](Governance/ApprovalWorkflow/README.md) |
| `VersionedResolution` | stale request handling and expected state version semantics | [`Examples/Governance/VersionedResolution/README.md`](Governance/VersionedResolution/README.md) |
| `Queries` | request queries, pending approvals, and decision-oriented governance read views | [`Examples/Governance/Queries/README.md`](Governance/Queries/README.md) |
| `RedisQueries` | Redis-backed governance request storage and query-oriented reads | [`Examples/Governance/RedisQueries/README.md`](Governance/RedisQueries/README.md) |

## How to use these examples

Start with the project-specific README for the domain you care about. Each one explains:

- the domain model used in the sample
- the mutation types and policies involved
- the main scenarios the app runs
- the key files to inspect first

Then open `Program.cs` in that example. That is the entry point that wires up the engine, registers policies, and runs the scenarios.

## Build

Build the full solution from the repository root:

```bash
dotnet build ModularityKit.Mutator.slnx -c Release
```

You can also build just one example:

```bash
dotnet build Examples/Core/BillingQuotas/BillingQuotas.csproj -c Release
dotnet build Examples/Core/FeatureFlags/FeatureFlags.csproj -c Release
dotnet build Examples/Core/IamRoles/IamRoles.csproj -c Release
dotnet build Examples/Core/WorkflowApprovals/WorkflowApprovals.csproj -c Release
dotnet build Examples/Governance/RequestLifecycle/RequestLifecycle.csproj -c Release
dotnet build Examples/Governance/GovernedExecution/GovernedExecution.csproj -c Release
dotnet build Examples/Governance/DecisionTaxonomy/DecisionTaxonomy.csproj -c Release
dotnet build Examples/Governance/ApprovalWorkflow/ApprovalWorkflow.csproj -c Release
dotnet build Examples/Governance/VersionedResolution/VersionedResolution.csproj -c Release
dotnet build Examples/Governance/Queries/Queries.csproj -c Release
dotnet build Examples/Governance/RedisQueries/RedisQueries.csproj -c Release
```

## Run

Each example is a separate console app.

From the repository root:

```bash
dotnet run --project Examples/Core/BillingQuotas/BillingQuotas.csproj
dotnet run --project Examples/Core/FeatureFlags/FeatureFlags.csproj
dotnet run --project Examples/Core/IamRoles/IamRoles.csproj
dotnet run --project Examples/Core/WorkflowApprovals/WorkflowApprovals.csproj
dotnet run --project Examples/Governance/RequestLifecycle/RequestLifecycle.csproj
dotnet run --project Examples/Governance/GovernedExecution/GovernedExecution.csproj
dotnet run --project Examples/Governance/DecisionTaxonomy/DecisionTaxonomy.csproj
dotnet run --project Examples/Governance/ApprovalWorkflow/ApprovalWorkflow.csproj
dotnet run --project Examples/Governance/VersionedResolution/VersionedResolution.csproj
dotnet run --project Examples/Governance/Queries/Queries.csproj
dotnet run --project Examples/Governance/RedisQueries/RedisQueries.csproj
```

If you want to run one sample repeatedly while changing code, stay in its folder:

```bash
cd Examples/Core/BillingQuotas
dotnet run
```

## Suggested reading order

1. Open the project README for the example you care about.
2. Read `Program.cs` to see how the engine is composed.
3. Inspect the `State/` types to understand the domain model.
4. Read the `Mutations/` files to see how changes are applied.
5. Read the `Policies/` files to see what is allowed or blocked.
6. Read the `Scenarios/` files to see how everything is exercised together.

## Common structure

Most examples follow the same layout:

- `Program.cs` - console entry point and composition root
- `State/` - immutable or mostly immutable domain state
- `Mutations/` - mutation implementations
- `Policies/` - business rules and safeguards
- `Scenarios/` - runnable flows that use the engine
- `README.md` - example-specific documentation

That layout is deliberate. It makes each sample easy to scan and keeps the mutation engine usage visible instead of hidden behind helper abstractions.

## Example summaries

### BillingQuotas

Shows quota management workflows such as increasing quotas, applying limits, and resetting values on schedule. It is the simplest place to look if you want a compact example of validation plus policy enforcement.

See [`Core/BillingQuotas/README.md`](Core/BillingQuotas/README.md).

### FeatureFlags

Shows feature toggle workflows with policy checks and history logging. This example is useful if you want to see how the engine behaves when you care about auditability and batch changes.

See [`Core/FeatureFlags/README.md`](Core/FeatureFlags/README.md).

### IamRoles

Shows role grant and revoke workflows with approval-style rules. This is the example to read if you care about protection against unsafe privilege changes.

See [`Core/IamRoles/README.md`](Core/IamRoles/README.md).

### WorkflowApprovals

Shows a multi-step approval process with ordered execution and rejection handling. This example is the best fit if you want to study state transitions that must happen in a strict sequence.

See [`Core/WorkflowApprovals/README.md`](Core/WorkflowApprovals/README.md).

### RequestLifecycle

Shows the governance runtime as a request lifecycle system instead of an immediate execution path. This is the example to read if you want to understand pending requests, approval, cancellation, and expiration.

See [`Governance/RequestLifecycle/README.md`](Governance/RequestLifecycle/README.md).

### GovernedExecution

Shows the full governed execution loop from approved request to version resolution, core mutation execution, and terminal executed decision.

See [`Governance/GovernedExecution/README.md`](Governance/GovernedExecution/README.md).

### ApprovalWorkflow

Shows how governance turns approval requirements into explicit request-level approval actions with ordered steps and terminal approval or rejection behavior.

See [`Governance/ApprovalWorkflow/README.md`](Governance/ApprovalWorkflow/README.md).

### DecisionTaxonomy

Shows why governance request decisions are split into lifecycle, approval, and version-resolution categories instead of being kept in one flat enum.

See [`Governance/DecisionTaxonomy/README.md`](Governance/DecisionTaxonomy/README.md).

### VersionedResolution

Shows how governance resolves approved requests once the underlying state version has moved. This is the example to read if you want concrete stale request semantics.

See [`Governance/VersionedResolution/README.md`](Governance/VersionedResolution/README.md).

### Queries

Shows the governance read side as a first-class API for listing requests, approvals, and decision history.

See [`Governance/Queries/README.md`](Governance/Queries/README.md).

### RedisQueries

Shows the same governance read side backed by `ModularityKit.Mutator.Governance.Redis` instead of the in-memory store.

See [`Governance/RedisQueries/README.md`](Governance/RedisQueries/README.md).

## Notes

- The examples are separate console applications, not libraries.
- Core examples reference the mutation engine under `src/ModularityKit.Mutator.csproj`.
- Governance examples reference `src/ModularityKit.Mutator.Governance.csproj`.
- The sample code is meant to be readable and runnable, not minimal for its own sake.
- Benchmarking lives in [`Benchmarks/`](../Benchmarks/README.md) and is separate from the examples.
- Tests, when added, should live in [`Tests/`](../Tests/).

## Related documentation

- Root project overview: [`README.md`](../README.md)
- Mutation engine source: [`src/`](../src/)
- Benchmark project: [`Benchmarks/README.md`](../Benchmarks/README.md)
