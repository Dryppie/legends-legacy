# Legacy's Ascension — World Tower implementation status

Last updated: 2026-08-13

This document is the implementation source of truth for Legacy's Ascension. It translates the original design into the system that exists in the repository and tracks what is shipped, partial, or not implemented.

The original design document remains useful for product intent, but this file takes precedence when it records a later product decision or an intentional implementation change.

Player-facing terminology uses **Expedition**. Existing `TowerRally*` domain types, persisted tables, API routes, and realtime event names remain internal compatibility contracts and are intentionally not renamed.

## Status definitions

| Status              | Meaning                                                                                                                          |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| **Shipped**         | Implemented in the current repository and covered by the listed verification. This does **not** mean deployed to an environment. |
| **Partial**         | A usable implementation exists, but part of the intended behavior, integration, UX, or test coverage is still missing.           |
| **Not implemented** | No production implementation currently exists.                                                                                   |
| **Deferred**        | Intentionally outside the current 10-floor MVP.                                                                                  |

## Current product decisions

These decisions supersede conflicting details in the original proposal:

1. **Power rating is advisory only.** Floors expose a recommended power rating. There is no minimum power rating and it never blocks Expedition creation, joining, or starting.
2. **Floor detail lives in the Tower overview.** Selecting a floor changes the overview workspace and its Scouting, Preparation, and Expedition tabs. The separate `/game/world/tower/floors/:floorNumber` page was removed because it duplicated the overview.
3. **Existing character snapshots are reused.** Tower participants reference the game's normalized `CharacterSnapshot` model rather than introducing a Tower-only build snapshot or serialized snapshot blob.
4. **Hall of Fame records are derived.** The first-clear attempt referenced by `TowerFloorProgress` is the immutable source for Hall of Fame rows. Separate clear-record and clear-participant models would duplicate attempt and roster data.
5. **Server state is derived.** Progression, the current floor, Echo availability, and floor lifecycle states are computed from persisted floor progress rather than stored in a redundant `TowerServerState` aggregate.
6. **Scouting and preparation share one contribution pipeline.** Contribution kind distinguishes scouting and preparation effects, avoiding parallel command and persistence implementations.
7. **Attempt reports are embedded in Expedition detail.** A report API is available, but a separate attempt-report route is not required for the MVP UI.
8. **Expedition membership uses applications.** A non-leader submits a locked build for review; only the Expedition leader can accept or decline it. A roster slot is occupied only after acceptance.
9. **Realtime is a notification channel, not a playback transport.** Version 2 Tower combat is an immutable, participant-authorized, Brotli-compressed bundle downloaded once through REST and played locally from server timestamps. Realtime only causes authorized state refreshes. Version 1 frame delivery remains temporarily readable for rollout compatibility.
10. **Sovereign floors are first-clear encounters only.** They never expose Echo Expeditions or Echo rewards, even after the realm clears them.
11. **A floor permits one active attempt at a time.** Other ready Expeditions remain intact, but cannot start while an attempt on the same server and floor is queued or playing back.
12. **Monster presentation is text and combat state only.** Creature artwork is not rendered in Tower, dungeon, or combat surfaces, and the duplicated frontend creature artwork folders have been removed.
13. **Scouting reveals combat abilities.** Guardian ability names, descriptions, types, and cooldowns come from the combat catalog rather than duplicated Tower hints. Actives unlock at 25%, 50%, and 75%; the passive is always the final 100% reveal.

## Executive status

| Area                      | Status      | Current result                                                                                                                                                                                                                                                                                                                                                           |
| ------------------------- | ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Ten-floor catalog         | **Shipped** | Floors 1–10 are JSON-driven and validated at startup.                                                                                                                                                                                                                                                                                                                    |
| Server progression        | **Shipped** | Sequential unlocks, first-clear state, and current-floor derivation are implemented.                                                                                                                                                                                                                                                                                     |
| Expedition lifecycle      | **Shipped** | Create, apply, accept, decline, withdraw, leave, cancel, readiness, start, and completion flows are implemented.                                                                                                                                                                                                                                                         |
| Locked builds             | **Shipped** | Applying captures the existing immutable character snapshot; acceptance promotes that same snapshot into the roster.                                                                                                                                                                                                                                                     |
| Tower combat              | **Shipped** | Full Expeditions run through the existing combat engine, persist a battle report and deterministic timeline, and play at ten ticks per real-time second.                                                                                                                                                                                                                 |
| Scouting                  | **Shipped** | Failed-attempt progress, immediate scouting on every released floor, reveal thresholds, and a three-click Tower-wide weekly limit are implemented.                                                                                                                                                                                                                        |
| Preparation               | **Shipped** | Three contribution kinds affect the next attempt, share a three-click Tower-wide weekly limit per player, and cap each realm bonus at 10%.                                                                                                                                                                                                                                |
| Echo Mode                 | **Shipped** | Floor 1 unlock, cleared non-Sovereign-floor eligibility, a Tower-wide weekly reward lockout, and Tower Mark rewards are implemented.                                                                                                                                                                                                                                   |
| Hall of Fame              | **Shipped** | First-clear records, roster, guild names, attempt number, duration, and clear time are displayed.                                                                                                                                                                                                                                                                        |
| Overview UI               | **Shipped** | Tower summary, floor rail, floor workspace, Expeditions, rewards, unlocks, and recent clears use the existing game design system.                                                                                                                                                                                                                                        |
| Expedition UI             | **Shipped** | Apply/withdraw, leader approval controls, leave, start, roster, readiness, and result views are implemented.                                                                                                                                                                                                                                                             |
| Hall of Fame UI           | **Shipped** | A full first-server-clear table is available.                                                                                                                                                                                                                                                                                                                            |
| Server unlock effects     | **Partial** | Unlock keys are persisted, but most downstream game systems do not consume them yet.                                                                                                                                                                                                                                                                                     |
| Rewards and prestige      | **Partial** | First-clear and Echo Tower Tokens are granted; titles, cosmetics, and other prestige rewards are not implemented.                                                                                                                                                                                                                                                       |
| Guardian mechanics        | **Shipped** | Floors 1–10 have fully authored active/passive kits. Floor 10's Mad King combines repeated area strikes and damage-based healing, locked highest-Health targeting, a reversible timed damage tradeoff, and missing-Health-step Lifesteal. Monster artwork is intentionally excluded. |
| Hall combat statistics    | **Partial** | Battle reports contain combat data, but the Hall table does not surface richer performance metrics.                                                                                                                                                                                                                                                                      |
| Automated coverage        | **Partial** | Catalog, domain invariants, application approval, realtime delivery, Expedition rules, combat outcomes, progression, Echo, Hall projection, controller dispatch, and indexes have focused tests; PostgreSQL concurrency, HTTP integration, and frontend flows need broader coverage.                                                                                     |
| Frontend automated tests  | **Partial** | The Angular API client has route/payload mapping tests and the complete Angular development build passes; component, browser, and full-suite execution coverage remain.                                                                                                                                                                                                  |
| Live asynchronous updates | **Shipped** | Every Expedition lifecycle mutation queues a durable outbox event and broadcasts it through the existing authenticated SignalR channel; overview and Expedition clients refresh their authorized server projections automatically.                                                                                                                                       |
| Real-time combat playback | **Shipped** | New attempts use compact cumulative frames, one static entity/ability header, one immutable compressed download, local wall-clock seeking, in-memory/HTTP caching, and due-time server finalization. Refresh, reconnect, route return, and tab wake re-seek without per-frame database reads or SignalR messages. Version 1 remains a compatibility read path. |
| Database rollout          | **Partial** | The version 2 EF Core migration is generated but has not been applied to a shared or production database. |

## Shipped implementation

### Content and server initialization

- A data-driven catalog defines ten contiguous floors.
- Each floor defines its guardian creature and combat-profile reference, title, tier, Expedition size, recommended rating, strength multiplier, tags, Tower Mark reward, Echo eligibility, and server unlock keys with player-facing descriptions. Scouting reveals are projected from the referenced combat abilities.
- Catalog validation rejects Echo-enabled Sovereign floors and invalid or duplicate unlock definitions.
- Guardian creature references are validated against the existing creature catalog.
- The initial server state is created lazily: Floor 1 is unlocked and later floors remain locked.
- Existing progress is preserved when definitions are loaded; newly defined floors can be initialized without replacing prior progress.
- Floor lifecycle is derived as `Locked`, `Sealed`, `Scouting`, `Rallying`, or `Cleared`; the internal `Rallying` value is displayed as **Expedition forming**.

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

The migration `20260812112124_HardenWorldTowerCombatWorkers` adds expiring simulation and dispatch leases, retry counts, and worker-claim indexes. It must be applied before deploying this version of the API.

The migration `20260813175534_OptimizeWorldTowerPlaybackV2` adds compact playback metadata, a cold one-to-one compressed artifact table, nullable legacy timeline storage, and the playback-end index. It must be applied before enabling `WorldTower:CompactPlaybackEnabled` outside an environment whose schema has been upgraded.

Important database invariants include:

- one progress row per server and floor;
- at most one first-clear attempt per floor;
- at most one attempt per Expedition;
- one character and one account slot per Expedition;
- one application per character/account and Expedition;
- one rewarded Echo clear per server, floor, character, and week;
- one row per server unlock key.

### Expedition and participation rules

- `FirstClear` and `Echo` Expedition modes are supported.
- An Expedition creator is added to the roster immediately and receives a locked character snapshot.
- Other characters apply with a locked snapshot; applying does not occupy a roster slot.
- Only the leader can accept or decline a pending application.
- Acceptance reuses the submitted snapshot and promotes the applicant into the roster.
- Applicants can withdraw while the Expedition remains open; filling the roster declines the remaining pending applications.
- An account can occupy only one slot in an Expedition.
- A character can participate in only one active Expedition.
- Expedition size comes from the selected floor definition.
- A full roster becomes ready; only the leader can start it.
- Starting requires every slot to be filled.
- Starting is rejected while another attempt on the same server and floor is queued or playing back.
- A non-leader can leave and reopen a recruiting Expedition.
- A leader leaving cancels their Expedition.
- Ownership, applications, and membership are evaluated from the authenticated character/account, including guest accounts that otherwise meet participation rules.
- Recommended power contributes to readiness labels and warnings but is not an eligibility gate.

### Combat and outcomes

- Tower combat rehydrates the locked character snapshots, rather than current mutable loadouts.
- Guardian combatants reuse the existing creature and combat systems with Tower scaling.
- Floor 1 uses Garran, the Gatekeeper with Gatehammer, Slam the Gates, permanent Power transfer through Gatekeeper's Toll, and three defensive Gate Seals that shatter at 75%, 50%, and 25% Health.
- Preparation modifiers are applied to player power, penetration, and guardian power as appropriate.
- Attempts persist status, timing, success/failure, duration, and serialized battle report data.
- A successful first clear records the immutable first-clear attempt, sets scouting to 100%, unlocks the next floor, records server unlock keys, and grants four times the floor's soft-power-curve Tower Tokens.
- Echo Tower Tokens use `round(100 × (1 + 1.5 × ((floor - 1) / 99)^0.8))`, rising from 100 on Floor 1 to 250 on Floor 100 while increasing on every floor.
- A failed first-clear attempt grants scouting progress for the configured number of weekly failures.
- A successful Echo attempt grants its configured Tower Tokens only to participants who have not already received an Echo reward anywhere in the Tower during the current week.
- Attempt failures are persisted as errored/completed instead of leaving an Expedition indefinitely in progress.

### Transactions and concurrency

- Normal commands use the application's command transaction behavior.
- starting an Expedition is intentionally marked non-transactional at the command-pipeline level and owns short transaction boundaries around state transitions, combat, and outcome persistence;
- PostgreSQL advisory locks serialize mutations for a server/floor pair;
- while holding that floor lock, start verifies that no other queued or playing attempt exists for the floor;
- application acceptance also locks the applicant character before the floor, preserving the established lock order across concurrent commands;
- unique constraints prevent duplicate first clears, duplicate roster membership, duplicate attempt creation, and duplicate Echo rewards.

This split avoids running the combat simulation inside one long database transaction and prevents the nested-transaction error observed during Expedition apply/leave/start testing.

### Scouting

- Failed first-clear attempts add configurable scouting progress for the first three failures each week by default.
- Manual `Research` contributions increase scouting progress.
- Manual scouting is immediate, is available before a floor unlocks, and has a configurable Tower-wide weekly per-character click cap.
- Scouting progress is clamped from 0 to 100.
- Data-driven reveals become visible at their configured thresholds.
- Clearing a floor completes its scouting progress.

### Preparation

The contribution pipeline supports:

- `SupplyWeapons` — increases Expedition damage;
- `InscribeWards` — reduces Guardian damage;
- `ScoutWeakPoints` — increases armor and magic penetration.

Preparation has a configurable Tower-wide weekly per-character click cap, per-click effect, and maximum effect. Actions are rejected before persistence if a bonus is maxed, so weekly allowance is not wasted. Completed scouting likewise rejects further scouting actions. Current actions do not consume a separate item or currency.

### Echo Mode

- Echo Mode is derived from Floor 1 being cleared.
- Echo Expeditions may target cleared floors whose definitions permit Echo attempts.
- Sovereign floors categorically reject Echo Expeditions; their catalog entries expose no Echo reward.
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
- `/game/world/tower/expeditions/:rallyId` (`/rallies/:rallyId` redirects for compatibility)
- `/game/world/tower/hall-of-fame`

The Tower screens use the application's existing `ll-*` surfaces, buttons, badges, navigation tabs, typography, spacing, and responsive layout. The overview contains:

- server progression summary;
- scrollable floor ascent rail;
- selected-floor identity, state, Expedition size, recommended rating, scouting level, rewards, and descriptive unlocks;
- Guardian intelligence and scouting reveals;
- Scouting, Preparation, and Expedition tabs;
- active Expeditions and Expedition creation;
- recent Hall of Fame records.

The overview suppresses Expedition-creation actions while the current character belongs to an active Expedition and offers a direct **View your Expedition** action instead. Active Expedition summaries identify their leader. Scouting shows the current character's weekly action usage, permits selecting and scouting locked floors, and clearly disables scouting when the cap is reached. Unlock cards render catalog descriptions rather than internal persistence keys, Sovereign floors omit the Echo reward card, and monster art is not rendered. The Expedition page contains application/withdrawal actions, leader accept/decline controls, readiness, roster slots, start controls, and the resulting battle report. Accepted applications become withdrawn when their participant leaves; the same application record can then be resubmitted with a fresh locked build. Roster, applicant, and battle-report character names reuse the shared player menu for profile viewing and whispers. The dedicated Hall page contains the complete first-clear table.

Local Development exposes a leader-only **Fill with test roster** action on recruiting Expeditions. It fills open slots from the seeded local guest pool using real character snapshots and the normal power-rating path, then marks the Expedition ready so the standard start, combat, reward, progression, and SignalR flows can be tested without signing into helper accounts. Both the ASP.NET Development environment and `FeatureManagement:WorldTowerDevelopmentTools` are required. Local launch profiles enable the tool and seed twenty helper accounts; non-Development hosts return `404` for the hidden endpoint.

Completed Tower attempts persist the authoritative engine combat result. Expedition participants can open the final detail after the paced playback completes or later after refreshing the Expedition page. The detailed view reuses the standard team, combatant, aggregate-stat, and per-ability breakdown UI; non-participants cannot retrieve the result. Engine-created summons are collapsed by owner and summon type in that shared statistics roster. Each group shows its total count, standing count, combined health/barrier and combined output, and can be expanded to inspect the unnumbered individual summons.

Tower attempts now enter `Playback` after fast deterministic resolution. The engine captures tick 0, every 10 ticks, and an exact partial final frame. The API stores those frames separately from ordinary attempt reads, returns the current participant-authorized snapshot for refresh recovery, and withholds the completed report/result until terminal status. A hosted dispatcher derives the due frame from server time, sends only the newest due frame to all roster-character SignalR groups, and runs the existing reward/progression transaction at the scheduled final frame. The Angular Expedition screen enters the shared combat viewer, deduplicates frames by sequence, updates combatants and cumulative stats, can leave/re-enter the live view, and transitions to the existing final report.

Expedition creation, application submission, acceptance, decline, withdrawal, member departure, cancellation, start, victory, defeat, and combat error all emit the compatibility event `WorldTowerRallyUpdated` after the associated state is committed. The existing outbox worker delivers these events through SignalR. Payloads contain identifiers, status, and aggregate counts only; each client refreshes the REST resource to receive the projection it is authorized to see.

### API surface

All endpoints require authentication.

| Method | Route                                                                        | Purpose                                   |
| ------ | ---------------------------------------------------------------------------- | ----------------------------------------- |
| `GET`  | `/api/v1/world-tower`                                                        | Tower overview and recent clears          |
| `GET`  | `/api/v1/world-tower/floors/{floorNumber}`                                   | Selected-floor workspace data             |
| `GET`  | `/api/v1/world-tower/rallies/{rallyId}`                                      | Expedition, roster, readiness, and result |
| `GET`  | `/api/v1/world-tower/attempts/{attemptId}/report`                            | Persisted battle report                   |
| `GET`  | `/api/v1/world-tower/hall-of-fame`                                           | Full first-clear table                    |
| `POST` | `/api/v1/world-tower/rallies`                                                | Create a FirstClear or Echo Expedition    |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/applications`                         | Apply with the current locked build       |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/applications/{applicationId}/accept`  | Leader accepts an application             |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/applications/{applicationId}/decline` | Leader declines an application            |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/leave`                                | Leave or cancel the Expedition            |
| `POST` | `/api/v1/world-tower/rallies/{rallyId}/start`                                | Run the locked-roster attempt             |
| `POST` | `/api/v1/world-tower/floors/{floorNumber}/contributions`                     | Contribute research or preparation        |

## Partial implementation

### Server unlock consumption

First clears persist data-driven unlock keys, including Echo-related and future content keys. The Tower API returns each stable key with its catalog-authored player description, and the UI displays only the description. Downstream shops, encounters, bands, or other feature systems do not yet query `ServerUnlock`. Until consumers are added, most keys are durable progression markers rather than active content switches.

### Rewards and prestige

Tower Mark rewards are complete for first clears and weekly Echo clears. Each floor defines its Echo amount, first clears grant four times that amount, and catalog validation requires rewards to increase floor by floor. The broader reward proposal remains partial:

- no Tower-specific title grant;
- no cosmetic reward delivery;
- no reward mail/fallback flow;
- no contribution or role awards;
- shop inventory and purchase handling are not yet implemented.

These omissions avoid new reward models until concrete rewards and existing integration points are chosen.

### Guardian mechanics

Guardian identity and abilities are data-driven, and combat uses existing creature definitions with configurable strength. Floors 1–10 have authored kits. Floor 3's Morrowmaw owns and consumes Broodlings; Floor 5's Kharad manages paired pillars and synchronized Resonance; Floor 6's Orsenn uses source-bound Cinder; Floor 7's Eydis gains permanent Abundance every ten seconds, scaling both Springtide and her periodic Max-Health healing; Floor 8's Kodoku maintains a capped Venomspawn brood; Floor 9's Ni summons nine inert copies and scales from their survival; and Floor 10's Mad King combines repeated area damage, damage-based healing, conditional highest-Health execution damage, a timed damage tradeoff, and missing-Health-step Lifesteal. Reusable effect repetition, status-stack and missing-Health-step attribute scaling, owned-summon scaling and targeting, capped-summon overflow healing, timed-modifier magnitude tracking, and Health-swap primitives support these kits and later content. Tags and scouting reveals communicate intended mechanics.

Ni's source design omitted active cooldowns, copy durability, Ninth Seal hit semantics, and ended the passive mid-sentence. The authored defaults are 8/16/20-second cooldowns, copies with 10% of Ni's Max Health and inherited Armor/Resistance, and one combined `20% × surviving copies` Ninth Seal hit. No behavior was invented for the unfinished passive clause; it remains a content-design follow-up if the missing text is recovered.

Eydis's source design omitted active cooldowns, whether percentage healing uses Max Health, and an Abundance cap. The authored defaults are 12/20/15-second active cooldowns, `1% Max Health × Abundance` periodic healing, and a 60-stack permanent combat cap (the maximum reachable within the 6,000-tick encounter limit). Her Floor 7 profile is calibrated at `23.3` Health, `12.5` Offense, and `8.7` on each defensive, penetration, and regeneration axis. Ni's relocated Floor 9 profile is calibrated at `70` Health, `58.2` Offense, and `15` on the remaining axes. Both fixed 256-attempt balance seeds pass the prepared mixed-roster 5–15% win-rate gate for both floors.

The Mad King's Floor 10 profile is calibrated at `57` Health, `35` Offense, and `15` on each defensive, penetration, and regeneration axis. Both fixed 256-attempt balance seeds pass the prepared mixed-roster 5–15% win-rate gate. King's Cleaver selects and locks the highest-current-Health enemy once per cast; a target above 50% Health takes the base 300% strike plus a second 300% strike. Bloodlust recalculates on the Mad King's own Health changes, granting 5% Lifesteal per complete 10% of Max Health missing and removing steps again when he heals.

Kodoku's source design omitted Venomspawn combat stats. The authored defaults give each Venomspawn 8% of Kodoku's Max Health and 15% of Kodoku's Power, with normal Physical basic attacks and no active ability. Kodoku's Floor 8 profile is calibrated at `60` Health, `54` Offense, and `15` on each defensive, penetration, and regeneration axis; both fixed 256-attempt balance seeds pass the prepared mixed-roster 5–15% win-rate gate.

### Hall of Fame depth

The first-clear record and roster are complete. Richer statistics such as total damage, healing, deaths, or role awards remain available only where represented in the attempt report and are not promoted to the Hall table. Cross-server and seasonal views are not part of the MVP.

### User experience notifications

Expedition state is server-authoritative and updates live through SignalR-triggered REST refreshes. There are no Tower-specific chat announcements or durable notification-inbox entries yet.

### Automated and manual coverage

Focused tests currently cover:

- the ten-floor contiguous catalog and existing guardian references;
- valid Expedition sizes, strength values, recommended ratings, and unlock keys;
- immutable first-clear recording and completion of scouting;
- scouting clamping and negative-input rejection;
- the non-transactional start-command regression;
- below-recommendation Expedition creation and readiness warnings;
- snapshots remaining unchanged after the source character changes;
- a distinct guest account applying successfully and occupying no slot before acceptance;
- leader-only acceptance/decline, applicant withdrawal, and promotion of the locked application snapshot;
- durable outbox routing and world-audience SignalR delivery for Expedition state changes;
- same-account duplicate-slot and one-active-Expedition rejection;
- member leave, leader cancellation, and pre-combat start authorization;
- separate research/preparation caps and preparation math;
- Echo Expedition unlock eligibility;
- Hall of Fame projection from the first-clear attempt and locked roster;
- concurrency-sensitive unique indexes in the EF Core model;
- successful first-clear reports, Tower Mark grants, server unlocks, next-floor progression, and Hall creation;
- failed-attempt scouting and its weekly attempt cap;
- Tower-wide weekly per-character Echo reward lockouts across repeat clears and different floors;
- combat failure reports and combat-exception persistence;
- mixed Echo rosters rewarding only members who remain weekly-eligible;
- supply, ward, and weak-point preparation values reaching the friendly and hostile combat runtimes;
- authored Guardian catalog and runtime behavior through Floor 10, including Eydis's Abundance loop, Kodoku's capped acting summons and survivor scaling, Ni's relocated nine-copy mechanics, and the Mad King's complete kit;
- every controller endpoint dispatching the authenticated character and route/body payloads;
- missing-character claim rejection and controller authorization metadata;
- Angular API-client route and mutation payload mapping, including the absence of a minimum-power field.

Coverage still needed:

- hosted HTTP request/response, model-binding, and authentication-middleware integration;
- same-account and one-active-Expedition constraints against PostgreSQL under concurrency;
- equipment and essence snapshot immutability after a player changes their build;
- first-clear race and duplicate-clear handling against PostgreSQL;
- Angular overview/Expedition/Hall component behavior and end-to-end flows with multiple accounts.

### Database rollout

The migration and model snapshot are present. The migration has not been applied to a shared or production database, and no service has been deployed as part of this work.

## Not implemented

These items were proposed or identified during implementation but are not in the current feature:

| Item                              | Status              | Notes                                                                                                                                                                                                                          |
| --------------------------------- | ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Practice attempt mode             | **Not implemented** | Optional in the original proposal; no enum value, rules, rewards, or UI.                                                                                                                                                       |
| Entrance keys                     | **Not implemented** | No inventory key requirement or consumption.                                                                                                                                                                                   |
| Expedition expiration/cleanup     | **Not implemented** | Recruiting Expeditions do not have a scheduled expiration worker.                                                                                                                                                              |
| Notifications/chat announcements  | **Not implemented** | No integration for Expedition or first-clear announcements.                                                                                                                                                                    |
| Bespoke Guardian phase engine     | **Not implemented** | Existing combat and creature mechanics are reused.                                                                                                                                                                             |
| Tower titles/cosmetics            | **Not implemented** | Cinders are the only delivered Tower reward.                                                                                                                                                                                   |
| Active downstream unlock switches | **Not implemented** | Unlock keys are persisted but not generally consumed.                                                                                                                                                                          |
| Operational telemetry dashboard   | **Not implemented** | Existing logging is used; no Tower-specific metrics/dashboard.                                                                                                                                                                 |
| Simulation-led Tower calibration  | **Partial**         | Deterministic canonical roster analysis, split Guardian scaling, Tier 1 benchmark loadouts, recommendations, and initial floor values are shipped. Wider held-out seed validation, telemetry, and continued tuning remain.     |
| Real-time combat hardening        | **Partial**         | Queued simulation, multi-instance leases, bounded reconnect recovery, visible reconnect state, and health/barrier interpolation are shipped. Telemetry, retention cleanup, size limits, and fake-time browser coverage remain. |

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

| Requirement                                                  | Status      | Evidence or remaining work                                                                                                                                |
| ------------------------------------------------------------ | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Initialize Floor 1 and lock later floors                     | **Shipped** | Lazy initialization and sequential floor progress.                                                                                                        |
| Preserve server progress across requests                     | **Shipped** | Persisted `TowerFloorProgress`.                                                                                                                           |
| Clear exactly once and unlock the next floor                 | **Shipped** | First-clear reference, unique constraint, and floor advisory lock.                                                                                        |
| Create, apply, approve, leave, cancel, and start Expeditions | **Shipped** | Service commands, API endpoints, approval-aware overview/Expedition UI, and locked application snapshots.                                                 |
| Enforce one account per Expedition slot                      | **Shipped** | Account-aware service rule and unique index.                                                                                                              |
| Enforce one active Expedition per character                  | **Shipped** | Service validation.                                                                                                                                       |
| Enforce one active attempt per server/floor                  | **Shipped** | Floor advisory lock plus an active `Started`/`Playback` attempt check inside the start transaction.                                                       |
| Do not enforce a minimum rating                              | **Shipped** | Only `RecommendedPowerRating` remains in definitions, DTOs, service logic, and UI.                                                                        |
| Lock character builds on application                         | **Shipped** | Existing `CharacterSnapshot` relation is captured at application time and reused on acceptance.                                                           |
| Update Expedition state live                                 | **Shipped** | Transactional outbox events broadcast through SignalR and trigger authorized REST refreshes for all Expedition lifecycle states.                          |
| Resolve combat from locked builds                            | **Shipped** | Snapshot rehydration and existing combat engine.                                                                                                          |
| Persist an attempt report                                    | **Shipped** | Attempt timing, result, and serialized report.                                                                                                            |
| Create one permanent first-clear record                      | **Shipped** | Derived from floor progress and the first-clear attempt.                                                                                                  |
| Award scouting for early weekly failures                     | **Shipped** | Configurable gain and weekly failure cap.                                                                                                                 |
| Support manual scouting with a weekly cap                    | **Shipped** | Research contributions.                                                                                                                                   |
| Apply preparation to attempts                                | **Shipped** | Three preparation kinds and configurable cap/effect.                                                                                                      |
| Unlock Echo after Floor 1                                    | **Shipped** | Derived from Floor 1 clear.                                                                                                                               |
| Exclude Sovereign floors from Echo                           | **Shipped** | Catalog invariant, service validation, API projection, and UI reward suppression.                                                                         |
| Enforce weekly Echo rewards                                  | **Shipped** | Unique weekly Echo clear and eligible-participant rewards.                                                                                                |
| Deliver first-clear and Echo Cinders                         | **Shipped** | Character currency updates.                                                                                                                               |
| Activate all server content unlocks                          | **Partial** | Keys persist; downstream consumers remain.                                                                                                                |
| Show overview, floor details, Expeditions, reports, and Hall | **Shipped** | Floor detail is intentionally integrated into overview; reports appear in Expedition detail.                                                              |
| Provide comprehensive automated coverage                     | **Partial** | Thirty-four targeted World Tower .NET tests and the Angular build pass; HTTP integration, PostgreSQL concurrency, component, and browser coverage remain. |
| Complete production rollout                                  | **Partial** | Migration application and deployment are external operational steps.                                                                                      |

## Configuration

`WorldTower` settings are bound and validated at API startup:

| Setting                               | Current default | Purpose                                      |
| ------------------------------------- | --------------: | -------------------------------------------- |
| `ServerId`                            |       `default` | Progression partition key                    |
| `FailedAttemptScoutingGain`           |            `10` | Scouting gained per eligible failed attempt  |
| `FailedAttemptScoutingWeeklyCap`      |             `3` | Weekly failed attempts that grant scouting   |
| `ManualScoutingWeeklyCapPerCharacter` |             `3` | Weekly scouting clicks across all floors     |
| `PreparationWeeklyCapPerCharacter`    |             `3` | Weekly preparation clicks across all floors  |
| `PreparationPercentPerPoint`          |          `0.25` | Effect percentage per preparation click      |
| `PreparationMaxEffectPercent`         |            `10` | Maximum effect for each preparation kind     |
| `CombatTicksPerFrame`                 |            `10` | Authoritative ticks represented by one frame |
| `PlaybackPollMilliseconds`            |           `250` | Playback dispatcher polling interval         |
| `SimulationPollMilliseconds`          |           `250` | Queued simulation polling interval           |
| `WorkerLeaseSeconds`                  |            `30` | Expiring ownership window for worker claims  |
| `RecoveryFrameLimit`                  |            `60` | Maximum frames returned per recovery page    |

Balance and content changes should prefer options or floor JSON over code edits.

## Key reuse and redundancy decisions

| Original concept                              | Current implementation                                                    | Reason                                                                           |
| --------------------------------------------- | ------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| `TowerBuildSnapshot` with JSON payloads       | Existing `CharacterSnapshot` and its normalized equipment/essence data    | Preserves one canonical snapshot implementation and supports combat rehydration. |
| `TowerClearRecord` and participant rows       | `TowerFloorProgress.FirstClearAttemptId` plus `TowerAttempt` participants | Avoids duplicating immutable attempt and roster data.                            |
| `TowerServerState`                            | Derived from all `TowerFloorProgress` rows                                | Prevents current-floor and Echo flags from drifting out of sync.                 |
| Persisted floor lifecycle state               | Derived state                                                             | State follows unlock, scouting, Expedition, and clear facts automatically.       |
| Separate scouting/preparation APIs and models | `TowerContribution` with a contribution kind                              | One weekly-cap and aggregation pipeline serves both systems.                     |
| Tower-only combat implementation              | Existing creature, snapshot, stats, and combat services                   | Keeps Tower orchestration focused on roster, scaling, and outcomes.              |
| Dedicated floor page                          | Selected-floor workspace in overview                                      | Removes duplicate UI and keeps the main Tower flow in one place.                 |
| Separate attempt report page                  | Report within Expedition detail plus report API                           | Avoids an extra route while retaining direct API access.                         |

## Verification record

Verified on 2026-08-12:

- `dotnet build` for `API.LL` passed; pre-existing nullable warnings remain outside the Tower changes.
- Angular development build passed using the bundled workspace Node runtime.
- Targeted World Tower tests, including Sovereign Echo exclusion and active-floor attempt serialization, passed: 34/34.
- The Angular game-client development build and page-guide validation passed after the unlock contract and artwork removal.
- The Admin Dashboard TypeScript application check passed; its Angular builder currently exits on this Windows host with native code `-1073741819` before reporting a source diagnostic.
- The Expedition application tests cover applicant/leader projections, authorization, withdrawal, acceptance, participant promotion, and emitted state events.
- The realtime consumer test verifies durable event deserialization and world-audience SignalR publication; the outbox registry test verifies routing.
- The targeted Karma run is currently blocked before execution by an unrelated existing `guild-info.component.spec.ts` fixture that is missing `rolePermissions` and `vaultItems`.
- Repository search confirmed that no minimum-power property or gate remains in the Tower implementation.
- Repository search confirmed there are no frontend references to the removed creature artwork trees.

The current status document itself should be updated whenever a Tower item moves between **Not implemented**, **Partial**, and **Shipped**. A status should only move to **Shipped** after its production code exists and relevant verification passes.

## Recommended next work

1. Implement the simulation-led calibration workflow in `docs/world-tower-balancing-plan.md`, ending with its Tier 1-only Floor 10 release gate.
2. Make Region 2 the first real consumer of the Floor 10 `ServerUnlock`; settle the stable key before Region 2 content ships.
3. Finish real-time playback hardening with metrics, retention/size limits, and fake-time browser coverage.
4. Add PostgreSQL integration tests for Expedition concurrency and first-clear races, followed by hosted HTTP integration tests.
5. Add Angular component tests and a two-account browser flow covering create, apply, accept/decline, withdraw/leave, start, realtime refresh, and report rendering.
6. Decide concrete prestige rewards, then reuse existing title/cosmetic delivery systems instead of adding speculative Tower reward tables.
7. Add Expedition expiration and durable notification/chat behavior when the asynchronous UX requires it.

## Implementation file map

| Layer              | Primary paths                                                                                                                                                                                                                                            |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Static content     | `LL/src/API/API.LL/Data/world-tower/tower-floors.json`                                                                                                                                                                                                   |
| API                | `LL/src/API/API.LL/Controllers/V1/WorldTowerController.cs`                                                                                                                                                                                               |
| Application        | `LL/src/Core/Application/Interfaces/Services/LL/WorldTower/`, `LL/src/Core/Application/UseCases/WorldTower/`                                                                                                                                             |
| Domain             | `LL/src/Core/Domain/Models/WorldTower/`                                                                                                                                                                                                                  |
| Service            | `LL/src/Infrastructure/Service/Services.LL/WorldTower/`                                                                                                                                                                                                  |
| Persistence        | `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/WorldTower/`                                                                                                                                                                            |
| Migrations         | `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260811185743_AddWorldTowerMvp.cs`, `20260812081251_AddWorldTowerRallyApplications.cs`, `20260812103046_AddWorldTowerCombatPlayback.cs`, `20260812112124_HardenWorldTowerCombatWorkers.cs`, `20260813091414_OptimizeWorldTowerCombatProcessing.cs`, `20260813175534_OptimizeWorldTowerPlaybackV2.cs` |
| Realtime delivery  | `LL/src/Infrastructure/Service/Services.LL/Outbox/RealtimeWorldTowerGameEventOutboxConsumer.cs`, `LL/src/API/API.LL/HostedServices/WorldTowerCombatSimulationWorker.cs`, `WorldTowerCombatPlaybackWorker.cs`                                             |
| Angular API client | `LL/src/Presentation/ll/src/app/core/services/api/world-tower/`                                                                                                                                                                                          |
| Angular screens    | `LL/src/Presentation/ll/src/app/features/game/world/tower/`                                                                                                                                                                                              |
| Tests              | `LL/tests/EssenceSystem.Tests/WorldTowerTests.cs`, `WorldTowerServiceTests.cs`, `WorldTowerControllerTests.cs`; Angular client `world-tower.service.spec.ts`                                                                                             |

## Change log

### 2026-08-14

- Released Floor 10 with the Mad King and a complete data-driven four-ability kit.
- Added reversible missing-Health-step attribute synchronization for Bloodlust and fixed active-ability preflight to preserve conditional locked-target casts.
- Calibrated Floor 10 against two deterministic 256-attempt mixed-roster seeds at its level-50 Tier 1 Legendary checkpoint.

### 2026-08-13

- Added Eydis, the Endless Spring as Floor 7, moved Ni to Floor 9, and released the Tower through Floor 9.
- Added general status-stack attribute scaling and exact timed-modifier reversal to support Abundance healing and Ancient Heartwood.
- Calibrated both reassigned encounters against two deterministic 256-attempt mixed-roster seeds.
- Released Floor 8 with Kodoku, the Poisoned Vessel and a complete data-driven four-ability kit.
- Added acting Venomspawn, capped-summon overflow healing, owned-summon survivor targeting, lowest-Health Poison, and simultaneous healing/Health-Regeneration suppression.
- Calibrated Floor 8 against two deterministic 256-attempt mixed-roster seeds at its level-46 Tier 1 Unique checkpoint.

### 2026-08-12

- Replaced direct Expedition joining with locked-build applications.
- Added leader-only application acceptance and decline plus applicant withdrawal.
- Reused the application snapshot when an accepted character is promoted into the roster.
- Added account/application uniqueness constraints and an EF Core migration; it was generated but not applied.
- Added durable outbox events for every Expedition lifecycle mutation and world-audience SignalR delivery.
- Added automatic realtime refreshes to both the Tower overview and Expedition detail screens without exposing applicant details in broadcast payloads.
- Added approval workflow, outbox routing, realtime consumer, and contribution-saturation tests; 26 targeted tests pass.
- Prevented maxed or overflowing preparation contributions and completed-scouting research from consuming weekly contribution allowance; the UI disables and labels unavailable actions.
- Hid Expedition-creation actions for characters already in an active Expedition and replaced them with a direct link to their existing Expedition.
- Added the leader's character name to active Expedition summaries.
- Reused the shared character tag for Expedition roster, application, and battle-report names, enabling profile and whisper actions.
- Added visible weekly research usage, a limit-reached notice, and cap-aware research controls to scouting.
- Fixed accepted members leaving an Expedition so their application is withdrawn, hidden from the active state, and safely reusable for reapplication with a fresh locked build.
- Added a Development-only one-click test-roster filler backed by twenty seeded local guest characters and real locked snapshots.
- Exposed persisted Tower combat results to Expedition participants and integrated them with the shared combat detail viewer.
- Planned restart-safe real-time combat playback with authoritative checkpoints every 10 ticks and one-second participant-scoped delivery.
- Implemented the first end-to-end real-time playback slice: reusable engine checkpoints, durable timelines, server-clock recovery, participant-only SignalR frames, paced dispatch, deferred/idempotent outcome finalization, and incremental shared-viewer updates.
- Added deterministic checkpoint parity/boundary tests and updated Tower outcome tests to assert that rewards are absent until the playback deadline.
- Moved combat calculation out of the start request into a deterministic, expiring-lease background simulation queue.
- Added PostgreSQL `SKIP LOCKED` claims for simulation and playback workers so multiple API replicas cannot process the same work concurrently.
- Added bounded missed-frame REST recovery, automatic reconnect/gap repair, a visible reconnect state, and one-second health/barrier interpolation.
- Added worker lease configuration and migration `20260812112124_HardenWorldTowerCombatWorkers`; generated but not applied.
- Verified the full Angular development build.
- Added a simulation-led Tower balancing plan anchored to Region 1's attainable Power Rating curve, with an explicit Tier 1-only Floor 10 and Region 2 unlock release gate.
- Added authored Tier 1 benchmark checkpoints from level 30/Uncommon/four Essences on Floor 1 through level 50/Legendary/six Essences on Floor 10.
- Replaced the single Guardian strength scalar with independent health, offense, defense, resistance, penetration, and regeneration scaling axes.
- Added a deterministic, non-persisting World Tower balance analyzer to Admin diagnostics with canonical mixed and stress-test rosters, optional comparison loadouts, confidence intervals, duration, survival, and Guardian-health metrics.
- Recalibrated recommendations to canonical average ratings (146–179) and added regression coverage proving pre-Tower level 25/Common/three-Essence rosters are not a reliable Floor 1 clear.
- Made Sovereign floors first-clear-only by disabling Echo rewards in content and rejecting Sovereign Echo mode in catalog validation and service rules.
- Serialized starts per server/floor so another ready Expedition cannot start while an attempt is queued or playing back.
- Replaced raw unlock-key presentation with catalog-authored descriptions while retaining stable keys for persistence and future consumers.
- Removed monster artwork from Tower, dungeon, and combat presentation and deleted both frontend creature artwork trees (660 image files).
- Renamed the player-facing Tower group activity from Rally to Expedition across UI copy, catalog descriptions, validation messages, warnings, and the browser route; internal `TowerRally*` contracts remain stable and the former route redirects for compatibility.
- Replaced Floor 1's placeholder Guardian with Garran, the Gatekeeper; added his complete data-driven kit, reusable percentage-transfer and combat-start-attribute effects, health-threshold Seal shatters, scouting copy, focused mechanics tests, and Floor 1 balance regression coverage.
- Expanded Floor 1 to five Expedition slots and reran the released-floor balance matrix against the authored Tier 1 checkpoints.
- Locked the 256-seed prepared mixed-roster win-rate regression for released Floors 1–5: Floors 1–4 remain within their 85–97% target band and Floor 5 remains within its 75–90% Warden band. Each floor also retains at least two viable canonical roster shapes. Clear duration is diagnostic only; any victory within the 6,000-tick limit is equally valid.
- Replaced Floor 3's placeholder Guardian with Morrowmaw, Broodkeeper; added multi-target Broodling summoning, weakest-Broodling consumption and Max-Health healing, live brood-count modifiers, scouting copy, manifests, and focused catalog/combat tests.
- Expanded Floor 3 to five Expedition slots and gave each Broodling 10% inherited Broodkeeper Max Health plus Venomous Bite, which has a ten-second cooldown and applies Poison(10) to one random Ascendant.
- Replaced hand-authored scouting hints with combat-catalog ability reveals at 25%, 50%, 75%, and 100%; active abilities are shown first and the Guardian passive is always last, including type and cooldown metadata.
- Added Legacy's Ascension directly to the shared World sidebar and simplified Tower headings so Expedition identity consistently uses the Guardian name without generated floor-description copy.
- Completing a floor now permanently caps all three preparation bonuses at 10% for its UI and subsequent Echo Expeditions without fabricating weekly character contributions.
- Changed scouting and preparation from per-floor point budgets to separate three-click weekly allowances shared across the Tower; scouting is immediate on locked floors, while preparation still requires an unlocked floor.
- Upgraded the development roster shortcut to lock canonical, floor-benchmark combat builds for seeded guests (level, full equipment rarity, Essence count, and mixed party profiles) without modifying the guest accounts themselves.
- Hardened Hall of Fame and Expedition endpoints at catalog release boundaries so stale records and links from unreleased floors cannot throw or prevent the Tower from loading; stale links return to the overview.

### 2026-08-11

- Implemented the ten-floor World Tower MVP across domain, persistence, service, API, and Angular presentation layers.
- Reworked the Tower UI to use the game's established design system.
- Integrated floor detail into the overview and removed the redundant floor route/component.
- Reused existing character snapshots, creature combat, and design-system components.
- Derived Hall of Fame and server state instead of adding redundant persisted models.
- Corrected Expedition transaction ownership so apply/leave/start flows do not create nested transactions.
- Explicitly register joined participants as new EF Core rows, fixing the optimistic concurrency failure when a second account joins an Expedition.
- Added account-aware apply/leave behavior and leader Expedition cancellation.
- Removed minimum power rating entirely; recommended rating remains informational.
- Added focused service tests for Expedition rules, guest participation, snapshots, contributions, Echo eligibility, Hall projection, and unique persistence indexes.
- Added deterministic combat tests for first-clear progression/rewards, failed-attempt scouting caps, weekly Echo rewards, reports, and errored attempts.
- Added mixed-roster Echo eligibility and preparation-to-combat-runtime modifier tests.
- Added controller-boundary tests for authenticated character dispatch, request payload mapping, and authorization metadata.
- Added Angular API-client tests for all Tower read and mutation routes; isolated compilation passes, while the full Karma target remains blocked by an unrelated Guild test fixture.
- Added this living implementation-status document.
