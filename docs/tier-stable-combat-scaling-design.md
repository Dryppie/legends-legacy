# Tier-Stable Combat Scaling Design

## Status

Implemented with combat rules version 9 and equipment balance version 11. The values in
this document are the measured playtest baseline and should be changed only through a new
versioned balance pass.

## Purpose

Define a combat-stat model that:

- remains understandable without hidden tier conversions or rating curves;
- keeps Physical damage, Magical damage, healing, and barriers comparable from T1 through T10;
- lets equipment tiers increase character power without making sustain progressively dominant;
- gives ability authors stable Power coefficients that do not need to be retuned for every tier;
- makes the values shown to players match the values used by combat.

## Problem

The current defence calculation uses a fixed rating scale:

`mitigation = effectiveDefence / (effectiveDefence + 100)`

Equipment ratings grow substantially between tiers, but the fixed `100` does not. As a result, mitigation approaches 100% at higher tiers.

The T10 Epic Balanced audit exposed the consequence:

- Power: `2,520`
- effective defence after penetration: approximately `905`
- mitigation: approximately `90%`
- Healing Power: approximately `59%`

A 100% Power effect therefore produced approximately:

- `251` post-mitigation damage;
- `4,013` healing before missing-health clamping.

This made an equivalent heal roughly sixteen times larger than an equivalent damage effect. Healing and barriers also became increasingly valuable because restored or protected Health benefited from the same excessive mitigation.

Equalizing Armor and Resistance fixes Physical-versus-Magical parity, but it does not solve the underlying damage-versus-sustain imbalance when both mitigation values remain near 90%.

## Design principles

### Display the real mechanic

If the character sheet displays 30% Physical Mitigation, combat reduces eligible Physical damage by 30%. There is no hidden rating conversion.

### Tier scales raw capacity, not mitigation efficiency

Higher tiers primarily increase Power and Max Health. A Balanced T10 character has larger numbers than a Balanced T1 character, but does not mitigate a radically larger percentage of incoming damage.

### Ability coefficients remain portable

A 100% Power damage effect, a 75% Power heal, and a 65% Power barrier retain approximately the same relationship at every tier.

### Specialization is explicit

A defensive build has visibly higher mitigation. A sustain build has visibly higher Healing Power. These advantages come from intentional allocation, not automatic tier inflation.

## Equipment balance version 11

Version 11 removes authored item-base modifiers from recipe-crafted equipment. Crafted
items receive their complete combat budget from generated and tempered instance modifiers;
authored base modifiers remain only for legacy and directly granted equipment. This closes
the unbudgeted path that previously gave light and cloth armor large rarity-scaled Power
bonuses outside the recipe allocator.

Status Resistance now uses reviewed costs of `2.00`, `0.40`, and `0.665` at tiers 1, 5,
and 10. Light armor deliberately spends part of its identity budget on this stat. The old
flat `2.00` cost left that equipment materially below other profiles once its unbudgeted
base Power was removed. The adjusted anchors preserve the approved equal-budget, hand-mode,
summon, aggregate-cap, and maximum-progression gates while keeping Tier-1 Epic and max-tier
canonical Combat Ratings comparable.

### Version 13 foundation

Power remains the single, player-facing magnitude attribute. Its internal item-budget
price is tiered because one additional Power is a much larger relative increase when a
character begins with low Power. This keeps Power competitive with direct secondary
stats at each equipment tier without adding a player-facing conversion formula.

| Attribute | Tier 1 cost | Tier 5 cost | Tier 10 cost |
| --------- | ----------: | ----------: | -----------: |
| Power | `24.00` | `6.70` | `3.50` |

Intermediate tiers interpolate linearly between those anchors. These costs are generation
weights, not extra combat rules: an item still grants the displayed number of Power and
combat consumes that displayed value directly.

The analyzer validates Power against Attack Speed in a 600-tick, basic-attack-only
throughput context. Active abilities are deliberately excluded from this one comparison,
because Attack Speed does not accelerate them. A damage multiplier prevents integer event
rounding from hiding small Tier 1 changes without altering either stat's relative gain.

Measured equal-budget gains are:

| Tier | Power | Attack Speed | Difference |
| ---: | ----: | -----------: | ---------: |
| 1 | `3.88%` | `3.20%` | `+0.68pp` |
| 5 | `13.77%` | `14.11%` | `-0.34pp` |
| 10 | `52.81%` | `52.56%` | `+0.25pp` |

The matched Summoner-versus-Direct-Caster checks also pass their 20% tolerance at 90,
180, and 600 ticks for tiers 1, 5, and 10. Summons now inherit authored shares of ordinary
owner attributes, so their offense and durability scale through Power, Max Health, and
crit stats instead of dedicated summon equipment rolls.

Combat Rating projects final attributes and values all of them at Tier-1 reference
weights, while crafting continues to use tier-aware prices. This prevents an
identical base bonus from losing rating at a higher tier
and keeps every canonical profile's rating strictly increasing from T1 through T20.

## Target defensive attributes

Replace rating-style Armor and Resistance with direct percentage-point attributes:

- `Physical Mitigation`
- `Magical Mitigation`

The existing Armor and Resistance names may be retained if the UI clearly displays them as percentages, but names that state the resulting mechanic are preferred.

### Damage formula

`effectiveMitigation = clamp(targetMitigation - attackerPenetration, 0%, mitigationCap)`

`finalDamage = round(incomingDamage × (1 - effectiveMitigation))`

Penetration subtracts percentage points rather than multiplying or ignoring a percentage of a hidden rating.

Example:

- target Physical Mitigation: `30%`;
- attacker Armor Penetration: `10%`;
- effective Physical Mitigation: `20%`;
- incoming Physical damage: `1,000`;
- final Physical damage: `800`.

### Initial profile targets

| Equipment profile | Physical Mitigation | Magical Mitigation |
| ----------------- | ------------------: | -----------------: |
| Offensive         |                 15% |                15% |
| Balanced          |                 30% |                30% |
| Sustain           |                 25% |                25% |
| Defensive         |                 45% |                45% |
| Area              |                 25% |                25% |

These are starting calibration targets, not immutable final values.

The hard mitigation cap is `80%`. Defensive crafting targets remain below that ceiling so reaching 80% requires deliberate specialization rather than ordinary tier progression.

### Damage-type specialization

Individual items and builds may redistribute mitigation while preserving an understandable budget.

Examples:

- anti-Physical build: `40% Physical`, `20% Magical`;
- anti-Magical build: `20% Physical`, `40% Magical`;
- Balanced build: `30% Physical`, `30% Magical`.

The Balanced audit profile must always use equal Physical and Magical mitigation.

## Tier progression

Power and Max Health should grow at approximately the same rate across equipment tiers.

If both are multiplied by the same tier factor, a Power-scaled attack removes approximately the same percentage of a comparable opponent's Health at every tier.

Conceptually:

`PowerAtTier = basePower × tierGrowth`

`MaxHealthAtTier = baseMaxHealth × tierGrowth`

Mitigation percentages do not receive the same unrestricted tier multiplier.

Higher-tier equipment can still improve a build through:

- larger Power and Max Health values;
- more or better secondary-stat allocation;
- stronger affixes and equipment effects;
- more specialization flexibility;
- rarity, quality, and tempering improvements.

It should not automatically turn 30% mitigation into 90% mitigation.

## One primary scaling attribute

Power is the only active primary attribute. Every authored damage, healing, barrier,
periodic, and summon coefficient uses Power unless a mechanic explicitly has no scaling.
This keeps abilities readable: `100% Power` means the same thing regardless of damage
type or character role.

Equipment specializes a build with direct secondary attributes. It grants Max Health,
Crit Chance, Attack Speed, Healing Power, regeneration, mitigation, penetration,
resilience, and summon bonuses by name instead of hiding several effects inside a
primary-stat bundle.

Fortitude, Precision, and Spirit are not part of the attribute model. Every completed
character level-up grants `+0.25 Power` and `+20 Max Health`. This spends the current
one-CR level budget as 0.6 CR of Power and 0.4 CR of whole-number health, and tier
reference profiles include that same deterministic growth.

## Tank progression and crafting value

The neutral Balanced audit baseline remains stable across tiers, but live defensive builds may gain a bounded amount of mitigation as they progress. Flat neutral mitigation is a coefficient-calibration tool, not a rule that prevents tanks from becoming tougher.

### Initial defensive progression targets

| Equipment tier | Defensive-build mitigation |
| --- | ---: |
| T1 | 35% |
| T5 | 42% |
| T10 | 50% |
| Absolute cap | 80% |

The corresponding effective-Health multipliers are:

- 35% mitigation: approximately `1.54×`;
- 42% mitigation: approximately `1.72×`;
- 50% mitigation: `2.00×`;
- 80% mitigation: `5.00×`.

This gives tank equipment meaningful vertical progression without returning to the current 90% mitigation and `10×` effective-Health multiplier.

### Tank equipment progression axes

Tank equipment should improve through several visible mechanics rather than one indefinitely increasing defence rating:

- Max Health;
- Physical and Magical Mitigation percentage points;
- Block Chance;
- Guard generation;
- Barrier generation and capacity;
- Status and Crowd Control Resistance;
- Threat and Taunt reliability;
- explicit Healing Received modifiers;
- low-Health emergency effects;
- Thorns and retaliation;
- Physical-versus-Magical specialization.

Crafting families should communicate those choices directly. Example identities include:

- reinforced plating for Physical Mitigation;
- runic lining for Magical Mitigation;
- bastion construction for Block and Guard;
- vital construction for Max Health and healing received;
- retaliatory plating for Thorns and block-triggered damage;
- unyielding construction for control resistance and low-Health protection.

### Tiered tank item value

Higher-tier tank equipment may provide:

- more Max Health;
- slowly increasing mitigation percentage points;
- more affix capacity;
- stronger conditional effects;
- better mixed defensive combinations;
- stronger Guard and Barrier interactions;
- additional tank-specific crafting choices.

Illustrative chest progression:

- T1: `+4% Physical Mitigation` and Max Health;
- T5: `+5% Physical Mitigation`, Max Health, and Block;
- T10: `+6% Physical Mitigation`, larger Max Health, Block, and a defensive effect.

These examples describe the desired progression shape, not final recipe values.

### Effective-Health presentation

Crafting and equipment comparison should expose the result of defensive choices:

- Physical Effective Health;
- Magical Effective Health;
- percentage change from the equipped item.

For example, an equipment comparison may show:

- `+7.2% Physical Effective Health`;
- `+3.4% Magical Effective Health`;
- `−2% damage output`.

Mitigation rolls that would exceed the hard cap must not consume crafting budget. Generation and tempering should redirect that budget into compatible defensive attributes such as Max Health, Block, Guard, Barrier support, or resistance to control.

## Healing

Healing remains authored directly as a percentage of Power. There is no hidden global healing scalar.

At the Balanced target of 30% mitigation:

- a 100% Power damage effect deals 70% Power after mitigation;
- a 70% Power heal restores 70% Power before missing-health clamping.

This produces an understandable baseline authoring relationship.

### Initial authoring ranges

| Effect shape                   |                  Suggested starting coefficient |
| ------------------------------ | ----------------------------------------------: |
| Standard single-target damage  |                                      100% Power |
| Standard single-target heal    |                                    70–80% Power |
| Standard single-target barrier |                                    60–70% Power |
| Multi-target damage            |      Reduced according to expected target count |
| Multi-target healing           |      Reduced according to expected target count |
| Heal over Time                 | Total coefficient includes every scheduled tick |

These ranges assume similar cooldowns and no major additional utility. Conditions, control, targeting quality, duration, and reliability consume part of the effect's budget.

### Healing Power

Healing Power is an explicit specialization percentage rather than an automatically escalating consequence of a bundled primary attribute.

Initial targets:

- Balanced build: `0%` Healing Power;
- Sustain build: approximately `20%`;
- no hard cap; each percentage point continues to provide its displayed additive increase.

Formula:

`finalHealing = round(authoredHealing × (1 + HealingPower))`

Example:

- authored heal: `70% Power`;
- Healing Power: `20%`;
- resulting heal: `84% Power`.

The displayed percentage is the combat percentage. Healing Power should not use an additional hidden rating curve.

Support equipment grants Healing Power, regeneration, status resistance, crowd-control resistance, Summon Power, or Summon Health directly.

### Healing received

Wound, Recovery, and other incoming-healing modifiers remain explicit percentage modifiers. Their descriptions must state whether they add percentage points or multiply final healing.

Prefer additive percentage points within one incoming-healing bucket to avoid opaque multiplier stacking.

## Barriers

Barriers may continue absorbing post-mitigation damage.

At stable mitigation percentages, their effective value also remains stable between tiers:

- with 30% mitigation, a 700-point Barrier prevents 1,000 raw incoming damage;
- that relationship is the same at T1 and T10.

Barrier coefficients should be authored below equivalent direct-healing coefficients when barriers offer advantages such as overheal avoidance, pre-emptive protection, or interactions with barrier-triggered abilities.

If barriers later prove too multiplicative with defensive builds, absorbing pre-mitigation damage is the preferred systemic alternative. Do not introduce a hidden tier-dependent barrier scalar.

## Critical strikes

Critical strikes may continue using the same eligibility and multiplier rules for direct damage and direct healing.

Because the base damage and healing relationship is stable, shared critical rules no longer amplify a large systemic imbalance. Individual effects may explicitly disallow critical strikes when required by their design.

## Neutral balance-audit profile

The automated essence balance audit needs a neutral calibration profile that is separate from testing a specific live equipment meta.

For every selected tier, the neutral profile should use:

- equal Physical and Magical mitigation;
- `30%` mitigation for both damage types;
- `0%` Armor Penetration;
- `0%` Magic Penetration;
- `0%` Healing Power;
- no build-specific outgoing or incoming damage modifier;
- Power and Max Health values derived from the selected equipment tier and rarity;
- the same Power-to-Max-Health relationship at every tier.

Equipment tier therefore changes combat scale, while the relationship between damage, healing, and barriers remains constant.

Separate audit runs may use Offensive, Sustain, Defensive, and Area profiles to measure meta-specific performance. Those results should not replace the neutral profile as the primary coefficient-balancing signal.

## Ability-budget guidelines

Ability strength cannot be determined from magnitude alone. The balance audit and authoring review should account for:

- cooldown;
- target count;
- target-selection quality;
- direct versus periodic delivery;
- duration and tick count;
- critical eligibility;
- control and harmful conditions;
- defensive utility;
- reliability and application chance;
- conditional requirements;
- stacking and refresh behavior.

For Heal over Time effects, the authored total is the sum of every scheduled tick. A five-tick effect healing for 20% Power per tick has a total coefficient of 100% Power.

## Implemented migration

### 1. Percentage-based combat attributes

- Replace rating interpretation for Armor and Resistance.
- Retain the Armor and Resistance names and display them as percentages.
- Define percentage-point penetration behavior.
- Apply the mitigation cap in one shared combat rule.

### 2. Equipment stat budgets

- Convert equipment defensive allocations into percentage-point budgets.
- Prevent tier growth from multiplying mitigation percentages.
- Let higher tiers spend most progression budget on Power, Max Health, and secondary flexibility.
- Update rarity, quality, tempering, and cap constraints for the new units.

### 3. Healing Power progression

- Remove Healing Power growth from bundled primary attributes.
- Author explicit Healing Power percentages for sustain-specialized equipment.
- Display the total Healing Power directly without imposing a character-level cap.

### 4. Ability and content conversion

- Establish damage, healing, barrier, and multi-target reference coefficients.
- Reduce healing coefficients that were authored around excessive mitigation.
- Evaluate long-duration and overlapping Heal over Time effects by total coefficient.
- Preserve mechanical identity instead of solving every outlier with magnitude changes.
- Use Power as the sole scaling attribute and explicit secondaries for attribute modifiers.

### 5. Update presentation and descriptions

- Display Physical and Magical mitigation as percentages.
- Display penetration as percentage points ignored.
- Ensure tooltips use the same final values as combat.
- Remove any UI language that implies an unexplained rating conversion.

### 6. Update balance automation

- Make the neutral audit profile the default.
- Retain optional real-equipment profiles for meta testing.
- Report mitigation, post-mitigation damage per 100% Power, healing per 100% Power, and barrier effective value.
- Run calibration at T1, T5, and T10 before accepting essence coefficient changes.

## Automated acceptance checks

For the neutral audit profile:

- Physical and Magical mitigation differ by less than `0.1` percentage points.
- Balanced mitigation remains at `30%` for T1 through T10.
- A 100% Power Physical hit and Magical hit differ by no more than rounding.
- The post-mitigation value of a 100% Power hit changes by no more than `1%` relative to Power across tiers.
- A standard heal's value relative to standard damage changes by no more than `5%` across tiers.
- A standard barrier's effective value relative to standard damage changes by no more than `5%` across tiers.
- No default profile exceeds the mitigation cap.
- Penetration subtracts the exact displayed percentage points.

For live profile audits:

- Offensive, Balanced, Sustain, Defensive, and Area profiles are tested separately.
- Results identify profile-dependent essence performance rather than combining it with neutral coefficient balance.
- An essence is not globally adjusted from one profile unless its direction is confirmed by the neutral audit or multiple live profiles.

## Expected outcome

Players can reason about combat using the values they see:

- 30% Physical Mitigation means 30% less Physical damage;
- 10% Armor Penetration removes ten percentage points of that mitigation;
- 20% Healing Power increases a heal by 20%;
- a 70% Power heal visibly competes with a 100% Power attack after 30% mitigation.

Tiers increase the size of combat numbers without changing the fundamental value of damage, healing, or barriers. This allows one set of readable ability coefficients to remain useful from T1 through T10.

## Open decisions

The following values require playtesting before the proposal becomes canonical:

- whether Balanced mitigation should be 25%, 30%, or 35%;
- whether the 80% mitigation cap produces enough counterplay at maximum specialization;
- how much uncapped Healing Power equipment should be allowed to buy at each tier;
- whether barriers continue absorbing post-mitigation damage;
- whether Armor and Resistance retain their existing names;
- whether later playtests justify changing the measured Power-to-Max-Health growth curve.

Balance version 10 uses one primary scaling attribute (Power), 30% neutral mitigation,
an 80% mitigation cap, uncapped
Healing Power, post-mitigation barriers, and the existing Armor and Resistance
names displayed as percentages. Playtesting may still revise those visible values and
the exact Power-to-Max-Health growth curve. None require a hidden per-tier rating formula.
