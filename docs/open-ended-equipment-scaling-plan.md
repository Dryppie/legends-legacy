# Open-Ended Equipment Attribute Scaling

Status: implemented foundation; hybrid-unit contract superseded by v17
Target balance model: v17 (this document retains the v16 rating-model history)
Scope: crafted equipment, tempering, combat attribute conversion, progression calibration, persistence migration, and automated verification

The tier-budget curve and recipe-weight rules below remain active. The hybrid
v17 unit contract in `hybrid-equipment-attribute-units-design.md` supersedes the
v16 rule that every bounded effect is a rating: only Armor and Resistance remain
ratings; all other percentage-like equipment attributes are direct percentages.

## Why this change exists

The current equipment model changes an attribute's budget price by recipe tier. That makes a recipe's stated weights mean different things in different regions: a 30% secondary allocation can buy a different relative amount at Tier 1, Tier 5, and Tier 10. Several supporting systems also assume Tier 10 is the end of progression by clamping budgets, tempering gains, mitigation targets, and canonical builds.

The replacement model has one rule:

> A recipe weight always represents the same share of an item's budget, at every tier.

Tier controls the size of the item budget. It does not change the exchange rate between attributes. The same formulas must remain finite and monotonic beyond Region 10 without adding another table of tier-specific exceptions.

This document supersedes the tier-dependent attribute-price and Tier-10 projection assumptions in `docs/tier-stable-combat-scaling-design.md` and `LL/docs/attribute-equipment-balancing-system-plan.md`. Those documents remain useful historical records for earlier balance versions.

## Design principles

1. **Recipes describe identity.** Their weights decide what the item specializes in.
2. **Tier describes magnitude.** It increases the one shared budget available to the recipe.
3. **Attribute prices are constant.** Each attribute has one global budget cost, independent of tier.
4. **Bounded combat effects use ratings.** Percent-like attributes can grow forever as ratings while their effective combat result approaches a safe cap.
5. **Progression is formula-driven.** Region 11 and Tier 20 use the same model as Region 1 and Tier 2.
6. **Combat is calibrated around outcomes.** Time-to-kill, time-to-death, and marginal value are the control measurements.
7. **Persisted equipment is versioned.** Old and new stat points are never interpreted silently as the same model.

## The mathematical model

### 1. Open-ended tier budget

The total one-handed, standard-quality item budget is:

```text
TierBudget(t) = BaseBudget * Growth^(t - 1)
```

Initial constants:

```text
BaseBudget = 100
Growth = (1520 / 100)^(1 / 9) ~= 1.353057
```

This preserves the existing Tier 1 and Tier 10 endpoints approximately while removing the lookup table and Tier 10 clamp. The exact growth constant is a calibration input, not an attribute-specific correction.

The complete item budget is:

```text
ItemBudget = TierBudget(t)
           * SlotMultiplier
           * QualityMultiplier
           * RollVarianceMultiplier
```

Existing slot and quality multipliers remain data-driven. Two-handed and dual-wield configurations must continue to receive equivalent total budget under their intended slot rules.

### 2. Recipe allocation

For recipe weight `w`:

```text
AttributeBudget = ItemBudget * w / Sum(all recipe weights)
RawStatPoints   = AttributeBudget / ConstantAttributeCost
```

The catalog exposes `Get(attribute)` rather than `Get(attribute, tier)`. There are no tier anchors and no interpolation.

For the approved weapon recipes this means exactly:

```text
Power budget share    = 70%
Secondary budget share = 30%
```

The secondary differs by weapon family, but its price does not change with tier.

### 3. Flat attributes and rating attributes

Attributes belong to one of two scaling families.

**Flat attributes** remain ordinary points:

- Power
- Maximum Health
- Health Regeneration

**Rating attributes** can grow without a stored hard cap and are converted into bounded combat effects:

- Armor and Resistance
- Critical Chance and Critical Damage
- Armor and Magic Penetration
- Dodge Chance and Block Chance
- Damage Reduction
- Healing Power and Life Steal
- Cooldown Reduction
- Status Resistance and Crowd-Control Resistance
- Attack Speed

The classification is centralized in the attribute catalog so allocation, display, combat, tempering, and migration cannot disagree.

### 4. Progression-normalized rating conversion

Raw ratings grow with item budgets. Combat converts them relative to the expected budget at the combatant's progression tier:

```text
ProgressionScale(p) = TierBudget(ExpectedEquipmentTier(p)) / BaseBudget
NormalizedRating    = RawRating / ProgressionScale(p)
EffectivePercent    = Cap * NormalizedRating / (HalfCapRating + NormalizedRating)
```

Properties:

- The same recipe keeps the same relative role at Tier 1, Tier 10, and Tier 50.
- More rating always helps.
- Effective percent never crosses its cap.
- Higher-tier gear is still stronger when equipped below its expected progression because the denominator uses combatant progression, not item tier.
- No recipe-tier price table is needed.

`HalfCapRating` is a global per-attribute calibration constant: the normalized rating that reaches half of the effective cap.

### 5. Specialized combat formulas

Not every rating should be applied through an identical final operation.

#### Defense and penetration

Armor and Resistance remain progression-normalized ratings. Penetration is a
direct percentage of the target's remaining typed rating:

```text
RatingAfterTargetReduction = DefenseRating * (1 - TargetReduction / 100)
RatingAfterPenetration     = max(0, RatingAfterTargetReduction * (1 - Penetration / 100))
Mitigation                 = DefenseCap * N / (DefenseHalfCap + N)
```

This prevents subtraction of unrelated percentages and keeps both defense and penetration useful beyond Tier 10.

#### Cooldown reduction

Cooldown Reduction is a direct percentage capped at 40%:

```text
Cooldown = AuthoredCooldown * (1 - CooldownReduction / 100)
```

The engine clamps the complete character total once, rounds upward to a whole
combat tick, and retains a one-tick floor:

```text
FinalCooldownTicks = max(
    1,
    ceil(AuthoredCooldownTicks
         * (1 - clamp(CooldownReduction, 0, 40) / 100)))
```

Unlike direct percentage subtraction, the rate formula cannot naturally reach
zero cooldown. Additional rating always helps, but removes progressively less
time from the remaining cooldown.

#### Attack speed

Attack rate is also rate-based:

```text
AttacksPerSecond = BaseAttacksPerSecond
                 * (1 + NormalizedAttackSpeedRating / RateConstant)
```

The engine safety clamp remains a guardrail, not a balance target.

#### Critical damage, healing, and other bonuses

Bounded bonus multipliers use the shared diminishing-return curve. Their effective result is then applied to the relevant base multiplier. This avoids an indefinitely dominant multiplicative stat while retaining a meaningful specialization.

## System changes

### Equipment budget and allocation

- Replace the Tier 1-10 budget table with the open-ended budget formula.
- Replace tier-anchored attribute costs with one constant price per attribute.
- Remove allocator tier interpolation.
- Replace raw fixed caps with maximum budget-share constraints where a recipe needs one.
- Preserve deterministic remainder allocation and exact total-budget accounting.

### Attribute aggregation

- Preserve raw equipment ratings through aggregation.
- Derive progression tier from character/encounter progression, never from recipe tier.
- Calculate effective combat values in one central projection layer.
- Expose both raw rating and effective value to diagnostics and UI DTOs where percent-like stats are shown.
- Stop rounding rating attributes to whole numbers before combat conversion.

### Tempering

Directed improvement uses a fixed fraction of the item's total budget:

```text
TemperingBudget = ItemBudget * DirectedImprovementFraction
```

The initial fraction is 2%. Attribute costs remain constant. Constraints are expressed as budget shares, so tempering uses the same rules at every tier.

### Constraints

- Remove Tier 1, Tier 5, and Tier 10 mitigation anchor interpolation.
- Remove Tier 10 normalization clamps.
- Use recipe allocation rules and maximum budget shares for authored restrictions.
- Use effective rating curves for combat caps.
- Treat runtime clamps only as last-resort safety checks.

### Recipes and unlocks

Recipe unlock ranges decide when content is available; they do not define stat mathematics. The item tier may continue past a recipe's original authored regions when progression rules permit it.

### Open-ended regions and canonical builds

- Remove `RegionCount = 10` as a mathematical progression ceiling.
- Keep authored-region validation separate from formula-based future-region projections.
- Remove the post-Tier-10 canonical modifier multiplier. Canonical equipment must use the same budget formula and allocator as real equipment.
- Calibrate creatures from canonical player outcomes: target time-to-kill, time-to-death, and role pressure. Region profiles shape difficulty but do not introduce a second equipment economy.

## Persistence and migration

`EquipmentInstance` gains a `StatModelVersion` field.

- Existing items without an explicit value are interpreted as v15.
- The EF migration rewrites every persisted v15 equipment instance name to
  `Broken <existing name>`. It stores that result in the existing `CraftedName`
  column instead of introducing a separate prefix column. `ItemInstances` is
  the shared TPH source referenced by inventories, equipped slots, marketplace
  listings, guild vaults, crafting state, and pending rewards, so updating that
  row covers every live location without location-specific update scripts.
- A follow-up compatibility migration repeats the idempotent name rewrite and
  removes the abandoned `NamePrefix` columns if an earlier local draft created
  them. This keeps both fresh databases and databases that recorded that draft
  on the same canonical schema.
- Newly rolled items use v16.
- A v15 item is converted by reconstructing the budget represented under the old tier-price catalog, preserving each attribute's budget share, then purchasing v16 raw points at the new constant price.
- Conversion is deterministic and idempotent.
- Conversion occurs through one migration service at the crafting/tempering
  boundary; combat code does not guess an item's model.
- The schema migration does not rewrite legacy stat values. V15 remains a
  supported compatibility model and is visibly identified as `Broken`; any
  future bulk stat conversion must be an explicit, separately reviewed operation.

No shared or production database migration is applied automatically from this repository.

## Verification and calibration

Automated verification covers Tier 1, 5, 10, 20, 50, and 100.

### Initial combat-pacing targets

The v16 formulas are calibrated against outcomes rather than against isolated stat totals. The following are the initial targets and should be treated as the balance contract for implementation. They can be changed later if playtesting shows that the overall combat cadence is wrong, but attribute prices must not be adjusted without measuring their effect on these outcomes.

Gameplay pacing and balance sensitivity are intentionally measured separately:

- **Gameplay encounters** determine whether ordinary combat feels appropriately fast.
- **Long-horizon benchmarks** determine whether small equipment and attribute changes have the correct value.

A short standard-enemy fight is therefore not the primary tool for pricing an attribute. Its result is naturally affected by whether the final basic attack, critical strike, or cooldown lands just before the enemy dies.

The engine runs at ten ticks per second. Tests should store durations in ticks and report them in seconds.

#### Reference conditions

Unless a scenario explicitly says otherwise, a reference matchup uses:

- a character and opponent at the same expected progression tier;
- standard-quality, common-rarity equipment;
- a complete seven-slot canonical equipment set;
- no tempering, temporary buffs, consumables, encounter hazards, or level advantage;
- the canonical ability and Essence loadout intended for the tested role;
- a neutral target with neither an artificial weakness nor an artificial counter to the build;
- sustained combat beginning with all abilities ready;
- at least 250 deterministic seeds during development and 1,000 seeds before activating a new balance version.

The median is the primary result. The 10th and 90th percentiles are recorded to make sure critical strikes, dodge, block, and targeting randomness do not create unreasonable volatility.

#### Time-to-kill targets

| Scenario                          | Target median TTK | Acceptable band | Purpose                                                                                         |
| --------------------------------- | ----------------: | --------------: | ----------------------------------------------------------------------------------------------- |
| Offense build vs standard enemy   |        11 seconds |    9-14 seconds | A normal enemy lives long enough for weapon identity; the upper edge includes one finishing action. |
| Balanced build vs standard enemy  |        14 seconds |   12-16 seconds | Default solo-combat cadence.                                                                    |
| Defensive build vs standard enemy |        18 seconds |   14-21 seconds | Defense has a visible damage tradeoff; the lower edge includes one discrete finishing action.    |
| Sustain build vs standard enemy   |        17 seconds |   13-21 seconds | Sustain sacrifices some speed for recovery; the upper edge includes one full late-game action cadence. |
| Balanced build vs elite enemy     |        52 seconds |   45-60 seconds | Several ability and defensive cycles occur and modest build differences become visible.         |
| Balanced build vs solo boss       |       180 seconds | 150-210 seconds | Long enough for complete rotations and attrition mechanics without becoming a damage sponge.    |

A party boss must have its own party-size target. It must not reuse the solo-boss health value and then depend on an assumed number of players. The initial party target is 180-240 seconds for the intended party size, measured using the corresponding canonical party composition.

An equal-tier standard enemy must not be killed by one ordinary unassisted basic attack. A canonical offense build's opening burst should normally remove no more than 45% of its health; exceptional build synergies may reach 60%, but must be called out by the analyzer.

Standard-enemy TTK is a gameplay-cadence gate, not the primary attribute-value gate. A result that differs by a single final attack should be explained but must not, by itself, cause an attribute-price change.

#### Long-horizon offensive benchmark

Attribute prices and weapon parity use a fixed 90-second target-dummy window. The target cannot die, has stable authored defenses, does not attack, and does not introduce movement, control, or phase downtime. Every build begins with all abilities ready and continues its normal priority rotation for the entire window.

The analyzer records:

- total damage and damage per second;
- basic-attack, direct-ability, periodic, critical, and summon contributions;
- number of basic attacks and ability activations;
- critical-strike rate and damage;
- effective Armor or Magic Penetration contribution;
- resource starvation and idle time.

Each candidate is tested against three targets:

1. a neutral target;
2. a physical-defense target;
3. a magical-defense target.

The 90-second damage total is the primary offensive balance measurement. Elite and boss TTK verify that the result translates into real encounters. Standard-enemy TTK verifies gameplay feel and burst safety.

The 120-second control must remain within 10% of the 90-second damage-per-second
result. This tolerance is intentionally separate from the 9% cross-tier
damage-stability gate: fixed cooldown cadence and a declared ready-at-start state
create a small, legitimate opening-window bias even for a stable rotation. The
extra cross-tier percentage point covers the measured Tier-1 whole-point
persistence edge without hiding a full action-cadence change.

The eight-seed smoke run uses a 15% 90/120 consistency tolerance because its
median is intentionally low-resolution and cannot approve a balance version.
The 250-seed development and 1,000-seed activation gates enforce the canonical
10% limit.

#### Time-to-death targets

TTD is measured under a standardized, continuous mixed-damage pressure profile. Half of pre-mitigation damage is physical and half is magical. The attacker has the expected equal-tier offensive ratings, but does not use hard crowd control.

Two values are required:

- **Raw TTD** disables healing, health regeneration, life steal, barriers, and defensive active abilities. It measures Maximum Health, Armor, Resistance, Dodge, Block, and Damage Reduction.
- **Effective TTD** enables the complete canonical kit. It measures the real survival benefit of recovery, barriers, cooldowns, and defensive abilities.

| Role           | Target raw TTD | Acceptable raw band | Target effective TTD | Acceptable effective band |
| -------------- | -------------: | ------------------: | -------------------: | ------------------------: |
| Offense        |     36 seconds |       30-42 seconds |           43 seconds |             36-50 seconds |
| Balanced       |     52 seconds |       45-60 seconds |           65 seconds |             55-76 seconds |
| Sustain        |     39 seconds |       34-45 seconds |          102 seconds |            85-120 seconds |
| Defensive/tank |     72 seconds |       60-85 seconds |          120 seconds |            90-135 seconds |

Sustain intentionally has a lower raw-TTD target than Balanced. Cloth sustain
equipment spends its defensive budget on healing throughput, regeneration, and
cooldown rather than raw health and mitigation. Its role-defining durability is
therefore enforced by the much higher effective-TTD band; requiring it to also
beat Balanced in raw TTD would erase that equipment tradeoff.

These targets make defensive differences visible across many incoming hits and cooldown cycles. Actual encounters can still be dangerous through multiple enemies, concentrated burst windows, crowd control, hazards, healing reduction, and resource depletion.

Effective TTD must remain finite. Sustain and defensive benchmarks run for at least 120 seconds even when the expected death occurs earlier. A build that remains alive at the end of the window is rerun under progressively increasing pressure to identify its break point. A sustain or defensive build that survives the reference pressure indefinitely fails calibration even when its healing-per-second only slightly exceeds incoming damage.

#### Weapon and attribute parity

For equal-budget weapons in the 90-second offensive benchmark:

- median total damage against the neutral target should remain within 8% of the weapon-family median;
- opening-burst TTK may differ by up to 15% where that difference is part of the weapon identity;
- a weapon's 30% secondary allocation must improve its intended scenario by 5-12% compared with replacing that allocation with an irrelevant secondary of equal budget;
- no secondary may be the best choice in every neutral, armored, resistant, burst, and sustained scenario;
- Armor Penetration and Magic Penetration are measured against targets with the corresponding defense, while Attack Speed, Critical Chance, and Critical Damage are measured over enough seeds to stabilize their variance.

These are outcome-parity limits, not a requirement for identical damage patterns. A crossbow and gauntlets can reach similar 90-second output through very different hit cadence and variance.

#### Tier-to-tier stability

At Tier 1, 5, 10, 20, 50, and 100, an equal-tier character fighting the corresponding equal-tier reference opponent must remain within:

- 10% of the Tier 1 normalized TTK result;
- 9% of the Tier 1 normalized 90-second damage result;
- 12% of the Tier 1 normalized raw TTD result;
- 15% of the Tier 1 normalized effective TTD result.

The wider effective-TTD tolerance acknowledges interactions among healing, cooldowns, barriers, and incoming damage cadence. A persistent drift in one direction across three checkpoints is still a failure even when each individual checkpoint barely remains inside the band.

Equipping a complete set one tier above the encounter should produce a noticeable but non-trivial advantage. The calibrated target is 7-18% faster TTK and 8-18% longer raw TTD against the previous-tier reference opponent. The earlier 20-30% / 20-35% hypothesis was rejected by the real-engine control: it would make a single equipment tier overwhelm character base stats and compound too aggressively across open-ended regions. The narrower measured band keeps an upgrade visible while preserving build and encounter headroom.

#### Volatility and failure rules

The balance version is blocked when any of the following is true:

- the 90th-percentile standard-enemy TTK is more than 50% longer than the median;
- a 90-second offensive result changes materially when the window is extended to 120 seconds without an authored ramping mechanic explaining the difference;
- the 10th-percentile raw TTD is less than 70% of the median for a non-offense role;
- a reference sustain or tank build becomes immortal under continuous pressure;
- an attribute has no positive measurable marginal value in any intended scenario;
- an attribute has the highest marginal value in every tested scenario;
- normalized TTK or TTD continually drifts as tiers increase;
- more than 1% of an item's intended budget is left unspent outside quantization tolerance.

### Dedicated canonical TTK/TTD analyzer

Combat pacing is enforced by a dedicated canonical analyzer. It complements the
attribute marginal-value analyzer: the marginal-value analyzer measures what an
equal equipment budget buys, while the canonical TTK/TTD analyzer determines
whether the resulting complete builds satisfy the gameplay-duration contract.

#### Canonical roles and builds

The analyzer owns four reproducible build definitions:

| Role      | Intended outcome                                   |
| --------- | -------------------------------------------------- |
| Offense   | Highest damage and lowest survivability            |
| Balanced  | General-purpose reference damage and survivability |
| Sustain   | Lower damage with strong recovery                  |
| Defensive | Lowest damage with the greatest raw durability     |

Each definition fixes the character level, seven equipment recipes, rarity,
quality, weapon and armor family, abilities, priority rotation, Essences,
starting resources, and any other loadout input that can affect combat. The same
definitions are generated at Tier 1, 5, 10, 20, 50, and 100 through the real
budget curve and equipment allocator.

Canonical definitions cannot contain hidden per-tier multipliers or hand-tuned
attribute amounts. A tier checkpoint may change progression inputs such as level,
but it cannot change the build's role or recipe budget shares.

#### Fixed reference controls

All roles fight the same equal-tier standard enemy. The opponent cannot be
changed per role: doing so would make the test self-fulfilling. The standard
enemy is calibrated once around the Balanced build's 14-second median target;
the Offense, Sustain, and Defensive builds must then fall naturally into their
own target bands.

Elite and solo-boss references are also shared authored controls. They derive
from the same reference model using explicit encounter profiles, rather than
being regenerated to fit the candidate build. The party-boss benchmark fixes its
intended party size and complete canonical composition; it never estimates party
health by multiplying a solo-boss value by an assumed number of players.

TTD uses a separate, unkillable pressure source so the character cannot end the
measurement by defeating its attacker. It applies continuous equal-tier pressure
with an even physical/magical pre-mitigation split, expected penetration ratings,
and no hard crowd control. Effective-TTD scenarios also provide a stable target
for offensive recovery mechanics such as life steal without allowing that target
to terminate the pressure test.

The reference enemies and pressure source are versioned inputs. They may be
changed when the intended overall combat cadence changes, but must not be altered
merely to make a candidate equipment version pass.

#### Scenario matrix

For every checkpoint tier, the analyzer runs:

1. all four roles against the standard enemy for TTK;
2. the Balanced build against the elite and solo-boss references;
3. the canonical party against the party-boss reference;
4. raw TTD for all four roles with healing, regeneration, life steal, barriers,
   and defensive active abilities disabled;
5. effective TTD for all four roles with the complete canonical kit enabled;
6. the fixed 90-second offensive benchmark and its 120-second consistency check;
7. one-tier-overgeared TTK and raw-TTD comparisons against the previous-tier
   reference opponent.

All scenarios begin from the same declared initial state, including whether
active abilities start ready. Simulation event capture is disabled unless a
failure needs a diagnostic replay; aggregate measurements remain deterministic.

#### Measurements and percentile gates

Every scenario records:

- P10, median, P90, minimum, and maximum duration;
- victories, defeats, draws, and timeouts;
- opening basic-attack and burst damage as a percentage of target health;
- remaining health and survivor counts where applicable;
- basic attacks, ability activations, and complete ability cycles;
- healing, regeneration, life-steal, barrier, mitigation, dodge, and block
  contributions relevant to the scenario.

The median is compared with the role and encounter bands defined above. The
analyzer additionally blocks the candidate when:

- an ordinary basic attack defeats the equal-tier standard enemy;
- the Offense build's normal opening burst exceeds 45% of standard-enemy health;
- an explicitly identified exceptional synergy exceeds the 60% burst ceiling;
- P90 standard-enemy TTK exceeds 150% of its median;
- P10 raw TTD is below 70% of its median for Balanced, Sustain, or Defensive;
- any required encounter ends in a draw or unresolved timeout;
- a Sustain or Defensive build is classified as immortal under reference
  pressure.

Percentiles are calculated from the full deterministic sample, not from averages
of smaller batches. The report records the percentile method so identical inputs
produce byte-stable results.

#### Immortality detection

Surviving the initial effective-TTD window is not automatically immortality. A
build may legitimately die just beyond 120 seconds or after a long defensive
cycle. When a Sustain or Defensive build reaches the end of the normal window,
the analyzer:

1. extends the run through several complete recovery and defensive cycles;
2. inspects late-window health floors, peaks, and trend;
3. reruns the scenario under progressively increasing pressure;
4. identifies and reports the lowest pressure multiplier that produces a finite
   death.

A build fails the immortality gate when it establishes a repeatable,
non-declining health cycle under reference pressure rather than merely surviving
slightly beyond the target band. The report includes the reference-pressure
survival time, final health, late-window trend, pressure breakpoint, and evidence
used for the classification.

#### Tier stability and overgear enforcement

Tier 1 is the normalized baseline. Tier 5, 10, 20, 50, and 100 must satisfy the
TTK, 90-second damage, raw-TTD, and effective-TTD tolerances defined above. A
persistent movement in the same direction across three checkpoints is reported
as drift even if every individual value is barely within its allowed tolerance.

The one-tier-overgeared scenario separately enforces 7-18% faster TTK and
8-18% longer raw TTD. This proves both sides of the progression contract:
equal-tier pacing remains stable, while genuinely better equipment provides a
noticeable but non-trivial advantage.

High-tier analysis uses progression-normalized combat coordinates to avoid
mistaking numeric magnitude or integer saturation for a balance result. A
separate numeric-safety assertion verifies that raw generated equipment values
remain finite and serializable.

#### Enforcement levels and artifacts

The analyzer supports three explicit execution levels:

- a small deterministic smoke sample for ordinary regression tests;
- at least 250 seeds per scenario for development calibration;
- at least 1,000 seeds per scenario before activating a new balance version.

Only the latter two enforce the complete pacing contract. A smoke run can detect
structural breakage but cannot approve balance. Expensive matrices are split into
balance-test shards and use the combat engine without event logging; any failed
sample can be replayed with its recorded seed and full diagnostics.

Every development and activation run writes a reviewable, versioned JSON artifact
containing the balance version, formula inputs, canonical build identities,
reference-control versions, seed schedule, scenario measurements, expected bands,
actual results, and machine-readable failure reasons. A representative entry is:

```json
{
  "role": "Balanced",
  "tier": 10,
  "scenario": "StandardEnemyTtk",
  "medianTicks": 143,
  "p10Ticks": 130,
  "p90Ticks": 158,
  "acceptableBand": {
    "minimumTicks": 120,
    "maximumTicks": 160
  },
  "passed": true,
  "failures": []
}
```

The balance version cannot be activated unless the 1,000-seed artifact contains
all required scenarios, every blocking gate passes, and the artifact has been
reviewed. The artifact is evidence of the fixed-control measurement; it is not a
place to override or waive failed outcomes silently.

### Player-facing hybrid-unit presentation

The progression-normalized model is an internal balance system. Players are not
expected to calculate normalization scales, half-cap ratings, or diminishing-
return formulas. The presentation contract is:

> Equipment shows its intrinsic flat points, opposed ratings, or direct
> percentages; character and comparison views show complete capped outcomes.

#### Item and crafting displays

V17 equipment and crafting previews show canonical intrinsic values, for example:

```text
+50 Armor
+10% Critical Chance
+8% Dodge Chance
```

Armor and Resistance are the only rating inputs and omit the redundant word
`Rating` on item lines. Every other percentage is stored and displayed directly.

Legacy v15 equipment migrates through v16 before v17; native v16 percentage-like
ratings are frozen at their effective value at the item's own tier.

#### Character overview and comparisons

The character overview shows both the aggregated raw rating and its effective
combat result:

```text
Armor: 150
Physical Damage Reduction: 46.5%

Resistance: 105
Magical Damage Reduction: 38.5%
```

Equipment comparisons calculate the complete character result before and after
the candidate replacement. Because ratings have diminishing returns, an item
cannot truthfully advertise one standalone mitigation or critical-chance value.
A comparison therefore uses an outcome presentation such as:

```text
Armor                110 -> 150
Physical Mitigation  41.5% -> 46.5%  (+5.0 percentage points)
Critical Chance       20% -> 28%     (+8 percentage points)
```

When no character context exists, such as an unauthenticated catalog, the item
shows raw rating only. Advanced tooltips may explain that ratings are combined
and converted with diminishing returns and may state the effective cap, but the
normalization formula is not required knowledge for ordinary play.

Direct temporary percentage effects and multiplicative effects remain visibly
distinct from equipment ratings. A `%` suffix is reserved for a genuine direct
or already-derived percentage; `Rating` identifies an input to combat
conversion.

#### Naming and explanations

Player-facing names should describe both the input and result unambiguously:

- `Armor` produces `Physical Damage Reduction`;
- `Resistance` produces `Magical Damage Reduction`;
- offensive and utility item inputs use direct names such as `Critical Chance`,
  `Attack Speed`, and `Cooldown Reduction`;
- the character overview uses result names such as `Critical Chance`, `Attack
  Rate`, and `Cooldown Reduction` where it displays converted values.

Only Armor and Resistance use the tooltip language "Combined rating determines
effective reduction." Direct percentage tooltips state the exact operation.

#### Progression transitions

The UI must not hide a change in effective percentages at a progression
boundary. Before activation, the Tier 1-to-2 and later transitions are tested to
ensure that entry gear from the new tier compensates for the stronger rating
requirement. The character overview and equipment comparison always calculate
against the character's current progression scale so the displayed result is the
result combat will use.

If discrete 50-level normalization produces a perceptible loss of character
power on level-up, the scale must be smoothed or the progression handoff must be
redesigned before release. Leveling up must not silently make an unchanged
character feel weaker. The canonical analyzer records both sides of every
progression boundary in addition to the main tier checkpoints.

The first implementation should tune global tier growth, attribute cost, rating caps, and half-cap ratings until these bands are met. Recipe weights should change only when the role or identity of the recipe is itself incorrect.

### Mathematical invariants

- Tier budgets are positive, finite, and strictly increasing.
- Attribute prices never depend on tier.
- Recipe budget shares are identical at every tested tier.
- Allocation spends its intended budget within the quantization tolerance.
- Effective ratings are finite, monotonic, and below their caps.
- Canonical builds do not use a separate Tier-10-plus multiplier.
- Tempering spends the same percentage of item budget at every tier.

### Combat outcome gates

- Player-versus-peer and player-versus-creature time-to-kill remain within target bands.
- Time-to-death remains within target bands for tank, damage, and sustain profiles.
- Each recipe's secondary attribute has positive marginal value in its intended scenario.
- No attribute dominates equivalent-budget alternatives across all tested encounters.
- Defense and penetration remain mutually useful.
- Two-handed and dual-wield budget equivalence remains intact.

### Simulation workflow

1. Generate canonical builds for each role and checkpoint tier.
2. Simulate repeated deterministic encounter matrices.
3. Compare TTK, TTD, healing pressure, resource pressure, and marginal-value deltas.
4. Adjust only global inputs first: tier growth, constant attribute price, rating cap, and half-cap rating.
5. Change recipe weights only when the item identity itself is wrong.
6. Save analyzer output as reviewable artifacts before changing the active balance version.

This follows the useful common theme in the linked balancing discussion: choose a control measurement and tune through repeated simulated outcomes instead of trying to derive every stat in isolation. Relevant contributions include [Josef Shindler's control-variable framing](https://www.quora.com/Are-there-methods-or-standard-formulas-for-balancing-stats-in-role-playing-games/answer/Josef-Shindler), [Gerson Da Silva's fixed control-number approach](https://www.quora.com/Are-there-methods-or-standard-formulas-for-balancing-stats-in-role-playing-games/answer/Gerson-Da-Silva), and [Bram Cohen's simulation-and-feedback recommendation](https://www.quora.com/Are-there-methods-or-standard-formulas-for-balancing-stats-in-role-playing-games/answer/Bram-Cohen).

## V17 rollout order

1. Add the hybrid unit catalog and v17 prices behind the balance-version boundary.
2. Update allocation, evaluation, constraints, tempering, combat, and presentation.
3. Add staged v15 -> v16 -> v17 migration for live items and snapshots.
4. Update canonical builds and open-ended progression helpers.
5. Run invariant tests and combat simulations at all checkpoints.
6. Review the generated v17 activation artifact before enabling new crafting.
7. Review the generated EF Core migration; do not apply it to shared environments.

## Production activation checklist

The implementation is complete in the application, but release activation remains
an explicit operator action. Use this order so old and new equipment remain
readable throughout a rolling deployment:

1. Back up the production database and record row counts for equipment instances
   and equipment snapshots.
2. Review the existing stat-version migrations and
   `20260816022220_MigrateEquipmentStatsToV17`. The v17 migration chains v15 rows
   through v16, freezes v16 percentage-like ratings at the owning item's tier,
   and covers the shared `ItemInstances` table plus equipment snapshots.
3. Deploy API, worker, and both clients together so v17 direct percentages are
   never interpreted by a v16 client as ratings.
4. Run the Admin Dashboard analyzer at `Activation` level against the exact
   release build. Confirm all required tiers and scenarios are present,
   `CanApprove` is true, and there are no blocking failures. Archive and review
   the generated `equipment-combat-pacing-v17-activation.json` artifact.
5. In staging, verify one legacy item in each important location (inventory,
   equipped, marketplace, and guild vault), a newly crafted v17 item, tempering,
   complete-character comparison, character rating display, and one combat of
   each damage type.
6. Activate v17 crafting, then monitor crafting distributions, equip/comparison
   errors, TTK/TTD telemetry, combat timeouts, and market behavior through at
   least one normal content cycle.

Rollback disables new v17 creation, but a service-only rollback is unsafe after
the migration because v17 reuses columns that held v16 ratings for direct
percentages. Restore a reviewed database backup and the matching service/client
release together; the migration intentionally has no automatic `Down` rewrite.

## Decisions intentionally centralized

The following values are calibration inputs, not recipe-tier data:

- base item budget
- per-tier growth factor
- constant price per attribute
- direct-percentage cap per attribute
- Armor and Resistance rating caps and half-cap ratings
- tempering budget fraction
- canonical TTK and TTD target bands

Keeping these inputs centralized makes later balance work auditable and allows progression to continue without introducing Region 11-specific stat rules.
