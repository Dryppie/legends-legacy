# Reaper

**Status: Implemented — catalog `combat-styles.v8`.** Reaper is available at mastery level 0. Death Sentence's additional flat +10% applies in combat and previews, and Grave Seed applies five Poison stacks. The existing Bleed, Burn and Poison rules remain the source condition behavior. Already committed battles retain their captured tuning.

Reaper lets you decide how to use the damage still waiting in your own **Bleed, Burn and Poison**. Your direct Essence attacks **Harvest** that damage early. At mastery level 3, Soul Siphon turns it into healing, Last Rites saves it for a finishing blow, and Death Sentence adds a flat +10% before storing it as delayed **Doom**.

The defining choice is which conditions to apply and which active Essences will harvest them. Harvest consumes scheduled damage before converting it, so the same future tick has one outcome: its scheduled tick or the chosen Harvest payout.

Reaper follows the [shared Combat Style progression](../README.md): full core at level 0, independent levels through 10, one refinement, two upgrade slots and one empowered upgrade.

## Core mechanic — Harvest

Each qualifying direct hit from an Essence active cast harvests the next unpaid future tick from **every eligible stack** on the enemy hit. Last Rites instead harvests all unpaid future ticks when its Health threshold is met. Every form accepts character-applied Bleed, Burn and Poison; any one family is sufficient to trigger Harvest.

A qualifying hit deals positive damage to Health or Barrier and leaves both the character and enemy alive after that hit's ordinary reactions. The enemy must still have an eligible stack that existed before this Essence cast began.

For each selected stack, consume the selected future ticks and add their stored damage to the Harvest. The base Harvest delivers **110% of the consumed damage**, before ordinary damage modifiers and mitigation.

**Harvest amount = consumed raw tick damage × (1.10 + 0.01 × mastery level + qualifying upgrade bonuses).**

Death Sentence adds another **0.10** to the multiplier before storing Doom. This refinement bonus adds directly; it does not multiply the finished Harvest amount by 1.10.

The +10% is part of the complete level-0 mechanic. Each mastery level adds a flat +1%; each qualifying upgrade below adds a flat +5%. The multiplier applies to the consumed portion only.

### A worked Harvest

Assume Power was 1,000 when the character applied three Bleed stacks and two Poison stacks. Each stack stores 10 raw damage per tick under the existing condition rules.

At mastery level 4, a qualifying direct hit advances one tick from each of these five stacks:

| Source             | Raw damage consumed | Harvest multiplier | Damage before modifiers and mitigation |
| ------------------ | ------------------: | -----------------: | -------------------------------------: |
| Three Bleed stacks |                  30 |               114% |                          34.2 Physical |
| Two Poison stacks  |                  20 |               114% |                           22.8 Magical |
| Total              |                  50 |               114% |                                     57 |

If all five stacks were fresh, they originally held 240 raw damage: 120 from Bleed and 120 from Poison. This Harvest leaves **190 scheduled raw damage**, delivers **57 now**, and adds **7 damage** through Reaper's bonus. The triggering attack remains a separate damage event.

Examples retain decimals for clarity; normal damage rounding happens when each damage packet resolves.

## Existing conditions and ownership

Reaper uses the current independent-stack model:

| Condition | Stored damage per stack and tick         | Normal schedule                           | Damage channel                            |
| --------- | ---------------------------------------- | ----------------------------------------- | ----------------------------------------- |
| Bleed     | 1% of the applier's Power at application | Every 2 seconds for 8 seconds; four ticks | Physical: Armor and Armor Penetration     |
| Burn      | 1% of the applier's Power at application | Every 1 second for 4 seconds; four ticks  | Magical: Resistance and Magic Penetration |
| Poison    | 1% of the applier's Power at application | Every 2 seconds for 12 seconds; six ticks | Magical: Resistance and Magic Penetration |

Every application retains its own applier, stored tick damage and timing. Reapplication adds stacks and leaves existing stacks' timing intact.

Only stacks whose applier is the Reaper character are eligible, including this style's Opening Technique. Other characters' stacks remain theirs. Summon-applied stacks retain the summon as applier and are outside Harvest ownership, even when the Reaper owns that summon.

## Consumption and timing

Harvest advances specific future damage events; it neither refreshes conditions nor rewrites the remaining events' schedule.

1. Resolve condition ticks due at the current combat timestamp, including a final tick due at expiration, before active casts at that timestamp.
2. At the start of an actual Essence active cast, record which eligible applications already exist. Conditions added during this cast or its reactions become eligible for a later cast.
3. Resolve the qualifying direct hit and its ordinary reactions. Check that the character and enemy remain alive and that the candidate applications still exist.
4. Select the earliest unpaid tick of each eligible stack whose scheduled time is strictly later than the current timestamp. Last Rites first checks its Health threshold, selecting all unpaid future ticks when it qualifies and none otherwise. All forms select from Bleed, Burn and Poison.
5. Commit consumption of all selected ticks together, then resolve the selected form's payout: immediate damage, self-healing or a Doom application. Damage mitigation, healing limits and Doom prevention apply after consumption; they do not refund consumed ticks.
6. Each consumed event is skipped at its former due time. Remaining tick timestamps and the original expiration stay unchanged. Remove a stack immediately when it has no unpaid future ticks left.

For example, a Bleed stack applied at time 0 has ticks at 2, 4, 6 and 8 seconds. Harvesting at time 1 advances the tick due at 2; the remaining ticks stay at 4, 6 and 8. Harvesting again at 1.5 can advance the tick due at 4, leaving ticks at 6 and 8.

If the direct hit occurs exactly at time 2, the ordinary tick at 2 resolves first. Harvest can then advance the tick at 4. A consumed final tick cannot also resolve through expiration.

Stacks removed by cleanse, death or another effect before consumption are excluded. Consuming the final tick is a distinct removal reason, not a cleanse or natural expiration trigger. There is no damage to claim from an already consumed event.

Encounter end uses ordinary condition cleanup. Unused future ticks and pending Harvest-created Doom leave with their conditions without paying out; Harvest carries no stored damage or preparation into the next battle.

## Targeting and damage resolution

These damage-packet rules apply to the base form and Last Rites. Soul Siphon and Death Sentence retain the qualifying-hit rules, but replace immediate damage with the payout described under their refinement.

- Area attacks evaluate each enemy hit in normal hit-resolution order. Repeated direct hits and separately resolved direct damage components each check eligibility against the remaining unpaid ticks. Resolve consumption and payout before evaluating the next hit.
- A hit that misses, deals zero damage or leaves no living eligible enemy does not harvest.
- Basic attacks, periodic ticks, reflected damage, secondary attacks and triggered copies of damage cannot initiate Harvest.
- Harvest has no separate accuracy roll. It delivers terminal **stored damage**, preserving the source condition's damage type and ownership.
- Aggregate the selected damage into at most three packets, in Bleed, Burn, Poison order. Bleed uses Physical defenses; Burn and Poison use Magical defenses. Preserve each condition family for its ordinary damage modifiers.
- Start with each stack's stored tick amount. Apply the outgoing and incoming modifiers its ordinary tick would use at that moment exactly once, alongside the Harvest multiplier. Use the ordinary typed-defense and penetration calculation at delivery.
- The triggering attack's coefficient, damage, critical result and direct-only modifiers do not scale Harvest. Changing current Power does not replace a stack's stored Power snapshot.
- Stored-damage defaults apply: Harvest is noncritical and generates no on-hit, Lifesteal, reflection or periodic-tick reactions. Direct-only Vulnerable and Guard apply to the triggering attack according to their normal rules, not to the Harvest packets.
- Ordinary damage reduction and Barrier absorption apply to Harvest. Preserve ordinary damage, threat, kill attribution and Barrier absorption/break events; their resulting reactions cannot initiate another Harvest.

This establishes one damage delivery per consumed event and keeps condition-application, condition-tick and direct-hit reaction chains separate.

## Mastery level bonuses

Every mastery level adds **a flat +1%** to the bonus on damage selected for Harvest. It does not change how much future damage is consumed. Level 0 retains the complete mechanic and its base +10% bonus. The table shows the base form's multiplier, also used by Soul Siphon and Last Rites. Death Sentence adds its own flat +10% on top once selected at mastery 3.

| Mastery level | Total bonus over consumed damage | Harvest from 100 raw consumed damage |
| ------------: | -------------------------------: | -----------------------------------: |
|             0 |                             +10% |                                  110 |
|             1 |                             +11% |                                  111 |
|             2 |                             +12% |                                  112 |
|             3 |                             +13% |                                  113 |
|             4 |                             +14% |                                  114 |
|             5 |                             +15% |                                  115 |
|             6 |                             +16% |                                  116 |
|             7 |                             +17% |                                  117 |
|             8 |                             +18% |                                  118 |
|             9 |                             +19% |                                  119 |
|            10 |                             +20% |                                  120 |

At level 10 without upgrades, consuming 80 raw future damage yields a Harvest amount of 96 for the base form, Soul Siphon and Last Rites, or 104 with Death Sentence's extra flat +10%. In the base form, harvesting all future damage from an application without upgrades adds 20% to its raw damage budget; no consumed tick remains available for later damage. Each level contributes directly to this multiplier, without compounding.

## Refinements — level 3

Choose one refinement or keep the base form. Every form works with your own Bleed, Burn and Poison, whether you use just one condition or mix all three. They share the same flat mastery-level bonus, ownership and timing rules; Death Sentence also adds a flat +10%. The base form, Soul Siphon and Death Sentence each take one future tick per stack. Last Rites takes all remaining ticks when the enemy is weak enough. Healing and Doom replace the immediate Harvest damage completely.

| Form           | Eligible families      | Future ticks consumed per eligible stack | Harvest payout |
| -------------- | ---------------------- | ---------------------------------------: | -------------- |
| Base form      | Bleed, Burn and Poison |                                        1 | Immediate typed damage |
| Soul Siphon    | Bleed, Burn and Poison |                                        1 | Heal yourself instead of dealing Harvest damage |
| Last Rites     | Bleed, Burn and Poison |                            All remaining | Deal all remaining damage at 35% enemy Health or less, then remove the harvested stacks |
| Death Sentence | Bleed, Burn and Poison |                                        1 | Add a flat +10% to Harvest and store it as Doom; Magical Damage after 15 seconds |

### Soul Siphon

Draw strength from your afflicted enemies. Harvest takes the next tick from each of your eligible Bleed, Burn and Poison stacks and combines that damage into one self-heal. You recover the resulting Harvest amount instead of dealing it to the enemy.

**Base healing = consumed raw tick damage × Harvest multiplier.** Apply Healing Power and ordinary healing-received modifiers, including Wound and Recovery, once; cap the result at missing Health. This heal cannot critically strike. Enemy defenses, Barrier, damage modifiers and the triggering hit's damage do not scale it. It is a self-heal, not Lifesteal, and cannot initiate another Harvest.

Consumption still occurs at full Health; overhealing is lost and grants no Barrier or stored healing. This form trades future condition damage for sustain, so it favors builds that keep applying conditions while taking sustained damage.

Two Bleed stacks, one Burn stack and one Poison stack storing 10 per tick contribute 40 raw damage when each has at least one future tick left; mastery level 10 restores up to **48 Health** before healing modifiers and missing-Health limits. Those consumed ticks deal no damage now or later.

### Last Rites

Save your Harvest for weakened enemies. When your direct hit and its ordinary reactions leave an enemy at **35% Health or less**, Last Rites deals all the damage still waiting in your eligible Bleed, Burn and Poison stacks at once, then removes those stacks. Above that threshold, Harvest takes nothing and the conditions keep dealing damage on their normal schedules.

When the threshold is met, consume **all unpaid future ticks from every eligible character-applied Bleed, Burn and Poison stack** on the enemy hit. Apply the Harvest multiplier to their combined raw damage and deliver immediate typed damage using the base form's packet rules. All selected stacks are exhausted and removed; their consumed ticks cannot resolve later. Conditions applied during the current cast remain ineligible until a later cast.

At mastery level 10 without upgrades, one Bleed stack with two ticks remaining and one Poison stack with three ticks remaining, each storing 10 per tick, contribute 50 raw damage. Last Rites delivers **24 Physical and 36 Magical Damage** before modifiers and mitigation. This is ordinary damage and does not guarantee a kill.

This form preserves condition damage until a finishing window opens, then cashes out the entire eligible future budget. Closing Hand always qualifies when Last Rites activates; empowering it does not raise Last Rites' own 35% threshold. Neither ordinary nor empowered Deep Roots can qualify because no harvested stack retains an unpaid future tick.

### Death Sentence

Save your Harvest for a later blow. At any enemy Health percentage, take the next tick from each of your eligible Bleed, Burn and Poison stacks. Add **an additional flat +10%** to Harvest's multiplier and store the result as **one independent Doom stack on that enemy**, instead of dealing immediate Harvest damage.

**Stored Doom damage = consumed raw tick damage × (1.10 + 0.10 from Death Sentence + 0.01 × mastery level + qualifying upgrade bonuses).** The stack is applied immediately and deals its stored amount as **Magical Damage exactly 15 seconds later**, including the portion taken from Bleed. A later Harvest creates another stack with its own amount and timer; it never refreshes or combines existing Doom.

Before upgrades, this means **123% at mastery 3** and **130% at mastery 10**. At mastery 10 with two qualifying upgrades, it reaches **140%**: 110% base +10% flat from this form +10% flat from mastery +5% flat +5% flat. Combat, the API preview and the mastery panel include the form's extra flat +10%.

This uses a Harvest-funded application of [Doom](../../combat-lexicon/conditions/doom.md): it supplies an explicit stored damage amount rather than ordinary `Doom(X)`'s percentage of current Power. Keep the consumed ticks' original Power snapshots through that amount; do not resnapshot current Power or apply the Harvest multiplier again at detonation. Ordinary Essence-authored `Doom(X)` remains unchanged. The style's application does not publish condition-application reactions.

Do not apply source-family damage modifiers or defenses while banking the amount. At detonation, use ordinary Doom stored-damage handling and its applicable modifiers once, with Resistance, Magic Penetration, damage reduction and Barrier. It is noncritical and generates no Lifesteal, reflection, on-hit or periodic-tick reactions. Doom is never eligible for Harvest, so neither its application nor its detonation can harvest or bank itself again.

Ward can block the application after consumption without restoring the source ticks. Cleanse removes the earliest-triggering Doom stack, with application order breaking ties, without detonating it. Target death and encounter end discard pending stacks without damage or transfer; the Reaper's death alone does not cancel them. Status Resistance does not shorten the 15-second delay. Due Doom detonations resolve before active casts at the same timestamp. The current standard-condition runtime has no separately authored Doom-immunity rule; Reaper uses its existing Ward and Cleanse handling.

At mastery level 10 without upgrades, consuming one tick from each of three stacks storing 10 per tick banks **39 Magical Damage** before delivery modifiers and defenses. A Harvest at time 1 applies Doom immediately for detonation at time 16. The three consumed ticks are skipped at their original due times; any unconsumed ticks keep their schedules.

This form trades immediate damage for a stronger delayed Magical hit, including damage originally stored as Physical Bleed. The extra flat +10% rewards waiting, but Doom can still be prevented, cleansed or lost when the enemy dies before it detonates. Compare that larger payout with the base form's immediate damage in playtesting.

## Upgrades and Upgrade Mastery

All three upgrades become available with the first slot at level 5. The second slot opens at level 8. Equip at most two different upgrades; at level 9, empower one equipped upgrade in its existing slot.

| Upgrade      | Ordinary effect                                                                                                               | Empowered effect at level 9                                                                                           |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Closing Hand | Add **a flat +5%** when the harvested enemy is at or below **35% Health**.                                           | The same bonus qualifies at or below **50% Health**.                                                                  |
| Crosscut     | Add **a flat +5%** when this Harvest consumes ticks from at least **two different condition families** on the enemy. | The same bonus also qualifies when it consumes ticks from at least **three distinct stacks**, even within one family. |
| Deep Roots   | Add **a flat +5%** when **every harvested stack** retains at least one unpaid future tick after consumption.         | The same bonus qualifies when **at least one harvested stack** retains an unpaid future tick.                         |

Each upgrade contributes its bonus once per Harvest, evaluated separately for the enemy hit. Closing Hand checks the opponent's current Health after the direct hit's reactions, before the Harvest payout. The hit does not need to cross the threshold: opponents already at or below it qualify, including when the triggering hit only damages their Barrier. Evaluate Crosscut from the selected families/stacks and Deep Roots from their remaining tick budgets immediately after consumption.

Qualification can be determined from the same selection before the payout resolves, so all packets in that enemy's Harvest use one multiplier. Soul Siphon applies that multiplier to healing; Death Sentence locks it into Doom's stored amount at application and never reevaluates upgrades at detonation. Crosscut counts the consumed source families before conversion to healing or Doom. Multiple qualifying conditions on an empowered upgrade still provide one flat +5% bonus.

At mastery level 10, two qualifying upgrades produce **130% of consumed damage** for the base form, Soul Siphon and Last Rites: 110% base +10% flat from mastery levels +5% flat +5% flat. Death Sentence's extra flat +10% brings its stored amount to **140%** instead. Mastery broadens an upgrade's condition; it does not add another copy of its bonus. Last Rites must use Closing Hand and Crosscut to receive both bonuses, since Deep Roots cannot qualify.

Removing the empowered upgrade clears its mastery choice. Retaining an upgrade without mastery preserves its ordinary effect.

## Opening Technique — level 7

**Grave Seed:** At battle start, apply **Poison(5)** to the living enemy with the highest Max Health. Resolve ties by the encounter's stable enemy order.

These are five ordinary Poison stacks attributed to the character. They share one application snapshot and schedule, as in the existing runtime: they snapshot the character's Power at application and tick every 2 seconds for 12 seconds. Normal Poison prevention, duration and removal rules apply. The opening makes one application attempt to its initial target. The latest catalog supplies five stacks to new snapshots; previously committed battles retain their captured opening amount.

Grave Seed activates automatically once per battle with every form. It does not count as an Essence cast and does not publish Essence-hit or condition-application reactions. Its stacks are present before the character's first active cast and can be harvested normally by every form.

Grave Seed does not repeat on later waves or after revival. If no living enemy is present at battle start, the opening is skipped for that battle.

At Power 1,000, the opening seeds 300 raw scheduled damage in total. A base-form Harvest at level 7, before the first Poison tick, advances 50 raw damage and delivers **58.5** before modifiers and mitigation; the other 250 raw damage remains scheduled.

## Milestones

| Mastery level | Benefit                                                                                    |
| ------------: | ------------------------------------------------------------------------------------------ |
|             0 | Full Harvest mechanic; 110% of consumed damage.                                            |
|             1 | 111% Harvest multiplier before upgrades.                                                   |
|             2 | 112% Harvest multiplier before upgrades.                                                   |
|             3 | 113% Harvest multiplier before upgrades. Refinement choice.                                |
|             4 | 114% Harvest multiplier before upgrades.                                                   |
|             5 | 115% Harvest multiplier before upgrades. First upgrade slot; all three upgrades available. |
|             6 | 116% Harvest multiplier before upgrades.                                                   |
|             7 | 117% Harvest multiplier before upgrades. Grave Seed applies Poison(5) at battle start.     |
|             8 | 118% Harvest multiplier before upgrades. Second upgrade slot.                              |
|             9 | 119% Harvest multiplier before upgrades. Empower one equipped upgrade.                     |
|            10 | 120% Harvest multiplier before upgrades. Maximum Reaper level.                             |

The multiplier column describes the base form, Soul Siphon and Last Rites. Death Sentence adds a flat +10% at every level where the refinement is available.

## Example builds

- **Mixed pressure:** Base form with Crosscut and Deep Roots; empower Crosscut. Mix condition sources and direct attacks. Harvesting three stacks worth 10 each at mastery level 10, with both upgrade conditions met, consumes 30 and deals 39 before defenses.
- **Soul-fed sustain:** Soul Siphon with Closing Hand and Crosscut; empower Crosscut. One Bleed, one Burn and one Poison stack storing 10 per tick, with one tick consumed from each, contribute 30 raw damage. Against an enemy at 30% Health, mastery level 10 with both bonuses restores up to 39 Health before healing modifiers and missing-Health limits, replacing those ticks' damage.
- **Prepared execution:** Last Rites with Closing Hand and Crosscut; empower Crosscut. Against an enemy at 30% Health, consume all remaining ticks from one Bleed stack with two ticks and one Poison stack with three ticks, each storing 10 per tick. At mastery level 10 both bonuses qualify: consume 50 raw damage and deal 26 Physical plus 39 Magical Damage before defenses. No future ticks remain on either stack.
- **Deferred sentence:** Death Sentence with Crosscut and Deep Roots; empower Deep Roots. Harvest one tick each from one Bleed and one Poison stack storing 10 per tick, with at least one future tick remaining on either stack after consumption. At mastery level 10 both bonuses qualify: consume 20 raw damage and bank 28 Magical Damage as one Doom stack, due 15 seconds later. Its stored amount stays 28 even if the source conditions expire before detonation.

Only one Combat Style is equipped. An ally using Bastion or Conduit keeps their own mechanics and conditions; Reaper does not spend their applications. The same ownership boundary applies between multiple Reapers.

## Playtest checks

- Compare slow and fast Essence activations against short enemies and durable bosses. Verify that each form's payout is noticeable and that every consumed tick leaves the future budget exactly once.
- Check direct hits at a condition's due timestamp, a final tick's expiration, cleanse reactions and target death. Validate the defined tick-first and consume-once outcomes.
- Compare a many-hit single-target Essence with a single-hit Essence, then area attacks. Confirm each qualifying direct hit consumes only the ticks still available on its target, and assess how quickly repeated hits exhaust condition budgets.
- Test mixed Physical/Magical defenses, Barrier, Guard on the triggering hit, direct-hit criticals and damage reactions. Immediate Harvest damage must retain its source type/channel; Death Sentence must convert the full bank to Magical Doom without applying damage modifiers twice.
- Test Soul Siphon at full and missing Health, with Healing Power, Wound and Recovery. Confirm noncritical self-healing, lost overhealing, no damage from consumed ticks and no dependence on enemy mitigation or the direct hit's damage.
- Verify the base form, Soul Siphon and Death Sentence consume exactly one future tick per eligible stack on each qualifying hit. Test Last Rites just above, exactly at and below 35% Health after direct-hit reactions; it must consume nothing above the threshold and all eligible future ticks at or below it. Check exhausted-stack removal, no repeated payout on later hits, Deep Roots never qualifying and empowered Closing Hand leaving the activation threshold unchanged.
- Test independent Doom timers, detonation at an active-cast timestamp, Ward, single-stack Cleanse, source death, target death and encounter end. Confirm no refunds, early detonations, transfer, recursive Harvest or second mastery/upgrade multiplier. Changing Power or upgrade eligibility after banking must not change the stored amount.
- Check opening ownership, summon-applied conditions, ally-applied stacks and multiple Reapers. Verify every form with Bleed alone, Burn alone, Poison alone and all three together; Grave Seed's Poison must be eligible in every form. Check mixed-family Soul Siphon healing and the base form's typed damage on each target separately.
- Verify Death Sentence's flat +10% is added once before banking: at mastery 10, 30 raw damage should store 39 without upgrades, and 20 raw damage should store 28 with two qualifying upgrades. Check that the preview matches and detonation does not add the form bonus again.
- Assess every refinement with each upgrade pair, including mastery at level 9. Compare actual combat gains with the base form's 10–30% bonus on consumed raw damage and Death Sentence's 23–40% bonus across mastery 3–10, separating immediate damage, effective healing and Doom that actually survives to detonate. Assess Death Sentence's larger Magical payout against its delay and removal risk, and Last Rites' full-budget payout against the loss of early Harvest damage.

## Reference rules

These references describe the shared combat behavior used by Reaper.

- [Bleed](../../combat-lexicon/conditions/bleed.md), [Burn](../../combat-lexicon/conditions/burn.md) and [Poison](../../combat-lexicon/conditions/poison.md): independent stacks, stored damage, ownership and exact tick schedules.
- [Doom](../../combat-lexicon/conditions/doom.md): independent 15-second timers, Magical delivery, Ward prevention and removal; Death Sentence supplies its stored amount from Harvest.
- [Wound](../../combat-lexicon/conditions/wound.md) and [Recovery](../../combat-lexicon/conditions/recovery.md): ordinary healing-received modifiers for Soul Siphon.
- [Damage types](../../combat-lexicon/damage-types.md) and [damage categories](../../combat-lexicon/damage-categories.md): typed mitigation and terminal stored-damage defaults.
- [Barrier](../../combat-lexicon/conditions/barrier.md) and [Guard](../../combat-lexicon/conditions/guard.md): ordinary absorption and direct-hit protection.
- [Combat Styles overview](../README.md): the shared progression and configuration structure Reaper uses.

## Implementation and verification

The catalog captures Death Sentence's additional flat +10% as `deathSentenceBonus` in Reaper tuning. The Harvest runtime and API preview share the same refinement-aware multiplier, and the frontend receives the bonus through the tuning DTO. It is applied when banking damage, not again when Doom detonates. Soul Siphon, Last Rites and Closing Hand retain their existing gameplay rules.

Grave Seed's catalog tuning supplies five Poison stacks to the opening runtime and preview. The tuning model's fallback default remains two. Historical snapshots without `deathSentenceBonus` use zero and omit it again when serialized, preserving their old payout and serialized identity. Committed snapshots continue using the opening count they captured; new snapshots use catalog `combat-styles.v8`.

- [Catalog](../../../LL/src/API/API.LL/Data/combat-styles/combat-styles.v1.json): choices, descriptions and snapshot tuning.
- [Harvest runtime](../../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.Reaper.cs): consumption, payout and upgrade qualification.
- [Reaper tests](../../../LL/tests/EssenceSystem.Tests/CombatStyleReaperEngineTests.cs): ownership, tick schedules, all forms, upgrades, prevention, cleanup and logless outcomes.

Encounters containing Reaper resolve condition ticks before active attacks, starting after the first elapsed combat tick. Encounters without Reaper retain their historical ordering. Existing committed Bastion and Conduit snapshots omit the optional Reaper tuning and keep their serialized identity. No schema migration is required; the API catalog and frontend changes must be released together. No deployment is performed by this change.
