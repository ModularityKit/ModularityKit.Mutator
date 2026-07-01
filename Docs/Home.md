![ModularityKit.Mutator](../assets/brand/mutator-landing-banner.png)

# ModularityKit.Mutator

ModularityKit.Mutator is a .NET mutation runtime with governance, request lifecycle, approval flow,
and Redis-backed storage.

## Packages

| Package | What it covers |
| --- | --- |
| [`ModularityKit.Mutator`](../src/README.md) | mutation runtime, policies, execution, audit, history |
| [`ModularityKit.Mutator.Governance`](../src/Governance/README.md) | request lifecycle, approvals, resolution, governed execution |
| [`ModularityKit.Mutator.Governance.Redis`](../src/Redis/README.md) | Redis-backed storage and query provider |

## Explore

| Area | What to read |
| --- | --- |
| API reference | [`Docs/API/Reference.md`](API/Reference.md) |
| Core concepts | [`Docs/Core-Concepts.md`](Core-Concepts.md) |
| Execution model | [`Docs/ExecutionModel.md`](ExecutionModel.md) |
| Roadmap | [`Docs/Roadmap.md`](Roadmap.md) |

## What this site contains

- package overviews for the runtime and governance extensions
- conceptual docs for the mutation model and request flow
- generated API reference from XML docs

## Build locally

```bash
dotnet tool update -g docfx
dotnet build ModularityKit.Mutator.slnx -c Release
docfx docfx.json
cp index.html _site/index.html
```
