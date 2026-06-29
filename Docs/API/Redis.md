# Redis Governance Provider API

This page covers the Redis-backed provider package `ModularityKit.Mutator.Governance.Redis`.

It is the storage and query backend for governed requests. The package implements the public
request store and query store contracts while keeping Redis-specific persistence details internal.

## What this API is for

Use the Redis provider when you want the governance model from `ModularityKit.Mutator.Governance`
but need a persistent backing store instead of in-memory state.

Typical reasons:

- you want request persistence across process restarts
- you want Redis-backed query projections for approval queues and decision history
- you want the same governance API surface with a different storage backend
- you want to share governed request state across multiple runtime instances

## Primary APIs

### Dependency injection

- `RedisGovernanceServiceCollectionExtensions.AddRedisGovernanceStore(...)`

Registers the Redis-backed governance store and the query store against an existing
`IConnectionMultiplexer`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using ModularityKit.Mutator.Governance.Redis;
using StackExchange.Redis;

var services = new ServiceCollection();
var multiplexer = await ConnectionMultiplexer.ConnectAsync("localhost:6379");

services.AddRedisGovernanceStore(
    multiplexer,
    options => options.KeyPrefix = "modularitykit:governance");
```

The extension method registers the provider-facing storage and query services against DI.

### Configuration details

`RedisMutationRequestStoreOptions.KeyPrefix` scopes all Redis keys created by the provider.

Use a dedicated prefix per environment or application if you need isolation between deployments.
The examples use `modularitykit:governance` as a readable default.

### Configuration

- `RedisMutationRequestStoreOptions`

Current option:

- `KeyPrefix` - Redis key prefix used for all request data, indexes, and query projections

### Storage facade

- `RedisMutationRequestStore`

Implements:

- `IMutationRequestStore`
- `IMutationRequestQueryStore`

Use it through DI rather than constructing it directly unless you are testing internals.

### What gets stored

The provider stores the data needed for:

- request documents
- decision and approval projections
- query indexes and queue membership
- optimistic concurrency state for request writes

The provider does not change the governance model. It only persists the same model through Redis.

### Keyspace and key helpers

- `RedisMutationRequestKeyspace`
- `RedisMutationRequestDocumentKeyFactory`

These types define the Redis naming and partitioning rules for request documents, indexes, and
query views.

## What the provider does

The Redis provider handles:

- request persistence
- query projection
- optimistic concurrency on request writes
- Redis index maintenance
- candidate selection for query execution

It does not change the governance model itself. It only replaces the in-memory storage backend with
Redis-specific persistence and read paths.

## Common usage

### Register the provider

```csharp
services.AddRedisGovernanceStore(
    multiplexer,
    options => options.KeyPrefix = "modularitykit:governance");
```

### Resolve the stores

```csharp
var requestStore = provider.GetRequiredService<IMutationRequestStore>();
var queryStore = provider.GetRequiredService<IMutationRequestQueryStore>();
```

### Use the normal governance APIs

Once registered, the rest of the application uses the same request and query contracts as the
in-memory provider.

## Usage pattern

1. Connect to Redis with `StackExchange.Redis`
2. Register the provider with `AddRedisGovernanceStore(...)`
3. Resolve `IMutationRequestStore` and `IMutationRequestQueryStore` from DI
4. Use the normal governance request and query APIs

## Related docs

- [`src/Redis/README.md`](../../src/Redis/README.md)
- [`Examples/Governance/RedisQueries/README.md`](../../Examples/Governance/RedisQueries/README.md)
- [`API-Reference.md`](API-Reference.md)
- [Governance API](API.md)
