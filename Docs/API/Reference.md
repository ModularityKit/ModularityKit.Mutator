# API Reference

DocFX generates the public API reference from the compiled assemblies and XML documentation files.

## Included packages

- `ModularityKit.Mutator`
- `ModularityKit.Mutator.Governance`
- `ModularityKit.Mutator.Governance.Redis`

## Build locally

```bash
dotnet tool update -g docfx
docfx docfx.json
```

The rendered API pages appear under the `API` section of the generated site.
