# Equipment Set Blueprints and Bonus Catalog

Status: content and balance proposal. Set membership exists, but none of the
bonuses in this document are implemented or assigned to production Blueprints
yet.

## Purpose

This document proposes one equipment set for each current crafting Blueprint.
It defines the intended identity, reachable thresholds, and candidate bonuses
for the later set-benefit implementation.

The numbers are deliberately concrete enough to implement and test, but they
are provisional until they have been run through combat calibration. Mechanics,
thresholds, and set identities are the primary decisions; individual values are
balance levers.

## Global rules

- A Blueprint receives one `equipmentSetId`, and every equipment instance
  crafted with that Blueprint snapshots the same set ID.
- Bonuses count distinct equipped equipment instance IDs. There are no required
  named pieces.
- Separate copies crafted from the same Blueprint count separately.
- A two-handed weapon occupying both hand slots counts once.
- Threshold bonuses are cumulative. At four equipped items, both the two-item
  and four-item bonuses are active.
- Replacing an equipped set item with another instance from the same set does
  not change active thresholds.
- The highest authored threshold is the full-set bonus even when more compatible
  equipment instances could be equipped.
- Static bonuses apply once per active threshold, never once per contributing
  item.
- Triggered bonuses are granted passive abilities and use the existing combat
  trigger, condition, status, and effect system.
- Current combat timing is ten ticks per second. Durations below are written in
  seconds for readability and should be authored as ticks.
- All internal cooldowns are per character unless a bonus explicitly says
  "per target."

## Numeric language

The proposal uses two deliberately different forms of percentage:

- **Percentage points** add directly to an attribute already measured as a
  percentage. For example, `+5 percentage points Crit Chance` changes 20% to
  25%.
- **Percent of total** is a multiplicative modifier. For example, `+10% total
  Max Health` changes 1,000 to 1,100 after flat and additive contributions.

Armor and Resistance equipment values are ratings, so their set bonuses are
described as percentages of the final rating rather than percentage points of
damage reduction. Power, Max Health, and Health Regen use the same percent-of-
total wording where appropriate.

Damage dealt, damage taken, healing received, barriers, condition application,
and resource restoration are combat effects rather than ordinary item
attribute modifiers.

## Threshold policy and reachability

Reachability is based on the current compatible recipes and the nine equipment
slots. The Tool slot is not used by any of these sets. Main Hand and Off Hand
can each hold a separate one-handed instance where the existing hand rules
permit it.

| Blueprint | Proposed set ID | Current maximum | Bonuses at | Intended role |
| --- | --- | ---: | --- | --- |
| Fury | `set_fury` | 5 | 2, 4 | Critical-hit momentum |
| Arcane | `set_arcane` | 5 | 2, 4 | Repeated-cast Power windows |
| Execution | `set_execution` | 5 | 2, 4 | Finishing wounded enemies |
| Aegis | `set_aegis` | 7 | 2, 4, 6 | Barrier-backed mitigation |
| Warden | `set_warden` | 7 | 2, 4, 6 | Durable recovery under pressure |
| Endurance | `set_endurance` | 6 | 2, 4, 6 | Attrition and status resilience |
| Phoenix | `set_phoenix` | 8 | 2, 4, 6 | Emergency recovery and rebirth |
| Spirit | `set_spirit` | 8 | 2, 4, 6 | Healing and ally protection |
| Primal | `set_primal` | 5 | 2, 4 | Power scaling from living summons |
| Venom | `set_venom` | 2 | 2 | Basic-attack Poison application |
| Hive | `set_hive` | 2 | 2 | Basic-attack-driven speed windows |
| Raidforged | `set_raidforged` | 8 | 2, 4, 6, 8 | Raid-grade hybrid ramping |
| Gravebound | `set_gravebound` | 8 | 2, 4, 6, 8 | Retaliation and death-defying sustain |

Weapon/accessory Blueprints stop at four pieces even though five may be
equipped. This leaves one flexible slot and avoids making dual wield mandatory.
Phoenix and Spirit stop at six so they can support mixed-set builds. The two
raid Blueprints alone receive eight-item capstones, giving a full raid-crafted
loadout a distinctive long-term target.

Venom and Hive currently support only weapon recipes. Their two-item bonus is
therefore their full-set bonus. Adding a three- or four-item threshold would be
unreachable without first expanding their compatible recipes.

## Blueprint proposals

### Fury — `set_fury`

Source: Goblin Mines dungeon. Current profile emphasizes Power, Crit Chance,
and Crit Damage. Compatible outputs cover weapons, Necklace, Relic, and Ring.

Set identity: direct critical hits build short-lived offensive momentum.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Kindling:** +5 percentage points Crit Chance. | Static attribute modifier |
| 4 | **Unbound Fury:** A direct critical hit grants one Fury stack for 5 seconds, up to five stacks. Each stack increases damage dealt by 2%. A stack can be gained at most once every 0.5 seconds; gaining a stack refreshes the duration. | Passive ability using `OnHit`, `EventWasCritical`, and a stacking condition |

The gain limiter prevents one area or multi-hit event from filling the entire
stack count immediately. At maximum stacks the bonus is +10% damage dealt,
which is strong but requires ongoing critical hits.

### Arcane — `set_arcane`

Source: Forgotten Catacombs dungeon. Current profile emphasizes Power, Magic
Penetration, Cooldown Reduction, and Crit Chance. Compatible outputs cover
weapons, Off Hand, Necklace, Relic, and Ring.

Set identity: repeated active ability use creates a controlled Power window.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Arcane Attunement:** +5 percentage points Magic Penetration and +3 percentage points Cooldown Reduction. | Static attribute modifiers |
| 4 | **Arcane Surge:** Every third active ability grants +15% Power for 6 seconds. Reapplying the effect refreshes its duration rather than stacking it. | Passive ability using `OnAbilityUsed`, `everyNthOccurrence`, and a refreshing timed Power modifier |

Only active abilities count. The recurring trigger has no separate cooldown, so
every third qualifying use activates Arcane Surge even while an earlier Surge is
active; in that case the existing +15% modifier is replaced and its six-second
duration starts again.

### Execution — `set_execution`

Source: Tangled Cave dungeon. Current profile emphasizes Power, Armor
Penetration, and Crit Damage. Compatible outputs cover weapons, Necklace,
Relic, and Ring.

Set identity: specialize in ending targets that have already been wounded.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Find the Opening:** +6 percentage points Armor Penetration and +8 percentage points Crit Damage. | Static attribute modifiers |
| 4 | **No Escape:** Deal 18% more damage to enemies at or below 30% Health. | Conditional passive using target-health checks and `ModifyDamageDealt` |

The bonus should evaluate the target's health immediately before each hit. It
should not retroactively amplify a hit that merely pushes the target below the
threshold.

### Aegis — `set_aegis`

Source: Great Tree dungeon. Current profile emphasizes Armor, Resistance, Max
Health, and Damage Reduction. Compatible outputs cover armor, Off Hand,
Necklace, Relic, and Ring.

Set identity: defenses become substantially stronger while protected by a
barrier.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Fortified:** +10% total Armor rating and +10% total Resistance rating. | Multiplicative static attribute modifiers |
| 4 | **Raised Aegis:** At combat start, gain a barrier equal to 10% of Max Health for 15 seconds. | Passive ability using `OnCombatStart` and `GrantBarrier` |
| 6 | **Behind the Shield:** While any barrier remains, take 10% less damage. When the last barrier contribution breaks or expires, this mitigation ends immediately. | Maintained passive using barrier events/state and `ModifyDamageTaken` |

The six-item effect applies based on having a barrier, regardless of which
ability created it. The start-of-combat barrier provides a reliable opening but
does not permanently maintain the capstone by itself.

### Warden — `set_warden`

Source: Great Tree dungeon. Current profile emphasizes Max Health, Crowd Control
Resistance, Armor, and Health Regen. Compatible outputs cover armor, Off Hand,
Necklace, Relic, and Ring.

Set identity: stabilize after taking sustained pressure rather than preventing
the opening hit.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Deep Roots:** +10% total Max Health. | Multiplicative static attribute modifier |
| 4 | **Unyielding Growth:** +25% total Health Regen and +5 percentage points Crowd Control Resistance. | Static attribute modifiers |
| 6 | **Warden's Refuge:** When Health falls to or below 40%, gain Renewal and 15% damage reduction for 8 seconds. This has a 30-second internal cooldown. | Passive ability using `OnHealthChanged`, a health condition, status application, and `ModifyDamageTaken` |

The trigger occurs only when crossing from above 40% to at or below 40%, so
repeated small heals around the boundary cannot retrigger it during cooldown.

### Endurance — `set_endurance`

Source: Forgotten Catacombs dungeon. Current profile emphasizes Health Regen,
Max Health, Armor, and Status Resistance. Compatible outputs cover armor,
Necklace, Relic, and Ring.

Set identity: outlast damage-over-time and other non-control harmful effects.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Second Wind:** +25% total Health Regen. | Multiplicative static attribute modifier |
| 4 | **Weather the Storm:** +8% total Max Health and +5 percentage points Status Resistance. | Static attribute modifiers |
| 6 | **Tested Resolve:** When a harmful status or condition is applied to you, restore 3% of Max Health and gain +10 percentage points Status Resistance and Crowd Control Resistance for 6 seconds. This has an 8-second internal cooldown. | Passive ability using `OnStatusApplied`/`OnStatusChanged`, healing, and timed attribute modifiers |

One application event can trigger only one heal even if it creates several
stacks. The temporary resistance is allowed to reach normal attribute caps but
does not bypass them.

### Phoenix — `set_phoenix`

Source: Goblin Mines dungeon. Current profile emphasizes Health Regen, Healing
Power, Max Health, and Status Resistance. Compatible outputs cover armor,
weapons, Necklace, Relic, and Ring.

Set identity: stronger recovery when wounded, culminating in a once-per-combat
rebirth.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Warmth of Embers:** +8 percentage points Healing Power. | Static attribute modifier |
| 4 | **Rising Flame:** While below 50% Health, receive 15% more healing from all sources. | Maintained passive using a self-health condition and `ModifyHealingReceived` |
| 6 | **Rebirth:** Once per combat, when Health falls to or below 20%, restore 25% of Max Health, cleanse harmful non-crowd-control conditions, and gain Renewal for 8 seconds. | Passive ability using `OnHealthChanged`, healing, cleanse, and status application |

Rebirth triggers after the damaging event resolves and therefore is not a
death-prevention effect. A lethal hit still defeats the character. If true
cheat-death behavior is desired later, it needs an explicit pre-death engine
operation rather than relying on `OnDeath` ordering.

### Spirit — `set_spirit`

Source: Great Tree dungeon. Current profile emphasizes Healing Power,
Resistance, Cooldown Reduction, and Health Regen. It supports every non-Tool
equipment slot.

Set identity: convert sustained healing into protection for allies.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Gentle Current:** +8 percentage points Healing Power. | Static attribute modifier |
| 4 | **Clear Mind:** +5 percentage points Cooldown Reduction and +10% total Resistance rating. | Static attribute modifiers |
| 6 | **Sheltering Spirit:** When you directly heal another ally, grant that ally a barrier equal to 5% of their Max Health. This has a 4-second internal cooldown. | Passive ability using `OnHeal`, ally/event-source checks, and `GrantBarrier` |

Self-healing does not trigger Sheltering Spirit. Periodic healing may trigger it
only if the engine event is attributed directly to the set wearer; the internal
cooldown prevents every healing tick from creating another barrier.

### Primal — `set_primal`

Source: Great Tree dungeon. Current profile emphasizes Power and Max Health,
with a summon and nature identity. Compatible outputs cover weapons, Off Hand,
Necklace, Relic, and Ring.

Set identity: the wearer and their owned summons reinforce one another.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Primal Vigor:** +6% total Power and +6% total Max Health. | Multiplicative static attribute modifiers |
| 4 | **Strength of the Pack:** Gain +5% Power for every living summon, up to three. | Maintained passive using `OnSummonChanged` and `SynchronizeAttributePerOwnedSummon` |

Only living summons owned by the wearer count; allied summons belonging to
another character do not. The Power bonus caps at +15% even when more than three
owned summons are alive.

### Venom — `set_venom`

Source: Tangled Cave dungeon. Current profile emphasizes Power, Life Steal,
Crit Chance, and Attack Speed. It currently supports only one- and two-handed
weapon recipes.

Set identity: two equipped Venom weapons add a concentrated Poison application
to every Basic Attack.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Venomous Assault:** Basic Attack applies Poison(5). | Passive ability using `OnBasicAttack` and condition application |

Only the wearer's Basic Attacks trigger the effect. Active abilities, periodic
damage, and other direct hits do not apply this set's Poison.

### Hive — `set_hive`

Source: Tangled Cave dungeon. Current profile emphasizes Attack Speed, Status
Resistance, Crit Chance, and Life Steal. It currently supports only the Dagger
recipe, so its set is completed by dual-wielding two separately crafted Hive
Daggers.

Set identity: rapid basic attacks create recurring Attack Speed windows.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Brood Cycle:** Every fifth Basic Attack grants +15% Attack Speed for 6 seconds. Reapplying the effect refreshes its duration rather than stacking it. | Passive ability using `OnBasicAttack`, `everyNthOccurrence`, and a refreshing timed Attack Speed modifier |

Brood Cycle counts Basic Attacks continuously. Reaching another fifth attack
while the speed bonus is active replaces the existing modifier and restarts its
six-second duration, preserving the cadence without allowing multiple +15%
bonuses.

### Raidforged — `set_raidforged`

Source: Hives' Abyss raid boss. Current profile is a hybrid of Power, Armor,
Resistance, and Max Health. It supports every non-Tool equipment slot.

Set identity: raid-grade equipment tempers the wearer through repeated incoming
damage, then converts maximum heat into a short power window.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Raid Temper:** +8% total Armor rating and +8% total Resistance rating. | Multiplicative static attribute modifiers |
| 4 | **Forged for War:** +8% total Power and +8% total Max Health. | Multiplicative static attribute modifiers |
| 6 | **Battle Heat:** Taking direct damage grants one Heat stack for 8 seconds, up to five stacks. Each stack increases damage dealt by 1% and reduces damage taken by 1%. Heat can be gained at most once per second; gaining Heat refreshes its duration. | Passive ability using `OnDamaged`, direct-hit checks, and a stacking condition |
| 8 | **Masterwork:** Reaching five Heat consumes all Heat and grants Forgeguard for 8 seconds, increasing damage dealt by 10% and reducing damage taken by 10%. This has a 20-second internal cooldown. Heat can build normally while Masterwork is on cooldown. | Passive ability using condition-stack checks/consumption and timed output modifiers |

Masterwork replaces the five-stack Battle Heat effect during its window instead
of stacking with it. After Forgeguard ends, newly accumulated Heat resumes its
normal six-item effect.

### Gravebound — `set_gravebound`

Source: Sanguine Horror raid boss. Current profile emphasizes Max Health,
Health Regen, Damage Reduction, and Power. It supports every non-Tool equipment
slot.

Set identity: absorb punishment, retaliate against attackers, and survive one
otherwise dangerous low-health state.

| Equipped | Bonus | Implementation kind |
| ---: | --- | --- |
| 2 | **Bound in Blood:** +10% total Max Health. | Multiplicative static attribute modifier |
| 4 | **Restless Flesh:** +25% total Health Regen and +3 percentage points Damage Reduction. | Static attribute modifiers |
| 6 | **Grave's Answer:** When directly damaged by an enemy, retaliate for shadow damage equal to 20% of the post-mitigation damage received, capped at 50% of the wearer's Power per event. This can trigger at most once per second. | Passive ability using `OnDamaged`, event-source checks, and `Damage` |
| 8 | **Refuse the Grave:** Once per combat, when Health falls to or below 20%, restore 30% of Max Health and gain Empower, Guard, and Thorns for 10 seconds. | Passive ability using `OnHealthChanged`, healing, and condition application |

Like Phoenix Rebirth, Refuse the Grave resolves after the triggering damage and
does not save a character from a lethal hit. Its larger recovery and offensive
retaliation are justified by requiring all eight non-Tool slots and raid-sourced
crafting.

## Initial content mapping

When the catalog is approved, the production Blueprint records should receive
these associations:

| Blueprint ID | `equipmentSetId` |
| --- | --- |
| `blueprint_fury` | `set_fury` |
| `blueprint_arcane` | `set_arcane` |
| `blueprint_execution` | `set_execution` |
| `blueprint_aegis` | `set_aegis` |
| `blueprint_warden` | `set_warden` |
| `blueprint_endurance` | `set_endurance` |
| `blueprint_phoenix` | `set_phoenix` |
| `blueprint_spirit` | `set_spirit` |
| `blueprint_primal` | `set_primal` |
| `blueprint_venom` | `set_venom` |
| `blueprint_hive` | `set_hive` |
| `blueprint_raidforged` | `set_raidforged` |
| `blueprint_gravebound` | `set_gravebound` |

This is a one-Blueprint-to-one-set initial mapping. The domain should still
allow several Blueprints to reference the same set later; set identity belongs
to the set ID, not to the Blueprint ID.

## Recommended implementation order

Implement benefits in increasing order of engine complexity:

1. Add the threshold/effect schema and pure distinct-instance set resolver.
2. Implement static bonuses and calibrate Fury 2, Arcane 2, Execution 2, Aegis
   2, Warden 2/4, Endurance 2/4, Phoenix 2, Spirit 2/4, Primal 2, Raidforged
   2/4, and Gravebound 2/4.
3. Implement one conditional damage vertical slice with Execution 4.
4. Implement maintained state with Aegis 6 and Phoenix 4.
5. Implement stacking-condition bonuses with Fury 4 and Raidforged 6/8, plus
   recurring trigger bonuses for Arcane 4, Venom 2, and Hive 2.
6. Implement event-driven recovery/support with Warden 6, Endurance 6, Phoenix
   6, Spirit 6, and Gravebound 8.
7. Implement summon integration last with Primal 4.

The first release can enable only calibrated static thresholds while leaving
complex thresholds visible as "coming later." A set should never silently show
a bonus as active before its gameplay effect is enabled.

## Balance and validation gates

Before enabling a set in production:

- Validate every threshold is reachable from that Blueprint's currently
  compatible recipe outputs.
- Compare the threshold against equivalent ordinary equipment budget at early,
  middle, and endgame tiers.
- Run single-target, multi-target, short-burst, and long-sustain combat samples.
- Test solo and party value separately for Spirit, Warden, Aegis, and summon
  effects.
- Verify percentage-point attributes respect their existing caps.
- Verify a two-handed instance counts once and two one-handed instances count
  twice.
- Verify multi-hit and area events respect per-character and per-target gain
  limiters.
- Verify snapshot combat retains the active thresholds captured for that run.
- Verify triggers cannot recursively activate themselves or other set triggers
  without the normal repeat/echo restrictions.
- Verify Phoenix 6 and Gravebound 8 do not claim to prevent lethal damage.
- Verify Arcane and Hive refresh their timed modifiers without increasing the
  modifier amount.

## Decisions intentionally deferred

- Final numerical tuning after combat calibration.
- Player-facing set display names beyond the Blueprint names.
- Exact status/condition definition IDs for Fury, Heat, and Forgeguard.
- Whether complex bonuses launch together or in staged content updates.
- Whether retired Blueprints keep crafting enabled while their bonuses remain
  active for existing items.

None of these deferred decisions changes the membership model: the Blueprint
stamps `equipmentSetId` at craft time, and active thresholds depend only on the
number of distinct matching equipment instances currently equipped.
