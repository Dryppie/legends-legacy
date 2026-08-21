# Multiplayer Boss Stagger Meter Analysis

## Executive conclusion

A shared Stagger meter is a good fit for LegendsLegacy's Tower and Raid boss content, provided it is implemented as an engine-owned combat mechanic rather than as Tower- or Raid-specific presentation logic.

The existing deterministic combat engine and checkpoint playback pipeline provide most of the required foundation. The main work is to add persistent runtime Stagger state to eligible bosses, define which ability effects contribute to it, serialize that state through combat checkpoints and compact playback bundles, expose contribution statistics, and render a boss-only meter in the shared combat UI.

The recommended first version is:

1. Only specifically configured Tower guardians and Raid Final Assault bosses have a Stagger meter.
2. Successful Stun and Freeze attempts against those bosses do not directly disable them. Instead, explicitly authored `StaggerPower` on the relevant control effect fills the meter.
3. Reaching the threshold causes a short, engine-owned Stagger state that blocks the boss from acting and can optionally increase damage taken.
4. The meter resets after the Stagger window, followed by a brief recovery period during which more Stagger cannot be applied.
5. Later breaks become harder through an authored threshold-growth rule.
6. Stagger contribution is recorded separately from damage so control-oriented characters receive visible credit.

This is a moderate cross-cutting feature, not merely a new progress bar. A production-ready implementation for both Tower and Raid is approximately **10–15 focused working days** for one developer, including engine work, playback compatibility, frontend work, tests, initial content authoring, and calibration. A restricted pilot for one boss could be completed in roughly **7–10 days**.

## Important scope distinction: simulated multiplayer versus live multiplayer

Tower and Raid battles currently run as deterministic server-side simulations and are then presented through stored checkpoint playback. Players choose a roster or loadout before the simulation; they do not submit time-sensitive commands during the fight.

Within that architecture, Stagger would provide:

- a reason to include control-oriented characters or abilities;
- a visible payoff for team composition;
- automatic damage or safety windows during playback;
- another boss-balancing axis besides health, damage, armor, and timers;
- meaningful contribution statistics for non-DPS roles.

It would **not** provide real-time coordination such as “save the interrupt,” “burst now,” or “stop attacking until the next mechanic.” The simulation has already finished before the player watches it.

If the desired feature is truly interactive multiplayer Stagger, the scope is much larger. It would require a server-authoritative live combat session, player command ingestion, synchronization and latency rules, reconnect behavior, anti-cheat protections, live state broadcasting, and a different failure/recovery model. That is a separate architecture project and should not be hidden inside the Stagger-meter estimate.

The remainder of this document assumes Stagger is a deterministic roster-and-loadout mechanic that is faithfully shown in playback.

## Current architecture and fit

### Shared combat engine

`FastCombatEngine` is the correct owner of the mechanic. It already owns:

- deterministic tick processing at ten ticks per second;
- combatant health, barrier, statuses, and standard conditions;
- action-blocking checks for Stun and Freeze;
- damage and condition application;
- combat events and statistics;
- periodic combat checkpoints used by playback.

Keeping Stagger here ensures the same rules apply in Tower and Raid, prevents service-level duplication, and keeps the combat result deterministic for a given seed and definition snapshot.

### Tower

Tower floors are data-driven through `TowerFloorDefinition`, and Tower combat already uses the shared engine and persists a compact playback artifact. A floor can therefore opt its guardian into Stagger through a new definition block.

Tower's current compact playback schema is version 2. Its per-frame entity state contains health and barrier but no boss-mechanic state. Supporting Stagger requires a new schema version and backward-compatible playback handling.

### Raids

Raid combat is divided into preparation lanes and a Final Assault:

1. Rearguard waves;
2. Vanguard Guardian, producing the existing `GuardianBreak` defense reduction;
3. Main Guard projection, producing `SignatureDisruption`;
4. Final Assault against the actual Raid boss.

The new Stagger mechanic should initially be enabled only for the Final Assault boss. The existing Guardian Break is a pre-boss preparation modifier and must remain a separate concept.

Raid definitions and active runs already snapshot their definitions and rules version. That is valuable for deterministic compatibility. The Raid compact playback schema is currently version 3 and also stores only health and barrier in per-frame entity state.

### Shared frontend

Tower and Raid focused playback both flow through shared combat state and the shared combat entity UI. The meter should therefore be implemented in the shared boss presentation and activated by entity metadata, rather than duplicated in Tower and Raid pages.

## Recommended gameplay contract

### Meter semantics

Use a meter that fills from `0` to `MaxStagger`.

- Eligible effects add Stagger.
- The bar fills toward a clearly marked threshold.
- At the threshold, the boss enters `Staggered` and the bar resets.
- During `Staggered` and the subsequent recovery period, further contribution is ignored rather than banked.
- Non-boss entities and bosses without a Stagger definition expose no meter.

Filling a meter is easier to understand and report as “+25 Stagger” than a hidden poise value that depletes.

### What should contribute

The first version should not derive Stagger directly from ordinary damage. Doing so would make the best DPS composition automatically the best Stagger composition and would fail to create a distinct control role.

Recommended contribution rules:

| Source                  | Version 1 behavior                                      | Reason                                                                   |
| ----------------------- | ------------------------------------------------------- | ------------------------------------------------------------------------ |
| Stun and Freeze effects | High, explicitly authored `StaggerPower`                | Converts otherwise binary boss control into predictable progress         |
| Other soft control      | No contribution initially                               | Slow, Weaken, Mark, and similar effects can keep their existing identity |
| Ordinary damage         | No contribution                                         | Prevents Stagger from becoming another DPS calculation                   |
| Selected heavy attacks  | Optional later, explicitly authored                     | Allows bruiser identities after the core system is calibrated            |
| Summons                 | Allowed when their effect is authored; credit the owner | Preserves build identity and correct participant attribution             |

The existing `IsHardCrowdControl` ability metadata is useful for cataloging and validation, but it is not sufficient by itself. Runtime condition application operates at the effect level, and different effects within an ability may need different values. Add an effect-level `StaggerPower` value and validate that it is only used on supported hostile control effects in version 1.

An eligible boss should intercept a successful Stun or Freeze application after its normal application roll and defenses have been resolved:

1. Resolve chance, Ward, and other application rules.
2. If the target has no Stagger profile, apply the condition normally.
3. If the target has a Stagger profile, do not apply direct Stun or Freeze.
4. Add the effect's authored Stagger contribution.
5. Trigger Stagger if the threshold is reached.

This makes boss control resistant without making control abilities useless. It also avoids indefinite direct Stun chains.

The exact interaction with `Unstoppable` must be made explicit. The recommended rule is that normal Unstoppable blocks Stagger contribution from control effects, while the Stagger recovery timer supplies its own contribution immunity. “Guaranteed application” should bypass normal Ward/Unstoppable checks only where it already does today; it should not bypass a boss's post-Stagger recovery lockout.

### Staggered state

Staggered should be a dedicated runtime boss state, not a synthetic Stun condition. This allows it to have separate rules, events, visuals, and immunity behavior.

Recommended initial effects:

- the boss cannot start actions for the configured duration;
- existing ability cooldowns continue ticking;
- the boss takes an optional authored percentage of additional damage;
- no new Stagger contribution is accepted;
- at the end, the boss enters a short recovery period;
- no cooldown reset or forced ability cast occurs in version 1.

Letting cooldowns continue keeps the implementation aligned with existing combat ticking. It may cause a ready boss ability to fire shortly after recovery; that behavior should be measured in calibration. A special post-Stagger ability delay can be introduced later if needed.

### Repeated breaks

A completely repeatable fixed threshold risks permanent boss suppression in control-heavy teams. Use authored diminishing returns:

```text
effectiveThreshold = participantScaledThreshold
                   * (1 + breakCount * thresholdGrowthPerBreak)
```

The definition may also cap the number of breaks. A threshold-growth rule is preferable to reducing the visible effect duration because the Stagger payoff remains readable when it occurs.

Continuous meter decay should be deferred unless calibration shows that Stagger is too guaranteed. In an automated simulation, decay is primarily another loadout check rather than a reaction challenge. Reset, recovery immunity, and rising thresholds provide simpler initial control. Decay can remain an optional future field without being part of the first content rollout.

### Participant scaling

The threshold should not be based on boss health or raw player damage. It should scale independently so health tuning does not silently retune control value.

Calculate the encounter threshold once from the number of non-summoned participants present at combat start:

```text
participantScaledThreshold = baseThreshold
                           * (participantCount / referenceParticipantCount) ^ participantExponent
                           * difficultyMultiplier
```

An exponent near `1.0` keeps break frequency stable as the group grows. A lower exponent gives larger groups a modest cooperative advantage. The exact value should be determined through Raid and Tower calibration rather than hard-coded into the engine.

Summons created after combat begins must not change the threshold.

## Proposed domain model

Add a reusable boss configuration owned by Core, for example:

```csharp
public sealed class BossStaggerDefinition
{
    public bool Enabled { get; set; }
    public int BaseThreshold { get; set; }
    public int ReferenceParticipantCount { get; set; } = 1;
    public double ParticipantExponent { get; set; } = 1.0;
    public int BreakDurationTicks { get; set; }
    public int RecoveryDurationTicks { get; set; }
    public int DamageTakenBonusPercent { get; set; }
    public int ThresholdGrowthPercentPerBreak { get; set; }
    public int? MaximumBreaks { get; set; }
}
```

Add this definition to:

- `TowerFloorDefinition` for an eligible guardian;
- `RaidBossCombatDefinition` for an eligible Final Assault boss.

Do not put it on every generic creature definition initially. Stagger is a multiplayer boss rule, and putting it on base creatures would widen the feature into all game modes before its behavior is established.

The runtime combatant needs an engine-only state similar to:

```csharp
public sealed class RuntimeStaggerState
{
    public int Current { get; set; }
    public int Threshold { get; set; }
    public int StaggeredRemainingTicks { get; set; }
    public int RecoveryRemainingTicks { get; set; }
    public int BreakCount { get; set; }
}
```

It should be absent for ordinary combatants. The state is initialized by the Tower/Raid resolver when it creates the configured boss runtime entity.

## Combat-engine changes

### Ability data and compilation

Add `StaggerPower` to `AbilityEffectSpec` and its compiled equivalent, clone/mutation paths, JSON validation, and progression-copy paths. The value should default to zero so existing abilities do not change behavior until intentionally authored.

Version 1 should permit it on hostile `ApplyCondition` effects for Stun and Freeze only. Validation should reject:

- negative values;
- Stagger power on self/allied targets;
- Stagger power on unsupported operations or conditions;
- an ability marked as hard control whose intended boss-control effect was accidentally left unclassified, if strict catalog validation is enabled.

Adding a general `ApplyStagger` effect operation can be considered later for heavy attacks and boss-specific mechanics. It is not necessary for the first control-conversion version.

### Runtime resolution

Extend condition resolution so Stun/Freeze applications against a stagger-enabled target route to a dedicated `ApplyStagger` method after ordinary application defenses succeed.

That method must:

- ignore zero contribution and ineligible targets;
- ignore contribution while Staggered or recovering;
- clamp contribution at the threshold for display and statistics;
- attribute summon contribution to the owning participant where the existing stats-source mechanism permits it;
- emit deterministic combat events;
- enter Stagger exactly once when multiple effects reach the threshold on the same tick;
- calculate the next threshold after recovery;
- preserve deterministic random-number consumption relative to the documented rule set.

`IsActionBlocked` should include the dedicated Staggered state. The normal combatant tick should decrement Stagger and recovery timers even while actions are blocked.

### Damage modification

If the authored Stagger window includes increased damage taken, apply it in the common damage resolution pipeline as a separately reported amplification source. Do not temporarily mutate the boss's base armor or resistance, because that would obscure telemetry and could interact incorrectly with Raid Guardian Break.

### Events and statistics

Add explicit event types such as:

- `StaggerApplied`;
- `StaggerBroken`;
- `StaggerRecovered`.

Extend `EntityStats`, `AbilityStats`, and `CombatStatsAccumulator` with at least:

- total Stagger contributed;
- Stagger breaks caused;
- optional wasted Stagger attempts during immunity;
- boss Stagger uptime for encounter-level calibration.

This is important for Raid contribution scoring. Raid participant reporting currently centers on damage, so a control-oriented build would otherwise help the group without receiving visible credit. The initial reward formula does not have to change, but the data must exist before deciding whether it should.

## Checkpoints, playback, and API contracts

### State shape

The shared entity checkpoint state needs enough information to reconstruct the meter:

- `Stagger` or `CurrentStagger`;
- `MaxStagger` or current effective threshold;
- `IsStaggered`;
- optionally `StaggeredRemainingTicks` if the UI shows a countdown.

For compact bundles, put stable values such as capability and maximum threshold in entity metadata where practical, and frame-varying values in frame state. Because the threshold can increase after a break, either the current threshold must remain frame state or a sparse threshold-change event must update metadata during playback.

Recommended compact representation:

```text
Entity metadata: supportsStagger
Frame state: currentStagger, maxStagger, isStaggered
```

Non-Stagger entities should omit these optional values or use zero without displaying a meter.

### Playback schema versions

The change requires:

- Tower compact playback schema `2 -> 3`;
- Raid compact playback schema `3 -> 4`.

Old persisted artifacts must remain playable. The API and Angular playback services should accept both the previous and new schema during the compatibility window, defaulting missing Stagger fields to “unsupported.” Do not rewrite historical artifacts.

Deploying an API that only emits the new schema before the frontend understands it would break playback, so API and frontend changes must be released together or the API must negotiate the bundle schema.

### Checkpoint timing

Current boss playback is checkpoint-driven, commonly at one-second intervals. A short Stagger window can begin between checkpoints. Merely adding fields to periodic frames could make the visual state appear late or, for a very short break, miss it entirely.

The preferred solution is to force a playback keyframe on `StaggerBroken` and `StaggerRecovered`, while retaining periodic checkpoints for ordinary state. An alternative is a sparse Stagger transition stream keyed by combat tick. Recording every combat tick would be simpler conceptually but unnecessarily increases artifact size.

The UI should treat Stagger as discrete jumps caused by effects, not interpolate it as if it were continuous damage.

### Stored data and migrations

Stagger state can live in the existing serialized combat result and compressed playback artifact. That does not require a database migration by itself.

A migration is only necessary if the implementation adds normalized queryable columns, such as Stagger contribution on `RaidLaneResult`, or adds Tower definition snapshot fields to `TowerAttempt`.

Raid already snapshots its definition JSON and hash. Ongoing Raid runs can therefore preserve the exact Stagger rules under which they were created, provided the definition and Raid rules version are updated correctly.

Tower deserves additional care: its definitions are data-driven, but its attempt/replay path does not have the same clearly established boss-definition snapshot guarantee. A deployment that changes Stagger tuning while attempts are queued could produce inconsistent expectations. The minimum safe rollout is to enable Stagger only for attempts created after the new rules become active. The stronger long-term solution is to snapshot or hash the relevant Tower encounter definition when an attempt is created; that stronger solution may require persistence changes.

## Tower-specific changes

1. Add optional Stagger configuration to `TowerFloorDefinition`.
2. Extend `JsonWorldTowerDefinitionProvider` validation.
3. Calculate the participant-scaled threshold from the locked Tower roster.
4. Attach the runtime profile only to the floor guardian, not reinforcements.
5. Extend Tower checkpoints, compact bundle DTOs, and bundle construction.
6. Bump the compact playback schema and retain version-2 decoding.
7. Add Stagger contribution and breaks to the Tower battle report.
8. Decide whether Tower attempt definition snapshotting is part of this feature or a follow-up integrity improvement.

## Raid-specific changes

1. Add optional Stagger configuration to `RaidBossCombatDefinition`.
2. Extend `JsonRaidBossDefinitionProvider` validation.
3. Preserve and intentionally scale the configuration in `RaidPlusDifficulty.Create`.
4. Bump `RaidRules.Version` because combat behavior changes.
5. Include the Stagger definition in the existing Raid definition snapshot and hash.
6. Attach the runtime profile only during Final Assault in `RaidCombatResolver`.
7. Keep Vanguard `GuardianBreak` and Final Assault `Stagger` separate in naming, logic, and reporting.
8. Extend Raid playback DTOs and `RaidPlaybackBundleBuilder`.
9. Bump the compact playback schema and retain version-3 decoding.
10. Add Stagger contribution to participant reports before considering changes to score or rewards.

## Frontend changes

The frontend work should be centralized in shared combat presentation:

1. Extend TypeScript entity/frame models with optional Stagger fields.
2. Update `TowerPlaybackService` and `RaidPlaybackService` to project new fields and default older bundles safely.
3. Update `CombatService.applyPlaybackFrame` and combat state so Stagger survives frame application.
4. Add a boss-only bar to `CombatEntityStatsComponent` or a small child component.
5. Show a distinct `STAGGERED` state during the break window.
6. Show contribution and breaks in the post-combat report where available.

Accessibility requirements:

- use `role="progressbar"` with `aria-valuemin`, `aria-valuemax`, and `aria-valuenow`;
- include a textual Staggered state rather than using color alone;
- keep sufficient contrast in empty, filling, full, and recovery states;
- avoid rapid flashing at the threshold;
- respect reduced-motion preferences for break animation.

## Configuration validation

Definition providers should reject invalid content during startup rather than allowing runtime surprises. At minimum:

- threshold must be positive when enabled;
- reference participant count must be positive;
- participant exponent must be within an agreed safe range;
- break and recovery durations must be positive and bounded;
- damage-taken bonus must be non-negative and capped;
- threshold growth must be non-negative and capped;
- maximum breaks, when present, must be positive;
- disabled definitions should not contain active tuning values unless intentionally permitted;
- Raid Plus generation must produce a valid profile at every supported depth.

## Balancing and content implications

### Target experience

Stagger should create a noticeable tactical payoff without becoming mandatory permanent suppression. Initial calibration targets should be expressed as outcomes, not fixed numbers:

- a balanced eligible group achieves one or two breaks in a normal successful encounter;
- a control-heavy group earns additional breaks but sacrifices enough damage or survival that the choice is meaningful;
- a group with no eligible control can still defeat the boss if otherwise strong enough;
- the boss is not action-blocked for an excessive share of the encounter;
- the damage bonus does not make every optimal build converge on maximum Stagger;
- Plus-depth scaling preserves approximately intended break frequency.

### Metrics to capture

For each calibration simulation, report:

- total contribution by participant and ability;
- break count and combat ticks of each break;
- time to first break;
- total Stagger uptime;
- damage dealt during Stagger windows;
- contribution rejected during recovery;
- win rate and fight duration compared with the same encounter without Stagger;
- outcome spread across no-control, balanced, and control-heavy loadouts.

The existing deterministic calibration tooling can run fixed seeds for repeatable comparison. Stagger should be evaluated across multiple seeds because the current hard-control application has chance-based behavior unless guaranteed.

### Essences and abilities

Unlike creature attribute scaling, Stagger is not purely an attribute mechanic. Its value is produced by authored ability effects, including Essence abilities where applicable. Attributes may influence how often an ability is used through cooldown or attack cadence, but version 1 should not multiply `StaggerPower` directly from Power or ordinary damage attributes.

That keeps Stagger independently tunable and avoids compounding existing attribute and Essence scaling. A future dedicated control-strength attribute should only be added if there is enough content to justify another player-facing stat.

## Test strategy

### Engine tests

- non-Stagger targets retain existing Stun/Freeze behavior;
- eligible boss converts authored control into Stagger and is not directly Stunned/Frozen;
- chance, Ward, Unstoppable, and guaranteed-application behavior matches the contract;
- exact-threshold and overflow contributions cause one break;
- Stagger blocks actions for the correct number of ticks;
- cooldowns and other effects continue ticking while Staggered;
- contribution is ignored during Stagger and recovery;
- threshold growth and maximum breaks work correctly;
- summon contribution is attributed correctly;
- damage-taken bonus is reported separately and reconciles with damage telemetry;
- fixed seeds produce identical results and checkpoints;
- ordinary dungeon, region, and PvP combat is unchanged.

### Definition and integration tests

- invalid Tower and Raid profiles fail catalog validation;
- Raid Plus carries and scales the profile deterministically;
- Raid definition snapshots deserialize old definitions without Stagger;
- Tower guardian and Raid Final Assault boss receive the state, while adds and preparation-lane enemies do not;
- battle reports include accurate contribution totals;
- forced keyframes exist at break and recovery transitions.

### Playback and frontend tests

- new Tower and Raid bundles serialize and deserialize the meter;
- previous Tower v2 and Raid v3 bundles still play without a meter;
- new playback never shows Stagger on an unrelated entity;
- meter jumps, break, recovery, and threshold growth render at the correct ticks;
- paused, resumed, skipped, and accelerated playback preserve state;
- accessibility attributes and text state are correct.

## Implementation phases and estimate

| Phase                         | Scope                                                                                       |       Estimate |
| ----------------------------- | ------------------------------------------------------------------------------------------- | -------------: |
| 0. Gameplay contract          | Finalize sources, immunity, break effect, repeat rules, and scaling formula                 |      0.5–1 day |
| 1. Engine and domain          | Definitions, runtime state, condition interception, events, statistics, deterministic tests |       2–4 days |
| 2. Tower and Raid integration | Providers, validators, resolver wiring, Raid Plus and rules version                         |       1–2 days |
| 3. Playback and API           | Checkpoint state, forced transition frames, DTOs, schema compatibility, reports             |       2–3 days |
| 4. Shared frontend            | Models, projection services, meter UI, reports, accessibility, tests                        |       1–2 days |
| 5. Authoring and calibration  | Initial abilities/bosses, fixed-seed comparisons, tuning and rollout checks                 |       2–4 days |
| **Production total**          | Both modes with compatibility and initial tuning                                            | **10–15 days** |

The estimate assumes no conversion to live interactive combat and no new dedicated Stagger attribute or skill tree. Tower definition snapshot persistence, normalized analytics columns, or extensive ability re-authoring would add time.

## Probable file impact

The implementation is expected to touch these areas:

### Core combat and definitions

- `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`
- `LL/src/Core/Domain/Models/Combat/SimpleCombatEntity.cs`
- `LL/src/Core/Domain/Models/Combat/EntityStats.cs`
- `LL/src/Core/Domain/Models/Combat/AbilityStats.cs`
- combat event type/model files
- `LL/src/Core/Domain/Models/WorldTower/WorldTowerDefinitions.cs`
- `LL/src/Core/Domain/Models/WorldTower/WorldTowerModels.cs`
- `LL/src/Core/Domain/Models/Raids/RaidDefinitions.cs`
- `LL/src/Core/Domain/Models/Raids/RaidModels.cs`

### Application contracts

- `LL/src/Core/Application/UseCases/CharacterActions/Dtos/Responses/CombatDtos/SimpleCombatEntityDto.cs`
- `LL/src/Core/Application/UseCases/WorldTower/Dtos/TowerPlaybackBundleDtos.cs`
- `LL/src/Core/Application/UseCases/Raids/Dtos/RaidDtos.cs`

### Engine and content services

- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityCompiler.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Stats/CombatStatsAggregator.cs`
- `LL/src/Infrastructure/Service/Services.LL/WorldTower/JsonWorldTowerDefinitionProvider.cs`
- `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Raids/JsonRaidBossDefinitionProvider.cs`
- `LL/src/Infrastructure/Service/Services.LL/Raids/RaidCombatResolver.cs`
- `LL/src/Infrastructure/Service/Services.LL/Raids/RaidPlaybackBundleBuilder.cs`

### Angular playback and UI

- `LL/src/Presentation/ll/src/app/core/services/client-side/combat/combat.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/client-side/combat/tower-playback.service.ts`
- `LL/src/Presentation/ll/src/app/core/services/client-side/combat/raid-playback.service.ts`
- `LL/src/Presentation/ll/src/app/shared/components/combat/combat-entity-stats/*`
- Tower/Raid playback models and associated specifications

### Content

- selected Tower floor definitions;
- selected Raid boss tier definitions;
- eligible control ability definitions with explicit Stagger contribution.

## Risks and mitigations

| Risk                                              | Consequence                                                 | Mitigation                                                                      |
| ------------------------------------------------- | ----------------------------------------------------------- | ------------------------------------------------------------------------------- |
| Stagger is derived from all damage                | DPS remains the only optimal role                           | Use explicit control-effect contribution                                        |
| Direct boss Stun remains active                   | Control chains can trivialize bosses                        | Convert eligible hard control into meter contribution                           |
| Fixed repeat threshold                            | Permanent suppression in control-heavy teams                | Recovery immunity, threshold growth, optional break cap                         |
| Periodic frames miss short breaks                 | Meter and boss behavior look inconsistent                   | Force keyframes on break/recovery transitions                                   |
| Old playback schemas are rejected                 | Historical Tower/Raid replays break                         | Decode both old and new schemas during compatibility window                     |
| Raid Guardian Break and Stagger share terminology | Players cannot distinguish preparation and combat mechanics | Reserve `GuardianBreak` for the lane modifier and `Stagger` for live boss state |
| Support players receive no credit                 | Mechanic feels unrewarding despite helping the group        | Add per-entity and per-ability Stagger telemetry before reward changes          |
| Content changes during queued Tower attempts      | Replays no longer match player expectations                 | Gate by attempt creation time; later snapshot Tower definitions                 |
| Stagger becomes mandatory                         | Encounter composition narrows                               | Keep no-control kills viable and calibrate opportunity cost                     |

## Recommended rollout

1. Approve the gameplay contract before adding UI fields.
2. Implement and test the engine state behind disabled content configuration.
3. Add playback schema compatibility and shared UI support.
4. Enable one late Tower guardian as the first controlled pilot.
5. Run fixed-seed calibration against no-control, balanced, and control-heavy rosters.
6. Enable one Raid Final Assault boss after Tower playback and reporting are stable.
7. Review telemetry before changing Raid score or rewards.
8. Expand to additional bosses only after contribution ranges and break uptime are understood.

## Final recommendation

Proceed with Stagger as a shared deterministic boss mechanic. It is well aligned with the current engine and can add genuine composition depth, especially by giving hard-control abilities value against bosses without allowing direct control chains.

The implementation should begin with the engine contract and telemetry, not the visual bar. The meter is only trustworthy if it represents authoritative state that is stored in checkpoints, survives replay, remains compatible with old artifacts, and can be balanced independently from damage and health scaling.

Do not treat this work as a step toward live interactive Raid combat unless that larger architecture change is explicitly chosen and estimated separately.
