# Spellweaver

**Status:** Design proposal — not implemented. All values below are starting points for playtesting.

Let steel prepare the spell, and let the spell prepare the next strike. Casting a Physical Essence sets up your next Magical Essence, and casting a Magical Essence sets up your next Physical Essence. Switching between them creates a **Weave**, making the new attack stronger.

Spellweaver rewards a loadout whose Physical and Magical attacks complement each other's cooldowns. You do not need a particular weapon, and you do not choose attacks manually. Essences cast automatically as usual; the order they actually cast determines the benefit.

The core mechanic, **Spellweaving**, starts at mastery level 0. Equipping, saved configurations and XP follow the [shared Combat Style rules](../README.md).

## Spellweaving

Your first qualifying Essence cast begins the pattern at normal strength. A later cast of the opposite type creates a Weave and deals **125% of its normal direct damage**. That cast also prepares the next switch, so a sequence of Physical, Magical, Physical, Magical keeps weaving after the first cast.

Casting the same type again deals normal damage and keeps the opposite type prepared. It does not store multiple Weaves. There is no time limit on the preparation, and changing enemy targets does not interrupt it.

For example, at mastery level 0:

| Cast     | What happens           | Direct damage strength | Next type prepared |
| -------- | ---------------------- | ---------------------: | ------------------ |
| Physical | Begin the pattern      |                   100% | Magical            |
| Magical  | Switch types and Weave |                   125% | Physical           |
| Magical  | Repeat the same type   |                   100% | Physical           |
| Physical | Switch types and Weave |                   125% | Magical            |
| Magical  | Switch again and Weave |                   125% | Physical           |

A Weaving cast that normally deals 200 direct damage instead deals **250** before defenses. A repeated cast still deals its normal 200. Spellweaver does not delay a ready Essence to create a better sequence.

## Which Essences take part

For this proposal, Physical and Magical describe the direct damage in an Essence's current active ability, including its evolution. Its name, element tags and scaling attribute do not determine its Spellweaver type.

| Active ability's own direct damage      | Spellweaver type | Interaction                                       |
| --------------------------------------- | ---------------- | ------------------------------------------------- |
| Physical, with no Magical direct damage | Physical         | Prepares Magical; Weaves after Magical.           |
| Magical, with no Physical direct damage | Magical          | Prepares Physical; Weaves after Physical.         |
| Both Physical and Magical               | Mixed            | Deals normal damage and leaves the pattern alone. |
| Neither Physical nor Magical            | Neither          | Leaves the pattern alone.                         |

Determine the type from the active ability's authored direct-damage effects, including conditional branches, rather than changing it according to which targets were hit. Exclude passive effects, triggered damage, periodic effects and summons' own attacks from this classification. This gives each equipped Essence a stable, visible type for that battle.

Only direct effects whose damage type is **Physical** or **Magical** count. Untyped damage and effects using the separate Bleed, Burn or Poison damage types neither establish a type nor receive a Weave bonus. A Physical strike that also applies Burn still counts as Physical; Burn's later ticks do not make it a mixed attack.

Mixed abilities remain useful for their normal effects but do not build or spend preparation. This deliberately requires separate Physical and Magical actives to sustain the pattern. One mixed ability cannot alternate with itself by resolving its hits in a different order.

Basic attacks, healing-only or Barrier-only abilities, condition-only casts, passive effects, automatic repeats, triggered copies, reflection and summons' actions leave the pattern alone and receive no Weave bonus. They also do not reset any progress described below.

## Cast timing and damage rules

A qualifying cast must make at least one direct Physical or Magical attack attempt against a living enemy. A cast with no such attempt leaves the pattern and the Opening Technique unused. When the first attempt begins, decide whether the cast Weaves and lock its bonuses for the whole cast.

A miss or fully prevented hit still counts as an attempted cast: it can use preparation, begin the next side of the pattern and advance or break a sequence. There is no refund for an unsuccessful Weave. Effects that require damage to land, such as Warding Weave's Barrier, have their own success requirement.

After the cast and its immediate reactions finish, remember its type for the next qualifying cast, provided the Spellweaver is still alive. A multi-hit cast makes this decision once. Its later hits, triggered copies and repeats cannot create another Weave or change the type remembered. Separate normal casts at the same timestamp follow the combat engine's normal resolution order.

The bonus applies to all of the cast's own direct damage matching its Spellweaver type, across its normal targets. A Physical cast strengthens its Physical direct damage; a Magical cast strengthens its Magical direct damage. Each hit keeps its original critical rules, penetration and other modifiers. Armor, Resistance, damage reduction and Barrier apply normally after the damage bonus.

Healing, Barrier, conditions, damage over time, untyped damage and triggered secondary effects keep their normal amounts. Lifesteal uses the resulting damage under its normal rules, without receiving the Weave bonus again. A Weave creates no extra hit.

For every form:

**Weave damage = normal matching direct damage × (form strength + mastery-level bonus + applicable upgrade bonuses).**

All percentages inside the brackets add together. For example, **125% + 10% flat + 5% flat = 140%**, so a normal 200-damage hit becomes 280 before defenses. Casts that do not Weave stay at 100%, regardless of mastery level or upgrades.

## Mastery level bonuses

Every mastery level adds **a flat +1%** to Weave damage in every form. It does not increase the number of stored preparations, change cast order or strengthen a cast that does not Weave.

| Mastery level |        Weave bonus | Base-form Weave strength | Additional unlock                                |
| ------------: | -----------------: | -----------------------: | ------------------------------------------------ |
|             0 |  +0% flat increase |                     125% | Full core mechanic                               |
|             1 |  +1% flat increase |                     126% | —                                                |
|             2 |  +2% flat increase |                     127% | —                                                |
|             3 |  +3% flat increase |                     128% | Refinement choice                                |
|             4 |  +4% flat increase |                     129% | —                                                |
|             5 |  +5% flat increase |                     130% | First upgrade slot; all three upgrades available |
|             6 |  +6% flat increase |                     131% | —                                                |
|             7 |  +7% flat increase |                     132% | Opening Technique                                |
|             8 |  +8% flat increase |                     133% | Second upgrade slot                              |
|             9 |  +9% flat increase |                     134% | Empower one equipped upgrade                     |
|            10 | +10% flat increase |                     135% | Maximum mastery level                            |

These values are before upgrades. Spellweaver uses the same individual XP requirements and level-10 cap as the implemented styles, with no separate rank track.

## Refinements — mastery 3

Choose one refinement or keep the base form. All forms use the same Physical and Magical classification and work in both directions.

| Form                | Weave strength before levels and upgrades | What changes                                                                      |
| ------------------- | ----------------------------------------: | --------------------------------------------------------------------------------- |
| Base form           |                                      125% | Reliable extra damage each time you switch types.                                 |
| **Gathered Thread** |        110%, plus a flat +15% per Tension | Repeated casts of the same type build toward a stronger switch.                   |
| **Fraying Weave**   |                                      110% | Weaving attacks ignore 20% of the opponent's remaining Armor or Resistance.       |
| **Warding Weave**   |                                      110% | A Weave that lands damage also grants you Barrier equal to 3% of your Max Health. |

### Gathered Thread

Let repeated attacks draw the thread tight before you release it. Each qualifying cast of the same type as the previous one builds **1 Tension**, up to **2**. Your next Weave spends all Tension for **a flat +15% damage per Tension**, then starts preparing the opposite type as usual.

The first qualifying cast begins the pattern without building Tension. A switching cast spends Tension and cannot build it. Repeating at the cap keeps 2 Tension, without banking more. Misses still count under the normal cast rules, and actions outside the pattern leave Tension alone.

At mastery 10, a Weave uses **120%**, **135%** or **150%** strength with 0, 1 or 2 Tension before upgrades. For example, Physical, Physical, Physical, Magical produces two ordinary repeats, then a Magical Weave at 150%. This favors uneven cooldowns and a strong attack waiting on the other side of the pattern; the repeated casts themselves receive no damage bonus.

### Fraying Weave

Alternate attacks to work through an enemy's defenses. A Weaving Physical attack ignores **20% of the Armor remaining after normal Armor Penetration**. A Weaving Magical attack does the same to **Resistance after normal Magic Penetration**.

Multiply the remaining relevant defense rating by **0.8** for those hits, then use the normal mitigation calculation. This is not a flat increase to the character's penetration attribute. It cannot reduce a defense below zero and does not bypass other damage reduction or Barrier.

For example, if an enemy has 1,000 Armor and normal penetration leaves 700, Fraying Weave uses **560 Armor** for that cast's Physical hits. It does not remove 20% of the final damage reduction percentage.

At mastery 10, the Weave deals **120%** damage before upgrades and uses this reduced defense. The benefit ends with the cast. It places no debuff, does not affect allies' attacks, and does not change the opponent's stats for later damage. Against an enemy with no relevant defense, the extra penetration gives no benefit.

### Warding Weave

Work protection into your attacks. After a Weaving cast finishes and its immediate reactions resolve, gain **Barrier equal to 3% of your Max Health** if at least one matching direct hit removed enemy Health or Barrier and you are still alive.

Grant this Barrier once per Weaving cast. More hits or enemies do not multiply it. A fully missed or prevented cast grants none, though its place in the pattern still advances. A lethal hit can qualify, but the Barrier cannot protect you from an immediate reaction that resolved before the grant.

Use your Max Health at the time of the grant. Ordinary Barrier grant modifiers and the existing Barrier cap apply; overflow is discarded. This is a separate style grant, not an Essence effect, and receives no Weave damage multiplier. It follows normal Barrier behavior after being granted.

At mastery 10, a Warding Weave deals **120%** damage before upgrades. With 4,000 Max Health, a successful Weave requests **120 Barrier** before Barrier modifiers and the cap. Mastery levels improve its damage, while the Barrier remains 3% of Max Health.

## Upgrades — mastery 5 and 8

All three upgrades become available at mastery 5. Equip one at mastery 5 and up to two different upgrades at mastery 8. Each works with every form.

| Upgrade              | Effect                                                                                                                            |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| **Unbroken Pattern** | A Weave gains **a flat +5%** damage if the previous qualifying cast also Weaved.                                                  |
| **Quick Hands**      | A Weave gains **a flat +5%** damage if it begins within **4 seconds** of the previous qualifying cast.                            |
| **Mending Thread**   | A Weave cast while you have **35% Health or less** heals you for **2% of your Max Health** after it lands matching direct damage. |

Check these conditions when the cast's first qualifying attack attempt begins. Damage bonuses then remain fixed for all its matching direct hits. Mending Thread checks current Health divided by Max Health, excluding Barrier, at that same moment; falling below the threshold later in the cast does not qualify it.

Unbroken Pattern begins benefiting the second consecutive Weave. A same-type repeat breaks the sequence, including a repeat used to build Tension. Ignored actions do not break it, and a Weave that misses still counts.

Quick Hands measures time between the first qualifying attack attempts of the two casts. Exactly 4 seconds qualifies; a longer gap does not. It needs a real previous qualifying cast, so the battle's first cast cannot gain this bonus. A same-type repeat updates that timestamp even though the repeat receives no bonus.

Mending Thread requires at least one matching direct hit to remove enemy Health or Barrier. Request the heal once after the cast and its immediate reactions finish, provided you are alive. Use Max Health at the time of the heal, then apply normal healing modifiers, suppression and missing-Health limits. It cannot revive you, receives no Weave damage multiplier, and does not create another cast. With Warding Weave, resolve this heal before its Barrier grant.

## Opening Technique — mastery 7

**First Thread:** Begin each battle ready to Weave with either type. Your first qualifying Essence cast counts as a Weave and gains your selected form's normal Weave benefits, even without a preceding cast.

That cast then prepares the opposite type normally. It counts as the first Weave for Unbroken Pattern and begins Gathered Thread with 0 Tension. Quick Hands has no preceding cast to compare against. Warding Weave and Mending Thread still require damage to land, and Mending Thread still checks its Health threshold.

Mixed casts and other ignored actions leave First Thread waiting. The first qualifying attack attempt uses it even if it misses. First Thread happens once per battle in every form; it does not grant a second Weave within that cast or change any cooldown.

## Upgrade Mastery — mastery 9

Choose one equipped upgrade to empower. It keeps its existing slot and ordinary effect.

| Empowered upgrade    | Additional effect                                                                                              |
| -------------------- | -------------------------------------------------------------------------------------------------------------- |
| **Unbroken Pattern** | Its **flat +5%** damage applies to every Weave, including the first in a sequence.                             |
| **Quick Hands**      | Its **flat +5%** damage allows up to **8 seconds** between qualifying casts instead of 4.                      |
| **Mending Thread**   | Its **2% Max Health heal** can activate at any Health percentage. It still requires a Weave that lands damage. |

Mastery changes the upgrade's requirement; it does not add another copy of its ordinary bonus. Removing the empowered upgrade clears its mastery selection under the shared configuration rules.

## Worked examples

Consider a mastery-10 base-form Spellweaver with Unbroken Pattern and empowered Quick Hands. Each cast has one matching direct hit normally worth 200. First Thread is available, every attack lands, and the times below mark each cast's first qualifying attack attempt.

|       Time | Cast     | What applies                                                     | Damage strength | Damage before defenses |
| ---------: | -------- | ---------------------------------------------------------------- | --------------: | ---------------------: |
|  0 seconds | Physical | First Thread; mastery bonus                                      |            135% |                    270 |
|  3 seconds | Magical  | Weave; mastery; Unbroken Pattern; Quick Hands                    |            145% |                    290 |
|  5 seconds | Magical  | Same-type repeat; break Unbroken Pattern                         |            100% |                    200 |
| 11 seconds | Physical | Weave; mastery; Quick Hands within its empowered 8-second window |            140% |                    280 |
| 21 seconds | Magical  | Weave; mastery; Unbroken Pattern; too late for Quick Hands       |            140% |                    280 |

These five casts deal **1,320** before defenses, compared with 1,000 at normal strength. This illustrates one sequence, not a guaranteed bonus for every cooldown arrangement.

For a separate Gathered Thread example at mastery 10 with no upgrades, begin after First Thread has been used, with Magical as the previous type and no Tension:

| Cast     | Result                    | Damage strength | Tension afterward |
| -------- | ------------------------- | --------------: | ----------------: |
| Magical  | Same-type repeat          |            100% |                 1 |
| Magical  | Same-type repeat          |            100% |                 2 |
| Physical | Weave and spend 2 Tension |            150% |                 0 |
| Magical  | Weave without Tension     |            120% |                 0 |

Finally, a mastery-10 Warding Weave with Mending Thread, 4,000 Max Health and 1,000 current Health qualifies for recovery. If its normal 200-damage hit lands, it deals **240 before defenses**, requests **80 healing**, then requests **120 Barrier**, before the usual healing and Barrier rules. Several hits in that cast still grant only that one heal and one Barrier contribution.

## Battle continuity and player information

The remembered type, Tension, previous Weave result, previous qualifying cast time and unused First Thread belong to the current battle. Target changes and enemy deaths do not clear them. Your own death clears the pattern, Tension and cast history; revival does not restore a used First Thread. An unused First Thread remains available until a qualifying attempt uses it.

A new battle begins with no remembered type or Tension and grants First Thread at mastery 7 or higher. A new wave within the same continuing battle preserves the pattern and does not grant the Opening Technique again. Gaps between waves can still cause Quick Hands to miss its time window.

Reconnects and display refreshes preserve the active battle's state. Committed replays and offline combat must use the same cast order and combat timestamps. Saved style and loadout changes apply at the normal encounter boundary.

The Combat Styles page should show each equipped active as **Physical**, **Magical**, **Mixed**, or **Neither**, and explain that sustained weaving needs at least one Physical and one Magical active. A loadout lacking either can still be saved, but the page should make its limited benefit clear. First Thread alone does not sustain a one-type build.

During battle, a compact **Magical prepared** or **Physical prepared** indicator explains the next benefit. Gathered Thread can also show **Tension: 1/2**. The style preview should show current Weave strength, relevant upgrade conditions and any separate healing or Barrier grant without implying that damage percentages multiply those grants.

## Build directions and playtests

- **Steady alternation:** Base form with Unbroken Pattern and Quick Hands rewards complementary Physical and Magical cooldowns.
- **Uneven rhythms:** Gathered Thread lets several casts of one type prepare a larger attack of the other. Compare the payoff with damage lost on unboosted repeats.
- **Armored opponents:** Fraying Weave trades raw damage for help against Armor and Resistance. Test low and high defenses and builds that already have substantial penetration.
- **Sustained protection:** Warding Weave with Mending Thread supports attacking through a longer fight, provided enough Weaves actually land.

Compare equal mastery levels and upgrade counts over complete fights. Test balanced and uneven cooldowns, different hit sizes, area attacks, very short encounters and one-type loadouts. Measure the actual share of casts that Weave rather than assuming perfect alternation from the number of equipped Essences.

Check mixed abilities, conditional direct effects, damage-type conversions, evolutions, no-target casts, misses, Barrier-only damage, lethal hits, simultaneous casts and automatic repeats. Resolve any supported loadout damage-type conversion before classifying the saved battle ability, and make the player preview agree with that result. An ability whose own direct effects contain both types remains Mixed even when only one conditional branch lands.

Verify exact 4- and 8-second timing boundaries, the 35% Health threshold, Tension's cap, owner death, revival, wave transitions and offline replay. Confirm that First Thread cannot be reused and that extra targets or hits do not multiply healing, Barrier or Tension. Playtest whether rapid low-damage casts generate too much recovery compared with their offensive cost before committing the grant values.

## Design references

This expands the [original Spellweaver concept](../future-style-ideas.md#spellweaver). The [damage types](../../combat-lexicon/damage-types.md), [damage categories](../../combat-lexicon/damage-categories.md), [Barrier rules](../../combat-lexicon/conditions/barrier.md) and [Conduit guide](conduit.md) provide the existing vocabulary and direct-effect boundaries.

Spellweaving, Weave, Tension, the four-form design, upgrades and First Thread are proposed additions. This document does not add Spellweaver to the content catalog or implement its battle behavior.
