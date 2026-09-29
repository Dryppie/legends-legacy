# Tower floors 12–15 — calibration, 29 September 2026

**Floors 12–14 are applied locally and verified; floor 15 is in progress.** Target: primary LL Tower guardian data and offline Balance Harness. The user selected seven Essences on floors 12–14 and eight on floor 15. Earlier ten-Essence floor-15 diagnostics are not the intended budget for this pass.

| Floor | Characters | Level / Essences | Tier | Cycle gear | Retained gear |
| --- | ---: | --- | ---: | --- | --- |
| 12–13 | 10 | 60 / 7 | 2 | Rare / Standard / rank 2 | Legendary / Masterpiece / rank 5 |
| 14 | 10 | 60 / 7 | 2 | Epic / Fine / rank 3 | Legendary / Masterpiece / rank 5 |
| 15 | 15 | 70 / 8 | 2 | Epic / Fine / rank 3 | Legendary / Masterpiece / rank 5 |

The [separate later-floor fixture](../LL/tools/BalanceHarness/Fixtures/tower-later-floor-balance-budgets.json) preserves the original eleven-floor preview. Baseline rolls, no styles, level-1 unascended/unevolved Essences and hypothetical complete ownership apply. Retained equipment means preserving the stronger item budget available from floor 10; this is not an earned-inventory replay or proof of acquisition. No selectable dungeon supplies or dungeon balancing is included.

## Frozen procedure

Process floors 12, 13, 14 and 15 sequentially, verifying an application before capturing the next floor. Each initial family contains the first three retained compositions at the declared Essence count, each with all seven existing profiles at both equipment budgets: **42 cells**. Canonical ordinal Essence order is fixed; permutations are not candidates.

1. Capture and prepare the native party with zero fights. Screen the current setting with 32 fresh seeds per cell, **1,344 fights**.
2. Select a linked health/offense multiplier within 0.25–16× the captured setting. Start at 1×, double easy settings or halve hard settings, then bisect the observed bracket. At most twelve initial screens; strongest observed rate must be 15–35%. Retain every screen, including unsuccessful settings.
3. Use the unchanged `affinity-creation-with-benchmark-validation-v1` search with the three measured references at the strongest equipment/profile budget: 528 search fights plus five nominees on 128 separate seeds, **1,168 fights**. Search uses that selected rank and quality explicitly. Do not transfer the cycle rank onto Legendary items or keep Legendary rank when projecting a finalist to the cycle budget.
4. Retain both generated finalists across all fourteen equipment/profile variants, giving **70 cells**. Screen all cells with 32 fresh seeds, **2,240 fights**. Select a strongest observed count of 7–10/32. Start at the initial selected factor; when too easy, probe the nearest earlier harder initial factor if one exists, then bracket by doubling/bisection. A prior hard observation is a probe, never assumed to apply to the expanded family. At most ten expanded screens.
5. Freeze the full family and selected setting before one fresh **256-seed confirmation per cell, 17,920 fights**. Use approximate simultaneous 95% Bonferroni-Wilson intervals separately for each floor's full 70-cell family. Every upper bound must be ≤50%; at least one lower bound must be ≥10%. Report cycle and retained results separately without dropping either family from the correction. No pooling, early acceptance, sample extension or silent retry. Fail/inconclusive settings remain unapplied.
6. Apply only a passing setting, changing that floor's health/offense scalars alone. Check all 17,920 native input hashes and one complete saved report per cell against applied content. Preserve all other floors and guardian fields. Report pacing and viable compositions/profiles separately from acceptance.

Each phase retains the existing 900-second owner / 840-second native deadline, 2 GiB native output cap and 20,000-fight ceiling. Before confirmation, extrapolate the last screen's duration and archived bytes; stop if either projects above 80% of its native cap. Do not launch an obviously over-budget confirmation or alter caps after launch. Preserve setup failures, incomplete output and unused reservations. No new search policy, reward, migration, database action or deployment.

Starting Tower SHA-256: `d6b5f829cd5921d739fb974c20ecf29105b03c732fcf3f94df7d888552e8ea92`. Starting scientific exclusion union: **893,646**. Latest preceding ledger: `TestResults/tower-balance-pass-floor9-cycle-confirmation-owner-20260929/seed-ledger.json`, SHA-256 `4e015c7cc4b8185680140c9b8845dba22c08d968a7ab7a8cc2f2f6d6956044ae`. Import every subsequent balance-pass reservation, including unused values.

Build with `build/run-tests.ps1` into `TestResults/tower-floors12-15-build-20260929`. Owner: [run-tower-balance-pass.py](analysis/run-tower-balance-pass.py); local selection driver and immutable evidence receipts will be recorded below. The existing equipment-cycle fixture and floors 1–11 remain unchanged by this pass.

## Preparation and execution

The corrected build passes **77 regressions**, with three intentional opt-in skips. An initial preparation assertion incorrectly expected identical native character GUIDs across different ranks/qualities; all four new cases exposed that assumption before any study combat. Native IDs include that power budget. The regression now preserves authored IDs, positions, Essence sets and identity equipment without asserting GUID equality. These are complete-budget comparisons, not an isolated causal estimate of item rarity.

Execution uses the [bounded local driver](../TestResults/tower-floors12-15-selection-driver-20260929.py) and [checked application sequence](../TestResults/tower-floors12-15-apply-and-continue-20260929.py). Each phase saves its declaration, fresh seed ledger, source hashes, native reports, independent audit and process receipt. The driver stops on resource preflight, selection-cap, failed or inconclusive confirmation; it never silently retries.

### Prospective reference-retention amendment

Floor 12's search evaluated references with the search's native identity encoding. These preserve Essence composition but differ from the original screen's exact recipes. Its generated finalists won 68/128 and 55/128 at 3.375×; projected references won 51/128, 34/128 and 0/128. The initial expansion retained the two generated finalists across all fourteen gear variants, but omitted those three projected reference recipes. Keep them as measured controls rather than assuming recipe equivalence from Essence sets alone.

The two 70-cell expanded screens completed at 3.375× and 3.5×, with strongest counts 16/32 and 8/32. The orchestration stop arrived after the 70-cell confirmation at 3.5× had launched. Let that bounded owner finish and preserve its verdict; **do not apply from that incomplete-family confirmation**. Its parent orchestration was intentionally stopped. A PowerShell process-stop error was resolved through the .NET process API, targeting only the two verified orchestration PIDs; native combat and its bounded owner were left to finish.

Before observing that confirmation result, declare the following correction. Retain the original 70 cells plus the **three exact projected references at their evaluated profile**, for **73 cells**. They are known reference representations, not a search over identities or Essence permutations. Use 32 fresh seeds per cell for selection (2,336 fights) and one separate 256-seed confirmation (18,688 fights), adjusting over all 73 cells. All resource limits and the 80% preflight remain. The 70-cell confirmation is neither extended nor pooled with this new family.

For floor 12 reuse its sealed preparation and completed search, start at factor 3.5, and allow at most ten new complete-family screens before one confirmation. Do not repeat the search or initial bracket. For floors 13–15 use the original procedure, but retain all five search nominees: generated finalists across fourteen variants, and the three exact projected reference recipes. Apply only from the complete-family confirmation and verify all **18,688 inputs plus 73 full reports** per passing floor.

The amended [selection driver](../TestResults/tower-floors12-15-selection-driver-v2-20260929.py) and [application sequence](../TestResults/tower-floors12-15-apply-and-continue-v2-20260929.py) use fresh `later-complete` owner paths. The unchanged initial floor-12 archives and all reserved seeds remain in the exclusion history. This corrects reference coverage; it changes neither the supported search nor the user's progression budget. Earlier 35/56-cell floor confirmations remain claims about their captured families; broader search-reference coverage should be audited before strengthening those claims.

The original 70-cell floor-12 confirmation completed **17,920 fights in 358.72 native seconds**, with all eight processes drained and zero retries. Its scoped verdict was **Pass**, led by 60/256 (23.44%; adjusted interval 15.74–33.41%) and 45/256. It remains unapplied because it omits the three projected references. The first amended 73-cell screen completed 2,336 fights at the same 3.5× setting, strongest 8/32. That full family and **health 9.275 / offense 11.9** were frozen before the separate 18,688-fight confirmation. Results from the earlier confirmation are not pooled into it.

### Floor 12 complete-family result

The fresh **73-cell confirmation passed**: 18,688 fights, 376.65 native seconds, zero retries and all eight processes drained. The strongest generated resistance-and-health team won **61/256 (23.83%)**, adjusted interval **16.04–33.87%**, averaging **70.58 seconds**. Every upper bound is below 50%, but only this one cell establishes the 10% viability threshold. The next strongest result is 36/256. All **35 cycle-gear cells won zero**: this calibrates around retained Legendary equipment, not a claim of Rare-only viability or ordinary-player acquisition. Application matched **all 18,688 native inputs and 73 complete reports**. Floor 12 now uses health **9.275**, offense **11.9**, with all other guardian fields and floors 1–11 preserved. Application receipt SHA-256: `2f71e73b782b801b2ce6b0533fdc057d23ebb2d56ae5da8b4c6aa3eb9e6f64b1`.

### Floor 13 selection

Six initial screens selected 3.75×, strongest 5/32. The supported search and separate nominee evaluation completed, strongest 50/128. Seven complete-family screens used factors **3.75, 4, 3.875, 3.8125, 3.84375, 3.828125, 3.8203125**, with strongest counts **15, 4, 4, 11, 2, 6, 7** out of 32. Freeze the final setting, **health 13.180078125 / offense 12.83625**, and all 73 cells before fresh confirmation. Resource preflight projects 415.50 native seconds and 468.18 MB, below the declared limits.

Fresh confirmation **passed**, completing 18,688 fights in **375.61 native seconds**, zero retries. The strongest generated resistance-and-health team won **54/256 (21.09%)**, adjusted interval **13.78–30.90%**, averaging **91.35 seconds**. Only this cell establishes viability; the next two results are 19/256. All 35 cycle-budget cells won zero. The confirmed health/offense values are now applied locally; **18,688 native inputs and 73 complete replays matched**. Application receipt SHA-256: `02402fb39e6fb2adb0cc790fd5643b357d3e952ca4585b579c8f03d104b49f51`. Every other floor-13 field and the earlier floors remain intact.

### Floor 14 selection

Eight initial screens tested **1, 2, 4, 3, 3.5, 3.25, 3.125, 3.1875×**, strongest counts **32, 32, 0, 26, 0, 4, 13, 7** out of 32. Both cycle-budget armor-and-health and retained equipment reached 32/32 at the original setting. The supported search's strongest separate nominee results were **38/128 and 33/128**. The first 73-cell screen at 3.1875× selected the setting with **10/32**, followed by 8/32 and 6/32. Freeze **health 12.68625 / offense 10.933125** for one fresh 18,688-fight confirmation. Its resource preflight projects 479.55 seconds and 478.23 MB. Record the roughly 131–137-second strongest-team durations as pacing observations, without inventing an acceptance threshold.

The fresh confirmation **passed**: 18,688 fights in **444.32 native seconds**, no retries. The strongest generated team won **91/256 (35.55%)**, adjusted interval **26.21–46.13%**, averaging **132.44 seconds**; the next generated team won 64/256. A retained composition's original and projected representations each won 51/256. Thus **four viable cells represent three distinct Essence compositions**, all at retained armor-and-health gear. Representation changes are not counted as new compositions. Every upper bound stays below 50%; all 35 cycle-budget cells won zero. Application matched **18,688 native inputs and 73 complete reports**. Floor 14 now uses health **12.68625**, offense **10.933125**, preserving other fields and earlier floors. Application receipt SHA-256: `8b97140da6722630da5a3a6bcd261fb9a097d33f299c9080c818897bc2584ca2`.

### Floor 15 selection and prospective resource amendment

Eight initial screens used **1, 2, 4, 3, 2.5, 2.75, 2.875, 2.9375×**, strongest counts **32, 32, 0, 4, 32, 26, 15, 10** out of 32. The supported search's strongest separate nominee results were **42/128, 38/128 and 34/128**. The first 73-cell screen selected 2.9375× with a strongest **10/32**. Its 2,336 fights took **97.83 native seconds**.

The declared resource preflight **stopped before any floor-15 confirmation launched or reserved seeds**: projected native time **782.64 seconds** exceeds the original 672-second safety allowance. Projected storage **716.74 MB** remains within the unchanged disk cap. Preserve that stop and all completed selection evidence; no screen or search is repeated.

Declare one separate, larger confirmation envelope **before launch**: **1,140 native seconds / 1,200 owner seconds**, with the same 80% safety rule (912 native seconds), 2 GiB output cap, 20,000-fight ceiling and **18,688 planned fights**. Freeze all 73 cells from `tower-balance-pass-floor15-later-complete-expanded-01-study-20260929`, at **health 15.275 / offense 10.28125**, before allocating 256 fresh seeds per cell. The acceptance rule and family are unchanged. The new envelope is explicitly restricted to floor-15 confirmation; ordinary phases retain 840/900 seconds. It cannot extend an already running study.

Rebuild the native deadline guard through the required test wrapper, verify all **70 non-test assembly hashes** still match the selected runtime, then launch one fresh `floor15-later-complete-large-confirmation` owner. Do not apply unless it independently passes and the usual 18,688-input / 73-replay application check succeeds. This is a prospective resource allocation after a zero-confirmation preflight stop, not a retry of combat or an extension of an observed confirmation result.

### Earlier-family coverage follow-up

A [read-only coverage audit](../TestResults/tower-earlier-reference-coverage-audit-complete-20260929.json) authenticated the saved recipe/result members for the earlier floor-1–9 searches and final confirmations. All **30 projected-reference entries** (including both floor-5 searches) lack an exact party representation in those final captured families. This is an entry count, not a claim of 30 distinct new compositions. No fights or seeds were used. Audit SHA-256: `1297cd6fe3041b98b9a68fc311fcffe0c904ebbfc883211d29349bc9ec46ebc6`.

The search observations were made at their own captured settings; absence does **not** establish an upper-bound breach at the applied settings. The earlier family-specific claims retain their scope. The next Tower verification should retain those evaluated reference recipes at the currently applied settings, alongside the previously confirmed families, before strengthening whole-Tower claims. Keep this ahead of another search-algorithm campaign or unrelated dungeon work. Broader composition viability and floor-8 pacing remain separate unfinished concerns.
