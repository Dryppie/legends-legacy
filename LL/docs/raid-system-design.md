# Raid System — Game Design Document

**Status:** Implemented (Phases 1–3)
**Author:** Design pass, 2026-08-18
**Feature name (player-facing):** **Raids**
**Surface:** World Map, in the Raids rail alongside Dungeons
**Guild involvement:** none

**Implementation note:** Phases 1–3 are implemented. Simulation-backed power calibration is shipped behind `RaidPowerCalibration:Enabled` and uses authored values as a safe fallback until calibrated rows exist. Launch content is **The Hive's Abyss** (tiers 1–3) and **Sanguine Horror** (tiers 2–3). The Ant King is an 8% Vanguard boss replacement; Bloodthorn Vine is a 22% rare Ward guard.

---

## 0. Executive summary

A **Raid** is a public, player-created assault on a **raid boss** — a regional raid boss that lives
permanently on the World Map next to the region's dungeons. Any player can create a raid at a
raid boss site. Any player can sign up. When signups are in, the **raid leader** sorts them into
**three Wings** — _Vanguard_, _Flank_ and _Ward_ — and starts the raid. The server resolves the
three wings as three separate simulated battles, in a fixed order, where the Flank and Ward results
change the conditions the Vanguard fights under. Vanguard kills the raid boss, or it doesn't.

The strategy is not in twitch execution or role labels. It is entirely in **allocation**: the leader
has a finite roster of known power ratings and must decide who fights where. Stack everyone strong
into the Vanguard and it meets a shielded, reinforced raid boss and dies. Over-invest in the support
wings and the Vanguard lacks the damage to finish. There is a right answer for every roster and it
changes with every roster.

Nothing needs to happen in real time. Every participant is a frozen `CharacterSnapshot`, exactly as
World Tower Expeditions already work, and resolution is done by a worker with results displayed
afterwards. Fifteen people never need to be online at once — they need to have signed up. Per-wing
frame playback is stored per wing and available after resolution.

### Why this shape

| Constraint                                                   | How the design answers it                                                                                                                                                             |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Idle game, players not co-present                            | Snapshot signups + worker resolution + playback. Signup window can be hours or days.                                                                                                  |
| World Tower already owns "public 5-player lobby vs one boss" | Raids are one leader allocating **three** wings whose outcomes feed each other. Tower is vertical, single-party, floor-by-floor; raids are horizontal, multi-wing, allocation-driven. |
| No guild gating                                              | Raids are listed publicly per region. Create, sign up, done. No guild reference anywhere in the domain.                                                                               |
| No role system wanted                                        | Strategy lives in _who goes where_, not in role labels. Build identity still matters, emergently, through which wing a character suits.                                               |
| Must sit where dungeons sit                                  | The World Map's Raids rail panel already exists and is empty (§1).                                                                                                                    |

### What already exists to build on

| Need                                     | Existing thing it reuses                                                                          |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------- |
| Raids panel on the World Map             | `region.component.html` `<section class="activity-panel raids">`, now populated from the raid API |
| Raid-site component                      | `features/game/world/region/raids/raids.component.ts` — open raids, free creation, and joining    |
| Region DTO                               | `shared/models/Dtos/regionDto.ts` → lightweight raid-boss map metadata                            |
| Commented-out domain hook                | `Core/Domain/Models/Regions/Region.cs` → `// ICollection<Raid> Raids`                             |
| Public lobby + signup + approval         | `TowerRally` / `TowerRallyApplication` / `TowerRallyParticipant`                                  |
| Frozen participants                      | `CharacterSnapshot`, `ICharacterSnapshotService.CreateAsync`                                      |
| N-vs-M tick combat                       | `FastCombatEngine.Run(friendly, hostile, …)`                                                      |
| Boss authoring & scaling                 | `tower-floors.json` schema + `WorldTowerGuardianScaling.Apply`                                    |
| Worker resolution + replay               | `TowerAttempt` leases, `TowerCombatPlayback` / `TowerCombatPlaybackArtifact`                      |
| Entry cost pattern                       | `DungeonDefinition.EntryCosts` + sigil assembly                                                   |
| Broadcast to everyone                    | `GameHub` + `Audience.World()` + outbox consumer                                                  |
| Rewards                                  | `rewards/reward-tables.json` + `IRewardRoller`                                                    |
| Once-per-week-per-character reward locks | `TowerEchoClear`                                                                                  |

`CombatMode.Raid` (=3), `RaidEncounterSourceContext(Guid RaidRunId, int PhaseIndex, string StageKey)`
and `RaidCombatOrchestrationRequest(Guid RaidRunId, Guid RaidPartyId, DateTimeOffset Now)` already
exist in `Services.LL.Combat.Layers.Orchestration.Models`. They are the only two `Raid`-prefixed
types in the solution. Note the existing context already anticipates a _party_ id and a _phase/stage_
— this design maps `RaidPartyId` → Wing and `StageKey` → lane key.

---

## 1. Where it lives — the World Map

The World Map is `features/game/world/region/region.component.*`, routed as `world/:id`
(default `shenic`). It is a card grid of areas plus a right-hand rail
(`<aside class="world-map-rail">`) with two `activity-panel` sections: **Dungeons** and **Raids**.

**The Raids panel uses the existing map markup** (`data-tour="raids-introduction"`). Phase 1 loads
live raid-boss summaries separately from the still-hardcoded region data, then renders the implemented
`region/raids/raids.component.ts` beside the working dungeon component.

So raids do not need a new surface. They need that panel populated and made clickable, mirroring
`selectDungeon()` → inline preview → enter.

**Presentation parity with dungeons.** A raid boss row in the rail should read like a dungeon row:
name, required level, whether you can act, and a state chip. Where a dungeon shows
`ownedSigilCount` / `canEnter`, a raid boss shows **live raid state** — the thing that makes it feel
alive on the map:

```
The Hive's Abyss       Lv. 25    ● 2 raids recruiting
Hollow Tyrant          Lv. 40    ○ locked — requires Lv. 40
Duskmaw                Lv. 55    ⏱ on cooldown for you — 6h
```

Clicking expands an inline panel (the raid equivalent of `dungeon-card`) listing that raid boss's
**open raids**: leader name, signup count per wing, tier, closes-in timer, and a Join button —
plus a "Create raid" button if the player holds a key and isn't already committed.

> **Recommendation:** move region data server-side while doing this. `region.service.ts` is
> hardcoded and `RegionController.GetRegion` already exists but is not called by the map. Raid state
> is inherently live (recruiting counts, cooldowns) and cannot be hardcoded, so the map will need a
> real endpoint regardless. Do that properly rather than bolting a second data path onto a hardcoded
> component.

### Region anchoring and gating

Each region gets 1–2 Raid bosses, gated like areas are: `levelRequirement`, optional
`requiredCompletedQuestId`, optional `requiredTowerFloor`. Region membership follows the existing
convention — dungeons already carry `"region": 1` in `dungeons.json`, so Raid bosses carry the same.
Shenic gets an entry-level raid boss; Meran a harder one. They are **always available** — no rotation,
no windows. A player who wants a specific drop can keep raiding that specific raid boss.

Locked Raid bosses should be **visible but locked** (like areas without `hideWhenLocked`), because a
visible locked raid is aspirational and teaches that the content exists.

---

## 2. Vocabulary

| Term          | Meaning                                                             | Code shape                        |
| ------------- | ------------------------------------------------------------------- | --------------------------------- |
| **raid boss** | A regional raid boss. Content, not state. Permanent map fixture.    | `RaidBossDefinition` (JSON)       |
| **Raid**      | One player-created instance: a roster, three wings, one resolution. | `RaidRun` aggregate (`RaidRunId`) |
| **Leader**    | The player who created the raid. Sorts the roster, starts it.       | `RaidRun.LeaderCharacterId`       |
| **Signup**    | A request to join, freezing a snapshot.                             | `RaidSignup`                      |
| **Wing**      | One of three sub-forces. 1:1 with a lane.                           | `RaidWing` (`RaidPartyId`)        |
| **Lane**      | Vanguard / Flank / Ward — what a wing attacks.                      | `RaidLane` enum, → `StageKey`     |
| **Muster**    | The recruiting phase.                                               | `RaidRunStatus.Mustering`         |
| **Trophy**    | Bounded raid currency.                                              | New currency                      |

> **Naming check:** do not call the boss a "Warden" — `TowerFloorType.Warden` already exists and the
> collision will be confusing in code and in the UI. "raid boss" is used throughout this document;
> grep before committing to it.

---

## 3. The raid lifecycle

```
   ┌─ CREATE ────────────────────────────────────────────────────────────┐
   │  Player creates a free raid at a raid boss site on the World Map.    │
   │  Picks tier. Raid appears publicly in that raid boss's raid list.    │
   │  Creator becomes Leader. Signup window opens (default 24h).        │
   └──────────────────────────────┬──────────────────────────────────────┘
                                  ▼
   ┌─ MUSTER ───────────────────────────────────────────────────────────┐
   │  Anyone eligible signs up → CharacterSnapshot + PowerRating frozen │
   │  Leader sees the roster with power ratings and suggested fits       │
   │  New signups remain benched until the Leader assigns them           │
   │  Leader clicks or drags them into exact Vanguard / Flank / Ward slots│
   │  Leader may run free Battle Plan previews (§6), rate-limited 30/hour│
   └──────────────────────────────┬──────────────────────────────────────┘
                                  ▼
   ┌─ RESOLVE (leader presses Commence; worker-driven) ─────────────────┐
   │  1. Flank battle   → ReinforcementPenalty                          │
   │  2. Ward battle    → WardBreak                                      │
   │  3. Vanguard battle, with 1 & 2 applied → boss lives or dies        │
   │  Playback artifacts written for all three wings                     │
   └──────────────────────────────┬──────────────────────────────────────┘
                                  ▼
   ┌─ SETTLE ───────────────────────────────────────────────────────────┐
   │  Outcome graded. Contribution computed per participant.            │
   │  Rewards written to pending bag; each participant claims.          │
   │  World broadcast + world chat line on a kill. Cooldowns stamped.   │
   └────────────────────────────────────────────────────────────────────┘
```

**Auto-expiry.** If the leader never presses Commence, the raid auto-resolves at the end of the
signup window with whatever assignment exists, or auto-cancels if fewer than
the minimum roster signed up. A leader going offline must not strand a dozen people's snapshots.
This is the single most important reliability rule in the design.

---

## 4. The three lanes — where the strategy is

This is the core mechanic. Three wings fight three separate battles, and **the resolution order
creates a dependency chain**:

### 4.1 Flank — resolved first

**Fights:** the raid boss's escort — an add group (3–6 creatures, authored per tier).

**Produces:** `ReinforcementPenalty`, a scalar in `[0, 1]`.

```
addsRemainingFraction = survivingAddHealth / totalAddHealth
ReinforcementPenalty  = addsRemainingFraction
```

Every add left alive reinforces the boss. Applied to the Vanguard battle as:

- the surviving adds **join the Vanguard fight** as extra hostiles, and
- the raid boss gains `+ (ReinforcementPenalty × MaxReinforceOffense)` offense (authored, e.g. 40%).

Clearing the Flank completely means the Vanguard fights the raid boss alone. Ignoring the Flank means
the Vanguard fights the raid boss _and_ its whole escort, with the raid boss hitting 40% harder.

### 4.2 Ward — resolved second

**Fights:** the raid boss's protective ward — a high-mitigation, high-barrier, low-damage objective
creature, plus a small guard.

**Produces:** `WardBreak`, a scalar in `[0, 1]`.

```
WardBreak = min(1, damageDealtToWard / wardHealth)
```

Applied to the Vanguard battle as a reduction of the raid boss's defences:

- `Armor`, `Resistance` and `DamageReduction` reduced by `WardBreak × MaxWardBreakPercent`
  (authored, e.g. 50%).

The Ward is deliberately a _damage-soak check_, not a kill check — partial credit is smooth, so a
weak Ward wing still contributes something and a strong one caps out. That smoothness is what makes
allocation a tuning problem rather than a binary.

### 4.3 Vanguard — resolved last

**Fights:** the raid boss itself, with `ReinforcementPenalty` and `WardBreak` applied, plus any
surviving Flank adds.

**Produces:** the raid outcome.

```
Slain      — raid boss reaches 0 HP within the tick budget
Broken     — raid boss ends below 25% HP
Wounded    — raid boss ends below 60% HP
Repelled   — raid boss ends above 60% HP, or the Vanguard wipes
```

The raid boss has real, authored HP and can genuinely die inside one battle. `BattleOutcome.Victory`
means what it says. There is no shared pool and no artificial HP slice.

### 4.4 Why this is a real decision

The leader's roster is finite. Three examples with the same 12 players:

- **All strength into Vanguard.** Flank and Ward wings are weak → adds survive, ward holds → the
  Vanguard meets a 40%-stronger raid boss at full mitigation, alongside six adds. Repelled.
- **Even split.** Flank clears, Ward breaks ~60% → Vanguard meets a solo raid boss at ~70% mitigation.
  Usually Wounded or Broken.
- **Correct read.** Enough in Flank to fully clear (it's a low-HP-many-targets fight — multi-target
  builds shine), the minimum in Ward that still caps `WardBreak` (it's a sustained-damage-into-a-wall
  fight — single-target attrition builds shine), everything else Vanguard. Slain.

Build identity matters _emergently_ — a multi-target build is genuinely better in Flank, a
high-sustained-DPS build is genuinely better in Ward — without any role labels, eligibility checks,
or assignment validation. That is the whole point of the "no roles" decision: the lanes do the work
that roles would have done, and they do it through the combat sim rather than through metadata.

---

## 5. Roster sizing and eligibility

Roster size is authored per tier so raids scale with the live population:

| Tier | Slots per wing | Max roster | Minimum to commence |
| ---- | -------------- | ---------- | ------------------- |
| I    | 3              | 9          | 3                   |
| II   | 4              | 12         | 6                   |
| III  | 5              | 15         | 9                   |

The minimum is deliberately well below the maximum. A raid that can only fire at full capacity will
never fire on a small server. Under-strength raids should be _possible and hard_, not blocked.

**Eligibility to sign up**, following the Tower's `GetJoinEligibilityAsync` precedent almost exactly:

1. Character meets the raid boss's `levelRequirement` and any quest/area gate.
2. Character is not already committed to another raid in `Mustering` or `Resolving` status —
   one active raid per character.
3. One slot per **account** per raid (`AccountId == character.UserId`), so alts cannot stack a roster.
4. `PowerRating` must be in `PowerAnalysisState.Available` — the leader needs a number to allocate on.
5. Character is not on personal cooldown for this raid boss (§8.3).

**Snapshot freeze.** Signing up calls `ICharacterSnapshotService.CreateAsync` and stores
`CharacterSnapshotId` + `PowerRating` + `LoadoutHash` on the signup, mirroring
`TowerRallyParticipant`. A participant may re-snapshot while the raid is still mustering
(`POST raids/{id}/loadout`, as the Tower does) — so upgrading your gear during the window pays off,
and the leader sees a "loadout updated" marker so their plan isn't silently invalidated.

**Nobody is present at resolution.** Every combatant, including the leader, fights as a snapshot.
This is the load-bearing simplification: it makes signup windows arbitrarily long, removes all
scheduling, and means a raid resolves correctly at 04:00 with everyone asleep.

### Party layout and benching

The three existing wings are the Raid's parties. A wing has the tier-authored number of exact slots
(three, four, or five), so no Raid party can exceed five players. Every newly created signup,
including development-roster participants, starts with both `Lane` and `WingSlotIndex` unset and is
shown on the **Bench**. Only the Raid Leader can change the layout while the Raid is mustering and the
signup window remains open.

The Leader can select a benched participant and then click a destination slot, drag the participant
onto a slot, return an assigned participant to the Bench, distribute all benched participants, or
auto-balance/reset the full layout. Each interaction submits the complete roster layout atomically;
the server rejects missing participants, duplicate participants, partial lane/slot pairs, out-of-range
slots, and duplicate occupied positions. A Raid cannot commence or generate a Battle Plan while any
participant is benched, and all three parties must contain at least one player.

Each wing resolves as its own combat encounter. Friendly and self-or-ally targeting therefore stays
inside that Raid party. Hostile `TargetAllEnemies` effects continue to hit every friendly participant
in the encounter; Raid combatants also carry their wing-derived party number for consistency with the
shared combat targeting rules.

---

## 6. The Battle Plan preview — the leader's tool

Allocation is only strategic if the leader can reason about it. Give them a **free, stateless
simulation** of the current assignment, protected by a 30-per-hour leader rate limit:

- Runs all three lane battles at reduced sample count (8–12 seeds).
- Reports per lane: expected outcome, a Wilson-interval confidence band, and the derived
  `ReinforcementPenalty` / `WardBreak`.
- Reports overall: predicted raid outcome band (Repelled → Slain) with confidence.

This is `DungeonReadinessService` pointed at a raid: simulate the real snapshots in batches, report
a band, **persist nothing, grant nothing, spend nothing**. That service already establishes the
exact vocabulary (Very Unlikely / Risky / Uncertain / Favored / Comfortable) and the Wilson-interval
approach, so this is a re-point rather than new invention.

Rate-limit it (e.g. 30/hour per leader) purely to protect the CPU, not as a game mechanic. The
preview should feel free, because a leader who is afraid to experiment will just guess.

> This is the feature that turns raids from a lottery into a puzzle. If scope must be cut, cut a
> raid boss, not this.

---

## 7. Resolution mechanics

### 7.1 Pipeline

```
seed_base = stableHash(RaidRunId)

1. Flank:    engine.Run(flankWing,    addGroup)     seed = seed_base ^ 0x11
             → ReinforcementPenalty, survivingAdds
2. Ward:     engine.Run(wardWing,     wardObjective) seed = seed_base ^ 0x22
             → WardBreak
3. Vanguard: engine.Run(vanguardWing, [boss(mods)] + survivingAdds)
             seed = seed_base ^ 0x33
             → outcome, per-entity damage
```

Deterministic from `RaidRunId` alone, so the whole raid is exactly reproducible for support and
replay, and the Battle Plan preview can use different seeds to avoid leaking the real roll.

Compose each battle as a `CombatEncounterPlan` with `Mode = CombatMode.Raid` and
`SourceContext = new RaidEncounterSourceContext(RaidRunId, PhaseIndex: laneOrdinal, StageKey: "flank"|"ward"|"vanguard")`
— which is exactly the shape the existing record already has.

### 7.2 Worker-driven, leased, replayable

Copy the `TowerAttempt` pattern rather than resolving inline in the request:

- `RaidRun` carries `SimulationLeaseOwner`, `SimulationLeaseUntil`, `SimulationAttempts`.
- A worker picks up `Resolving` raids, takes the lease, runs the three battles, writes results.
- Write playback artifacts per wing (`TowerCombatPlayback` / `TowerCombatPlaybackArtifact` pattern:
  Brotli-compressed compact bundle, ETag'd, `TicksPerFrame 10`) so participants can watch their own wing.
- Take an advisory lock per raid (`AcquireWorldTowerFloorLockAsync` is the precedent for a
  scoped advisory lock).
- Broadcast progress via the outbox → `Audience.World()`, and post a world-chat line on a kill with
  a deterministic message id for idempotency (the `WorldTowerChatGameEventOutboxConsumer` pattern).

Resolving three 6000-tick battles is heavier than one. Pass `CaptureEventLog: false` for the
Battle Plan preview (which switches to the cheaper `CreateBalanceStats` populator) and reserve full
capture for the real resolution where playback is wanted.

### 7.3 Measurement caveats that must not be got wrong

Contribution and lane outputs both read `EntityStats.DamageDone`. Four verified gotchas:

- **`DamageDone` accumulates `CombatLogItem.Magnitude`, which is post-mitigation, post-barrier
  `healthBefore - target.Health`.** Barrier-absorbed damage is excluded (it lands in
  `BarrierAbsorbed` / `DamageBlocked`) and overkill is clipped. For the Ward lane — a
  deliberately barrier-heavy objective — this matters a lot: measure `WardBreak` against **health
  removed plus barrier absorbed**, or the Ward wing will read as contributing far less than it did.
- **Summon damage is attributed to the summon's own entity ID**, not the owner
  (`CombatStatsAggregator.GetOrAddEntity(actorId)`). Sum each participant plus every `IsSummoned`
  entity they own, or summon builds read as near-zero contribution.
- **`BattleOutcome` is only `{ Victory, Defeat, Draw }`**, and `DetermineOutcome` returns `Draw`
  both for "tick limit reached" and "mutual wipe on the same tick". Detect tick-limit termination on
  `Duration >= maxTicks`, never on `Outcome`. `CombatResult.Duration` is in **ticks** — divide by
  `TicksPerSecond` (10) before displaying.
- **Tick budget must be explicit.** `FastCombatEngineOptions.MaxTicks` defaults to 6000 and the
  executor's `ExecuteAsync` / `ExecuteWithCheckpointsAsync` hardcode 6000, but
  `CombatSimulationOptions` — the record callers actually construct for `ExecuteSimulationAsync` —
  defaults to **1800**. State the raid tick budget per lane in the raid boss definition; the entire
  difficulty model is calibrated against it.

### 7.4 A note on threat, since there are no roles

Targeting is threat-weighted roulette (`GetEffectiveThreat`, `BaseThreat = 100f`), and
`TauntThreatBonus` defaults to only `100f` — so a taunting character draws just ~33% of attacks
against four allies. **This design does not depend on that**, because it has no tank role. It is
still worth knowing: a Vanguard wing of five glass cannons will lose members unpredictably, and
that emergent lesson ("bring somebody who can absorb hits") is a _desirable_ teaching moment rather
than a mechanic to specify. If playtesting shows Vanguard outcomes are too random, raising
`TauntThreatBonus` per raid boss is a free dial — it is a constructor option, no engine change.

---

## 8. Costs, cooldowns and anti-abuse

### 8.1 Free raid creation

Creating and joining a raid are free. The system already limits a character to one active raid and
one led raid, while the server caps the number of open musters per boss. These direct constraints
control lobby spam without introducing an entry currency or punishing the player who volunteers to lead.

### 8.2 Participation locks

- **One active raid per character** (`Mustering` or `Resolving`) — the Tower's
  `ActiveRallyStatuses` check, verbatim in spirit.
- **One slot per account per raid** — blocks alt-stacking a roster.
- **One raid led at a time per character** — blocks a player opening a dozen simultaneous raids they
  never resolve.
- **Global cap on open raids per raid boss** (e.g. 20), oldest-expiring shown first, so the map list
  stays readable on a busy server.

### 8.3 Reward cooldown, not attempt cooldown

Signing up is unlimited. **Rewards** are capped: a character receives full rewards from a given
raid boss **once per ISO week**, enforced by a `RaidRewardClaim { RaidBossId, CharacterId, WeekKey }`
row — precisely the `TowerEchoClear { ServerId, FloorNumber, CharacterId, WeekKey, ClearedAt }`
pattern. Subsequent raids on the same raid boss that week pay **25%** (Trophies and materials only,
no blueprint or jackpot rolls).

This is the right shape because it lets a veteran help a friend's raid without being told "you have
no attempts left", while still bounding the economy. Use the ISO week key generator already in
`WorldTowerService.GetWeekKey` — but note the codebase currently has two `WeekKey` types
(`GuildMissionInstance.WeekKey` is `string`, `TowerContribution.WeekKey` is `int`). Raids should use
the **`int` ISO form** and, ideally, that generator should be lifted into a shared helper rather than
copied a third time.

> `EntryLimit(EntryLimitType {Unlimited, Daily, Weekly, Attempts}, Count, RefreshAtUtc)` already
> exists in `Core/Domain/Models/Dungeons/` and is **never populated or enforced** — dungeons declare
> `dailyEntries` client-side and it goes nowhere. Either wire that type for raids and finally give it
> a consumer, or don't reference it at all. Do not add a third dead limit type.

---

## 9. Contribution and rewards

### 9.1 Contribution must be lane-fair

The leader assigns your wing. Your reward must therefore **not** depend on getting a good
assignment — otherwise being put in Ward is a punishment and players will refuse assignments.

```
LaneShare      = yourDamageInLane / totalWingDamageInLane     // within your own wing only
LaneWeight     = 1/3 for each of Vanguard, Flank, Ward         // all lanes weighted equally
ContributionScore = LaneShare × LaneWeight × WingSizeFactor
```

Where `WingSizeFactor = wingSlotsFilled / rosterSize` normalises so a 3-person Ward wing and a
6-person Vanguard wing distribute proportionally. Net effect: **all three lanes are worth the same
in total**, and within a lane you are measured only against your own wing.

Reward payout is then **mostly flat and slightly performance-weighted**:

```
PayoutMultiplier = 0.70 + 0.30 × min(1.5, ContributionScore / medianContributionScore) / 1.5
```

Everyone who showed up gets at least 70%. A standout gets up to 100%. Nobody gets nothing for being
put in the boring wing, and nobody is rewarded for being carried into Vanguard.

### 9.2 Outcome multiplier

| Outcome           | Multiplier |
| ----------------- | ---------- |
| Slain             | 1.00       |
| Broken (<25% HP)  | 0.65       |
| Wounded (<60% HP) | 0.40       |
| Repelled          | 0.20       |

A failed raid still pays. Fifteen people spending a day's signup on nothing is how a feature dies.

### 9.3 What raids pay

Per the reward direction chosen earlier — gear and blueprints, essence progression materials, a
bounded currency, prestige. **No guild currencies** (raids have no guild involvement) and
**no meaningful Cinders** (Cinders already have many faucets and almost no permanent sink; adding an
endgame Cinder faucet would worsen a known inflation problem).

- **Trophies** — bounded raid currency, spent at a raid boss-site vendor. Bounded by the weekly
  reward cap rather than by attempts.
- **Blueprints first, finished gear rarely.** Each raid boss owns a raid-exclusive blueprint family.
  Blueprints route power through crafting, which already consumes tiered materials, special
  materials, Potential and blueprint copies — all healthy sinks. Two groups who both kill the same
  raid boss still differentiate on crafting. Blueprint copies stay marketplace-tradeable so
  non-raiders care that raids exist; finished raid gear is bound. A small chance (≈8% at Slain) of a
  finished **Unique**, much smaller for **Legendary**, keeps the jackpot moment.
- **Essence progression** — Soul Dust, monster cores, and evolution catalysts in quantity. raid boss
  catalysts should be the best source in the game, since essence progression is currently
  catalyst-starved. Plus one raid-exclusive **Raid Boss Essence** per boss, low drop rate with a
  Trophy purchase as a deterministic pity path.
- **Prestige** — first server kill of each raid boss gets a world-chat announcement (the
  `WorldTowerChatGameEventOutboxConsumer` pattern already exists), a title and a banner. Cheap to
  build, disproportionately motivating.

Reward tables in `rewards/reward-tables.json`, following existing namespacing:

```
reward.raid.<raid-boss>.tier<N>.slain
reward.raid.<raid-boss>.tier<N>.broken
reward.raid.<raid-boss>.tier<N>.wounded
reward.raid.<raid-boss>.tier<N>.repelled
reward.raid.<raid-boss>.tier<N>.first_kill
reward.raid.<raid-boss>.tier<N>.reduced      // post-weekly-cap, 25% payout
```

`WeightedWithNoDrop` rolls for jackpots, `All` rolls for the guaranteed Trophy/material floor.
Rolls use `IRandomSource`, never `Random.Shared`. Rewards are **pull-claimed** from a pending bag
(the `DungeonPendingRewardWriter` / `DungeonRunRewardClaimer` pattern, also how event quests work),
not fanned out on resolution.

### 9.4 A leaderboard, since one is cheap

`LeaderboardBoardKey` already has 14 board keys and no raid board. Add `raid-boss-kills` and
per-raid boss `fastest-slain` (by Vanguard `Duration` in ticks). The infrastructure
(`LeaderboardEntry`/`Board`/`Ranking`/`Cursor`) exists; this is close to free and gives raids a
long tail.

---

## 10. Content authoring

`src/API/API.LL/Data/raids/raid-bosses.json`, following the proven `tower-floors.json` shape.

```json
{
  "id": "raid-boss.hives-abyss",
  "name": "The Hive's Abyss",
  "region": 1,
  "levelRequirement": 25,
  "requiredCompletedQuestId": null,
  "requiredTowerFloor": null,
  "imagePath": "ant_queen",
  "tiers": [
    {
      "tier": 1,
      "laneSlots": 3,
      "minimumRoster": 3,
      "signupWindowHours": 24,
      "recommendedWingPower": { "vanguard": 210, "flank": 150, "ward": 160 },
      "tickBudget": { "vanguard": 6000, "flank": 3000, "ward": 4000 },

      "boss": {
        "creatureId": "…",
        "abilityProfileId": "monster.ant_queen",
        "scaling": {
          "health": 42.0,
          "offense": 26.0,
          "defense": 8.0,
          "resistance": 8.0,
          "penetration": 5.0,
          "regeneration": 3.0
        },
        "variants": [{ "creatureId": "…ant_king", "spawnChancePercent": 8 }],
        "maxReinforceOffensePercent": 40,
        "maxWardBreakPercent": 50,
        "tauntThreatBonus": 100,
        "overtimeStartsAtTick": 4500,
        "overtimePowerIncreasePercent": 6
      },

      "flank": {
        "addGroupId": "raid.hive.worker_brood",
        "adds": [
          {
            "creatureId": "…",
            "count": 4,
            "scaling": { "health": 6.0, "offense": 14.0 }
          }
        ]
      },

      "ward": {
        "objectiveCreatureId": "…",
        "objectiveScaling": {
          "health": 18.0,
          "defense": 22.0,
          "resistance": 22.0,
          "offense": 6.0
        },
        "guards": [
          {
            "creatureId": "…",
            "count": 2,
            "scaling": { "health": 4.0, "offense": 10.0 }
          }
        ]
      },

      "balanceBenchmark": {
        "characterLevel": 30,
        "equipmentTier": 1,
        "equipmentRarity": "Rare",
        "essenceCount": 4
      },

      "rewardTableIds": {
        "slain": "reward.raid.hives_abyss.tier1.slain",
        "broken": "reward.raid.hives_abyss.tier1.broken",
        "wounded": "reward.raid.hives_abyss.tier1.wounded",
        "repelled": "reward.raid.hives_abyss.tier1.repelled",
        "firstKill": "reward.raid.hives_abyss.tier1.first_kill",
        "reduced": "reward.raid.hives_abyss.tier1.reduced"
      }
    }
  ]
}
```

Provided by `IRaidBossDefinitionProvider` / `JsonRaidBossDefinitionProvider` and validated at startup:
unique ids, positive timing and roster values, `minimumRoster ≤ laneSlots × 3`, authored boss/Flank/Ward
creatures, group spawn chances in `(0, 100]`, and cumulative boss-variant chance no greater than 100%.

**Apply scaling the way `WorldTowerGuardianScaling.Apply` does** — convert each multiplier to a
`DungeonAttributeModifier(attr, (m-1)*100, ModifierType.Multiplicative)`.

> **Do not route raid boss scaling through `BossProfiles.RaidBoss`.** It exists with plausible
> multipliers (Health ×6.0, Damage ×2.0, Defense ×2.0, Speed ×1.1, Cdr ×1.3) but is effectively dead
> code: nothing in the JSON pipeline reads `BossRank` or calls `BossProfiles.Get`, and
> `CreatureTemplate.IsBoss`/`BossRank` are authoring-only types with no JSON producer. Author
> explicit per-attribute multipliers as above.

### In-fight phase mechanics

The ability system already supports HP-threshold phases via the shipped Garran idiom:
`OnHealthChanged` + `HealthAtOrBelowPercent` + a stack-guard status so each threshold fires once.
Since a raid boss's HP _is_ meaningfully depleted in the Vanguard battle, real HP-gated phases work
here — unlike the shared-pool design, this is the natural fit. Use them: a raid boss that summons
reinforcements at 50% makes the Flank wing's job feel consequential retroactively.

There is no declarative phase container and nothing consumes `RaidEncounterSourceContext.PhaseIndex`
today; model phases as ability triggers, not as an engine phase machine.

---

## 11. Tuning

### 11.1 Sizing the raid boss

```
BossHealth = ExpectedVanguardDPS × TargetFightSeconds × ClearTargetFraction

ExpectedVanguardDPS = per-character DPS at recommendedWingPower × laneSlots
TargetFightSeconds  = 240–420   (well inside the 600 s tick budget)
ClearTargetFraction = 0.85      // a well-allocated raid at recommended power should usually win
```

Critically, `ExpectedVanguardDPS` must be measured **with `WardBreak` at its expected value**, not
at zero and not at one. Calibrate against a reference allocation, not a best case — otherwise every
raid is either trivial or impossible.

### 11.2 Recommended wing power

Do not author it by hand. Follow `DungeonPowerAnalyzer`: build canonical rosters from
`CanonicalEquipmentBuildFactory` across the equipment tier/rarity ladder, allocate them to wings by
a reference rule, run fixed-seed resolutions, and publish the lowest rung reaching the 85% clear
target with a Wilson interval. Publish `RecommendedWingPower` per lane plus
`Lower/UpperRecommendedPower` and `Confidence`. Cache as
`DungeonPowerRecommendationCacheEntries` does, loaded by a `RaidPowerCalibrationWorker` behind a
config flag.

Reference allocation for calibration: fill Flank with the Area profile, Ward with Sustain, Vanguard
with Offense + Defensive. This reuses the five existing canonical profiles and, usefully, encodes
the intended lane identities into the calibration itself.

### 11.3 Version gates

Add `RaidRulesVersion` (starting at 1) alongside `PowerRatingAlgorithm.Version` (25),
`CombatRulesVersion` (14) and the rating-definition version (16). Bumping any invalidates cached
recommended power. **raid boss health and lane parameters must not change a raid already in
`Mustering`** — pin the resolved tier definition (or its hash) onto `RaidRun` at creation, so a
content deploy cannot alter a raid people have already signed up for.

---

## 12. Data model sketch

New folder `Core/Domain/Models/Raids/`:

- `RaidBossDefinition`, `RaidBossTierDefinition`, `RaidBossLaneDefinition` — content records from JSON.
- `RaidRun` (aggregate) — `Id, RaidBossId, Tier, DefinitionHash, LeaderCharacterId, RaidRunStatus,
CreatedAt, SignupClosesAt, CommencedAt, ResolvedAt, SettledAt, WeekKey,
ReinforcementPenalty?, WardBreak?, BossHealthRemainingPercent?, RaidOutcome?,
SimulationLeaseOwner?, SimulationLeaseUntil?, SimulationAttempts, uint RowVersion`.
- `RaidRunStatus { Mustering, Resolving, Resolved, Settled, Cancelled, Expired }`.
- `RaidOutcome { Repelled, Wounded, Broken, Slain }`.
- `RaidLane { Vanguard, Flank, Ward }`.
- `RaidSignup` — `Id, RaidRunId, CharacterId, AccountId, CharacterSnapshotId, LoadoutHash,
PowerRating, RaidLane?, WingSlotIndex?, SignedUpAt, SnapshotRefreshedAt?`.
  (Assignment lives on the signup — a "wing" is a projection of signups sharing a lane, so there is
  no separate wing table to keep consistent.)
- `RaidLaneResult` — `Id, RaidRunId, RaidLane, Seed, DurationTicks, BattleOutcome,
TotalFriendlyDamage, ObjectiveDamage, ObjectiveBarrierAbsorbed, SurvivingHostileHealthFraction,
DerivedModifier, PlaybackId?`.
- `RaidParticipantResult` — `RaidRunId, CharacterId, RaidLane, DamageDone (incl. summons),
DeathTick?, ContributionScore, PayoutMultiplier, ContributionRank`.
- `RaidRewardClaim` — `RaidBossId, CharacterId, WeekKey, ClaimedAt, WasReduced`.
- `RaidPlayback` / `RaidPlaybackArtifact` — mirror `TowerCombatPlayback*`.

Commands under `Core/Application/UseCases/Raids/Commands/`:
`CreateRaidCommand`, `JoinRaidCommand`, `LeaveRaidCommand`, `RefreshRaidSnapshotCommand`,
`AssignRaidWingCommand`, `UpdateRaidPartiesCommand`, `PreviewRaidBattlePlanCommand` (query — persists nothing),
`CommenceRaidCommand`, `ResolveRaidCommand` (worker), `ClaimRaidRewardsCommand`,
`CancelRaidCommand`, `TransferRaidLeadershipCommand`.

Endpoints, following `POST /api/v1/<feature>/<action>`:

```
GET  /api/v1/raids/bosses                       → map rail + inline panel data
GET  /api/v1/raids/bosses/{id}/open             → open raids at this site
POST /api/v1/raids/create
POST /api/v1/raids/{id}/join
POST /api/v1/raids/{id}/leave
POST /api/v1/raids/{id}/cancel
POST /api/v1/raids/{id}/transfer-leadership → { characterId }
POST /api/v1/raids/{id}/loadout
POST /api/v1/raids/{id}/assign               → { characterId, lane, slotIndex }
PUT  /api/v1/raids/{id}/parties              → { assignments: [{ characterId, lane?, wingSlotIndex? }] }
POST /api/v1/raids/{id}/battle-plan          → preview, stateless
POST /api/v1/raids/{id}/commence
POST /api/v1/raids/{id}/claim
GET  /api/v1/raids/{id}
GET  /api/v1/raids/{id}/lanes/{lane}/playback
GET  /api/v1/raids/active                    → this character's current raid
```

Realtime — add to `GameRealtimeEventNames`, dispatch via the outbox to `Audience.World()` for
listings and to `char:{id}` for personal state:
`RaidCreated, RaidSignupUpdated, RaidWingsUpdated, RaidCommenced, RaidLaneResolved, RaidResolved,
RaidCancelled`. Add a `raid` const to `StateSyncScopes` **and to its `WorldResources` grouping
list** — it is not just a bag of consts, and adding the const alone makes invalidation fan-out
silently skip the scope.

Frontend touch list (all confirmed to exist):

1. `features/game/world/region/region.component.html` — the `activity-panel raids` section: swap the
   data source, make rows clickable mirroring `selectDungeon`.
2. `features/game/world/region/region.component.ts` — add `regionRaidBosses()`, `selectedRaidBossId`,
   `selectRaidBoss()`.
3. `features/game/world/region/raids/raids.component.*` — fill the existing empty placeholder with
   the inline panel, mirroring `region/dungeons/dungeons.component.ts`.
4. New `shared/components/raids/raid-boss-card/` and `raid-muster/` (roster + wing assignment UI),
   mirroring `shared/components/dungeons/dungeon-card/`.
5. `features/game/world/world.routes.ts` — add `raid/:raidId` (muster + result view) alongside
   `dungeon` and `tower/expeditions/:rallyId`; register a `GUIDE_PAGE_IDS` entry in
   `shared/help/guide-catalog.ts`.
6. `shared/models/Dtos/regionDto.ts` — replace the stub `Raid` interface; add `shared/models/Dtos/raids/`.
7. `core/services/api/raid/raid.service.ts` + `raid-state.service.ts`, mirroring
   `world-tower/world-tower.service.ts` and `dungeon/dungeon-state.service.ts`.
8. `core/services/real-time/game-event/game-event.map.ts` + `core/services/real-time/raid/`.
9. `core/services/client-side/region/region.service.ts` — replace hardcoded region data with the
   `RegionController` call (§1).

Backend: `API/API.LL/Controllers/V1/RaidController.cs`,
`Infrastructure/Service/Services.LL/Raids/`, `Data/raids/raid-bosses.json` + provider + validator,
DbSets in `LLDbContext.cs`, a migration, and an outbox consumer alongside
`RealtimeWorldTowerGameEventOutboxConsumer.cs`.

### Correctness requirements

Learned from gaps the existing docs already flag — do not repeat them:

1. `RowVersion` on `RaidRun`; optimistic-concurrency retry on status transitions.
2. **Idempotency on join and on commence.** A double-submitted join must not create two signups; a
   double commence must not resolve twice. Colosseum's lack of idempotency keys is called out as a
   known defect.
3. Advisory lock per `RaidRunId` during resolution, plus the `SimulationLease*` fields so a crashed
   worker's raid is picked up rather than stuck.
4. Resolution + participant results + reward-claim rows written in **one** transaction; broadcast
   **after** commit, mirroring how Colosseum broadcasts already-applied results.
5. The engine checks cancellation every 64 ticks and **throws `OperationCanceledException`** rather
   than returning partial stats — wrap resolution so a cancelled run releases its lease and retries
   instead of half-settling.
6. `CombatantFactory.CreateCreatureCombatant` currently throws
   `NotSupportedException("Raid combatant creation requires raid phase environment data in the
source context.")` for `RaidEncounterSourceContext`. Implement that branch properly — World Tower
   dodges the factory entirely by hand-building participants, and copying that bypass is how the
   duplication problems in this codebase started.
7. **Extract a public snapshot→combatant service.** `WorldTowerService.RehydrateCharacter` is
   `private static` and `ColosseumService.CreateSnapshotCombatEntityAsync` is a private instance
   method. Three consumers (tower, arena, raids) justify lifting one into a shared
   `ISnapshotCombatantBuilder`. Do not write a third copy.
8. Pin `DefinitionHash` at creation so a content deploy cannot change a mustering raid (§11.3).

---

## 13. Phasing

**Phase 1 — MVP (shipped).** One Tier I raid boss in Shenic. Create / sign up / assign three wings / commence.
Three-lane resolution with `ReinforcementPenalty` and `WardBreak`. Graded outcome. Lane-fair
contribution and payout. Authored rewards + Trophies. World Map Raids panel populated and clickable.
Free raid creation. Weekly reward cap. Auto-expiry for invalid musters. Implement the
`CombatantFactory` raid branch and extract the snapshot→combatant service.

**Phase 2 — Depth (shipped).** Battle Plan preview (§6). Per-wing playback. Tier II + a second raid boss in
Meran. raid boss-site Trophy vendor. Raid-exclusive blueprint families. Simulation-backed recommended
wing power via `RaidPowerCalibrationWorker`. Raid leaderboards.

**Phase 3 — Spectacle (shipped).** Tier III. HP-gated raid boss phases that reach across lanes (a raid boss that
summons at 50%, making a cleared Flank retroactively valuable). First-kill world announcements and
titles. Cross-region raid boss with unique mechanics.

**Deliberately deferred:** guild integration of any kind, more than three lanes, live/synchronous
raiding, in-fight player control, seasons.

---

## 14. Risks

| Risk                                          | Mitigation                                                                                                                                                                                                                                                    |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Not enough players sign up on a small server  | `minimumRoster` is a third of max; under-strength raids are allowed and hard. All three wings must be staffed. Tier I is sized for 3.                                                                                                                         |
| Leader goes offline and strands the roster    | Auto-resolve a valid roster or auto-cancel when the signup window closes.                                                                                                                                                                                     |
| Allocation feels like a guess                 | The Battle Plan preview is the answer. If it slips, raids feel like a lottery — protect it in scope.                                                                                                                                                          |
| Players resent being assigned the boring wing | Lane-fair contribution (§9.1) + 70% floor payout. All lanes weighted equally by construction.                                                                                                                                                                 |
| Toxic leaders excluding people                | Signups are first-come and public; a leader can't kick after commence. Consider capping how many signups a leader may decline per raid if it becomes a problem.                                                                                               |
| Three battles per raid is 3× the CPU          | Worker-driven with leases, `CaptureEventLog: false` for previews, rate-limited preview. Tower already resolves 5-vs-1 at 6000 ticks with recorded perf metrics.                                                                                               |
| Raid gear devalues crafting                   | Blueprints-first (§9.3) means raids feed crafting rather than bypassing it.                                                                                                                                                                                   |
| Identity overlap with World Tower             | Tower = vertical, one party, server-shared floor progress, token rewards. Raids = horizontal, three wings, allocation puzzle, gear/material rewards, region-anchored. Keep Tower rewards as Tower Tokens and raid rewards as materials so they don't compete. |

---

## 15. Shipped implementation decisions

1. **Signups are first-come-first-served.** Eligible characters enter an unassigned pool. The leader
   controls allocation, not admission.
2. **Raid boss unlock gates apply to every signup.** A character below the authored level, quest, or
   World Tower requirement cannot join; there is no carry exception for first-kill rewards.
3. **Every raid uses exactly three lanes.** Vanguard, Flank, and Ward are fixed parts of the system's
   identity and its UI.
4. **Leadership has no payout multiplier.** Contribution remains lane-fair. A transferred leader can
   manage or cancel the raid without transferring or refunding an entry resource.
5. **Signup windows are authored per tier.** Current content uses 18–24 hours. Fill-rate telemetry
   should inform later content changes.
6. **Raid boss regions follow the dungeon content convention.** JSON owns the numeric primary region;
   the optional `regions` array exposes a raid boss in additional regions. No `Region` navigation is
   added to the domain.
7. **New participants start on the Bench.** Vanguard, Flank, and Ward are parties of at most five;
   the Leader owns exact-slot assignment through an atomic full-layout update.

---

## Appendix A — Codebase reality check

**Already exists, reuse as-is:**
World Map `activity-panel raids` section and empty `raids.component.ts` placeholder ·
`regionDto.ts` `Raid` stub · commented `Region.Raids` · `FastCombatEngine` (N-vs-M, threat-weighted
targeting, 32 `AbilityEffectOperation` primitives, 23 `StandardConditionType` conditions, 32
`AbilityTriggerEvent` triggers, 23 `AbilityConditionType` conditions incl. HP thresholds) ·
`CombatMode.Raid` (=3) · `RaidEncounterSourceContext(Guid, int, string)` ·
`RaidCombatOrchestrationRequest(Guid, Guid, DateTimeOffset)` · `FastCombatEngineOptions.Overtime*`
and `TauntThreatBonus` · `CharacterSnapshot` + `ICharacterSnapshotService.CreateAsync` ·
`TowerRally` / `TowerRallyApplication` / `TowerRallyParticipant` lobby pattern ·
`GetJoinEligibilityAsync` locks · `TowerAttempt` lease pattern ·
`TowerCombatPlayback` / `TowerCombatPlaybackArtifact` · `TowerEchoClear` weekly reward lock ·
`WorldTowerService.GetWeekKey` ISO week · `tower-floors.json` boss schema ·
`WorldTowerGuardianScaling.Apply` ·
`DungeonReadinessService` Wilson-interval readiness · `DungeonPowerAnalyzer` +
`CanonicalEquipmentBuildFactory` · `DungeonPendingRewardWriter` / `DungeonRunRewardClaimer` ·
`IRewardRoller` + `reward-tables.json` · `GameHub` + `Audience.World()` + outbox consumers ·
`WorldTowerChatGameEventOutboxConsumer` world-chat pattern · `LeaderboardBoardKey` infrastructure ·
`EntityStats.DamageDone` · advisory lock helpers.

**Implemented for Raids:**
raid domain models, EF configs, DbSets and migrations · JSON raid-boss and Trophy-vendor catalogs with
startup validation · the `CombatantFactory` raid branch and shared `ISnapshotCombatantBuilder` ·
three-lane resolution and lane-fair, summon-aware contribution · Battle Plan previews · leased
resolution worker and Brotli playback artifacts · free raid creation · persisted, version-gated
`RaidPowerCalibrationWorker` recommendations · World Map rail, muster/result UI, routes and state-sync
invalidation · Trophy vendor and raid blueprint families · aggregate and per-boss speed leaderboards ·
first-kill titles and world-chat announcements · explicit cancellation and leadership transfer ·
Tier II/III and cross-region raid-boss content.
Raid muster additionally includes a bench-first, exact-slot three-party layout with click/drag
placement, distribution and auto-balance controls, plus atomic server-side layout validation.

**Do not build on:** `BossProfiles.RaidBoss` / `BossRank` / `CreatureTemplate.IsBoss` —
authoring-only types with no JSON producer and no live consumer.
`EntryLimit` / `EntryLimitType` — declared in the dungeon domain and never populated or enforced;
wire it or ignore it, don't add a third dead limit.

**Known landmines flagged elsewhere in `docs/`:** Colosseum lacks row-version locking and
idempotency keys; `LootService` still uses static `Random`; guild content is duplicated between JSON
and a provider class; `DungeonPreviewData.dailyEntries` exists client-side with no server field.
A raid feature should not inherit any of these.
