# Raid System — Game Design Document

**Status:** Design proposal (not implemented)
**Author:** Design pass, 2026-08-18
**Feature name (player-facing):** **Raids**
**Surface:** World Map, in the Raids rail alongside Dungeons
**Guild involvement:** none

---

## 0. Executive summary

A **Raid** is a public, player-created assault on a **Scourge** — a regional raid boss that lives
permanently on the World Map next to the region's dungeons. Any player can create a raid at a
Scourge site. Any player can sign up. When signups are in, the **raid leader** sorts them into
**three Wings** — _Vanguard_, _Flank_ and _Ward_ — and starts the raid. The server resolves the
three wings as three separate simulated battles, in a fixed order, where the Flank and Ward results
change the conditions the Vanguard fights under. Vanguard kills the Scourge, or it doesn't.

The strategy is not in twitch execution or role labels. It is entirely in **allocation**: the leader
has a finite roster of known power ratings and must decide who fights where. Stack everyone strong
into the Vanguard and it meets a shielded, reinforced Scourge and dies. Over-invest in the support
wings and the Vanguard lacks the damage to finish. There is a right answer for every roster and it
changes with every roster.

Nothing needs to happen in real time. Every participant is a frozen `CharacterSnapshot`, exactly as
World Tower Expeditions already work, and resolution is done by a worker and replayed frame-by-frame
afterwards. Fifteen people never need to be online at once — they need to have signed up.

### Why this shape

| Constraint                                                   | How the design answers it                                                                                                                                                             |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Idle game, players not co-present                            | Snapshot signups + worker resolution + playback. Signup window can be hours or days.                                                                                                  |
| World Tower already owns "public 5-player lobby vs one boss" | Raids are one leader allocating **three** wings whose outcomes feed each other. Tower is vertical, single-party, floor-by-floor; raids are horizontal, multi-wing, allocation-driven. |
| No guild gating                                              | Raids are listed publicly per region. Create, sign up, done. No guild reference anywhere in the domain.                                                                               |
| No role system wanted                                        | Strategy lives in _who goes where_, not in role labels. Build identity still matters, emergently, through which wing a character suits.                                               |
| Must sit where dungeons sit                                  | The World Map's Raids rail panel already exists and is empty (§1).                                                                                                                    |

### What already exists to build on

| Need                                     | Existing thing it reuses                                                                                                       |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| Raids panel on the World Map             | `region.component.html` `<section class="activity-panel raids">`, bound to `region.raids` — **already rendered, always empty** |
| Empty component to fill                  | `features/game/world/region/raids/raids.component.ts` — **existing 185-byte placeholder**                                      |
| Stub DTO                                 | `shared/models/Dtos/regionDto.ts` → `Raid { id, name, creatures }`                                                             |
| Commented-out domain hook                | `Core/Domain/Models/Regions/Region.cs` → `// ICollection<Raid> Raids`                                                          |
| Public lobby + signup + approval         | `TowerRally` / `TowerRallyApplication` / `TowerRallyParticipant`                                                               |
| Frozen participants                      | `CharacterSnapshot`, `ICharacterSnapshotService.CreateAsync`                                                                   |
| N-vs-M tick combat                       | `FastCombatEngine.Run(friendly, hostile, …)`                                                                                   |
| Boss authoring & scaling                 | `tower-floors.json` schema + `WorldTowerGuardianScaling.Apply`                                                                 |
| Worker resolution + replay               | `TowerAttempt` leases, `TowerCombatPlayback` / `TowerCombatPlaybackArtifact`                                                   |
| Entry cost pattern                       | `DungeonDefinition.EntryCosts` + sigil assembly                                                                                |
| Broadcast to everyone                    | `GameHub` + `Audience.World()` + outbox consumer                                                                               |
| Rewards                                  | `rewards/reward-tables.json` + `IRewardRoller`                                                                                 |
| Once-per-week-per-character reward locks | `TowerEchoClear`                                                                                                               |

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

**The Raids panel already exists in the markup** (`data-tour="raids-introduction"`), bound to
`region.raids`, and is always empty because `region.service.ts` returns hardcoded client-side region
data with `raids: []`. There is also an empty `region/raids/raids.component.ts` placeholder sitting
next to the working `region/dungeons/dungeons.component.ts`.

So raids do not need a new surface. They need that panel populated and made clickable, mirroring
`selectDungeon()` → inline preview → enter.

**Presentation parity with dungeons.** A Scourge row in the rail should read like a dungeon row:
name, required level, whether you can act, and a state chip. Where a dungeon shows
`ownedSigilCount` / `canEnter`, a Scourge shows **live raid state** — the thing that makes it feel
alive on the map:

```
Ashen Scourge          Lv. 25    ● 2 raids recruiting
Hollow Tyrant          Lv. 40    ○ locked — requires Lv. 40
Duskmaw                Lv. 55    ⏱ on cooldown for you — 6h
```

Clicking expands an inline panel (the raid equivalent of `dungeon-card`) listing that Scourge's
**open raids**: leader name, signup count per wing, tier, closes-in timer, and a Join button —
plus a "Create raid" button if the player holds a key and isn't already committed.

> **Recommendation:** move region data server-side while doing this. `region.service.ts` is
> hardcoded and `RegionController.GetRegion` already exists but is not called by the map. Raid state
> is inherently live (recruiting counts, cooldowns) and cannot be hardcoded, so the map will need a
> real endpoint regardless. Do that properly rather than bolting a second data path onto a hardcoded
> component.

### Region anchoring and gating

Each region gets 1–2 Scourges, gated like areas are: `levelRequirement`, optional
`requiredCompletedQuestId`, optional `requiredTowerFloor`. Region membership follows the existing
convention — dungeons already carry `"region": 1` in `dungeons.json`, so Scourges carry the same.
Shenic gets an entry-level Scourge; Meran a harder one. They are **always available** — no rotation,
no windows. A player who wants a specific drop can keep raiding that specific Scourge.

Locked Scourges should be **visible but locked** (like areas without `hideWhenLocked`), because a
visible locked raid is aspirational and teaches that the content exists.

---

## 2. Vocabulary

| Term        | Meaning                                                             | Code shape                        |
| ----------- | ------------------------------------------------------------------- | --------------------------------- |
| **Scourge** | A regional raid boss. Content, not state. Permanent map fixture.    | `ScourgeDefinition` (JSON)        |
| **Raid**    | One player-created instance: a roster, three wings, one resolution. | `RaidRun` aggregate (`RaidRunId`) |
| **Leader**  | The player who created the raid. Sorts the roster, starts it.       | `RaidRun.LeaderCharacterId`       |
| **Signup**  | A request to join, freezing a snapshot.                             | `RaidSignup`                      |
| **Wing**    | One of three sub-forces. 1:1 with a lane.                           | `RaidWing` (`RaidPartyId`)        |
| **Lane**    | Vanguard / Flank / Ward — what a wing attacks.                      | `RaidLane` enum, → `StageKey`     |
| **Muster**  | The recruiting phase.                                               | `RaidRunStatus.Mustering`         |
| **Warhorn** | The consumable key the leader spends to create a raid.              | Inventory item                    |
| **Trophy**  | Bounded raid currency.                                              | New currency                      |

> **Naming check:** do not call the boss a "Warden" — `TowerFloorType.Warden` already exists and the
> collision will be confusing in code and in the UI. "Scourge" is used throughout this document;
> grep before committing to it.

---

## 3. The raid lifecycle

```
   ┌─ CREATE ────────────────────────────────────────────────────────────┐
   │  Player spends a Warhorn at a Scourge site on the World Map.       │
   │  Picks tier. Raid appears publicly in that Scourge's raid list.    │
   │  Creator becomes Leader. Signup window opens (default 24h).        │
   └──────────────────────────────┬──────────────────────────────────────┘
                                  ▼
   ┌─ MUSTER ───────────────────────────────────────────────────────────┐
   │  Anyone eligible signs up → CharacterSnapshot + PowerRating frozen │
   │  Leader sees the roster with power ratings and suggested fits       │
   │  Leader drags signups into Vanguard / Flank / Ward wings            │
   │  Leader may run a free Battle Plan preview (§6) any number of times │
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
signup window with whatever assignment exists, or auto-cancels (refunding the Warhorn) if fewer than
the minimum roster signed up. A leader going offline must not strand a dozen people's snapshots.
This is the single most important reliability rule in the design.

**Leader succession.** If the leader has not logged in for 12h and the window is within 2h of
closing, leadership passes to the highest-power participant who has logged in most recently. Cheap
to implement, prevents dead raids.

---

## 4. The three lanes — where the strategy is

This is the core mechanic. Three wings fight three separate battles, and **the resolution order
creates a dependency chain**:

### 4.1 Flank — resolved first

**Fights:** the Scourge's escort — an add group (3–6 creatures, authored per tier).

**Produces:** `ReinforcementPenalty`, a scalar in `[0, 1]`.

```
addsRemainingFraction = survivingAddHealth / totalAddHealth
ReinforcementPenalty  = addsRemainingFraction
```

Every add left alive reinforces the boss. Applied to the Vanguard battle as:

- the surviving adds **join the Vanguard fight** as extra hostiles, and
- the Scourge gains `+ (ReinforcementPenalty × MaxReinforceOffense)` offense (authored, e.g. 40%).

Clearing the Flank completely means the Vanguard fights the Scourge alone. Ignoring the Flank means
the Vanguard fights the Scourge _and_ its whole escort, with the Scourge hitting 40% harder.

### 4.2 Ward — resolved second

**Fights:** the Scourge's protective ward — a high-mitigation, high-barrier, low-damage objective
creature, plus a small guard.

**Produces:** `WardBreak`, a scalar in `[0, 1]`.

```
WardBreak = min(1, damageDealtToWard / wardHealth)
```

Applied to the Vanguard battle as a reduction of the Scourge's defences:

- `Armor`, `Resistance` and `DamageReduction` reduced by `WardBreak × MaxWardBreakPercent`
  (authored, e.g. 50%).

The Ward is deliberately a _damage-soak check_, not a kill check — partial credit is smooth, so a
weak Ward wing still contributes something and a strong one caps out. That smoothness is what makes
allocation a tuning problem rather than a binary.

### 4.3 Vanguard — resolved last

**Fights:** the Scourge itself, with `ReinforcementPenalty` and `WardBreak` applied, plus any
surviving Flank adds.

**Produces:** the raid outcome.

```
Slain      — Scourge reaches 0 HP within the tick budget
Broken     — Scourge ends below 25% HP
Wounded    — Scourge ends below 60% HP
Repelled   — Scourge ends above 60% HP, or the Vanguard wipes
```

The Scourge has real, authored HP and can genuinely die inside one battle. `BattleOutcome.Victory`
means what it says. There is no shared pool and no artificial HP slice.

### 4.4 Why this is a real decision

The leader's roster is finite. Three examples with the same 12 players:

- **All strength into Vanguard.** Flank and Ward wings are weak → adds survive, ward holds → the
  Vanguard meets a 40%-stronger Scourge at full mitigation, alongside six adds. Repelled.
- **Even split.** Flank clears, Ward breaks ~60% → Vanguard meets a solo Scourge at ~70% mitigation.
  Usually Wounded or Broken.
- **Correct read.** Enough in Flank to fully clear (it's a low-HP-many-targets fight — multi-target
  builds shine), the minimum in Ward that still caps `WardBreak` (it's a sustained-damage-into-a-wall
  fight — single-target attrition builds shine), everything else Vanguard. Slain.

Build identity matters _emergently_ — a multi-target build is genuinely better in Flank, a
high-sustained-DPS build is genuinely better in Ward — without any role labels, eligibility checks,
or assignment validation. That is the whole point of the "no roles" decision: the lanes do the work
that roles would have done, and they do it through the combat sim rather than through metadata.

### 4.5 Empty and short wings

A wing may be left empty. Its battle is skipped and its output takes the worst value
(`ReinforcementPenalty = 1`, `WardBreak = 0`). Starting a raid with an empty Flank is a legitimate,
punishing choice. Wings may be uneven — 6/3/3 is allowed. Cap per wing is authored
(`laneSlots`, e.g. 5), which is what bounds total roster size.

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

1. Character meets the Scourge's `levelRequirement` and any quest/area gate.
2. Character is not already committed to another raid in `Mustering` or `Resolving` status —
   one active raid per character.
3. One slot per **account** per raid (`AccountId == character.UserId`), so alts cannot stack a roster.
4. `PowerRating` must be in `PowerAnalysisState.Available` — the leader needs a number to allocate on.
5. Character is not on personal cooldown for this Scourge (§8.3).

**Snapshot freeze.** Signing up calls `ICharacterSnapshotService.CreateAsync` and stores
`CharacterSnapshotId` + `PowerRating` + `LoadoutHash` on the signup, mirroring
`TowerRallyParticipant`. A participant may re-snapshot while the raid is still mustering
(`POST raids/{id}/loadout`, as the Tower does) — so upgrading your gear during the window pays off,
and the leader sees a "loadout updated" marker so their plan isn't silently invalidated.

**Nobody is present at resolution.** Every combatant, including the leader, fights as a snapshot.
This is the load-bearing simplification: it makes signup windows arbitrarily long, removes all
scheduling, and means a raid resolves correctly at 04:00 with everyone asleep.

---

## 6. The Battle Plan preview — the leader's tool

Allocation is only strategic if the leader can reason about it. Give them a **free, unlimited,
stateless simulation** of the current assignment:

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
> Scourge, not this.

---

## 7. Resolution mechanics

### 7.1 Pipeline

```
seed_base = stableHash(RaidRunId)

1. Flank:    engine.Run(flankWing,    addGroup)     seed = seed_base ^ 0x11
             → ReinforcementPenalty, survivingAdds
2. Ward:     engine.Run(wardWing,     wardObjective) seed = seed_base ^ 0x22
             → WardBreak
3. Vanguard: engine.Run(vanguardWing, [scourge(mods)] + survivingAdds)
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
  gzipped compact bundle, ETag'd, `TicksPerFrame 10`) so participants can watch their own wing.
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
  defaults to **1800**. State the raid tick budget per lane in the Scourge definition; the entire
  difficulty model is calibrated against it.

### 7.4 A note on threat, since there are no roles

Targeting is threat-weighted roulette (`GetEffectiveThreat`, `BaseThreat = 100f`), and
`TauntThreatBonus` defaults to only `100f` — so a taunting character draws just ~33% of attacks
against four allies. **This design does not depend on that**, because it has no tank role. It is
still worth knowing: a Vanguard wing of five glass cannons will lose members unpredictably, and
that emergent lesson ("bring somebody who can absorb hits") is a _desirable_ teaching moment rather
than a mechanic to specify. If playtesting shows Vanguard outcomes are too random, raising
`TauntThreatBonus` per Scourge is a free dial — it is a constructor option, no engine change.

---

## 8. Costs, cooldowns and anti-abuse

### 8.1 The Warhorn — the leader's cost

Creating a raid consumes one **Warhorn**, held by the leader only. Signing up is free — charging
participants would suppress the roster, which is the resource the design actually needs.

Warhorns follow the sigil pattern exactly (`DungeonDefinition.EntryCosts` +
`SigilAssemblyCost` + `sigil-assembly.json`): they drop from tier-appropriate dungeons and areas,
and can be assembled from **Warhorn Fragments**. This makes leading a raid an earned act, gives
Scourge-tier progression a material gate, and reuses a system players already understand.

Suggested: Tier I Warhorn = 20 fragments; Tier II = 40; Tier III = 70. Refunded in full on
auto-cancel (insufficient roster), not refunded on a Repelled outcome — losing must cost something.

### 8.2 Participation locks

- **One active raid per character** (`Mustering` or `Resolving`) — the Tower's
  `ActiveRallyStatuses` check, verbatim in spirit.
- **One slot per account per raid** — blocks alt-stacking a roster.
- **One raid led at a time per character** — blocks a player farming Warhorns into a dozen
  simultaneous raids they never resolve.
- **Global cap on open raids per Scourge** (e.g. 20), oldest-expiring shown first, so the map list
  stays readable on a busy server.

### 8.3 Reward cooldown, not attempt cooldown

Signing up is unlimited. **Rewards** are capped: a character receives full rewards from a given
Scourge **once per ISO week**, enforced by a `RaidRewardClaim { ScourgeId, CharacterId, WeekKey }`
row — precisely the `TowerEchoClear { ServerId, FloorNumber, CharacterId, WeekKey, ClearedAt }`
pattern. Subsequent raids on the same Scourge that week pay **25%** (Trophies and materials only,
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

- **Trophies** — bounded raid currency, spent at a Scourge-site vendor. Bounded by the weekly
  reward cap rather than by attempts.
- **Blueprints first, finished gear rarely.** Each Scourge owns a raid-exclusive blueprint family.
  Blueprints route power through crafting, which already consumes tiered materials, special
  materials, Potential and blueprint copies — all healthy sinks. Two groups who both kill the same
  Scourge still differentiate on crafting. Blueprint copies stay marketplace-tradeable so
  non-raiders care that raids exist; finished raid gear is bound. A small chance (≈8% at Slain) of a
  finished **Unique**, much smaller for **Legendary**, keeps the jackpot moment.
- **Essence progression** — Soul Dust, monster cores, and evolution catalysts in quantity. Scourge
  catalysts should be the best source in the game, since essence progression is currently
  catalyst-starved. Plus one raid-exclusive **Scourge Essence** per boss, low drop rate with a
  Trophy purchase as a deterministic pity path.
- **Prestige** — first server kill of each Scourge gets a world-chat announcement (the
  `WorldTowerChatGameEventOutboxConsumer` pattern already exists), a title and a banner. Cheap to
  build, disproportionately motivating.

Reward tables in `rewards/reward-tables.json`, following existing namespacing:

```
reward.raid.<scourge>.tier<N>.slain
reward.raid.<scourge>.tier<N>.broken
reward.raid.<scourge>.tier<N>.wounded
reward.raid.<scourge>.tier<N>.repelled
reward.raid.<scourge>.tier<N>.first_kill
reward.raid.<scourge>.tier<N>.reduced      // post-weekly-cap, 25% payout
```

`WeightedWithNoDrop` rolls for jackpots, `All` rolls for the guaranteed Trophy/material floor.
Rolls use `IRandomSource`, never `Random.Shared`. Rewards are **pull-claimed** from a pending bag
(the `DungeonPendingRewardWriter` / `DungeonRunRewardClaimer` pattern, also how event quests work),
not fanned out on resolution.

### 9.4 A leaderboard, since one is cheap

`LeaderboardBoardKey` already has 14 board keys and no raid board. Add `raid-scourge-kills` and
per-Scourge `fastest-slain` (by Vanguard `Duration` in ticks). The infrastructure
(`LeaderboardEntry`/`Board`/`Ranking`/`Cursor`) exists; this is close to free and gives raids a
long tail.

---

## 10. Content authoring

`src/API/API.LL/Data/raids/scourges.json`, following the proven `tower-floors.json` shape.

```json
{
  "id": "scourge.ashen_colossus",
  "name": "The Ashen Colossus",
  "region": 1,
  "levelRequirement": 25,
  "requiredCompletedQuestId": null,
  "requiredTowerFloor": null,
  "imagePath": "…",
  "tiers": [
    {
      "tier": 1,
      "laneSlots": 3,
      "minimumRoster": 3,
      "signupWindowHours": 24,
      "warhornItemId": "warhorn_ashen",
      "recommendedWingPower": { "vanguard": 210, "flank": 150, "ward": 160 },
      "tickBudget": { "vanguard": 6000, "flank": 3000, "ward": 4000 },

      "scourge": {
        "creatureId": "…",
        "abilityProfileId": "monster.ashen_colossus",
        "scaling": {
          "health": 42.0,
          "offense": 26.0,
          "defense": 8.0,
          "resistance": 8.0,
          "penetration": 5.0,
          "regeneration": 3.0
        },
        "maxReinforceOffensePercent": 40,
        "maxWardBreakPercent": 50,
        "tauntThreatBonus": 100,
        "overtimeStartsAtTick": 4500,
        "overtimePowerIncreasePercent": 6
      },

      "flank": {
        "addGroupId": "raid.ashen.cinder_thralls",
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
        "slain": "reward.raid.ashen_colossus.tier1.slain",
        "broken": "reward.raid.ashen_colossus.tier1.broken",
        "wounded": "reward.raid.ashen_colossus.tier1.wounded",
        "repelled": "reward.raid.ashen_colossus.tier1.repelled",
        "firstKill": "reward.raid.ashen_colossus.tier1.first_kill",
        "reduced": "reward.raid.ashen_colossus.tier1.reduced"
      }
    }
  ]
}
```

Provided by `IScourgeDefinitionProvider` / `JsonScourgeDefinitionProvider`, validated at startup by
a `ScourgeDefinitionValidator` in the same pattern as `DungeonDefinitionValidator` /
`RewardTableDefinitionValidator`: unique ids, reward-table existence, ability-profile existence,
positive health, `minimumRoster ≤ laneSlots × 3`, non-null add group when Flank is authored.

**Apply scaling the way `WorldTowerGuardianScaling.Apply` does** — convert each multiplier to a
`DungeonAttributeModifier(attr, (m-1)*100, ModifierType.Multiplicative)`.

> **Do not route Scourge scaling through `BossProfiles.RaidBoss`.** It exists with plausible
> multipliers (Health ×6.0, Damage ×2.0, Defense ×2.0, Speed ×1.1, Cdr ×1.3) but is effectively dead
> code: nothing in the JSON pipeline reads `BossRank` or calls `BossProfiles.Get`, and
> `CreatureTemplate.IsBoss`/`BossRank` are authoring-only types with no JSON producer. Author
> explicit per-attribute multipliers as above.

### In-fight phase mechanics

The ability system already supports HP-threshold phases via the shipped Garran idiom:
`OnHealthChanged` + `HealthAtOrBelowPercent` + a stack-guard status so each threshold fires once.
Since a Scourge's HP _is_ meaningfully depleted in the Vanguard battle, real HP-gated phases work
here — unlike the shared-pool design, this is the natural fit. Use them: a Scourge that summons
reinforcements at 50% makes the Flank wing's job feel consequential retroactively.

There is no declarative phase container and nothing consumes `RaidEncounterSourceContext.PhaseIndex`
today; model phases as ability triggers, not as an engine phase machine.

---

## 11. Tuning

### 11.1 Sizing the Scourge

```
ScourgeHealth = ExpectedVanguardDPS × TargetFightSeconds × ClearTargetFraction

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

Add `RaidRulesVersion` (starting at 1) alongside `PowerRatingAlgorithm.Version` (23),
`CombatRulesVersion` (11) and the rating-definition version (13). Bumping any invalidates cached
recommended power. **Scourge health and lane parameters must not change a raid already in
`Mustering`** — pin the resolved tier definition (or its hash) onto `RaidRun` at creation, so a
content deploy cannot alter a raid people have already signed up for.

---

## 12. Data model sketch

New folder `Core/Domain/Models/Raids/`:

- `ScourgeDefinition`, `ScourgeTierDefinition`, `ScourgeLaneDefinition` — content records from JSON.
- `RaidRun` (aggregate) — `Id, ScourgeId, Tier, DefinitionHash, LeaderCharacterId, RaidRunStatus,
CreatedAt, SignupClosesAt, CommencedAt, ResolvedAt, SettledAt, WeekKey,
ReinforcementPenalty?, WardBreak?, ScourgeHealthRemainingPercent?, RaidOutcome?,
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
- `RaidRewardClaim` — `ScourgeId, CharacterId, WeekKey, ClaimedAt, WasReduced`.
- `RaidPlayback` / `RaidPlaybackArtifact` — mirror `TowerCombatPlayback*`.

Commands under `Core/Application/UseCases/Raids/Commands/`:
`CreateRaidCommand`, `JoinRaidCommand`, `LeaveRaidCommand`, `RefreshRaidSnapshotCommand`,
`AssignRaidWingCommand`, `PreviewRaidBattlePlanCommand` (query — persists nothing),
`CommenceRaidCommand`, `ResolveRaidCommand` (worker), `ClaimRaidRewardsCommand`,
`CancelRaidCommand`, `AssembleWarhornCommand`, `TransferRaidLeadershipCommand`.

Endpoints, following `POST /api/v1/<feature>/<action>`:

```
GET  /api/v1/raids/scourges                  → map rail + inline panel data
GET  /api/v1/raids/scourges/{id}/open        → open raids at this site
POST /api/v1/raids/create
POST /api/v1/raids/{id}/join
POST /api/v1/raids/{id}/leave
POST /api/v1/raids/{id}/loadout
POST /api/v1/raids/{id}/assign               → { characterId, lane, slotIndex }
POST /api/v1/raids/{id}/battle-plan          → preview, stateless
POST /api/v1/raids/{id}/commence
POST /api/v1/raids/{id}/claim
GET  /api/v1/raids/{id}
GET  /api/v1/raids/{id}/lanes/{lane}/playback
GET  /api/v1/raids/active                    → this character's current raid
POST /api/v1/raids/scourges/{id}/assemble-warhorn
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
2. `features/game/world/region/region.component.ts` — add `regionScourges()`, `selectedScourgeId`,
   `selectScourge()`.
3. `features/game/world/region/raids/raids.component.*` — fill the existing empty placeholder with
   the inline panel, mirroring `region/dungeons/dungeons.component.ts`.
4. New `shared/components/raids/scourge-card/` and `raid-muster/` (roster + wing assignment UI),
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
`Infrastructure/Service/Services.LL/Raids/`, `Data/raids/scourges.json` + provider + validator,
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

**Phase 1 — MVP.** One Tier I Scourge in Shenic. Create / sign up / assign three wings / commence.
Three-lane resolution with `ReinforcementPenalty` and `WardBreak`. Graded outcome. Lane-fair
contribution and payout. Reward tables + Trophies. World Map Raids panel populated and clickable.
Warhorn + fragment assembly. Weekly reward cap. Auto-expiry and leader succession. Implement the
`CombatantFactory` raid branch and extract the snapshot→combatant service.

**Phase 2 — Depth.** Battle Plan preview (§6). Per-wing playback. Tier II + a second Scourge in
Meran. Scourge-site Trophy vendor. Raid-exclusive blueprint families. Simulation-backed recommended
wing power via `RaidPowerCalibrationWorker`. Raid leaderboards.

**Phase 3 — Spectacle.** Tier III. HP-gated Scourge phases that reach across lanes (a Scourge that
summons at 50%, making a cleared Flank retroactively valuable). First-kill world announcements and
titles. Cross-region Scourge with unique mechanics.

**Deliberately deferred:** guild integration of any kind, more than three lanes, live/synchronous
raiding, in-fight player control, seasons.

---

## 14. Risks

| Risk                                          | Mitigation                                                                                                                                                                                                                                                    |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Not enough players sign up on a small server  | `minimumRoster` is a third of max; under-strength raids are allowed and hard. Empty wings are legal. Tier I sized for 3.                                                                                                                                      |
| Leader goes offline and strands the roster    | Auto-resolve or auto-cancel at window close; leader succession after 12h idle (§3). Non-negotiable.                                                                                                                                                           |
| Allocation feels like a guess                 | The Battle Plan preview is the answer. If it slips, raids feel like a lottery — protect it in scope.                                                                                                                                                          |
| Players resent being assigned the boring wing | Lane-fair contribution (§9.1) + 70% floor payout. All lanes weighted equally by construction.                                                                                                                                                                 |
| Toxic leaders excluding people                | Signups are first-come and public; a leader can't kick after commence. Consider capping how many signups a leader may decline per raid if it becomes a problem.                                                                                               |
| Three battles per raid is 3× the CPU          | Worker-driven with leases, `CaptureEventLog: false` for previews, rate-limited preview. Tower already resolves 5-vs-1 at 6000 ticks with recorded perf metrics.                                                                                               |
| Raid gear devalues crafting                   | Blueprints-first (§9.3) means raids feed crafting rather than bypassing it.                                                                                                                                                                                   |
| Identity overlap with World Tower             | Tower = vertical, one party, server-shared floor progress, token rewards. Raids = horizontal, three wings, allocation puzzle, gear/material rewards, region-anchored. Keep Tower rewards as Tower Tokens and raid rewards as materials so they don't compete. |

---

## 15. Open questions

1. **Should the leader approve signups, or is it first-come-first-served?** The Tower uses
   leader-approved applications. First-come is friendlier and less work; approval gives leaders
   control over roster quality. Recommendation: **first-come into an unassigned pool**, with the
   leader's power being _allocation_, not admission. Revisit if griefing appears.
2. **Can a player sign up to a raid whose tier is far above their level?** Allowing it means
   under-levelled players get carried; blocking it means small servers can't fill rosters.
   Recommendation: allow, but exclude from `firstKill` and jackpot rolls below a power floor.
3. **Three lanes forever, or is lane count per Scourge?** Three is the design's spine and the UI is
   built for it. A two-lane Tier I tutorial Scourge might teach better. Worth a playtest.
4. **Does the leader get a bonus?** They spend the Warhorn and do the work. A modest Trophy bonus
   (+15%) seems fair; anything larger creates lead-farming. Confirm.
5. **Signup window length** — 24h default is a guess. Long enough that a small server can fill,
   short enough that raids feel live. Instrument fill-rate-vs-window before authoring Tier II.
6. **Is `Region.cs`'s commented-out `ICollection<Raid> Raids` the intended model?** Attaching
   Scourges to `Region` in the domain is cleaner than the `"region": 1` int that dungeons use, but it
   diverges from the dungeon precedent. Pick one convention for both and say so.

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
`WorldTowerGuardianScaling.Apply` · `DungeonDefinition.EntryCosts` + `sigil-assembly.json` ·
`DungeonReadinessService` Wilson-interval readiness · `DungeonPowerAnalyzer` +
`CanonicalEquipmentBuildFactory` · `DungeonPendingRewardWriter` / `DungeonRunRewardClaimer` ·
`IRewardRoller` + `reward-tables.json` · `GameHub` + `Audience.World()` + outbox consumers ·
`WorldTowerChatGameEventOutboxConsumer` world-chat pattern · `LeaderboardBoardKey` infrastructure ·
`EntityStats.DamageDone` · advisory lock helpers.

**Must be built:**
All raid domain models, EF configs, DbSets and a migration (zero `Raid` DbSets today, no
`Domain/Models/Raids` folder) · `Data/raids/scourges.json` + provider + validator ·
`CombatantFactory` branch for `RaidEncounterSourceContext` (currently throws) · a **public
snapshot→combatant service** extracted from the two private implementations · three-lane resolution
pipeline and modifier derivation · lane-fair contribution maths · Battle Plan preview ·
resolution worker + playback artifacts · Warhorn item + fragment assembly ·
`RaidPowerCalibrationWorker` · World Map rail population, raid muster UI, routes, realtime handler ·
server-side region endpoint wiring · `raid` `StateSyncScopes` const **and** grouping-list entry ·
summon-aware damage attribution when summing `EntityStats`.

**Do not build on:** `BossProfiles.RaidBoss` / `BossRank` / `CreatureTemplate.IsBoss` —
authoring-only types with no JSON producer and no live consumer.
`EntryLimit` / `EntryLimitType` — declared in the dungeon domain and never populated or enforced;
wire it or ignore it, don't add a third dead limit.

**Known landmines flagged elsewhere in `docs/`:** Colosseum lacks row-version locking and
idempotency keys; `LootService` still uses static `Random`; guild content is duplicated between JSON
and a provider class; `DungeonPreviewData.dailyEntries` exists client-side with no server field.
A raid feature should not inherit any of these.
