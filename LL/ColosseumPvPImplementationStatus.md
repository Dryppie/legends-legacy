# Colosseum PvP Implementation Status

Date: 2026-06-25

This document tracks what is currently implemented from the Colosseum PvP finishing design, what is partially covered, and what remains to build.

## Implemented

### Backend battle safety and response flow

- Added canonical `POST /api/v1/colosseum/battle` route with structured `{ "opponentId": "..." }` body.
- Kept compatibility route `POST /api/v1/colosseum/StartArenaBattle`.
- Battle start now validates before ticket spend:
  - opponent id must be present;
  - self-attacks are rejected;
  - same-account attacks are rejected;
  - attacker and defender must exist;
  - defender must be from the server-generated eligible opponent pool;
  - defender can use either a valid arena defense snapshot or current live setup;
  - attacker and defender combat entities must be buildable.
- Ticket spend happens after validation and combat setup.
- Rating/history/reward mutation is handled inside the start-battle command transaction.
- SignalR battle completed events now broadcast already-applied results.

### Concurrency and repeat-attack protections

- Existing MediatR transaction behavior still serializes commands per attacking `CharacterId`.
- Added same attacker-to-defender short cooldown protection.
- Added daily incoming rated defense cap for defenders.
- Added daily defensive Glory cap tracking.

### Arena defense snapshots

- Added `ArenaDefenseSnapshot` domain model, EF configuration, repository methods, and `DbSet`.
- Added `POST /api/v1/colosseum/defense-snapshot` with compatibility route `UpdateDefenseSnapshot`.
- Defense snapshots are built from the current character snapshot and include equipment, modifiers, base attributes, and equipped essences.
- Rated battles prefer the defender's saved snapshot when it is valid and non-outdated.
- If no valid snapshot exists, rated battles fall back to the defender's current live setup.
- Colosseum status includes defense snapshot state and daily incoming rated defense usage.
- Frontend overview has an Arena Defense panel with update action and cap status.

### Rank tiers, Glory, and records

- Added derived arena tiers:
  - Bronze: 0-1099
  - Silver: 1100-1249
  - Gold: 1250-1449
  - Platinum: 1450-1699
  - Diamond: 1700-1999
  - Champion: 2000-2299
  - Ascendant: 2300+
- Added rank progress DTO support and rank boundary tests.
- Added `ArenaGlory`, current/best attack streaks, lifetime highest rating, and attack/defense records.
- Attack Glory rewards:
  - win: 12
  - draw: 8
  - loss: 5
  - daily first win bonus: +20 once per UTC day.
- Defense wins award defensive Glory, capped per UTC day.
- Glory and streaks do not affect rating.

### REST result and status APIs

- `StartArenaBattleResponseDto` includes battle id, combat result, outcome, ticket status, rating deltas, rank changes, Glory breakdown, streak changes, and opponent summary.
- `GET /api/v1/colosseum/status` includes rating, lifetime high, rank progress, Glory, ticket status, streaks, attack/defense records, defense status, and daily incoming defense cap.
- Match history stores outcome, rating deltas, Glory earned by each side, and attack streak before/after.

### Champion's Market v1

- Added data-driven `ChampionMarketItem` definitions in the Colosseum service.
- Added `ChampionMarketPurchase` domain model, EF configuration, repository methods, and `DbSet`.
- Added `GET /api/v1/colosseum/market` with compatibility route `GetChampionMarket`.
- Added `POST /api/v1/colosseum/market/purchase` with compatibility route `PurchaseChampionMarketItem`.
- Purchases validate:
  - enabled item;
  - positive quantity;
  - tier/rating requirements;
  - weekly limit;
  - lifetime limit;
  - sufficient Glory.
- Purchases deduct Glory and record purchase history.
- Weekly cache items grant existing currencies (`Cinders` and/or `Soulstones`).
- Cosmetic/title/banner ownership is represented by purchase records for now.
- Frontend Champion's Market tab lists items, Glory balance, reset date, limits, unavailable reasons, and purchase buttons.

### Frontend Colosseum updates

- Opponent cards use `opponentId` for battle starts.
- Opponent cards show rank tier, rating, expected rating deltas, and expected Glory.
- Overview shows tickets, rank tier, rating, Glory, attack streak, attack record, and defense status.
- Battle result modal uses the REST response and shows result, opponent, rating delta, Glory earned, daily first win bonus, rank tier, promotion copy, and streak changes.
- Battle history can be filtered between all, attacks, and defenses.

### Persistence

- Added EF migration `20260625151808_ColosseumPvPRewards`.
- Added EF migration `20260625153524_ColosseumDefenseAndChampionMarket`.
- New persistence includes Colosseum reward/rank/history fields, defense snapshot table, Champion's Market purchase table, and daily defense counter fields.

### Tests

- Added Colosseum rank boundary tests.
- Added weekly arena calendar reset tests for Champion's Market purchase windows.
- Added direct arena reward tests for base Glory, daily first win, and defensive Glory cap behavior.
- Full .NET test suite passes with 186 tests.

## Partially Implemented

### Database-level concurrency and idempotency

- In-process command serialization and transaction behavior are in place.
- Validation-before-ticket-spend, repeat defender cooldown, and daily incoming defense cap are in place.
- Still missing:
  - row-version/concurrency token on ticket/rating rows;
  - request idempotency keys for duplicate HTTP requests across multiple app instances;
  - database lock strategy for simultaneous incoming attacks on the same defender.

### Defense snapshot lifecycle

- Players can manually publish a valid defense snapshot.
- Opponent selection does not require a valid snapshot.
- Still missing:
  - automatic snapshot invalidation when PvP-relevant loadout/essence/stat changes occur;
  - cleanup of old replaced character snapshots;
  - richer defense history wording such as "Mira attacked you".

### Champion's Market rewards

- Currency cache purchases are functional through existing `Cinders`/`Soulstones`.
- Titles, banners, and cosmetics are purchasable only as durable purchase records.
- Still missing:
  - real title/banner/cosmetic entitlement systems;
  - admin/data-file item definitions instead of static in-service definitions;
  - purchase confirmation/toast feedback in the UI.

### Frontend verification

- Angular code was updated statically.
- Frontend build was not completed because the local npm shim is broken.
- Failed command: `npm run build`.
- Error: missing `C:\Users\HrHoe\AppData\Roaming\npm\node_modules\npm\bin\npm-cli.js`.

### Battle safety test coverage

- Rank and calendar tests exist.
- Reward math tests exist.
- Still missing direct automated tests for:
  - invalid opponent no-ticket-loss cases;
  - self/same-account prevention;
  - arbitrary opponent prevention;
  - Glory reward rules;
  - daily first win;
  - streak transitions;
  - full battle response shape;
  - duplicate request/concurrency behavior.

## Not Yet Implemented

### Weekly rewards

- No weekly ticket progress.
- No weekly chest unlocks.
- No highest-rank-this-week tracking.

### Season resets

- No season model.
- No seasonal highest rank tracking.
- No soft reset logic.

### Manual defense formations and multi-loadouts

- Not implemented, consistent with the design document's non-goals.

### Tournament Grounds

- Not implemented, consistent with the design document's non-goals.

## Top 5 Highest-Value Next Steps

1. **Add full backend battle safety tests.** Cover invalid-opponent no-ticket-loss paths, self/same-account rejection, arbitrary opponent rejection, Glory rules, daily first win, streak changes, and response shape.

2. **Add DB-level idempotency/concurrency controls.** Introduce request idempotency keys and row-version or locking around tickets, ratings, and defender incoming battle caps.

3. **Automate defense snapshot invalidation.** Mark snapshots outdated when equipment, equipped essences, stats, or other PvP-relevant loadout data changes.

4. **Promote Champion's Market data out of code.** Move market definitions to seed data or JSON/admin-managed content so balancing does not require service code edits.

5. **Implement real cosmetic/title entitlements and feedback.** Add durable ownership models for titles/banners/cosmetics and frontend success/error feedback after purchases.
