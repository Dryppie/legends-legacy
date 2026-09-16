# Measured World Tower teams: practical handoff

**Current closeout — 16 September 2026:** keep both admitted anchors, **040e (AA)** and **49f6 (BB)**, as practical reference teams. No stronger replacement or search method was validated. The latest saved-report diagnostic does not justify another candidate experiment; the subgroup, archive-model, block-search and Web Weaver proposals remain closed. See the [final assessment](Tower-Team-Search-Design-Review.md), [loss diagnostic](Tower-Anchor-Loss-Diagnostic-Review.md) and [latest diagnostic receipt](../TestResults/balance/tower-anchor-loss-diagnostic-20260916/completion.json). **No experiment is queued.** All **483,988 exclusions** remain, including V19's 512 unused confirmation values; adoption **Hold**, V19 **Unresolved** with 253 required recipes.

**Three exact fixed-cohort recipes are available.** Retain the selected challenger as a measured alternative with unproven superiority. The two evaluation panels below are separate experiments; their scores are not pooled or treated as a before/after improvement. The complete compositions and equipment remain unchanged.

## Latest subgroup comparison: separate 256-seed panel

| Reference team | Native label | Wins | Win rate | Adjusted rate interval |
| --- | --- | ---: | ---: | --- |
| Anchor 040e | AA | 179/256 | 69.92% | 61.61–77.10% |
| Anchor 49f6 | BB | 172/256 | 67.19% | 58.78–74.62% |

AA and BB retain the respective complete anchor parties, including slot placement, equipment and fixed canonical Essence order. They used the same 256 seeds within this panel. Both won on 128 seeds, only AA won on 51, only BB won on 44, and both lost on 33. The higher AA point estimate does not establish that it is stronger. Neither cross-combined candidate, AB or BA, cleared the frozen improvement gate; that comparison is closed.

The selected challenger was not evaluated on this panel. Its earlier 167/256 result cannot be compared directly with these later anchor scores to choose a stronger team. The displayed intervals describe rates under the subgroup comparison's correction; they are not an AA-minus-BB superiority test. See the [comparison execution review](Tower-Subgroup-Comparison-Execution-Review.md), [exact comparison arithmetic](../TestResults/balance/tower-subgroup-comparison-20260916/comparison.json) and [paired loss diagnostic](Tower-Anchor-Loss-Diagnostic-Review.md).

## Earlier practical pilot: its own 256-seed confirmation panel

The closed pilot confirmed 167/256 wins for the challenger and 161/256 for each anchor. Its 2.34-percentage-point advantage did not meet the fixed improvement criterion. These historical results remain unchanged.

| Recipe | Wins | Win rate | Adjusted interval | Evidence |
| --- | ---: | ---: | ---: | --- |
| Selected challenger | 167/256 | 65.23% | 56.91–72.72% | [Exact scenario](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-50ab9e8905d1d211df904c1100df16fa4680f96022a62ad1879234a74e0d19ec.json) |
| Anchor 040e | 161/256 | 62.89% | 54.52–70.55% | [Exact scenario](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-cc1f4a8c1bea7fde79d5cc317c2da802858b5260619d0069ea2684fe87200730.json) |
| Anchor 49f6 | 161/256 | 62.89% | 54.52–70.55% | [Exact scenario](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-f7315130ac1ae89b8d5ec0839f9cd35b2e6d2b7508ed234bf02730e74a361f30.json) |

The paired challenger-minus-anchor intervals are −12.07 to +16.62 points against 040e and −11.06 to +15.62 against 49f6. Neither superiority nor equivalence was established. Equal anchor totals do not imply interchangeable outcomes. These rates refer to the same saved 256-value confirmation panel; older pilot results are not pooled. See the [execution review](Tower-Incumbent-Practical-Pilot-Execution-Review.md) for the decision and [independent audit](../TestResults/balance/tower-incumbent-practical-pilot-20260916/independent-audit.json) for exact counts.

## What changed in the challenger

**The challenger differs from anchor 040e by only three Essence replacements across characters 1 and 8. Forty-seven of the 50 character–Essence assignments are unchanged.** This is the closest admitted anchor by replacement count. The candidate has supplied-anchor ancestry; its nominal 65.23% result is not evidence that search discovered a strong team independently from scratch.

| Starting recipe | Character | Remove | Add |
| --- | ---: | --- | --- |
| Anchor 040e | 1 | Venomous Spiderling | Blood Harpy |
| Anchor 040e | 8 | Bark Golem; Pack Howler | Poisonous Rat; Spider |
| Anchor 49f6 | 1 | Venomous Spiderling | Blood Harpy |
| Anchor 49f6 | 2 | Enchanted Fairy | Illusion Fox |
| Anchor 49f6 | 5 | Illusion Fox | Enchanted Fairy |
| Anchor 49f6 | 6 | Bark Golem; Spider Queen (royal_venom) | Hobgoblin (brutal_charge); Web Weaver Spider |
| Anchor 49f6 | 8 | Elder Treant; Flame Harpy; Pack Howler | Poisonous Rat; Spider; Spider Queen (royal_venom) |
| Anchor 49f6 | 9 | Elder Treant (thornstorm) | Bark Golem |

All equipment, character templates and neutral identity vectors match across the three recipes. The final recombination proposal’s local parent difference is separate from the complete recipe difference above. No ablation was performed, so the data cannot attribute strength to Blood Harpy, the character-8 replacements or their interaction. Do not rank individual Essences by these whole-party results.

## Exact compositions

Character numbers are fixed expedition slots. Slots 1–5 form subgroup 1; slots 6–10 form subgroup 2, following [WorldTowerPartyRules](../LL/src/Core/Domain/Models/WorldTower/WorldTowerPartyRules.cs) and the [battle adapter](../LL/tools/BalanceHarness/TowerBattleRunner.cs). Keep slot placement and the listed canonical order fixed. Display names below come from the captured Essence catalogue, with ID suffixes distinguishing variants such as Elder Treant (thornstorm); the glossary gives exact IDs.

### Selected challenger

Identity: `a0f9ffe3bcccabf28f1d7b3c2290075e94efb06b59df88a87af74a2be5134462`.

| Character | Subgroup | Essence 1 | Essence 2 | Essence 3 | Essence 4 | Essence 5 |
| ---: | ---: | --- | --- | --- | --- | --- |
| 1 | 1 | Bark Golem | Blood Harpy | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) |
| 2 | 1 | Elder Treant | Flame Harpy | Illusion Fox | Pack Howler | Spider Queen (royal_venom) |
| 3 | 1 | Enchanted Fairy | Pack Howler | Poisonous Rat | Spider Queen (royal_venom) | Venomous Spiderling |
| 4 | 1 | Elder Treant (thornstorm) | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 5 | 1 | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling | Wood Nymph |
| 6 | 2 | Enchanted Fairy | Hobgoblin (brutal_charge) | Pack Howler | Venomous Spiderling | Web Weaver Spider |
| 7 | 2 | Elder Treant (thornstorm) | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 8 | 2 | Enchanted Fairy | Poisonous Rat | Spider | Spider Queen (royal_venom) | Venomous Spiderling |
| 9 | 2 | Bark Golem | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 10 | 2 | Cinder Beetle | Enchanted Fairy | Pack Howler | Ravenous Ghoul | Venomous Spiderling |

### Anchor 040e

Identity: `team-040e60d3dbc5c127321653c47ed3a9d3`.

| Character | Subgroup | Essence 1 | Essence 2 | Essence 3 | Essence 4 | Essence 5 |
| ---: | ---: | --- | --- | --- | --- | --- |
| 1 | 1 | Bark Golem | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 2 | 1 | Elder Treant | Flame Harpy | Illusion Fox | Pack Howler | Spider Queen (royal_venom) |
| 3 | 1 | Enchanted Fairy | Pack Howler | Poisonous Rat | Spider Queen (royal_venom) | Venomous Spiderling |
| 4 | 1 | Elder Treant (thornstorm) | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 5 | 1 | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling | Wood Nymph |
| 6 | 2 | Enchanted Fairy | Hobgoblin (brutal_charge) | Pack Howler | Venomous Spiderling | Web Weaver Spider |
| 7 | 2 | Elder Treant (thornstorm) | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 8 | 2 | Bark Golem | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 9 | 2 | Bark Golem | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 10 | 2 | Cinder Beetle | Enchanted Fairy | Pack Howler | Ravenous Ghoul | Venomous Spiderling |

### Anchor 49f6

Identity: `team-49f6979895354870c89362d4abf214bb`.

| Character | Subgroup | Essence 1 | Essence 2 | Essence 3 | Essence 4 | Essence 5 |
| ---: | ---: | --- | --- | --- | --- | --- |
| 1 | 1 | Bark Golem | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 2 | 1 | Elder Treant | Enchanted Fairy | Flame Harpy | Pack Howler | Spider Queen (royal_venom) |
| 3 | 1 | Enchanted Fairy | Pack Howler | Poisonous Rat | Spider Queen (royal_venom) | Venomous Spiderling |
| 4 | 1 | Elder Treant (thornstorm) | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 5 | 1 | Illusion Fox | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling | Wood Nymph |
| 6 | 2 | Bark Golem | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 7 | 2 | Elder Treant (thornstorm) | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 8 | 2 | Elder Treant | Enchanted Fairy | Flame Harpy | Pack Howler | Venomous Spiderling |
| 9 | 2 | Elder Treant (thornstorm) | Enchanted Fairy | Pack Howler | Spider Queen (royal_venom) | Venomous Spiderling |
| 10 | 2 | Cinder Beetle | Enchanted Fairy | Pack Howler | Ravenous Ghoul | Venomous Spiderling |

## Equipped copies and canonical IDs

Counts describe the copies simultaneously equipped across the ten characters. The fixed cohort assumes those Essences are available and uses `OwnedCopies=null`, so account inventory is not a prerequisite for constructing or comparing these teams. This leaves ownership unconstrained while retaining the per-character legality rules. Each composition contains 50 equipped copies; repeated copies across different characters are intentional.

| Essence | Canonical ID | Challenger | Anchor 040e | Anchor 49f6 |
| --- | --- | ---: | ---: | ---: |
| Bark Golem | `essence.bark_golem` | 2 | 3 | 2 |
| Blood Harpy | `essence.blood_harpy` | 1 | 0 | 0 |
| Cinder Beetle | `essence.cinder_beetle` | 1 | 1 | 1 |
| Elder Treant | `essence.elder_treant` | 1 | 1 | 2 |
| Elder Treant (thornstorm) | `essence.elder_treant_thornstorm` | 2 | 2 | 3 |
| Enchanted Fairy | `essence.enchanted_fairy` | 9 | 9 | 9 |
| Flame Harpy | `essence.flame_harpy` | 1 | 1 | 2 |
| Hobgoblin (brutal_charge) | `essence.hobgoblin_brutal_charge` | 1 | 1 | 0 |
| Illusion Fox | `essence.illusion_fox` | 1 | 1 | 1 |
| Pack Howler | `essence.pack_howler` | 9 | 10 | 10 |
| Poisonous Rat | `essence.poisonous_rat` | 2 | 1 | 1 |
| Ravenous Ghoul | `essence.ravenous_ghoul` | 1 | 1 | 1 |
| Spider | `essence.spider` | 1 | 0 | 0 |
| Spider Queen (royal_venom) | `essence.spider_queen_royal_venom` | 8 | 8 | 8 |
| Venomous Spiderling | `essence.venomous_spiderling` | 8 | 9 | 9 |
| Web Weaver Spider | `essence.web_weaver_spider` | 1 | 1 | 0 |
| Wood Nymph | `essence.wood_nymph` | 1 | 1 | 1 |

## Fixed cohort and equipment

These recipes were measured against floor-5 Kharad with ten level-40 characters, five level-1 unascended/unevolved Essences each, tier 1 /rank 2, Standard quality, attribute-roll multiplier 1, neutral identity vectors, no styles and no contributed damage. The preparation state was `uncleared-no-contributions`. Real character identities, progression, gear or content changes can change results; the rates are specific to this harness cohort.

Level 40 and five available Essences per character are valid declared search conditions. The recipes were admitted under the captured gameplay rules; no actual roster or ownership evidence is needed for this fixed-cohort objective. Ability ordering remains fixed and outcome-independent, outside the search space.

The JSON [team handoff](../TestResults/balance/tower-practical-team-handoff-20260916/teams.json) preserves every exact party/build field, content/settings/execution identity and source export hash. It is a documentation bundle, with no allocation or combat schedule. Original scenario exports remain historical evidence; their saved values are not fresh evaluation samples.

| Equipment profile | Character slots | Exact slot → item assignments |
| --- | --- | --- |
| 1 | 1, 6 | MainHand → `plain.maul.rarity.uncommon`; Chest → `plain.heavy_breastplate.rarity.uncommon`; Head → `plain.heavy_helm.rarity.uncommon`; Legs → `plain.heavy_legplates.rarity.uncommon`; Ring → `plain.band.rarity.uncommon`; Necklace → `plain.amulet.rarity.uncommon`; Relic → `plain.vial.rarity.uncommon` |
| 2 | 2, 7 | MainHand → `plain.staff.rarity.uncommon`; Chest → `plain.light_vest.rarity.uncommon`; Head → `plain.light_hood.rarity.uncommon`; Legs → `plain.light_leggings.rarity.uncommon`; Ring → `plain.band.rarity.uncommon`; Necklace → `plain.amulet.rarity.uncommon`; Relic → `plain.vial.rarity.uncommon` |
| 3 | 3, 4, 8, 9 | MainHand → `plain.gauntlets.rarity.uncommon`; Chest → `plain.light_vest.rarity.uncommon`; Head → `plain.light_hood.rarity.uncommon`; Legs → `plain.light_leggings.rarity.uncommon`; Ring → `plain.band.rarity.uncommon`; Necklace → `plain.amulet.rarity.uncommon`; Relic → `plain.vial.rarity.uncommon` |
| 4 | 5, 10 | MainHand → `plain.greatsword.rarity.uncommon`; Chest → `plain.medium_mail.rarity.uncommon`; Head → `plain.medium_helm.rarity.uncommon`; Legs → `plain.medium_greaves.rarity.uncommon`; Ring → `plain.band.rarity.uncommon`; Necklace → `plain.amulet.rarity.uncommon`; Relic → `plain.vial.rarity.uncommon` |

## Handoff decision and verification

Use both anchors as the measured reference choices for this cohort. Keep anchor 040e as the direct baseline when discussing the challenger’s changes; neither the earlier challenger advantage nor the later AA advantage establishes a stronger default recipe. Preserve each panel's results and original decision rules. No next search variant, extra confirmation samples or adoption change follows from this handoff. Adoption remains **Hold** and V19 reliability **Unresolved**.

This documentation closeout updates this handoff and the current assessment in `Tower-Team-Search-Design-Review.md`. Verification checks the AA/BB mapping against the full saved anchor parties, displayed counts/rates against retained results, relative links, unchanged composition tables and scoped Markdown whitespace. The original JSON handoff, scenario exports, sealed experiment packages and resource receipts remain historical evidence and are unchanged. No build, backend test, new seed, fight, replay, native preparation or native reconstruction is part of this update. There are no gameplay changes, migrations, configuration changes or deployment implications.

The [original handoff receipt](../TestResults/balance/tower-practical-team-handoff-20260916/completion.json) records that earlier documentation checkpoint, when 483,640 reservations existed. The [latest diagnostic receipt](../TestResults/balance/tower-anchor-loss-diagnostic-20260916/completion.json) carries the subsequent accounting and **483,988 exclusions**. Earlier receipts and their remaining balances are historical, not additional allocations. This closeout creates no experiment allocation or cap increase.
