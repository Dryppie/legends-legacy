# Floor 12: fixed limited-Restoration diagnostic — 2 October 2026

## Completed result: viable equipment routes, no nomination

All **8,576 fresh fights / 32 reservations** closed and were independently recounted. Every one of the nine actual compositions has an eight-item Restoration recipe at **6/32 or more**, but three exceed the 13/32 exploratory ceiling. **The 0.55 setting is not nominated or accepted.**

| Actual composition prefix | Baseline | Six items, healer 2 | Six items, healer 7 | Four items each | Twelve-item control |
| --- | ---: | ---: | ---: | ---: | ---: |
| `a622367ad4bb` | 0 | 1 | 0 | 6 | 0 |
| `604ac1457f2f` | 1 | 0 | 1 | 12 | 2 |
| `d4f71fd0abeb` | 0 | 3 | 2 | 11 | 3 |
| `5c3ae59aa520` | 0 | 5 | 3 | 14 | 7 |
| `5ca15aacb3bd` | 1 | 2 | 2 | 12 | 4 |
| `e8fa67a114cf` | 2 | 5 | 2 | 17 | 11 |
| `046ef3e3ae8a` | 0 | 2 | 1 | 15 | 5 |
| `3e381aa53a03` | 0 | 2 | 1 | 11 | 6 |
| `b1b3d7ed4d94` | 1 | 2 | 0 | 10 | 3 |

Every count is out of 32, with identical seeds across recipes in this panel. Exact raw reports show that eight-item mixtures can outperform the twelve-item donor. These small diagnostic panels do not estimate precise population win rates or prove the reason for that difference.

A [closed native attribute comparison](../TestResults/tower-floor12-restoration-equipment-tradeoff-20261002.json) verified the same tradeoff on all **18 healers across nine compositions**. The eight-item mix has native `CritChance = 31.95` and `AbilityHaste = 31.95`, versus zero for both in the twelve-item profile; `Restoration` is **426 versus 596.4**. All other native combat attributes match between those healer counterparts. This establishes the equipment tradeoff, not an isolated causal estimate of its contribution to wins.

All **241 retained native snapshots** match the preceding closed 0.55 study exactly, including the guardian. All **27 new native variants** passed the baseline/donor item and unchanged-character checks, and prepared participants stayed invariant within every recipe. The native fixture and twelve penetration guards passed with zero native skips. Native combat took **244.09 seconds**.

[Closed receipt](../TestResults/tower-floor12-restoration-diagnostic-driver-20261002/completion.json), SHA-256 `e02be3c423d104179cddcc6a314274bd209488bd1422d6b63cbe6c7b35d88d8a`; [complete equipment comparison](../TestResults/tower-floor12-restoration-next-20261002/equipment-comparisons.json). Final exclusions: **928,924**. No confirmation, application or live guardian edit followed.

The next [separate fixed 0.575 refinement](Tower-Floor12-Restoration-Refinement-20261002.md) retains every recipe and the same diagnostic rule, with a separately frozen 8,576-fight / 32-seed budget. It does not pool or extend this closed panel.

## Fixed question and budget

After successful native preparation of the [saved 268-recipe family](Tower-Floor12-Limited-Restoration-20261002.md), test one isolated setting: original guardian offense **11.9 × 0.55 = 6.545**, penetration **1 × 40 = 40**. This setting kept the strongest previous control at 7/32 but had no eligible diagnostic nominee. The new question is whether six/eight-item healer equipment supplies viable routes within the existing equipment allowance. No old outcome contributes to the new panel.

Retain all **268 recipes / nine actual compositions / 155 eligible recipes**: all 241 previous controls, plus all 27 exact Restoration subsets. Preserve Health **9.414125**, defenses, regeneration, abilities, all other floors, player progression and every raw actor/Essence/item order.

Use exactly **32 fresh seeds / 8,576 fights**, disjoint from the starting union of **928,892**. One complete diagnostic only: no search, dropped controls, replacement seeds, retry, extension, other setting, confirmation or application. Stop on a failed prerequisite or execution and preserve its evidence and reservations.

## Admission and verification

Require the closed expanded-family preparation receipt, all 268 exact raw recipes, unchanged qualified runtime/catalog pins, passed binding/integration regressions and complete original qualification. Native execution goes through `build/run-tests.ps1`. Rerun the twelve existing penetration rejection checks before allocating combat.

Measure the slower closed 0.50 panel and scale its complete cost from 241 to 268 recipes, then double time and bytes. Check free space before allocation and retain **840 seconds native / 900 seconds native owner / 2 GiB output / 2 GiB disk reserve**. The enclosing execution owner has **1,800 seconds**. Observe supervisor stdout only while it runs.

After closure, independently recount all 8,576 raw outcomes, the exact recipe/seed schedule and per-recipe prepared-participant invariance. Authenticate the isolated candidate delta and all source/runtime/catalog/ledger pins.

Compare the retained 241 recipes' complete prepared participants with their earlier closed 0.55 diagnostic. For each new variant, require the same guardian; identical unchanged characters; exact full-donor participants for six-item healers; and unchanged identity, abilities, Essence, weapon and other non-stat fields for four-item healers. Every native equipment entry must be the exact baseline or donor item selected by the raw recipe, in the original native order. Health must match MaxHealth. Four-item combined attributes come from the qualified native preparation, not arithmetic interpolation of old attributes.

## Predeclared decision

Assess only after the complete recount and native comparisons. Nominate this fixed setting for **diagnostic follow-up only** if two distinct actual compositions each have an eligible recipe at **6/32 or more**, while **every recipe is at 13/32 or less**. Otherwise nominate none. Variants do not count as distinct compositions, and these exploratory cutoffs are not statistical acceptance.

Any nomination needs a separate strict full-family acceptance contract and fresh independent screening/confirmation, with simultaneous bounds recalculated for 268 recipes, followed by application parity. No formal acceptance or live guardian change is authorized by this diagnostic receipt itself.

Runner: `TestResults/tower-floor12-restoration-diagnostic-20261002.py`. Control: `TestResults/tower-floor12-restoration-diagnostic-driver-20261002/`. All studies and unsuccessful earlier settings remain immutable. No migration, environment configuration change or deployment is included.
