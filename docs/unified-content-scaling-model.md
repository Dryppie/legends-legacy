# LegendsLegacy Unified Content Scaling Model

## 1. What Player Progression Currently Implies

Player power is produced by several distinct numerical layers.

Character levels provide linear raw growth:

- Power: `10 + 0.25 × (level − 1)`
- Health: `140 + 20 × (level − 1)`
- Other base combat attributes generally do not increase with level.

Across the planned level range, this is significant: level 1 to approximately level 495 takes base Power from `10` to `133.5` and base Health from `140` to `10,020`.

Equipment provides the stronger structured progression. Its tier budget is geometric:

```text
Budget(tier) = 100 × 1.353057^(tier − 1)
```

Tier 10 therefore has `15.2×` Tier 1's budget. The canonical progression associates one equipment tier with each Region and five character levels with each Area.

Equipment quality, blueprints, and tempering modify that budget:

- Quality ranges from `0.90×` to `1.12×`.
- Normal dungeon blueprints add `20%` bonus budget; current raid blueprints add `25%`.
- Base rolls vary by approximately ±5%.
- Tempering improvements derive from `2%` of the appropriate tier budget per rarity improvement.
- Potential determines how many tempering attempts are possible; it is not itself a combat multiplier.

Crucially, different equipment stats use the tier budget differently:

- Power, Health, and regeneration are flat values and grow with the tier budget.
- Armor and Resistance are progression-normalized ratings. On-tier equipment maintains a broadly similar mitigation profile rather than granting `15.2×` mitigation.
- Crit, attack speed, cooldown reduction, life steal, damage reduction, healing power, dodge, block, and penetration are direct percentages whose cost increases with tier. A comparable on-tier build therefore receives roughly similar percentage values at every tier.

This is already strong evidence against one literal multiplier for every stat.

Offensive output is approximately:

```text
Basic attacks:
(1 + 0.5 × Power)
× weapon damage multiplier
× attack rate
× damage-dealt modifiers
× expected critical multiplier
× target mitigation

Abilities:
BaseValue + Power × coefficient
then crit, damage modifiers, mitigation, and cooldown frequency
```

There are no separate Physical Damage and Magical Damage attributes. Both use Power; damage type determines whether Armor or Resistance and the corresponding penetration apply. There is also no Accuracy stat. Dodge is an independent chance against direct melee/ranged attacks.

Defensive strength compounds several layers:

```text
Health
÷ (1 − Armor/Resistance mitigation)
÷ (1 − general damage reduction)
÷ expected block/avoidance effects
+ barriers
+ healing
+ regeneration
+ life-steal sustain
```

Healing commonly scales from Power, then Healing Power, crit, cooldown frequency, and healing-received modifiers. Life steal scales from actual outgoing damage and then receives healing modifiers. Offensive investment can therefore become defensive sustain.

Essences currently contribute abilities and build interactions rather than permanent raw attributes:

- The 63 current definitions have no active attribute bonuses or implemented Evolution stat/ability modifiers.
- Essence level primarily gates Ascension.
- Ascension can add up to `36%` damage scaling, `30%` healing/barrier scaling, and `15%` cooldown reduction.
- A fully ascended damaging active can therefore gain roughly `1.36 / 0.85 ≈ 1.60×` throughput before other synergies.
- Essence slots unlock every ten character levels, from one slot to ten by level 90. This is a large early-game stepwise increase in available mechanics and passive interactions.
- Codex and Soulstone bonuses are currently progression/economy bonuses rather than direct combat stats.

Doctrines are not presently implemented in the repository.

The natural player curve is therefore **piecewise and layered**:

```text
Linear character growth
+ geometric Region-tier equipment growth
+ bounded secondary-stat progression
+ discrete Essence slot/Ascension milestones
+ multiplicative build interactions
```

Region boundaries do create natural player milestones because each Region corresponds to a new equipment tier, whose budget rises about `35.3%`. However, the current enemy curve does not add a special Region jump: Region 2's starting anchors are exactly the next smooth Area step after Region 1. Floor 10 also gates Region 2, making the boundary a progression milestone even though the underlying enemy numbers remain smooth.

## 2. Recommended Scaling Structure

I recommend **Option C for progression structure feeding Option B for individual stats**:

```text
Region + Area position
        ↓
Shared progression foundation
        ↓
Related Health / pressure / durability / sustain curves
        ↓
Content modifier
        ↓
Archetype distribution
        ↓
Explicit optional tuning
```

Represent position as:

```text
region = floor((position − 1) / 10) + 1
area   = ((position − 1) mod 10) + 1
```

Use Region and Area explicitly rather than reducing everything permanently to an opaque level number. This preserves the equipment-tier milestones while still giving every Area a global ordering.

The model should define separate numerical targets:

- `Durability(position)`: total intended toughness.
- `Pressure(position)`: intended damage/healing pressure per unit time.
- `Health(position)`: the Health portion of durability.
- `Mitigation(position)`: the Armor/Resistance portion of durability.
- `Sustain(position)`: regeneration or healing budget.
- `AbilityMagnitude(position)`: only where an ability does not already derive its magnitude from Power.

Then:

```text
Final stat
= baseline(position)
× content modifier
× archetype modifier
× manual tuning
```

Do not independently grow Health and mitigation without accounting for their compound result. Define target durability first, then distribute it:

```text
Effective durability ≈ Health / (1 − mitigation)
```

Likewise, define pressure first, then distribute it between Power, attack speed, crit, cooldowns, and ability cadence. Otherwise a `10%` increase to every offensive dimension becomes much more than `10%` actual output.

Recommended stat behavior:

- Health and Power use related, unbounded curves with independently chosen lifetime ratios.
- Armor and Resistance target bounded effective mitigation, not an exponentially growing raw value.
- Percentage secondaries should be mostly archetype/build properties, with caps and limited progression.
- Regeneration should track encounter pressure or Health, not inherit a blind universal multiplier.
- Power-scaled abilities should normally keep authored coefficients. Scaling Power already scales them.
- A separate ability-value multiplier should affect flat values, summons, or effects that otherwise do not scale. Multiplying both Power and its coefficient by the full progression curve would double-compound ability output.
- Percentage buffs, debuffs, status stacks, and control durations should usually remain mechanically authored rather than scaling with progression.

This is not a Combat Power system. It is a small family of predictable baselines.

## 3. Idle Combat

Idle combat already closely matches the desired structure:

```text
Area baseline
→ archetype
→ damage profile
→ defense profile
→ stat overrides
```

The existing archetypes produce Tank, Bruiser, DPS, Support, and Balanced distributions. The current 80 creature definitions use those archetypes and none currently use individual stat overrides, so manual tuning is already exceptional.

The unified version should make the Area responsible for all progression. A creature should contain only:

- Archetype distribution.
- Physical/magical defensive bias.
- Ability kit and damage types.
- Optional explicit per-stat tuning, defaulting to `1.00`.

One current limitation should not guide the future model: attack speed, penetration, regeneration, and soft defenses often multiply a zero creature baseline and therefore remain zero. Creature crit values are also authored fractionally but truncated into integer percentage-point attributes. Those secondary curves are not reliable evidence for long-term scaling.

## 4. Dungeons

Dungeons should be **fixed progression checkpoints**, not “whatever the player's current Area is.”

Each dungeon difficulty should declare a position on the global curve:

```text
Dungeon baseline
= GlobalBaseline(anchor position)
× dungeon difficulty modifier
× room/encounter modifier
```

A sensible default is:

- Normal: late or end-of-hosting-Region baseline.
- Heroic: next equipment-tier milestone.
- Mythic: the following milestone.

That matches their current role: long, repeatable, sigil-gated expeditions awarding Monster Cores, blueprints, materials, mastery, and increased encounter rewards.

Currently, dungeon enemies start from a generic Area-1 baseline and receive manually authored `3.5×`, roughly `6×`, or roughly `8.2×` multipliers across Health, Power, defenses, penetration, and regeneration. Replacing those isolated multipliers with explicit global progression anchors would preserve the three difficulties while making future dungeons predictable.

## 5. World Tower

World Tower should use the same curve family but its own mapping from Floor to progression position:

```text
Floor
→ progression anchor
→ Tower baseline
→ participant scaling
→ boss archetype/Essence kit
→ optional boss tuning
```

The existing engine supports this separation well: Guardians are ordinary combat entities with authored native abilities, and those abilities already use the same Power, healing, barrier, status, summon, and mitigation systems as player Essences.

Current Floors are numerically hand-balanced and highly non-monotonic: Health ranges from `8×` to `150×`, offense from `35×` to `140×`, and participant requirements range from 3 to 15. That combines Floor strength, participant count, and ability-kit compensation into one set of manual values.

Recommended behavior:

- Floor determines monotonically increasing Health, pressure, and durability baselines.
- Boss kit determines targeting, AoE, healing punishment, adds, control, and composition requirements.
- Manual Health/Power/defense/Essence tuning defaults to `1.00`.
- Floor-scaled Power automatically scales most current damage and healing abilities.
- A separate Essence-value curve applies only to flat values, summons, and other non-Power-scaled magnitudes.
- Percentage mechanics and control durations should generally not grow with Floor.

Participant scaling should remain separate:

```text
Boss baseline
= Floor baseline × ParticipantScale(players)
```

Health may scale near-linearly or sublinearly with expected group throughput. Boss damage should scale much more mildly, or remain fixed, because adding players does not increase each player's survivability. Mechanics, target counts, and add counts should remain encounter-authored.

The existing exact `RequiredSlots` rule means current Floors effectively bake participant count into their manual numbers. Separating it will make comparisons between Floors meaningful.

## 6. Candidate Curves

The table below shows illustrative progression foundations. It applies equally to Areas 1–100 or Floors 1–100 after choosing the content's starting anchor. These are not final stat ratios.

| Position | Smooth geometric | Region + Area | Back-loaded power |
|---:|---:|---:|---:|
| 1 | 1.00× | 1.00× | 1.00× |
| 10 | 1.29× | 1.20× | 1.32× |
| 25 | 1.96× | 1.94× | 2.55× |
| 50 | 3.94× | 3.84× | 5.87× |
| 75 | 7.94× | 8.33× | 10.42× |
| 100 | 16.00× | 16.48× | 16.00× |

### Candidate 1 — Lifetime-anchored geometric

```text
x = (position − 1) / 99
G(position) = R^x
```

Illustration uses `R = 16`.

Why it fits: simple, smooth, predictable, and close to the existing `15.2×` Tier 1-to-10 equipment budget.

Main downside: Region boundaries have no numerical identity, even though equipment progression is tier-based.

### Candidate 2 — Region tier plus Area growth

```text
withinSteps = 9 × (region − 1) + (area − 1)

G(region, area)
= AreaGrowth^withinSteps
× RegionJump^(region − 1)
```

The table illustrates `AreaGrowth = 1.02` and `RegionJump = 1.12`, producing approximately `16.5×` lifetime growth.

Why it fits: it expresses the game's actual two-layer structure, keeps Area progression smooth, and makes Region milestones visible without huge discontinuities.

Main downside: the Region jump must be coordinated with equipment acquisition so the first Area of a Region does not feel like an arbitrary wall.

### Candidate 3 — End-anchored power curve

```text
x = (position − 1) / 99
G(position) = 1 + (R − 1) × x^k
```

The table uses `R = 16` and `k = 1.6`.

Why it fits: produces readable early numbers and leaves more room for large late-game progression.

Main downside: it is back-loaded and does not naturally match the existing geometric equipment tiers. Late content receives much larger absolute jumps.

For comparison, naïvely extending the current Region-local rates across 99 transitions would produce approximately:

```text
Health:  13,400,000×
Power:       94,000×
Defense:    139,000×
```

That would immediately cap mitigation and disconnect content from the roughly `15.2×` equipment-budget progression. The current rates should not be extrapolated across all 100 Areas.

## 7. Recommendation

Choose **Candidate 2: Region tier plus Area growth**, used as a shared progression foundation that feeds several related stat curves.

It best matches LegendsLegacy because:

- Equipment already progresses geometrically by Region.
- Character levels provide gradual Area-to-Area growth.
- Essence slots and Ascensions add milestone progression.
- Region boundaries already matter through equipment, quests, and World Tower unlocks.
- The creature system already supports baseline → archetype → override.
- Dungeons and Tower Guardians already use the common combat and ability engine.

I would target a headline lifetime foundation around the existing equipment scale—roughly `15–20×` from R1A1 to R10A10—then give Health, Power, sustain, and mitigation their own transformations. That does not mean every final stat ends at `16×`; character Health alone grows much faster than the equipment budget, while mitigation and percentage secondaries should remain bounded.

The universal-curve idea is sound. Its boundary is this:

> Universal progression should determine numerical budgets, not erase stat interactions or encounter mechanics.

Use one progression foundation, several related stat curves, content/archetype distribution, and small visible exceptions. Do not universally multiply every stat, every Power coefficient, and every secondary mechanic.

## 8. Implemented Initial Calibration

The initial implementation uses the recommended Region-plus-Area foundation:

```text
AreaGrowth = 1.02
RegionJump = 1.12
AreasPerRegion = 10
```

The shared foundation feeds distinct creature curves:

- Health: `1.60 × G(position)^1.35`
- Power/pressure: `1.85 × G(position)`
- Armor and Resistance ratings: `1.15 × G(position)^0.45`
- Regeneration: the geometric mean of the Health and pressure multipliers
- Attack speed, penetration, soft defense, crit chance, and crit damage: bounded additive percentage-point budgets rather than multipliers of zero-valued baselines

Dungeon tiers are anchored to positions 10, 20, and 30. Their content-pressure modifiers apply after the shared baseline, while individual dungeon families retain small authored tuning values near `1.00`.

Released World Tower Floors 1–10 map to progression positions 1–10. Participant scaling is separate from the Floor curve: Health scales sublinearly with participant count, offense scales mildly, and durability scales more slowly. Existing boss-specific differences are retained as explicit tuning values instead of being hidden in the Floor baseline.

These coefficients are an initial deterministic calibration. They should be adjusted from combat telemetry—especially win rate, encounter duration, and damage intake—without changing the model's structure.

## 9. Essence-Aware Calibration Without Runtime Adaptation

Creature baselines should **not** read the player's equipped Essences. Doing so would make upgrades feel self-cancelling, make encounters difficult to reproduce, and punish players for improving or experimenting with a build.

Essences should still be included in balance, but as offline player-power envelopes:

```text
Fixed content anchor + fixed player attributes
                         ↓
       attribute-only / minimum / expected / optimized loadouts
                         ↓
 deterministic combat samples across fixed random seeds
                         ↓
 damage, healing, barrier, duration, and outcome distributions
```

The implementation now enforces two catalog guardrails:

- Damage, healing, and barrier effects must have a positive attribute, event, condition, status, or owned-summon magnitude source. Fixed `baseValue` magnitude remains forbidden.
- Every summon inherits Health from owner MaxHealth, and every basic-attacking summon inherits Power from owner Power. This prevents creature summons from becoming flat-value outliers as content progresses.

`EssenceProgressionCalibrationRunner` provides the offline harness. Each scenario explicitly owns:

- Progression position and character level.
- The fixed player and target attribute snapshots.
- Named loadout envelopes with exact Essence IDs and Ascension tiers.
- Fixed random seeds and simulation duration.

It validates loadouts against the slots unlocked at the scenario's character level, applies the real Ascension scaler to the real catalog abilities, executes the normal combat engine, and reports average damage, healing, barrier generation, duration, and most common outcome. An empty `attributes-only` envelope isolates the uplift contributed by Essences.

The first automated matrix exercises levels 1, 10, 30, and 90 with attribute-only, minimum, expected, and optimized offensive envelopes. Those loadouts are diagnostic fixtures, not final claims about typical players. Production calibration should replace the expected envelope with telemetry-derived loadout occupancy and Ascension percentiles, and should add separate offense, sustain, control, and summon archetypes.

Recommended tuning workflow:

1. Freeze the Region/Area anchor and representative attribute snapshot.
2. Measure attribute-only, minimum, expected, and optimized Essence envelopes.
3. Tune ordinary content against the expected envelope while ensuring the minimum envelope can still progress.
4. Use optimized results to define challenge-content headroom, not ordinary enemy adaptation.
5. Add explicit kit compensation only when a creature, summon, or boss remains an outlier after attribute scaling.
6. Re-run the matrix whenever an Essence coefficient, Ascension rule, slot cadence, equipment curve, or creature baseline changes.

This keeps the live scaling model understandable:

> Enemies scale from content position; balance targets account for Essences offline.

## 10. Implemented Player Attribute Snapshots

Player attribute envelopes are now authored in `Data/progression/player-progression-snapshots.json` and generated by `PlayerProgressionSnapshotFactory`. The manifest stores assumptions rather than copied final attributes, so changes to level growth, equipment budgets, rating conversion, percentage caps, or Combat Rating automatically flow into the report.

The initial anchor set covers positions 1, 5, 10, 11, 15, 20, 21, 30, 41, 50, 91, and 100. These represent campaign start, selected Region midpoints, and the entry/completion boundaries of Regions 1, 2, 3, 5, and 10. Character levels follow the canonical five-level Area cadence: position 1 starts at level 1; later positions use `position × 5 − 5`.

The equipment model uses eight combat-loadout budget units:

- Six fixed armor/accessory slots.
- Two hand-slot budget units, whether represented by a two-handed weapon or two one-handed/off-hand items.
- The Tool slot is excluded because it is not part of the maximum combat-equipment budget.

At every anchor, the factory independently combines three gear-attainment envelopes with three attribute allocations.

| Gear envelope | Filled budget | Quality | Roll | Tempering budget | Current-tier share: entry → completion |
|---|---:|---|---:|---:|---:|
| Minimum | 75% | Crude | 0.95× | 0% | 0% → 40% |
| Expected | 100% | Standard | 1.00× | 5% | 15% → 85% |
| Optimized | 100% | Exceptional | 1.05× | 20% | 65% → 100% |

Region 1 always uses Tier 1 because no previous equipment tier exists. At later Region entries, the mixed budget models players carrying equipment from the previous Region while beginning to acquire the newly legal tier. The existing rules make Tier 2 legal at level 50 even though the expected-tier projection helper changes at level 51; snapshots follow the canonical Region tier and verify that the tier is legal at the anchor level.

The allocation profiles deliberately form a separate axis:

- Offensive invests in Power, crit, both penetrations, attack speed, and cooldown reduction.
- Balanced mixes Power and Health with typed defenses, moderate offense, sustain, and general damage reduction.
- Defensive/support emphasizes Health, typed defenses, damage reduction, avoidance, healing, regeneration, cooldown reduction, and status resistance.

For each combination, the factory emits:

- Every content-facing attribute, including zero values.
- Raw/materialized equipment points.
- Canonical Combat Rating and its category breakdown.
- An unmitigated basic-attack pressure index.
- Physical and magical effective-durability indices from Health, typed mitigation, and general damage reduction.

The output contains no Essence attributes, abilities, Ascension, Evolution, Codex, or Soulstone contribution. Those systems are layered onto a fixed snapshot by the separate Essence calibration runner.

These values are version-one design assumptions, not telemetry findings. In particular, tempering is represented as a total additional equipment-budget percentage rather than simulated attempt-by-attempt rolls. The assumptions should remain visible and replaceable until production distributions are available.

## 11. Implemented Essence Build Matrix

`Data/progression/essence-calibration-loadouts.json` now joins the generated player snapshots to representative Essence loadouts without merging their progression assumptions. A scenario is identified by:

```text
snapshot anchor
× gear envelope
× attribute allocation / build family
```

Each scenario then runs four independent Essence envelopes:

| Essence envelope | Unlocked slots filled | Ascension | Evolution |
|---|---:|---:|---|
| Attributes only | 0% | None | No |
| Minimum | 40% | Tier 0 | No |
| Expected | 70% | Tier 1 | No |
| Optimized | 100% | Tier 3 | No |

Slot counts are rounded up for non-empty envelopes and capped by the actual character-level unlock service. A level-1 character therefore receives one Essence in every non-empty envelope, while a character with ten unlocked slots receives four, seven, or ten. Changing the gear envelope never changes these Essence counts or Ascension tiers.

The first representative families are:

- Offensive, paired with the offensive attribute allocation.
- Sustain, paired with the defensive/support allocation.
- Summon, paired with the balanced allocation. Because the current catalog has only a small number of direct summon Essences, the remaining slots use complementary sustain and summon-interaction kits.
- Control, paired with the balanced allocation.

The automated matrix selects campaign start, the Region 1 midpoint, and the completion anchors for Regions 1, 2, 3, 5, and 10. It crosses those seven anchors with all three gear envelopes and four build families, producing 84 scenarios and 336 aggregated Essence-envelope results. Every result averages three fixed random seeds.

This stage still uses a calibration target rather than a real content encounter. The target has very high Health, no avoidance, and Power equal to 10% of the generated player Power; the player begins at 70% Health. This allows damage, healing, barriers, and survival resources to be observed during the same fixed 300-tick sample.

The report now includes:

- Damage uplift relative to the attribute-only result on the identical snapshot.
- Healing, barrier, and remaining Health/barrier deltas.
- Ability uses per minute.
- Summon and stun counts.
- Duration and outcome.
- The exact snapshot, gear envelope, allocation, build family, and Essence envelope identifiers.

Evolution is explicitly disabled in the version-one envelopes. Current definitions contain no authored Evolution attribute or ability modifiers, so enabling it would imply power that does not exist. The runner validates Evolution tier legality and refuses evolved modifier content that would require the full combat-entity path rather than silently ignoring it.

This matrix measures isolated Essence uplift. It does not yet establish content difficulty; that begins when these same scenarios are executed against real Idle, Dungeon, and World Tower encounters.

## 12. Implemented Authored-Encounter Calibration

`Data/progression/encounter-calibration-samples.json` defines a small, reviewable sample of released content. `AuthoredEncounterCalibrationFactory` resolves every reference back to the source content instead of copying final enemy attributes into a balance fixture:

- Idle samples reference an Area and creature IDs that must belong to that Area. The initial samples cover authored one-, two-, and three-creature spawn pressure.
- Dungeon samples reference a family, difficulty, and room template. The factory resolves the template roster, anchors the shared creature curve to the Dungeon tier, and then applies the live Dungeon content-strength multiplier.
- Tower samples reference a released Floor. The factory resolves its guardian, uses the Floor progression position and guardian tuning, applies the live participant-count formulas, and attaches the Floor's authored Stagger profile. Each sample also references authored five-slot party patterns—balanced, offense-heavy, sustain-heavy, control-oriented, and summon-oriented—which repeat to fill the Floor's exact 5-, 10-, or 15-player requirement. Released Floor 7 now requires one complete five-player party rather than a three-player partial party.
- Raid samples reference a boss and Tier or Raid Plus level. The factory applies the live Final Assault attribute multipliers, authored overtime, participant count, Stagger profile, and fully completed Guardian Break and Signature Disruption preparation. The five compositions have explicit three-player overrides so a small Raid party preserves its intended identity instead of truncating a larger pattern. Random variants and Rearguard survivors are excluded from this first reproducible ideal-preparation baseline.

Creature archetypes, damage and defense profiles, stat overrides, and native ability profiles are loaded from the normal creature and combat catalogs. All simulations use the normal compiled statuses, summons, abilities, Ascension rules, and deterministic combat engine.

The version-seven catalog contains 18 encounters:

| Content | Samples | Representative pressure |
|---|---:|---|
| Idle | 3 | Single, double, and triple spawns |
| Dungeon | 4 | Room and boss templates at Tiers 1 and 2 |
| World Tower | 4 | Floor 1 guardian, Floor 5 Warden, Floor 7 guardian, Floor 10 Sovereign |
| Raid | 7 | Two Raid families across released Tiers and Raid Plus +3 |

Solo Idle and Dungeon encounters cross the matching anchor's three gear envelopes, four build families, and four Essence envelopes. Tower and Raid encounters instead cross three gear envelopes, five mixed-party compositions, and four Essence envelopes. Together these produce 996 aggregated results and 2,988 seeded combat samples. Every multiplayer composition is one aggregated encounter result. Five-slot party patterns repeat for larger rosters, while every three-player composition has an explicit roster: balanced is two offense plus one sustain, offense-heavy is three offense, sustain-heavy is one offense plus two sustain, control-oriented is one offense plus two control, and summon-oriented is one offense plus two summon.

Each result reports:

- Win and timeout rates.
- Encounter duration and friendly damage taken.
- Remaining team Health plus barrier as a percentage of maximum Health.
- Healing, barrier generation, ability cadence, summons, and stuns.
- Enemy ability uses, which confirms that authored creature kits participate in the sample.
- For Stagger-enabled bosses: total accepted contribution, break count, first-break timing, break-window uptime, damage dealt during the break window, and break-cap rate.
- Failure diagnostics: friendly death rate and first-death tick, enemy Health remaining on draws, friendly and enemy basic-versus-ability damage, regeneration overheal, unused barrier, and the top damaging ability on each side.
- Exact content, anchor, gear, allocation, build-family or party-composition, and Essence-envelope identifiers.

The exception report evaluates the expected-gear/expected-Essence cohort against provisional content-specific bands. Assessment is role-aware: Idle and solo Dungeon bands use offensive, control, and summon families for completion and build-spread checks. The pure sustain family still runs against every solo encounter, but its outcome is reported in a separate observational section because its intended success criteria belong to multiplayer support play.

Each multiplayer sample now authors the strategic intent of every composition:

- `Expected` compositions use the complete encounter band, including completion, pacing, survival, and Stagger targets.
- `Alternative` compositions must remain viable, with at least 20% wins and at most 60% timeouts, but may be slower or less reliable than the intended answer.
- `Countered` compositions are expected to struggle and generate `UnexpectedSuccess` when their win rate exceeds 35%.
- `Challenge` compositions represent brute-force or off-strategy clears and generate `UnexpectedSuccess` above 65% wins.
- `Observational` compositions retain telemetry without producing exceptions.

Build-spread checks compare only compositions authored as `Expected`, so a deliberate counter no longer appears as accidental build sensitivity.

The assessed cohort therefore contains 76 results plus seven observational solo sustain results. Role eligibility removes 25 false solo sustain and build-spread exceptions without changing any combat simulation. Authored multiplayer intent removes another 21 false exceptions without changing combat, reducing the deterministic baseline from 133 to 112 review signals.

The initial Tower intent matrix is:

| Floor | Expected | Alternative | Countered | Challenge |
|---|---|---|---|---|
| 1 | All five compositions | — | — | — |
| 5 | Control-oriented | Balanced, summon-oriented | Sustain-heavy | Offense-heavy |
| 7 | Control-oriented | Offense-heavy, summon-oriented | Sustain-heavy | Balanced |
| 10 | Balanced, control-oriented | — | Sustain-heavy | Offense-heavy, summon-oriented |

Raids retain all five compositions as `Expected` until encounter-specific Raid identities are authored.

Multiplayer support is evaluated comparatively rather than by personal damage. The manifest identifies `balanced` as the baseline composition and `sustain-heavy` as the comparison composition, plus authored thresholds for meaningful death-rate reduction, survival-resource gain, first-death delay, and pacing cost. Every multiplayer result records its actual sustain-member count, effective ability healing, effective Health regeneration, barrier consumption, and regeneration waste.

The report pairs both compositions on the same encounter, gear envelope, Essence envelope, and deterministic seeds. It emits 132 machine-readable comparisons across all envelopes and eleven expected-cohort rows. Each comparison reports changes in completion, death rate, first-death timing, survival resources, duration, effective healing, effective regeneration, consumed barrier, regeneration waste, and friendly damage. A loss of completion rate takes precedence over secondary survival improvements and is classified as `CompletionRegressed`.

The initial expected-cohort findings are:

- Tower Floor 1 benefits from a second sustain member: friendly-death incidence falls by 33.3 percentage points, first death moves from tick 320 to 435, and survival resources rise by 28.8 percentage points for a 15% duration cost.
- Tower Floor 5 no longer universally wipes after reducing its health coefficient from 0.82 to 0.70 and offense coefficient from 2.91 to 1.25. In the 30-seed intent run, the expected control composition and balanced alternative complete reliably, summon-oriented completes 83%, and the countered sustain-heavy composition times out. Offense-heavy wins 100% and is correctly reported as an unexpectedly successful challenge route.
- Five-player Floor 7 is no longer a trivial 100% win for every composition after raising its health coefficient from 0.37 to 0.65 and offense coefficient from 0.95 to 1.50. In the 30-seed intent run, the expected control composition and summon alternative win reliably, offense-heavy wins 47%, and the countered sustain-heavy composition wins 60%. Balanced and sustain-heavy therefore remain unexpected shortcuts for the authored identity.
- Tower Floor 10 gains 34.2 percentage points of survival resources and delays first death from tick 500 to 900, but still cannot win; support is helpful but insufficient against the encounter pressure.
- Hives' Abyss +3 already wins without a friendly death, so replacing one offensive member with a second sustain member is unnecessary for completion.
- At Hives' Abyss Tier 1, a second sustain member delays first death from approximately tick 1,039 to 2,388, but both parties still lose; support is helpful but insufficient.
- Hives' Abyss Tier 2 remains insufficient with an extra sustain member. Tier 3 delays first death from tick 1,201 to 1,555 but remains unable to complete.
- All assessed Sanguine Horror baselines already win without a friendly death. Extra sustain raises end-of-fight resources or duration but is classified as unnecessary for completion.

Across the eleven expected-cohort comparisons, the classifications are two completion-regressed, one effective, three helpful-but-insufficient, one insufficient, and four unnecessary-for-completion. No comparison remains untestable because of an identical sustain-member count.

These classifications are diagnostic labels, not balance exceptions. They identify whether more support changes the party outcome before boss pressure or support kits are tuned.

For expected rows, the report identifies win rate, timeout rate, duration, survival, or Stagger outside the complete band. Alternatives are checked for unexpected failure; countered and challenge routes are checked for unexpected success. Build sensitivity compares only expected families or compositions. These are diagnostics, not test failures and not runtime adaptation: an exception is evidence to review content coefficients, mechanics, or a specific kit, never permission to scale an enemy from the current player's loadout.

The current runner intentionally measures isolated encounters. It does not yet model persistent Dungeon Vigor, damage carried between rooms, Tower preparation contributions, partial Raid preparation, Rearguard survivors, random Raid variants, matchmaking variance, or production player behavior. Those should be added as separate scenario dimensions after the reproducible single-encounter bands are reviewed and accepted.

## 13. Repeatable Calibration Reports

The offline matrix can be executed from the repository root:

```powershell
./build/run-encounter-calibration.ps1
```

The command builds the dedicated `LL/tools/BalanceCalibration` console project and writes two deterministic artifacts beneath the ignored `artifacts/balance-calibration` directory:

- `encounter-calibration-report.json` contains the complete machine-readable results, exceptions, catalog metadata, and summary counts.
- `encounter-calibration-report.md` contains the expected-cohort encounter overview, prioritized exception table, failure diagnostics, and review guidance.

Balance exceptions do not cause the command to fail. They are the intended output of the diagnostic. Invalid content references, build failures, or simulation failures do return a failing exit code.

The three authored seeds per result keep the complete matrix fast enough for regression checks, but they are too coarse for final balance decisions. A focused run can select encounters and loadout dimensions and deterministically extend the authored seed set up to 1,000 samples per result. For example, this runs 50 samples for every expected-cohort Tower composition:

```powershell
./build/run-encounter-calibration.ps1 `
  -EncounterId tower.floor-01.guardian,tower.floor-05.warden,tower.floor-10.sovereign `
  -GearEnvelopeId expected `
  -EssenceEnvelopeId expected `
  -Samples 50 `
  -OutputDirectory artifacts/balance-calibration/focused-tower
```

Focused runs also support `-BuildFamilyId` for solo content and `-PartyCompositionId` for Tower content. Results with at least ten samples include two-sided 95% Wilson score intervals for win and timeout rates in both the JSON artifact and a dedicated Markdown table. These intervals quantify sampling uncertainty; they do not account for whether the authored build and Essence envelopes represent the live player population.

A previous JSON artifact can be supplied as a baseline:

```powershell
./build/run-encounter-calibration.ps1 `
  -BaselinePath artifacts/balance-calibration/previous-report.json
```

The new artifact then includes changed result rows plus introduced and resolved exceptions. Comparison keys use encounter, gear envelope, build family, party composition, and Essence envelope. Reports deliberately omit a generation timestamp so identical content and deterministic seeds produce byte-stable output suitable for source-control or CI comparisons when desired.
