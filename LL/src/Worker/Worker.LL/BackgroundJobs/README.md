# Background Jobs

`Worker.LL` runs Quartz.NET as a separate process from `API.LL`. The API handles player traffic; this worker handles durable global schedules.

Quartz is configured with PostgreSQL persistent storage, clustering, stable job identities, System.Text.Json payload serialization, and graceful shutdown. Do not switch production to `RAMJobStore`.

## Database Setup

Apply the official Quartz PostgreSQL schema before starting the worker:

```text
LL/database/quartz/tables_postgres.sql
```

The vendored script comes from Quartz.NET `v3.17.1`. It includes a `DropDb` flag at the top because it is based on the official pristine-init script; the checked-in copy defaults that flag to `0` so it creates missing tables without dropping existing scheduler state.

The game-owned idempotency log is not part of Quartz. It is managed by EF Core through the `AddBackgroundJobExecutions` migration and stores one row per logical job operation.

## Adding Jobs

Add future jobs in `BackgroundJobRegistrationExtensions`, using constants from `BackgroundJobNames` and `BackgroundJobGroups`. Jobs should remain thin and call application/service-layer logic.

Every real job must compute a deterministic business key and execute through `IBackgroundJobExecutionService.RunOnceAsync`. Examples:

```text
daily-reset:2026-06-29
weekly-colosseum-settlement:2026-W27
tournament-rollover:season-12:phase-rewarding
guild-war-matchmaking:season-5:round-3
auction-expiration-settlement:2026-06-29T13:00Z
```

Most global jobs should use `[DisallowConcurrentExecution]`, but that is not enough by itself. Keep domain operations idempotent and rely on the execution log plus database constraints for retry safety.

Use UTC internally. Feature-specific reset times can convert from a game/server timezone before computing the business key.

## Misfire Guidelines

| Job Type | Misfire Policy |
|---|---|
| Daily reset | Fire once when the worker comes back |
| Weekly/season settlement | Fire when possible; must be idempotent |
| Auction expiration | Fire when possible |
| Cleanup/maintenance | Usually skip missed runs |
| World boss spawn window | Feature-specific; may skip if event window has passed |
| Guild war phase transition | Fire when possible and reconcile current phase from DB |

Choose misfire behavior deliberately for every important game system. Do not rely on Quartz defaults for settlement, reset, or phase-transition jobs.

## Non-Uses

Do not use Quartz for per-player idle combat, gathering, energy/ticket regeneration, or similar mechanics that can be calculated when a player interacts with the system.
