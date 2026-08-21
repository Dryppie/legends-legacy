# Rift System — Game Design Document

**Status:** Design proposal (not implemented)
**Author:** Design pass, 2026-08-18
**Feature name (player-facing):** **Rifts**
**Scope:** Open-world, region-scoped, cross-player. No guild involvement, no invites, no friend lists.
**Sibling document:** `docs/raid-system-design.md` (Raids). Read §4 and Appendix A of that doc first — the
two features share the snapshot and reward-table machinery, and this document deliberately
does not re-litigate decisions made there.

---

## 0. Executive summary

A **Rift** is a tear that opens in a **region** at an unannounced time, stays open for a short **join
window**, and then collapses into a single server-resolved battle. Any player who meets the region's
level band may **step through** while the window is open. Everyone who stepped through is automatically
formed into one **Warband** — no invites, no leader, no ready check — and fights the **Rift Anchor**
together. The fight is one deterministic simulation executed by a background worker after the window
closes. Every participant receives a claimable reward grant sized by the Warband's outcome and their own
contribution.

The load-bearing design decision is this: **all player agency happens before the window closes, never
during the fight.** A Rift is not a live encounter that a present player plays and an absent player
watches. It is a commitment window followed by a resolution. A player who steps through and immediately
closes the game gets exactly the same simulation as a player who watched the countdown. That property is
what makes the planned subscription perk — automatic Rift joining — a convenience rather than an
advantage, and it is what makes Rifts work at all in a game where the population is spread across every
timezone.

Three things about this codebase shaped the design more than any genre convention:

1. **Nothing in the game resolves combat while the owner is absent, except World Tower.** Idle combat is
   lazy and pull-based (`CharacterActionService.GetCharacterActionAsync` → `CombatService.PerformIdleCombatAsync`);
   there is no scheduler that touches a character who is not polling. World Tower's lease-based
   `WorldTowerCombatSimulationWorker` is the one existing precedent for server-authoritative resolution
   of a multi-player fight, and Rifts follow it closely rather than inventing a second pattern.
2. **There is no party, queue, lobby or matchmaker anywhere in the solution.** `DbSet<Party>` /
   `DbSet<PartyMember>` are commented out in both `IDbContext` and `LLDbContext`; the types were never
   written. `Matchmak*` matches zero files. The only implemented grouping aggregates are `TowerRally`
   (leader-approved) and `TournamentTeam` (merged into threes at bracket time). A Rift Warband is
   therefore new state — but it is the _simplest possible_ new state, because membership is
   append-only and closes forever when the window closes.
3. **Rifts have dormant integration hooks in the codebase.** `Domain.Models.Regions.Region` carries a
   commented-out `//public ICollection<Rift> Rifts { get; set; }`;
   `Application.WebSockets.Contracts.GameEventMsg` already declares
   `public record RiftOpenedMsg(Guid ZoneId, DateTimeOffset Time)` with a matching commented-out
   `// RiftOpened: RiftOpenedMsg;` in the client's `game-event.map.ts`. An earlier proposal reserved
   The Hive's Abyss for this system, but that encounter and its ant roster now belong to Raids. Rift
   launch content therefore needs a distinct encounter family.

### What this reuses instead of inventing

| Need                                            | Existing thing it reuses                                                                                                                     |
| ----------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| N-vs-M combat                                   | `FastCombatEngine.Run(friendly, hostile, …)` — already N-vs-M, no party-size limit anywhere in the engine                                    |
| Absent participants                             | `CharacterSnapshot` + `ICharacterSnapshotService.CreateAsync` (server-side, needs no player input)                                           |
| Server-side resolution while the player is away | `TowerAttempt` lease model: `IWorldTowerWorkLeaseService.ClaimSimulationsAsync` + `WorldTowerCombatSimulationWorker`                         |
| Scheduled world clock                           | Quartz, `BackgroundJobGroups.World` (currently **zero jobs** — the natural home) + `IBackgroundJobExecutionService.RunOnceAsync` idempotency |
| Deterministic spawn schedule                    | `Common.Randomness.StableRandom.Seed(...)` (SHA-256 over canonical strings)                                                                  |
| Enemy composition rolls                         | `WeightedSpawnSelector.SelectCreatures / SelectCreatureCount`                                                                                |
| Creature scaling to a difficulty band           | `ICreatureScaler.ApplyScaling(creature, area)` + `WorldTowerGuardianScaling.Apply` for the Anchor                                            |
| Boss authoring schema                           | `world-tower/tower-floors.json` (`guardianScaling`, `recommendedPowerRating`)                                            |
| Rewards                                         | `rewards/reward-tables.json` + `IRewardRoller` + `RewardRollContext` tag bonuses                                                             |
| Per-participant claimable payout                | `TournamentRewardGrant` (+ `TournamentRewardStatus`) and `ClaimTournamentRewardsCommand`                                                     |
| XP split across a party                         | `IExperienceRewardWriter.AddSplitExperienceAsync(IReadOnlyCollection<Guid>, int, ct)` — already multi-recipient                              |
| Cross-row locking before the row exists         | `ITournamentLockService.LockTournamentScheduleAsync` / `PostgresTournamentLockService`                                                       |
| Realtime                                        | `GameHub.WorldGroup` (`"world"`) — every logged-in client already calls `subscribeToWorld()` on connect                                      |
| Worker → client delivery                        | `OutboxGameRealtimeBroadcaster` → `GameEventTypes.RealtimeDeliveryRequested` → API.LL's `RealtimeDeliveryGameEventOutboxConsumer`            |
| Announcements                                   | `EventQuestChatGameEventOutboxConsumer` pattern (system chat POST with a deterministic `MessageId`)                                          |

---

## 1. Design pillars

1. **The fight is a consequence, not an activity.** Agency lives in the join decision and in the
   pre-fight Surge choice. Once the window closes, the outcome is a function of who joined, what they
   were wearing, and what they had already decided. Nothing about the simulation rewards being awake.
2. **Presence is worth a little; it must never be worth a lot.** A player standing at the rift when it
   opens should get a small, legible edge over an auto-joined subscriber — on the order of 5–8% of
   expected reward value, all of it from making a _fresh_ choice rather than a _standing_ one. Never
   more. The moment auto-join is the weaker option, the subscription is selling frustration.
3. **Rifts never interrupt what the player was doing.** A Rift participation is its own aggregate and
   does **not** touch `CharacterAction`. Idle combat keeps running; the `CombatSwitchLockSeconds = 10`
   switch lock is never engaged; nobody loses farming time by joining. Fiction: your Echo answers the
   rift while you keep hunting. This is the same fiction Tower and Colosseum already use.
4. **A thin server must still feel alive.** The design assumes that most rifts, most of the time, will
   have one or two real participants. Rift Echoes (§5.4) and per-capita-neutral Anchor scaling (§6.3)
   mean a rift with one player is a real fight with real rewards, and a rift with six is better but not
   required. No feature that only works at high population.
5. **Bounded rewards, no new Cinder faucet.** Per the economy doc's unambiguous position on Cinder
   inflation, Rifts grant no Cinders beyond incidental encounter drops. They pay in essence progression,
   crafting inputs, cosmetics, and a bounded Rift currency whose primary sink is _more Rifts_.
6. **Deterministic and inspectable end to end.** The spawn schedule, the enemy composition, the fight
   seed and the reward rolls are all derived from `StableRandom` over canonical inputs. A support ticket
   about "my rift went wrong" must be answerable by replaying it.

### Anti-pillars

- **No live input during the fight.** No abilities to press, no target switching, no interrupts. If a
  future feature wants that, it is a different feature.
- **No invites, no leader, no kick, no ready check.** Warband membership is "you stepped through".
- **No competitive contribution race.** Contribution is measured against _your own_ expected output for
  your power (§8.2), never against the other participants. A whale joining a rift must not make a new
  player's reward worse.
- **No rift-exclusive best-in-slot items landing finished.** Same rule as Sieges: blueprints and
  materials route through crafting.
- **No pay-to-join, no purchasable rift entries, no auto-join advantage beyond convenience.**
- **No new Quartz job for per-player mechanics.** `BackgroundJobs/README.md` forbids it explicitly. The
  only jobs Rifts add are world-clock jobs whose unit of work is a _rift_, never a _character_.

---

## 2. Vocabulary

| Term                       | Meaning                                                                        | Code shape                                        |
| -------------------------- | ------------------------------------------------------------------------------ | ------------------------------------------------- |
| **Rift definition**        | Authored content: which region, which Anchor, which adds, which reward tables. | `RiftDefinition` (JSON)                           |
| **Rift**                   | One instance that opened at a specific time in a specific region.              | `RiftInstance` (`RiftInstanceId`)                 |
| **Pulse**                  | The deterministic daily spawn schedule for a region.                           | `RiftPulseSchedule` (derived, not stored as rows) |
| **Join window**            | The interval during which players may step through.                            | `OpensAt` → `SealsAt`                             |
| **Step through**           | The act of joining. Captures a snapshot.                                       | `StepThroughRiftCommand`                          |
| **Warband**                | Everyone who stepped through one rift, plus any Echoes.                        | `RiftInstance.Participants`                       |
| **Rift Echo**              | A synthetic ally that fills a thin Warband. Deals damage, takes no reward.     | `RiftEcho` participant kind                       |
| **Anchor**                 | The rift's boss. Killing it seals the rift.                                    | `RiftAnchor` (authored)                           |
| **Surge**                  | A pre-fight buff choice applied to the whole Warband.                          | `RiftSurge`                                       |
| **Directive**              | A player's _standing_ Surge preference, used when they are absent.             | `RiftAttunement.StandingDirective`                |
| **Attunement**             | A character's standing Rift settings, incl. auto-join.                         | `RiftAttunement`                                  |
| **Seal / Hold / Collapse** | The three Warband outcomes.                                                    | `RiftOutcome`                                     |
| **Riftshard**              | Bounded Rift currency.                                                         | `Character.Riftshards`                            |
| **Rift Focus**             | A consumable that opens a rift of your choosing. The main Riftshard sink.      | Inventory item                                    |

Naming notes. Avoid **Warden** and **Sovereign** — both are taken by `TowerFloorType`, and Sieges use
Warden for its raid bosses. **Warband** is unused anywhere in the solution.

> **Fix the dead scaffolding rather than working around it.** `RiftOpenedMsg(Guid ZoneId, DateTimeOffset Time)`
> cannot be used as written: `Region.Id` is an `int` and `Area.Id` is a `string`, so `Guid ZoneId`
> matches no existing identity. Re-author it as
> `RiftOpenedMsg(Guid RiftInstanceId, int RegionId, string RiftDefinitionId, DateTimeOffset OpensAt, DateTimeOffset SealsAt)`
> and add the corresponding `DomainToClientMapper.Map` case plus the client `game-event.map.ts` entry —
> the client's dispatch is fail-silent, so an envelope with no registered name is received and dropped.
> Likewise uncomment `Region.Rifts` only if rifts are actually navigated from the region aggregate;
> since rift _instances_ are transient and region-scoped by `RegionId`, a navigation collection on the
> content entity is probably wrong. Delete the comment rather than honouring it.

---

## 3. The rift day

### 3.1 Pulse: random to players, deterministic to the server

Rifts must feel unscheduled. They must also be reproducible, testable, and knowable in advance by the
server so that auto-join can be evaluated without a live player. Both are satisfied by generating the
whole day's schedule deterministically and keeping it secret:

```
PulseSeed(regionId, dayKey) = StableRandom.Seed("rift-pulse-v1", regionId, dayKey)
```

`dayKey` is the UTC calendar day as `"yyyyMMdd"`. From that seed, for each region:

- draw `RiftsPerDay` from the region's authored range (default 5–7);
- place each rift's `OpensAt` inside the day by partitioning the 24 h into `RiftsPerDay` slots and
  drawing a uniform offset inside each slot, subject to a `MinimumSeparationMinutes` (default 45) so
  two rifts never overlap awkwardly in one region;
- draw the `RiftDefinition` for each slot from the region's weighted pool via `WeightedSpawnSelector.SelectIndex`.

The schedule is not persisted as rows. The `RiftSpawnJob` (Quartz, group `world`, 60 s simple trigger,
`RunOnceAsync` business key `rift-spawn:{yyyyMMddHHmm}`) recomputes the current day's pulse for every
region on each fire, and materialises a `RiftInstance` row only for pulses whose `OpensAt` has arrived
and which have no row yet. Idempotency comes from a unique index on
`(RegionId, RiftDefinitionId, OpensAt)`, not from the job's own bookkeeping.

Consequences worth stating plainly:

- **The same day replays identically** in dev, in tests and in production, which makes "why did no rift
  spawn in Shenic yesterday" a five-minute question rather than a forensic one.
- **A missed job window is self-healing.** If the worker was down for 20 minutes, the next fire
  materialises every pulse whose `OpensAt` has passed and is still inside its join window; anything whose
  window fully elapsed is skipped. This is exactly the misfire policy `BackgroundJobs/README.md` already
  prescribes for a _"World boss spawn window: Feature-specific; may skip if event window has passed"_.
- **Players cannot farm the schedule** unless they can compute `StableRandom` over a server-side salt.
  Add a deployment-scoped `Rifts:PulseSalt` to the seed identity so the schedule is not derivable from
  public information. Rotate it never — rotating it re-rolls history.

> **Do not use `SpawningService` for the pulse.** `Services.LL/Spawnings/SpawningService.cs` holds a
> `_random = new Random()` instance field and takes an optional `Random? random` parameter; it is not a
> deterministic source. Use its `WeightedSpawnSelector` (which takes an injected `Random`) with a
> `StableRandom`-seeded instance, or `IResolutionRandomSource.UseSeed`. Note `IRandomSource` alone does
> **not** guarantee determinism — `ResolutionRandomSource` falls through to `Random.Shared` outside a
> `UseSeed` scope.

### 3.2 Lifecycle

```
                 (deterministic, not stored)
   Pulse ────────────────────────────────────────────────────────────┐
                                                                     │
  OPEN ─────────────────────────────────────────────────────────────► │  RiftSpawnJob materialises the row
   │  • RiftInstance(Status = Open) written                           │  • world announcement (chat + SignalR)
   │  • auto-join evaluated for attuned characters (§9.3)             │  • StateSyncScopes.Rift invalidated
   │  • players step through at will; snapshot captured per joiner    │
   │  • Surge offered to each joiner; Directive used for auto-joins   │
   │        JoinWindowMinutes (default 25)                            │
   ▼                                                                 │
  SEALING ────────────────────────────────────────────────────────►  │  RiftResolutionJob claims the row
   │  • Status = Sealing; membership closed forever                   │  by lease, exactly like TowerAttempt
   │  • Echoes appended to reach MinimumWarbandSize                   │
   │  • one FastCombatEngine run, deterministic seed                  │
   │  • compact playback persisted for replay                         │
   ▼                                                                 │
  RESOLVED ──────────────────────────────────────────────────────►   │  per-participant RiftRewardGrant rows
   │  • RiftOutcome = Sealed | Held | Collapsed                       │  written Unclaimed, nothing else
   │  • participants notified (Audience.Characters)                   │  is mutated across characters
   ▼                                                                 │
  CLAIMED (per participant, lazily, under that character's own lock) ─┘
```

Two properties of this shape matter more than the diagram suggests.

**Membership closes before resolution begins.** There is no window in which the simulation is running
and someone can still join. That removes every race the feature would otherwise have.

**Resolution writes only grants.** The resolution transaction touches the rift aggregate and inserts one
`RiftRewardGrant` per participant. It does not touch `Character`, inventory, essences or experience for
anybody. Each participant later claims under their own `AcquireCharacterCommandLockAsync(characterId)`
via the normal `TransactionBehavior` path. This is the single most important structural decision in the
document, and §12.1 explains why.

### 3.3 Cadence and caps

| Knob                          | Default             | Rationale                                                                                   |
| ----------------------------- | ------------------- | ------------------------------------------------------------------------------------------- |
| Rifts per region per day      | 5–7                 | A player logging in twice a day should usually find one live or imminent.                   |
| Join window                   | 25 min              | Long enough that a push-free game still catches people; short enough to feel like an event. |
| Entries per character per day | 3                   | Scarcity is in entries, not in time. Resets at 00:00 UTC on the `dayKey`.                   |
| Auto-join entries             | Same 3              | Auto-join spends the same pool. It buys attendance, never volume.                           |
| Warband cap                   | 8 real participants | Perf and legibility. Overflow joins the next rift (§5.3).                                   |
| Minimum Warband size          | 3 (Echo-filled)     | Keeps the fight readable and the Anchor tuning stable.                                      |

Entries are a `RiftEntryLedger` counter keyed `(CharacterId, DayKey)`, evaluated lazily like
`GuildMissionService.GetWeek` does for weeks. No daily Quartz job.

> **Pick one calendar helper and put it somewhere shared.** There are currently four incompatible
> conventions: `int WeekKey` (`ISOWeek` `yyyyww`) on `TowerContribution`/`TowerEchoClear`, `string WeekKey`
> (`"yyyyMMdd"` of Monday) on `GuildMissionInstance` via `GuildMissionService.GetWeek`, a third
> independent `"yyyyMMdd"` reimplementation in `LeaderboardRepository.GetGuildWeekKey`, and
> `ArenaCalendar.GetCurrentWeeklyResetStart` returning a `DateTimeOffset`. Rifts need a _day_ key, which
> none of them provide. Add a `GameCalendar` static in Core with `GetDayKey(DateTimeOffset)` and
> `GetWeekKey(DateTimeOffset)`, migrate the two string implementations onto it, and do not add a fifth
> private helper. The Siege doc lists "a shared `WeekKey` helper" under _must be built_; this is the same
> item and whichever feature lands first should own it.

---

## 4. Joining

### 4.1 Stepping through

`POST /api/v1/rifts/{riftInstanceId}/step-through` → `StepThroughRiftCommand(Guid CharacterId, Guid RiftInstanceId, string? SurgeKey)`.

Gates, in order, all inside one transaction under the rift's advisory lock:

1. Rift exists and `Status == Open` and `now < SealsAt`.
2. Character's level is inside the definition's `levelBand` (`min`..`max`, inclusive).
3. Character has an entry remaining for today's `dayKey`.
4. No existing participant row for this `(RiftInstanceId, CharacterId)` — unique index.
5. No existing participant row for this `(RiftInstanceId, AccountId)` — unique index. One character per
   account per rift, exactly as `TowerRallyParticipant` enforces. Prevents alt-stacking a Warband.
6. `Participants.Count(kind == Player) < WarbandCap`.
7. `IPowerRatingService.GetCharacterOverallRatingAsync` returns `PowerAnalysisState.Available`. Tower
   already gates on this; a character with no computable rating cannot be scaled against.

On success: capture `ICharacterSnapshotService.CreateAsync(characterId, ct)`, store the resulting
`CharacterSnapshotId` on the participant row, record `PowerRatingAtJoin`, decrement the daily entry,
resolve the Surge (§7), broadcast `RiftParticipantsUpdated` to the participants and a light
`RiftUpdated` to `world`.

> **Resolve snapshots by id, never by character.** `ICharacterSnapshotService.GetSnapshotByCharacterIdAsync`
> is `FirstOrDefaultAsync(s => s.CharacterId == id)` with no ordering, and `CharacterSnapshots` has no
> uniqueness on `CharacterId` — every capture inserts a new row and nothing prunes. That method returns
> an arbitrary historical snapshot. Tower and Tournaments both store and read
> `CharacterSnapshotId`; Rifts must do the same.

### 4.2 The snapshot growth problem auto-join creates

This is the one place where the subscription feature has a real engineering cost, and it should be
priced in now rather than discovered later.

`CharacterSnapshot` has no `CapturedAt`, no version, and no owner account. Nothing prunes it. Tower FKs
are `DeleteBehavior.Restrict`, so referenced snapshots cannot be deleted at all, and
`SaveArenaDefenseSnapshotAsync` already orphans its previous snapshot permanently. Today that is
tolerable because captures are rare and player-initiated.

Auto-join changes the arithmetic: every attuned subscriber gets a fresh snapshot captured for every rift
they are auto-joined into — up to 3 per day each, server-initiated, forever. Each snapshot is one row
plus one row per base attribute, per equipment slot, per equipment modifier and per equipped essence.
For a thousand subscribers that is on the order of a hundred thousand child rows a week.

Required before auto-join ships (not before Rifts ship):

1. Add `DateTimeOffset CapturedAt` and `string Purpose` to `CharacterSnapshot`. Both are trivially
   backfillable and both are needed for any pruning policy at all.
2. A `SnapshotRetentionJob` (Quartz, `system` group) that deletes snapshots with `Purpose = "rift"`
   older than N days and referenced by no live aggregate. Rift participant FKs must therefore be
   `DeleteBehavior.SetNull` on the snapshot, with the participant row retaining a denormalised
   `PowerRatingAtJoin` and `LoadoutHash` so history stays legible after the snapshot is gone.
3. Reuse rather than recapture inside a short window: if an unmodified snapshot for this character
   exists from within `SnapshotReuseMinutes` (default 30) and the character's `LoadoutHash` is
   unchanged, point at it instead of capturing again. This requires the hash to actually be compared,
   which brings us to the next landmine.

> **`LoadoutHash` is currently write-only.** `ColosseumService.CreateLoadoutHash` computes a SHA-256 over
> attributes, equipment and essences; the value is written in `ColosseumService` and `ColosseumRepository`,
> surfaced in `ColosseumStatusDto`, and **never compared to anything**. `ArenaDefenseSnapshot.IsOutdated`
> is only ever written `false`. Also note the hash omits `Quality`, `Tier`, `StatModelVersion`, character
> `Level`, `BlueprintId` and essence `Level`/`CurrentXp` — two materially different loadouts can hash
> equal, which is fine for cache reuse and **not** fine for a staleness guarantee. If Rifts want reuse,
> extract the hash into a shared `ILoadoutHasher`, add the missing fields, and write a test that two
> loadouts differing only in essence level hash differently. `SnapshotLockMode { None, HardLock }` is a
> dead enum with zero references — do not assume it means anything.

---

## 5. The Warband

### 5.1 Formation is not matchmaking

There is no matchmaker and this design does not build one. A Warband is defined by a join, not by a
pairing algorithm: _everyone who stepped through this rift is in this Warband._ Definition-level level
bands do the segregation that a matchmaker would otherwise do, and there is exactly one of them per rift,
so no bucketing logic is needed.

This is deliberate and worth defending, because the obvious alternative — a queue that pools players
across rifts and forms balanced groups — requires a matchmaker, a queue, a pool, a rating band and a
timeout policy, none of which exist. (`ColosseumRepository.GetArenaOpponentsWithRating` is not a
counterexample: it loads _every character in the database_ via a `.Where(c => c.Id == characterId || c.Id != characterId)`
tautology and takes the nearest 25 by rating delta in memory. Do not model anything on it.)

The cost of no matchmaker is that Warband composition is arbitrary. §6.3 and §8.2 are designed so that
arbitrary composition is fine.

### 5.2 Roles are observed, not assigned

Rifts do not ask players to declare a role, and no officer assigns one. The resolution step _observes_
what the Warband happens to contain, using the same data-driven eligibility the Siege doc specifies
(`ApplyCondition: Taunt` / `ModifyThreat` for a front-liner; `Heal`, `GrantBarrier`,
`ModifyRegenerationRate` or `Cleanse` for a sustainer; an `AllEnemies`/`TwoEnemies`/`ThreeEnemies`-style
`AbilityTargetSelector` for add-clear), computed from the _resolved_ loadout rather than from
`abilities.json`, because `AbilitySpec.TargetingType` and `AbilityConversionFlags.AllowTargetingConversion`
let equipment and essences rewrite targeting at runtime.

Observation is used for two things only:

- **Echo selection** (§5.4): fill the gaps the Warband actually has.
- **Post-fight flavour**: "you held the Anchor's attention for 41% of the fight" is a good line in a
  result screen and costs nothing to compute from `EntityStats`.

It is explicitly _not_ used to gate participation or to penalise composition. A Warband of five glass
cannons is allowed to fail on its own merits.

> **Threat is roulette-wheel, not redirection.** `RuntimeCombatant.GetEffectiveThreat` = `Threat + (HasCondition(Taunt) ? TauntThreatBonus : 0)`
> with `BaseThreat = 100f` and `TauntThreatBonus` defaulting to `100f`, and `SelectThreatWeightedEnemy`
> draws `_random.NextDouble() * totalThreat`. A taunting front-liner at 200 against four allies at 100
> each draws ~33% of attacks. For rift Anchors, raise `TauntThreatBonus` per definition (e.g. `900f` →
> ~71%) so that a Warband containing a tank visibly survives longer. **This is currently unreachable
> through the executor:** `CombatSimulationOptions` has no `TauntThreatBonus` field and
> `CombatEngineExecutor` never forwards one. Either add the field and thread it through `ExecuteCoreAsync`
> (preferred — Sieges need the same thing) or construct `FastCombatEngine` directly in the rift resolver.
> Do not ship a design that claims tanks matter while passing the default.

### 5.3 Overflow

If a rift is at `WarbandCap` when a player tries to step through, the join fails with a distinct result
(`RiftJoinRejection.WarbandFull`) and the response carries the region's _next_ pulse time if it is inside
the next two hours. Never a queue, never a waitlist — a waitlist is a promise the server would have to
keep across a restart.

If a region reliably hits the cap, that is a signal to raise `RiftsPerDay` for that region, which is a
JSON change. Populated regions getting more rifts is the correct response to popularity.

## 6. Resolution — how one rift actually resolves

This is the load-bearing mechanic, so it is specified precisely.

### 6.1 What runs

`RiftResolutionJob` (Quartz, group `world`, 30 s trigger, `RunOnceAsync` key `rift-resolve:{yyyyMMddHHmmss}`)
selects up to `ResolutionBatchSize` (default 5) rifts where `Status == Open && now >= SealsAt`, and for
each one claims a lease exactly as `IWorldTowerWorkLeaseService.ClaimSimulationsAsync(owner, now, limit, ct)`
does — `SimulationLeaseOwner` / `SimulationLeaseUntil` columns on `RiftInstance`, renewed on a heartbeat,
with `SimulationAttempts` incremented so a poisonous rift dead-letters instead of looping. Then:

1. Set `Status = Sealing` and commit. Membership is now closed; any in-flight step-through fails on the
   status gate. This is a separate, tiny transaction on purpose.
2. Load every participant's `CharacterSnapshot` **by `CharacterSnapshotId`** with attributes, equipment,
   modifiers and essences (`GetWorldTowerRallyWithSnapshotsAsync` is the query shape to copy).
3. Rehydrate each into a combatant. Use the extracted `ISnapshotCombatantBuilder` (§12.3) — do not add a
   fourth private copy.
4. Append Echoes (§5.4).
5. Build the hostile side from the definition for this rift: one **Anchor** plus an add group whose count
   and composition are rolled from `WeightedSpawnSelector` under a seed derived from the rift identity.
6. Scale the Anchor via `WorldTowerGuardianScaling.Apply(anchor, definition.anchorScaling)` — remember
   each value is a _multiplier_ converted to `new DungeonAttributeModifier(attr, (m - 1f) * 100f, ModifierType.Multiplicative)`,
   so `health: 36.0` means +3500%, and that `Penetration` drives both `ArmorPenetration` and
   `MagicPenetration`. Scale adds via `ICreatureScaler.ApplyScaling(creature, area)` against the
   definition's `anchorAreaId` (a real `Area`, so `DifficultyTier` is authentic rather than the
   `new Area { DifficultyTier = 1 }` fake World Tower uses).
7. Apply Anchor health for the sealed participant count (§6.3) and the Warband's resolved Surge (§7).
8. Compose one `CombatEncounterPlan` with `Mode = CombatMode.Rift` (new enum value `5`),
   `SourceContext = new RiftEncounterSourceContext(Guid RiftInstanceId, string RiftDefinitionId, int RegionId, string AnchorAreaId)`,
   and `RandomSeed = StableRandom.Seed("rift-encounter-v1", riftInstanceId, sealedAt.UtcTicks)`.
9. Execute once via `ICombatEngineExecutor.ExecuteSimulationAsync(runtime, new CombatSimulationOptions(seed, MaxTicks: 1800, CaptureEventLog: false), ct)`
   for the authoritative result, and `ExecuteCompactPlaybackAsync` when the definition enables replay.
10. Classify the outcome (§6.4), compute per-participant contribution (§8.2), write grants, set
    `Status = Resolved`, commit, then broadcast.

> **`CombatantFactory` throws for every non-idle source context.** `Create(slot, sourceEntity, sourceContext)`
> handles `IdleEncounterSourceContext` for creatures and throws `NotSupportedException` for
> `DungeonEncounterSourceContext` and `RaidEncounterSourceContext`, and `InvalidOperationException` for
> `PvpEncounterSourceContext`. World Tower, Colosseum and Tournaments all sidestep it by hand-building
> `CombatRuntimeParticipant` lists. Add the `RiftEncounterSourceContext` branch properly — it has an
> `AnchorAreaId`, which is exactly the data the creature branch needs for `ICreatureScaler`, so this is
> the one context that can be implemented cleanly without inventing environment plumbing.

> **Register the mode or the coordinator throws.** `CombatOrchestrationCoordinator` builds
> `orchestrators.ToDictionary(x => x.Mode)` and throws `"No combat orchestrator is registered for mode 'X'"`
> at dispatch (and throws at construction on duplicate modes). `CombatMode.Raid` and `CombatMode.Pvp`
> exist with no registered orchestrator today. Rifts should register a real
> `RiftCombatOrchestrator` + `RiftCombatOutcomeProcessor` pair in
> `Services.LL/DependencyInjection.AddCombatDependencyInjection()`, or deliberately not use the
> orchestration layer at all — but pick one. The half-measure (a `CombatMode` value with no orchestrator)
> is what left `Raid` and `Pvp` as traps.

> **State the tick budget explicitly.** Three defaults disagree: `FastCombatEngineOptions.MaxTicks = 6000`,
> `CombatSimulationOptions.MaxTicks = 1800`, and `ExecuteAsync`/`ExecuteWithCheckpointsAsync` hardcode 6000. Rifts are balanced at **1800 ticks (180 s)** and must pass it explicitly. Also:
> `CombatResult.Duration` is in **ticks**, not seconds — divide by `TicksPerSecond` (10) before showing a
> fight length; and `DetermineOutcome` returns `Draw` both for "tick limit reached" and "mutual wipe on
> the same tick", so detect timeout on `Duration >= maxTicks`, never on `BattleOutcome`.

### 6.2 Cost, and why one rift is cheap

The measured idle benchmark is 8 640 single-player encounters in 20.16 s — **429 encounters/s, ~2.3 ms
and ~2.49 MiB allocated per encounter**, with 94% of the time inside the simulation. A rift is one
encounter with up to 8 friendly combatants plus an Anchor and adds, at 1800 ticks rather than the idle
distribution, so call it 30–80 ms and 30–60 MiB. Against a worst case of 8 regions × 7 rifts = 56 rifts a
day, total rift simulation cost is a rounding error next to a single player's 24 h catch-up.

The real cost risks are elsewhere, and both are avoidable:

- **Event-log capture.** `CaptureEventLog: true` switches the stats populator from `CreateBalanceStats`
  to `CreateDetailedStats`. Default rifts to `false` and persist a compact playback bundle only where
  replay is authored on (`TowerCombatPlayback` already caps bundles at 16 MiB raw / 4 MiB compressed —
  reuse those caps).
- **Snapshot loading.** 8 participants × (attributes + equipment + modifiers + essences) is a wide
  include graph. Load it in one query per rift, `AsNoTracking`, and never per participant.

### 6.3 Anchor health and per-capita fairness

Anchor health is computed at seal time from the sealed Warband, not authored per party size:

```
AnchorHealth = definition.anchorHealthPerParticipant × (0.60 + 0.40 × EffectiveParticipants)
EffectiveParticipants = PlayerCount + 0.60 × EchoCount
```

At one player: ×1.00. At three: ×1.80. At five: ×2.60. At eight: ×3.80. Damage scales roughly linearly
with participants while health scales at 0.4 per head, so **more participants is always better, and one
participant is still viable.** That asymmetry is intentional: it makes joining a busy rift attractive
without making a quiet rift a waste of an entry.

Echoes count at 0.6 so that Echo-padding a solo Warband raises the Anchor's health by less than the
Echoes contribute — otherwise the floor mechanic would quietly punish the players it exists to help.

The Anchor's _offense_ does not scale with participant count. A Warband of eight does not want an Anchor
that hits eight times harder; it wants an Anchor that survives long enough to be interesting. Overtime
covers the rest:

**Soft timer.** Author `overtimeStartsAtTick` / `OvertimePowerIncreaseIntervalTicks` /
`OvertimePowerIncreasePercent` per definition (these already exist on `FastCombatEngineOptions` and are
used by tournaments). Default: overtime from tick 1200, +6% per 100 ticks. An underpowered Warband gets a
shorter effective fight and its damage falls off smoothly instead of off a cliff, which is what makes
`Held` (§6.4) a meaningful middle outcome rather than a rounding artefact.

### 6.4 Outcomes

Unlike a Siege, a rift **can** be killed in its single fight — that is the point of the format. Three
outcomes, derived from the authoritative `CombatResult`:

| Outcome       | Condition                                                                               | Reward multiplier |
| ------------- | --------------------------------------------------------------------------------------- | ----------------- |
| **Sealed**    | Anchor dead (`BattleOutcome.Victory`)                                                   | 1.00              |
| **Held**      | Timeout (`Duration >= maxTicks`) with Anchor below `holdThresholdPercent` (default 35%) | 0.60              |
| **Collapsed** | Warband wiped, or timeout with Anchor above the hold threshold                          | 0.30              |

A Collapse still pays. Nothing corrodes an open-world event faster than a bad roll of who happened to be
online returning nothing, and unlike a Siege there is no officer to blame.

### 6.5 Determinism and replay

```
PulseSeed      = StableRandom.Seed("rift-pulse-v1",     PulseSalt, regionId, dayKey)
CompositionSeed = StableRandom.Seed("rift-composition-v1", riftInstanceId)
EncounterSeed  = StableRandom.Seed("rift-encounter-v1", riftInstanceId, sealedAt.UtcTicks)
RewardSeed     = StableRandom.Seed("rift-reward-v1",    riftInstanceId, characterId)
```

Participant iteration must be ordered (`OrderBy(JoinedAt).ThenBy(CharacterId)`) — the friendly list order
affects targeting rolls, so an unordered enumeration silently breaks replay. All reward rolls run inside
`IResolutionRandomSource.UseSeed(RewardSeed)`; outside a `UseSeed` scope that source falls through to
`Random.Shared` and determinism is lost with no error. Per-character reward seeds mean re-running one
player's grant (e.g. after a support fix) does not perturb anybody else's.

---

## 7. Surges — the entire agency budget

A Surge is one Warband-wide modifier chosen before the fight. It is the only thing a player _decides_,
and it exists to make presence worth something without making absence bad.

### 7.1 How a Surge is chosen

Each rift definition offers a fixed set of 4–6 Surges. When a player steps through, they pick one (the
request carries `SurgeKey`; omitting it uses their Directive, and omitting both uses the definition's
default). At seal time the Warband's Surge is the **plurality winner across participants, ties broken by
earliest join**. Echoes do not vote.

That rule has three properties worth the small complexity: an early solo joiner effectively decides;
a group converges on whatever most of them wanted; and no player's choice can be _overridden by someone
joining later_, which would make joining early feel bad.

### 7.2 What a Surge does

Surges are authored as a small list of attribute and behaviour modifiers applied to the friendly side, the
hostile side, or the encounter options. They deliberately reuse existing primitives rather than
introducing a rift-only effect system:

| Example Surge | Implementation                                                                                                                   |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| **Onslaught** | `+12%` Power to the friendly side (`DungeonAttributeModifier`, multiplicative)                                                   |
| **Bulwark**   | `+18%` Armor and Resistance to the friendly side                                                                                 |
| **Unravel**   | `−15%` Anchor Armor and Resistance                                                                                               |
| **Culling**   | `+35%` friendly damage against adds only, via an add-side `−` mitigation modifier                                                |
| **Stillness** | Overtime starts 400 ticks later (`OvertimeStartsAtTick + 400`)                                                                   |
| **Greed**     | No combat effect; `+1` reward-table roll weight tag on the completion table via `RewardRollContext.EntryWeightBonusPercentByTag` |

`Greed` matters more than it looks: it gives a strong Warband a reason to pick something other than the
strongest combat option, which is what keeps the choice interesting after the first week.

### 7.3 Directives and the presence gap

A player's `RiftAttunement.StandingDirective` is a single Surge key, set once in settings, used whenever
they are auto-joined or step through without choosing. So an absent player still votes.

The presence advantage is therefore only this: a present player sees _this_ rift's definition, this
Warband's current composition, and the current vote tally, and can pick accordingly. A Directive is a
guess made in advance. Modelled, that is worth a few percent of expected value — which is precisely the
target in pillar 2. It requires no separate "manual bonus" and no reward asymmetry, which is why it is
the right shape: **the perk removes a chore, and the chore was worth about 6%.**

Do not add a flat participation bonus for being online. It converts a convenience subscription into an
attendance tax on everyone else.

---

## 8. Contribution and rewards

### 8.1 What is measured

From the authoritative `CombatResult.EntityStats`, per participant:

- `AnchorDamage` — damage dealt to the Anchor.
- `AddDamage` — damage dealt to adds.
- `Mitigated` — damage absorbed and healing/barrier provided (`BarrierAbsorbed`, `DamageBlocked`, heals).
- `ThreatShare` — fraction of Anchor attacks received.

Two measurement details that must not be got wrong, both inherited from the Siege analysis:

- **`EntityStats.DamageDone` accumulates `CombatLogItem.Magnitude`, which is post-mitigation,
  post-barrier `healthBefore - target.Health`.** Barrier-absorbed damage lands in `BarrierAbsorbed` /
  `DamageBlocked` instead, and overkill is clipped. That is the right measure for "how much of the
  Anchor did you remove", but a barrier-heavy Anchor requires contribution accounting to include the
  barrier damage explicitly.
- **Summon damage is attributed to the summon's own entity id** (`CombatStatsAggregator.GetOrAddEntity(actorId)`),
  not the owner. Sum over the participant _plus every `IsSummoned` entity on the friendly side_, or
  summon builds read as near-zero contribution.

### 8.2 What Rifts pay

Rift definitions carry reward tables following the established namespacing:

```
reward.rift.<rift_key>.completion            // the guaranteed floor: materials + Riftshards
reward.rift.<rift_key>.first_seal            // first time this character seals this rift
reward.rift.<rift_key>.region_cosmetic       // low-weight WeightedWithNoDrop
```

Composition, per the user's chosen payout mix and the economy constraints:

1. **Essence progression** — Soul Dust, monster cores and evolution catalysts in quantity. Essence
   progression is currently gated on scarce catalysts; a repeatable daily group activity is the right
   faucet for them, and rift creatures are already wired into the essence catalog with a `"Rift" => 2`
   weighting.
2. **Crafting materials and blueprint copies** — tiered materials matching the region band, plus
   rift-flavoured blueprint copies. Blueprint copies and crafted output are marketplace-tradeable, so
   non-participants have a reason to care that Rifts exist. Finished rift items, if any, are bound.
3. **Region-flavoured cosmetics and titles** — one banner or title per rift family, low weight, no
   economic footprint. Cheap to build on the existing purchase/achievement records.
4. **Riftshards** — the bounded currency (§8.4).
5. **Experience** — via `IExperienceRewardWriter.AddSplitExperienceAsync`, which already takes a
   collection of recipient ids.
6. **No Cinders.** Beyond incidental creature drops, zero. The economy doc's position is explicit and a
   daily repeatable group activity is exactly the wrong place to break it.

> **The reward roller supports more than the content currently uses.** `RewardRollType` has
> `All, Independent, Weighted, WeightedWithNoDrop, Sequence, Reference` and `RewardEntryType` has
> `Item, RewardTableReference, Cinders, Soulstones, Experience`, but the entire live
> `rewards/reward-tables.json` uses only `Item` entries under `Weighted` / `WeightedWithNoDrop` rolls.
> Rifts will be the first content to exercise `All` (for the guaranteed floor) and
> `RewardTableReference` (to share a materials table across rift families). Expect to find bugs there;
> `docs/reward-table-system-spec.md` already lists roller test coverage for quantity ranges, chances,
> scalar rewards and tag bonuses as missing. Write those tests as part of Phase 1.
>
> Also note **`Riftshards` cannot be paid through the roller as designed.** `RewardRollResult` carries
> only `Cinders`, `Soulstones` and `Experience`, and `ICurrencyRewardWriter.AddAsync(characterId, cinders, soulstones, ct)`
> writes only those two. `Character` already has five other currency columns (`FateEcho`,
> `SigilFragments`, `GuildFavor`, `TowerTokens`) each written by hand by its own feature. Rifts should
> break that pattern rather than extend it: add a `CurrencyType` enum and an
> `ICurrencyRewardWriter.AddAsync(characterId, IReadOnlyDictionary<CurrencyType, long>, ct)` overload,
> plus a `RewardEntryType.Currency` with a `currencyType` field. That is a contained refactor and it stops
> the sixth currency from adding a sixth bespoke write path.

### 8.4 Riftshards, and the sink question

Riftshards are earned per rift, scaled by outcome × contribution, roughly 8–20 per rift for an engaged
player. They are bounded by construction: entries are capped at 3/day, so the maximum weekly income is
knowable and cannot be farmed.

You said the Rift resource is for "something I'm not sure of yet". Rather than leave it open, here is a
recommendation and the reasoning, with the alternatives kept as live options.

**Recommended primary sink: Rift Focus.** A consumable, bought with Riftshards, that opens a rift _of
your choosing_ in a region you have unlocked, immediately, with a normal join window that other players
can step into. Costed so that roughly 3–4 rifts' worth of shards buys one Focus.

Why this is the right primary sink:

- **It converts currency back into content**, which is the only sink category that never inflates
  anything. Nothing enters the economy that was not already going to enter it via rifts.
- **It fixes the timezone problem without a schedule change.** A player whose region is quiet at 06:00
  local can make their own rift. That is precisely the frustration a subscription would otherwise be
  tempted to sell, and it should be purchasable with play instead.
- **It is pro-social.** A Focus-opened rift is a real rift: it announces to the world, other players can
  join, and the opener does not own it. One player's stored-up currency becomes everybody's content.
- **It self-limits.** A Focus costs more shards than the rift it opens returns, so it drains.

Secondary sinks, in order of confidence:

1. **Attunement slots** — a Riftshard-priced permanent upgrade that raises `MaxDailyAutoJoins` or adds a
   second preferred region. Careful: this must never raise the _entry_ cap of 3/day, only the automation
   breadth, or auto-join becomes volume.
2. **Catalyst exchange** — a weekly-limited Riftshard→evolution-catalyst trade, giving the currency a
   deterministic pity path into essence progression the way the Siege doc gives Warden Trophies one.
3. **Cosmetics and titles** — region-flavoured, unlimited stock, pure prestige.
4. **Anchor stabilisation** — spend shards before a rift seals to raise its tier (better tables, harder
   Anchor). Interesting, but it is a _pre-commitment made by one player that changes everybody else's
   fight_, which is a governance problem. Defer until Rifts have a live population.

What Riftshards should **not** buy: entries, power, gear directly, or anything a non-participant would
have to buy to keep up. There is no vendor of finished items.

### 8.5 Granting and claiming

Resolution writes one `RiftRewardGrant` per player participant, `Status = Unclaimed`, following
`TournamentRewardGrant` — but storing a rolled item manifest rather than hardcoded currency columns
(`TournamentRewardGrant`'s per-currency columns are the mistake to avoid; `DungeonRun.PendingRewards` +
`RunReward` is the better shape for the item list).

Claiming is a normal single-character command: `POST /api/v1/rifts/grants/claim` →
`ClaimRiftRewardsCommand(Guid CharacterId, Guid? RiftInstanceId)` (null = claim all), which runs under
the standard `TransactionBehavior` character lock and reuses `DungeonRunRewardClaimer`'s exact sequence:
`IExperienceRewardWriter.AddSplitExperienceAsync` → currency writer → `IItemBaseRepository.GetItemBasesByIdsAsync`
→ `IInventoryItemFactory.CreateForQuantity` → `IInventoryService.AddItemsToInventory(characterId, items, ItemAcquisitionSources.RiftReward, ct)`
(a new member on `ItemAcquisitionSources`, beside `DungeonReward` / `TournamentReward` / `EventQuestReward`)
→ `ILootHistoryService.RecordAsync` → broadcast `RiftRewardsClaimed` / `InventorySnapshot` /
`CharacterSnapshot` to `Audience.Character`.

Grants expire. `ClaimEndsAt = ResolvedAt + 14 days`, after which an unclaimed grant is swept by the
retention job. Say so in the UI; an unbounded pending bag is a support liability.

> `DungeonRunRewardClaimer` silently drops items whose `ItemId` is unknown or whose quantity is `≤ 0`.
> That is acceptable for dungeons because the tables are validated at startup. Rifts inherit the same
> validation (`RewardTableDefinitionValidator.ThrowIfInvalid` runs in the singleton provider's
> constructor, so a bad table fails startup, not a request) — but log the drop rather than swallowing it.

---

## 9. Auto-join, Attunement, and the subscription

### 9.1 What the subscription actually sells

**Attendance, not advantage.** A subscriber's rifts are the same rifts, with the same entry cap, the same
Anchors and the same tables. What they no longer have to do is be at the keyboard inside a 25-minute
window they were not told about in advance.

Stated as a rule for future feature work: _any Rift perk that changes the expected value of one entry is
out of scope for the subscription; any perk that changes how many of your three daily entries you
actually manage to use is in scope._

### 9.2 Attunement

`RiftAttunement`, one row per character, all fields free to set for all players:

```
CharacterId (PK)
AutoJoinEnabled          bool     — requires entitlement to take effect
PreferredRegionIds       int[]    — empty = any region the character can enter
StandingDirective        string?  — Surge key, used when absent (§7.3)
MinimumAnchorTier        int      — skip trivial rifts
MaxDailyAutoJoins        int      — 0..3, default 3
PausedUntil              DateTimeOffset?
RowVersion               uint
```

Free players can set a Directive and see their Attunement; only `AutoJoinEnabled` is gated. That way the
settings screen is not a paywall advertisement, and a lapsed subscriber's configuration survives.

> **There is nowhere to put this today.** There is no per-character or per-account settings entity and no
> generic key/value preference store anywhere in `Core/Domain/Models`; every existing "setting" in the
> game is Angular `localStorage` (`chat-layout-preference.service.ts`, `sidebar-layout-preference.service.ts`).
> `RiftAttunement` is therefore the first server-side player preference in the solution. Name it for the
> feature rather than making it a generic `CharacterSettings` table — a generic settings table invites
> every future feature to dump booleans into one row with one `RowVersion`, and that contention is
> unpleasant to unpick later.

### 9.3 How auto-join runs

Inside `RiftSpawnJob`, immediately after a `RiftInstance` is materialised and in the same transaction:

1. Select candidate characters: `AutoJoinEnabled`, entitled, region matches `PreferredRegionIds`, level
   inside the definition's band, `PausedUntil` elapsed, entries remaining today, auto-joins used today
   `< MaxDailyAutoJoins`, `MinimumAnchorTier` satisfied.
2. Order by a **fairness key**: fewest rift entries used in the last 7 days, then earliest
   `LastAutoJoinedAt`, then `CharacterId`. This is the mechanism that stops the same twenty subscribers
   from filling every rift in the game.
3. Take up to `AutoJoinFillCap` — deliberately **less than** `WarbandCap` (default 5 of 8) so manual
   joiners always have room. A rift that is full of bots before a human sees it is worse than no rift.
4. For each: capture a snapshot (subject to the reuse rule in §4.2), insert the participant row with
   `JoinKind = Auto`, decrement the entry, record the Directive as their Surge vote.
5. Notify: system chat line for the region plus `Audience.Characters(autoJoinedIds)` with a
   `RiftAutoJoined` payload, so a player who opens the app later sees what their Echo walked into.

Auto-join happens at open, not at seal, for two reasons: the snapshot is then contemporaneous with the
rift rather than 25 minutes stale, and the Warband is visibly populated during the window, which makes a
manual joiner more likely to step through.

> **The join-window budget belongs to humans.** If auto-join filled at seal time it could top a Warband up
> to the cap after nobody joined, which maximises reward throughput and minimises the chance any player
> ever meets another. Optimise for the encounter, not the throughput.

### 9.4 Entitlement, given none exists

There is no subscription, premium, entitlement or VIP concept anywhere in the solution. The single
monetisation-shaped type, `Domain/Models/Users/Transactions/Transaction.cs` (with `PaymentMethod` and
`Status` siblings), has no `DbSet`, no configuration, no migration and no consumer — it is orphaned
scaffolding, not a foundation.

So: define the seam now, implement it trivially, and do not design the billing system inside the Rift
feature.

```csharp
public interface IRiftAutoJoinEntitlementProvider
{
    Task<bool> IsEntitledAsync(Guid accountId, CancellationToken cancellationToken);
}
```

Phase 1 implementation: `ConfigurationRiftAutoJoinEntitlementProvider`, reading
`Rifts:AutoJoin:Mode` = `Disabled | Everyone | AllowList`, with an account allow-list for internal
testing. When a real subscription lands, one class changes. Everything else in the design — Attunement,
the fairness key, `JoinKind`, the fill cap — is entitlement-agnostic and can be exercised end to end
before any billing exists.

Two things to decide before launch rather than after (see §15): whether `Everyone` is actually the right
long-run answer for auto-join, and whether a lapsed subscriber's Attunement stays visible (recommended:
yes, greyed, with the toggle inert).

---

## 10. Authoring rift content

### 10.1 Where it lives

`src/API/API.LL/Data/world/rifts.json`, beside `regions.json` and `creatures.json`. Loaded by a
`JsonRiftDefinitionProvider` following `JsonEventQuestDefinitionProvider` / `JsonWorldTowerDefinitionProvider`
exactly: singleton, deserialise and validate **in the constructor**, throw on any breach so bad content
fails startup rather than a request.

Note `Worker.LL.csproj` links `..\..\API\API.LL\Data\**\*` into its own `Data\` with `PreserveNewest` and
sets `Content:Root = "Data"`, so the spawn and resolution jobs see the same content as the API with no
extra wiring. Also note that content loaders glob `*.json` recursively — `event-quests/the-defense-of-lumo.example.json`
is currently `"enabled": true` and loads in production. Do not leave example rifts in the folder.

### 10.2 Schema

```json
{
  "regionPulses": [
    {
      "regionId": 1,
      "riftsPerDay": { "min": 5, "max": 7 },
      "minimumSeparationMinutes": 45,
      "joinWindowMinutes": 25,
      "pool": [
        { "riftId": "rift.shenic.moonveil_breach", "weight": 70 },
        { "riftId": "rift.shenic.emberfall_tear", "weight": 30 }
      ]
    }
  ],
  "rifts": [
    {
      "id": "rift.shenic.moonveil_breach",
      "version": 1,
      "enabled": true,
      "name": "Moonveil Breach",
      "summary": "A breach spilling shades into the Moonlit Graves.",
      "regionId": 1,
      "anchorAreaId": "region_01_area_04",
      "tier": 1,
      "levelBand": { "min": 8, "max": 22 },

      "anchor": {
        "creatureId": "00000000-0000-0000-0000-0000000000aa",
        "name": "Veilmaw",
        "abilityProfileId": "monster.veilmaw",
        "anchorScaling": {
          "health": 14.0,
          "offense": 9.0,
          "defense": 4.0,
          "resistance": 4.0,
          "penetration": 3.0,
          "regeneration": 2.0
        },
        "tauntThreatBonus": 900,
        "anchorHealthPerParticipant": 640000
      },

      "adds": {
        "creatures": [
          { "creatureId": "…lost_soul", "weightedSpawnRate": 0.5 },
          { "creatureId": "…grave_wisp", "weightedSpawnRate": 0.3 },
          { "creatureId": "…grave_hound", "weightedSpawnRate": 0.2 }
        ],
        "spawnProbabilities": [0.0, 0.2, 0.5, 0.3],
        "respawnIntervalTicks": 400
      },

      "encounter": {
        "maxTicks": 1800,
        "overtimeStartsAtTick": 1200,
        "overtimePowerIncreaseIntervalTicks": 100,
        "overtimePowerIncreasePercent": 6,
        "holdThresholdPercent": 35,
        "captureReplay": true
      },

      "surges": [
        {
          "key": "onslaught",
          "name": "Onslaught",
          "friendly": [{ "attribute": "Power", "percent": 12 }]
        },
        {
          "key": "bulwark",
          "name": "Bulwark",
          "friendly": [
            { "attribute": "Armor", "percent": 18 },
            { "attribute": "Resistance", "percent": 18 }
          ]
        },
        {
          "key": "unravel",
          "name": "Unravel",
          "anchor": [
            { "attribute": "Armor", "percent": -15 },
            { "attribute": "Resistance", "percent": -15 }
          ]
        },
        {
          "key": "stillness",
          "name": "Stillness",
          "encounter": { "overtimeStartsAtTickDelta": 400 }
        },
        {
          "key": "greed",
          "name": "Greed",
          "rewardTagBonusPercent": { "rift_bonus": 40 }
        }
      ],
      "defaultSurgeKey": "onslaught",

      "rewardTables": {
        "completion": "reward.rift.moonveil_breach.completion",
        "firstSeal": "reward.rift.moonveil_breach.first_seal"
      },
      "riftshards": { "base": 8, "sealBonus": 6 }
    }
  ]
}
```

### 10.3 Validator

`RiftDefinitionValidator`, in the shape of `RewardTableDefinitionValidator` / the World Tower provider's
constructor checks. Minimum rules:

- Unique `(id, version)`; non-blank id, name, `regionId` exists in `regions.json`.
- `anchorAreaId` exists in `regions.json` and its `difficultyTier > 0`.
- `levelBand.min ≤ levelBand.max`; both `> 0`.
- All six `anchorScaling` values finite and `> 0` (they are multipliers, not additions).
- `anchorHealthPerParticipant > 0`; `maxTicks ∈ [600, 6000]`; `holdThresholdPercent ∈ [0, 100]`.
- Every add `creatureId` exists in `creatures.json`; `spawnProbabilities` sums to ~1 and its length
  bounds the add count.
- Every reward table id resolves in `IRewardTableDefinitionProvider` (this is the check
  `docs/reward-table-system-spec.md` lists as a missing follow-up — Rifts should add it, and should add
  it for _all_ content while they are in there).
- `surges` non-empty, unique keys, `defaultSurgeKey` present in the list.
- Every `regionPulses[].pool[].riftId` resolves and every pooled rift's `regionId` matches the pulse's.
- `riftsPerDay.max × 1` ≤ minutes in a day ÷ `minimumSeparationMinutes` — i.e. the schedule is actually
  satisfiable, or the pulse generator will spin.

### 10.4 Launch content

Ship **one separately authored rift**, such as `rift.shenic.moonveil_breach`. The Hive's Abyss is Raid
content and must not be duplicated into the Rift pool. The sample above is illustrative; its anchor,
reward tables, and final creature roster must be authored before Rift implementation begins.

One rift is enough to validate everything: pulse, join, Echoes, resolution, contribution bands, grants,
claims, and auto-join. A second rift in the same region is a JSON change once the first is tuned; a rift
in a second region is a JSON and content-authoring change.

---

## 12. Engineering shape

### 12.1 The concurrency design, stated once and clearly

This is where a multi-player feature in this codebase goes wrong, so it gets its own section.

`TransactionBehavior` acquires an in-process `CharacterCommandLockRegistry` lock plus
`pg_advisory_xact_lock` derived from `TryGetCharacterId(request)` — **one** character per command. A
command that mutates N characters holds one lock and is exposed to lost updates and cross-character
deadlock. Concretely, `IdleCombatRewardApplier.ApplySettlementAsync` **throws** if its batches span more
than one character, and `ICurrencyRewardWriter` / `ILootRewardWriter` are single-owner by construction.
Only `IExperienceRewardWriter.AddSplitExperienceAsync` is party-aware.

The design's answer is to never write across characters:

| Step         | Lock                                                                                       | Writes                                                 |
| ------------ | ------------------------------------------------------------------------------------------ | ------------------------------------------------------ |
| Spawn a rift | rift-schedule advisory lock (`ITournamentLockService.LockTournamentScheduleAsync` pattern) | `RiftInstance`, auto-join participant rows, snapshots  |
| Step through | character lock (free, via `TransactionBehavior`) **+** rift advisory lock                  | one participant row, one snapshot, one entry decrement |
| Seal         | rift lease + rift advisory lock                                                            | `RiftInstance.Status`                                  |
| Resolve      | rift lease + rift advisory lock                                                            | `RiftInstance`, `RiftRewardGrant` × N, playback bundle |
| Claim        | that character's own lock only                                                             | that character's XP, currency, inventory, loot history |

Resolution writes **no** row owned by a character other than through the grant table, which nothing else
touches. Claiming is an ordinary single-character command. There is no point at which two characters'
state is mutated under one lock.

Everything else in the correctness list is the same list the Siege doc derived, and it applies verbatim:

1. `uint RowVersion` on `RiftInstance` and `RiftAttunement`.
2. Unique `IdempotencyKey` on the resolution write, so a re-leased rift cannot double-grant.
3. Unique indexes `(RiftInstanceId, CharacterId)` and `(RiftInstanceId, AccountId)` on participants.
4. Unique index `(RegionId, RiftDefinitionId, OpensAt)` on instances, so a double-fired spawn job is a
   no-op rather than a duplicate rift.
5. Status transitions are idempotent and computed from the current row, never by incrementing.
6. Broadcast **after** commit.
7. All reward rolls under `IResolutionRandomSource.UseSeed`; never `Random.Shared`.
8. The engine checks cancellation every 64 ticks and **throws `OperationCanceledException`** rather than
   returning partial stats — a cancelled resolution must release the lease and leave the rift claimable,
   not half-resolved.
9. `SimulationAttempts` with a dead-letter threshold, so one broken definition cannot occupy a worker
   forever.

### 12.2 Data model sketch

New folder `src/Core/Domain/Models/Rifts/`:

- `RiftDefinition`, `RiftAnchorDefinition`, `RiftAddGroupDefinition`, `RiftEncounterDefinition`,
  `RiftSurgeDefinition`, `RiftRegionPulseDefinition` — content records from JSON, provided by
  `IRiftDefinitionProvider` / `JsonRiftDefinitionProvider`, validated by `RiftDefinitionValidator`.
- `RiftInstance` (aggregate) — `Id, RegionId, RiftDefinitionId, DefinitionVersion, DayKey, OpensAt,
SealsAt, Status, SealedAt?, ResolvedAt?, Outcome?, AnchorHealth, AnchorHealthRemaining, ResolvedSurgeKey,
EncounterSeed, FightDurationTicks?, SimulationLeaseOwner?, SimulationLeaseUntil?, SimulationAttempts,
OpenedByCharacterId? (Focus-opened rifts), IdempotencyKey, uint RowVersion`.
- `RiftStatus { Open, Sealing, Resolving, Resolved, Failed }`.
- `RiftParticipant` — `Id, RiftInstanceId, Kind, CharacterId?, AccountId?, CharacterName,
CharacterSnapshotId?, LoadoutHash, PowerRatingAtJoin, JoinKind, SurgeVoteKey?, JoinedAt,
AnchorDamage, AddDamage, Mitigated, ThreatShare, PerformanceRatio, ContributionBand`.
- `RiftParticipantKind { Player, Echo }`, `RiftJoinKind { Manual, Auto, Focus }`,
  `RiftContributionBand { Present, Attuned, Resonant, Ascendant }`, `RiftOutcome { Sealed, Held, Collapsed }`.
- `RiftRewardGrant` — `Id, RiftInstanceId, CharacterId, Band, OutcomeMultiplier, ContributionMultiplier,
Riftshards, Experience, ItemManifestJson, Status, ClaimEndsAt, CreatedAt, ClaimedAt?`.
- `RiftAttunement` — as §9.2.
- `RiftEntryLedger` — `CharacterId, DayKey, EntriesUsed, AutoJoinsUsed, LastAutoJoinedAt?`.
- `RiftCombatPlayback` (+ artifact) — only if replay is authored on; copy `TowerCombatPlayback` including
  its `RowVersion`, `CompactBundleSchemaVersion` and size caps.

Commands under `src/Core/Application/UseCases/Rifts/Commands/`: `StepThroughRiftCommand`,
`SetRiftAttunementCommand`, `ClaimRiftRewardsCommand`, `OpenRiftWithFocusCommand`,
`ResolveRiftCommand` (worker-invoked), `SpawnDueRiftsCommand` (worker-invoked).
Queries: `GetActiveRiftsQuery`, `GetRiftQuery`, `GetRiftHistoryQuery`, `GetRiftAttunementQuery`,
`GetRiftReadinessQuery`, `GetRiftGrantsQuery`.

Endpoints, following the convention the Siege doc proposes (`POST /api/v1/<feature>/<action>`):
`GET /api/v1/rifts` (active + next pulse hint), `GET /api/v1/rifts/{id}`,
`POST /api/v1/rifts/{id}/step-through`, `GET/PUT /api/v1/rifts/attunement`,
`POST /api/v1/rifts/grants/claim`, `GET /api/v1/rifts/grants`, `POST /api/v1/rifts/focus/open`,
`GET /api/v1/rifts/{id}/replay`, `GET /api/v1/rifts/history`.

Every new command must be registered in `StateSyncCommandScopeCatalog` or the architecture tests fail.

### 12.3 Extractions this feature requires first

Three, all of which the Siege doc also needs. Whichever feature lands first should do them.

1. **`ISnapshotCombatantBuilder`** — a public snapshot→combatant service. Today there are _three_
   private, subtly divergent copies: `WorldTowerService.RehydrateCharacter` (private static, returns a
   `Character`, **throws** on a missing `ItemBase`, preserves `BlueprintId`, does not handle essences),
   `ColosseumService.CreateSnapshotCombatEntityAsync` (private instance, returns a `CombatEntity`,
   **requires a live `Character` as template**, silently drops unknown equipment, loses `BlueprintId`),
   and `TournamentGroundsService.CreateSnapshotCombatEntity` (same as Colosseum but synchronous). Rifts
   would be the fourth. The unified builder must not require a live `Character` (auto-joined absent
   players are the whole point), must preserve `BlueprintId`, and must fail loudly on a missing
   `ItemBase` — silently dropping a weapon changes a fight's outcome.
2. **`CombatantFactory` branch for `RiftEncounterSourceContext`** plus a decision about the orchestration
   layer (§6.1). Do not add another `CombatMode` value with no orchestrator.
3. **`GameCalendar`** with `GetDayKey` / `GetWeekKey` (§3.3), replacing the two private `"yyyyMMdd"`
   implementations.

Two more that are Rift-specific but small: a `TauntThreatBonus` field on `CombatSimulationOptions`
(§5.2), and a `CurrencyType`-aware currency writer (§8.3).

### 12.4 Realtime and state sync

- **Do not add a region group.** `GameHub` has `WorldGroup = "world"`, `CharacterGroup`, `GuildGroup`,
  and `Audience` has `Character | Characters | Guild | World` — anything else throws in
  `GameRealtimeEnvelopeSender` and in `RealtimeDeliveryGameEventOutboxConsumer.ToAudience`. Every
  logged-in client already calls `subscribeToWorld()` on connect. Rift open/seal/resolve announcements go
  to `Audience.World`; participant-specific payloads go to `Audience.Characters(participantIds)`.
  Region filtering is a client concern.
- New events: `RiftOpened` (rewrite the dead `RiftOpenedMsg`, §2), `RiftParticipantsUpdated`,
  `RiftSealed`, `RiftResolved`, `RiftAutoJoined`, `RiftRewardsClaimed`. Add each to
  `GameRealtimeEventNames` **and** to the client's registry/`GameEventMap`, or the envelope arrives and
  is silently dropped.
- Add `StateSyncScopes.Rift = "rift"` **and** put it in the `WorldResources` grouping list next to
  `marketplace`, `guild`, `colosseum`, `tournament`. Adding the const alone makes
  `GetCheckpointAsync` skip the scope and the client can never reconcile it. If per-character rift state
  (grants, entries, attunement) is surfaced separately, add `rift-personal` to `CharacterResources` too.
- **The worker cannot talk to SignalR.** `Worker.LL` never calls `AddRealTime()`, registers
  `NoOpGameRealtimeImmediatePublisher`, and does not host `GameEventOutboxWorker`. A rift resolved in the
  worker must publish via `IGameRealtimeBroadcaster` → `OutboxGameRealtimeBroadcaster` →
  `GameEventTypes.RealtimeDeliveryRequested`, which API.LL's outbox worker (500 ms `PeriodicTimer`,
  batch 20) picks up and delivers. Budget ~1 s of delivery latency and note that **realtime delivery stops
  entirely if API.LL is down while the worker keeps resolving.** That is an accepted, documented
  consequence, not a bug — but the client must therefore reconcile rift state on load rather than relying
  on having received the event.
- New outbox event types must be added to `GameEventOutboxConsumerRegistry.ConsumersByEvent` or they
  produce **zero deliveries, silently**.
- World announcement: reuse the event-quest announcement pattern — `EventQuestService.CreateAnnouncementMessageId`
  mints a deterministic id (`new Guid(SHA256($"event-quest:{id}:{key}").AsSpan(0, 16))`) and
  `EventQuestChatGameEventOutboxConsumer` forwards it to the chat service. Do the same with
  `"rift-opened:{riftInstanceId}"` so retries dedupe chat-side. There is no player inbox, no mail and no push notification anywhere in the product, so
  chat plus state-sync-on-load is the whole notification surface. An offline player learns about a rift
  from their grant list, which is another reason grants must be legible on their own.

### 12.5 Frontend

Per `src/Presentation/ll/AGENTS.md` (Tailwind, standalone components, signals, `bg-texture` panels,
`app-default-header` / `app-regular-button` / `app-tabs`, API calls in services not components, npm only):

- `features/game/world/region/rifts/` — a `rifts.component` sibling of `region/dungeons/`, embedded in
  `region.component.html` the way `DungeonsComponent` already is. Note `region/raids/raids.component.ts`
  is currently a stub rendering an empty state; Rifts is the first of those tabs to become real.
- A dedicated rift page at `features/game/world/rift-page/`, registered in `WORLD_ROUTES` **above** the
  catch-all `{ path: ':id', component: RegionComponent }` — any literal path declared after it is
  swallowed as a region id.
- State in `core/services/api/rift/{rift.service.ts, rift-state.service.ts}`, following
  `dungeon-state.service.ts`: private writable signals, computed readonly exposure, injecting
  `GameEventService`, `StateSyncCoordinator`, `InventoryStateService`, `CharacterStateService`, `ToastService`.
- DTOs in `shared/models/Dtos/rifts/`.
- Sidebar: append to the `world` section in `core/services/client-side/sidebar/sidebar.service.ts`; wire a
  notification badge keyed on the item id for "a rift is open in a region you can enter" and for
  unclaimed grants.
- Attunement toggle in `features/game/settings/` — and note this will be the **first** setting on that
  screen backed by the server rather than `localStorage`.
- Replay reuses the tower/tournament playback component.

---

## 13. Phasing

**Phase 1 — MVP. Ships alone, is fun alone.**
One rift definition (`rift.shenic.moonveil_breach`). Deterministic pulse + `RiftSpawnJob` in the `world`
Quartz group. Manual step-through with snapshot capture. Echo fill to 3. Single-simulation resolution via
a leased `RiftResolutionJob`. Three outcomes, four contribution bands, per-participant grants and claim.
Riftshards accrue with **no sink yet** (state that plainly in the UI — "a use is coming" is honest;
a currency with a fake sink is not). Rifts tab in the region page, world chat announcement, `StateSyncScopes.Rift`.
Prerequisites: `ISnapshotCombatantBuilder`, the `CombatantFactory` rift branch, `GameCalendar`,
`CombatSimulationOptions.TauntThreatBonus`, and reward-roller tests for `All` / `RewardTableReference`.

**Phase 2 — Agency and the sink.**
Surges and Directives. Rift Focus as the primary Riftshard sink. `RiftAttunement` entity and settings UI
(Directive editable by everyone; auto-join toggle inert). Readiness band on the rift card. Replay.
`CurrencyType` refactor. A second rift definition in Shenic and the first in region 2.

**Phase 3 — Auto-join.**
`IRiftAutoJoinEntitlementProvider` + config implementation, auto-join inside `RiftSpawnJob`, the fairness
key, `AutoJoinFillCap`. **Gated on** `CharacterSnapshot.CapturedAt` + `Purpose`, the
`SnapshotRetentionJob`, and a real `ILoadoutHasher` with reuse (§4.2) — auto-join without snapshot
lifecycle is a slow-motion storage incident.

**Phase 4 — Depth.**
Rift tiers per region and a per-region pool of 3+ definitions. Rift
leaderboards / first-seal prestige. Catalyst exchange and cosmetic vendor. Anchor stabilisation if §8.4's
governance question resolves well.

**Deliberately deferred:** live in-fight input; cross-region rift chains or invasions; rift-exclusive
finished gear; anything that requires a matchmaker; anything that requires a push notification.

---

## 14. Risks

| Risk                                                     | Mitigation                                                                                                                                                                                                                                                                       |
| -------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Nobody is online when a rift opens; every rift is a solo | Echoes (§5.4) and the sublinear health curve (§6.3) make a solo rift a real fight. Rift Focus (§8.4) lets players make rifts when _they_ are online. The metric to watch is median real participants (§11.4).                                                                    |
| Auto-join crowds out real players                        | `AutoJoinFillCap` below `WarbandCap`, plus a fairness key ordered by least-recent participation. Auto-join fills at open, not at seal.                                                                                                                                           |
| Auto-join reads as pay-to-win                            | Same entry cap for everyone; no reward asymmetry; the only edge from presence is a fresh Surge choice (~6% EV), and free players can set a Directive.                                                                                                                            |
| Snapshot table grows without bound                       | Phase 3 is gated on `CapturedAt` + retention job + hash-based reuse (§4.2). This is the single biggest engineering debt the feature can incur.                                                                                                                                   |
| Contribution scoring punishes weaker players             | Bands are measured against the participant's own power, never against the Warband (§8.2), and the floor band pays 0.70 with no performance requirement.                                                                                                                          |
| A whale joins and trivialises the rift                   | Anchor health scales with participant count, so a strong Warband seals faster but pays each member the same per-capita bands. Nobody's reward drops because someone strong joined.                                                                                               |
| Riftshards inflate with no sink                          | Phase 1 ships them with an explicit "sink coming" note and a low accrual rate; Phase 2 ships Rift Focus, which drains more than it grants. Do not raise accrual until a sink exists.                                                                                             |
| The 25-minute window is wrong for the population         | It is one number in `regionPulses`. Read the join-rate telemetry before changing anything else.                                                                                                                                                                                  |
| Realtime silently fails (worker resolves, API is down)   | Documented in §12.4; the client reconciles rift and grant state on load and never depends on having received an event.                                                                                                                                                           |
| The `world` chat channel becomes rift spam               | 5–7 rifts per region per day across N regions adds up fast. Announce only rifts the reader can actually enter (level band + region unlocked) — that means the announcement is a targeted `Audience.Characters` message, not a global chat line, once more than two regions ship. |

---

## 15. Open questions

1. **Is auto-join really a subscription perk, or should it be free?** The design works either way and
   §9.4's entitlement seam makes it a one-line switch. The argument for free: it removes the timezone tax
   for everyone and makes every rift better populated, which is the feature's main risk. The argument for
   paid: it is a genuinely non-power convenience, which is the healthiest thing a subscription can sell.
   A middle option worth considering: auto-join free for **one** region, additional regions gated.
2. **What is the Riftshard sink at launch?** §8.4 recommends Rift Focus and explains why. Confirm, or name
   a different one, before Phase 1 ships shards — accruing a currency with no announced purpose for weeks
   is a trust cost.
3. **Should a rift's Warband see each other before sealing?** Showing names and power makes the join
   window social and makes "step through, someone else already did" a real hook. It also enables
   spectating your own Warband fill up. The cost is a realtime payload per join. Recommendation: yes,
   names and power only, no gear inspection.
4. **Do Rifts respect area unlock requirements, or only level bands?** The `anchorAreaId` is a real area
   with `requiredCompletedQuestId` / `requiredTowerFloor` gates. Gating rifts on those makes the world
   consistent; not gating them makes rifts a way to see content early. Recommendation: level band only for
   participation, but the _region_ must be unlocked — a rift is a tear into the region, not a visit to
   the area.
5. **What happens to an entry if resolution fails permanently?** Recommendation: dead-lettered rifts
   refund entries to all participants and grant nothing, with a chat notice. Needs a `RiftStatus.Failed`
   path and a refund command, which Phase 1 should include rather than discover.
6. **Should Echoes exist forever, or only below a population threshold?** They are a floor mechanic. If
   the game grows, a rift that fills with real players never sees one — but the code path stays. Leaving
   it permanently is simpler and self-deactivating. Confirm that is acceptable rather than an
   embarrassment.
7. **Does a Focus-opened rift count against the opener's daily entries?** Recommendation: yes, it consumes
   one entry as well as the Focus, so Focus buys _timing_, not volume. Otherwise Riftshards become an
   entry-cap bypass, which §9.1's rule forbids for subscriptions and should equally forbid for currency.
8. **Does the Anchor need in-fight phases?** The ability system supports the Garran idiom
   (`OnHealthChanged` + `HealthAtOrBelowPercent` + a stack-guard status), and unlike a Siege slice, a rift
   Anchor's health _is_ meaningfully depleted in one fight — so health-threshold phases work here and are
   the cheapest way to make a rift feel unlike a dungeon boss. There is still no declarative phase
   container in the ability system, so this is authored per Anchor. Recommendation: one phase at 50% on
   the launch Anchor, more once the format is proven.

---

## Appendix A — What the codebase already has vs what must be built

**Already exists and should be reused as-is:**
`FastCombatEngine` (N-vs-M, threat-weighted targeting, no party-size limit) ·
`FastCombatEngineOptions.Overtime*` and `TauntThreatBonus` ·
`CombatEncounterPlan` / `CombatParticipantSlot` / `CombatEncounterRuntime` / `CombatRuntimeParticipant` ·
`ICombatEngineExecutor.ExecuteSimulationAsync` / `ExecuteCompactPlaybackAsync` ·
`CombatSetupService.CreatePlayerCombatEntities` / `CreateCreatureCombatEntities` / `PrepareEntitiesForCombat` ·
`ICreatureScaler.ApplyScaling` · `WorldTowerGuardianScaling.Apply` ·
`CharacterSnapshot` + `ICharacterSnapshotService.CreateAsync` ·
`WeightedSpawnSelector` · `Common.Randomness.StableRandom` · `IResolutionRandomSource.UseSeed` ·
Quartz infrastructure + `BackgroundJobGroups.World` (empty) + `IBackgroundJobExecutionService.RunOnceAsync` +
`BackgroundJobExecution` unique `(JobName, BusinessKey)` ·
lease columns and claim pattern from `TowerAttempt` / `IWorldTowerWorkLeaseService` ·
`PostgresTournamentLockService` advisory-lock-before-the-row-exists pattern ·
`IRewardRoller` + `rewards/reward-tables.json` + `RewardTableDefinitionValidator` +
`RewardRollContext.EntryWeightBonusPercentByTag` ·
`IExperienceRewardWriter.AddSplitExperienceAsync` (already multi-recipient) ·
`DungeonRunRewardClaimer` claim sequence · `TournamentRewardGrant` grant/claim shape ·
`GameHub.WorldGroup` + `Audience.World` / `Audience.Characters` ·
`OutboxGameRealtimeBroadcaster` → `RealtimeDeliveryGameEventOutboxConsumer` worker→client path ·
`EventQuestChatGameEventOutboxConsumer` deterministic-`MessageId` announcement pattern ·
`JsonWorldTowerDefinitionProvider` / `JsonEventQuestDefinitionProvider` validate-in-constructor pattern ·
a dedicated Rift creature set rather than reusing Raid content.

**Must be built:**
All rift domain models, EF configurations, `DbSet`s and migrations (`Core/Domain/Models/Rifts` does not
exist) · `world/rifts.json` content + `IRiftDefinitionProvider` + `RiftDefinitionValidator` ·
deterministic pulse generator + `RiftSpawnJob` + `RiftResolutionJob` (the `world` job group has zero jobs
today) · `CombatMode.Rift` + `RiftEncounterSourceContext` + the `CombatantFactory` branch (+ orchestrator
and outcome processor, or an explicit decision not to use the orchestration layer) ·
a **public** `ISnapshotCombatantBuilder` extracted from three private copies ·
`GameCalendar.GetDayKey` / `GetWeekKey` (four incompatible conventions exist today) ·
`TauntThreatBonus` on `CombatSimulationOptions` (currently unreachable through the executor) ·
`CurrencyType` + a multi-currency `ICurrencyRewardWriter` overload + `RewardEntryType.Currency`
(`RewardRollResult` carries only Cinders/Soulstones/Experience) ·
`Character.Riftshards` · `RiftAttunement` — the **first server-side player preference in the solution** ·
`IRiftAutoJoinEntitlementProvider` (no subscription/entitlement concept exists at all) ·
`CharacterSnapshot.CapturedAt` + `Purpose` + `SnapshotRetentionJob` + a real `ILoadoutHasher` ·
Echo support for partially filled rosters · a rewrite of `RiftOpenedMsg` and its
`DomainToClientMapper` + client map entries · `StateSyncScopes.Rift` in the const list **and** in
`WorldResources` · new outbox event types registered in `GameEventOutboxConsumerRegistry` ·
Rifts tab, rift page, `rift-state.service.ts`, DTO folder, sidebar entry, server-backed settings toggle ·
reward-roller tests for `All` / `RewardTableReference` / quantity ranges / tag bonuses · a content test
asserting every referenced `RewardTableId` resolves.

**Do not build on:**
`SpawningService`'s instance `new Random()` (not deterministic — use `WeightedSpawnSelector` with a
seeded `Random`) · `ColosseumRepository.GetArenaOpponentsWithRating` (loads every character in the DB via
a tautological `Where`) · `ICharacterSnapshotService.GetSnapshotByCharacterIdAsync` (unordered
`FirstOrDefault` over an unbounded, non-unique table) · `SnapshotLockMode` (dead enum, zero references) ·
`Domain/Models/Users/Transactions/Transaction` + `PaymentMethod` + `Status` (orphaned, no `DbSet`, no
migration, no consumer) · `BossProfiles.RaidBoss` / `BossRank` / `CreatureTemplate.IsBoss` (authoring-only,
no JSON producer, no live consumer — flagged in the Siege doc) · `ArenaDefenseSnapshot.IsOutdated` /
`LoadoutHash` as a staleness signal (write-only today, and the hash omits Quality, Tier,
StatModelVersion, Level, BlueprintId and essence level) · the `RaidCombatOrchestrationRequest` /
`CombatMode.Raid` shape as a template for "add a mode" (it is a mode with no orchestrator, which is
exactly the trap to avoid).

**Known landmines flagged elsewhere in `docs/`:** the docs' power-rating version constants are stale
(23/11/13 vs the actual 25/13/16); `ColosseumPvPImplementationStatus.md` claims Tournament Grounds is
unimplemented when a 3765-line service, Quartz job and full bracket machinery exist; `docs/loot-table-system-analysis.md`
warns against EF `LootTable` entities that have already been deleted and cites the old
`Data/reward-tables.json` path; `BackgroundJobNames.WeeklyColosseumSettlement`,
`DailyGameMaintenance` and `GuildWarPhaseRollover` are declared constants with no job class and no
trigger; `TournamentMatch.PlayerOne/TwoParticipantId` actually hold **team** ids;
`Character.Guild` is not the guild a character belongs to (EF pairs it with `Guild.Owner` — read
`GuildMembers`). A rift feature should not inherit any of these, and this document's claims about the
codebase were verified against the code rather than against the docs.
