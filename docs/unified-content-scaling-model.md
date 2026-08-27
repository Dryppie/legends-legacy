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

## 9. Balance Calibration Ownership

The former Essence progression matrix, generated player snapshots, authored-encounter calibration, and repeatable calibration report tooling were retired during P0 of the automated balance-system replacement.

The production scaling rules above remain authoritative for live content. New offline balance analysis must be implemented through the architecture described in `docs/content-balancing/legendslegacy-automated-balance-system-implementation-plan.md`; production encounters must not adapt dynamically to a player's equipped Essences.
