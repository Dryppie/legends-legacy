# Reaper

**Status: Proposed — not implemented.** All Reaper-specific numbers below are provisional for playtesting. The existing Bleed, Burn and Poison rules are reference behavior, not proposed changes to those conditions.

Reaper converts lingering pressure into earlier damage. Build around your own **Bleed, Burn and Poison**, then let automatic direct Essence attacks **Harvest** some of their future ticks.

The defining choice is which conditions to apply and which active Essences will harvest them. Harvest consumes scheduled damage before delivering it, so the same future tick has one outcome: its scheduled tick or a Harvest.

This proposal follows the [shared Combat Style progression](../README.md): full core at level 0, independent levels through 10, one refinement, two upgrade slots and one empowered upgrade.

## Core mechanic — Harvest

Once per Essence active cast, its first qualifying direct hit harvests the next unpaid future tick from **every eligible stack** on one enemy. The base form accepts character-applied Bleed, Burn and Poison.

A qualifying hit deals positive damage to Health or Barrier and leaves both the character and enemy alive after that hit's ordinary reactions. The enemy must still have an eligible stack that existed before this Essence cast began.

For each selected stack, consume its next future tick and add that tick's stored damage to the Harvest. The base Harvest delivers **110% of the consumed damage**, before ordinary damage modifiers and mitigation.

**Harvest amount = consumed raw tick damage × (1.10 + 0.01 × mastery level + qualifying upgrade bonuses).**

The +10% is part of the complete level-0 mechanic. Each mastery level adds another 1 percentage point; each qualifying upgrade below adds 5 percentage points. The multiplier applies to the consumed portion only.

### A worked Harvest

Assume Power was 1,000 when the character applied three Bleed stacks and two Poison stacks. Each stack stores 10 raw damage per tick under the existing condition rules.

At mastery level 4, a qualifying direct hit advances one tick from each of these five stacks:

| Source | Raw damage consumed | Harvest multiplier | Damage before modifiers and mitigation |
|---|---:|---:|---:|
| Three Bleed stacks | 30 | 114% | 34.2 Physical |
| Two Poison stacks | 20 | 114% | 22.8 Magical |
| Total | 50 | 114% | 57 |

If all five stacks were fresh, they originally held 240 raw damage: 120 from Bleed and 120 from Poison. This Harvest leaves **190 scheduled raw damage**, delivers **57 now**, and adds **7 damage** through Reaper's bonus. The triggering attack remains a separate damage event.

Examples retain decimals for clarity; normal damage rounding happens when each damage packet resolves.

## Existing conditions and ownership

Reaper uses the current independent-stack model:

| Condition | Stored damage per stack and tick | Normal schedule | Damage channel |
|---|---|---|---|
| Bleed | 1% of the applier's Power at application | Every 2 seconds for 8 seconds; four ticks | Physical: Armor and Armor Penetration |
| Burn | 1% of the applier's Power at application | Every 1 second for 4 seconds; four ticks | Magical: Resistance and Magic Penetration |
| Poison | 1% of the applier's Power at application | Every 2 seconds for 12 seconds; six ticks | Magical: Resistance and Magic Penetration |

Every application retains its own applier, stored tick damage and timing. Reapplication adds stacks and leaves existing stacks' timing intact.

Only stacks whose applier is the Reaper character are eligible, including this style's Opening Technique. Other characters' stacks remain theirs. Summon-applied stacks retain the summon as applier and are outside Harvest ownership, even when the Reaper owns that summon.

## Consumption and timing

Harvest advances specific future damage events; it neither refreshes conditions nor rewrites the remaining events' schedule.

1. Resolve condition ticks due at the current combat timestamp, including a final tick due at expiration, before active casts at that timestamp.
2. At the start of an actual Essence active cast, record which eligible applications already exist. Conditions added during this cast or its reactions become eligible for a later cast.
3. Resolve the qualifying direct hit and its ordinary reactions. Check that the character and enemy remain alive and that the candidate applications still exist.
4. Select the earliest unpaid tick of each eligible stack whose scheduled time is strictly later than the current timestamp. Refinements can change the family, target limit or number of ticks selected.
5. Commit consumption of all selected ticks together, then resolve Harvest damage. Mitigation and immunity use ordinary damage handling after consumption.
6. Each consumed event is skipped at its former due time. Remaining tick timestamps and the original expiration stay unchanged. Remove a stack immediately when it has no unpaid future ticks left.

For example, a Bleed stack applied at time 0 has ticks at 2, 4, 6 and 8 seconds. Harvesting at time 1 advances the tick due at 2; the remaining ticks stay at 4, 6 and 8. Harvesting again at 1.5 can advance the tick due at 4, leaving ticks at 6 and 8.

If the direct hit occurs exactly at time 2, the ordinary tick at 2 resolves first. Harvest can then advance the tick at 4. A consumed final tick cannot also resolve through expiration.

Stacks removed by cleanse, death or another effect before consumption are excluded. Consuming the final tick is a distinct removal reason, not a cleanse or natural expiration trigger. There is no damage to claim from an already consumed event.

Encounter end uses ordinary condition cleanup. Unused future ticks leave with their conditions; Harvest carries no stored damage or preparation into the next battle.

## Targeting and damage resolution

- The base form harvests at most **one enemy once per actual Essence active cast**. Multi-hit attacks and multiple direct damage components share that allowance.
- Area attacks use the first qualifying enemy in normal hit-resolution order. A hit that misses, deals zero damage or leaves no living eligible enemy does not use the allowance.
- Basic attacks, periodic ticks, reflected damage, secondary attacks and triggered copies of damage cannot initiate Harvest. A genuinely new normal Essence activation has its own allowance.
- Harvest has no separate accuracy roll. It delivers terminal **stored damage**, preserving the source condition's damage type and ownership.
- Aggregate the selected damage into at most three packets, in Bleed, Burn, Poison order. Bleed uses Physical defenses; Burn and Poison use Magical defenses. Preserve each condition family for relevant immunity rules.
- Start with each stack's stored tick amount. Apply the outgoing and incoming modifiers its ordinary tick would use at that moment exactly once, alongside the Harvest multiplier. Use the ordinary typed-defense and penetration calculation at delivery.
- The triggering attack's coefficient, damage, critical result and direct-only modifiers do not scale Harvest. Changing current Power does not replace a stack's stored Power snapshot.
- Stored-damage defaults apply: Harvest is noncritical and generates no on-hit, Lifesteal, reflection or periodic-tick reactions. Direct-only Vulnerable and Guard apply to the triggering attack according to their normal rules, not to the Harvest packets.
- Ordinary damage reduction and Barrier absorption apply to Harvest. Preserve ordinary damage, threat, kill attribution and Barrier absorption/break events; their resulting reactions cannot initiate another Harvest.

This establishes one damage delivery per consumed event and keeps condition-application, condition-tick and direct-hit reaction chains separate.

## Mastery level bonuses

Every mastery level adds **1 percentage point** to the bonus on damage selected for Harvest. It does not change how much future damage is consumed. Level 0 retains the complete mechanic and its base +10% bonus.

| Mastery level | Total bonus over consumed damage | Harvest from 100 raw consumed damage |
| ---: | ---: | ---: |
| 0 | +10% | 110 |
| 1 | +11% | 111 |
| 2 | +12% | 112 |
| 3 | +13% | 113 |
| 4 | +14% | 114 |
| 5 | +15% | 115 |
| 6 | +16% | 116 |
| 7 | +17% | 117 |
| 8 | +18% | 118 |
| 9 | +19% | 119 |
| 10 | +20% | 120 |

At level 10, consuming 80 raw future damage delivers 96 before modifiers and mitigation. If all future damage from an application is eventually harvested without upgrades, Reaper adds 20% to its raw damage budget; no consumed tick remains available for later damage. Each level contributes directly to this multiplier, without compounding.

## Refinements — level 3

Choose exactly one refinement or retain the base form. All forms use the same mastery level multiplier, ownership, timing and packet rules.

| Form | Eligible families | Future ticks consumed per eligible stack | Targets per Essence cast |
|---|---|---:|---:|
| Base form | Bleed, Burn and Poison | 1 | 1 |
| Bloodletting | Bleed | Up to 2 | 1 |
| Plague Sweep | Burn and Poison | 1 | Up to 2 distinct enemies |
| Last Rites | Bleed, Burn and Poison, on enemies at or below 35% Health | Up to 2 | 1 |

### Bloodletting

Harvest draws from character-applied Bleed and advances its next two unpaid future ticks per stack. If a stack has only one future tick, consume that one.

This form concentrates the build around Bleed application and larger Physical Harvests. Four Bleed stacks storing 10 per tick contribute 80 raw damage when each has at least two ticks left; mastery level 10 delivers 96 before defenses.

### Plague Sweep

Harvest draws from character-applied Burn and Poison. Each Essence cast can harvest up to two distinct enemies, once each, when its direct hits qualify. A multi-hit attack against one enemy still harvests that enemy once.

Evaluate and consume each enemy's stacks separately when its qualifying hit resolves. Mastery levels and upgrades are evaluated per harvested enemy. This supports active area attacks that collect existing pressure across a group.

### Last Rites

Harvest activates on an enemy only when its Health is at or below **35% after the qualifying direct hit and its ordinary reactions**. It then advances up to two unpaid future ticks from every eligible owned stack on that enemy.

This form builds an automatic finishing window: direct attacks bring an afflicted enemy into range, and the qualifying attack gathers a larger portion of its remaining damage. The base form's unrestricted one-tick Harvest remains the option for harvesting throughout an enemy's Health bar.

## Upgrades and Upgrade Mastery

All three upgrades become available with the first slot at level 5. The second slot opens at level 8. Equip at most two different upgrades; at level 9, empower one equipped upgrade in its existing slot.

| Upgrade | Ordinary effect | Empowered effect at level 9 |
|---|---|---|
| Closing Hand | Add **5 percentage points** when the harvested enemy is at or below **35% Health**. | The same bonus qualifies at or below **50% Health**. |
| Crosscut | Add **5 percentage points** when this Harvest consumes ticks from at least **two different condition families** on the enemy. | The same bonus also qualifies when it consumes ticks from at least **three distinct stacks**, even within one family. |
| Deep Roots | Add **5 percentage points** when **every harvested stack** retains at least one unpaid future tick after consumption. | The same bonus qualifies when **at least one harvested stack** retains an unpaid future tick. |

Each upgrade contributes its bonus once per harvested enemy. Evaluate Closing Hand's Health before Harvest damage, after the direct hit's reactions. Evaluate Crosscut from the selected families/stacks and Deep Roots from their remaining tick budgets immediately after consumption.

Qualification can be determined from the same selection before packets resolve, so all packets in that enemy's Harvest use one multiplier. Multiple qualifying conditions on an empowered upgrade still provide one +5-point bonus.

At mastery level 10, two qualifying upgrades produce **130% of consumed damage**: 110% base +10 points from mastery levels +5 +5. Mastery broadens an upgrade's condition; it does not add another copy of its bonus.

Removing the empowered upgrade clears its mastery choice. Retaining an upgrade without mastery preserves its ordinary effect.

## Opening Technique — level 7

**Grave Seed:** At battle start, apply **Poison(2)** to the living enemy with the highest Max Health. Resolve ties by the encounter's stable enemy order.

These are two ordinary, independent Poison stacks attributed to the character. They snapshot the character's Power at application, tick every 2 seconds for 12 seconds, and follow normal Poison immunity, duration and removal rules. The opening makes one application attempt to its initial target.

Grave Seed activates automatically once per battle with every form. It does not count as an Essence cast and does not publish Essence-hit or condition-application reactions. Its stacks are present before the character's first active cast and can be harvested normally by a form that accepts Poison.

Grave Seed does not repeat on later waves or after revival. If no living enemy is present at battle start, the opening is skipped for that battle.

At Power 1,000, the opening seeds 120 raw scheduled damage in total. A base-form Harvest at level 7, before the first Poison tick, advances 20 raw damage and delivers **23.4** before modifiers and mitigation; the other 100 raw damage remains scheduled.

## Milestones

| Mastery level | Benefit |
| ---: | --- |
| 0 | Full Harvest mechanic; 110% of consumed damage. |
| 1 | 111% Harvest multiplier before upgrades.  |
| 2 | 112% Harvest multiplier before upgrades.  |
| 3 | 113% Harvest multiplier before upgrades. Refinement choice. |
| 4 | 114% Harvest multiplier before upgrades.  |
| 5 | 115% Harvest multiplier before upgrades. First upgrade slot; all three upgrades available. |
| 6 | 116% Harvest multiplier before upgrades.  |
| 7 | 117% Harvest multiplier before upgrades. Grave Seed applies Poison(2) at battle start. |
| 8 | 118% Harvest multiplier before upgrades. Second upgrade slot. |
| 9 | 119% Harvest multiplier before upgrades. Empower one equipped upgrade. |
| 10 | 120% Harvest multiplier before upgrades. Maximum Reaper level. |

## Example builds

- **Mixed pressure:** Base form with Crosscut and Deep Roots; empower Crosscut. Mix condition sources and direct attacks. Harvesting three stacks worth 10 each at mastery level 10, with both upgrade conditions met, consumes 30 and deals 39 before defenses.
- **Bleed finisher:** Bloodletting with Closing Hand and Crosscut; empower Crosscut. Three Bleed stacks with two ticks each contribute 60 raw damage. Against an enemy at 30% Health, mastery level 10 with both bonuses delivers 78 Physical before defenses.
- **Group harvest:** Plague Sweep with Crosscut and Deep Roots; empower Deep Roots. One area Essence can harvest two enemies. Each enemy's Burn/Poison mix and remaining ticks determine its own bonus; their condition budgets remain independent.
- **Prepared execution:** Last Rites with Closing Hand and Deep Roots; empower Deep Roots. Let conditions establish pressure, then automatically advance two ticks per stack when a direct hit leaves the enemy within the finishing threshold.

Only one Combat Style is equipped. An ally using Bastion or Conduit keeps their own mechanics and conditions; Reaper does not spend their applications. The same ownership boundary applies between multiple Reapers.

## Playtest checks

- Compare slow and fast Essence activations against short enemies and durable bosses. Verify that earlier damage is noticeable and that every accelerated tick leaves the future budget exactly once.
- Check direct hits at a condition's due timestamp, a final tick's expiration, cleanse reactions and target death. Validate the defined tick-first and consume-once outcomes.
- Compare a many-hit single-target Essence with a single-hit Essence, then area attacks with Plague Sweep. Confirm the per-cast and distinct-target limits remain readable.
- Test mixed Physical/Magical defenses, Barrier, Guard on the triggering hit, direct-hit criticals and damage reactions. Harvest must retain its own type/channel and terminal stored-damage behavior.
- Check opening ownership, summon-applied conditions, ally-applied stacks and multiple Reapers. Examine Bloodletting's Bleed focus and Plague Sweep's group behavior separately.
- Assess every refinement with each upgrade pair, including mastery at level 9. Compare actual combat gains with the 10–30% bonus on consumed raw damage, separately from the benefit of advancing its timing.

## Reference rules

These references describe existing combat behavior. Harvest, its tick-consumption policy and every Reaper-specific choice above remain proposals.

- [Bleed](../../combat-lexicon/conditions/bleed.md), [Burn](../../combat-lexicon/conditions/burn.md) and [Poison](../../combat-lexicon/conditions/poison.md): independent stacks, stored damage, ownership and exact tick schedules.
- [Damage types](../../combat-lexicon/damage-types.md) and [damage categories](../../combat-lexicon/damage-categories.md): typed mitigation and terminal stored-damage defaults.
- [Barrier](../../combat-lexicon/conditions/barrier.md) and [Guard](../../combat-lexicon/conditions/guard.md): ordinary absorption and direct-hit protection.
- [Combat Styles overview](../README.md): the shared progression and configuration structure this proposal would use.
