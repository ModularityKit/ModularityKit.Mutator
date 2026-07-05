# FeatureFlags

This example shows how to treat feature flags as first class state mutations with auditability, history inspection, and approval style restrictions.

It is good fit when you want to see how the engine behaves in an operational workflow where toggles may be simple, but the decision process around them is not.

## Domain

The state is dictionary of feature names to enabled or disabled values.

The example covers three workflows:

- enabling feature
- disabling feature
- toggling multiple features in batch

## What this example demonstrates

- mutation driven feature rollout
- policy checks around sensitive toggles
- batch execution with mixed outcomes
- history retrieval and log printing
- using `MutationContext` metadata for approval tracking

## Project structure

- [`Program.cs`](Program.cs)
- [`FeatureFlags.csproj`](FeatureFlags.csproj)
- [`State/FeatureFlagsState.cs`](State/FeatureFlagsState.cs)
- [`Mutations/EnableFeatureMutation.cs`](Mutations/EnableFeatureMutation.cs)
- [`Mutations/DisableFeatureMutation.cs`](Mutations/DisableFeatureMutation.cs)
- [`Policies/BusinessHoursPolicy.cs`](Policies/BusinessHoursPolicy.cs)
- [`Policies/RequireTwoManApprovalPolicy.cs`](Policies/RequireTwoManApprovalPolicy.cs)
- [`Policies/FeatureFlagGovernancePolicies.cs`](Policies/FeatureFlagGovernancePolicies.cs)
- [`Scenarios/EnableNewCheckoutScenario.cs`](Scenarios/EnableNewCheckoutScenario.cs)
- [`Scenarios/DisableLegacyCheckoutScenario.cs`](Scenarios/DisableLegacyCheckoutScenario.cs)
- [`Scenarios/BatchFeatureToggleScenario.cs`](Scenarios/BatchFeatureToggleScenario.cs)

## How it works

`Program.cs`:

1. registers the engine with strict options
2. resolves `IMutationEngine`
3. registers the composed `CriticalFeatureFlagGovernance` policy set
4. runs the example scenarios
5. prints history for the main state
6. prints engine statistics

The sample also includes `BusinessHoursPolicy` as reference implementation. It is not registered by default, but it shows how time based operational limits would be expressed.

## Mutation flow

### Enable feature

[`EnableFeatureMutation`](Mutations/EnableFeatureMutation.cs) enables a flag.

- validates that the feature name exists
- writes the new enabled value into copied state
- emits `ChangeSet` entry for the feature

### Disable feature

[`DisableFeatureMutation`](Mutations/DisableFeatureMutation.cs) disables a flag.

- validates that the feature exists
- writes the new disabled value into copied state
- emits change record for the flag

## Policies

### Two man approval

[`RequireTwoManApprovalPolicy`](Policies/RequireTwoManApprovalPolicy.cs) blocks certain high risk feature changes unless metadata contains two distinct approvers.

It demonstrates:

- inspection of `MutationContext.Metadata`
- policy decisions based on the mutation type
- excluding the actor from the approval list

### Business hours

[`BusinessHoursPolicy`](Policies/BusinessHoursPolicy.cs) is simple time based policy for highrisk changes.

Use it as reference if you want to restrict rollout windows.

### Composed governance set

[`FeatureFlagGovernancePolicies.CriticalChanges`](Policies/FeatureFlagGovernancePolicies.cs) combines business-hours restrictions and two-man approval into one reusable policy set.

It demonstrates:

- `PolicyComposition.AllOf(...)`
- explicit policy merge behavior
- registering one composed policy instead of multiple hand-wired policy classes

## Scenarios

### Enable new checkout

[`EnableNewCheckoutScenario`](Scenarios/EnableNewCheckoutScenario.cs) shows how new feature is turned on from known starting state.

### Disable legacy checkout

[`DisableLegacyCheckoutScenario`](Scenarios/DisableLegacyCheckoutScenario.cs) shows how feature can be turned off with approval metadata attached.

### Batch feature toggle

[`BatchFeatureToggleScenario`](Scenarios/BatchFeatureToggleScenario.cs) shows mixed feature operations in single batch.

It demonstrates:

- batch mutation submission
- partial success and failure handling
- result logging
- folding the batch output back into the final state

## What to read first

1. [`Program.cs`](Program.cs)
2. [`State/FeatureFlagsState.cs`](State/FeatureFlagsState.cs)
3. [`Mutations/EnableFeatureMutation.cs`](Mutations/EnableFeatureMutation.cs)
4. [`Mutations/DisableFeatureMutation.cs`](Mutations/DisableFeatureMutation.cs)
5. [`Policies/RequireTwoManApprovalPolicy.cs`](Policies/RequireTwoManApprovalPolicy.cs)
6. [`Scenarios/BatchFeatureToggleScenario.cs`](Scenarios/BatchFeatureToggleScenario.cs)

## Run

```bash
dotnet run --project Examples/Core/FeatureFlags/FeatureFlags.csproj
```

## Expected output

You should see:

- one or more feature flag changes
- batch mutation logs
- a history dump for the tracked state
- a statistics summary at the end

The sample is intentionally written as an operational console app, not as presentation layer.
