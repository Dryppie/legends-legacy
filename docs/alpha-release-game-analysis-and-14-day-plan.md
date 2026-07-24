# Legends Legacy Alpha Release Analysis and 14-Day Plan

**Analysis date:** July 24, 2026  
**Target alpha release:** Friday, August 7, 2026  
**Schedule:** Day 1 is July 25; Day 14 is release day  
**Assumed capacity:** One or two major work packages per day, with AI assistance

## Executive Verdict

Legends Legacy is a real, feature-rich idle RPG rather than an early prototype. Its main loop is already recognizable:

> Fight while active or away, collect equipment/materials/Essences, improve a build through equipment/Crafting/Essences/Soulstones, then test that build in harder areas, Dungeons, Prophecies, guild activities, and asynchronous PvP.

The game has enough content and systems for a meaningful closed alpha. It does **not** need another major feature before release.

It is **not release-ready today**, primarily because of release engineering and unverified integration risks:

1. The main game projects target .NET 10, but the backend GitHub workflow installs .NET 8 and the API runtime container uses ASP.NET 8. The current delivery path cannot reliably build and run the main game.
2. The Angular development build passes, but the production build fails because the active Dungeon page stylesheet exceeds the configured component-style budget.
3. Crafting V2 has a large uncommitted cross-layer change set and a new migration. Unit tests pass, but the full player workflow and migration have not been proven in a clean deployed environment.
4. There is no end-to-end test suite for the critical player journey. The backend has broad unit/service coverage, but account creation through Tutorial completion is not automatically protected.
5. Tracked configuration still contains development database credentials, a default JWT signing key, and system-chat secret material. Issuer and audience checks remain conditional on non-empty deployment configuration.
6. The separate worker is disabled by default in the checked-in Helm values even though Marketplace expiration and Tournament Grounds progression depend on it.
7. CI builds on pushes to `main`/release branches but does not currently run the 564 backend tests, the Angular tests, or a golden-path browser test. It is also not a pull-request gate.
8. The chat service builds with a known high-severity AutoMapper advisory.

The recommended alpha strategy is:

- Make build, deployment, migration, security configuration, and the first-player journey reliable first.
- Ship the strongest connected slice: Tutorial → idle combat → rewards → Essences/equipment/Crafting → first Dungeon.
- Treat guilds, Marketplace, chat, Colosseum, Tournament Grounds, and broader economy as explicitly experimental alpha systems.
- Hide or label unfinished surfaces instead of completing them now.
- Freeze feature work early enough to conduct two clean-environment release rehearsals.

If the plan below is followed, August 7 is a realistic date for a **small, controlled alpha**. It should not be treated as a broad public launch.

## Scope and Method

This analysis is based on:

- Current source under `LL/` and `LL-Chat/`.
- Current authored game content under `LL/src/API/API.LL/Data`.
- Frontend routes, game screens, state services, and public account flows.
- Backend controllers, domain/application services, persistence, worker jobs, migrations, and runtime configuration.
- Existing design and implementation-status documents in `docs/` and `LL/docs/`.
- Recent repository history, including the Dungeon, combat summary, power-rating, offline-combat, and Crafting V2 changes.
- Current builds and automated tests, run on July 24, 2026.

This was a source-and-build audit, not a live production playtest. The external infrastructure repository, production configuration, production database, monitoring, backups, and player telemetry were not available here. Balance and game feel conclusions are therefore informed by authored data and code behavior, but must be confirmed through the closed-alpha rehearsals in this plan.

## Current Game Snapshot

### Product identity

Legends Legacy is best positioned as a:

> Dark-fantasy online idle RPG centered on collecting creature Essences, constructing a build, and converting idle progress into active Dungeon and social progression.

The most distinctive feature is the Essence system. Crafting is becoming the targeted equipment path, while Dungeons are the main active test of a build. Prophecies provide daily/weekly direction. Guilds, Marketplace, chat, leaderboards, and Colosseum give the game an online world, but their value will depend heavily on alpha population.

### Authored content footprint

The current data catalog is substantial:

| Content                          |         Current footprint |
| -------------------------------- | ------------------------: |
| Regions                          |                         1 |
| Combat areas, including Tutorial |                        11 |
| Creatures                        |     55 authored creatures |
| Essence definitions              |                        61 |
| Abilities                        |                       126 |
| Ability behaviors                |                       131 |
| Statuses                         |                        36 |
| Dungeon families                 |                         3 |
| Dungeon difficulties             | 9 total, three per family |
| Dungeon delves                   |                         3 |
| Crafting base recipes            |                        35 |
| Blueprint families               |                        11 |
| Crafting materials               |                        21 |
| Item definitions                 |                       207 |
| Achievement definitions          |                        53 |
| Title definitions                |          approximately 33 |
| Daily Prophecies                 |                        19 |
| Weekly Prophecies                |                         8 |
| Guild buildings                  |                         9 |
| Guild weekly mission definitions |                         5 |
| Guild daily order definitions    |                         4 |
| Guild shop items                 |                         8 |
| Champion's Market items          |                         9 |

This is enough breadth for an alpha. The problem to solve is not content quantity; it is whether the first region, first build, first equipment project, and first Dungeon form a understandable and reliable progression arc.

## Core Loop Analysis

### 1. Acquisition loop

The player selects an area and starts idle combat. Combat produces:

- Character experience.
- Cinders and Soulstones.
- Items and crafting materials.
- Unbound Essences.
- Gathering rewards.
- Dungeon sigils and related progression.
- Achievement, Prophecy, guild, and other event progress.

Offline progression is capped and resolved when the player returns. Recent work has improved the catch-up state, action resolution, and combat summary.

**Strength:** A single activity feeds nearly every meaningful system. This is a strong idle-game foundation.

**Risk:** The reward surface is so broad that a player may receive many currencies and items without understanding which result matters. Reward correctness is also distributed across HTTP results, realtime events, outbox consumers, and frontend state stores, making duplicate or stale client state a release risk.

### 2. Build loop

Power comes from:

- Character level and attributes.
- Equipped items.
- Equipment quality, rarity, affixes, and special modifiers.
- Essence loadouts and their active/passive abilities.
- Essence level, potential, ascension, and evolution.
- Soulstone upgrades.
- Guild and other permanent bonuses.

Recent combat summaries and power-rating work make this more legible than it was in older game-design reports.

**Strength:** There is real build depth. Essences, equipment, statuses, summons, and ability behaviors give the game more identity than a simple numeric idle game.

**Risk:** The game still exposes many parallel power systems before it establishes a clear priority order. A new player can reasonably ask: should I level, craft, temper, upgrade a Soulstone, absorb an Essence, ascend an Essence, buy from the market, or attempt a Dungeon? The Tutorial answers only the first few minutes.

### 3. Crafting/equipment loop

Crafting V2 now supports:

- Broad base recipes and physical forms.
- Blueprint-themed outcomes.
- Recipe-scoped blueprint learning.
- Tiered material resolution.
- Quality and potential.
- Recipe mastery.
- Tempering through the Character Action system.
- Rarity progress, affixes, and special modifiers.
- Region One acquisition paths.

**Strength:** Crafting can become a purposeful, targetable equipment path rather than a second random loot faucet.

**Risk:** This is the largest active change set in the worktree: more than 30 modified files, a new EF migration, backend and frontend contract changes, content edits, and new tests. It passes the backend test suite and Angular development build, but has not been proven through a clean database plus real browser journey. Its balance is explicitly first-pass.

**Alpha decision:** Ship it only after the Day 4–6 migration and vertical-slice gates pass. If they do not pass, hide blueprint learning/advanced tempering behind a feature flag rather than destabilizing the rest of the alpha.

### 4. Dungeon loop

The current Dungeon design has recently been consolidated around:

- Three recognizable families.
- Three difficulties per family.
- Room sequences and encounters.
- Vigor.
- Delves.
- Gathering and reward tables.
- First-clear and repeat rewards.
- Mastery.
- Simulated power recommendations and readiness feedback.

**Strength:** Dungeons are the clearest active answer to “why improve my build?” The power recommendation system helps turn raw stats into a decision.

**Risk:** The Dungeon redesign was large and recent. The active Dungeon component is also the reason the production frontend build fails. Power recommendations rely on calibration work, and the frontend polls while recommendations are missing. Dungeon start, reconnect, action execution, reward claiming, death/abandonment, and inventory updates must be tested as one workflow.

**Alpha decision:** Ship all three families only if the common workflow passes. Otherwise expose the Goblin Mines family first and label the other two as alpha-locked.

### 5. Daily/weekly retention loop

Prophecies provide:

- Daily and weekly offers.
- Acceptance, progress, and claiming.
- Favor/reroll economy.
- Weekly revelation milestones.
- Cache rewards.
- Sidebar notifications.

Achievements and titles provide long-term records and prestige.

**Strength:** The game has a real answer to “why log in today?” Prophecies can guide players across existing systems instead of requiring new content.

**Risk:** With 19 daily definitions, 8 weekly definitions, achievements, guild orders, guild missions, Marketplace, and PvP, the player can experience a wall of obligation. Prophecies should guide rather than punish inefficient play.

### 6. Social and competitive loop

Implemented surfaces include:

- Guild membership, invitations, applications, resources, missions, personal orders, buildings, contribution tracking, rankings, and shop.
- General/guild/whisper chat and system messages.
- Marketplace listings and commodity buy/sell orders.
- Leaderboards.
- Colosseum opponents, tickets, battle records, rating, Glory, defense snapshots, and Champion's Market.
- Asynchronous Tournament Grounds and replay UI.

**Strength:** The alpha can feel like a shared world even with one PvE region.

**Risk:** These systems are population-sensitive. In a small alpha, empty markets, empty rankings, unavailable opponents, and guild requirements can make otherwise working features appear broken. Several flows are also concurrency- or ownership-sensitive. The worker must run for Marketplace expiration and Tournament progression.

**Alpha decision:** Keep them available as experimental systems if authorization, empty-state, and worker tests pass. Do not spend the 14 days expanding rewards, seasons, guild wars, guild raids, or tournament formats.

## New-Player Journey Analysis

### Current journey

The intended first-session flow is:

1. Register, use Google, or enter as a guest.
2. Bootstrap the current character, current action, and Tutorial state.
3. Defeat the Training Area creature.
4. Absorb the Goblin Essence.
5. Equip that Essence in the active loadout.
6. Open Crafting and claim the Tutorial chest.
7. Equip the Tutorial chest.
8. Continue into Lumo Ruins and the broader game.

The five-step `first-steps.v1.json` definition, backend-owned progression, outbox integration, quest bar, contextual routes, and guided overlays are all strong foundations.

### What is working well

- Guest entry reduces registration friction.
- The Tutorial teaches the signature Essence mechanic rather than only teaching menus.
- Tutorial truth is backend-owned.
- Game bootstrap prevents a page from becoming interactive before core state is available.
- Offline action resolution has a visible “catching up” state.
- Completed Tutorial state can become inactive rather than polling forever.

### High-risk gaps

1. **No end-to-end protection.** There is no browser test for register/guest → bootstrap → Tutorial → first area.
2. **Registration navigation is contradictory.** Successful authentication routes to `/game`, while the Signup component's success callback also routes to `/login`. The public guard may correct it, but the flow is unnecessarily race-prone.
3. **Forgot password is a non-functional clickable control.** It currently executes “do nothing.”
4. **Guest-account abuse controls are not visible.** Guest creation is anonymous and the main API has no general login/guest rate limiting.
5. **Post-Tutorial direction is weak.** The player finishes the chest step but is not given a short priority ladder such as “fight in Lumo Ruins → improve one Essence → craft one target item → attempt Goblin Mines.”
6. **Too much navigation is available immediately.** A new player can leave the intended sequence and encounter guild, PvP, market, Prophecies, Soulstones, and advanced Essence screens before understanding the base loop.
7. **Small unfinished signals remain.** The region exposes a Raids tab that only says raids are unavailable. Settings hardcodes version `1.0.0` and calls the support section “Beta and Support,” despite the release being alpha.

### Alpha target for the first session

By release, a fresh player should be able to:

- Enter the game in under one minute.
- Understand the Tutorial objective without reading external documentation.
- Complete the Tutorial without refreshing the browser.
- Know why the acquired Essence matters.
- Equip the chest and see its power effect.
- Start Lumo Ruins combat.
- Return after a short offline period and understand the reward recap.
- See no dead button or “coming soon” promise in the primary path.
- Receive a simple next-goal prompt after Tutorial completion.

## System-by-System Alpha Assessment

| System                | Current maturity                       | Player value   | Alpha risk                                                | Recommendation                                 |
| --------------------- | -------------------------------------- | -------------- | --------------------------------------------------------- | ---------------------------------------------- |
| Account/login/guest   | Mostly mature                          | Critical       | Rate limits, config, signup routing, no password recovery | Must harden and E2E-test                       |
| Tutorial/bootstrap    | Strong architecture                    | Critical       | No golden-path browser test                               | Must ship                                      |
| Idle combat/offline   | Strong core loop                       | Critical       | State duplication, catch-up edge cases, reward clarity    | Must ship and regression-test                  |
| Combat/abilities      | Deep, data-driven                      | Critical       | Balance outliers and warning noise                        | Ship; fix only severe outliers                 |
| Inventory/equipment   | Mature                                 | Critical       | Client/backend state drift and comparison clarity         | Must ship                                      |
| Essences/Soul Archive | Signature system                       | Critical       | Complexity and build guidance                             | Must ship                                      |
| Soulstones            | Functional                             | Medium         | Yet another early currency/sink                           | Ship, explain, do not expand                   |
| Crafting V2           | Feature-complete but actively changing | High           | Migration, browser flow, first-pass balance               | Conditional ship gate                          |
| Tempering             | Functional                             | High           | RNG trust, queue/action integration                       | Ship if full flow passes                       |
| Dungeons              | Ambitious and recently redesigned      | High           | Regression, calibration, production CSS failure           | Must harden; reduce exposed families if needed |
| Prophecies            | Mature retention feature               | High           | Chore pressure and reward balance                         | Ship                                           |
| Achievements/titles   | Broad                                  | Medium         | Coverage and presentation polish                          | Ship                                           |
| Guilds                | Broad alpha implementation             | Medium/high    | Low population and concurrency                            | Ship as experimental                           |
| Marketplace           | Broad order/listing support            | Medium/high    | Empty economy, ownership, expiration worker               | Ship only with worker and tests                |
| Chat                  | Functional                             | Medium/high    | Vulnerable dependency, detailed errors, in-memory limiter | Fix dependency/config; ship                    |
| Colosseum             | Functional async PvP                   | Medium         | Small population, snapshot/concurrency behavior           | Ship as experimental                           |
| Tournament Grounds    | Functional architecture                | Medium         | Worker disabled by default, timing/concurrency            | Feature-flag unless worker rehearsal passes    |
| Leaderboards          | Functional                             | Medium         | Empty boards in small alpha                               | Ship with good empty states                    |
| Raids                 | Not implemented                        | None for alpha | Visible unfinished promise                                | Hide tab                                       |
| Password recovery     | Not implemented                        | Account safety | Dead button and recovery gap                              | Hide control; document support recovery        |
| Admin dashboard       | Auth protected in code                 | Operations     | Configuration and separate verification                   | Verify, do not expand                          |

## Game Design Strengths

### Distinctive identity

The Essence system is a credible signature mechanic. Sixty-one creature-linked Essences, active/passive abilities, loadouts, resonance, ascension, evolution, and the Soul Archive create a collection/build fantasy that can distinguish Legends Legacy from generic idle RPGs.

### Strong systemic reuse

Idle combat feeds experience, equipment, Essences, crafting materials, gathering, Dungeon entry, achievements, Prophecies, and guild progress. This is efficient for a solo-developed game: new depth can often come from connecting existing systems rather than adding another feature.

### Data-driven content

Abilities, behaviors, statuses, creatures, Essences, Dungeons, rewards, recipes, blueprints, Prophecies, guild content, achievements, titles, and progression are authored in JSON with substantial startup/test validation. This supports rapid AI-assisted tuning and reduces the need to hardcode individual content behavior.

### Active/idle balance

The design correctly leaves repeated production in idle actions while using Dungeons, build editing, market decisions, Prophecy choices, and PvP as active management. That is the right genre fit.

### Testing foundation

The main game has 564 passing tests covering combat, Essences, Dungeons, Crafting, Prophecies, guild systems, Colosseum, achievements, outbox behavior, offline planning, rewards, power ratings, tutorials, and authorization-sensitive flows.

## Game Design Weaknesses and Alpha Risks

### Breadth is ahead of clarity

The player sees many credible systems, but no single screen states the next three meaningful goals. The game should not add another progression axis. It should convert existing state into recommendations and contextual links.

### Currency overload

The economy includes Cinders, Soulstones, Essence Dust, Monster Cores, Essence Potential resources, Dungeon sigils/fragments, Fate Echo, Ascension fragments, Arena Glory, Guild Favor, Guild Honors, Guild Supplies, materials, blueprints, caches, and equipment.

That breadth can work later, but every alpha currency needs:

- A one-sentence purpose.
- A visible source.
- A visible sink.
- A reason it is not just Cinders with another name.

The immediate goal is explanation and gross-balance safety, not a complete economy redesign.

### Region depth is the content ceiling

There is one region. This is acceptable for alpha if its 10 non-Tutorial areas, three Dungeon families, 61 Essences, and Crafting projects create a coherent progression arc. It is not acceptable if progression stalls because the player lacks a material, blueprint, level, or clear next target.

### Small population can invalidate social presentation

Marketplace, guild, chat, rankings, and PvP need other people. For a controlled alpha:

- Start with enough invited testers to populate at least two guilds.
- Make zero-result states explain how the system will populate.
- Avoid rewards that force a player into a currently empty system.
- Ensure PvE remains a complete experience.

### Recent large rewrites concentrate regression risk

Dungeon, power rating, combat summary, offline combat flow, and Crafting V2 all changed recently. The code is progressing quickly, but the alpha schedule must now reduce change volume and increase integrated verification.

### Balance is test-rich but playtest-light

Many content-validity and mechanic tests exist, but those cannot answer:

- Is the first hour fun?
- Does the first equipment upgrade arrive at the right time?
- Is the first Dungeon recommendation trustworthy?
- Are blueprint materials understandable and attainable?
- Does a failed temper feel fair?
- Does an offline return feel rewarding rather than noisy?

Days 9–10 are intentionally reserved for short simulations and human playthroughs instead of more features.

## Technical and Release Analysis

### Verified release blockers

#### 1. .NET target/runtime mismatch

Main `LL` projects target `net10.0`.

The checked-in delivery path still includes:

- `actions/setup-dotnet` with `8.x` in `.github/workflows/backend-build.yml`.
- `mcr.microsoft.com/dotnet/aspnet:8.0` in `build/ll-backend.dockerfile`.
- .NET 8 SDK/runtime stages in `LL/src/API/API.LL/Dockerfile`.

The worker image uses .NET 10, so the backend deployment is internally inconsistent.

**Required outcome:** Select .NET 10 as the supported main-game runtime and align SDK installation, Docker images, local documentation, and CI. Do not downgrade the code to .NET 8 during an alpha crunch unless there is a specific platform constraint.

#### 2. Angular production build failure

The production bundle compiles but fails the configured component-style maximum:

- Initial bundle: 744.22 kB, above the 500 kB warning threshold but below the 1 MB error threshold.
- Dungeon page component stylesheet: 18.91 kB, above the 4 kB error threshold.

**Required outcome:** Either reduce/move shared Dungeon styles or deliberately set a realistic reviewed component budget. A budget should not be raised blindly, but the current threshold is incompatible with the committed active Dungeon design.

The production build also fetches Google Fonts during optimization. A build without network access fails during font inlining. For deterministic artifacts, self-host the fonts or explicitly account for this build dependency.

#### 3. Delivery pipeline is not a release gate

The workflows:

- Trigger on pushes to `main` and `releases/**`, not pull requests.
- Build backend solutions but do not run the 564 backend tests.
- Do not run Angular unit tests.
- Have no end-to-end smoke test.
- Package and push artifacts after build without a formal release-candidate promotion step.

The PowerShell publish scripts also need explicit fail-fast behavior so a failed publish cannot continue into a Docker command using stale artifacts.

### Migration and data risks

The main database currently has:

- A July 23 base migration.
- A new uncommitted `RecipeScopedBlueprintUnlocks` migration.
- API startup code that calls `Database.MigrateAsync()` before serving requests.

The new migration changes blueprint uniqueness from character + blueprint to character + recipe + blueprint and adds nullable `RecipeId`.

Risks:

- The base-migration reset implies that existing databases need an explicit compatibility decision.
- Automatic migrations from every API startup are risky if replicas start together or a migration is slow/fails.
- A nullable `RecipeId` needs a clear policy for pre-existing unlocks.
- Content JSON and schema must be deployed as one compatible unit.

**Alpha recommendation:** Rehearse on a disposable copy, back up first, run migrations as an explicit release step, and make API auto-migration opt-in outside development.

### Configuration and security risks

Tracked configuration includes development defaults for:

- PostgreSQL credentials.
- JWT signing key.
- System-chat secret.

These may be local-only values, but they are dangerous if reused in an alpha environment. The alpha environment must provide unique secrets and rotate any value that has ever been deployed from the tracked defaults.

Issuer and audience validation are enabled only when the configured values are non-empty. Production/alpha startup should fail validation when any required JWT setting is absent.

The main API has no general ASP.NET rate limiter on register, login, Google login, refresh, or guest creation. The chat limiter is custom, uses distributed-memory caching in the current registration, and therefore does not coordinate across replicas.

The chat service has detailed SignalR errors enabled unconditionally and a high-severity AutoMapper advisory in version 14.0.0.

### Runtime and operations risks

- Readiness checks do not currently prove database/content/worker dependencies are ready.
- API and chat both migrate their databases at application startup.
- The worker is disabled by default in chart values.
- Marketplace expiration and Tournament progression are worker jobs.
- There is no visible global exception handler/standard problem-details pipeline for unexpected failures.
- Security event logging and account throttling remain open from the auth review.
- The repository does not prove backup, restore, alerting, log aggregation, or rollback behavior; those belong to the separate infrastructure environment and must be verified there.

## Prioritized Alpha Backlog

### P0 — release cannot proceed without these

1. Align the main game on .NET 10 across CI, publish, API runtime, worker runtime, and documentation.
2. Make the Angular production build pass and make font handling deterministic.
3. Turn CI into a real release gate: Release build, 564 tests, chat build/advisory check, Angular production build, and at least one golden-path smoke test.
4. Complete and stabilize the current Crafting V2 worktree; verify its migration and browser workflow on a clean database.
5. Rehearse base + Crafting migration, seed/content validation, API boot, chat boot, and worker boot in a clean alpha-like environment.
6. Remove reliance on tracked default secrets and validate required alpha JWT/CORS/database/chat configuration at startup.
7. Update the vulnerable chat dependency and verify chat authorization/rate behavior.
8. Prove register, guest, login, refresh, logout, bootstrap, Tutorial, idle combat, offline return, equipment, and first Dungeon as one end-to-end player journey.
9. Enable and verify the worker, or disable every feature that requires it.
10. Establish backup/restore and rollback procedures before the first persistent alpha database is opened.

### P1 — fix during the 14-day window

1. Add throttling for registration, login, guest creation, Google login, refresh, and sensitive write endpoints.
2. Add standard unexpected-error handling, correlation IDs, structured logs, and dependency-aware readiness.
3. Remove/hide dead UI: Forgot password, Raids, inaccessible credential-change panel, hardcoded version, and misleading “Beta” wording.
4. Add post-Tutorial next goals and ensure the first hour has no progression dead end.
5. Run the full Crafting and Dungeon vertical-slice tests, including reconnect and duplicate/retry behavior.
6. Test Marketplace, guild, chat, Colosseum, and Tournament flows with multiple accounts and low-population states.
7. Perform a first-hour, six-hour, and 24-hour balance simulation focused on material bottlenecks, power spikes, and currency floods.
8. Make feature flags/kill switches available for Crafting advanced features, Dungeons beyond the first family, Marketplace, and Tournament Grounds.

### P2 — useful only after P0/P1 are green

1. Improve currency source/sink tooltips and item/material source links.
2. Add gear comparison and build contribution guidance where existing data already supports it.
3. Improve small-screen and keyboard behavior on the densest screens.
4. Reduce warning noise, especially nullable warnings along hot persistence paths.
5. Add more frontend component tests.

### Explicitly defer until after alpha

- New regions.
- New Dungeon families.
- Raids and guild raids.
- Guild wars.
- New currencies.
- New classes/combat systems.
- More Tournament formats.
- PvP seasons and weekly reward expansion.
- Marketplace feature expansion.
- Active gathering minigames.
- Monetization.
- Admin content editors for every JSON catalog.
- Large architecture refactors that do not close a release risk.

## The 14-Day Execution Plan

Each day contains at most two major work packages. The acceptance criteria are the stopping rule: once they pass, stop polishing and move to the next day.

### Day 1 — Saturday, July 25: Make release artifacts possible

**Implementation status — started early on Friday, July 24**

- [x] Main-game and chat CI now select the .NET 10 SDK.
- [x] Both main API Dockerfiles now use the .NET 10 ASP.NET runtime, and the project-local build stage uses the .NET 10 SDK.
- [x] The main backend publish script now cleans API/worker output, stops after any failed native command, and supports `-SkipDockerBuild` for safe local publish verification.
- [x] API and worker Release publishes completed from seeded dirty output; both stale sentinel files were removed before packaging.
- [x] The Chat packaging script uses isolated output, cleans it before publishing, stops after failed publish/image commands, and supports `-SkipDockerBuild`.
- [x] Angular production font inlining is disabled, so compiling the artifact no longer requires access to Google Fonts. The browser still loads the existing fonts from Google at runtime; self-hosting is deferred.
- [x] The Dungeon stylesheet has an explicitly reviewed 16 kB warning/20 kB error budget. It currently emits an 18.94 kB warning but no longer blocks production.
- [x] Locked dependency installation and `npm run build` completed successfully. The initial bundle remains a visible 741.20 kB warning.
- [x] The frontend Linux workflow’s case-sensitive Dockerfile path was corrected.
- [ ] Run all four actual Docker image builds on a Docker-capable machine or in CI. Docker is unavailable in the current workspace, so no image was built or pushed locally.

**Work package A: Align .NET 10 delivery**

- Update the reusable backend workflow to install .NET 10.
- Align the main API Docker runtime with .NET 10.
- Align or remove the stale project-local API Dockerfile.
- Verify both API and worker publish from a clean output directory.
- Add fail-fast behavior to publish scripts and ensure artifacts are cleaned before packaging.

**Work package B: Fix the Angular production build**

- Resolve the Dungeon stylesheet budget failure through shared/global style extraction or an explicitly reviewed budget.
- Decide whether to self-host Google Fonts for deterministic builds.
- Confirm the real production Docker build runs `npm ci` and `npm run build` successfully.

**Acceptance criteria**

- Main API, worker, chat, and frontend production images build from the same commit.
- No step can package stale output after a failed compile/publish.
- `ng build --configuration production` exits 0.

### Day 2 — Sunday, July 26: Create a non-negotiable release gate

**Implementation status — started early on Friday, July 24**

- [x] Added reusable service-specific gates plus a manually dispatched full release gate; no pull-request workflow is used because this is a solo-development release.
- [x] Each deployment workflow runs only its own service gate before packaging or deployment jobs can begin.
- [x] The full release gate composes the main Release build and complete test project, Chat Release build, Angular production build, and cross-service smoke journey.
- [x] Local verification passed: 553 main tests, Chat Release build, and Angular production build.
- [x] High/critical .NET advisories are promoted to errors. The main graph passes; Chat is correctly blocked by its current AutoMapper and MessagePack findings.
- [x] Production npm dependencies are audited at high severity after a successful frontend build.
- [x] Added a disposable PostgreSQL-backed API smoke journey using generated accounts only: guest login, registration/login, both bootstrap paths, and Training Area discovery.
- [x] The real smoke journey passed locally from clean migrations and seed data, and its uniquely named temporary database was removed afterward.
- [ ] Run the complete gate in GitHub Actions. The gate is expected to remain red until its current dependency findings are fixed.

**Work package A: CI gate**

- Add service-specific deployment gates and a complete manually dispatched release gate; a pull-request workflow is intentionally unnecessary for the solo-development workflow.
- Run the main Release build and full test suite, Chat Release build, and Angular production build.
- Fail on known high-severity package advisories.
- Cache dependencies only after the clean path works.

**Work package B: Minimal release smoke test**

- Add a small browser/API smoke suite capable of starting with a clean account.
- Initially protect login/guest, bootstrap, and reaching the Training Area.
- Store no production credentials in the test.

**Acceptance criteria**

- A deliberately broken backend test or frontend build blocks every deployment workflow.
- No deployment packaging begins without green build/test status.
- CI output identifies the exact failing layer.

### Day 3 — Monday, July 27: Security and configuration hardening

**Work package A: Secrets and required configuration**

- Remove usable default database/JWT/chat secret material from committed non-development configuration.
- Generate unique alpha secrets outside the repository.
- Require non-empty JWT signing key, issuer, and audience in the alpha environment.
- Verify exact CORS origins for the alpha frontend.
- Disable detailed chat errors outside development.
- Rotate any tracked default that was ever used remotely.

**Work package B: Abuse and dependency fixes**

- Upgrade/replace the vulnerable chat AutoMapper dependency.
- Add rate limits for register, login, Google login, guest creation, token refresh, and other obvious anonymous abuse points.
- Ensure rate limits work with the intended replica count.

**Acceptance criteria**

- Alpha services fail fast with a clear error when required secure configuration is missing.
- No usable alpha secret is present in Git or built frontend assets.
- Repeated guest/login abuse receives `429`, while normal login/refresh works.
- Chat builds without a known high-severity advisory.

### Day 4 — Tuesday, July 28: Database, content, and worker rehearsal

**Work package A: Clean database rehearsal**

- Start from an empty alpha-like PostgreSQL database.
- Apply the base migration and `RecipeScopedBlueprintUnlocks`.
- Start API and verify all authored content validators and seeds complete.
- Decide how pre-existing blueprint unlocks with null `RecipeId` are handled.
- Capture migration duration and failure/rollback behavior.

**Work package B: Worker and scheduled jobs**

- Initialize the Quartz PostgreSQL schema.
- Enable one worker replica.
- Verify Marketplace expiration and Tournament progression jobs.
- Verify job idempotency by retrying the same business interval.
- Make production API auto-migration opt-in or move migration to an explicit release step.

**Acceptance criteria**

- API, chat, and worker start against clean databases.
- Read/write gameplay smoke calls work after migration.
- A backup can be restored to a second database.
- The worker visibly executes one safe smoke/expiration operation exactly once.
- The migration procedure is written down and repeatable.

### Day 5 — Wednesday, July 29: Protect the new-player golden path

**Work package A: Full account/Tutorial E2E**

- Cover registered signup, guest login, refresh/reload, logout, and guest conversion.
- Cover bootstrap and all five Tutorial steps.
- Fix the signup `/game` versus `/login` navigation race.
- Verify Tutorial rewards cannot be duplicated by refresh/retry.

**Work package B: First-session UX**

- Hide the dead Forgot Password action or replace it with a clear alpha support instruction.
- Confirm every Tutorial route/overlay works at desktop and mobile widths.
- Add a lightweight post-Tutorial goal: enter Lumo Ruins, start combat, and identify the next upgrade.

**Acceptance criteria**

- A fresh registered player and a fresh guest can complete the Tutorial without a manual database edit or browser refresh.
- Reloading at every Tutorial step resumes correctly.
- No primary first-session control is dead.
- The first post-Tutorial action is explicit.

### Day 6 — Thursday, July 30: Finish and lock Crafting V2

**Work package A: Crafting vertical slice**

- Learn a blueprint for a selected compatible recipe.
- Prevent invalid/duplicate learning for the same recipe while allowing the intended recipe-scoped behavior.
- Craft base and blueprint items.
- Verify exact material consumption and inventory updates.
- Queue tempering, resolve the Character Action, and verify potential/rarity/affix results.
- Cover reconnect/reload during the queue.

**Work package B: Migration and UI finish**

- Review and commit the current Crafting V2 change set as one coherent release unit.
- Verify DTO/frontend enum alignment.
- Show missing material and blueprint sources where data exists.
- Ensure advanced options have clear disabled reasons.

**Acceptance criteria**

- Automated integration coverage protects the main command/query paths.
- The browser flow passes on the Day 4 clean database.
- No inventory count requires a manual refresh to become correct.
- All Crafting V2 changes and the migration are committed; no partial work remains in the release worktree.

**Fallback**

If this is not green by the end of Day 6, disable advanced blueprint learning/tempering UI and ship only the verified base-crafting slice.

### Day 7 — Friday, July 31: Harden idle combat and offline return

**Work package A: Action/reward correctness**

- Test start, stop, resolve, reconnect, and 24-hour capped offline progress.
- Test duplicate HTTP/realtime/outbox delivery and ensure rewards apply exactly once.
- Verify inventory, character XP, Essence XP, Prophecy progress, achievements, and guild contributions remain consistent.
- Test loss/no-progress and empty-reward cases.

**Work package B: Reward comprehension**

- Ensure the return summary groups rewards by purpose: power, Crafting, Essence, Dungeon access, and currencies.
- Show the action's next useful destination without building a large recommendation engine.

**Acceptance criteria**

- Repeated resolve/reconnect requests do not duplicate rewards.
- Offline resolution has bounded execution time and a visible recoverable error state.
- The player can identify at least one meaningful upgrade from the result.

### Day 8 — Saturday, August 1: Harden the first Dungeon

**Work package A: Complete Goblin Mines workflow**

- Verify entry cost, readiness recommendation, run creation, room progression, Vigor, combat, death/abandonment, reconnect, completion, first-clear reward, repeat reward, mastery, and claim.
- Verify action and reward ownership with two accounts.
- Verify claims are idempotent.

**Work package B: Catalog/calibration sanity**

- Run simulation diagnostics for all nine difficulties.
- Check recommendations for monotonicity and obvious impossible/easy outliers.
- Confirm all Dungeon-gated crafting resources have a reachable source.

**Acceptance criteria**

- Goblin Mines passes end-to-end in browser/API tests.
- No reward can be claimed twice.
- Power recommendations are present or fail gracefully.
- The production stylesheet/build remains green.

**Fallback**

If other families fail the same workflow, expose Goblin Mines only for the first alpha build.

### Day 9 — Sunday, August 2: First-hour UX, mobile, and unfinished surfaces

**Work package A: Guided first-hour playtest**

- Complete two fresh-account playthroughs without developer knowledge.
- Record every hesitation, unclear currency, dead end, and forced menu search.
- Fix only issues that block comprehension or progression.
- Add compact explanations for the top early currencies and systems.

**Work package B: Release-facing UI cleanup**

- Hide the Raids tab.
- Remove inaccessible “coming soon” panels.
- Use the generated app version instead of hardcoded `1.0.0`.
- Change “Beta” wording to “Alpha.”
- Add real patch notes, support contact, known-issues link, and data-wipe expectation.
- Check the densest core screens at phone, tablet, laptop, and large-desktop widths.

**Acceptance criteria**

- A new tester reaches Lumo Ruins and understands the next goal without verbal coaching.
- No visible primary navigation surface is an empty feature shell.
- The UI displays the exact deployed version/commit-derived version.
- Core actions are usable at 390 px width and keyboard focus is visible.

### Day 10 — Monday, August 3: Economy and progression sanity

**Work package A: Progression simulations**

- Simulate or play the first hour, six hours, and 24 hours.
- Track level, combat power, equipment upgrades, Essence progression, Soulstones, Cinders, materials, sigils, and blueprint access.
- Check for impossible recipe requirements, progression stalls, runaway currency, and rewards that invalidate earlier content.

**Work package B: Targeted balance pass**

- Tune only severe Region One outliers.
- Verify first-clear Dungeon rewards and repeat blueprint chances.
- Verify Prophecies do not frequently demand unavailable/empty social content.
- Verify tempering failure/negative outcomes are clearly communicated.

**Acceptance criteria**

- Every required early material has a discoverable source.
- The first meaningful equipment/Essence upgrade arrives within the intended first session.
- The first Dungeon has a believable preparation horizon.
- No general currency becomes obviously infinite or useless in the 24-hour simulation.
- All changed balance values are recorded in patch notes.

### Day 11 — Tuesday, August 4: Multiplayer and low-population behavior

**Work package A: Multi-account integrity**

- With at least five accounts, test guild invitations/applications, contributions, missions, orders, shop, and leave/rejoin behavior.
- Test Marketplace list, buy, buy order, cancel, expiration/refund, and ownership.
- Test chat general/guild/whisper authorization and rate limits.
- Test Colosseum challenge, tickets, records, defense snapshots, and repeated requests.

**Work package B: Small-alpha presentation**

- Improve empty states for rankings, Marketplace, guild search, Tournament, and opponent lists.
- Ensure no Prophecy or Tutorial requires another player.
- Decide whether Tournament Grounds is enabled for launch based on worker and participant tests.

**Acceptance criteria**

- Cross-account ownership attempts fail safely.
- Repeated purchase/battle/contribution requests do not double-spend or double-reward.
- Empty systems look intentionally quiet, not broken.
- Tournament Grounds is either proven with the worker or disabled by configuration.

### Day 12 — Wednesday, August 5: Operations and full dress rehearsal

**Work package A: Operability**

- Add/verify standard unexpected-error responses with correlation IDs.
- Log authentication security events, migration/startup, outbox backlog, worker job success/failure, and high-value economy operations.
- Make readiness verify the dependencies needed to accept players.
- Define alerts for API/chat 5xx, auth failure spikes, database unavailability, worker/job failures, and outbox backlog.
- Verify the global request-disable switch and per-feature kill switches.

**Work package B: Release-candidate dress rehearsal**

- Deploy the exact release process to an alpha-like environment from a clean tag/commit.
- Run the golden path on desktop and mobile.
- Leave accounts offline, redeploy/restart all services, then return and resolve progress.
- Execute backup and restore.

**Acceptance criteria**

- A failure can be traced from the player-visible error to one correlated log trail.
- API, chat, worker, database, and critical job health are visible.
- The release candidate survives restart/reconnect/offline-return.
- Restore and rollback procedures have measured completion times.

### Day 13 — Thursday, August 6: Code freeze and release candidate

**Work package A: Final regression**

- Freeze features and balance.
- Run all CI gates from a clean checkout.
- Run the complete golden path and multi-account smoke suite.
- Verify production artifacts use the same immutable version/tag.
- Confirm migration, content, and frontend contracts match.

**Work package B: Release readiness**

- Prepare final patch notes and known issues.
- State whether alpha data may be wiped.
- Prepare Discord/support triage templates.
- Back up the target database.
- Prepare one-command/request rollback to the prior artifact and documented database response.
- Decide go/no-go against the gates below.

**Acceptance criteria**

- No open P0 issue.
- Every accepted P1 issue has an owner, mitigation, and known-issue entry.
- No uncommitted release change.
- The release candidate is immutable and has passed every gate.

### Day 14 — Friday, August 7: Controlled alpha release

**Work package A: Release**

- Apply the reviewed migration procedure.
- Deploy database/config, worker, API, chat, and frontend in the rehearsed order.
- Run health checks and golden-path smoke tests before inviting players.
- Open access to a small first wave, then expand only if metrics remain healthy.

**Work package B: Monitor and triage**

- Watch errors, auth, database, worker jobs, outbox, latency, and Tutorial completion.
- Triage player reports into P0 live issue, P1 hotfix, or post-alpha backlog.
- Make no feature or balance changes on release day unless they resolve a severe live issue.

**Acceptance criteria**

- Fresh account and guest paths work in the live alpha.
- Tutorial, idle combat, offline return, Crafting slice, first Dungeon, and logout/refresh pass live smoke tests.
- No sustained 5xx/auth/job anomaly.
- Roll back or disable the affected feature immediately if a P0 gate regresses.

## Alpha Go/No-Go Gates

Release only if every P0 gate is green.

### Build and artifact gates

- [ ] Main game Release build passes on .NET 10.
- [ ] Main API and worker images use compatible .NET 10 runtimes.
- [ ] Chat Release build has no known high-severity package advisory.
- [ ] Angular production build passes.
- [ ] All images are built from the same immutable commit/version.
- [ ] Build scripts fail fast and cannot use stale artifacts.

### Automated quality gates

- [ ] All 564 current backend tests pass.
- [ ] New account/Tutorial golden-path test passes.
- [ ] Crafting vertical-slice test passes.
- [ ] Idle/offline reward idempotency test passes.
- [ ] First Dungeon vertical-slice test passes.
- [ ] Multi-account ownership/concurrency smoke tests pass.
- [ ] PR CI runs these gates before merge.

### Database/content gates

- [ ] Base and RecipeScopedBlueprintUnlocks migrations pass on a clean database.
- [ ] Existing alpha-data upgrade behavior is known and tested.
- [ ] Content validation passes at startup.
- [ ] Backup and restore have been rehearsed.
- [ ] Migration rollback/forward-fix decision is documented.

### Security/configuration gates

- [ ] Unique alpha JWT/database/chat secrets are externally supplied.
- [ ] JWT issuer and audience are non-empty and validated.
- [ ] Exact alpha CORS origin is configured.
- [ ] Anonymous auth/guest endpoints are rate-limited.
- [ ] Chat detailed errors are disabled outside development.
- [ ] Cross-account Marketplace/Dungeon/guild/chat authorization is verified.

### Runtime/operations gates

- [ ] Worker is running, or worker-dependent features are disabled.
- [ ] Marketplace expiration and Tournament jobs are verified if enabled.
- [ ] API/chat readiness reflects necessary dependencies.
- [ ] Unexpected failures have correlation IDs and useful logs.
- [ ] Kill switch and rollback procedure are tested.
- [ ] Support channel, known issues, and data-wipe policy are visible.

### Player-experience gates

- [ ] Fresh register and guest paths reach the game.
- [ ] Tutorial completes without refresh or developer intervention.
- [ ] Player can start combat and return from offline progress.
- [ ] Player can understand and equip an upgrade.
- [ ] Verified Crafting slice works.
- [ ] First Dungeon works end-to-end.
- [ ] No primary navigation item leads to a dead feature.
- [ ] Core flow is usable on mobile and desktop.

## Day-One Metrics

Do not build a large analytics platform in the next 14 days. Capture these through existing database state, structured events, and logs:

### Reliability

- API and chat availability.
- 5xx rate by endpoint.
- P50/P95 latency for bootstrap, action resolution, inventory, and Dungeon actions.
- Database connection and query failures.
- Outbox pending/failed delivery count and oldest age.
- Worker job success, failure, and last-success time.
- SignalR reconnect failures.

### Acquisition/onboarding

- Register success/failure.
- Guest creation success/failure.
- Login and refresh success/failure.
- Tutorial started.
- Completion and drop-off count for each of the five steps.
- Median time to Tutorial completion.
- Median time to first Lumo Ruins combat.

### Core loop

- Combat actions started/stopped/resolved.
- Offline resolve duration and failure count.
- Duplicate/idempotency protection triggers.
- First equipment equipped.
- First Essence absorbed/equipped.
- First item crafted/tempered.
- First Dungeon started/completed/abandoned.

### Economy guardrails

- Cinders, Soulstones, major materials, and sigils created/spent per active player.
- Marketplace transaction and cancellation failure rates.
- Unusually large inventory/currency deltas.
- Dungeon and Prophecy reward claim failures.

### Social health

- Chat send failure/rate-limit counts.
- Guild joins and weekly contributors.
- Marketplace active listings/orders.
- Colosseum participants and successful battles.
- Tournament registrations if enabled.

## Hotfix and Rollback Policy

Classify live issues immediately:

### P0 live issue

Examples:

- Login/refresh unavailable.
- Data corruption or duplicated rewards/currency.
- Cross-account access.
- Migration failure.
- Core combat/action resolution unavailable.
- Unbounded server error/latency.

Action:

- Disable the affected feature or all requests.
- Preserve logs and database evidence.
- Roll back the artifact if schema-compatible.
- Prefer a forward data fix if rollback would conflict with an applied migration.
- Communicate status and data impact immediately.

### P1 live issue

Examples:

- One secondary system unavailable.
- Confusing but recoverable Tutorial/UI step.
- Bad non-exploitable balance value.
- Broken cosmetic/title display.

Action:

- Add to known issues.
- Hotfix only after the fix passes the same relevant CI and smoke gates.
- Batch low-risk fixes rather than deploying continuously on launch day.

## Recommended Alpha Scope Statement

Use a clear promise such as:

> Legends Legacy Alpha includes the first region, idle and offline combat, creature Essence collection and buildcraft, equipment Crafting and Tempering, three Dungeon families, Prophecies, achievements, guild progression, Marketplace, chat, leaderboards, and asynchronous Colosseum features. Systems, balance, and progression may change, and alpha data may be reset when necessary.

If Tournament Grounds or advanced Crafting does not pass its gate, remove it from this statement rather than shipping it in a knowingly unstable state.

## Verification Performed for This Analysis

### Passed

- `dotnet test LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj` using an isolated output path:
  - **564 passed**
  - **0 failed**
  - **0 skipped**
- `dotnet build LL/LegendsLegacy.sln --configuration Release` using an isolated output path:
  - Build passed.
  - 61 warnings, primarily nullable-flow and DTO member-hiding warnings.
- `dotnet build LL-Chat/LL-Chat.sln --configuration Release` using an isolated output path:
  - Build passed.
  - Reported a high-severity AutoMapper 14.0.0 advisory.
- Angular development build:
  - Passed.
- `git diff --check`:
  - No whitespace errors; line-ending conversion warnings were reported.

### Failed or blocked

- Angular production build:
  - Failed because `dungeon-page.component.scss` is 18.91 kB against a 4.10 kB maximum error budget.
  - The first sandboxed attempt also showed that font inlining requires network access; the build reached the real budget failure when network access was allowed.
- Angular unit tests:
  - Could not execute because ChromeHeadless repeatedly failed to start its GPU process in the current Windows environment.
- First backend test attempt:
  - Build output was locked by an already-running API/Visual Studio process. The suite passed after using an isolated output path.
- No database migration was applied.
- No external environment was deployed or changed.
- No live browser player journey was completed as part of this source audit.

## Final Recommendation

Release on August 7 only as a small, invite-based alpha and only if the P0 gates are green by the end of August 6.

The highest-value work is not another game system. It is converting the strong existing game into a trustworthy release:

1. Fix the delivery path.
2. Secure and validate configuration.
3. Prove migration and worker behavior.
4. Automate the first-player journey.
5. Lock Crafting V2.
6. Prove idle return and the first Dungeon.
7. Remove unfinished presentation.
8. Simulate the first 24 hours.
9. Rehearse, freeze, and release cautiously.

If schedule pressure forces cuts, cut exposed feature breadth in this order:

1. Tournament Grounds.
2. Advanced Marketplace orders.
3. Advanced Crafting/Tempering options.
4. Dungeon families beyond Goblin Mines.
5. Other low-population social surfaces.

Do **not** cut account integrity, migration rehearsal, idle reward correctness, Tutorial reliability, observability, backup/restore, or rollback. Those are the actual foundation of a successful alpha.
