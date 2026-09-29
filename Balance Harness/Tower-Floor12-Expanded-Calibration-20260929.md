# Floor 12: expanded-family health calibration — 29 September 2026

**Applied locally and verified:** Volgrin health is **9.414125 (+1.5%)**; offense remains **11.9** and regeneration **1.0**. Fresh confirmation across **131 exact recipes / nine compositions** establishes **2 viable compositions**, with qualifying cells at **53/152 (34.87%)**, **36/152 (23.68%)**. Every adjusted upper bound is below 50%. All **19,912 native inputs and 131 full replays** match the applied content. **82 backend regressions passed**, with three intentional opt-in skips. The approved level-60, seven-Essence, tier-2 budget is unchanged; the viable teams remain poison-focused and use retained Legendary resistance-and-health gear. Ownership remains hypothetical.

## Prospective scope

The [supported-search challenge](Tower-Floor12-Search-Challenge-20260929.md) is closed `NoEligibleDiversityConfirmation`, without confirmation or a content change. Its complete screen found leaders at **41, 26, 19, 17, 11, 9, 7, 3 and 0 wins out of 96**. The strongest exceeded the frozen selection ceiling. This separate scope tests modest health increases, seeking at least two viable compositions while keeping the complete family below the balance ceiling.

Retain all **131 exact recipes / nine actual compositions / fourteen gear profiles**, including all 73 earlier recipes and every exact search nominee. Source: `TestResults/tower-balance-pass-floor12-search-challenge-expanded-screen-study-20260929`; manifest SHA **`23730cb1384eff8597b883882e5178a744f6bc4d06a0ed9ff976800603cd5eef`**. Search closeout SHA: **`ff164c3616288f8ca55e902312d82aea6bffb0040fc7d2590855b022d27c12d8`**. Initial exclusion union: **907,186**.

Entry health/offense is **9.275 / 11.9**, regeneration **1.0**; current Tower SHA is **`4227b811e5ce81428dc183f9ff18a80f11c2c02f4f31091743ee31b7bfc9916c`**. Preserve ten level-60 characters, seven level-1 unascended/unevolved Essences, tier-2 equipment, fixed rolls/positions and no styles. Keep both cycle Rare/Standard/rank-2 and stronger retained Legendary/Masterpiece/rank-5 profiles. Ownership remains hypothetical. The new candidates are related poison builds, not evidence of broad archetype diversity.

Freeze before combat:

1. Authenticate the complete source, current runtime/input pins and prior closeout. Prepare current content with zero fights and no seeds; require settings/catalog parity.
2. Evaluate exactly three health factors relative to entry content: **1.005, 1.01 and 1.015**, each across **131 × 64 = 8,384 fights** on fresh panels. Offense and all other content stay fixed in isolated copies. Grid eligibility requires every cell at most **26/64 wins**, with at least two actual compositions at **13/64 or more**.
3. Rank eligible settings by qualifying composition count, second-best composition wins, strongest rate closest to 30%, smallest scalar change, then label. Freeze at most the first two for separate **128-seed complete-family stability panels**, 16,768 fights each. Evaluate all frozen nominees; do not stop when the first looks favorable. Stability eligibility requires every cell at most **44/128**, and at least two compositions at **26/128 or more**. Select the best eligible setting with the same ranking; otherwise close without confirmation.
4. Extrapolate selected-panel time/bytes to **152 seeds**, requiring at most 80% of 840 seconds / 2 GiB. Freeze one independent **131 × 152 = 19,912-fight confirmation**. Across all 131 cells, require every approximate simultaneous 95% Bonferroni-Wilson upper bound at most 50% and at least two actual compositions with a lower bound at least 10%. This corresponds to qualifying cells at **29–54 wins**, with every cell at most **54**. The native one-composition `Pass` alone is insufficient.
5. Apply only a fully accepted health value. Verify all **19,912 native inputs and 131 full replays** against applied current content, with zero new seeds. Run backend regressions through `build/run-tests.ps1`; independently reconcile the archive, inputs, selection, seed ledger and exact content change.

Maximum **78,600 study fights + 131 conditional replays**, **600 fresh reservations**. Each native phase remains below 20,000 fights, 840 seconds / 900 process seconds and 2 GiB. No retries, extended panels, pooled observations, reused seeds, dropped cells, algorithm change, Essence reordering, identity search or fallback confirmation. All earlier failed/closed scopes and unused reservations remain preserved.

Driver: `TestResults/tower-floor12-expanded-calibration-driver-20260929.py`. Reuse the freshly verified `TestResults/tower-floor12-diversity-build-20260929` runtime; maintained search/combat code is unchanged. The protocol is captured before execution. Evidence under ignored `TestResults` is local and immutable. No dungeon changes, replacement supplies, migration, application configuration change or deployment is included.

## Completed selection and confirmation

The grid retained every cell and every result. At 64 samples, at least two compositions needed 13 wins and no cell could exceed 26. Only the declared +1.5% setting qualified; the +0.5% panel had too few qualifying compositions and +1% exceeded the ceiling by one win.

| Health increase | Best cell per composition, wins / 64 | Compositions at least 13 wins |
| --- | --- | ---: |
| +0.5% | 26, 11, 11, 10, 9, 7, 5, 4, 0 | 1 |
| +1% | 27, 13, 9, 8, 6, 4, 4, 3, 0 | 2 |
| +1.5% | 21, 16, 13, 9, 7, 6, 5, 4, 0 | 3 |

The single nominated stability panel also retained all 131 cells. It required two compositions at least 26/128 and no cell above 44/128.

| Health increase | Best cell per composition, wins / 128 | Compositions at least 26 wins |
| --- | --- | ---: |
| +1.5% | 31, 31, 28, 18, 14, 10, 9, 6, 0 | 3 |

The resource preflight passed before **one fresh 152-seed confirmation**. Selection observations were not pooled into it.

| Composition | Best measured gear | Wins | Adjusted interval | Mean engine seconds |
| --- | --- | --- | --- | ---: |
| `35bf6d…` | retained-resistance-and-health | 53/152 (34.87%) | 22.78%–49.27% | 68.90 |
| `353e4f…` | retained-resistance-and-health | 36/152 (23.68%) | 13.76%–37.64% | 71.25 |
| `93f5bc…` | retained-resistance-and-health | 27/152 (17.76%) | 9.37%–31.10% | 71.64 |
| `2c6a99…` | retained-resistance-and-health | 16/152 (10.53%) | 4.53%–22.57% | 71.80 |
| `7210d6…` | retained-resistance-and-health | 14/152 (9.21%) | 3.74%–20.93% | 72.13 |
| `da0192…` | retained-resistance-and-health | 13/152 (8.55%) | 3.36%–20.10% | 72.08 |
| `59a738…` | retained-resistance-and-health | 11/152 (7.24%) | 2.63%–18.40% | 73.55 |
| `e30c8c…` | retained-resistance-and-health | 8/152 (5.26%) | 1.62%–15.76% | 73.66 |
| `43c543…` | retained-resistance-and-health | 0/152 (0.00%) | 0.00%–7.67% | 74.03 |

Intervals are approximate simultaneous 95% Bonferroni-Wilson bounds across all **131 exact cells**. Qualifying cells require lower bounds at least 10%; every upper bound must be at most 50%. Exactly **2 cells / 2 actual compositions** qualify. Actual composition counts ignore labels, actor identities, gear and Essence ordering, without rewriting raw recipes.

Both qualifying compositions came from the new search. The previous leader won **27/152**, with an adjusted lower bound of **9.37%**, so it does not qualify at the new setting and expanded family.

All **63 cycle-only recipes** won **0/152**. These findings concern the declared retained Legendary budget and related poison teams. They do not establish Rare-only viability, broad archetypes or ordinary-player acquisition. The supported search and combat mechanics remain unchanged.

## Descriptive pacing

A read-only review authenticated **304 accepted-cell battle hashes**, reproducing win counts and duration means with **zero new fights or seeds**. Quantiles use nearest ranks; medians use the arithmetic midpoint.

| Accepted composition | All-outcome median seconds | All-outcome p90 seconds | Victory median seconds | Victory p90 seconds |
| --- | ---: | ---: | ---: | ---: |
| `35bf6d…` | 71.0 | 73.0 | 65.0 | 71.0 |
| `353e4f…` | 72.0 | 76.0 | 66.5 | 75.0 |

These are engine battle durations, not measured player waiting or acquisition time. No pacing threshold or pacing-mechanic change was introduced.

## Application, verification and next work

Only **floor-12 `guardianScaling.health` changed: 9.275 → 9.414125**. Parsed whole-Tower comparison matched the confirmation snapshot; offense, regeneration, every other guardian field and every other floor stayed unchanged. Application matched **19,912 native inputs and 131 full reports**, with zero new seeds. Final Tower SHA: **`0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd`**.

This calibration completed **61,832 study fights + 131 replays**, using **472 fresh reservations** and **1285.066 native study seconds**. All **762 immutable current runtime/input pins** matched; the authorized live Tower change was separately matched to the accepted snapshot. Together with the search, this continuation ran **81,416 study fights + 131 replays**, reserving **1,106 fresh values**. Final exclusion union: **907,658**. No active study remains.

The final backend suite passed **82 tests**, with three intentional opt-in skips, through `build/run-tests.ps1`. It was rerun after applying health. The unchanged maintained owner passed **14 Python tests**; the two scopes passed **22 seed-free selection/reference/budget checks**. The initial sandbox build could not read the local NuGet configuration; a separately authorized build succeeded before combat. No required command remains blocked.

| Artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor12-expanded-calibration-evidence-20260929.json` | `b217f1846048e331fe2ad59e0f0ed94b5ac19c079f6fb8e79e72b592013c00e8` |
| `tower-balance-pass-floor12-expanded-calibration-confirmation-study-20260929/files.json` | `7facc8455060b9c1c97bb0eb3ab36066bf665a9d288d4146b7ecc2156626e0c5` |
| `tower-balance-pass-floor12-expanded-calibration-confirmation-owner-20260929/independent-audit.json` | `24e7a755a3340592c49e690a7174e0cdf7de51ecd5c41510ce1d552982cffbdc` |
| `tower-balance-pass-floor12-expanded-calibration-confirmation-owner-20260929/seed-ledger.json` | `3ccd4e14ddd79b341e88b01dfa9f1e174acda842415e1bd2185e383f7a7df32c` |
| `tower-floor12-expanded-calibration-application-owner-20260929/result.json` | `dafd5d8ea5a1375c52e13ded4e264e2c724b9b29bb932c8ca299b54bbc088488` |
| `tower-floor12-expanded-calibration-final-regression-20260929.trx` | `145964d9efdcaf2eb350ccca61d46ebff5d07871ac6ab6de291862a7f46874ce` |
| `tower-floor12-confirmed-pacing-review-20260929.json` | `5f06f9539c599942b64fca2dd63e934c694dbe7451e297ff968044dd94c5692d` |

For later floor-12 work use this accepted 131-cell source, preserving all nine compositions, fourteen gear profiles and exact representations. Do not return to the old 73-cell family or rerun already retained searches. Preserve the closed search and rejected grid settings. Ignored `TestResults` evidence is local and is not included in a clean checkout.

Changed maintained files: [Tower data](../LL/src/API/API.LL/Data/world-tower/tower-floors.json), these two floor-12 reports, [handoff](Tower-Continuation-Handoff-20260928.md), [harness README](../LL/tools/BalanceHarness/README.md), [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md) and the historical [floors-12–15 report](Tower-Floors12-15-Balance-Pass-20260929.md). No migration, application configuration change, service startup or deployment occurred. The content takes effect when released through the normal process.

Next priority: **floor-13 build diversity**, preserving its 73-cell family and approved seven-Essence/level-60/tier-2 budget with retained Legendary gear. Broader archetypes and floor-8/14 pacing remain open. Dungeon work and replacement supplies are not prerequisites.
