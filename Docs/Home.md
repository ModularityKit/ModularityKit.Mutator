# ModularityKit.Mutator

ModularityKit.Mutator is a .NET mutation runtime with governance, request lifecycle control,
approval flow, and Redis-backed storage.

![ModularityKit.Mutator](../assets/brand/mutator-landing-banner.png)

## Start here

- [API reference](API/Reference.md)
- [Architecture](Architecture.md)
- [Core concepts](Core-Concepts.md)
- [Execution model](ExecutionModel.md)
- [ADR index](Decision/listadr.md)

## Packages

| Package | What it covers |
| --- | --- |
| [`ModularityKit.Mutator`](../src/README.md) | mutation runtime, policies, execution, audit, and history |
| [`ModularityKit.Mutator.Governance`](../src/Governance/README.md) | request lifecycle, approvals, resolution, and governed execution |
| [`ModularityKit.Mutator.Governance.Redis`](../src/Redis/README.md) | Redis-backed storage and query provider |

## What is covered

- package overviews for the runtime and governance extensions
- conceptual docs for the mutation model and request flow
- decision records for architecture-level changes
- generated API reference from XML docs

## Build locally

```bash
dotnet tool update -g docfx
dotnet build ModularityKit.Mutator.slnx -c Release
docfx docfx.json
```
