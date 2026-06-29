# Tournament Grounds Implementation Status

Updated: 2026-06-27

## Scope

Tournament Grounds has been implemented as a v1 asynchronous Colosseum tournament feature for the primary `LL` game service and Angular player frontend.

The implemented direction follows the design recommendation: scheduled single-elimination tournaments, registration before a deadline, locked combat snapshots, automatic server-side bracket progression, and durable reward grants. It does not implement live player-vs-player rooms.

## Implemented

### Domain model

Added tournament domain models under:

```text
LL/src/Core/Domain/Models/Colosseum/Tournaments
```

Implemented models and enums:

- `TournamentDefinition`
- `TournamentInstance`
- `TournamentParticipant`
- `TournamentCombatSnapshot`
- `TournamentRound`
- `TournamentMatch`
- `TournamentCombatReplay`
- `TournamentRewardGrant`
- `TournamentStatus`
- `TournamentRoundStatus`
- `TournamentMatchStatus`
- `TournamentMatchOutcome`
- `TournamentParticipantStatus`
- `TournamentRewardStatus`
- `TournamentFormat`

Also added `TournamentRules` for deterministic single-elimination helper logic:

- next-power-of-two bracket sizing;
- bye count calculation;
- round naming;
- placement band calculation.

### Persistence

Added EF Core configuration for the tournament tables and updated `LLDbContext` / `IDbContext`.

Migration added:

```text
LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260627110535_AddTournamentGrounds.cs
LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260627121312_AddTournamentCombatReplays.cs
```

The migration creates:

- `TournamentDefinitions`
- `ArenaTournaments`
- `TournamentParticipants`
- `TournamentCombatSnapshots`
- `TournamentRounds`
- `TournamentMatches`
- `TournamentCombatReplays`
- `TournamentRewardGrants`

Implemented important indexes/constraints:

- unique tournament definition key;
- unique tournament number;
- unique participant by tournament + character;
- unique participant by tournament + account;
- unique tournament combat snapshot by tournament + character;
- unique round by tournament + round number;
- unique match by tournament + round number + match number;
- unique combat replay by tournament match;
- unique combat replay by combat session;
- unique combat replay by battle history row;
- unique reward grant by tournament + character + reward key.

### Backend service layer

Added:

```text
LL/src/Core/Application/Interfaces/Services/LL/Colosseum/ITournamentGroundsService.cs
LL/src/Core/Application/Interfaces/Services/LL/Colosseum/ITournamentLockService.cs
LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs
LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/PostgresTournamentLockService.cs
```

Implemented service behavior:

- creates a default `Daily Open Grounds` definition when needed;
- ensures an upcoming tournament exists;
- opens registration based on UTC schedule;
- closes registration based on UTC schedule;
- cancels if minimum participant count is not met;
- registers a character during the open window;
- prevents duplicate character registration in the same tournament;
- prevents multiple characters from the same account entering the same tournament;
- locks a tournament combat snapshot at registration;
- stores a serialized snapshot audit payload with character level, rating, rank tier, base attributes, equipment, modifiers, and equipped essences;
- allows withdrawal while registration remains open;
- generates single-elimination brackets;
- assigns deterministic seeds by entry arena rating descending;
- calculates byes for non-power-of-two participant counts;
- auto-advances bye participants;
- resolves due rounds automatically;
- runs tournament matches through the existing combat engine path using locked snapshots;
- advances winners to later rounds;
- resolves draws by advancing the better seed;
- records completed tournament matches in the existing Colosseum battle history stream;
- stores `CombatSessionId` and `BattleHistoryId` on tournament matches for bracket/history correlation;
- stores full tournament combat replay payloads as JSONB `CombatResultDto` snapshots;
- exposes replay retrieval for tournament participants and reward recipients;
- exposes player tournament history and global champion archive/Hall of Fame query surfaces;
- exposes current-season tournament points leaderboard query surfaces;
- uses an injectable tournament lock service for PostgreSQL advisory transaction locking;
- marks eliminated participants and stores placement bands;
- marks the champion after the final;
- creates durable reward grants once;
- claims rewards once and mutates arena Glory, Cinders, and Soulstones;
- uses configurable reward tiers with default v1 values.

Tournament matches do not consume arena tickets and do not mutate normal Colosseum rating.

### Background worker

Added API-hosted progression worker:

```text
LL/src/API/API.LL/HostedServices/TournamentProgressionHostedService.cs
```

The worker:

- runs periodically;
- creates upcoming tournaments;
- advances due tournaments;
- catches/logs exceptions so one failed tick does not permanently stop the worker;
- respects the configured `Colosseum:TournamentGrounds:Enabled` flag;
- uses scoped services correctly.

The worker is registered in:

```text
LL/src/API/API.LL/Program.cs
```

### Configuration

Added default options to:

```text
LL/src/API/API.LL/appsettings.json
```

Configuration section:

```json
"Colosseum": {
  "TournamentGrounds": {
    "Enabled": true,
    "ProgressionIntervalSeconds": 60,
    "UsePostgresAdvisoryLocks": true,
    "DefaultDefinitionKey": "daily-open-grounds",
    "DefaultName": "Daily Open Grounds",
    "DefaultDescription": "A daily asynchronous single-elimination Colosseum bracket.",
    "DefaultDailyRegistrationStartHourUtc": 18,
    "DefaultDailyRegistrationEndHourUtc": 20,
    "DefaultStartDelayAfterRegistrationMinutes": 5,
    "DefaultRoundIntervalMinutes": 10,
    "DefaultMinParticipants": 4,
    "DefaultMaxParticipants": 32,
    "DefaultMinimumCharacterLevel": 1,
    "DefaultMinimumArenaRating": null,
    "DefaultMinimumRankTier": null,
    "AllowWithdrawDuringRegistration": true,
    "RequireValidArenaDefenseSnapshot": false,
    "Rewards": [
      { "Key": "champion", "MaxPlacement": 1, "ArenaGlory": 120, "Cinders": 600, "Soulstones": 12 },
      { "Key": "finalist", "MaxPlacement": 2, "ArenaGlory": 80, "Cinders": 400, "Soulstones": 8 },
      { "Key": "semi-finalist", "MaxPlacement": 4, "ArenaGlory": 50, "Cinders": 250, "Soulstones": 5 },
      { "Key": "quarter-finalist", "MaxPlacement": 8, "ArenaGlory": 35, "Cinders": 175, "Soulstones": 3 },
      { "Key": "participant", "MaxPlacement": null, "ArenaGlory": 20, "Cinders": 100, "Soulstones": 2 }
    ]
  }
}
```

### CQRS and API

Added DTOs, commands, and queries under:

```text
LL/src/Core/Application/UseCases/Colosseum/Tournaments
```

Implemented operations:

- `GetTournamentGroundsStatusQuery`
- `GetTournamentDetailsQuery`
- `GetTournamentBracketQuery`
- `GetTournamentRewardsQuery`
- `GetTournamentMatchReplayQuery`
- `GetTournamentHistoryQuery`
- `GetTournamentHallOfFameQuery`
- `GetTournamentSeasonLeaderboardQuery`
- `RegisterTournamentCommand`
- `WithdrawTournamentRegistrationCommand`
- `ClaimTournamentRewardsCommand`

Added endpoints to `ColosseumController`:

```text
GET  colosseum/tournaments/status
GET  colosseum/tournaments/history
GET  colosseum/tournaments/hall-of-fame
GET  colosseum/tournaments/season-leaderboard
GET  colosseum/tournaments/{tournamentId}
GET  colosseum/tournaments/{tournamentId}/bracket
GET  colosseum/tournaments/{tournamentId}/matches/{matchId}/replay
GET  colosseum/tournaments/rewards
GET  colosseum/tournaments/{tournamentId}/rewards
POST colosseum/tournaments/{tournamentId}/register
POST colosseum/tournaments/{tournamentId}/withdraw
POST colosseum/tournaments/rewards/claim
POST colosseum/tournaments/{tournamentId}/rewards/claim
```

### Realtime

Added a generic realtime event:

```text
TournamentGroundsUpdated
```

The backend publishes best-effort updates for important tournament changes, including:

- registration updates;
- bracket generation;
- state changes;
- round resolution;
- completion;
- reward availability/claim-related refreshes.

The event payload includes tournament number/name, status, participant capacity, bracket presence, current round, next action time, and completion/cancellation times. The Angular realtime event registry recognizes the event, refreshes the Tournament Grounds panel when the current tournament changes, refreshes archives on completion/reward events, and shows the latest event metadata in the panel.

REST endpoints remain authoritative. Realtime is only a refresh hint.

### Angular frontend

Enabled the previously commented-out `Tournament Grounds` Colosseum tab.

Added TypeScript DTOs:

```text
LL/src/Presentation/ll/src/app/shared/models/Dtos/colosseum/tournamentGrounds.ts
```

Extended the Colosseum API service with:

- `getTournamentGroundsStatus`
- `getTournament`
- `getTournamentBracket`
- `registerTournament`
- `withdrawTournament`
- `getTournamentRewards`
- `getTournamentMatchReplay`
- `getTournamentHistory`
- `getTournamentHallOfFame`
- `getTournamentSeasonLeaderboard`
- `claimTournamentRewards`
- `startTournamentReplay`

Implemented the Tournament Grounds component:

```text
LL/src/Presentation/ll/src/app/features/game/city/colosseum/tournament-grounds
```

The UI now shows:

- current tournament summary;
- registration window;
- status;
- participant count and min/max requirements;
- player registration state;
- register and withdraw actions;
- bracket rounds and matches;
- byes and completed match outcomes;
- replay buttons for completed recorded matches;
- routeable replay page for completed tournament matches;
- unclaimed and claimed rewards;
- claim rewards action;
- player seed, entry rating, status, and final placement;
- recent player tournament results;
- player tournament history;
- champion archive / Hall of Fame;
- current-season tournament points leaderboard;
- latest realtime tournament event metadata;
- upcoming tournaments;
- refresh behavior from realtime events.

### Tests

Added focused unit tests:

```text
LL/tests/EssenceSystem.Tests/TournamentGroundsRulesTests.cs
LL/tests/EssenceSystem.Tests/TournamentGroundsServiceTests.cs
```

Covered:

- bracket size calculation;
- bye count calculation;
- round names;
- placement bands.

Service tests cover:

- registration creates a participant and immutable snapshot;
- duplicate same-account registration is rejected;
- withdrawal updates participant state and counts;
- under-filled tournaments cancel;
- bracket generation with byes is idempotent;
- full combat-driven tournament progression resolves matches, advances winners, records battle history, completes the bracket, and grants rewards;
- full combat-driven tournament progression stores and reads combat replay payloads;
- completed tournament progression populates player history and Hall of Fame results;
- completed tournament progression populates the computed current-season points leaderboard;
- rewards can only be claimed once.
- PostgreSQL advisory locking serializes concurrent registrations into a full tournament slot when `LL_TEST_TOURNAMENT_POSTGRES_CONNECTION` is provided.

Existing backend tests continue to pass.

## Partially Implemented

### Concurrency protection

The implementation uses:

- database transactions;
- re-read behavior;
- unique constraints for idempotency-sensitive rows;
- an injectable tournament lock service;
- best-effort PostgreSQL advisory transaction locks around tournament mutation/progression paths.

The advisory lock is skipped if the database provider does not support the PostgreSQL function, which keeps tests and local non-PostgreSQL providers usable. A provider-specific integration test now covers concurrent capacity registration when a PostgreSQL connection string is supplied through `LL_TEST_TOURNAMENT_POSTGRES_CONNECTION`.

### Replay and spectator experience

Completed tournament matches now persist full combat replay payloads and expose a replay endpoint. The Angular bracket links to a dedicated replay page, which loads tournament and bracket metadata and launches the existing Colosseum combat simulator for playback.

This is still participant/reward-recipient scoped. It does not include public replay sharing, public tournament viewing, replay browser filters, or admin-facing replay inspection.

### Realtime polish

Realtime refresh events exist and the frontend responds to them.

The event payload now includes useful tournament metadata and the frontend shows the latest update. It does not yet include full bracket deltas, per-match deltas, participant-specific reward information, or per-character targeted notifications.

### Backend test coverage

The deterministic tournament rules, core non-combat service flows, and a full combat-driven progression path have direct tests. The existing project suite passes.

The PostgreSQL concurrency test is intentionally environment-gated so regular local and CI runs do not require a database server. It creates a temporary schema, runs migrations, exercises the advisory lock, and drops the schema afterward when `LL_TEST_TOURNAMENT_POSTGRES_CONNECTION` is set.

### Reward design

Reward grants are durable and claim-once.

The reward tiers are now configurable through `Colosseum:TournamentGrounds:Rewards`, with default v1 values in code as a fallback.

Rewards are not yet stored per tournament definition in the database and cannot yet be edited from admin tooling.

### Tournament definition storage

The default tournament definition is created by service code when the scheduler first runs.

The schema supports definitions in the database, but no admin UI or JSON definition pipeline has been added for tournaments.

## Not Implemented Yet

### Live PvP

Not implemented:

- live PvP rooms;
- ready checks;
- player disconnect handling;
- manual match actions;
- forfeits triggered by players;
- best-of-three matches.

This is intentional for v1.

### Alternative formats

Not implemented:

- Swiss tournaments;
- round-robin tournaments;
- team tournaments;
- guild tournaments;
- rating-bracketed tournaments;
- rank-restricted special events beyond basic definition fields.

Only single-elimination is implemented.

### Admin tooling

Not implemented:

- admin tournament creation UI;
- admin cancellation controls;
- admin bracket repair tools;
- admin reward editing;
- tournament definition editor.

### Hall of Fame and long-term history

Implemented:

- player tournament history endpoint and panel;
- recent champion archive / Hall of Fame endpoint and panel;
- computed current-season tournament points leaderboard endpoint and panel.

Not implemented:

- seasonal Hall of Fame;
- durable tournament season records;
- admin-configurable point rules;
- title/banner/cosmetic entitlement grants.

The player panel also shows recent tournaments for the current character, including status, placement, completion/cancellation time, reward state, and replay counts.

### Battle history and replay integration

Tournament match completion now creates a `ColosseumMatchResult` row using the tournament match ID as the stable history ID. Tournament matches do not mutate arena rating or glory in that history record.

Tournament match completion also stores the full replay payload in `TournamentCombatReplays`, and the bracket UI links to a dedicated replay page for completed matches.

Not implemented:

- public replay links;
- replay search/filter tooling.

### Advanced eligibility and validation copy

Implemented eligibility checks cover the basic definition fields and account/character duplicate rules.

Not fully implemented:

- deleted/banned/invalid character checks;
- rich machine-readable failure codes;
- all detailed frontend unavailable-reason variants from the original design.

### Data-driven rewards and schedules

Partially implemented:

- default tournament definition key/name/description are configurable;
- default registration hours, eligibility, min/max participants, start delay, and round interval are configurable;
- reward tiers are configurable in API configuration.

Not implemented:

- multiple tournament definitions with different schedules;
- weekly/special event definitions;
- reward tables stored as database content;
- entry fees;
- refunds.

### Strong distributed locking

Partially implemented:

- PostgreSQL advisory transaction locking is attempted inside tournament transactions.
- tournament locking is behind `ITournamentLockService`.
- provider-gated PostgreSQL concurrency coverage verifies that two concurrent registrations cannot overfill a one-slot tournament.

Not implemented:

- explicit `FOR UPDATE` repository method;
- CI-backed PostgreSQL concurrency test execution.

## Verification Performed

Backend build:

```text
dotnet build LL\LegendsLegacy.sln
```

Result:

```text
Build succeeded.
```

Backend tests:

```text
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

Result:

```text
Passed: 232
Failed: 0
Skipped: 0
```

Frontend build:

```text
node_modules\.bin\ng.cmd build
```

Result:

```text
Application bundle generation complete.
```

The Angular build still reports the existing initial bundle budget warning.

PostgreSQL concurrency integration coverage:

```text
LL_TEST_TOURNAMENT_POSTGRES_CONNECTION=<connection-string> dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

When the connection string is not set, the provider-specific test exits without touching PostgreSQL.

## Verification Notes

The local `npm` shim in this environment points to a missing global npm CLI, so `npm run build` could not be used directly.

Frontend verification was done by temporarily installing dependencies with the bundled `pnpm` runtime and invoking the local Angular CLI. Generated `node_modules`, `dist`, and pnpm workspace artifacts were removed afterward.

## Migration and Deployment Implications

The migration must be applied before using Tournament Grounds:

```text
20260627110535_AddTournamentGrounds
20260627121312_AddTournamentCombatReplays
```

The API now starts a hosted tournament progression worker. If the backend runs multiple API instances, each instance may execute the worker. The current implementation is designed to be retry-safe through transactions, unique constraints, and PostgreSQL advisory transaction locks. Non-PostgreSQL providers skip the advisory lock path.

No infrastructure-as-code changes were made.

## Suggested Next Steps

1. Move tournament definitions and reward tables into admin/content-managed database records.
2. Add public-safe replay sharing and a replay browser/search view.
3. Add admin scheduling, cancellation, and bracket repair tools.
4. Add durable season records, configurable point rules, and title/banner rewards.
5. Promote the PostgreSQL concurrency test to CI once a disposable test database is available.
