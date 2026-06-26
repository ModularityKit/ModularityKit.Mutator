# Governance RedisQueries

This example shows how to use `ModularityKit.Mutator.Governance.Redis` as the backing store for governed request writes and query-oriented reads.

It focuses on real Redis-backed request persistence, approval queue reads, and decision/history queries through the same provider.

## What it demonstrates

- connecting to Redis with `ConnectionMultiplexer`
- registering the Redis governance provider through `AddRedisGovernanceStore(...)`
- creating governed requests through `IMutationRequestStore`
- reading pending request queues through `IMutationRequestQueryStore`
- projecting approval views and decision views from Redis-backed data
- isolating sample data with a dedicated Redis key prefix

## Prerequisite

You need a running Redis instance.

Default connection:

```bash
localhost:6379
```

Override it with a full connection string:

```bash
export MODULARITYKIT_REDIS="your-host:6379"
```

Or with separate settings:

```bash
export MODULARITYKIT_REDIS_HOST="localhost"
export MODULARITYKIT_REDIS_PORT="6379"
export MODULARITYKIT_REDIS_PASSWORD=""
```

## Start Redis locally

From this folder:

```bash
docker compose up -d
```

This starts a local Redis instance using [`docker-compose.yml`](docker-compose.yml).

Optional overrides:

```bash
export MODULARITYKIT_REDIS_PORT="6380"
export MODULARITYKIT_REDIS_PASSWORD="secret"
docker compose up -d
```

Stop it with:

```bash
docker compose down
```

## Key files

- [`Program.cs`](Program.cs)
- [`Scenarios/GovernanceRedisQueriesScenario.cs`](Scenarios/GovernanceRedisQueriesScenario.cs)
- [`src/Redis/DependencyInjection/RedisGovernanceServiceCollectionExtensions.cs`](../../../src/Redis/DependencyInjection/RedisGovernanceServiceCollectionExtensions.cs)
- [`src/Redis/Storage/RedisMutationRequestStore.cs`](../../../src/Redis/Storage/RedisMutationRequestStore.cs)

## Run

```bash
dotnet run --project Examples/Governance/RedisQueries/RedisQueries.csproj -c Release
```

## Expected output

The sample prints:

- the Redis connection and key prefix used by the run
- pending approval queue entries
- approval views filtered by approver
- recent decision views for execution outcomes
