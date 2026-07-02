# Tournament Grounds Quartz Integration

## Target Services

Tournament Grounds currently spans two service boundaries:

- `API.LL` hosts the HTTP/SignalR API and owns player-facing Tournament Grounds requests.
- `Worker.LL` hosts Quartz scheduled jobs and owns durable Tournament Grounds progression.

The integration target was to move Tournament Grounds progression from the API hosted-service loop into `Worker.LL` Quartz scheduling while keeping the existing tournament domain/application behavior intact.

## Current Tournament Grounds Implementation

`API.LL` previously registered `TournamentProgressionHostedService` from `Program.cs`. That hosted service was a simple loop:

1. Check `Colosseum:TournamentGrounds:Enabled`.
2. Create a DI scope.
3. Resolve `ITournamentGroundsService`.
4. Call `EnsureUpcomingTournamentsAsync`.
5. Call `AdvanceDueTournamentsAsync`.
6. Delay by `max(15, ProgressionIntervalSeconds)` seconds.

The API hosted service has been removed. Tournament progression is now owned by `Worker.LL` through Quartz. The current default configuration lives under `Colosseum:TournamentGrounds` in both API and worker appsettings so API reads and worker progression share the same tournament rules.

The main tournament implementation is `TournamentGroundsService` in `Services.LL`. It owns both player-facing commands and background progression. Important methods:

- `EnsureUpcomingTournamentsAsync` creates the default tournament definition if missing and creates the next weekly tournament if no active/upcoming tournament exists.
- `AdvanceDueTournamentsAsync` queries up to 10 due tournaments and advances each.
- `AdvanceTournamentAsync` locks one tournament and applies state transitions until no more due work remains or a 100-step guard is exceeded.
- `GenerateBracketAsync`, `ResolveDueRoundsAsync`, `ResolveMatchAsync`, and `CompleteTournamentAsync` handle bracket creation, match simulation, placements, reward grants, and completion.

Progression state is represented by `TournamentStatus`:

- `Scheduled`
- `RegistrationOpen`
- `RegistrationClosed`
- `BracketGenerated`
- `InProgress`
- `Completed`
- `Cancelled`

Per-tournament mutation is protected by repository-owned transactions and PostgreSQL transaction-scoped advisory locks via `PostgresTournamentLockService`. This is important because player commands and background progression can both touch tournament state.

## Background-Job Risks Addressed

The old hosted service ran inside every API process. If the API was horizontally scaled, every API replica ran the same loop. Tournament progression is now registered in `Worker.LL` as a Quartz job, and the API hosted progression service has been removed.

The per-tournament advisory lock protects tournament-id-based mutations. `EnsureUpcomingTournamentsAsync` creates a tournament before a tournament id exists, so it now acquires a schedule-level advisory lock through `ITournamentLockService.LockTournamentScheduleAsync` before creating the default definition or next weekly tournament.

Read and player-command paths no longer perform lazy tournament progression. Status, details, bracket, register, and withdraw operations use the current persisted tournament state; only the Quartz progression job calls `EnsureUpcomingTournamentsAsync` and `AdvanceDueTournamentsAsync`.

Realtime behavior also matters. `TournamentGroundsService` publishes `TournamentGroundsUpdated` events after key transitions. In the API process this can reach connected clients. In the worker process, the current worker realtime implementation is intentionally no-op.

## Current Quartz Implementation

`Worker.LL` is the canonical Quartz host. It registers the same persistence, repository, application, common, and service-layer dependencies as the API, then adds Quartz background-job infrastructure.

Quartz is configured with:

- Scheduler name `LegendsLegacy.Background`.
- Scheduler id `AUTO`.
- Configurable max concurrency from `BackgroundJobs:MaxConcurrency`.
- PostgreSQL persistent store using `ConnectionStrings:LegendsLegacyDB`.
- System.Text.Json serialization.
- Quartz clustering with 20-second check-ins and 60-second misfire threshold.
- Hosted-service shutdown that waits for jobs to complete.

The Quartz PostgreSQL schema is checked in at `LL/database/quartz/tables_postgres.sql`.

The worker also has a game-owned idempotency layer, `IBackgroundJobExecutionService`, backed by `BackgroundJobExecutions`. Every real job is expected to compute a deterministic business key and execute through `RunOnceAsync`. This layer skips completed work, skips still-running work younger than the configured timeout, and retries failed or stale running executions.

`QuartzSmokeJob` remains available as a smoke job. Tournament Grounds progression is registered as `TournamentGroundsProgressionJob` under `BackgroundJobNames.TournamentGroundsRollover` in the `pvp` job group.

## Implemented Integration

1. Added a thin `TournamentGroundsProgressionJob` in `Worker.LL`.
   - Inject `ITournamentGroundsService`, `IBackgroundJobExecutionService`, options, and logger.
   - Mark it with `[DisallowConcurrentExecution]`.
   - Use `RunOnceAsync`.
   - Call `EnsureUpcomingTournamentsAsync` and `AdvanceDueTournamentsAsync`.

2. Registered the job in Quartz.
   - Use `BackgroundJobNames.TournamentGroundsRollover`.
   - Use `BackgroundJobGroups.PvP`.
   - Store durably and request recovery.
   - Add a trigger controlled by `Colosseum:TournamentGrounds:Enabled`.
   - Use `ProgressionIntervalSeconds`, clamped to the same 15-second minimum as the old hosted service.

3. Added equivalent Tournament Grounds config to `Worker.LL`.
   - The worker must receive the same `Colosseum:TournamentGrounds` settings as the API.
   - Deployment should source both API and worker settings from the same secret/config flow.

4. Removed API-hosted progression.
   - `Colosseum:TournamentGrounds:Enabled` continues to mean the feature is enabled.
   - The API no longer registers `TournamentProgressionHostedService`.

5. Hardened upcoming-tournament creation.
   - Added a schedule-level lock around `EnsureUpcomingTournamentsAsync`.
   - The current unique `TournamentNumber` index remains a database backstop.

6. Removed lazy API progression paths.
   - `GetStatusAsync`, `GetDetailsAsync`, `GetBracketAsync`, `RegisterAsync`, and `WithdrawAsync` no longer call private progression methods.
   - Quartz is now the only scheduled progression writer.

## Remaining Decisions

1. Decide realtime delivery.
   - Accept no worker realtime and rely on refresh/polling.
   - Or add a cross-process notification path, such as outbox/pubsub, rather than hosting SignalR directly in the worker.

## Verification Strategy

- Existing `TournamentGroundsService` tests continue to cover the state machine.
- Focused job tests prove the job delegates to `ITournamentGroundsService` through `IBackgroundJobExecutionService`.
- A service test proves upcoming-tournament creation acquires the schedule lock.
- Service tests prove API reads and registration/withdrawal do not lazily advance tournament state.
- Worker service-provider tests prove the configured Quartz scheduler can persist the Tournament Grounds job and trigger, with durable/recoverable job settings and the expected simple trigger interval, when `LL_TEST_TOURNAMENT_POSTGRES_CONNECTION` points at a live PostgreSQL database.
- An opt-in PostgreSQL smoke test drives `TournamentGroundsProgressionJob` through create, registration, bracket advancement, completion, rewards, and `BackgroundJobExecutions` persistence when `LL_TEST_TOURNAMENT_POSTGRES_CONNECTION` is set.
- Run `dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj`.

## Deployment Implications

No EF migration is required for the initial Quartz job if `BackgroundJobExecutions` already exists and the Quartz tables have already been created. Production deployment must ensure:

- The Quartz schema has been applied.
- The worker has the same relevant Tournament Grounds configuration as the API.
- `Worker.LL` is deployed and running, because the API no longer has a progression fallback.
- Worker replicas share the same PostgreSQL job store if clustering is used.
