# Duelist

**Status:** Design proposal — not implemented. All values below are starting points for playtesting.

Stay on one opponent, learn how they fight, and make your next Essence strike count. Your attacks build **Read** against that enemy. At **3 Read**, you find an **Opening** that your next damaging Essence uses for a stronger attack.

Duelist suits builds that keep pressure on the same enemy through basic attacks and direct Essence damage. It works with Physical and Magical attacks and does not require a particular weapon. Everything happens automatically through the existing Combat Style slot.

The core mechanic, **Read the Opponent**, starts at mastery level 0. Selection, saved configurations and XP follow the [shared Combat Style rules](../README.md).

## Read the Opponent

Your first direct attack chooses the opponent you are reading. Each basic attack or normally cast Essence that lands direct damage on that opponent builds **1 Read**, up to **3** in the base form. One action can build Read only once, even when it hits several times.

At 3 Read, your Opening is ready. Your next normally cast Essence that directly attacks that opponent spends the Opening and deals **145% of its normal direct damage to them**. It consumes all Read before the attack resolves. Basic attacks keep dealing their normal damage while you wait for an Essence to use the Opening.

An action that builds the final Read cannot also spend the Opening it just created. An Essence that spends an Opening does not build Read, even if it has several hits. Read and a ready Opening have no time limit while you remain on the same living opponent.

For example, at mastery level 0:

| Action against the same enemy | Result | Read afterward |
| --- | --- | ---: |
| Basic attack lands | Begin reading the opponent | 1 |
| Damaging Essence lands | Keep building Read | 2 |
| Basic attack lands | Find an Opening | 3 — ready |
| Next damaging Essence attacks | Spend the Opening; direct damage to that enemy uses 145% strength | 0 |

If the final Essence would normally deal 200 direct damage before defenses, its Opening makes that **290**. It follows its normal cooldown and targeting; Duelist does not hold a cast for a better opportunity or add manual attack selection.

## Staying on one opponent

Read belongs to you and the opponent you are currently studying. Attacking a different opponent starts a fresh Read, even when that attack misses. Returning to an earlier opponent does not restore the progress you left behind.

Area attacks can still help you learn your current opponent. Hitting other enemies alongside them does not erase your Read, and those other enemies take their normal damage. Duelist does not change an Essence's targets, Threat or Taunt rules.

Use these rules for mixed and multi-hit abilities:

- A basic attack uses its selected enemy as the opponent for that action.
- For a normal Essence cast, its first direct-damage component with living enemy targets chooses the opponent for that cast. Keep your current opponent if they are among those targets; otherwise, choose the first enemy in that component's normal resolution order. Later components do not switch your Read again during the same cast.
- If this changes your opponent, clear the old Read, ready Opening and target-specific upgrade progress before resolving damage. A successful hit can then start the new Read at 1.
- If an Opening is ready for the chosen opponent, spend it before the first direct attack attempt against them. All of that cast's own direct damage to that opponent receives the bonus. Damage to other enemies does not.
- If the cast does not spend an Opening, award its 1 Read after the cast finishes, provided it landed direct damage on the chosen opponent and both of you are still alive. Several hits and mixed damage types still give only 1 Read.
- An ability with no living direct-damage target leaves Read alone. Healing, Barrier and condition-only casts do not interrupt your progress.

Landing damage means removing some Health or Barrier. A fully dodged or fully prevented attack builds no Read. A spent Opening is still spent if its attacks miss or are prevented. Damage absorbed by Barrier counts normally.

Only basic attacks and normal Essence casts build Read. Damage over time, reflection, passive damage, triggered copies, automatic repeats and summons' attacks neither build nor spend it. They also do not change your chosen opponent.

Read and Opening are personal Combat Style state, not conditions placed on the enemy. Ward, Cleanse and Dispel do not remove them. Other Duelists keep their own progress against the same enemy.

## What an Opening strengthens

An Opening multiplies the spending cast's normal direct damage to your chosen opponent. Each hit keeps its original damage type, critical rules and other modifiers. Armor, Resistance, damage reduction and Barrier apply normally. Calculate damage from its original amount rather than multiplying an amount that has already passed through defenses.

The bonus does not strengthen healing, Barrier, damage over time, condition strength or duration, summons, or secondary damage triggered by the attack. Lifesteal uses the final damage under its normal rules and receives no separate Opening multiplier. No extra attack is created.

For every form:

**Opening damage = normal direct damage × (form strength + mastery-level bonus + applicable upgrade bonuses).**

The percentages inside the brackets add together. For example, **145% + 10% flat + 10% flat = 165%**, so a normal 200-damage hit becomes 330 before defenses.

## Mastery level bonuses

Every mastery level adds **a flat +1%** to Opening damage in every form. Ordinary attacks keep their normal strength. Levels do not change how much Read an action builds or how much Read an Opening needs.

| Mastery level | Opening bonus | Base-form Opening strength | Additional unlock |
| ---: | ---: | ---: | --- |
| 0 | +0% flat increase | 145% | Full core mechanic |
| 1 | +1% flat increase | 146% | — |
| 2 | +2% flat increase | 147% | — |
| 3 | +3% flat increase | 148% | Refinement choice |
| 4 | +4% flat increase | 149% | — |
| 5 | +5% flat increase | 150% | First upgrade slot; all three upgrades available |
| 6 | +6% flat increase | 151% | — |
| 7 | +7% flat increase | 152% | Opening Technique |
| 8 | +8% flat increase | 153% | Second upgrade slot |
| 9 | +9% flat increase | 154% | Empower one equipped upgrade |
| 10 | +10% flat increase | 155% | Maximum mastery level |

These values are before upgrades. Duelist uses the same individual XP requirements as the implemented styles, with no separate rank track.

## Refinements — mastery 3

Choose one refinement or keep the base form. Each choice changes what you want from an Opening while keeping the same attacks, target rules and flat mastery-level bonus.

| Form | Read needed | Opening strength before levels and upgrades | What changes |
| --- | ---: | ---: | --- |
| Base form | 3 | 145% | A dependable damage payoff after three successful actions. |
| **Flurry** | 3 | 130% | Keep 1 Read after spending an Opening, bringing the next one closer. |
| **Patient Blade** | 5 | 180% | Study the opponent for longer before committing to a heavier strike. |
| **Guarded Thrust** | 3 | 125% | After using an Opening, take less damage from that opponent's next direct attack. |

### Flurry

Keep the pressure on. After your Essence spends an Opening and finishes casting, regain **1 Read** if you and your opponent are still alive. Your next Opening then needs two more successful actions.

The returned Read is granted even if the spending cast misses. It does not count as a basic attack or successful hit for upgrades. Your first Opening still requires 3 Read, and changing opponents clears the returned Read normally.

At mastery 10, a Flurry Opening uses **140%** strength before upgrades. You trade some damage on each Opening for more frequent follow-ups.

### Patient Blade

Wait until you know exactly where to strike. Store up to **5 Read**, and spend it on an Opening at **180%** strength before levels and upgrades. Partial Read gives no damage bonus.

At mastery 10, a Patient Blade Opening uses **190%** strength before upgrades. This favors longer fights where the same opponent survives long enough for the preparation to pay off. Target changes risk losing more progress.

### Guarded Thrust

Strike without leaving yourself exposed. After the spending Essence finishes, prepare to take **25% less direct damage from your chosen opponent's next basic attack or normal Essence cast against you**, for up to **5 seconds**. That Opening deals less damage: **135%** strength at mastery 10 before upgrades.

The protection covers all direct hits against you from that one incoming action. It is consumed on the first attack attempt, including a dodge or fully prevented hit, and ends when that action finishes. The incoming action must begin attacking you before the five-second window expires; an attempt exactly at expiry is too late. Other enemies' attacks do not consume it.

Apply the reduction once, multiplicatively with normal damage reduction, before Barrier absorbs damage. It does not cover damage over time, reflection, passive damage, triggered copies or summons' attacks. A fresh Guarded Thrust replaces unused protection and refreshes its duration; the reductions do not stack. Changing opponents or either combatant dying clears it.

Protection is granted even if your Opening misses, provided both combatants survive. It begins after your cast and its immediate reactions finish, so it cannot reduce a reaction to the strike that created it. This is personal style state, not a new Guard condition or a Barrier grant.

## Upgrades — mastery 5 and 8

All three upgrades become available at mastery 5. Equip one at mastery 5 and up to two different upgrades at mastery 8. Each works with every form.

| Upgrade | Effect |
| --- | --- |
| **Measured Strikes** | An Opening gains **a flat +10%** damage if you landed at least one basic attack against that opponent while preparing it. |
| **Know Your Enemy** | After you have spent one Opening against an opponent, later Openings against that same opponent gain **a flat +10%** damage until you change opponents. |
| **Finishing Touch** | An Opening gains **a flat +10%** damage if its opponent has **35% Health or less** when you spend it. |

Check the upgrades when the Opening is spent and keep their bonuses for the whole cast. Finishing Touch uses current Health divided by Max Health, excluding Barrier; crossing its threshold during the cast does not change that cast's bonus.

Measured Strikes counts successful basic-attack actions since your previous Opening or since choosing this opponent. Basic attacks at maximum Read also count. Spending an Opening resets that count. Returned or granted Read does not count as a basic attack.

Know Your Enemy begins benefiting the second Opening spent against the same opponent. Spending the first counts even if the cast misses. It does not accumulate another bonus for each later Opening.

## Opening Technique — mastery 7

**First Impression:** Begin each battle prepared to gain **2 extra Read** from your first successful basic attack or normal Essence cast against a living opponent.

Add these 2 Read alongside that action's normal 1 Read, up to the form's cap. The base form, Flurry and Guarded Thrust reach 3 Read immediately; Patient Blade reaches 3 of its required 5. The action itself deals normal damage, and a later Essence spends the Opening.

First Impression waits through misses, healing and other actions that do not build Read. It is used only when the extra Read is awarded and does not activate on an action that kills its opponent. Once used, changing targets does not grant it again. This is an automatic battle-start benefit in every form, not another equipped upgrade.

## Upgrade Mastery — mastery 9

Choose one equipped upgrade to empower. It keeps its existing slot and ordinary effect.

| Empowered upgrade | Additional effect |
| --- | --- |
| **Measured Strikes** | Two or more successful basic attacks while preparing an Opening raise its bonus to **a flat +20%** instead of +10%. One basic attack still grants +10%. |
| **Know Your Enemy** | Its **flat +10%** also applies to your first Opening against each opponent. |
| **Finishing Touch** | Its **flat +10%** applies at **50% Health or less**, instead of 35%. |

Mastery changes that upgrade's rule; it does not add another copy of the ordinary bonus. Removing the empowered upgrade clears its mastery selection under the shared configuration rules.

## Worked examples

Consider a mastery-10 base-form Duelist with empowered Measured Strikes and Know Your Enemy. First Impression has already been used. Each Essence below would normally deal 200 direct damage before defenses. Every listed attack lands on the same surviving opponent until the final row.

| Sequence | Opening bonuses | Opening strength | Essence damage before defenses |
| --- | --- | ---: | ---: |
| Land three basic attacks, then spend the first Opening against this opponent | +10% flat from mastery; +20% flat from Measured Strikes | 175% | 350 |
| Land three more basic attacks, then spend another Opening against them | The same bonuses, plus +10% flat from Know Your Enemy | 185% | 370 |
| Switch to a new opponent and land one basic attack | Previous progress and upgrade history are cleared; start at 1 Read | No Opening yet | — |

For a separate comparison, take each form at mastery 10 with no upgrades. A spending cast has two direct hits, each normally worth 100 against the chosen opponent:

| Form | Each hit with an Opening | Total direct damage before defenses | After the cast |
| --- | ---: | ---: | --- |
| Base form | 155 | 310 | Start again at 0 Read. |
| Flurry | 140 | 280 | Return to 1 Read if the opponent survives. |
| Patient Blade | 190 | 380 | Start again at 0 Read; the next Opening needs 5. |
| Guarded Thrust | 135 | 270 | Start again at 0 Read and prepare protection if the opponent survives. |

Any other enemies hit by that cast still take the two normal 100-damage hits. With Guarded Thrust, an incoming hit that would deal 200 after other defenses instead deals **150** before Barrier, provided it belongs to the protected attack and arrives in time.

## Battle continuity and player information

Read, the chosen opponent, upgrade preparation and unused Guarded Thrust protection belong to the current battle. Opponent death clears them without transferring progress; your own death clears them too. Revival starts a fresh Read and does not restore a used First Impression. A new battle starts from zero and grants First Impression again at mastery 7 or higher. A new wave within the same continuing battle does not grant it again.

A reconnect or display refresh must preserve the active battle's state. Committed battle replays and offline combat must use the same action order, chosen opponents and Opening decisions. Saved style changes take effect at the normal encounter boundary.

The Combat Styles page should show the selected form's Read requirement, current Opening strength, refinements and upgrade effects. During battle, show the chosen opponent and a compact **Read: 2/3** or **Opening ready** indicator. For Guarded Thrust, also show whether its protection is waiting and how long remains. These explain automatic behavior without adding controls.

## Build directions and playtests

- **Sustained pressure:** Base form with Measured Strikes and Know Your Enemy rewards repeatedly attacking a durable opponent.
- **Frequent follow-ups:** Flurry suits builds with enough direct Essence casts to spend its more frequent Openings.
- **A heavier finish:** Patient Blade with Finishing Touch rewards reaching an Opening while the opponent is already wounded. Test whether normal cast order delivers this often enough to feel useful.
- **Trading blows:** Guarded Thrust gives up damage to survive an opponent who regularly attacks back. Compare its five-second window against fast enemies and slower bosses.

Compare the forms over complete fights at equal mastery and upgrade counts, including preparation time and unused Read at the end. Test basic-attack speed, short and long Essence cooldowns, differently sized attacks, and healing-heavy loadouts. No form should consistently win both short fights and sustained encounters.

Target stability needs particular attention: current ordinary targeting can change opponents between attacks. Test against one boss, several enemies with similar Threat, enemy summons, random-target Essences, Taunt and area abilities. Measure how often target changes erase progress. Duelist should reward focused builds without requiring target control the game does not provide.

Verify misses, complete prevention, Barrier-only damage, multi-hit casts, mixed target groups, lethal hits, simultaneous actions, revival and wave transitions. Check exact Read caps, Health thresholds and Guarded Thrust expiry. Confirm that passive effects and repeated casts cannot accelerate Read or spend another Opening, and that an area cast neither multiplies Read gains nor resets progress for every enemy hit.

## Design references

This expands the [original Duelist concept](../future-style-ideas.md#duelist). The [targeting rules](../../combat-lexicon/targeting-rules.md), [damage categories](../../combat-lexicon/damage-categories.md) and [Conduit guide](conduit.md) provide the existing combat vocabulary and direct-effect boundaries.

Read the Opponent, Read, Opening, all Duelist refinements and upgrades, and First Impression are proposed additions. This document does not add Duelist to the content catalog or implement its battle behavior.
