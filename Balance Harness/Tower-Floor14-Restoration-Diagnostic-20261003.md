# Floor 14: limited Restoration diagnostic — 3 October 2026

**Subsequent fixed refinement:** **Power factor 0.60 / penetration 40 is nominated for formal acceptance** (guardian offense **10.933125 → 6.559875**). Next implement and test its strict aggregate/native application contract, then complete **512 fresh screening seeds and independent 512-seed confirmation** if screening passes: maximum **202,752 fights / 1,024 reservations**, currently unallocated. Both phases require **77/512 or more on at least two eligible compositions**, and **214/512 or fewer on every recipe**. Floor 14 remains unchanged. See the [complete refinement](Tower-Floor14-Restoration-Refinement-20261003.md).

The closed pressure refinement found no nominee. At Power factor **0.575** and penetration factor **40**, the strongest control won **10/32**; two full-Restoration compositions reached at least **6/32**, but every equipment-eligible composition fell short. This fixed diagnostic tests exact partial Restoration sets at that same setting. Partial sets retain other attributes and need not perform between baseline and full Restoration. Prior results guide this declaration only; they will not be pooled with the new panel.

Retain all **183 existing recipes** and add **15 exact Restoration variants** across all five existing compositions: six saved items on healer 2, six on healer 7, and four each on healers 2 and 7. The eight-item variant uses MainHand, Chest, Head and Necklace. The complete family contains **198 recipes / five actual compositions / 135 eligible recipes**. Eligibility remains at most eight specialized items on at most two characters. Preserve expected progression, level 60, tier 2, seven Essences, identities and raw actor, Essence and equipment order.

Bind the saved 198-recipe proposal through the original qualified 73-recipe source and the saved 183-recipe armor family. Retain the completed **18,688 input comparisons / 73 historical replays**, without repeating them. Require **176 Python regression checks**, the authenticated unchanged **415 backend-case / zero-skip** runtime proof, and successful native preparation of all 198 recipes before combat allocation. Preparation allocates no fights or seeds.

Test one isolated candidate: unchanged floor-14 guardian offense **×0.575**, penetration **×40**. Retain Health **12.68625**, defenses, regeneration and every ability. Live offense remains **10.933125**, penetration **1**. Complete **32 fresh seeds per recipe: 6,336 fights / 32 reservations**, starting at **931,516 exclusions**. No interim tuning, pooled outcomes, retries, replacement seeds or extensions.

Nominate only if at least **two distinct eligible compositions reach 6/32**, and **every recipe remains at 13/32 or below**. A nomination requires a separately declared full-family acceptance screen and independent confirmation. This diagnostic cannot accept or apply a change.

Use doubled measured time and output estimates from the closed, slower 0.50 diagnostic, with projected output plus a **2 GiB** free-space reserve. Keep **840-second native / 900-second native-owner / 1,800-second outer-owner** limits. Native preparation retains its separate 3,600-second outer limit for accepted-catalog authentication. Observe supervisor stdout only while an owner is active.

After closure, independently recount all 6,336 raw outcomes. Require every retained native participant snapshot to equal the previous 0.575 panel, then verify all 15 added recipes against their exact baseline and Restoration donor. Keep all previous studies, failures and reservations.

Preparation supervisor: `TestResults/tower-floor14-restoration-prepare-20261003.py`. Diagnostic supervisor: `TestResults/tower-floor14-restoration-diagnostic-20261003.py`. No migration, environment configuration change or deployment.

## Closed result

The [limited-Restoration diagnostic](Tower-Floor14-Restoration-Diagnostic-20261003.md) completed **6,336 fresh fights / 32 reservations** across **198 recipes / five actual compositions / 135 eligible recipes**, retaining all 183 controls and adding 15 exact healer-equipment subsets. At offense **0.575 / penetration 40**, **all five** eligible compositions reached 6/32; their best results were **18, 17, 15, 9 and 8 wins /32**, all using four Restoration items on each healer. The strongest result **18/32** exceeds the **13/32** ceiling, so **no setting is nominated**. All outcomes were independently recounted; every retained native loadout matched the prior same-setting panel, and all 15 new equipment subsets passed exact donor comparisons. All 198 recipes prepared natively with zero fights/seeds. **176 Python regression checks, 17 diagnostic boundary checks, twelve penetration guards and both native preparation/study fixtures passed with zero skips**. The unchanged **415-case backend runtime proof** was authenticated and reused; the original **18,688 input matches / 73 replays** remain mandatory and were not repeated. **Live floor 14 remains unchanged and unaccepted** (Health **12.68625**, offense **10.933125**, penetration **1**). Floor-14 diagnostics now total **35,616 fights / 192 reservations**, counted separately without pooled outcomes; final exclusions **931,548**. Next: implement and freeze the proposed **0.60 / 0.625 / 0.65** Power refinement with penetration 40 and all 198 recipes (**19,008 fights / 96 reservations maximum; zero allocated**). This closed diagnostic must not be extended. Floor 13 remains applied; floor 15 and the final current-version 1–15 sweep remain. No owner is active. No migration, environment configuration change or deployment.

| Composition | Baseline | Six on healer 2 | Six on healer 7 | Four each | Full Restoration |
|---|---:|---:|---:|---:|---:|
| a622367ad4bb | 0/32 | 0/32 | 0/32 | 8/32 | 0/32 |
| 604ac1457f2f | 0/32 | 0/32 | 3/32 | 9/32 | 4/32 |
| d4f71fd0abeb | 0/32 | 1/32 | 1/32 | 15/32 | 12/32 |
| 13d8f4105e58 | 0/32 | 1/32 | 3/32 | 17/32 | 10/32 |
| 2050cebe2f37 | 0/32 | 0/32 | 1/32 | 18/32 | 6/32 |

[Complete equipment comparison](../TestResults/tower-floor14-restoration-publication-20261003/equipment-comparisons.json). [Closed diagnostic receipt](../TestResults/tower-floor14-restoration-diagnostic-driver-20261003/completion.json). [Unallocated next refinement proposal](../TestResults/tower-floor14-restoration-publication-20261003/next-refinement-proposal.json). The original pre-run protocol remains in the diagnostic control directory.
