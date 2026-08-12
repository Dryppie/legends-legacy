# Legacy's Ascension — World Tower implementation status

Last updated: 2026-08-12

This document is the implementation source of truth for Legacy's Ascension. It translates the original design into the system that exists in the repository and tracks what is shipped, partial, or not implemented.

The original design document remains useful for product intent, but this file takes precedence when it records a later product decision or an intentional implementation change.

## Status definitions

| Status              | Meaning                                                                                                                          |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| **Shipped**         | Implemented in the current repository and covered by the listed verification. This does **not** mean deployed to an environment. |
| **Partial**         | A usable implementation exists, but part of the intended behavior, integration, UX, or test coverage is still missing.           |
| **Not implemented** | No production implementation currently exists.                                                                                   |
| **Deferred**        | Intentionally outside the current 10-floor MVP.                                                                                  |

## Current product decisions

These decisions supersede conflicting details in the original proposal:

1. **Power rating is advisory only.** Floors expose a recommended power rating. There is no minimum power rating and it never blocks rally creation, joining, or starting.
2. **Floor detail lives in the Tower overview.** Selecting a floor changes the overview workspace and its Scouting, Preparation, and Rally tabs. The separate `/game/world/tower/floors/:floorNumber` page was removed because it duplicated the overview.
3. **Existing character snapshots are reused.** Tower participants reference the game's normalized `CharacterSnapshot` model rather than introducing a Tower-only build snapshot or serialized snapshot blob.
4. **Hall of Fame records are derived.** The first-clear attempt referenced by `TowerFloorProgress` is the immutable source for Hall of Fame rows. Separate clear-record and clear-participant models would duplicate attempt and roster data.
5. **Server state is derived.** Progression, the current floor, Echo availability, and floor lifecycle states are computed from persisted floor progress rather than stored in a redundant `TowerServerState` aggregate.
6. **Scouting and preparation share one contribution pipeline.** Contribution kind distinguishes research and preparation effects, avoiding parallel command and persistence implementations.
7. **Attempt reports are embedded in rally detail.** A report API is available, but a separate attempt-report route is not required for the MVP UI.
8. **Rally membership uses applications.** A non-leader submits a locked build for review; only the rally leader can accept or decline it. A roster slot is occupied only after acceptance.
9. **Realtime is a delivery channel, not a second state store.** Rally mutations use durable world refresh events; combat frames are sent only to authenticated rally-character groups and recover from a persisted playback timeline through REST.

## Executive status

| Area                      | Status              | Current result                                                                                                                                                                                                                                                                  |
| ------------------------- | ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Ten-floor catalog         | **Shipped**         | Floors 1–10 are JSON-driven and validated at startup.                                                                                                                                                                                                                           |
| Server progression        | **Shipped**         | Sequential unlocks, first-clear state, and current-floor derivation are implemented.                                                                                                                                                                                            |
| Rally lifecycle           | **Shipped**         | Create, apply, accept, decline, withdraw, leave, cancel, readiness, start, and completion flows are implemented.                                                                                                                                                                |
| Locked builds             | **Shipped**         | Applying captures the existing immutable character snapshot; acceptance promotes that same snapshot into the roster.                                                                                                                                                            |
| Tower combat              | **Shipped**         | Full rallies run through the existing combat engine, persist a battle report and deterministic timeline, and play at ten ticks per real-time second.                                                                                                                            |
| Scouting                  | **Shipped**         | Failed-attempt progress, manual research, reveal thresholds, and weekly limits are implemented.                                                                                                                                                                                 |
| Preparation               | **Shipped**         | Three contribution kinds affect the next attempt, have configurable weekly limits, and reject points that cannot increase an already-maxed bonus.                                                                                                                               |
| Echo Mode                 | **Shipped**         | Floor 5 unlock, cleared-floor eligibility, weekly reward lockout, and Cinder rewards are implemented.                                                                                                                                                                           |
| Hall of Fame              | **Shipped**         | First-clear records, roster, guild names, attempt number, duration, and clear time are displayed.                                                                                                                                                                               |
| Overview UI               | **Shipped**         | Tower summary, floor rail, floor workspace, rallies, rewards, unlocks, and recent clears use the existing game design system.                                                                                                                                                   |
| Rally UI                  | **Shipped**         | Apply/withdraw, leader approval controls, leave, start, roster, readiness, and result views are implemented.                                                                                                                                                                    |
| Hall of Fame UI           | **Shipped**         | A full first-server-clear table is available.                                                                                                                                                                                                                                   |
| Server unlock effects     | **Partial**         | Unlock keys are persisted, but most downstream game systems do not consume them yet.                                                                                                                                                                                            |
| Rewards and prestige      | **Partial**         | First-clear and Echo Cinders are granted; titles, cosmetics, and other prestige rewards are not implemented.                                                                                                                                                                    |
| Guardian mechanics        | **Partial**         | Guardians use existing creatures, scaling, tags, art, and scouting descriptions; bespoke authored phase/mechanic execution is not implemented.                                                                                                                                  |
| Hall combat statistics    | **Partial**         | Battle reports contain combat data, but the Hall table does not surface richer performance metrics.                                                                                                                                                                             |
| Automated coverage        | **Partial**         | Catalog, domain invariants, application approval, realtime delivery, rally rules, combat outcomes, progression, Echo, Hall projection, controller dispatch, and indexes have focused tests; PostgreSQL concurrency, HTTP integration, and frontend flows need broader coverage. |
| Frontend automated tests  | **Partial**         | The Angular API client has route/payload mapping tests and the complete Angular development build passes; component, browser, and full-suite execution coverage remain.                                                                                                         |
| Live asynchronous updates | **Shipped**         | Every rally lifecycle mutation queues a durable outbox event and broadcasts it through the existing authenticated SignalR channel; overview and rally clients refresh their authorized server projections automatically.                                                        |
| Real-time combat playback | **Partial**         | The engine captures 10-tick frames, the timeline is durable, participant-scoped SignalR sends one due frame per second, REST supports refresh recovery, the shared combat viewer updates live, and rewards finalize only on the scheduled final frame. Multi-instance leasing, interpolation, metrics, cleanup, and browser fake-time coverage remain. |
| Database rollout          | **Partial**         | Three EF Core migrations exist but have not been applied by this implementation task.                                                                                                                                                                                           |

## Shipped implementation

### Content and server initialization

- A data-driven catalog defines ten contiguous floors.
- Each floor defines its guardian creature, title, tier, rally size, recommended rating, strength multiplier, art, tags, scouting reveals, Cinder rewards, Echo eligibility, and server unlock keys.
- Guardian creature references are validated against the existing creature catalog.
- The initial server state is created lazily: Floor 1 is unlocked and later floors remain locked.
- Existing progress is preserved when definitions are loaded; newly defined floors can be initialized without replacing prior progress.
- Floor lifecycle is derived as `Locked`, `Sealed`, `Scouting`, `Rallying`, or `Cleared`.

### Persistence and constraints

The migration `20260811185743_AddWorldTowerMvp` introduces:

- `TowerFloorProgress`
- `TowerRally`
- `TowerRallyParticipant`
- `TowerAttempt`
- `TowerContribution`
- `TowerEchoClear`
- `ServerUnlock`

The follow-up migration `20260812081251_AddWorldTowerRallyApplications` introduces `TowerRallyApplication` and its snapshot, status, resolution, character, account, and rally constraints.

The migration `20260812103046_AddWorldTowerCombatPlayback` introduces the one-to-one durable combat timeline, playback schedule, and dispatch cursor.

Important database invariants include:

- one progress row per server and floor;
- at most one first-clear attempt per floor;
- at most one attempt per rally;
- one character and one account slot per rally;
- one application per character/account and rally;
- one rewarded Echo clear per server, floor, character, and week;
- one row per server unlock key.

### Rally and participation rules

- `FirstClear` and `Echo` rally modes are supported.
- A rally creator is added to the roster immediately and receives a locked character snapshot.
- Other characters apply with a locked snapshot; applying does not occupy a roster slot.
- Only the leader can accept or decline a pending application.
- Acceptance reuses the submitted snapshot and promotes the applicant into the roster.
- Applicants can withdraw while the rally remains open; filling the roster declines the remaining pending applications.
- An account can occupy only one slot in a rally.
- A character can participate in only one active rally.
- Rally size comes from the selected floor definition.
- A full roster becomes ready; only the leader can start it.
- Starting requires every slot to be filled.
- A non-leader can leave and reopen a recruiting rally.
- A leader leaving cancels their rally.
- Ownership, applications, and membership are evaluated from the authenticated character/account, including guest accounts that otherwise meet participation rules.
- Recommended power contributes to readiness labels and warnings but is not an eligibility gate.

### Combat and outcomes

- Tower combat rehydrates the locked character snapshots, rather than current mutable loadouts.
- Guardian combatants reuse the existing creature and combat systems with Tower scaling.
- Preparation modifiers are applied to player power, penetration, and guardian power as appropriate.
- Attempts persist status, timing, success/failure, duration, and serialized battle report data.
- A successful first clear records the immutable first-clear attempt, sets scouting to 100%, unlocks the next floor, records server unlock keys, and grants the configured Cinders.
- A failed first-clear attempt grants scouting progress for the configured number of weekly failures.
- A successful Echo attempt grants its configured Cinders only to participants who have not already received that floor's reward during the current week.
- Attempt failures are persisted as errored/completed instead of leaving a rally indefinitely in progress.

### Transactions and concurrency

- Normal commands use the application's command transaction behavior.
- starting a rally is intentionally marked non-transactional at the command-pipeline level and owns short transaction boundaries around state transitions, combat, and outcome persistence;
- PostgreSQL advisory locks serialize mutations for a server/floor pair;
- application acceptance also locks the applicant character before the floor, preserving the established lock order across concurrent commands;
- unique constraints prevent duplicate first clears, duplicate roster membership, duplicate attempt creation, and duplicate Echo rewards.

This split avoids running the combat simulation inside one long database transaction and prevents the nested-transaction error observed during rally join/leave/start testing.

### Scouting

- Failed first-clear attempts add configurable scouting progress for the first three failures each week by default.
- Manual `Research` contributions increase scouting progress.
- Manual research has a configurable weekly per-character cap.
- Scouting progress is clamped from 0 to 100.
- Data-driven reveals become visible at their configured thresholds.
- Clearing a floor completes its scouting progress.

### Preparation

The contribution pipeline supports:

- `SupplyWeapons` — increases rally damage;
- `InscribeWards` — reduces Guardian damage;
- `ScoutWeakPoints` — increases armor and magic penetration.

Preparation has a configurable weekly per-character cap, per-point effect, and maximum effect. Contributions are rejected before persistence if a bonus is maxed or if any requested points would exceed its remaining useful capacity, so weekly allowance is not wasted. Completed scouting likewise rejects further research. Current contributions do not consume a separate item or currency.

### Echo Mode

- Echo Mode is derived from Floor 5 being cleared.
- Echo rallies may target cleared floors whose definitions permit Echo attempts.
- First-clear records and progression cannot be modified by Echo attempts.
- Weekly reward eligibility is enforced per character and floor.

### Hall of Fame

- The first successful `FirstClear` attempt becomes the floor's permanent clear source.
- Records are derived from the attempt and its locked participants, preventing a second mutable Hall roster.
- The API and UI expose floor, guardian, roster, guild names, attempt number, duration, and cleared time.
- The overview includes recent clears and the dedicated page displays the full table.

### Frontend

Routes:

- `/game/world/tower`
- `/game/world/tower/rallies/:rallyId`
- `/game/world/tower/hall-of-fame`

The Tower screens use the application's existing `ll-*` surfaces, buttons, badges, navigation tabs, typography, spacing, and responsive layout. The overview contains:

- server progression summary;
- scrollable floor ascent rail;
- selected-floor identity, state, rally size, recommended rating, scouting level, rewards, and unlocks;
- Guardian intelligence and scouting reveals;
- Scouting, Preparation, and Rally tabs;
- active rallies and rally creation;
- recent Hall of Fame records.

The overview suppresses rally-creation actions while the current character belongs to an active rally and offers a direct **View your rally** action instead. Active rally summaries identify their leader. Scouting shows the current character's weekly research usage and clearly disables contributions when the cap is reached. The rally page contains application/withdrawal actions, leader accept/decline controls, readiness, roster slots, start controls, and the resulting battle report. Accepted applications become withdrawn when their participant leaves; the same application record can then be resubmitted with a fresh locked build. Roster, applicant, and battle-report character names reuse the shared player menu for profile viewing and whispers. The dedicated Hall page contains the complete first-clear table.

Local Development exposes a leader-only **Fill with test roster** action on recruiting rallies. It fills open slots from the seeded local guest pool using real character snapshots and the normal power-rating path, then marks the rally ready so the standard start, combat, reward, progression, and SignalR flows can be tested without signing into helper accounts. Both the ASP.NET Development environment and `FeatureManagement:WorldTowerDevelopmentTools` are required. Local launch profiles enable the tool and seed twenty helper accounts; non-Development hosts return `404` for the hidden endpoint.

Completed Tower attempts persist the authoritative engine combat result. Rally participants can open the final detail after the paced playback completes or later after refreshing the rally page. The detailed view reuses the standard team, combatant, aggregate-stat, and per-ability breakdown UI; non-participants cannot retrieve the result.

Tower attempts now enter `Playback` after fast deterministic resolution. The engine captures tick 0, every 10 ticks, and an exact partial final frame. The API stores those frames separately from ordinary attempt reads, returns the current participant-authorized snapshot for refresh recovery, and withholds the completed report/result until terminal status. A hosted dispatcher derives the due frame from server time, sends only the newest due frame to all roster-character SignalR groups, and runs the existing reward/progression transaction at the scheduled final frame. The Angular rally screen enters the shared combat viewer, deduplicates frames by sequence, updates combatants and cumulative stats, can leave/re-enter the live view, and transitions to the existing final report.

Rally creation, application submission, acceptance, decline, withdrawal, member departure, cancellation, start, victory, defeat, and combat error all emit `WorldTowerRallyUpdated` after the associated state is committed. The existing outbox worker delivers these events through SignalR. Payloads contain identifiers, status, and aggregate counts only; each client refreshes the REST resource to receive the projection it is authorized to see.

### API surface

All endpoints require authentication.

| Method | Route                                                                        | Purpose                              |
| ------ | ---------------------------------------------------------------------------- | ------------------------------------ |
| `GET`  | `/api/v1/world-tower`                                                        | Tower overview and recent clears     |
| `GET`  | `/api/v1/world-tower/floors/{floorNumber}`                                   | Selected-floor workspace data        |
| `GET`  | `/api/v1/world-tower/rallies/{rallyId}`                                      | Rally, roster, readiness, and result |
| `GET`  | `/api/v1/world-tower/attempts/{attemptId}/report`                            | Persisted battle report              |
| `GET`  | `/api/v1/world-tower/hall-of-fame`                                           | Full first-clear table               |
| `POST` | `/api/v1/world-tower/rallies`                                                | Create a FirstClear or Echo rally    |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/applications`                         | Apply with the current locked build  |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/applications/{applicationId}/accept`  | Leader accepts an application        |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/applications/{applicationId}/decline` | Leader declines an application       |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/leave`                                | Leave or cancel the rally            |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/start`                                | Run the locked-roster attempt        |
| `POST` | `/api/v1/world-tower/floors/{floorNumber}/contributions`                     | Contribute research or preparation   |

## Partial implementation

### Server unlock consumption

First clears persist data-driven unlock keys, including Echo-related and future content keys. The Tower API exposes the keys, but downstream shops, encounters, bands, or other feature systems do not yet query `ServerUnlock`. Until consumers are added, most keys are durable progression markers rather than active content switches.

### Rewards and prestige

Cinder rewards are complete for first clears and weekly Echo clears. The broader reward proposal remains partial:

- no Tower-specific title grant;
- no cosmetic reward delivery;
- no reward mail/fallback flow;
- no contribution or role awards;
- no dedicated Tower currency.

These omissions avoid new reward models until concrete rewards and existing integration points are chosen.

### Guardian mechanics

Guardian identity is data-driven, and combat uses existing creature definitions with configurable strength. Tags and scouting reveals communicate intended mechanics. The combat layer does not yet interpret a Tower-specific phase/mechanic definition, scripted threshold events, unique enrage behavior, or floor-authored ability sequence.

### Hall of Fame depth

The first-clear record and roster are complete. Richer statistics such as total damage, healing, deaths, or role awards remain available only where represented in the attempt report and are not promoted to the Hall table. Cross-server and seasonal views are not part of the MVP.

### User experience notifications

Rally state is server-authoritative and updates live through SignalR-triggered REST refreshes. There are no Tower-specific chat announcements or durable notification-inbox entries yet.

### Automated and manual coverage

Focused tests currently cover:

- the ten-floor contiguous catalog and existing guardian references;
- valid rally sizes, strength values, recommended ratings, and unlock keys;
- immutable first-clear recording and completion of scouting;
- scouting clamping and negative-input rejection;
- the non-transactional start-command regression;
- below-recommendation rally creation and readiness warnings;
- snapshots remaining unchanged after the source character changes;
- a distinct guest account applying successfully and occupying no slot before acceptance;
- leader-only acceptance/decline, applicant withdrawal, and promotion of the locked application snapshot;
- durable outbox routing and world-audience SignalR delivery for rally state changes;
- same-account duplicate-slot and one-active-rally rejection;
- member leave, leader cancellation, and pre-combat start authorization;
- separate research/preparation caps and preparation math;
- Echo rally unlock eligibility;
- Hall of Fame projection from the first-clear attempt and locked roster;
- concurrency-sensitive unique indexes in the EF Core model;
- successful first-clear reports, Cinder grants, server unlocks, next-floor progression, and Hall creation;
- failed-attempt scouting and its weekly attempt cap;
- weekly per-character Echo reward lockouts across repeat clears;
- combat failure reports and combat-exception persistence;
- mixed Echo rosters rewarding only members who remain weekly-eligible;
- supply, ward, and weak-point preparation values reaching the friendly and hostile combat runtimes;
- every controller endpoint dispatching the authenticated character and route/body payloads;
- missing-character claim rejection and controller authorization metadata;
- Angular API-client route and mutation payload mapping, including the absence of a minimum-power field.

Coverage still needed:

- hosted HTTP request/response, model-binding, and authentication-middleware integration;
- same-account and one-active-rally constraints against PostgreSQL under concurrency;
- equipment and essence snapshot immutability after a player changes their build;
- first-clear race and duplicate-clear handling against PostgreSQL;
- Angular overview/rally/Hall component behavior and end-to-end flows with multiple accounts.

### Database rollout

The migration and model snapshot are present. The migration has not been applied to a shared or production database, and no service has been deployed as part of this work.

## Not implemented

These items were proposed or identified during implementation but are not in the current feature:

| Item                              | Status              | Notes                                                                                                                                                                       |
| --------------------------------- | ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Practice attempt mode             | **Not implemented** | Optional in the original proposal; no enum value, rules, rewards, or UI.                                                                                                    |
| Entrance keys                     | **Not implemented** | No inventory key requirement or consumption.                                                                                                                                |
| Rally expiration/cleanup          | **Not implemented** | Recruiting rallies do not have a scheduled expiration worker.                                                                                                               |
| Notifications/chat announcements  | **Not implemented** | No integration for rally or first-clear announcements.                                                                                                                      |
| Bespoke Guardian phase engine     | **Not implemented** | Existing combat and creature mechanics are reused.                                                                                                                          |
| Tower titles/cosmetics            | **Not implemented** | Cinders are the only delivered Tower reward.                                                                                                                                |
| Active downstream unlock switches | **Not implemented** | Unlock keys are persisted but not generally consumed.                                                                                                                       |
| Operational telemetry dashboard   | **Not implemented** | Existing logging is used; no Tower-specific metrics/dashboard.                                                                                                              |
| Real-time combat hardening        | **Partial**         | Durable paced playback exists; separately leased simulation, multi-instance dispatcher claims, interpolation, telemetry, retention cleanup, and fake-time browser coverage remain. |

## Deferred beyond the MVP

- Floors 11–100 and 100-player floors
- the Basement and downward progression
- simultaneous split-front encounters
- real-time synchronous combat participation
- advanced role profiles and role quotas
- MVP/contributor awards
- seasonal or cross-server leaderboards
- procedural Guardians
- recurring server events built on Tower progress

These are expansion hooks, not blockers for the current ten-floor MVP.

## Acceptance tracker

| Requirement                                              | Status      | Evidence or remaining work                                                                                                                                         |
| -------------------------------------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Initialize Floor 1 and lock later floors                 | **Shipped** | Lazy initialization and sequential floor progress.                                                                                                                 |
| Preserve server progress across requests                 | **Shipped** | Persisted `TowerFloorProgress`.                                                                                                                                    |
| Clear exactly once and unlock the next floor             | **Shipped** | First-clear reference, unique constraint, and floor advisory lock.                                                                                                 |
| Create, apply, approve, leave, cancel, and start rallies | **Shipped** | Service commands, API endpoints, approval-aware overview/rally UI, and locked application snapshots.                                                               |
| Enforce one account per rally slot                       | **Shipped** | Account-aware service rule and unique index.                                                                                                                       |
| Enforce one active rally per character                   | **Shipped** | Service validation.                                                                                                                                                |
| Do not enforce a minimum rating                          | **Shipped** | Only `RecommendedPowerRating` remains in definitions, DTOs, service logic, and UI.                                                                                 |
| Lock character builds on application                     | **Shipped** | Existing `CharacterSnapshot` relation is captured at application time and reused on acceptance.                                                                    |
| Update rally state live                                  | **Shipped** | Transactional outbox events broadcast through SignalR and trigger authorized REST refreshes for all rally lifecycle states.                                        |
| Resolve combat from locked builds                        | **Shipped** | Snapshot rehydration and existing combat engine.                                                                                                                   |
| Persist an attempt report                                | **Shipped** | Attempt timing, result, and serialized report.                                                                                                                     |
| Create one permanent first-clear record                  | **Shipped** | Derived from floor progress and the first-clear attempt.                                                                                                           |
| Award scouting for early weekly failures                 | **Shipped** | Configurable gain and weekly failure cap.                                                                                                                          |
| Support manual scouting with a weekly cap                | **Shipped** | Research contributions.                                                                                                                                            |
| Apply preparation to attempts                            | **Shipped** | Three preparation kinds and configurable cap/effect.                                                                                                               |
| Unlock Echo after Floor 5                                | **Shipped** | Derived from Floor 5 clear.                                                                                                                                        |
| Enforce weekly Echo rewards                              | **Shipped** | Unique weekly Echo clear and eligible-participant rewards.                                                                                                         |
| Deliver first-clear and Echo Cinders                     | **Shipped** | Character currency updates.                                                                                                                                        |
| Activate all server content unlocks                      | **Partial** | Keys persist; downstream consumers remain.                                                                                                                         |
| Show overview, floor details, rallies, reports, and Hall | **Shipped** | Floor detail is intentionally integrated into overview; reports appear in rally detail.                                                                            |
| Provide comprehensive automated coverage                 | **Partial** | Twenty-six targeted Tower/realtime/outbox .NET tests and the Angular build pass; HTTP integration, PostgreSQL concurrency, component, and browser coverage remain. |
| Complete production rollout                              | **Partial** | Migration application and deployment are external operational steps.                                                                                               |

## Configuration

`WorldTower` settings are bound and validated at API startup:

| Setting                               | Current default | Purpose                                     |
| ------------------------------------- | --------------: | ------------------------------------------- |
| `ServerId`                            |       `default` | Progression partition key                   |
| `EchoModeUnlockFloor`                 |             `5` | Floor whose clear enables Echo Mode         |
| `FailedAttemptScoutingGain`           |            `10` | Scouting gained per eligible failed attempt |
| `FailedAttemptScoutingWeeklyCap`      |             `3` | Weekly failed attempts that grant scouting  |
| `ManualScoutingWeeklyCapPerCharacter` |            `10` | Weekly manual research limit                |
| `PreparationWeeklyCapPerCharacter`    |            `10` | Weekly preparation limit                    |
| `PreparationPercentPerPoint`          |          `0.25` | Effect percentage per contribution point    |
| `PreparationMaxEffectPercent`         |             `5` | Maximum effect for each preparation kind    |

Balance and content changes should prefer options or floor JSON over code edits.

## Key reuse and redundancy decisions

| Original concept                              | Current implementation                                                    | Reason                                                                           |
| --------------------------------------------- | ------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `TowerBuildSnapshot` with JSON payloads       | Existing `CharacterSnapshot` and its normalized equipment/essence data    | Preserves one canonical snapshot implementation and supports combat rehydration. |
| `TowerClearRecord` and participant rows       | `TowerFloorProgress.FirstClearAttemptId` plus `TowerAttempt` participants | Avoids duplicating immutable attempt and roster data.                            |
| `TowerServerState`                            | Derived from all `TowerFloorProgress` rows                                | Prevents current-floor and Echo flags from drifting out of sync.                 |
| Persisted floor lifecycle state               | Derived state                                                             | State follows unlock, scouting, rally, and clear facts automatically.            |
| Separate scouting/preparation APIs and models | `TowerContribution` with a contribution kind                              | One weekly-cap and aggregation pipeline serves both systems.                     |
| Tower-only combat implementation              | Existing creature, snapshot, stats, and combat services                   | Keeps Tower orchestration focused on roster, scaling, and outcomes.              |
| Dedicated floor page                          | Selected-floor workspace in overview                                      | Removes duplicate UI and keeps the main Tower flow in one place.                 |
| Separate attempt report page                  | Report within rally detail plus report API                                | Avoids an extra route while retaining direct API access.                         |

## Verification record

Verified on 2026-08-12:

- `dotnet build` for `API.LL` passed with zero warnings and zero errors in the final incremental build.
- Angular development build passed using the bundled workspace Node runtime.
- Targeted World Tower, realtime, controller, playback-finalization, and checkpoint tests passed: 25/25.
- The rally application tests cover applicant/leader projections, authorization, withdrawal, acceptance, participant promotion, and emitted state events.
- The realtime consumer test verifies durable event deserialization and world-audience SignalR publication; the outbox registry test verifies routing.
- The targeted Karma run is currently blocked before execution by an unrelated existing `guild-info.component.spec.ts` fixture that is missing `rolePermissions` and `vaultItems`.
- Repository search confirmed that no minimum-power property or gate remains in the Tower implementation.

The current status document itself should be updated whenever a Tower item moves between **Not implemented**, **Partial**, and **Shipped**. A status should only move to **Shipped** after its production code exists and relevant verification passes.

## Recommended next work

1. Harden real-time playback with PostgreSQL multi-instance leasing, bounded reconnect ranges, interpolation, metrics, retention, and fake-time browser coverage.
2. Add PostgreSQL integration tests for rally concurrency and first-clear races, followed by hosted HTTP integration tests.
3. Add Angular component tests and a two-account browser flow covering create, apply, accept/decline, withdraw/leave, start, realtime refresh, and report rendering.
4. Choose the first real consumers for `ServerUnlock` and implement those integrations before adding more unlock keys.
5. Decide concrete prestige rewards, then reuse existing title/cosmetic delivery systems instead of adding speculative Tower reward tables.
6. Add rally expiration and durable notification/chat behavior when the asynchronous UX requires it.

## Implementation file map

| Layer              | Primary paths                                                                                                                                                |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Static content     | `LL/src/API/API.LL/Data/world-tower/tower-floors.json`                                                                                                       |
| API                | `LL/src/API/API.LL/Controllers/V1/WorldTowerController.cs`                                                                                                   |
| Application        | `LL/src/Core/Application/Interfaces/Services/LL/WorldTower/`, `LL/src/Core/Application/UseCases/WorldTower/`                                                 |
| Domain             | `LL/src/Core/Domain/Models/WorldTower/`                                                                                                                      |
| Service            | `LL/src/Infrastructure/Service/Services.LL/WorldTower/`                                                                                                      |
| Persistence        | `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/WorldTower/`                                                                                |
| Migrations         | `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260811185743_AddWorldTowerMvp.cs`, `20260812081251_AddWorldTowerRallyApplications.cs`, `20260812103046_AddWorldTowerCombatPlayback.cs` |
| Realtime delivery  | `LL/src/Infrastructure/Service/Services.LL/Outbox/RealtimeWorldTowerGameEventOutboxConsumer.cs`, `LL/src/API/API.LL/HostedServices/WorldTowerCombatPlaybackWorker.cs` |
| Angular API client | `LL/src/Presentation/ll/src/app/core/services/api/world-tower/`                                                                                              |
| Angular screens    | `LL/src/Presentation/ll/src/app/features/game/world/tower/`                                                                                                  |
| Tests              | `LL/tests/EssenceSystem.Tests/WorldTowerTests.cs`, `WorldTowerServiceTests.cs`, `WorldTowerControllerTests.cs`; Angular client `world-tower.service.spec.ts` |

## Change log

### 2026-08-12

- Replaced direct rally joining with locked-build applications.
- Added leader-only application acceptance and decline plus applicant withdrawal.
- Reused the application snapshot when an accepted character is promoted into the roster.
- Added account/application uniqueness constraints and an EF Core migration; it was generated but not applied.
- Added durable outbox events for every rally lifecycle mutation and world-audience SignalR delivery.
- Added automatic realtime refreshes to both the Tower overview and rally detail screens without exposing applicant details in broadcast payloads.
- Added approval workflow, outbox routing, realtime consumer, and contribution-saturation tests; 26 targeted tests pass.
- Prevented maxed or overflowing preparation contributions and completed-scouting research from consuming weekly contribution allowance; the UI disables and labels unavailable actions.
- Hid rally-creation actions for characters already in an active rally and replaced them with a direct link to their existing rally.
- Added the leader's character name to active rally summaries.
- Reused the shared character tag for rally roster, application, and battle-report names, enabling profile and whisper actions.
- Added visible weekly research usage, a limit-reached notice, and cap-aware research controls to scouting.
- Fixed accepted members leaving a rally so their application is withdrawn, hidden from the active state, and safely reusable for reapplication with a fresh locked build.
- Added a Development-only one-click test-roster filler backed by twenty seeded local guest characters and real locked snapshots.
- Exposed persisted Tower combat results to rally participants and integrated them with the shared combat detail viewer.
- Planned restart-safe real-time combat playback with authoritative checkpoints every 10 ticks and one-second participant-scoped delivery.
- Implemented the first end-to-end real-time playback slice: reusable engine checkpoints, durable timelines, server-clock recovery, participant-only SignalR frames, paced dispatch, deferred/idempotent outcome finalization, and incremental shared-viewer updates.
- Added deterministic checkpoint parity/boundary tests and updated Tower outcome tests to assert that rewards are absent until the playback deadline.
- Verified the full Angular development build.

### 2026-08-11

- Implemented the ten-floor World Tower MVP across domain, persistence, service, API, and Angular presentation layers.
- Reworked the Tower UI to use the game's established design system.
- Integrated floor detail into the overview and removed the redundant floor route/component.
- Reused existing character snapshots, creature combat, and design-system components.
- Derived Hall of Fame and server state instead of adding redundant persisted models.
- Corrected rally transaction ownership so join/leave/start flows do not create nested transactions.
- Explicitly register joined participants as new EF Core rows, fixing the optimistic concurrency failure when a second account joins a rally.
- Added account-aware join/leave behavior and leader rally cancellation.
- Removed minimum power rating entirely; recommended rating remains informational.
- Added focused service tests for rally rules, guest participation, snapshots, contributions, Echo eligibility, Hall projection, and unique persistence indexes.
- Added deterministic combat tests for first-clear progression/rewards, failed-attempt scouting caps, weekly Echo rewards, reports, and errored attempts.
- Added mixed-roster Echo eligibility and preparation-to-combat-runtime modifier tests.
- Added controller-boundary tests for authenticated character dispatch, request payload mapping, and authorization metadata.
- Added Angular API-client tests for all Tower read and mutation routes; isolated compilation passes, while the full Karma target remains blocked by an unrelated Guild test fixture.
- Added this living implementation-status document.
