# Gambler

**Status:** Design proposal; not implemented. All values below are initial playtest values.

**Core mechanic:** Fortune. **Identity:** Build around occasional exceptional casts within a bounded, readable pattern of chance.

Gambler changes the output of ordinary Essence actives through automatic card draws. It offers frequent small payouts, larger spikes, or predictable peaks. The player chooses the build before combat; Fortune needs no manual timing or additional active ability.

Use the [shared progression rules](../README.md) for individual XP, global selection, Save/Discard, and milestones. This proposal starts at level 0 with the full mechanic and uses the existing XP schedule.

## Core mechanic: Fortune

The base form uses repeating **five-card hands** containing **four Steady cards and one Lucky card**. Each hand is shuffled automatically. Every eligible Essence active cast draws and consumes one card:

| Card | Effect at mastery level 0 |
| --- | ---: |
| Steady | 90% of the cast's eligible immediate amounts |
| Lucky | 160% of the cast's eligible immediate amounts |

After all five cards have been drawn, the next hand begins. Each completed hand has exactly one Lucky outcome. Across two consecutive shuffled hands, there can be at most **eight Steady draws between Lucky draws**. This bound counts eligible casts, not seconds; Essence cooldowns still determine when those casts occur.

For an eligible effect normally worth 200, Steady produces **180** and Lucky produces **320**, before ordinary combat resolution. Across five equally sized effects, the level-0 hand averages **104%** output: (4 × 90 + 160) / 5.

This average explains the starting tuning. Real performance depends on which effects draw Lucky, their targets, missing Health, Barrier capacity, and battle duration.

### Which casts draw cards

- An eligible cast is an ordinary active cast from an equipped Essence with at least one immediate direct damage, healing, Health-restoration, or Barrier component.
- Draw once when the cast actually begins, after ordinary activation requirements are met. An action prevented or cancelled before activation draws nothing. All eligible immediate components use the same card, including multiple hits and targets. A miss or recovery rejected by full Health or Barrier still consumes the card.
- Basic attacks, passive triggers, reflected or secondary damage, periodic ticks, summons' actions, and automatic copies of an already resolved cast do not draw another card.
- A mixed Essence can draw for its direct component while its periodic conditions, summons, duration, and utility effects retain their ordinary rules. An active with only those other effects draws nothing.
- Recovery derived from already resolved damage is calculated from that damage once; Fortune does not multiply it again.

Fortune changes effect amounts, not attack chance, critical chance, cooldowns, or target selection. Ordinary mitigation, critical rules, healing modifiers, and Barrier caps continue to apply. The card modifies the original cast's amounts, rather than creating a separate damage or healing event that can trigger another cast.

## Mastery level bonuses

Every mastery level adds **1 percentage point to the total Lucky payout across one hand**. Base form, Counted Cards and High Stakes have one Lucky card, which receives +1 point per level. Safe Bet has two Lucky cards and gives each **+0.5 points per level**.

| Mastery level | Base / Counted Cards Lucky | Safe Bet Lucky, each | High Stakes Lucky |
| ---: | ---: | ---: | ---: |
| 0 | 160% | 115% | 180% |
| 1 | 161% | 115.5% | 181% |
| 2 | 162% | 116% | 182% |
| 3 | 163% | 116.5% | 183% |
| 4 | 164% | 117% | 184% |
| 5 | 165% | 117.5% | 185% |
| 6 | 166% | 118% | 186% |
| 7 | 167% | 118.5% | 187% |
| 8 | 168% | 119% | 188% |
| 9 | 169% | 119.5% | 189% |
| 10 | 170% | 120% | 190% |

Mastery levels leave Steady amounts unchanged. At level 10 in the base form, a 200-point effect resolves at **180 on Steady or 340 on Lucky**. The equal-effect hand average rises from 104% at level 0 to **106%** at level 10. Safe Bet retains its 120% Lucky value at level 10. Bonuses add directly; they do not compound.

## Refinements — level 3

Choose one refinement or keep the base form. Every hand contains five cards, and only its stated composition and draw pattern change.

| Form | Hand | Draw pattern | Equal-effect average, mastery level 0 → 10 |
| --- | --- | --- | ---: |
| **Base form** | Four Steady at 90%; one Lucky at 160% | Shuffled | 104% → 106% |
| **Safe Bet** | Three Steady at 95%; two Lucky at 115% | Shuffled | 103% → 105% |
| **High Stakes** | Four Steady at 85%; one Lucky at 180% | Shuffled | 104% → 106% |
| **Counted Cards** | Four Steady at 90%; one Lucky at 160% | Four Steady followed by one Lucky | 104% → 106% |

**Safe Bet** gives more frequent Lucky casts with a smaller difference between outcomes. It allows at most six consecutive Steady draws across hand boundaries. A level-10 200-point effect produces **190 on Steady or 240 on Lucky**.

**High Stakes** concentrates more of the hand's output into its single Lucky cast. It retains the base form's eight-Steady bound. At mastery level 10, a 200-point effect produces **170 on Steady or 380 on Lucky**.

**Counted Cards** replaces shuffling with a fixed repeating pattern. Every fifth draw is Lucky; there are four Steady draws between Lucky draws. The pattern controls Fortune outcomes while Essences continue using their normal automatic activation rules. It does not designate a Focus Essence.

## Upgrades — slots at levels 5 and 8

All three upgrades become available at level 5. Equip up to two different upgrades as the slots open.

| Upgrade | Ordinary effect |
| --- | --- |
| **Rising Fortune** | A Lucky cast following at least **two consecutive Steady draws** gains **+10 percentage points** to its eligible immediate effects. |
| **Steady Nerves** | A Steady cast beginning at or below **35% Health** gains **+10 percentage points** to immediate self-healing and self-Barrier only. |
| **Follow Through** | After a Lucky cast, the next Steady cast gains **+10 percentage points** to its eligible immediate effects. |

Check Health and the preceding card sequence when the cast begins. Steady Nerves applies only to eligible components targeting the caster; Rising Fortune and Follow Through apply to all eligible components.

Follow Through grants one pending bonus. Another Lucky refreshes that pending bonus rather than adding another. Basic attacks and other actions that draw no card leave it unchanged. These upgrade counters start empty each battle; effects are not banked between encounters.

All percentage-point bonuses add to the card's value. For example, a level-10 base-form Lucky cast satisfying Rising Fortune uses **180%**: 160 + 10 from mastery levels + 10 from the upgrade. A Steady self-heal satisfying both Steady Nerves and Follow Through uses **110%**: 90 + 10 + 10.

## Opening Technique — level 7

**Opening Stake:** The first eligible Essence cast of each battle gains **+10 percentage points** to its eligible immediate effects, whichever card it draws.

The technique applies once per battle, in every form. It consumes the normal card and neither replaces that card nor changes the hand. A new wave within the same continuing battle does not grant another Opening Stake.

At mastery level 10 in the base form, the first cast uses **100% on Steady or 180% on Lucky**, before applicable upgrade bonuses. It can combine with Steady Nerves on a qualifying Steady self-recovery cast. Rising Fortune and Follow Through begin with empty counters, so they do not activate on that first cast.

## Upgrade Mastery — level 9

Empower one equipped upgrade; it keeps its existing slot and ordinary effect.

| Empowered upgrade | Enhancement |
| --- | --- |
| **Rising Fortune** | Its Lucky bonus requires **one preceding Steady draw** instead of two. |
| **Steady Nerves** | Its Steady self-healing and self-Barrier bonus applies at **any Health percentage**. |
| **Follow Through** | A Lucky cast prepares the bonus for the next **two Steady casts**. Each consumes one; a new Lucky refreshes the remaining count to two, without stacking above two. |

Neither mastery levels, upgrades, nor mastery create extra draws or change the number of Lucky cards in a hand.

## Worked hand

Consider a level-10 base-form Gambler with Rising Fortune and empowered Follow Through. Each cast below has one eligible damage effect normally worth 200. The sequence occurs after Opening Stake has already been used, and any prior Follow Through bonuses have been consumed. Its first two Steady draws ensure Rising Fortune applies to the Lucky draw.

| Draw | Card | Relevant benefit | Final percentage | Effect amount |
| ---: | --- | --- | ---: | ---: |
| 1 | Steady | Build the Steady streak | 90% | 180 |
| 2 | Steady | Build the Steady streak | 90% | 180 |
| 3 | Lucky | Rising Fortune; prepare two Follow Through bonuses | 180% | 360 |
| 4 | Steady | Consume one Follow Through bonus | 100% | 200 |
| 5 | Steady | Consume the second Follow Through bonus | 100% | 200 |

The five effects total **1,120** before ordinary mitigation. This is one valid hand and its particular upgrade interactions, not a guaranteed average for every build or partial hand.

## Encounter continuity and player information

Fortune's draw history is part of the character's persistent style state. A new encounter, page refresh, reconnect, defeat, or leaving and returning to Gambler does not reshuffle an unfinished hand. Each form retains its own hand position when switching refinements. The upcoming shuffled card is not previewed.

Once a real cast has consumed a card, abandoning or retrying the encounter does not refund that draw. Replaying the same committed battle reproduces its original outcomes. Online and offline combat follow the same sequence. Switching forms is a saved build choice at the normal encounter boundary and cannot change an active battle's captured hand rules.

Only the draw sequence persists; Health, Barrier, Opening Stake, and pending upgrade benefits follow ordinary encounter boundaries. The first use of Counted Cards begins at its first Steady card; later battles resume its position.

The Combat Styles page should show the selected form's card composition, Steady and Lucky percentages at the current mastery level, and upgrade effects. Its Fortune details can also show cards remaining, Lucky cards remaining in the current hand, and the most recent result. Counted Cards can show draws until Lucky because its pattern is fixed.

## Build directions and playtests

- **Frequent payouts:** Safe Bet with Follow Through keeps bonuses spread across more casts.
- **Large peaks:** High Stakes with Rising Fortune emphasizes the Lucky result, using equipment and Essences that sustain the character between peaks.
- **Predictable rhythm:** Counted Cards with Rising Fortune makes progression toward its peak easy to follow during automatic combat.
- **Steady recovery:** Steady Nerves and its mastery make ordinary self-recovery casts useful alongside either an offensive or supportive hand.

Playtest equally sized and differently sized effects, mixed damage/recovery abilities, multi-hit and area effects, very short fights, and longer fights spanning several hands. Compare forms at equal mastery levels and upgrade counts. Check that retrying, reconnecting, switching forms, and offline resolution preserve committed draws, and evaluate whether preparing hand positions in easier fights creates too much advantage in harder encounters.

## Design references

This expands the [original Gambler concept](../game-design.md#5-three-potential-combat-styles). It follows the existing [Conduit guide's effect boundaries](conduit.md#choosing-a-focus) for describing immediate Essence components and the normal [Barrier rules](../../combat-lexicon/conditions/barrier.md). Fortune, its cards, and the proposed rewards are new design, not existing game behavior.
