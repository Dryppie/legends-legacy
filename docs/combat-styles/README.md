# Combat Styles

Combat Styles define how a character's equipment and Essences work together. Equipment supplies attributes, Essences supply abilities, and the equipped Combat Style adds a defining combat interaction.

This folder describes the two implemented styles and three developed design proposals. Current implemented rules and values were checked against the repository on 10 September 2026. Proposed mechanics and numerical values are starting points for playtesting.

Shared documentation lives in this folder. Individual Combat Style guides live in `styles/`.

## Documentation

| Document | Contents |
| --- | --- |
| [Game design](game-design.md) | The feature's original design and rationale. Use this overview and the individual guides for the latest progression values and style descriptions. |
| [Implementation plan](implementation-plan.md) | Planned implementation stages and delivery requirements. |
| [Implementation status](implementation-status.md) | Recorded implementation scope and verification results. |
| [Initial verification](verification.md) | Historical engine checks and early balance evidence. |
| [Possible Combat Styles](future-style-ideas.md) | Six brief ideas for additional styles. |

## Style directory

| Combat Style | Status | Identity |
| --- | --- | --- |
| [Bastion](styles/bastion.md) | Implemented | Turn all healing received into Health and Barrier; refine it for recovery, Reprisal damage, or ally protection. |
| [Conduit](styles/conduit.md) | Implemented | The first Essence in your battle loadout is your Channeled Essence; your other Essences build Charge to strengthen its casts. |
| [Reaper](styles/reaper.md) | Design proposal | Advance damage from your own lingering conditions through direct Essence attacks. |
| [Shepherd](styles/shepherd.md) | Design proposal | Build mutual protection around a bond with one chosen summon. |
| [Gambler](styles/gambler.md) | Design proposal | Draw bounded hands of Steady and Lucky outcomes for your Essence actives. |

Every style file covers its core mechanic, mastery level bonuses, refinements, upgrades, Opening Technique, and Upgrade Mastery. Reaper, Shepherd, and Gambler now have complete proposed rules and worked examples; they are not implemented or available in the game. Their proposals use the same level-0 start, level-10 cap, milestone schedule, individual XP structure, and initial XP requirements described below.

## Equipping and configuring a style

- Equip one global Combat Style for all battles, or leave the slot empty.
- Bastion and Conduit are available at level 0 with their full core mechanics. Each style has its own level and XP.
- Retain the base form or choose one refinement once that style reaches level 3.
- Choose the bonuses that suit your Combat Style. Unlock your first slot at Mastery 5 and a second at Mastery 8. All three upgrade choices become available with the first slot; equip up to two different upgrades, and either slot may stay empty.
- At level 7, the style's Opening Technique activates automatically once at the start of each battle, with any refinement or the base form.
- At Mastery 9, choose one of your equipped upgrades to gain its additional mastery effect. The choice is optional and occupies its existing slot. Removing that upgrade clears its mastery selection.
- Save applies the configuration. Discard restores the saved configuration. Each style remembers its refinement, upgrades, and Upgrade Mastery choice.
- Conduit automatically channels the first occupied slot in the Essence loadout used for each battle. Arrange that loadout on the Essence page; **Channel Essence** swaps an eligible equipped Essence into the first occupied slot in one save. A **Channeled** badge identifies it while Conduit is equipped. **Creature Focus** is the separate creature-selection feature in the Creatures tab.

Battle configurations use the saved style. Ordinary idle combat picks up changes at the next encounter; committed battles, dungeon runs, and matches retain their captured configuration until their normal boundary.

Bastion's **Reprisal** refinement stores 25% of enemy damage absorbed by your Barrier, capped at 10% of Max Health, for the next normally cast damaging Essence's first direct enemy attack attempt. It works with your Barrier from any source and does not spend Barrier. Reprisal replaces Counterweight in current choices; saved Counterweight selections resolve to Reprisal for new battles, while committed historical battles retain their captured rules.

## Individual progression

Only the Combat Style used for a rewarded encounter earns its eligible base combat XP, at one Combat Style XP per point. Progress belongs to that style even if another is selected before rewards are collected. Switching preserves each style's earned progress. Level 10 is the cap; XP beyond the cap is not banked.

| Mastery level | Automatic level bonus | Additional unlock | Upgrade slots |
| ---: | --- | --- | ---: |
| 0 | Base mechanic; no level bonus | Full core and base form | 0 |
| 1 | +1% of Bastion's base converted Barrier / +1% flat increase to Conduit's charged Channeled Essence | — | 0 |
| 2 | +2% / +2% flat increase | — | 0 |
| 3 | +3% / +3% flat increase | Refinement selection | 0 |
| 4 | +4% / +4% flat increase | — | 0 |
| 5 | +5% / +5% flat increase | Upgrade slot 1 | 1 |
| 6 | +6% / +6% flat increase | — | 1 |
| 7 | +7% / +7% flat increase | Opening Technique | 1 |
| 8 | +8% / +8% flat increase | Upgrade slot 2 | 2 |
| 9 | +9% / +9% flat increase | Upgrade Mastery | 2 |
| 10 | +10% / +10% flat increase | Maximum mastery level | 2 |

Every mastery level from 1 through 10 automatically improves the core mechanic. Bastion adds 1% of its base converted Barrier per level; Conduit adds a flat +1% to charged Channeled Essence effects per level. Flat bonuses add directly to Channeled Essence strength: `100% + 1% flat = 101%`. Level 0 supplies the full base mechanic, and zero-Charge Conduit output stays unchanged. There is no separate Core Rank system or rank track. Refinements, upgrades and Upgrade Mastery remain player choices.

Reaper, Shepherd and Gambler proposals use the same continuous progression: Reaper adds 1 point to Harvest per level; Shepherd adds 2 points of Bond protection; Gambler adds 1 point to the total Lucky payout per hand, split into 0.5 points per Lucky card for Safe Bet. Their level-10 totals are unchanged.

### Current XP requirements

These are the catalog's requirements for each individual advancement, not cumulative totals. Both implemented styles use this schedule independently.

| Advancement | XP required |
| --- | ---: |
| 0 → 1 | 36,900 |
| 1 → 2 | 232,225 |
| 2 → 3 | 613,475 |
| 3 → 4 | 1,180,825 |
| 4 → 5 | 1,934,100 |
| 5 → 6 | 2,873,400 |
| 6 → 7 | 3,998,725 |
| 7 → 8 | 5,309,975 |
| 8 → 9 | 6,807,325 |
| 9 → 10 | 8,490,600 |

## Documentation sources

The implemented guides follow the [current content catalog](../../LL/src/API/API.LL/Data/combat-styles/combat-styles.v1.json), [progression rules](../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleProgression.cs), [selection rules](../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs), and [combat implementation](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.CombatStyles.cs). The catalog currently declares content version `combat-styles.v6` despite its filename. Version 6 introduces the Channeled Essence terminology without changing tuning or the first-slot rule introduced in version 5.

The [game design](game-design.md) retains the original concepts and design rationale, updated for the level-0 starting point and bonuses at every mastery level. Historical verification records identify earlier rank-based behavior explicitly. Update these guides alongside future changes to the catalog or combat rules.
