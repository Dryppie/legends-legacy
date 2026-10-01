# Floor 2 fixed Feast-pressure trial — 30 September 2026

## Prospective protocol

This protocol is frozen before any fresh reservation. Test exactly **Gale 0.35 / Feast 0.65**, following the [armor-outlier diagnosis](Tower-Floor2-Armor-Outlier-Diagnostic-20260930.md). This magnitude is a hypothesis, not a prediction of acceptance. The source is the original live-catalog 115-recipe panel, `TestResults/tower-balance-pass-floor2-two-armor-expanded-screen-study-20260929`, manifest `b8e1f3b5bd00c1feac0035976c10f8960d12e1ea03d6a8378ee73baeb20d9862`.

Retain all **115 exact recipes / seven actual compositions**, original Essence order, character identities, party slots and equipment. Count compositions by per-slot Essence sets. The target is armor/health on slots **3 and 4**, eight specialized items: Chest/Head/Legs armor and Necklace health on each. Party budget remains five level-30 characters, four level-1 unascended/unevolved Essences each, tier-1 Rare/Standard/rank-2 gear, roll 1, no styles. Gear ownership is a balance assumption, not an acquisition claim.

The screen uses **512 fresh shared seeds per recipe**, split into four complete 128-seed native batches for resource limits: **58,880 fights**. Evaluate only the complete panel. A passing screen permits one independent equivalent 512-seed confirmation; maximum **117,760 fresh fights / 1,024 reservations**. Exclude all **915,673** prior seeds and every subsequent reservation. Never pool old observations, replace seeds, extend panels, drop controls, retry native batches or select from intermediate results.

Acceptance uses approximate 95% simultaneous Bonferroni-Wilson intervals across all 115 recipes. **Every recipe upper bound must be at most 50%; at least two actual compositions at the target partial gear must have lower bounds at least 10%.** At 512 samples the integer limits are **76–216 wins**. Individual batch verdicts do not decide aggregate acceptance. A failed screen stops the trial before confirmation; a failed confirmation stops before application.

Every batch keeps the existing **20,000-fight / 840-native-second / 2-GiB / 900-owner-second** limits. Admission uses doubled measured time/bytes below 80% of the corresponding native limits. The first reference is the preceding precision screen's fourth batch; subsequent batches use measured predecessors. Freeze all eight distinct screen/confirmation paths in a new declaration before allocation.

Use the authenticated `tower-floor2-precision-runtime-20260930` runtime with the exact original five combat assemblies and previously verified aggregate-aware test assembly. Reuse the authenticated **108 backend passes / four opt-in skips** and **18 aggregate Python safeguards**; no source implementation changed. Execute native fixtures only through `build/run-tests.ps1`. New driver, collector and conditional application scripts are under `TestResults/tower-floor2-feast-pressure-*`.

Materialize only the candidate Gale/Feast damage coefficients and corresponding descriptions in an isolated content copy. Keep floor-2 health/offense **2.6618600366 / 2.5333570679**, Dive **1.60**, targeting, cooldowns, Bleed, Scent, Feast stack limit and healing parameters. Live Gale/Feast starts at **0.65/0.20**, abilities SHA `fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`; Tower SHA `0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`.

Before any local abilities edit, independently recount both complete passing panels and verify native application parity across all four confirmation batches: **58,880 inputs and 460 full saved-seed replays**, without new seeds. Then apply only the verified ability changes and run the relevant backend regression. No deployment, migrations, database changes, search redesign or dungeon work.

The proposal is `TestResults/tower-floor2-feast-pressure-proposal-20260930.json`, SHA `bdb20113cb2217ba4e026d614476c0ef6ca274e192d7d4aa7d7f65be47090dd2`. The driver saves an immutable copy of this protocol before running. Append results below only after completion.

## Completed result

**Confirmed independently, applied locally and verified.** Gale is now **0.35** and Feast damage **0.65**; the matching descriptions were updated. The complete screen and separate confirmation each passed the original 115-recipe simultaneous bounds. No acceptance rule or retained loadout changed.

The screen's strongest recipe won **176/512 (34.38%)**, adjusted interval **27.44%–42.05%**. Its target partial-gear compositions D/C/A won **125/107/87 out of 512**, respectively. All screen upper bounds stayed below 50%.

The independent confirmation's strongest recipe is composition **D**, profile `armor-and-health`, at **171/512 (33.40%)**, adjusted interval **26.53%–41.05%**. Every one of the 115 recipes stays below the ceiling. The exact two-character armor target produces **3 qualifying actual compositions**:

| Composition | Confirmation wins | Adjusted interval | Meets 10% minimum |
| --- | ---: | ---: | --- |
| D (`34497de3…`) | 134/512 (26.17%) | 19.96%–33.51% | Yes |
| C (`5a063eba…`) | 93/512 (18.16%) | 12.94%–24.89% | Yes |
| A (`170058c2…`) | 80/512 (15.62%) | 10.80%–22.07% | Yes |
| B (`25c6b08e…`) | 49/512 (9.57%) | 5.91%–15.14% | No |
| Third reference (`e7de338f…`) | 40/512 (7.81%) | 4.57%–13.05% | No |

Across all retained gear variants, **11 recipes / 3 actual compositions / 7 gear profiles** qualify. Profiles: `armor-and-health`, `mixed-armor-baseline-slots-1-2-3-4`, `mixed-armor-baseline-slots-1-3-4`, `mixed-armor-baseline-slots-1-3-4-5`, `mixed-armor-baseline-slots-2-3-4`, `mixed-armor-baseline-slots-2-3-4-5`, `mixed-armor-baseline-slots-3-4`. These are related poison compositions, not established broad archetypes. The accepted equipment target needs **eight specialized armor/health items on slots 3 and 4**, instead of requiring all twenty full-party items. Ownership remains hypothetical; this is no new acquisition claim.

The full-armor ceiling control remains in the family. Historical Gale 0.35 / Feast 0.55 failed with D full armor at 251/512 and adjusted upper 56.73%; that closed study is not pooled into this result. The current fresh panels support the new setting under the declared family and budgets. They do not prove balance for every possible loadout.

## Application and verification

**117,760 fresh study fights / 1,024 new reservations** completed across eight native batches with zero retries. Screening and confirmation each use separate sets of 512 shared seeds. Final exclusions are **916,697**. After independent recounting and interval calculation, all **58,880 confirmation inputs and 460 complete saved-seed replays** matched the isolated candidate, across all four confirmation batches. These parity checks allocated no seeds. Only then were the four verified coefficient/description lines copied to the local abilities catalog.

**108 fresh backend tests passed / four intentional opt-in skips** after application. Each of the eight study fixtures passed; all four native application invocations passed their eight fixtures. The authenticated earlier 108-pass regression and 18 Python aggregate safeguards were reused at entry because their implementation/runtime was unchanged. No C# source, search helper, aggregate contract or combat binary changed in this trial. The independent collector checked complete raw recipes, every saved outcome, separately calculated confidence bounds, all seed panels, resource receipts and native fixture counts.

| Native batch | Fights | Native seconds | Archived bytes |
| --- | ---: | ---: | ---: |
| screen 1 | 14,720 | 158.07 | 182,568,817 |
| screen 2 | 14,720 | 158.53 | 182,593,191 |
| screen 3 | 14,720 | 158.83 | 182,552,155 |
| screen 4 | 14,720 | 150.07 | 182,589,666 |
| confirm 1 | 14,720 | 158.11 | 182,533,538 |
| confirm 2 | 14,720 | 159.29 | 182,571,931 |
| confirm 3 | 14,720 | 161.37 | 182,588,835 |
| confirm 4 | 14,720 | 150.02 | 182,568,431 |

All batches passed the measured resource admission and stayed within their original caps. Every owned process finished with zero active children. The post-application regression also completed successfully. Link and whitespace validation are recorded in the publication receipt; no required verification command remains blocked.

Live floor-2 health/offense remains **2.6618600366 / 2.5333570679**; Dive **1.60**, cooldowns, targets, Bleed, Scent, Feast's three-stack consumption and healing parameters are preserved. All other 101 live JSON catalogs are byte-identical to entry, including prior floor-5/floor-6 work. Only the abilities catalog changes. No migrations, configuration changes, database actions or deployment.

## Continuation

Floor 2's declared partial-armor target is now complete. **Review floor 4's equipment dependence next**, using the saved accepted family to define a finite partial-gear target before allocating another trial. The [gear coverage review](Tower-Gear-Coverage-20260930.md) identified floor 4 as the next early single-profile checkpoint. Keep its approved level/Essence/gear budget and full-gear controls; floor 4 actually uses five level-30 characters with four Essences each (the earlier ten-character description was a documentation error). The [completed floor-4 review](Tower-Floor4-Gear-Assessment-20260930.md) now defines its own unallocated partial-armor trial. Preserve the expected repeating gear curve and stronger carried equipment. No dungeon or supply work is needed for this balance review.

This acceptance belongs to the **complete aggregate confirmation**, not to any one 128-seed batch. A later consumer must use all four confirmation manifests and the aggregate evidence; it must not substitute one individually passing batch or silently repin older catalogs. The old floor-2 accepted-family row in historical coverage tables is superseded by this report.

## Evidence and changed files

- New report: `Balance Harness/Tower-Floor2-Feast-Pressure-20260930.md`. Updated continuation handoff, balance status, both harness guides and the preceding outlier report. Local game change: `LL/src/API/API.LL/Data/combat/abilities.json`, only Gale/Feast damage and descriptions.
- Frozen driver/collector/conditional application, candidate copy, receipts, commands and archives: `TestResults/tower-floor2-feast-pressure-*`. The original proposal and every preceding archive remain unchanged.
- Declaration SHA **`e7b2e4899e7a6a935e4e20b50b5f0588461c73c95f433091150eed9b242e0ce5`**; independent evidence `TestResults/tower-floor2-feast-pressure-evidence-20260930.json`, SHA **`e20330c6bff5f9514747cfb738207c088523f514e38d55e4a0ef92e7098ca120`**.
- Screen aggregate SHA **`6389b79e95ff42be543783a34bec7c5fc69be23e32c93891cbd112d8b5159e83`**; confirmation aggregate SHA **`5b8b785af27b9636e7309d3439d2f1d38cb1d6c9ccb7d6ce184757c7a1effcef`**. All eight paths/manifests are listed in the declaration and aggregate evidence.
- Application receipt `TestResults/tower-floor2-feast-pressure-application-20260930/completion.json`, SHA **`0affe03db2b1368043953394d8d8c7603cb8256dcea5ab60271fcff7876abe44`**; runtime remains `TestResults/tower-floor2-precision-runtime-20260930`.
- Latest seed ledger `TestResults/tower-balance-pass-floor2-feast-pressure-confirm-4-owner-20260929/seed-ledger.json`, SHA **`211bfb253870962ea0e0804435544e97aa31d5ae69f8389101ae8a58cb45f446`**, plus all ancestors/later reservations.
- Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**; abilities SHA **`169b61f23c2e1e301e64e176962bbe3dc8386ea941a87a7f5a8205c872963d6a`**.
- Current publication: `TestResults/tower-floor2-feast-pressure-publication-check-20260930.json`. Backend commands use `build/run-tests.ps1`; exact native and regression invocations are retained in the owned receipts/scripts. Independent verification command: `python -B -X utf8 TestResults/tower-floor2-feast-pressure-collect-20260930.py` (completed before application).
