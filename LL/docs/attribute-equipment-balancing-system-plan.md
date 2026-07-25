# Attribute and equipment balancing system

## Purpose

This document analyzes the current attribute and equipment implementations and proposes a system for:

1. defining exactly how every attribute affects combat;
2. measuring how much stronger an attribute makes a character;
3. converting that measured value into fair equipment stat costs;
4. preventing a stat such as Armor from consuming the same item budget as a vastly stronger amount of Max Health;
5. keeping combat, crafting, item comparison, and displayed Power consistent as the game changes.

The core recommendation is:

> Use the production combat engine to measure marginal attribute value, publish those measurements as a versioned balance profile, and make equipment generation and display consume that profile.

A single static score is not enough for every purpose. The system should deliberately expose two different measurements:

- **Item Budget**: the intrinsic amount of generation budget contained in an item. This is stable and suitable for crafting, loot generation, tempering, and checking whether two items of the same tier are equally funded.
- **Build Impact**: the context-dependent improvement the item gives a particular character or reference build. This is suitable for balance analysis and equipment comparison. The existing simulation-backed character Power remains the authoritative whole-build measurement.

## Executive diagnosis

Before combat rules version 3, the repository contained many of the right pieces, but they were not connected:

- `EquipmentStatBudgetCatalog` assigns a hand-authored `CostPerPoint` and hard cap to all 24 attributes.
- Crafting spends tier, slot, and quality budget through those costs.
- The production `FastCombatEngine` determines actual combat value.
- The Power Rating system already measures whole builds through deterministic production-engine simulations.
- The Angular equipment display separately assigns another set of hand-authored attribute weights.

That diagnosis drove the first implementation slice. Production combat, equipment evaluation, and the player-facing Gear Value now share the same attribute definitions and backend budget catalog. The simulation-backed whole-build Power rating remains separate by design.

Armor and Resistance were the clearest examples:

- equipment recipes allocate meaningful budget to both;
- the frontend gives both a displayed Power weight of `4`;
- an unused `ArmorDamageReductionConstants` helper contains a possible mitigation curve;
- `FastCombatEngine.ApplyDamage` did not read Armor, Resistance, Armor Penetration, or Magic Penetration;
- therefore, adding Armor or Resistance did not change damage taken.

Combat rules version 3 closes those gaps. Typed mitigation, penetration, crit, block, recovery, cooldown, resistance, and summon mechanics now run through `FastCombatEngine`.

The remaining problem is calibration rather than missing core formulas: a static cost can be fair at one health/defense scale and badly wrong at another. The marginal analyzer added in the second slice measures that mismatch directly at tiers 1, 5, and 10.

The first balancing milestone must therefore be **mechanical completeness**, not coefficient tuning. It is impossible to derive fair weights for a stat whose behavior is absent or ambiguous.

The target design deliberately removes arbitrary primary-stat selection from ability output. All magnitude-bearing abilities use Power. The other primary attributes influence explicit, limited groups of derived attributes: Fortitude provides toughness, Precision provides offensive reliability and tempo, and Spirit provides sustain, support, status resilience, and summon strength.

## Implementation status

The production foundation and controlled calibration slices are implemented with combat rules version 4 and equipment balance version 4:

- all authored ability-effect scaling has been migrated to Power, and catalog validation rejects non-Power effect scaling;
- Fortitude, Precision, and Spirit project into only their approved derived-stat groups;
- runtime primary-stat buffs and debuffs update the same dependency groups;
- Armor, Resistance, penetration, block, capped general reduction, crit, Healing Power, character Life Steal, Cooldown Reduction, status resistance, crowd-control resistance, Weapon Damage, and primary-derived summon stats are consumed by production combat;
- direct healing crits by default through the three-state `CritEligibility` policy;
- active Cooldown Reduction is additive and globally capped at 40%;
- equipment generation, tempering, API DTOs, and the UI Gear Value use the backend equipment budget catalog;
- the frontend-local `ATTRIBUTE_WEIGHTS` table has been removed;
- duplicate two-handed equipment instances are ignored during character attribute aggregation;
- status resistance now shortens both the status lifecycle and timed payloads owned by that status;
- Summon Power and Summon Health now multiply the corresponding summon attributes exactly once;
- a deterministic marginal-value analyzer covers all 24 equipment attributes at tiers 1, 5, and 10;
- the analyzer executes thirteen production-engine scenarios over eight fixed seeds, reports paired 95% confidence intervals, proposes tier-specific candidate costs, and flags inert, cap-limited, mispriced, and unequal-budget results;
- those scenarios now include mixed typed pressure, unmitigated pressure, a short burst check, and a 600-tick long-sustain check;
- an explicit 114-cell equal-budget matrix covers primary identities, offense, crit frontiers, steady/burst defense, sustain, Health Regeneration versus tank peers, and low/high-defense penetration at all three reference tiers; 108 cells are release gates and six are high-defense sensitivity probes;
- five canonical full-equipment loadouts are analyzed at tiers 1, 5, and 10: Heavy Shield, Medium Dual Wield, Cloth Support, Two-Handed Damage, and Summoner;
- loadout budgets are assembled from the actual individual equipment slot weights, including shield, dual-wield, and two-handed hand configurations;
- every loadout runs the complete scenario matrix, reports its relevant scenarios separately, and exposes target, spent, and unspent budget plus any per-item hard-cap pressure;
- comparison coverage includes a Medium Dual Wield versus Two-Handed peer check and a Cloth Support versus Summoner output decomposition at every reference tier;
- every loadout scenario now decomposes direct damage, summon damage, healing, regeneration, barrier generation, barrier absorption, damage taken, remaining health, duration, and avoided attacks;
- the reported utility score includes an explicit damage, sustain, prevention, and survival breakdown, so no aggregate score is unexplained;
- generated barrier is reported but only barrier actually absorbed contributes prevention utility;
- each loadout reports a relevant-scenario utility index, where `100` is the per-scenario median before the profile's intended scenarios are averaged;
- comparisons are classified as either a genuine peer-balance check or output decomposition; cross-role Cloth Support versus Summoner results no longer emit a misleading loadout-mismatch warning;
- matched summon calibration runs equal-budget Summoner and Direct Caster controls at tiers 1, 5, and 10 over 90, 180, and 600 ticks; both kits contain the common magical strike plus one 100-tick role ability;
- the Direct Caster's second ability is authored as the Power-scaling, direct-damage equivalent of one nominal 100-tick summon lifetime, so the control no longer gives Summoner a free extra ability;
- the matched Direct Caster now reserves the same 20% defensive budget that the Summoner spends on owner/summon durability, and summon-only pressure no longer accidentally tests owner survivability;
- summon abilities and newly created summons enter ready, removing the former double startup delay and its compatibility switch;
- Summon Power uses active tier anchors of `1.50`, `1.25`, and `1.00` at tiers 1, 5, and 10; all nine matched summon comparisons pass the 20% gate;
- Health Regeneration restores a flat amount every five seconds, displays that unit explicitly as `HP/5s`, and uses active tier anchors of `1.50`, `1.50`, and `1.75`;
- the summon report separately measures the effect of removing the summon ability, Spirit-derived summon bonuses, and explicit Summon Power/Health rolls, plus summon count, average active summons, and uptime;
- matched hand calibration runs actual funding/behavior, equal-budget, and equal-budget/equal-behavior controls over the same tier and duration matrix;
- the analyzer now includes a tier-10 maximum-progression envelope across every real recipe/blueprint loadout: maximum `1.05` craft variance, Masterwork quality, Legacy rarity's `6x` static-base boost, and all six rarity-upgrade improvement budgets;
- all 1,248 maximum-progression loadouts spend their complete generated, blueprint, and rarity budgets without exceeding an effective character cap;
- maximum-progression diagnostics are grouped by saturated attribute and by recipe/equipment type/blueprint for unspent generated-versus-rarity budget, including the item caps and combat caps that caused the pressure;
- capped percentage modifiers on static item bases are clamped before entering generation, so rarity cannot turn a 50% Block Chance base into an unusable 300% value;
- blueprint bonus rolls and rarity improvements have separate 25% cap headroom; when all authored tempering choices are exhausted, rarity improvements redirect into a role-appropriate fallback group instead of silently losing the promised upgrade;
- a deterministic constrained budget allocator shares whole-character cap capacity across independently generated slots, counts direct and primary-derived consumption together, and redirects saturated weight into role-appropriate attributes for crafted base rolls, previews, quality upgrades, tempering, and analyzer loadouts;
- per-item allocation caps are now explicitly distinct from effective whole-character combat caps;
- every canonical loadout reports whole-character cap utilization, direct equipment overflow, equivalent wasted budget, and wasted target-budget percentage; the Attack Speed cap is derived from weapon interval and the production 4x attack-rate ceiling;
- an aggregate allocator remains as an exact-loadout reference, applying baseline and primary-derived contributions before caps and deterministically redistributing remaining budget;
- the report compares the conservative production allocation against that exact-loadout reference;
- canonical scenarios and matched summon/hand controls run through the active production profile;
- balance-version-4 constraints translate whole-character combat caps into conservative per-item capacity shares, account for primary-derived cap consumption, and redirect saturated recipe weight into role-appropriate uncapped attributes;
- the production validator composes the complete enabled recipe and blueprint catalog into armor-family and hand-configuration loadouts at tiers 1, 5, and 10;
- a second production-engine matrix runs 33 comparisons made from actual composed recipes and blueprints through the active profile: 21 same-purpose release gates and 12 deliberately diagnostic cross-role or cross-specialization cells;
- those real-content comparisons cover balanced, fast, and penetration-oriented dual-wield/two-handed configurations, a tower-shield defensive peer, armor-family decompositions, and six blueprint relationships;
- the active-profile calibration gate fails if any equal-budget, summon, representative hand, real-content, or maximum-progression release comparison exceeds its tolerance; it also fails if any maximum-progression loadout exceeds a combat cap or leaves generated, blueprint, or rarity budget unspent;
- the equipment budget profile supports reviewed tier anchors with linear interpolation for tiers 1 through 10;
- crafting rolls, crafting previews, tempering, Admin diagnostics, API Gear Value, and frontend metadata all resolve the same tier-specific cost;
- all equipment generation and Gear Value evaluation use the single active balance profile;
- the report is available from the Admin diagnostics `attribute-balance` endpoint;
- focused combat, analyzer, and equal-budget regression tests have been added.

The initial primary coefficients are:

| Primary   | Derived contribution per point                                                                                                                            |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Fortitude | `+4 Max Health`, `+0.5 Armor`, `+0.5 Resistance`                                                                                                          |
| Precision | `+0.1 Crit Chance`, `+0.1 Armor Penetration`, `+0.1 Magic Penetration`, `+0.05 Attack Speed`                                                              |
| Spirit    | `+0.15 Healing Power`, `+0.05 Health Regeneration (HP/5s)`, `+0.1 Status Resistance`, `+0.1 Crowd Control Resistance`, `+0.05 Summon Power`, `+0.1 Summon Health` |

The initial mitigation scale is `100`. Physical and Bleed damage use Armor; Magical, Burn, and Poison damage use Resistance. Block is capped at 50% chance and prevents 50% of an eligible direct hit. General Damage Reduction and Cooldown Reduction are each capped at 40%.

Max Health costs `0.20` item budget per point. Armor and Resistance use reviewed cost anchors of `0.54`, `1.02`, and `1.37` at tiers 1, 5, and 10, with linear interpolation between anchors. Those values come from the marginal effective-health derivative at the reference character's health and existing typed defense. Equal-budget analyzer comparisons are required to remain within 10 percentage points at all three reference tiers.

Fortitude is priced as its exact derived basket. Its tier 1, 5, and 10 anchors are therefore `1.34`, `1.82`, and `2.17`:

```text
Fortitude cost =
    4 * Max Health cost
    + 0.5 * Armor cost
    + 0.5 * Resistance cost
```

Precision costs `1.15`, equal to its direct Crit Chance, physical penetration, magical penetration, and Attack Speed basket. Spirit uses `1.05`, `1.0375`, and `1.0375` at tiers 1, 5, and 10, equal to its active healing, regeneration, resistance, and summon basket. Crit Damage uses tier anchors of `2.00`, `2.25`, and `2.50` because its marginal value rises alongside endgame Crit Chance. Power and the remaining derived attributes retain their earlier costs.

The hand calibration gate is complete. Two-handed weapons receive the same combined `1.70` hand-slot funding as dual-wield. The authored two-handed Maul behavior uses `1.25 / 1.18` interval/damage multipliers, and the Gauntlets use `0.75 / 0.865`. All nine representative Dual-Wield versus Two-Handed comparisons and all normal and maximum-progression real hand-configuration release cells pass the 20% tolerance.

The code-side balance program is complete. The remaining runtime check is to let the calibration worker regenerate dungeon recommendations under Power algorithm version 13 and combat rules version 4, then review the results.

### Implemented marginal analyzer

`AttributeMarginalValueAnalyzer` is a read-only diagnostic. It never edits the live budget catalog.

For each equipment attribute it:

1. creates tier 1, 5, and 10 reference characters;
2. allocates 10% of that tier's standard crafting budget to the tested stat;
3. respects the current cost and hard cap;
4. runs the baseline and modified build with the same eight deterministic seeds;
5. scores only scenarios relevant to that stat;
6. reports the paired relative gain and a 95% confidence interval;
7. computes median relevant-scenario gain and gain per budget;
8. derives a candidate tier cost relative to the median measured efficiency;
9. constructs each canonical loadout one slot at a time, applying the same per-item hard caps as generated equipment;
10. runs every loadout through all thirteen scenarios and reports relevant-scenario medians separately;
11. compares intended peer loadouts and emits warnings when their relevant outcome differs by more than 20%;
12. runs the approved equal-budget peer matrix with per-relationship intent and tolerance, and blocks balance-version promotion when any cell fails;
13. emits warnings for inert paths, ordinary-gear cap pressure, cost changes over 20%, and failed peer comparisons.

The implemented scenarios are physical offense, magical offense, periodic offense, physical pressure, magical pressure, healing sustain, status resilience, crowd-control resilience, summon offense, mixed pressure, unmitigated pressure, burst pressure, and long sustain.

Candidate costs are diagnostic evidence, not an automatic balance mutation. Discrete tick rounding and conditional mechanics can make a small tier-1 perturbation appear inert, so a cost should be accepted only after its adjacent tiers and confidence interval are reviewed.

### Implemented full-loadout diagnostic

The loadout diagnostic answers a different question from single-stat marginal value: whether a complete, plausible equipment allocation remains competitive after slot weights, per-item caps, stat interactions, combat cadence, and encounter duration are all applied together.

Each profile owns:

- an explicit stat-budget share map that sums to 100%;
- its actual list of equipment slot weights rather than one aggregate budget;
- a hand configuration, basic-attack cadence and magnitude, damage type, and authored abilities;
- a set of scenarios that defines the profile's intended role.

Budget is spent independently on every item before totals are aggregated. This is important because a global loadout allocation can hide a real generation constraint: two moderate rolls on separate items may be legal even when their combined value exceeds one item's hard cap.

The first diagnostic pass exposes three tuning targets rather than silently changing production costs. Positive values in the table favor the first profile; negative values favor the second:

| Comparison and scenario                                  |    Tier 1 |    Tier 5 |    Tier 10 |
| -------------------------------------------------------- | --------: | --------: | ---------: |
| Medium Dual Wield vs Two-Handed Damage, physical offense |  `-7.11%` | `+32.69%` |  `-33.41%` |
| Cloth Support vs Summoner, magical output decomposition  | `-57.87%` | `-98.85%` | `-148.97%` |

1. **The magical roles produce fundamentally different output.** This comparison is retained only as output decomposition, not as a peer-balance warning. At tier 5, Cloth Support produces `3,114.38` direct damage, `12` healing, and `96` absorbed barrier; Summoner produces `3,002.88` direct damage, `6,504` summon damage, and `84` regeneration. The decomposition shows that direct Power output is close and the separation comes from the summon channel. A matched control is still required before deciding whether that channel is overpriced or the expected role difference.
2. **Hand balance is tier-dependent.** Medium Dual Wield and Two-Handed Damage reverse their relative result across the reference tiers and leave the intended 20% tolerance band. Slot funding cannot be tuned independently from weapon magnitude and attack cadence.
3. **Hard-cap waste is now resolved deterministically.** When a stat reaches its per-item cap, the allocator removes it from the active pool and redistributes the overflow proportionally among the profile's remaining eligible stats. The tier-10 Two-Handed Damage and Summoner profiles now spend their full `9,348` target budget even though their reports still identify which attributes reached a cap.

These were the diagnostic findings that motivated balance version 3. The later constrained-allocation and real-content calibration sections record how they were resolved before promotion.

### Explainable loadout utility

Every scenario result now publishes both raw output and the contribution of each channel to its utility score:

```text
total utility =
    damage contribution
    + sustain contribution
    + prevention contribution
    + survival contribution
```

The scenario defines the coefficients rather than the loadout. This means two builds running the same scenario are judged by the same rules. Offense scenarios value direct and summon damage equally and give smaller credit to actual sustain and prevention. Pressure scenarios value duration, remaining health, actual healing/regeneration, and absorbed barrier. Generated but unused healing or barrier is visible in telemetry but does not inflate utility.

The relevant-scenario utility index is contextual, not a universal character Power value. For each relevant scenario, the analyzer expresses the loadout relative to the median loadout at that tier, rebases the median to `100`, and then averages only the profile's declared relevant scenarios. It is useful for detecting tier inversions within one profile; it must not be used to claim that a tank index and a damage index are interchangeable.

`EquipmentLoadoutComparisonPurpose.PeerBalance` enables the 20% mismatch warning. `OutputDecomposition` deliberately suppresses that warning and exists to compare channels across different roles. This distinction prevents the diagnostic from treating “support heals while summoner deals summon damage” as automatic evidence that one equipment budget is wrong.

### Matched calibration gate

Equipment balance version 4 is mechanically guarded by the controlled tier matrices, aggregate cap utilization, and the tier-10 Masterwork/Legacy maximum-progression envelope. The gate verifies the active profile rather than a shadow candidate.

The summon matrix compares damage per 100 spent equipment budget after aggregate-safe candidate redistribution. The Direct Caster has a second Power-scaling direct ability with the same 100-tick cooldown as the summon. Its authored magnitude is equal to the nominal damage of one 100-tick summon lifetime: four summon strikes plus five basic attacks.

Three confounders were removed from the first matrix:

1. The Direct Caster reserves 20% of its equipment budget for Fortitude, matching the Summoner's 15% Summon Health and 5% Max Health allocation instead of spending its entire budget on direct offense.
2. The hostile pressure ability targets `SummonedEnemies`, so it measures Summon Health and summon uptime without killing the owner and turning the test into an owner-survival comparison.
3. Summon abilities begin ready and newly created summons begin with their active ability and basic attack ready. This removes the combination of an owner initial cooldown followed by a second summon initial cooldown.

The remaining tier progression is handled through the active Summon Power prices:

| Tier | Active Summon Power cost |
| ---- | ------------------------: |
| 1    |                    `1.50` |
| 5    |                    `1.25` |
| 10   |                    `1.00` |

Intermediate tiers interpolate linearly. The resulting matched differences are:

| Tier |  90 ticks | 180 ticks | 600 ticks |
| ---- | --------: | --------: | --------: |
| 1    | `+19.21%` |  `+4.93%` |  `-3.40%` |
| 5    | `-19.09%` | `-14.18%` |  `-9.69%` |
| 10   |  `-8.99%` |  `-4.97%` |  `+1.43%` |

Positive values favor Summoner and negative values favor the equal-budget Direct Caster. All nine cells pass the 20% tolerance, and the worst result is `+19.21%`. This closes the summon calibration gate without changing Power-only ability scaling or making Summon Power nonlinear.

Summon contribution telemetry provides the causal split:

- summon damage rises from roughly 51% of tier-1 short-fight output to roughly 94% of tier-10 long-fight output;
- Spirit-derived summon bonuses account for roughly 3% at tier 1 and 5.5% at tiers 5 and 10;
- explicit Summon Power/Health rolls account for roughly 29% to 32% at tier 1, 69% to 70% at tier 5, and 89% at tier 10;
- summon uptime is 100% in the isolated controls, while average active summons still exposes duration and concurrency behavior.

### Equal-budget peer matrix

The analyzer now runs 38 declared peer/context relationships at tiers 1, 5, and 10, producing 114 deterministic cells. Every result includes a stable ID, category, intent, scenario, benchmark context, candidate budget, tolerance, gains for both investments, difference in percentage points, pass/fail status, whether it blocks release, and averaged baseline/first/second combat outputs. The release gate requires all 108 acceptance cells to pass; six high-defense penetration cells remain diagnostic sensitivity probes.

The six categories are primary identity, offense, crit, defense, sustain, and penetration. Strict substitutes use a 10-percentage-point tolerance. Contextual generalist-versus-specialist relationships use 20 percentage points. Primary-versus-derived-basket controls use `0.1` percentage point because both sides must construct the same effective attributes.

Fortitude, Precision, and Spirit are compared with their complete derived baskets rather than with a single focused derived roll. The direct basket receives the exact points produced by the primary, and the candidate primary price is calculated from those same candidate derived costs. This prevents an invalid conclusion such as expecting equal budget in Spirit—a six-channel generalist—to equal the same budget concentrated entirely in Health Regeneration.

The first sustain pass exposed two real diagnostic problems:

1. Long Sustain did not give the reference character an authored damage ability, so equipment Life Steal had no eligible ability damage to convert into healing. The scenario now includes the common Power-scaling physical strike.
2. The initial tier-10 Health Regeneration profile produced `26.74%` gain versus `3.06%` for Life Steal, exceeding the approved 20-point contextual tolerance. Its cost was therefore raised to `1.75` at tier 10, interpolated from `1.50` at tier 5. The active replay produces `21.49%` versus `3.06%`, a passing `18.43`-point difference.

Because Spirit contains both Health Regeneration and Summon Power, its price is computed from the complete active basket. The resulting Spirit anchors are `1.05`, `1.0375`, and `1.0375`.

The frontier extension adds:

- Crit Chance versus Crit Damage at explicit `10 / 50`, `40 / 125`, and `65 / 200` chance/damage baselines;
- Max Health versus Armor, Dodge, Block, and Damage Reduction under the authored burst-pressure ability, in addition to steady pressure;
- flat physical and magical penetration at 25% and 300% of reference target defense.

Flat penetration's relative gain is higher against low defense and declines against high defense under the production formula. The 25% defense context is therefore the declared specialist release matchup; the 300% context is reported as sensitivity evidence and does not pretend that flat penetration is percentage penetration. All 108 current release cells pass.

The hand matrix separates representative funding and behavior from two controls:

The representative fast and slow weapon behaviors use the authored recipe values `0.75 / 0.78` and `1.25 / 1.18` for interval/damage multipliers. The slow-weapon damage multiplier was reduced from `1.22` after the equal-budget control demonstrated that slot funding did not cause the tier-10 inversion.

| Tier | Representative behavior, 90 ticks | Representative behavior, 180 ticks | Representative behavior, 600 ticks |
| ---- | --------------------------------: | ---------------------------------: | ---------------------------------: |
| 1    |                         `+12.16%` |                           `+6.02%` |                           `+8.14%` |
| 5    |                         `+18.07%` |                          `+16.31%` |                          `+13.55%` |
| 10   |                         `-18.81%` |                          `-17.90%` |                          `-18.12%` |

Positive values favor Dual Wield. All nine representative comparisons now pass the 20% tolerance, with at least `1.19` percentage points of headroom. Equal-budget comparisons with the representative cadence still expose the tier-10 weapon-behavior inversion, while equalizing both budget and behavior reduces every difference to at most roughly `1.21%`. This confirms that the production-consistent magnitude adjustment fixes the identified cause without changing hand-slot funding.

Aggregate cap telemetry confirms that this is broader than Attack Speed. It evaluates the final character value after all equipped items and primary-derived contributions, then compares it with the production combat cap. Equivalent wasted budget counts only direct equipment points above the cap, conservatively treating primary-derived points as useful first.

| Loadout           | Tier | Capped stat        | Total / effective cap | Direct equipment waste | Wasted target budget |
| ----------------- | ---: | ------------------ | --------------------: | ---------------------: | -------------------: |
| Medium Dual Wield |    5 | Crit Chance        |         `126.57 / 75` |                `51.57` |              `8.20%` |
| Two-Handed Damage |    5 | Crit Chance        |         `100.25 / 75` |                `25.25` |              `4.21%` |
| Heavy Shield      |   10 | Block Chance       |            `190 / 50` |                  `140` |              `7.37%` |
| Medium Dual Wield |   10 | Crit Chance        |         `471.23 / 75` |               `245.10` |             `10.00%` |
| Medium Dual Wield |   10 | Attack Speed       |        `600.77 / 200` |               `400.77` |             `12.26%` |
| Cloth Support     |   10 | Cooldown Reduction |         `158.33 / 40` |               `118.33` |              `7.47%` |
| Two-Handed Damage |   10 | Crit Chance        |         `386.77 / 75` |               `245.62` |             `10.51%` |
| Summoner          |   10 | Cooldown Reduction |         `157.10 / 40` |               `117.10` |              `7.52%` |

Cloth Support also exceeds the Cooldown Reduction cap by `0.625` points at tier 5, but its `0.15%` equivalent waste is below the 1% gate threshold. Seven canonical loadouts fail the aggregate-cap gate. Per-item redistribution succeeds in spending their budgets, but it cannot prevent several individually legal items from producing a wasteful whole-character total. The next profile revision must add recipe/loadout-aware allocation constraints or escalating cost bands for capped percentage stats.

The analyzer-only candidate allocator removes all aggregate excess while preserving full target spend. It moves the following budget away from capped channels:

| Loadout           | Tier | Budget redistributed |
| ----------------- | ---: | -------------------: |
| Medium Dual Wield |    5 |             `386.19` |
| Cloth Support     |    5 |               `3.75` |
| Two-Handed Damage |    5 |             `165.91` |
| Heavy Shield      |   10 |             `700.00` |
| Medium Dual Wield |   10 |           `3,892.98` |
| Cloth Support     |   10 |             `710.00` |
| Two-Handed Damage |   10 |           `2,048.79` |
| Summoner          |   10 |             `702.60` |

The most important tier-10 candidate share changes are:

- Medium Dual Wield: Precision `25% → 3.38%`, Crit Chance `10% → 1.35%`, Attack Speed `15% → 5.56%`, with the released budget moving mainly to Power, Weapon Damage, and Crit Damage;
- Heavy Shield: Block Chance `10% → 2.63%`;
- Cloth Support: Cooldown Reduction `10% → 2.53%`;
- Summoner: Cooldown Reduction `10.08% → 2.57%`;
- Two-Handed Damage: Precision `15.77% → 2.62%` and Crit Chance `10.51% → 1.74%`.

These exact whole-loadout targets remain a diagnostic reference. Production must allocate each item independently, so it uses conservative slot shares based on the largest supported combat loadout and redirects blocked budget without requiring knowledge of the character that will eventually equip the item.

### Recipe-catalog production constraints

Balance version 4 uses an item-level form of the aggregate-safe allocator. For every capped combat stat, each item receives a conservative share of the character's remaining capacity based on its slot-budget weight relative to the largest supported loadout. A shared linear constraint counts both direct rolls and primary-derived contributions against that capacity. For example, Precision and direct Crit Chance consume the same Crit Chance allowance.

The production allocator uses the real recipe profile first. When all remaining recipe stats are blocked by a combat or allocation cap, it spends the residual budget through a small role-specific overflow group:

- heavy armor and tower shields redirect toward Fortitude, Max Health, Armor, and Resistance;
- physical weapons redirect toward Power, Weapon Damage, Crit Damage, and Armor Penetration;
- magical weapons redirect toward Power, Spirit, Magic Penetration, and Health Regeneration;
- cloth, other armor, off-hands, and jewelry use corresponding sustain, resilience, or general-purpose groups.

Per-item allocation caps scale with slot-budget weight. This lets a two-handed weapon or chest piece accept the larger roll its funding implies while the independent whole-character combat constraint remains authoritative. `EquipmentConstraintProfile.ProductionActive` is `true`.

The catalog validator currently covers:

| Catalog measure                                |  Result |
| ---------------------------------------------- | ------: |
| Enabled equipment recipes                      |    `31` |
| Enabled blueprints                             |    `11` |
| Compatible base/blueprint designs              |   `225` |
| Full loadouts across tiers 1, 5, and 10        | `3,744` |
| Production loadouts over a combat cap          |     `0` |
| Exact-reference loadouts over a combat cap     |     `0` |
| Production loadouts unable to spend budget     |     `0` |
| Exact-reference loadouts unable to spend budget |     `0` |

The active constrained path produces no over-cap or unspent-budget failures across the full matrix.

This result establishes that recipe-level constraints preserve funding while avoiding capped channels across the present catalog. Equal-budget, summon, representative hand, real-content peer, and aggregate-cap calibration all pass under the active profile.

The current gate result is:

```text
overflow redistribution: PASS
production aggregate use: PASS
exact-reference use:      PASS
equal-budget peers:      PASS (108 release cells)
summon calibration:      PASS
hand calibration:        PASS
real crafting peers:     PASS (21 release cells)
maximum progression:     PASS (1,248 loadouts, 0 capped, 0 unspent)
active profile:          PASS
```

Equipment balance version 4 is active.

### Deterministic cap-overflow allocation

The shared allocator uses weighted water filling:

1. calculate each eligible stat's remaining capacity in budget units;
2. distribute the available budget according to profile weights;
3. when a stat would exceed its cap, fund it only to the cap and remove it from the active pool;
4. redistribute the remaining budget across the remaining stats in stable `AttributeType` order;
5. report budget as unspent only when every eligible stat is capped.

Crafted rolls use continuous constrained allocation. Preview ranges call the same allocator at their minimum and maximum budgets. Quality upgrades allocate incremental budget against existing proportions and remaining coupled capacity. Directed tempering improvements also test direct, slot-scaled, and primary-derived caps before changing a stat. This keeps generation, preview, and later quality growth consistent.

## Pre-version-3 system audit

The following system map and failure analysis capture the baseline that motivated this work. Statements about inert combat paths, duplicate hand aggregation, frontend-local weights, and non-Power ability scaling describe the pre-version-3 implementation; the implementation-status section above records what has since changed.

### Attribute definitions and base values

The canonical enum is:

- `LL/src/Core/Domain/Models/Attributes/AttributeType.cs`

It defines 24 attributes:

- Primary: Power, Fortitude, Precision, Spirit
- Offense/defense inputs: Max Health, Weapon Damage, Armor, Resistance, Crit Chance, Crit Damage, Armor Penetration, Magic Penetration
- Defensive secondary stats: Dodge Chance, Block Chance, Damage Reduction
- Recovery: Healing Power, Health Regeneration, Life Steal
- Utility: Cooldown, Status Resistance, Crowd Control Resistance
- Summons: Summon Power, Summon Health
- Cadence: Attack Speed

`AttributeCatalog` currently gives only a description and `IsContentFacing`. It does not define:

- unit;
- valid range;
- soft or hard cap;
- stacking semantics;
- combat formula;
- equipment eligibility;
- which scenarios make the stat useful;
- whether the stat is fully implemented.

New characters are created by `EntityBaseAttributeHelper` with:

| Attribute           | Character base |
| ------------------- | -------------: |
| Power               |             10 |
| Fortitude           |             10 |
| Precision           |             10 |
| Spirit              |             10 |
| Max Health          |            100 |
| Crit Damage         |            100 |
| Health Regeneration |       `2 HP/5s` |
| Everything else     |              0 |

There is no general character-level attribute growth in the inspected attribute path. Equipment and Essences are therefore major sources of combat-stat growth.

There is also a unit inconsistency between player and creature baselines:

- player Crit Damage starts at `100`;
- creature Crit Damage starts at `1.5`;
- creature Crit Chance starts at `0.05`;
- the frontend formats Crit Chance and Crit Damage as percentage points;
- the combat engine does not currently use either value, so the inconsistency is hidden.

Before critical strikes are enabled, their unit must be standardized and existing content converted.

### Modifier aggregation

`AttributeCalculator` applies modifiers in this order:

```text
round_to_nearest_midpoint_toward_zero(
    (base + sum(flat))
    * (1 + sum(additive_percentage) / 100)
    * product(1 + multiplicative_percentage / 100)
)
```

The result is clamped to a minimum of zero.

This ordering is sensible and can be retained, but balance calculations must distinguish modifier types:

- `+100 Max Health` is not the same as `+100% Max Health`;
- `+10 Armor` is not the same as `+10% Armor`;
- multiplicative modifiers change value based on all prior flat and additive values.

The current Angular item Power score ignores `ModifierType` and multiplies every positive `amount` by one flat weight. It therefore cannot correctly score additive or multiplicative modifiers.

Equipment and Essence aggregation follow different paths:

- equipment base and instance modifiers are projected into `BaseCombatAttributes`;
- Essence modifiers are attached as temporary modifiers and applied when combat attributes are initialized;
- ability/status `ModifyAttribute` effects adjust the runtime attribute by a flat amount, regardless of the normal modifier pipeline.

That last behavior should be made explicit in the ability schema. If ability effects are intended to support percent changes, the effect needs a modifier type rather than silently treating every value as flat.

### Equipment model

Each equipment instance combines:

- static `EquipmentBase.AttributeModifiers`;
- generated `EquipmentInstance.InstanceModifiers`;
- an implicit rarity multiplier called `Boost` that applies only to base modifiers;
- quality, tier, Potential, tempering state, recipe, blueprint, and affinity metadata;
- weapon behavior from the crafting recipe/blueprint;
- weapon behavior now comes only from crafting recipe behavior and combat attributes; the disconnected EquipmentBase weapon fields have been removed.

The effective modifier collection is:

```text
ceil(base modifier * rarity Boost) + instance modifiers
```

Rarity Boost ranges from `1.0x` at Common to `6.0x` at Legacy.

This causes several balance concerns:

1. Base modifiers and rolled modifiers do not share one visible budget ledger.
2. Rarity scales base modifiers but not instance modifiers.
3. Tempering rarity progression can both increase base modifiers implicitly and add/increase an instance modifier.
4. A crafted item receives its old static item-base modifiers in addition to its new recipe-generated stats.
5. Two items with equal generated budgets can have very different total budgets because their item bases differ.

The recommended end state is one explicit, auditable budget ledger for every combat modifier on an item.

### Equipment slots and hand economy

There are nine slots:

- Head, Chest, Legs
- Necklace, Ring, Relic
- Main Hand, Off Hand
- Tool

One-handed equipment can occupy either hand. A two-handed item occupies both hand slots. `CombatEntity` correctly deduplicates equipped instances by ID before combat aggregation.

`AttributeCalculator.CalculateBaseAttributes(Entity)`, used for the character overview, does not deduplicate equipment instances before selecting modifiers. A two-handed item stored in both hand slots can therefore be counted twice in the overview while it is counted once in combat. This should be fixed before relying on overview stats or item comparisons.

Current default slot budget weights include:

| Configuration              | Combined weight |
| -------------------------- | --------------: |
| Two-handed                 |            1.70 |
| One-handed + off-hand item |            1.50 |
| Two one-handed items       |            1.70 |

Two-handed and dual-wield configurations therefore receive equal hand-slot funding. Their authored attack cadence, damage multiplier, stat profiles, off-hand aggregation, and main-hand behavior are measured together by the representative and real-content combat matrices.

### Crafting budget

Crafted base stats use:

```text
budget =
    TierPowerBudget[tier]
    * SlotBudgetWeight[equipmentType]
    * QualityStatMultiplier[quality]

stat points =
    round(budget * profileShare * randomVariance / CostPerPoint[stat])
```

The result is clamped to at least 1 and to a per-stat hard cap. Variance is 95% to 105%.

Tier budget rises from `100` at tier 1 to `1,520` at tier 10. Quality ranges from `0.90x` to `1.12x`.

The 31 current base recipes all allocate exactly 100% of their generated budget across four attributes. Their most common generated stats are:

| Attribute                                        | Recipe count |
| ------------------------------------------------ | -----------: |
| Precision                                        |           21 |
| Max Health                                       |           13 |
| Weapon Damage                                    |           11 |
| Spirit                                           |            8 |
| Attack Speed                                     |            8 |
| Armor, Fortitude, Armor Penetration, Crit Chance |       7 each |

Many of those frequently generated stats are either inert or only conditionally useful in current combat.

Tempering uses the same `EquipmentStatBudgetCatalog` to:

- convert current points back into budget;
- compare current stat share to desired profile share;
- select a stat with a weighted deficit;
- convert a small tier-based roll budget back into points;
- enforce stat hard caps.

This reuse is good. Replacing the catalog with an evidence-backed valuation provider can improve both initial rolls and tempering without replacing the whole crafting system.

### Three separate meanings of “Power”

The code currently has three different concepts:

1. **Crafting budget cost**
   - Backend `EquipmentStatBudgetCatalog`
   - Example: Max Health costs `0.2` budget per point and Armor costs `1.2`.

2. **Equipment tooltip Power**
   - Angular `ATTRIBUTE_WEIGHTS` in `equipment-display.ts`
   - Example: Max Health is multiplied by `1.8` and Armor by `4`.
   - Modifier type is ignored.

3. **Character Power Rating**
   - `PowerRatingService` and `PowerAnalysisSimulationRunner`
   - Determined through deterministic battles using `FastCombatEngine`.
   - Measures Overall, single target, area, physical durability, magical durability, sustain, and control.

Only the third system observes actual combat. It is the correct foundation for measuring build strength, but it should not be called for every tooltip render or crafting roll.

The first system should become a versioned, simulator-calibrated approximation. The second should be removed or replaced by server-calculated item budget and comparison data.

## Pre-version-3 combat influence by attribute

The following table describes production behavior, not intended behavior.

| Attribute                    | Current production influence                                                                                                                     | Status                                   |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------- |
| Power                        | Basic attack damage is `round((1 + Power / 10) * weapon damage multiplier)`. Eighteen authored effects also scale explicitly from Power.         | Functional, very strong baseline         |
| Fortitude                    | Three authored effects scale explicitly from it. No universal toughness conversion.                                                              | Build/content dependent                  |
| Precision                    | Four authored effects scale explicitly from it. It does not affect hit chance or crit chance.                                                    | Build/content dependent                  |
| Spirit                       | Ten authored effects scale explicitly from it, mainly enabling magical/support identity.                                                         | Build/content dependent                  |
| Max Health                   | Sets maximum and initial health. Runtime increases also grant the added health; decreases clamp current health.                                  | Functional                               |
| Weapon Damage                | No general basic-attack or ability formula reads it.                                                                                             | Inert                                    |
| Armor                        | Not read during damage resolution.                                                                                                               | Inert                                    |
| Resistance                   | Not read during damage resolution.                                                                                                               | Inert                                    |
| Crit Chance                  | No general critical roll exists in `FastCombatEngine`.                                                                                           | Inert                                    |
| Crit Damage                  | No general critical multiplier exists in `FastCombatEngine`.                                                                                     | Inert                                    |
| Armor Penetration            | Not read during damage resolution.                                                                                                               | Inert                                    |
| Magic Penetration            | Not read during damage resolution.                                                                                                               | Inert                                    |
| Dodge Chance (`DodgeChance`) | Eligible melee/ranged attacks are fully avoided using direct percentage points clamped from 0% to 100%.                                          | Functional                               |
| Block Chance                 | Not read during damage resolution.                                                                                                               | Inert                                    |
| Damage Reduction             | Reduces every damage type linearly: `damage * (1 - DR / 100)`, clamped from -100% to 100%. At 100 it grants immunity.                            | Functional but dangerously uncapped      |
| Healing Power Percent        | Does not modify healing or barrier effects.                                                                                                      | Inert                                    |
| Health Regeneration          | Restores the listed amount every five seconds (50 combat ticks).                                                                                  | Functional, duration dependent           |
| Life Steal                   | The attribute is not read. Individual ability effects can carry their own fixed `LifeStealPercentage`.                                           | Attribute inert                          |
| Cooldown                     | Does not change active or trigger cooldowns.                                                                                                     | Inert                                    |
| Status Resistance            | Does not affect status application chance, stacks, or duration.                                                                                  | Inert                                    |
| Crowd Control Resistance     | Does not affect stun application or duration.                                                                                                    | Inert                                    |
| Summon Power                 | Can be selected as a summon attribute's explicit scaling source. The one current summon uses it.                                                 | Functional for compatible summon content |
| Summon Health                | Can be selected as a summon attribute's explicit scaling source. The one current summon uses it.                                                 | Functional for compatible summon content |
| Attack Speed                 | Changes basic-attack rate using `(1 + AttackSpeed / 100) / intervalMultiplier`, clamped to 0.25x through 4x. It does not speed active abilities. | Functional                               |

Additional combat observations:

- Damage Type is logged and used by content selection, but Armor/Resistance mitigation is not selected from it.
- Attack Type currently matters primarily for Dodge and event triggers.
- Bleed, Burn, and Poison do not have distinct mitigation behavior in the damage resolver.
- Barriers absorb already-reduced damage and have no explicit cap.
- Healing and barriers use `BaseValue + ScalingAttribute * ScalingCoefficient`.
- Ability cooldowns are authored ticks. The Cooldown attribute is not consulted.
- Weapon recipe behavior can change basic-attack interval, damage multiplier, attack type, and damage type.
- Equipment tooltips and crafting previews no longer expose disconnected weapon magnitude, interval, or scaling fields.

## Why the pre-version-3 weights could not be trusted

### A simple 100-budget comparison

At current catalog prices, putting 100 crafting budget into one stat yields approximately:

| Attribute        | Points from 100 budget | Effect at a fresh character baseline                  |
| ---------------- | ---------------------: | ----------------------------------------------------- |
| Power            |                    100 | Basic attack rises from 2 to 12 damage: about 6x      |
| Max Health       |                    500 | Health rises from 100 to 600: 6x raw health           |
| Armor            |                     83 | No change in damage taken                             |
| Resistance       |                     83 | No change in damage taken                             |
| Damage Reduction | 16.7 percentage points | About 1.20x effective health                          |
| Dodge Chance     |   20 percentage points | About 1.25x expected health against dodgeable attacks |
| Attack Speed     | 33.3 percentage points | About 1.33x basic-attack DPS only                     |
| Crit Chance      |   25 percentage points | No change                                             |
| Weapon Damage    |                   66.7 | No change                                             |

This is not a proposed benchmark; it only demonstrates current magnitude.

The Max Health versus Damage Reduction comparison also shows why one global constant is insufficient. Spending 100 budget gives 500 Max Health or 16.7% Damage Reduction:

- at 100 current health, 500 Max Health is overwhelmingly stronger;
- at 2,500 current health, the two are roughly comparable in raw effective-health gain;
- at still higher health, percentage mitigation becomes stronger.

Therefore, Max Health cost must be calibrated against tier-appropriate reference health, and percentage defenses need diminishing returns or budget bands. No fixed ratio is universally fair across all progression.

### Existing intended armor math is disconnected

`ArmorDamageReductionConstants.CalculateEffectiveDefense` contains an exponential curve based on defense and level difference. Nothing calls it.

It should not simply be wired in without calibration:

- exponential mitigation causes effective health to grow exponentially with defense;
- its `K` depends on level difference rather than expected stat scale;
- the result is rounded to two decimals, which can create stepwise value;
- it does not account for penetration or damage-type mapping;
- its value relative to Max Health still depends on the character's current health.

The desired mitigation equation should be selected as part of the balance model, tested, and then used consistently by production and simulations.

### Threshold-based whole-build Power is necessary but not sufficient

The existing Power Rating is a strong foundation because it:

- uses the production engine;
- uses deterministic seeds;
- tests multiple scenarios;
- measures the full interaction between abilities, equipment, and Essences.

However, using only “highest benchmark intensity passed” produces discrete thresholds. A small stat change may be valuable without crossing the next threshold, producing a measured delta of zero.

The attribute analyzer should reuse the same production simulations but collect continuous metrics in addition to pass/fail:

- damage per tick;
- time to kill;
- survival ticks;
- ending-health fraction;
- healing and barrier generated;
- hostile actions prevented;
- win probability across seeds;
- benchmark intensity when a threshold is crossed.

This gives stable marginal measurements while keeping final character Power simulation-backed.

## Target design principles

1. **Production combat is the source of truth.** No balance formula may award value to behavior the combat engine does not execute.
2. **Every equipment-eligible stat must have a tested mechanic.** Inert stats fail content validation and cannot appear on new items.
3. **Item budget and build impact are different.** Item budget is stable; build impact depends on the wearer, enemy, abilities, and encounter duration.
4. **Weights are derived, not guessed.** Hand-authored values may seed the first run but must be replaced by analyzer output.
5. **Valuation is scenario-aware.** Armor may be valuable against physical damage and worthless against true damage; that is identity, not necessarily imbalance.
6. **No mandatory stat may be universally dominant.** General-purpose stats can be slightly less efficient than conditional specialists, but not orders of magnitude stronger.
7. **Diminishing returns must be explicit.** Percentage avoidance, mitigation, cooldown, attack speed, and life steal cannot scale linearly to immunity or infinite throughput.
8. **Units must be unambiguous.** A stored value of `5` must consistently mean either 5 percentage points, 5 rating, or 5 flat points.
9. **The balance profile is versioned.** Combat or valuation changes invalidate analysis outputs and dungeon Power calibration.
10. **Balance tooling is deterministic and repeatable.** The same version, content, build matrix, and seeds must produce the same report.
11. **Power is the sole ability-magnitude scaler.** Damage, healing, barriers, damage over time, recovery, and other numeric ability outputs use Power rather than selecting different primary attributes.
12. **Each primary attribute has one explicit identity.** Power drives ability output, Fortitude drives toughness, Precision drives offensive reliability/tempo, and Spirit drives sustain/support/summons. Primary attributes never recursively scale one another.
13. **Healing can crit by default.** An authored healing effect inherits critical-strike eligibility unless it explicitly opts out.
14. **Cooldown Reduction caps at 40%.** No combination of attributes, equipment, or modifiers can reduce an affected cooldown by more than 40% of its authored duration.

## Proposed combat influence model

The formulas below are a recommended starting model. Constants such as `KArmor`, caps, and primary-stat conversion coefficients are calibration parameters, not values to finalize by intuition.

### Standard units

Use these conventions:

- flat resources and ratings: raw points;
- chances shown to players: percentage points, but preferably stored internally as rating when diminishing returns are required;
- output modifiers: percentage points;
- Attack Speed: haste rating expressed as percentage points;
- Cooldown Reduction: direct percentage points, additively combined and hard-capped at 40%;
- Crit Damage: bonus percentage above a normal hit, with one canonical default;
- durations: combat ticks, with UI conversion to seconds.

Add unit, floor, soft cap, hard cap, and display metadata to the attribute definition.

### Base and primary attributes

Recommended primary identities:

| Primary attribute | Identity                                          | Initial derived targets                                                                                      |
| ----------------- | ------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Power             | Universal ability and basic-attack magnitude      | No derived-stat bundle; it is consumed directly                                                              |
| Fortitude         | Durable tank foundation                           | Max Health, Armor, Resistance                                                                                |
| Precision         | Offensive reliability and tempo                   | Crit Chance rating, Armor Penetration, Magic Penetration, a modest Attack Speed contribution                 |
| Spirit            | Sustain, support, control resilience, and summons | Healing Power, Health Regeneration, Status Resistance, Crowd Control Resistance, Summon Power, Summon Health |

These are deliberately limited mappings. Dodge, Block, general Damage Reduction, Crit Damage, Life Steal, Cooldown Reduction, and Weapon Damage remain independently itemized stats rather than being granted by a primary attribute. That preserves valuable specialist affixes and prevents one primary stat from becoming a complete build by itself.

Resolve primary-derived contributions in one versioned profile:

```text
PrimaryAttributeInfluenceDefinition
    PrimaryAttribute
    DerivedAttribute
    ContributionPerPoint
    OptionalSoftCap
```

The coefficients are calibration outputs, not constants to guess. Directly purchasing a derived stat should normally be slightly more efficient than receiving it through a flexible primary-stat bundle. A starting target is a 5% to 15% versatility tax on the primary bundle.

#### Power

- Continues to scale basic attacks.
- Is the only attribute used to scale ability magnitude.
- Applies consistently to damage, healing, barriers, damage-over-time ticks, resource restoration, and any other numeric output produced by an ability.
- Uses an authored coefficient per effect so different abilities can have different Power efficiency without changing scaling attributes.
- Must not also apply an unlisted global output multiplier, which would double-dip.

Canonical ability formula:

```text
abilityMagnitude =
    authoredBaseValue
    + effectivePower * authoredPowerCoefficient
```

Rules:

- `ScalingAttribute` on magnitude-bearing ability effects must be absent or `Power`.
- Prefer renaming `ScalingCoefficient` to `PowerCoefficient` once content has migrated.
- Damage-over-time uses the same Power formula per tick. Preserve the current dynamic behavior—reading the source's effective Power when a tick resolves—unless snapshotting is deliberately introduced as a separate combat-rule change.
- Chance, duration, target count, status stacks, and proc coefficient remain authored mechanics rather than gaining implicit Power scaling.
- Power determines the raw magnitude; typed mitigation, crit, Healing Power, barriers, life steal, and other downstream mechanics apply afterward.

Candidate basic-attack formula:

```text
rawBasicDamage =
    max(1, effectiveWeaponDamage + effectivePower * BasicAttackPowerCoefficient)
    * weaponBehavior.DamageMultiplier
```

The current `1 + Power / 10` formula can be reproduced by initial constants if migration stability is needed.

#### Fortitude

Fortitude needs a universal toughness benefit so it has a clear combat role without being used as an ability coefficient.

Candidate:

```text
effectiveMaxHealth =
    modifiedMaxHealth
    + Fortitude * HealthPerFortitude

effectiveArmor =
    modifiedArmor
    + Fortitude * ArmorPerFortitude

effectiveResistance =
    modifiedResistance
    + Fortitude * ResistancePerFortitude
```

Fortitude's three contributions form one budgeted bundle; each coefficient must be smaller than buying that derived stat directly. Fortitude does not add Block or general Damage Reduction because those multiplicative defenses would make its Max Health and typed-defense synergy too self-contained. Defensive abilities still derive their numeric output from effective Power.

#### Precision

Precision should support reliable offense without making direct Crit Chance redundant.

Candidate:

```text
effectiveCritChance =
    clamp(
        modifiedCritChance
        + Precision * CritChancePerPrecision,
        0,
        75%)

effectiveArmorPenetration =
    modifiedArmorPenetration
    + Precision * ArmorPenetrationPerPrecision

effectiveMagicPenetration =
    modifiedMagicPenetration
    + Precision * MagicPenetrationPerPrecision

effectiveAttackSpeed =
    modifiedAttackSpeed
    + Precision * AttackSpeedPerPrecision

```

The Attack Speed coefficient should be deliberately modest; Precision's main identity is reliable offense, not becoming the only cadence stat. Precision does not add Crit Damage. If accuracy is later introduced, use Precision against a separate evasion rating; do not silently make attacks miss based only on level. Precision is not an ability coefficient.

#### Spirit

Spirit is the primary attribute for sustain, support, resisting hostile effects, and summons. It is valuable to healers, barrier/support builds, attrition builds, summoners, and tanks facing control or status pressure without becoming a universal multiplier.

Candidate:

```text
effectiveHealingPower =
    modifiedHealingPower
    + Spirit * HealingPowerPerSpirit

effectiveHealthRegeneration =
    modifiedHealthRegeneration
    + Spirit * HealthRegenerationPerSpirit

effectiveStatusResistance =
    modifiedStatusResistance
    + Spirit * StatusResistancePerSpirit

effectiveCrowdControlResistance =
    modifiedCrowdControlResistance
    + Spirit * CrowdControlResistancePerSpirit

effectiveSummonPower =
    modifiedSummonPower
    + Spirit * SummonPowerPerSpirit

effectiveSummonHealth =
    modifiedSummonHealth
    + Spirit * SummonHealthPerSpirit
```

Spirit does not increase Power, Max Health, Armor, Resistance, Precision, Crit, penetration, Attack Speed, Damage Reduction, Dodge, Block, Life Steal, Cooldown Reduction, or Weapon Damage.

This narrower identity avoids several dangerous double-dips:

- Spirit does not increase Power and Healing Power simultaneously.
- Spirit does not increase both Max Health and every form of recovery.
- Spirit does not improve summon output through both owner Power and Summon Power.
- Spirit does not become mandatory for every damage build through Cooldown Reduction.

Spirit-derived percentage/rating stats still use their normal caps and diminishing-return curves. Summon and recovery coefficients should be calibrated across support, solo, summon, and no-healing builds. Spirit should be efficient in its target archetypes and intentionally weak in a pure damage build with no sustain or summon mechanics.

Runtime primary-stat changes invalidate only their dependency group:

- Fortitude recalculates Max Health, Armor, and Resistance and safely synchronizes current health.
- Precision recalculates Crit Chance, both penetration stats, and Attack Speed.
- Spirit recalculates Healing Power, Health Regeneration, both resistance-to-status stats, and both summon stats.
- Power affects future ability/basic-attack calculations directly and does not trigger a derived-stat rebuild.

Primary attributes never add to another primary attribute, so there is no recursive primary-stat scaling. Keep intermediate calculations as floating point and round only at the final attribute/mechanic boundary.

### Offense

#### Weapon Damage

- Must be part of basic attacks.
- Does not scale ability magnitude; abilities use Power.
- Can still affect explicitly non-ability weapon mechanics introduced later, but those mechanics must not masquerade as ability scaling.
- Weapon Damage and recipe behavior are the only equipment-side basic-attack sources.

#### Critical strikes

Recommended rules:

```text
critChance = clamp(CritChance + PrecisionContribution, 0, 75%)
critMultiplier = 1 + CritDamageBonus / 100
```

- Represent eligibility with an explicit three-state policy such as `CritEligibility.Default`, `CritEligibility.Allowed`, and `CritEligibility.Disallowed`. Do not use an omitted Boolean whose absence can be interpreted as `false`.
- Resolve `Default` by operation type:

| Operation          | `Default` resolves to |
| ------------------ | --------------------: |
| Direct damage      |               Allowed |
| Direct healing     |               Allowed |
| Damage over time   |            Disallowed |
| Reflected damage   |            Disallowed |
| Life-steal healing |            Disallowed |
| Secondary proc     |            Disallowed |
| Barrier            |            Disallowed |

- A healing effect with no authored override can crit. Only `CritEligibility.Disallowed` suppresses its critical strikes.
- `CritEligibility.Allowed` can opt an otherwise-disallowed operation into crits when a specific ability requires it.
- Critical healing uses the normal crit chance and crit multiplier unless a future, explicitly justified mechanic introduces a separate healing-critical multiplier.
- Resolve a direct heal in this order: calculate its base plus Power coefficient, apply Healing Power and other permitted output modifiers, roll and apply the critical multiplier once, then clamp the restored amount to missing health.
- Emit `EventType.HealCrit` for successful critical heals so combat logs and `CombatStatsAggregator` distinguish them from normal heals. Overhealing must not inflate restored-health metrics.
- Store and display Crit Chance as direct percentage points for combat rules version 3. Replacing it with rating-based diminishing returns would require another combat-rule and balance-version increment.

#### Attack Speed

Keep it as basic-attack haste:

```text
attackRate =
    clamp(
        (1 + AttackSpeed / 100) / weaponBehavior.IntervalMultiplier,
        MinAttackRate,
        MaxAttackRate)
```

The effective useful Attack Speed ceiling is weapon-dependent:

```text
usefulAttackSpeedCap =
    max(0, MaxAttackRate * weaponBehavior.IntervalMultiplier - 1) * 100
```

At the current 4x maximum rate this is `200%` for a `0.75` fast weapon, `300%` for a neutral weapon, and `400%` for a `1.25` slow weapon. Attack Speed above that value has no combat effect. The profile must stop ordinary full loadouts before this aggregate ceiling or explicitly lower and redesign the general rate cap. Attack Speed does not reduce active ability cooldowns.

#### Armor and Magic Penetration

Use the same rating unit as their matching defense:

```text
effectiveArmor = max(0, Armor - ArmorPenetration)
effectiveResistance = max(0, Resistance - MagicPenetration)
```

Do not allow penetration to create bonus damage by making defense negative. Use a separate vulnerability mechanic for that purpose.

Penetration is inherently enemy-dependent. Its item cost must be derived across an enemy-defense matrix, not against a zero-defense target.

### Defense

#### Armor and Resistance

Use a hyperbolic curve:

```text
mitigation(defense, tier) =
    defense / (defense + KDefense(tier))
```

Then:

```text
physical damage:
    afterTypedDefense = raw * (1 - mitigation(effectiveArmor, tier))

magical damage:
    afterTypedDefense = raw * (1 - mitigation(effectiveResistance, tier))
```

Why hyperbolic:

- each additional point gives less displayed mitigation;
- effective health grows approximately linearly with defense at a fixed tier;
- it is easier to reason about and calibrate than exponential effective-health growth;
- `KDefense(tier)` can track expected stat scale so tier progression does not collapse the curve.

Recommended damage mapping:

| Damage type | Typed defense                                        | Dodge/block eligibility           |
| ----------- | ---------------------------------------------------- | --------------------------------- |
| Physical    | Armor                                                | Based on Attack Type              |
| Magical     | Resistance                                           | Based on Attack Type              |
| Burn        | Resistance unless explicitly tagged as true          | No dodge/block for periodic ticks |
| Poison      | No typed defense or a defined fraction of Resistance | No dodge/block for periodic ticks |
| Bleed       | No typed defense or a defined fraction of Armor      | No dodge/block for periodic ticks |

The exact ailment mapping is a design choice, but it must be explicit and covered by tests.

#### General Damage Reduction

Apply after typed defense and combine multiplicatively:

```text
damageTaken =
    afterTypedDefense
    * (1 - effectiveGeneralReduction)
```

Use a hard cap well below 100%, initially 30% to 40%, and a diminishing-rating curve or escalating equipment cost. General reduction protects against more scenarios than Armor or Resistance and should therefore be less budget-efficient than either specialist defense.

#### Dodge

- Applies only to eligible direct melee/ranged attacks.
- Does not apply to unavoidable, periodic, environmental, or true-damage effects.
- Uses diminishing returns and a hard cap.
- Full avoidance is powerful and stochastic; its expected value should be validated across many paired seeds and against burst-death risk.

#### Block

Give Block a distinct identity from Dodge:

- block triggers on eligible direct attacks;
- a block reduces damage by a configured severity, such as 50%, rather than avoiding the entire hit;
- shields or abilities can alter block severity;
- block chance and block severity are independently capped.

Expected reduction is approximately:

```text
blockChance * blockSeverity
```

but simulations must capture burst, barriers, on-block triggers, and healing interactions.

### Recovery and utility

#### Healing Power

```text
rawHealing =
    authoredBaseValue
    + effectivePower * authoredPowerCoefficient

rawBarrier =
    authoredBaseValue
    + effectivePower * authoredPowerCoefficient

healing = rawHealing * (1 + effectiveHealingPowerPercent / 100)
barrier = rawBarrier * (1 + effectiveHealingPowerPercent / 100)
```

The recommended simple rule is that Healing Power also affects barriers and is displayed as Recovery Power. If that identity is rejected, remove the barrier multiplier; do not add a separate barrier stat until enough content justifies it.

#### Health Regeneration

Expose Health Regeneration as health restored every five seconds:

```text
regenPerFiveSecondInterval = HealthRegeneration
```

Its value depends strongly on fight duration, incoming pressure, Max Health, and anti-heal, so the analyzer must include short, normal, and attrition scenarios.

#### Life Steal

Combine the character attribute with effect-specific life steal:

```text
effectiveLifeSteal =
    clamp(characterLifeSteal + effectLifeSteal, 0, LifeStealCap)

healing =
    postMitigationHealthDamage
    * effectiveLifeSteal
    * procCoefficient
```

- use actual health damage, not barrier damage or overkill;
- retain proc coefficients for area and periodic effects;
- prevent life-steal healing from recursively triggering damage/heal loops;
- apply healing modifiers exactly once.

#### Cooldown

Treat the current attribute as direct Cooldown Reduction, matching the existing player-facing label:

```text
effectiveCooldownReduction =
    clamp(totalCooldownReduction / 100, 0, 0.40)

effectiveCooldown =
    max(1, ceil(baseCooldown * (1 - effectiveCooldownReduction)))
```

All additive attribute, equipment, buff, and effect-specific Cooldown Reduction contributions enter `totalCooldownReduction` before the clamp. The 40% cap is on the resulting reduction, not on an individual source. Values above 40 provide no further cooldown reduction and therefore no marginal equipment value.

The cap guarantees that an affected ability retains at least 60% of its authored cooldown. Integer tick rounding may make the realized reduction slightly smaller, especially on short cooldowns; it must never make the reduction exceed 40%.

Apply it consistently to:

- active ability cooldown;
- internal trigger cooldown only if the design intends it;
- not to fixed periodic status cadence unless explicitly configured.

Snapshot the effective reduction when a cooldown starts. Later equipment or temporary-stat changes affect newly started cooldowns rather than retroactively rescaling time already remaining.

#### Status and Crowd Control Resistance

Separate ordinary debuffs from hard control:

```text
negativeStatusDuration =
    baseDuration / (1 + StatusResistance / 100)

hardControlDuration =
    baseDuration / (1 + CrowdControlResistance / 100)
```

- Status Resistance affects poison, bleed, burn, vulnerability, and non-control debuffs.
- Crowd Control Resistance affects stun/freeze/charm and other `Control.*` tags.
- Define a minimum duration of at least one tick.
- Do not use resistance as a binary application roll unless the game deliberately wants more randomness.
- Positive statuses are unaffected.

### Summons

Make summon attributes global owner-to-summon modifiers rather than relying solely on each summon template to remember explicit scaling:

```text
summonDamage *= 1 + SummonPower / 100
summonMaxHealth *= 1 + SummonHealth / 100
```

Explicit summon-template scaling can remain for special relationships, but the standard attributes must reliably affect all compatible summons. Add summon-duration and summon-cap scenarios to valuation so disposable and permanent summons are not priced identically by accident.

## Attribute valuation system

### 1. Versioned balance profile

Replace the static cost-only catalog with a versioned profile containing:

```text
AttributeBalanceDefinition
    Attribute
    Unit
    EquipmentEligible
    ImplementedInCombat
    Floor
    SoftCap
    PerItemHardCap
    EffectiveCharacterCap
    TierCostBands
    RelevantScenarios
    ModifierTypesAllowed
    DisplayPrecision
    Notes

PrimaryAttributeInfluenceDefinition
    PrimaryAttribute
    DerivedContributions[]
        DerivedAttribute
        ContributionPerPoint
        OptionalSoftCap
```

Each tier cost band maps a range of stat points to a budget cost per point. This allows:

- early Max Health to cost more when baseline health is low;
- Dodge and Damage Reduction to become increasingly expensive near caps;
- penetration to track tier-appropriate enemy defenses;
- low-value early points and high-value synergy bands to be represented without one misleading global scalar.

Store the authored/approved profile as versioned JSON, for example:

- `LL/src/API/API.LL/Data/balance/attribute-balance.json`

Validate it at startup in the same style as combat/crafting content. Domain models remain in Core; JSON loading and analysis remain in Infrastructure so dependency direction is preserved.

Price a primary attribute from the measured value of the complete bundle it produces, including interactions, rather than summing the standalone catalog prices of its derived attributes. The latter would miss Max Health/Armor synergy, Crit/Attack Speed synergy, and the conditional nature of Spirit's support and summon benefits.

### 2. Reference build matrix

Measure stats across a small but representative matrix:

#### Progression points

- Tier 1: fresh character
- Tier 3: early established build
- Tier 5: midgame
- Tier 8: late game
- Tier 10: endgame

#### Build archetypes

- basic-attack physical
- slow heavy physical
- precision/crit
- magical-damage Power ability
- sustain/healer
- tank/block
- summon
- balanced/generalist

#### Incoming encounter profiles

- physical direct pressure
- magical direct pressure
- mixed pressure
- damage-over-time/attrition
- burst
- control-heavy
- low-defense enemy
- high-Armor enemy
- high-Resistance enemy
- multiple targets

Keep the matrix deliberately small enough for a solo developer to run locally and in scheduled CI. Add a scenario only when it represents a real game archetype.

### 3. Paired perturbation experiments

For each reference build, scenario, attribute, and tier:

1. clone the exact baseline combatant;
2. add a small positive stat perturbation;
3. run baseline and perturbed builds with the same seed;
4. repeat across a fixed seed set;
5. measure continuous and threshold outcomes;
6. test multiple perturbation sizes and multiple starting values.

Use central differences when the stat permits:

```text
marginalValue(stat, x) =
    (utility(x + delta) - utility(x - delta)) / (2 * delta)
```

Use forward differences near zero or a floor.

Testing several starting values is essential for:

- crit synergies;
- penetration versus enemy defense;
- percentage mitigation versus current Max Health;
- regen versus fight duration;
- cooldown breakpoints;
- Attack Speed and tick rounding;
- caps and diminishing returns.

### 4. Continuous outcome vector

Collect at least:

- win/loss/draw;
- combat duration;
- friendly survival ticks;
- ending-health fraction;
- damage dealt and taken;
- healing and barrier generated;
- damage prevented by Armor, Resistance, Block, Dodge, and general reduction;
- basic attacks and active abilities resolved;
- hostile actions prevented;
- summon damage, survival, and uptime;
- existing scenario Power threshold.

Damage-prevention telemetry must identify the prevention source. Without that attribution it will be difficult to explain why Armor is valuable or to detect a reduction being applied twice.

### 5. Utility calculation

Do not add raw DPS, health, and duration values together. Normalize changes as ratios:

```text
offenseDelta = log(perturbedDps / baselineDps)
durabilityDelta = log(perturbedSurvival / baselineSurvival)
sustainDelta = log(perturbedSustain / baselineSustain)
```

For each scenario, define which dimensions matter. Examples:

- single-target offense emphasizes DPS and time to kill;
- physical durability emphasizes survival and ending health under matched physical pressure;
- sustain emphasizes survival, net health loss, and healing over a long horizon;
- Overall uses a weighted geometric mean of offense and durability rather than letting one extreme compensate infinitely for the other.

Aggregate a stat's result with:

- median across seeds;
- weighted median or trimmed mean across relevant scenarios;
- low/high percentile range to expose matchup sensitivity.

Do not average a stat across scenarios where it is intentionally irrelevant. Armor should be priced from physical/mixed relevance, then charged a versatility discount relative to universal defense because it does not protect against everything.

### 6. Deriving cost per point

Choose a reference stat and normalize its approved cost to `1.0`, initially Power if its combat formula is stable.

For stat `s`:

```text
relativeCostPerPoint(s) =
    marginalUtilityPerPoint(s)
    / marginalUtilityPerPoint(referenceStat)
```

A point that creates twice as much measured utility costs twice as much budget.

Generate separate values by tier and stat band. Smooth adjacent results so random simulation noise does not create jagged item rolls. Require a minimum sample confidence before publishing a value.

Then replay equal-budget bundles:

```text
100 budget of Max Health
100 budget of Armor
100 budget of Damage Reduction
100 budget of Dodge
```

The bundles should produce comparable median benefit in their relevant scenarios, within a target tolerance such as 10%, while maintaining their intended specialist/generalist differences.

### 7. Interaction and dominance analysis

Single-stat perturbations miss combinations. Test pairwise interactions for:

- Max Health × Armor
- Max Health × Resistance
- Max Health × Damage Reduction
- Armor × Block
- Crit Chance × Crit Damage
- Precision × Crit Chance
- Fortitude × Max Health, Armor, and Resistance
- Precision × Crit Chance, penetration, and Attack Speed
- Spirit × Healing Power, regeneration, status/control resistance, and summon stats
- Attack Speed × on-hit effects
- Power × ability coefficients
- Weapon Damage × Attack Speed
- Healing Power × cooldown
- Life Steal × damage
- Summon Power × summon count/duration

For a pair `(a, b)`:

```text
interaction =
    utility(a + b)
    - utility(a)
    - utility(b)
    + utility(baseline)
```

Large positive interactions justify:

- escalating cost bands;
- lower caps;
- recipe budget-share caps;
- or retaining the synergy as an intentional build reward while reducing standalone efficiency.

Also run dominance checks: for every two attributes available to the same recipe role, confirm there is no realistic range where one is strictly better in all relevant scenarios at the same budget.

### 8. Runtime use

Do not run hundreds of simulations during crafting or tooltip rendering.

Use the analyzer offline or through an Admin diagnostics command to produce an approved balance profile. Runtime systems consume the profile:

- `ItemStatRollService` converts budget to points;
- `TemperingMechanicsService` calculates budget shares and increments;
- crafting preview shows accurate ranges and budget;
- an equipment budget evaluator totals flat/additive/multiplicative modifiers correctly;
- content validation rejects unknown, inert, over-cap, or disallowed stats;
- the frontend receives server-owned budget/comparison values.

Whole-character Power continues to use the existing production simulations.

## Equipment-system changes

### One authoritative budget evaluator

Add a service that can evaluate:

- a flat modifier against the tier reference value;
- an additive modifier against the tier reference base;
- a multiplicative modifier as an incremental multiplier;
- stat cost bands;
- caps;
- weapon behavior budget;
- total item budget and budget by stat.

Suggested conceptual API:

```csharp
EquipmentBudgetBreakdown Evaluate(
    EquipmentInstance item,
    AttributeBalanceProfile profile);

float SpendBudget(
    AttributeType attribute,
    double budget,
    int tier,
    float startingValue = 0);
```

All crafting and tempering conversions should call this service rather than reading a static scalar directly.

### One budget ledger per item

The item breakdown should contain:

- template/base budget;
- generated stat budget;
- quality bonus budget;
- tempering-earned budget;
- behavior budget;
- total;
- unpriced or invalid modifiers.

Avoid implicit `Boost` multiplication as the long-term model. Prefer materialized, inspectable instance modifiers or a clearly valued rarity budget multiplier that applies consistently.

This local-only development environment uses one canonical equipment profile. Equipment instances do not persist historical balance versions, and all existing and newly generated equipment is evaluated with the active profile.

### Price weapon behavior

Recipe behavior changes real combat without appearing in the stat budget. Give behavior an equivalent budget adjustment:

- basic-attack damage multiplier;
- interval multiplier;
- melee/ranged delivery;
- physical/magical damage type if encounter defenses make one more valuable;
- special on-hit or proc behavior added later.

The existing dagger/gauntlet and mace/crossbow interval/damage pairs are close to DPS-neutral, but they still interact with on-hit effects, burst, and rounding. The analyzer should validate rather than assume neutrality.

Disconnected `EquipmentBase` weapon magnitude, attack-speed, and scaling fields have been removed. Tooltips present only combat-consumed attributes and authored recipe behavior.

### Recalibrate slot budgets

After stat mechanics are complete, compare equal-tier full loadouts:

- two-handed;
- one-handed plus shield/off-hand;
- dual one-handed;
- empty off-hand;
- armor slot packages;
- jewelry packages.

Set slot weights from the opportunity cost and measured behavior. The goal is not identical DPS; it is comparable overall budget with deliberate identities:

- two-handed: stronger main-hand behavior or concentrated offense;
- dual wield: more affix/stat flexibility and on-hit synergy;
- weapon + shield: lower offense and higher conditional defense.

### Replace frontend tooltip weights

Remove the Angular `ATTRIBUTE_WEIGHTS`.

Return from the backend:

- `itemBudget`;
- `budgetByAttribute`;
- optional comparison deltas against the currently equipped item;
- warnings for unpriced/inert modifiers in development/admin responses.

Do not label intrinsic item budget as character Power. Suggested player-facing labels:

- “Item Budget” for debugging/admin only; or
- “Item Level” / “Gear Value” if exposed to players.

For a useful player comparison, show concrete projected changes:

- Max Health: `+128`
- physical mitigation at current tier: `+3.2 percentage points`
- basic-attack DPS estimate: `+4.1%`
- character Overall Power change only when a cached or explicitly requested simulation is available.

## Required content validation

Add startup and test-time validation:

1. every `AttributeType` has a combat/balance definition;
2. every equipment-eligible attribute has a production consumer;
3. every recipe/blueprint stat is equipment-eligible;
4. all initial stat profiles sum to 1 within tolerance;
5. every tempering stat has a valuation and valid cap;
6. every modifier type is allowed for that attribute;
7. hard caps cannot reach immunity, zero cooldown, or invalid attack cadence;
8. weapon behavior is valid and priceable;
9. no displayed equipment field is disconnected from combat;
10. no item contains an unknown or unpriced stat;
11. base plus generated modifiers do not exceed the intended slot/tier budget without an explicit exception;
12. two-handed overview aggregation counts an instance once;
13. every magnitude-bearing ability effect either omits `ScalingAttribute` or sets it to Power;
14. no ability effect scales directly from Fortitude, Precision, Spirit, or a secondary attribute;
15. every primary attribute has an explicit allowlist of derived targets;
16. no primary attribute derives another primary attribute;
17. Fortitude, Precision, and Spirit contribute only to their approved dependency groups;
18. an omitted or `Default` crit policy resolves direct healing as crit-eligible;
19. disabling critical healing requires an explicit `CritEligibility.Disallowed`;
20. omitted crit eligibility resolves through the canonical `Default` policy rather than being interpreted as `Disallowed`;
21. the Cooldown Reduction hard cap is exactly 40%, and content cannot override it with a higher local cap.

In development and CI, inert equipment stats should be errors. In production startup, consider failing fast as well; silently selling dead stats is worse than rejecting invalid content.

## Implementation phases

### Phase 0: Lock terminology and units

Deliverables:

- approve the distinction between Item Budget, Build Impact, and character Power;
- record Power as the sole ability-magnitude scaling attribute;
- approve the primary influence groups: Fortitude for toughness, Precision for reliability/tempo, and Spirit for sustain/support/summons;
- record the operation-specific `CritEligibility.Default` matrix, including crit-enabled direct healing;
- record direct percentage-point units and combat caps for Crit Chance, Dodge, Block, Damage Reduction, Life Steal, and Cooldown Reduction;
- record 100% bonus Crit Damage as the canonical character default;
- record the balance-version-1 primary-to-derived coefficients;
- record Cooldown Reduction as direct percentage points with a global 40% hard cap;
- record Bleed as Armor-mitigated and Burn/Poison as Resistance-mitigated;
- record that Healing Power does not affect barriers;
- record that Cooldown Reduction does not affect internal trigger cooldowns;
- document tick-to-second conversion.

Exit criteria:

- every attribute has an unambiguous unit and intended consumer;
- no combat implementation begins while Crit/percentage units remain mixed.

### Phase 1: Mechanical completeness

Implement pure, tested combat math for:

- Power-only magnitude scaling for damage, healing, barriers, damage over time, recovery, and other numeric ability effects;
- Fortitude contributions to Max Health, Armor, and Resistance;
- Precision contributions to Crit Chance rating, Armor Penetration, Magic Penetration, and Attack Speed;
- Spirit contributions to Healing Power, Health Regeneration, Status Resistance, Crowd Control Resistance, Summon Power, and Summon Health;
- Armor/Resistance and penetration;
- crit chance/damage;
- direct healing that crits by default, explicit healing opt-out, and `HealCrit` event emission;
- Weapon Damage;
- Block;
- Healing Power;
- character Life Steal;
- Cooldown Reduction with a 40% global cap;
- Status Resistance;
- Crowd Control Resistance;
- summon-wide Power/Health;
- caps and damage-type eligibility.

Also:

- fix two-handed overview double counting;
- remove ignored weapon fields from the model, schema, API, content, and frontend;
- add prevention-source telemetry to combat results/logging;
- increment `PowerRatingAlgorithm.CombatRulesVersion`.

Exit criteria:

- no production ability magnitude scales directly from Fortitude, Precision, Spirit, or a secondary attribute;
- damage, healing, damage over time, barriers, and recovery all respond to Power through the same canonical calculation;
- each primary-stat change recalculates only its documented dependency group;
- Fortitude changes safely synchronize health after recalculating Max Health;
- no primary attribute changes another primary attribute;
- every equipment-eligible attribute changes at least one relevant deterministic combat test;
- every intentionally conditional attribute also has a negative test proving where it does not apply;
- no current recipe spends budget on an inert stat.

### Phase 2: Balance profile and evaluator

Deliverables:

- balance profile domain models;
- JSON provider and validator;
- tier/stat cost bands;
- authoritative equipment budget evaluator;
- `ItemStatRollService` and tempering integration;
- modifier-type-aware evaluation;
- behavior budget evaluation.

Initialize the profile from measured and reviewed costs; do not retain a compatibility price table.

Exit criteria:

- all runtime equipment generation reads one provider;
- the active cost catalog is the single authoritative profile;
- every item can produce an auditable budget breakdown.

### Phase 3: Marginal-value analyzer

Build an isolated diagnostic runner next to the existing Power Rating simulation infrastructure.

Deliverables:

- canonical reference-build factory;
- scenario definitions;
- paired baseline/perturbation runner;
- continuous metrics;
- deterministic seed sets;
- cost recommendation report in JSON and Markdown/CSV;
- confidence and sample counts;
- equal-budget and interaction tests.

Do not make the analyzer silently overwrite the approved production profile. It should generate a candidate diff for review.

Exit criteria:

- rerunning the same version produces identical results;
- Armor versus Max Health can be explained at every reference tier;
- every stat has a measured marginal curve or is explicitly tagged conditional/unsupported.

### Phase 4: First calibration pass

Recommended order:

1. Power-only ability coefficients
2. Power and Max Health anchors
3. Fortitude-derived toughness
4. Armor, Resistance, Damage Reduction, Dodge, Block
5. Precision-derived reliability and tempo
6. Weapon Damage, Attack Speed, Crit Chance, and Crit Damage
7. penetration
8. Spirit-derived sustain, support, resilience, and summons
9. Healing Power, regeneration, Life Steal, cooldown
10. status/control resistance
11. summons
12. slot and weapon behavior budgets

For each group:

- approve formula constants;
- generate costs;
- replay equal-budget bundles;
- inspect pairwise synergies;
- update recipe profile caps if needed.

Exit criteria:

- equal-budget options fall within the chosen tolerance in relevant scenarios;
- no stat dominates all alternatives;
- intentional specialist stats outperform generalists only in their target matchup;
- full current recipe and blueprint catalog passes validation.

### Phase 5: Item and content normalization

Deliverables:

- use the single active profile for every item;
- migrate every Spirit-, Precision-, Fortitude-, or secondary-scaled ability effect to Power;
- recalculate migrated Power coefficients against canonical reference builds rather than copying coefficients blindly;
- convert Crit units and any renamed stats;
- remove disconnected weapon properties;
- normalize static item-base modifiers versus generated modifiers;
- update recipe/blueprint profiles and caps;
- recalculate item previews;
- update tutorial items explicitly.

Schema implications:

- pure combat formulas and JSON cost changes require no EF migration;
- because this is a local-only development environment, the base schema and model snapshots contain only the canonical item model;
- an existing local database created from the removed schema should be recreated instead of supported through compatibility code.

For an existing effect that used another scaling stat, a baseline-preserving starting conversion is:

```text
newPowerCoefficient =
    oldCoefficient
    * referenceOldScalingAttribute
    / referenceEffectivePower
```

Run the converted ability through the production simulator and adjust deliberately. This preserves a reference magnitude only; it does not guarantee identical progression because the old scaling attributes now influence distinct derived-stat bundles and have different growth curves from Power.

### Phase 6: API and frontend

Deliverables:

- backend budget/comparison DTOs;
- remove Angular `ATTRIBUTE_WEIGHTS`;
- display consistent units and caps;
- replace placeholder/obsolete attribute tooltip text;
- show physical and magical mitigation at the character's tier;
- distinguish Item Budget from Overall Power;
- development/admin display for budget breakdown and the active profile version.

Exit criteria:

- frontend cannot invent an attribute weight;
- flat, additive, and multiplicative modifiers display and compare correctly;
- a two-handed item shows the same total stats as combat receives.

### Phase 7: Regression and release calibration

Run:

- combat unit/property tests;
- crafting and tempering tests;
- full Power Rating tests;
- dungeon recommendation recalibration;
- dungeon readiness diagnostics;
- representative PvP mirror tests;
- full `EssenceSystem.Tests`.

Increment:

- broad `PowerRatingAlgorithm.Version` if player-facing Power semantics change;
- `CombatRulesVersion` for combat formula changes;
- `BenchmarkDefinitionVersion` if benchmark scenarios or success criteria change;
- balance profile version whenever equipment conversion changes.

Recompute persisted dungeon recommendations before deployment. Do not compare Power values across incompatible versions.

## Test plan

### Formula unit tests

- zero defense produces zero typed mitigation;
- mitigation is monotonic and remains below its cap;
- penetration never creates negative defense;
- Armor affects Physical but not Magical damage;
- Resistance affects Magical but not Physical damage;
- general Damage Reduction combines multiplicatively;
- Dodge and Block apply only to eligible attacks;
- 100 rating cannot create 100% Dodge, Block, or Damage Reduction;
- crit unit/default and multiplier are correct;
- direct healing with an omitted or `Default` crit policy can crit;
- `CritEligibility.Disallowed` prevents a direct heal from critting;
- `CritEligibility.Allowed` enables crits for an otherwise-disallowed operation;
- critical healing applies the crit multiplier exactly once and emits `EventType.HealCrit`;
- life-steal healing does not inherit direct healing's crit-enabled default;
- critical overhealing records only health actually restored;
- Cooldown Reduction of 0% leaves the authored cooldown unchanged;
- Cooldown Reduction of 40% produces no less than 60% of the authored cooldown after tick rounding;
- Cooldown Reduction above 40% produces the same cooldown as 40%;
- combined equipment, attribute, and temporary Cooldown Reduction sources share the same 40% cap;
- short-cooldown tick rounding never exceeds the 40% reduction cap;
- Attack Speed and Cooldown Reduction remain finite;
- status duration never drops below one tick;
- life steal uses health damage after mitigation and excludes overkill/barrier damage;
- summon stats affect all compatible summons exactly once;
- all magnitude-bearing ability operations use effective Power;
- Spirit never directly appears in an ability calculation;
- Fortitude affects only Max Health, Armor, and Resistance;
- Precision affects only Crit Chance, Armor Penetration, Magic Penetration, and Attack Speed;
- Spirit affects only Healing Power, Health Regeneration, Status Resistance, Crowd Control Resistance, Summon Power, and Summon Health;
- primary-derived contributions respect the target attribute's final cap;
- no primary attribute derives or recursively modifies another primary attribute.

### Property tests

- spending more positive budget never reduces the generated stat;
- `Evaluate(SpendBudget(B))` returns approximately `B` within rounding tolerance;
- generated values never exceed hard caps;
- total recipe shares equal 100%;
- higher tier/quality does not generate a weaker item for the same deterministic roll, except at a cap;
- mitigation, crit, haste, and avoidance curves are monotonic;
- no finite valid input produces NaN, infinity, negative cooldown, or negative damage.

### Integration tests

- a crafted item budget matches tier × slot × quality within tolerance, including base modifiers;
- rarity/tempering changes have one accounted budget effect;
- two-handed items aggregate once in overview and combat;
- main-hand behavior resolves from recipe/blueprint and is included in analysis;
- equipping an item changes the simulation snapshot fingerprint;
- frontend DTO budget equals the backend evaluator;
- every recipe and blueprint stat has an implemented combat path.

### Simulation balance tests

- equal budget Max Health versus Armor under physical pressure;
- equal budget Max Health versus Resistance under magical pressure;
- mixed Armor/Resistance versus general Damage Reduction under mixed pressure;
- equal budget Fortitude versus its direct Max Health/Armor/Resistance basket;
- equal budget Precision versus its direct Crit/penetration/Attack Speed basket;
- equal budget Spirit versus its direct recovery/resilience/summon basket;
- primary-stat comparisons inside and outside each primary's intended archetypes;
- Power sensitivity for damage, healing, barrier, damage-over-time, and recovery effects;
- Dodge/Block versus steady and burst damage;
- Crit Chance/Crit Damage pair frontier;
- Weapon Damage/Power/Attack Speed offense frontier;
- regen/healing/life-steal short versus long fights;
- penetration against low/high defense;
- two-handed/dual-wield/shield full-loadout comparisons;
- all tiers in the reference matrix.

Use tolerance bands, not exact combat totals, for balance tests. Exact deterministic totals belong in formula/mechanics tests.

## Diagnostics and reports

Add an Admin diagnostic that reports:

- active balance and combat-rule versions;
- attribute cost curves by tier;
- marginal utility and confidence interval;
- relevant and irrelevant scenarios;
- equal-budget outcome deltas;
- cap utilization;
- recipe/item budget breakdown;
- unpriced or inert modifiers;
- pairwise synergy warnings;
- tier inversions;
- slot/loadout balance comparisons.

Recommended warning thresholds:

- more than 10% equal-budget median difference within a specialist matchup;
- more than 20% Overall difference between intended peers;
- any zero-value equipment stat in all relevant scenarios;
- any stat that reaches a hard cap on ordinary same-tier gear;
- any recipe whose actual budget differs from target by more than rounding tolerance;
- any item whose displayed value differs from server evaluation;
- any stat whose recommended cost changes more than 20% between adjacent tiers without an explained breakpoint.

## Concrete file-level implementation map

Suggested additions:

- `LL/src/Core/Domain/Models/Attributes/Balance/AttributeBalanceDefinition.cs`
- `LL/src/Core/Domain/Models/Attributes/Balance/AttributeBalanceProfile.cs`
- `LL/src/Core/Domain/Models/Attributes/Balance/PrimaryAttributeInfluenceDefinition.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/Balance/IAttributeBalanceProfileProvider.cs`
- `LL/src/Infrastructure/Service/Services.LL/Balance/JsonAttributeBalanceProfileProvider.cs`
- `LL/src/Infrastructure/Service/Services.LL/Balance/AttributeBalanceProfileValidator.cs`
- `LL/src/Infrastructure/Service/Services.LL/Balance/EquipmentBudgetEvaluator.cs`
- `LL/src/Infrastructure/Service/Services.LL/Balance/AttributeMarginalValueAnalyzer.cs`
- `LL/src/API/API.LL/Data/balance/attribute-balance.json`

Likely modifications:

- `AttributeCatalog.cs`: add or connect unit/mechanic metadata and update all four primary-stat descriptions to their explicit identities.
- `AttributeCalculator.cs`: deduplicate equipped instances; retain consistent modifier ordering; apply versioned primary-to-derived contributions before target-stat caps and combat rules.
- `FastCombatEngine.cs`: implement typed mitigation, penetration, crit, block, healing, life steal, cooldown, resistance, and summon rules.
- `AbilitySpec.cs` / compiler/runtime: enforce Power-only magnitude scaling, migrate `ScalingCoefficient` toward `PowerCoefficient`, reject other scaling attributes, add the three-state crit policy, and resolve direct healing as crit-eligible by default.
- `CombatStatsAggregator.cs` and combat result models: prevention-source and utility telemetry.
- `EquipmentInstance.cs`: replace or account for implicit rarity Boost.
- `ItemStatRollService.cs`: spend versioned banded budget.
- `TemperingMechanicsService.cs`: evaluate and spend through the same service.
- `CraftingService.cs`: expose approved budget breakdown and ranges.
- `EquipmentStatBudgetCatalog.cs`: keep as the single active reviewed cost profile.
- `PowerAnalysisSimulationRunner.cs`: expose reusable continuous benchmark metrics without coupling crafting directly to runtime simulation.
- `PowerRatingAlgorithm`: increment appropriate versions.
- equipment DTOs: return budget/version/comparison metadata.
- Angular `equipment-display.ts`: remove `ATTRIBUTE_WEIGHTS`.
- Angular attribute formatting/tooltips: use canonical unit metadata.

Keep all combat math reusable by both gameplay and simulations. Do not implement a second “analysis-only” mitigation or damage formula.

## Resolved decisions and remaining decisions

Resolved for combat rules version 4 and equipment balance version 4:

1. Fortitude grants `+4 Max Health`, `+0.5 Armor`, and `+0.5 Resistance` per point.
2. Precision grants `+0.05 Attack Speed` per point alongside `+0.1` to Crit Chance and both penetration stats.
3. Spirit retains the six-stat dependency group for the first calibration pass.
4. Crit Chance currently uses direct percentage points with a 75% combat cap.
5. Crit Damage is bonus percentage above a normal hit; the canonical character base is 100%, producing a 2x critical.
6. Bleed uses Armor; Burn and Poison use Resistance.
7. Healing Power affects health restoration, including life steal, but not barriers.
8. Cooldown Reduction affects active cooldowns when they start, not internal trigger cooldowns or time already remaining.
9. Block has 50% severity and a 50% chance cap.
10. General Damage Reduction caps at 40%, Life Steal at 50%, Dodge at 50%, and Cooldown Reduction at 40%. Attack Speed retains the 4x runtime rate bound, with a weapon-dependent useful stat ceiling derived from that rate.
11. Backend Item Budget is exposed to players as Gear Value; simulation-backed whole-build Power remains distinct.
12. Equipment costs resolve by item tier from reviewed tier anchors; intermediate tiers use linear interpolation.
13. Armor and Resistance use effective-health-derived tier anchors, and Fortitude is priced from its exact derived basket.
14. Every item uses the active balance profile; equipment instances do not persist or branch on historical balance versions.
15. Summon abilities and newly created summons always enter ready; there is no delayed-lifecycle compatibility mode.
16. Disconnected `EquipmentBase` magnitude, magnitude-range, attack-speed, and scaling fields are removed from the domain model, schema, API, JSON content, Admin editor, crafting preview, and equipment tooltip.

Still open:

1. Should the Spirit dependency group be narrowed after multi-scenario simulation?
2. Should Attack Speed keep the 4x rate ceiling with loadout-aware allocation constraints, or move to a lower universal rate ceiling with new costs and weapon behavior?
3. Should exact build-impact simulation remain in Admin diagnostics or also power an on-demand player comparison?

## Historical first implementation slice

The original smallest slice that directly addressed the reported Defense-versus-HP problem was:

1. migrate all magnitude-bearing ability effects to Power-only scaling;
2. implement the versioned primary influence profile for Fortitude, Precision, and Spirit;
3. standardize units for Max Health, Armor, Resistance, penetration, and Damage Reduction;
4. implement and test physical/magical mitigation in `FastCombatEngine`;
5. add prevention-source telemetry;
6. create tier 1, 5, and 10 physical/magical reference builds;
7. add a paired marginal analyzer for all four primaries and their derived-stat baskets;
8. generate tier-aware cost bands for those stats;
9. make crafting and tempering consume the profile;
10. remove the frontend's local weights for those stats and return backend budget data;
11. validate all heavy/medium/cloth/shield recipes with equal-budget simulations;
12. increment combat/Power versions and recalibrate dungeon recommendations.

Do not price Armor from the currently unused helper and stop there. The implementation is complete only when production combat, analyzer results, equipment generation, tooltip values, and regression tests all agree.

## Definition of done

The balancing system is complete when:

- every equipment-eligible attribute has a defined, tested production combat effect;
- every attribute has canonical units, caps, and stacking rules;
- every numeric ability output derives from effective Power and an authored Power coefficient;
- direct healing is crit-eligible by default, and only an explicit authored opt-out disables it;
- Cooldown Reduction is additive, globally capped at 40%, and cannot reduce a cooldown below 60% of its authored duration;
- Power, Fortitude, Precision, and Spirit each have one documented and tested identity;
- Fortitude, Precision, and Spirit affect only their approved derived-stat groups;
- no primary attribute recursively scales another primary attribute;
- every item modifier is assigned budget by one backend evaluator;
- crafting, tempering, previews, and tooltips use the same versioned profile;
- no frontend-local weights remain;
- deterministic marginal analysis can regenerate candidate costs;
- equal-budget peers are within approved tolerances in relevant scenarios;
- conditional stats expose their matchup sensitivity rather than pretending to have universal value;
- whole-build Power remains simulation-backed;
- every item and content path uses the single active balance profile;
- dungeon recommendations are recalibrated after the combat-rule change;
- the full relevant test suite passes.

### Current gap assessment

The approved behavior, prices, allocator, canonical attribute metadata, and reconciling prevention telemetry now run together in production code. The code-side balance program meets its definition of done; automatically invalidated dungeon recommendations still require runtime execution and review.

| Definition-of-done requirement                                                 | Status                                             | Evidence and remaining work                                                                                                                                                                                                                                                                                                                |
| ------------------------------------------------------------------------------ | -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Every equipment attribute has a tested production combat effect                | **Complete**                                       | All 24 equipment attributes have relevant deterministic analyzer coverage and a production combat consumer.                                                                                                                                                                                                                                |
| Defensive prevention telemetry is attributable and reconciling                 | **Complete**                                       | Every incoming hit now records avoidance, physical/Armor mitigation, magical/Resistance mitigation, Block, general Damage Reduction, barrier absorption, damage amplification, and final health damage. Per-hit, entity, and averaged analyzer outputs enforce `incoming + amplification = avoidance + prevention + barrier + final health damage`. |
| Canonical units, caps, and stacking rules exist for every attribute            | **Complete**                                       | All 24 definitions now declare their display name, description, unit, additive stacking rule, valid range, fixed/contextual cap behavior, equipment/content visibility, precision/suffix, approved primary source, and relevant benchmark scenarios. Combat cap lookups, allocation constraints, GameBootstrap, Admin diagnostics, and Angular equipment formatting/tooltips consume the catalog. |
| Every numeric ability output scales from Power                                 | **Complete**                                       | Ability catalog validation rejects non-Power effect scaling; damage, healing, barriers, periodic effects, and other magnitude-bearing effects use effective Power. Summons act through their own Power after owner-to-summon attribute construction.                                                                                       |
| Healing crits by default                                                       | **Complete**                                       | The three-state crit policy defaults direct healing to crit-eligible and supports an explicit opt-out.                                                                                                                                                                                                                                     |
| Cooldown Reduction is additive and capped at 40%                               | **Complete**                                       | The shared cooldown calculation clamps reduction to 40% and uses ceiling-based tick rounding so cooldowns cannot fall below 60% of authored duration.                                                                                                                                                                                      |
| The four primaries have distinct identities                                    | **Complete**                                       | Power owns ability magnitude; Fortitude owns toughness; Precision owns crit, penetration, and attack tempo; Spirit owns the approved sustain, resilience, and summon group.                                                                                                                                                                |
| Primary dependencies are limited and non-recursive                             | **Complete**                                       | Fortitude, Precision, and Spirit project only into their declared derived groups, and no primary grants another primary.                                                                                                                                                                                                                   |
| One backend evaluator prices every item modifier                               | **Complete**                                       | Crafting, tempering, API Gear Value, and frontend display all use the single active balance profile.                                                                                                                                                                                                                                       |
| Crafting, tempering, previews, and tooltips use the approved profile           | **Complete**                                       | Base rolls and previews use the active constrained allocator; quality growth and directed tempering respect the same direct and coupled caps.                                                                                                                                                                                              |
| No frontend-local attribute weights remain                                     | **Complete**                                       | The Angular `ATTRIBUTE_WEIGHTS` table has been removed; displayed Gear Value comes from the backend.                                                                                                                                                                                                                                       |
| Deterministic analysis can regenerate candidate prices                         | **Complete**                                       | The analyzer measures all 24 stats over deterministic production-engine scenarios and emits tier-specific candidate costs and confidence data. Candidate overrides are visible in Admin diagnostics.                                                                                                                                       |
| Equal-budget peers pass approved tolerances                                    | **Complete for the active profile**                | All 108 synthetic release cells, nine summon cells, nine representative hand cells, and 21 real recipe/blueprint release cells pass. Twelve cross-role or cross-specialization cells remain diagnostic by design.                                                                                                                           |
| Conditional stats expose matchup sensitivity                                   | **Complete in Admin diagnostics**                  | Each stat declares relevant scenarios and reports per-scenario results, medians, confidence intervals, cap pressure, and contextual loadout output rather than one universal value.                                                                                                                                                        |
| Whole-build Power remains simulation-backed                                    | **Complete**                                       | Character Power continues to use production-engine benchmark simulations and remains separate from Gear Value.                                                                                                                                                                                                                             |
| Every item and content path uses the active profile                            | **Complete**                                       | Historical item-price branching and persisted item balance versions have been removed. The clean local schema, crafting, tempering, DTOs, and frontend all use the active profile.                                                                                                                                                           |
| Dungeon recommendations match the active combat rules                          | **Pending operational recalibration**              | Power algorithm version 13 and combat rules version 4 invalidate older recommendation identities. The existing calibration worker will recompute and persist replacements after deployment; those generated values still need operational review.                                                                                          |
| Full relevant tests pass                                                       | **Complete in the working tree**                   | Backend coverage includes explicit current-only equipment-surface and additive-blueprint guards plus canonical metadata, prevention-ledger, and stale-recommendation replacement coverage. The Angular development build and the two focused metadata-formatting tests pass; the unrelated full Angular suite still has four pre-existing failures in Tavern/AppComponent specs.                                                                                       |

### Recently completed: blueprints are additive upgrades

Blueprints no longer blend their stat profile into the base recipe's fixed budget. Crafting now performs two constrained allocations:

1. allocate the complete base-recipe budget from the unchanged base-recipe profile;
2. allocate a separate blueprint bonus equal to `20%` of the base budget from the blueprint's bonus-stat profile, using the completed base roll as the starting point for all item and combat caps.

This guarantees that selecting a blueprint cannot lower Power, Spirit, or any other base-recipe roll. Overlapping blueprint stats increase the inherited value, new stats are added, and any cap-driven overflow remains aligned with the recipe or blueprint theme. The active content catalog spends the full authorized budget without exceeding item or effective combat caps.

Tempering composition follows the same additive rule: the recipe's tempering weights and eligibility remain intact, while the blueprint adds or strengthens its own tempering options. The crafting preview explicitly reports that the base is retained, shows the separate bonus-budget percentage, and decomposes inherited versus blueprint-added ranges.

### Recently completed: tier-10 hand cadence gate

The Maul's authored basic-attack damage multiplier was reduced from `1.22` to `1.18`, while its `1.25` interval multiplier and all hand-slot budgets remain unchanged. The analyzer now reads the representative Dagger and Maul behavior directly from the production crafting definitions instead of duplicating their numeric values, and it enforces `HandCalibrationPassed` with a regression assertion.

The tier-10 representative differences are `-18.81%`, `-17.90%`, and `-18.12%` over 90, 180, and 600 ticks respectively. All nine representative tier/duration comparisons pass the 20% tolerance. Equal-budget/equal-behavior controls remain within roughly `1.21%`, preserving the evidence that cadence/magnitude behavior—not allocation waste—caused the original inversion.

### Recently completed: real-content peer calibration

Synthetic coverage remains complete: the 114-cell matrix has 108 passing release gates and six high-defense penetration sensitivity probes. The analyzer now also composes real crafting designs and runs 33 candidate-profile combat cells at tiers 1, 5, and 10:

- 21 same-purpose release gates cover balanced, fast, and penetration-oriented dual-wield/two-handed choices, a tower-shield defensive peer, and offensive, sustain, and summon-oriented blueprint peers;
- 12 diagnostic cells cover Heavy/Medium and Medium/Light armor-family decompositions, Arcane/Spirit offense-versus-sustain specialization, and Spirit/Primal support-versus-summon specialization;
- every result exposes both composed design identifiers, scenario, candidate budget spent, allocated attribute points, utility per 100 budget, tolerance, pass state, and decomposed combat output;
- `CraftingCombatPeerMatrixPassed` is part of `ActiveProfilePassed`, and failed cells emit `CraftingCombatPeerMismatch` findings.

All 21 release cells now pass without widening the 20% peer tolerance. The authored adjustments are:

- Greatsword basic-attack damage multiplier: `1.00 → 1.02`;
- Gauntlets basic-attack damage multiplier: `0.78 → 0.865`, while retaining the `0.75` interval multiplier;
- every blueprint bonus-stat budget multiplier: `0.20`;
- Execution profile: Power `40%`, Weapon Damage `30%`, Crit Chance `20%`, Crit Damage `10%`, avoiding the former all-in Crit Damage/Precision feedback loop;
- Aegis profile: Max Health `35%`, Armor `25%`, Resistance `25%`, Damage Reduction `15%`, reducing excessive investment into already-diminishing Armor;
- Fury/Execution overflow remains offensive on every compatible slot, while Aegis overflow remains defensive.

Arcane and Spirit remain fully measured, but their difference no longer blocks release because Arcane promises magical offense while Spirit promises sustain/support. Cross-role output decompositions remain diagnostic and are not promoted into false peer gates.

### Recently completed: canonical attribute metadata

`AttributeCatalog` is now the machine-readable contract for all 24 attributes. Each definition declares its unit, stacking rule, valid range, cap mode, equipment/content visibility, display precision and suffix, approved primary source, and relevant benchmark scenarios. Fixed caps resolve from the catalog, while Attack Speed resolves its useful cap from the weapon interval and the 4x runtime rate ceiling.

Production combat cap clamps, Cooldown Reduction, allocation constraints, GameBootstrap, and Admin balance diagnostics consume the catalog. The Angular formatter is hydrated from GameBootstrap instead of maintaining its own label and percentage lists, and equipment rows build their tooltip descriptions, caps, and primary-source notes from the same definitions. In particular, Summon Power and Summon Health are explicitly percentage points, while Status Resistance and Crowd Control Resistance are ratings.

Display precision follows the unit: flat points, ratings, and HP/5s render as rounded integers; percentage-point attributes retain up to two decimal places. Health Regeneration is canonically measured and displayed as `HP/5s`, matching the five-second runtime interval.

### Recently completed: Health Regeneration comparison coverage

Equal-budget Health Regeneration checks now compare it against Max Health in normal physical pressure and long sustain, Armor in physical pressure, Resistance in magical pressure, Damage Reduction in mixed pressure and long sustain, Healing Power in healing sustain, and Life Steal in long sustain. All comparisons run at tiers 1, 5, and 10 and are release gates under the existing 20-percentage-point generalist tolerance.

The equal-budget records retain averaged combat outputs for the baseline and both investments. Regression tests therefore require an actual Health Regeneration investment to restore more HP than the baseline; a finite or non-negative utility value alone is no longer sufficient. Focused combat tests also enforce no pulse before five seconds, exact five-second cadence and magnitude, telemetry aggregation, no overheal, no healing at full health, and no effect from zero or negative values.

The active tier-10 marginal results are:

| Scenario | Equal-budget comparison | Health Regeneration gain | Peer gain |
| --- | --- | ---: | ---: |
| Physical pressure | Health Regeneration vs Max Health | `7.58%` | `9.03%` |
| Physical pressure | Health Regeneration vs Armor | `7.58%` | `9.03%` |
| Magical pressure | Health Regeneration vs Resistance | `7.58%` | `9.03%` |
| Mixed pressure | Health Regeneration vs Damage Reduction | `4.89%` | `1.21%` |
| Long sustain | Health Regeneration vs Max Health | `21.49%` | `12.15%` |
| Long sustain | Health Regeneration vs Damage Reduction | `21.49%` | `4.10%` |
| Healing sustain | Health Regeneration vs Healing Power | `8.34%` | `3.12%` |
| Long sustain | Health Regeneration vs Life Steal | `21.49%` | `3.06%` |

This demonstrates the intended duration tradeoff: direct Health, Armor, and Resistance are slightly stronger during normal focused pressure, while Health Regeneration becomes substantially stronger during attrition. Damage Reduction, Healing Power, and Life Steal remain more conditional in these particular reference kits. Tier-1 percentage and healing peers can produce zero marginal score at the small test budget because integer combat rounding consumes their sub-point changes; the raw output fields make that granularity visible instead of misclassifying Health Regeneration as universally superior.

The maximum-progression analyzer runs all 1,248 real tier-10 recipe/blueprint loadouts at the upper equipment envelope: maximum craft variance, Masterwork quality, Legacy rarity's static-base boost, and six rarity-upgrade improvement budgets per item. It records cap saturation grouped by attribute, unspendable generated/rarity budget grouped by recipe and applied blueprint, and the full real crafting combat-peer matrix.

The maximum-progression gate now requires all release-designated combat peers to pass, every loadout to remain at or below effective character caps, every loadout to spend its complete progression budget, and every result to be finite and complete. The active result is 1,248 analyzed loadouts, zero capped loadouts, zero unspent loadouts, and zero failed release peers. Cross-role and cross-specialization comparisons remain visible diagnostics rather than false equivalence gates.

Completeness tests require one definition for every `AttributeType`, exact parity with every equipment-budgeted stat, valid formatting data, aligned fixed caps, aligned primary contribution sources, non-recursive primary derivation, and analyzer-scenario compatibility.

### Recently completed: reconciling prevention telemetry

Damage events now carry a non-overlapping prevention ledger:

- incoming pre-defense damage, after any critical multiplier;
- Dodge/miss avoided damage and avoided-hit count;
- physical mitigation prevented by Armor after physical penetration;
- magical mitigation prevented by Resistance after magical penetration;
- Block prevented;
- general Damage Reduction prevented;
- explicit damage amplification from negative reduction effects;
- barrier absorbed;
- final post-prevention damage delivered to health.

Each combat-log hit, aggregated `EntityStats`, and averaged analyzer loadout output can be reconciled with:

`incoming raw damage + amplification = avoided damage + typed mitigation + Block + general Damage Reduction + barrier absorbed + final health damage`

Physical and magical typed mitigation are also required to sum to total typed mitigation. This lets mixed-pressure diagnostics attribute prevention to Armor versus Resistance and show how penetration changes that prevention without folding it into HP or barrier value.

### Remaining operational steps

The current profile is active, all `3,744` catalog loadouts spend their budgets without exceeding combat caps, and the active calibration gate passes. There is no item-version migration: the local schema contains only the canonical equipment model.

If the local database was created before the schema cleanup, recreate it from the current migrations. This is intentionally a development reset rather than an application compatibility path.

`PowerRatingAlgorithm.Version` is `13` and `PowerRatingAlgorithm.CombatRulesVersion` is `4`. Stale dungeon recommendations are rejected by that identity and recomputed by the existing calibration worker. `DungeonPowerCalibration.Enabled` is `true` in both default and Development API settings.

Post-startup recommendation checks:

```sql
SELECT
    "DungeonId",
    "AlgorithmVersion",
    "CombatRulesVersion",
    "BenchmarkDefinitionVersion",
    "RecommendationSeedSetVersion",
    "UpdatedAtUtc"
FROM "DungeonPowerRecommendationCacheEntries"
WHERE "AlgorithmVersion" <> 13
   OR "CombatRulesVersion" <> 4
   OR "BenchmarkDefinitionVersion" <> 6
   OR "RecommendationSeedSetVersion" <> 2
ORDER BY "DungeonId";
```

After calibration completes, this query must return no rows for the currently deployed dungeon catalog. Review worker logs for the number loaded versus recalibrated, any `CalculationFailed` results, low-confidence results, and per-dungeon recommended Power changes. Sample at least the lowest, middle, and highest tier recommendations in the player-facing dungeon preview before closing the release.

### Decisions that do not block completion by themselves

- Keeping the approved profile in reviewed code instead of JSON is acceptable if operational editing is not needed. If JSON is chosen, it needs startup validation, deterministic ordering, and versioned snapshots.
- Retaining Spirit's six derived attributes is acceptable if the expanded peer matrix passes; narrowing Spirit is a balance decision, not an architectural requirement.
- Keeping exact build-impact simulation in Admin only is acceptable. A player-facing on-demand comparison is optional.

### Minimum ordered operational path to done

1. Recreate any stale local database from the current canonical migrations.
2. Review the automatically regenerated dungeon recommendations for Power algorithm 13 / combat rules 4, benchmark definition 6, and seed set 2.
3. Run the release verification suite and require every definition-of-done row above to be **Complete**.
