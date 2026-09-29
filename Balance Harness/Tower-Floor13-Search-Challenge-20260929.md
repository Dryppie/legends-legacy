# Floor 13: retained-family search challenge — 29 September 2026

**Diversity confirmed and verified at unchanged settings:** the full **130-recipe / nine-composition** family establishes **5 viable compositions**, including **4 new compositions**, at **42/140 (30.00%)**, **38/140 (27.14%)**, **31/140 (22.14%)**, **30/140 (21.43%)**, **30/140 (21.43%)**. Every adjusted upper bound is below 50%. All **18,200 native inputs and 130 full replays** match current content. Nhalia remains at **health 13.180078125 / offense 12.83625 / regeneration 1.0**. The supported search, progression budget and all other game content remain unchanged. The successful builds remain related poison teams using retained Legendary equipment; ownership is hypothetical.

## Prospective scope (frozen before execution)

Target: primary LL Tower data and offline Balance Harness. At entry, floor 13 had one clearly viable composition in its complete 73-cell confirmation. This bounded scope uses the unchanged `affinity-creation-with-benchmark-validation-v1` search to seek additional real Essence compositions. Guardian health **13.180078125**, offense **12.83625** and regeneration **1.0** remain fixed throughout this scope.

Retain every exact recipe from `TestResults/tower-balance-pass-floor13-later-complete-confirmation-study-20260929`, manifest SHA **`cb70424799129e72ad5ea389d59a41724dd03bf54304d490b11779cb653c1133`**: **73 cells / five actual compositions / fourteen gear profiles**. Keep ten level-60 characters with seven level-1 unascended/unevolved Essences each, tier-2 equipment, fixed rolls/positions and no styles. Preserve both cycle Rare/Standard/rank-2 and stronger retained Legendary/Masterpiece/rank-5 budgets. Ownership is hypothetical; no dungeon or supply work is included.

The current checkout retains the verified floor-12 application. All 527 unchanged runtime/input pins matched its accepted archive; current Tower data matches that accepted snapshot. Initial current Tower SHA is **`0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd`**; initial seed exclusion union is **907,658**. Preserve all prior ledgers and unconsumed reservations. Authenticate the historical archive and its captured owner snapshot; record changed historical test/runtime inputs without repinning them. Reuse the verified backend build and perform zero-fight current-content preparation to establish current settings/catalog parity. Historical observations guide scope only; they are not pooled into new estimates.

Freeze before combat:

1. Prepare the original complete family against current content with zero fights and no seed allocation. Stop if native settings, target-floor content or catalogs differ.
2. Screen all **73 cells on 64 fresh seeds**, 4,672 fights. Separately freeze three distinct measured reference compositions at **retained-resistance-and-health** and **retained-restorer-specialization**, ordered by wins, remaining guardian health and cell ID.
3. Run one supported search at each frozen profile: 528 search fights plus all five nominees on 128 fresh samples, **1,168 fights per search**. No retries or alternate search policy. Preserve every exact finalist and projected reference, regardless of success.
4. Append each finalist at all fourteen original gear profiles, retaining its exact evaluated recipe and all three exact search references. Deduplicate only identical whole scenarios. Preserve all 73 originals and every nominee; at most **139 cells**. Count compositions by per-slot Essence sets without rewriting raw recipes or optimizing order/identity.
5. Before evaluating the expanded family, project time and bytes from the reference screen. Require at most 80% of 840 native seconds / 2 GiB. Then run one **96-seed complete-family screen**. Eligibility requires every cell at most **33/96 wins**, and at least two actual compositions with a cell at least **22/96 wins**. This is selection only.
6. If eligible and its separate resource preflight passes, freeze the complete family for exactly one independent **140-seed confirmation**. Adjust approximate simultaneous 95% Bonferroni-Wilson bounds across every exact cell. Every upper bound must be at most 50%; at least two actual compositions must have a lower bound at least 10%. The native one-composition `Pass` alone is insufficient. No pooling, seed reuse, sample extension, cell dropping or retry.
7. If accepted, verify all confirmed native input hashes and one full replay per cell against unchanged game content, with zero new seeds. Otherwise preserve the closed result for a separately declared calibration scope.

Maximum **39,812 study fights + 139 conditional replays**, **774 fresh reservations**. Each phase stays under 20,000 fights, 840 native seconds / 900 process seconds and 2 GiB; admission is at 80% of native time/bytes. Conditional phases allocate no seeds unless reached. No guardian adjustment is authorized within this search scope. Subsequent calibration must declare its own bounded candidate selection and fresh confirmation before combat.

Use `build/run-tests.ps1` and the verified artifacts `TestResults/tower-floor12-diversity-build-20260929`. Reuse the unchanged 82-test backend receipt and 14-test Python receipt only after binding their hashes and current inputs; native phases retain their own independent audits. Rerun regressions after any later gameplay edit. Driver: `TestResults/tower-floor13-search-challenge-driver-20260929.py`. Native evidence and captured protocols are immutable under ignored `TestResults`; they are unavailable in a clean checkout unless separately preserved.

No migration, application-setting change, API startup or deployment is part of this scope. See the [current handoff](Tower-Continuation-Handoff-20260928.md) and [preceding later-floor calibration](Tower-Floors12-15-Balance-Pass-20260929.md).

## Completed search and confirmation

The current reference screen completed **4,672 fights**; its strongest recipes won **6/64**. The retained-resistance-and-health search produced finalists at **27/128 and 25/128**, versus **20/128** for its benchmark on that panel. The restorer search produced finalists at **6/128 and 9/128**, versus **2/128** for its benchmark. Both searches returned `BenchmarkRetained`; neither passed its own paired promotion gate. These provisional observations do not establish comparative superiority.

All 73 original recipes, both generated finalists from each search and all ten exact nominee entries were preserved. Whole-scenario deduplication added **57 recipes**, with **66 provenance entries**, yielding **130 cells / nine actual compositions**. Every finalist has all fourteen frozen gear variants. The four new builds each make real Essence substitutions on one character:

| New composition | Party slot | Removed Essences | Added Essences |
| --- | ---: | --- | --- |
| `5ae872…` | 1 | crystal_wisp, glade_panther | venomous_snake, viper |
| `0cc3f8…` | 9 | venomous_snake | viper |
| `4ca327…` | 5 | frost_imp, goblin | poisonous_rat, viper |
| `a7068a…` | 8 | cinder_beetle, giant_bat | venomous_spiderling, viper |

No Essence permutation or identity optimization occurred. The variants remain poison-focused.

The complete-family selection screen ran **130 × 96 = 12,480 fresh fights**:

| Composition | Best measured gear | Wins |
| --- | --- | --- |
| `a7068a…` | retained-resistance-and-health | 28/96 |
| `0cc3f8…` | retained-resistance-and-health | 24/96 |
| `5ae872…` | retained-resistance-and-health | 20/96 |
| `7ca30c…` | retained-resistance-and-health | 19/96 |
| `4ca327…` | retained-resistance-and-health | 16/96 |
| `819a34…` | retained-resistance-and-health | 5/96 |
| `7210d6…` | retained-resistance-and-health | 3/96 |
| `59a738…` | retained-resistance-and-health | 2/96 |
| `43c543…` | retained-resistance-and-health | 0/96 |

The top two actual compositions reached **28/96 and 24/96**, meeting the frozen minimum 22 wins; no recipe exceeded 33. The fixed **140-seed confirmation** was then admitted by its time/byte preflight. No grid, scalar adjustment or repeated confirmation was needed.

| Composition | Best measured gear | Confirmation wins | Adjusted interval | Mean engine seconds |
| --- | --- | --- | --- | ---: |
| `5ae872…` | retained-resistance-and-health | 42/140 (30.00%) | 18.38%–44.93% | 89.63 |
| `a7068a…` | retained-resistance-and-health | 38/140 (27.14%) | 16.11%–41.95% | 90.20 |
| `4ca327…` | retained-resistance-and-health | 31/140 (22.14%) | 12.29%–36.60% | 89.14 |
| `7ca30c…` | retained-resistance-and-health | 30/140 (21.43%) | 11.76%–35.82% | 90.39 |
| `0cc3f8…` | retained-resistance-and-health | 30/140 (21.43%) | 11.76%–35.82% | 90.44 |
| `7210d6…` | retained-resistance-and-health | 9/140 (6.43%) | 2.11%–17.94% | 97.11 |
| `819a34…` | retained-resistance-and-health | 7/140 (5.00%) | 1.43%–16.00% | 95.46 |
| `59a738…` | retained-resistance-and-health | 1/140 (0.71%) | 0.05%–9.52% | 91.74 |
| `reference-1` | retained-restorer-specialization | 1/140 (0.71%) | 0.05%–9.52% | 96.32 |

The approximate simultaneous 95% Bonferroni-Wilson bounds include every one of the **130 exact cells**. Every upper bound is at most 50%; at least two actual compositions have lower bounds at least 10%. Exactly **5 cells / 5 compositions** qualify, including **4 compositions absent from the original family**. Counts use per-slot Essence sets, ignoring labels, identity fields, gear and Essence order without rewriting raw recipes. Search and screen observations were not pooled into this confirmation.

All **63 cycle-only recipes** won **0/140 fights**. The result supports the declared retained Legendary equipment budget. It does not establish Rare-only viability, broad archetype diversity, ordinary-player win rates or equipment acquisition.

## Descriptive pacing

The read-only duration review authenticated **700 qualifying-cell battle hashes** and reproduced the counts and means. It ran zero new fights and allocated zero seeds. Quantiles use nearest ranks; medians use the arithmetic midpoint.

| Qualifying composition | All-outcome median seconds | All-outcome p90 seconds | Victory median seconds | Victory p90 seconds |
| --- | ---: | ---: | ---: | ---: |
| `7ca30c…` | 89.0 | 99.0 | 84.0 | 92.0 |
| `5ae872…` | 89.0 | 99.0 | 83.0 | 93.0 |
| `0cc3f8…` | 90.0 | 99.0 | 84.0 | 93.0 |
| `4ca327…` | 89.0 | 99.0 | 82.0 | 95.0 |
| `a7068a…` | 91.0 | 99.0 | 78.5 | 92.0 |

These are engine durations, not measured player waiting or acquisition time. No pacing threshold or mechanic changed.

## Current-content verification and continuation

Whole-Tower content is unchanged from the entry snapshot, SHA **`0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd`**. The native verification matched **18,200 confirmed inputs and 130 complete battle reports**, with **zero new seeds**. Floor 12's preceding health edit and all other existing work remain intact. There is no floor-13 guardian adjustment to apply.

This scope completed **37,688 study fights + 130 replays**, reserving **774 fresh values** and reaching **908,432 exclusions**. Native study time was **830.360 seconds**, excluding audits and replay verification. All owners completed without retry and drained their processes. All **711 current immutable runtime/input pins** matched.

The preceding **82-test backend receipt (three intentional skips)** and **14-test Python receipt** were authenticated and reused because current content and implementation stayed unchanged. They were **not rerun** in this scope. Initial baseline verification checked 527 runtime/input pins against the accepted floor-12 state; this scope also passed **11 fresh seed-free selection/family/budget checks**. Every native phase and the parity verification ran through `build/run-tests.ps1` with its own passing execution receipt and audit. Historical floor-13 test DLL/source drift was recorded without repinning the earlier archive; fresh current-content preparation independently verified settings/catalog parity. No required command remains blocked.

| Artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor13-search-challenge-evidence-20260929.json` | `df7a8636fe3913ea3c6e4043f535fcfda105498989c8228532d9b3344302e066` |
| `tower-balance-pass-floor13-search-challenge-confirmation-study-20260929/files.json` | `af56729b3f475edad39f99766bc8f64fdd4d1a789900c686fa361ec97f2685ac` |
| `tower-balance-pass-floor13-search-challenge-confirmation-owner-20260929/independent-audit.json` | `c067b7cca17c7c0bb86fd31d990824fa00fad4336f2013d7cbbadfa616b65b8b` |
| `tower-balance-pass-floor13-search-challenge-confirmation-owner-20260929/seed-ledger.json` | `7c4c95402a1a30857a31c525e69a2779e03fad0aab533dc46519291100142818` |
| `tower-floor13-search-challenge-parity-owner-20260929/result.json` | `51faf5033f98213107ddd29c6c71e32eb390ea76766f5938a379edc2f5819165` |
| `tower-floor13-verification-reuse-20260929.json` | `230d19ac79883d34ac4878c15f6fc7c8e62635d98d55c5d130e7beec0d7589af` |
| `tower-floor13-confirmed-pacing-review-20260929.json` | `a434c477742da0837fb36652b70ea857208b8a35819cde52023d6faece42eb80` |

For later floor-13 work, use this accepted **130-cell family**, preserving all nine compositions, fourteen gear profiles and exact variants. Do not return to the old 73-cell family, drop unsuccessful nominees or rerun these searches. Preserve every prior archive and unused reservation. Ignored `TestResults` evidence is local and is not included in a clean checkout.

Changed maintained files are this report, the [handoff](Tower-Continuation-Handoff-20260928.md), [harness README](../LL/tools/BalanceHarness/README.md), [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md), and a follow-up notice in the [historical floors-12–15 report](Tower-Floors12-15-Balance-Pass-20260929.md). No game data, search algorithm, combat code, dungeon, supply, migration, application setting or deployment changed in this scope.

Next: consolidate the latest **floors-1–15 balance evidence** into a read-only status review. Count actual viable compositions separately from exact recipes and gear variants, identify poison/gear concentration, and review saved floor-8/14 duration distributions. Do not declare the whole Tower broadly balanced or invent a pacing threshold. Existing fixed-budget evidence does not establish acquisition; dungeon work and replacement supplies are not prerequisites.
