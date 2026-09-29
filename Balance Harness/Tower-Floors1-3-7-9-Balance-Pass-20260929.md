# Tower floors 1–3 and 7–9 — calibration, 29 September 2026

**Later floor-8 result:** the [midpoint refinement](Tower-Floor8-Health-Refinement-20260929.md) applies health **9.9064526367 (+5%)**, preserving offense and regeneration. The complete 67-cell confirmation establishes **47/160 (29.38%)** and **33/160 (20.62%)**; all 10,720 inputs and 67 full replays match. The report below retains its own historical setting and results. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for **906,552 exclusions**, current content and the floor-12 queue.

**Later floor-7 result:** the [expanded-family refinement](Tower-Floor7-Diversity-Refinement-20260929.md) applies health **4.4454238281 (+5.5%)**, with offense and regeneration unchanged. Fresh confirmation across **60 exact recipes / eight compositions** establishes two new lineups at **95/256 and 82/256**; 15,360 inputs and 60 full replays match. The results below remain historical at their stated settings. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for **905,278 exclusions**, the current Tower hash and the floor-8 queue.

**Later floor-3 tuning:** the [confirmed diversity pass](Tower-Floor3-Diversity-20260929.md) supersedes this report's floor-3 health value with **1.5047963378**; offense is unchanged. Two closely related compositions qualify at 90/256 and 56/256. Other floors and the historical evidence below remain unchanged.

**Later coverage verification:** the [complete reference-coverage pass](Tower-Reference-Coverage-20260929.md) retained these families, added the omitted exact search references and confirmed all floors 1–9 at their applied settings. Every expanded family passed; 93,184 inputs and 364 full replays matched. Guardian values stayed unchanged. This report preserves the original calibration; its earlier next-work recommendations and exclusion counts are historical. Follow the [current handoff](Tower-Continuation-Handoff-20260928.md) for the latest exclusion union and the next build-diversity pass.

**Completed: all six floors are applied locally and verified.** Each applied setting passed a separate fresh 35-cell, 8,960-fight confirmation. Application checks matched **53,760 native inputs and 210 complete battle reports**. Earlier floor-4/5/6 changes are preserved. These are fixed-budget benchmark results, not a claim that ordinary parties or the entire Tower are balanced.

| Floor | Applied health / offense | Strongest confirmation | Adjusted interval | Viable compositions / gear profiles | Mean seconds, strongest team |
| --- | --- | --- | --- | --- | ---: |
| 1 | 2.5101634336 / 2.4508682344 | 92/256 (35.94%) | 27.08–45.87% | 2 / 2 | 76.61 |
| 2 | 2.6618600366 / 2.5333570679 | 82/256 (32.03%) | 23.57–41.86% | 3 / 1 | 68.12 |
| 3 | 1.5394335937 / 6.7842480469 | 46/256 (17.97%) | 11.59–26.80% | 1 / 1 | 74.77 |
| 7 | 4.213671875 / 6.5953125 | 47/256 (18.36%) | 11.91–27.23% | 1 / 1 | 69.91 |
| 8 | 9.4347167969 / 8.8260253906 | 95/256 (37.11%) | 28.15–47.06% | 1 / 1 | 196.01 |
| 9 | 2.970703125 / 4.7036132812 | 42/256 (16.41%) | 10.34–25.04% | 2 / 1 | 62.67 |

Intervals are approximate simultaneous 95% Bonferroni-Wilson intervals **within each floor's 35-cell family**, not a joint six-floor coverage guarantee. A viable cell has a lower bound of at least 10%; every cell's upper bound must be at most 50%. Floor 9 passes with small lower-bound margins. Floor 1's earlier inconclusive setting and floor 8's selection-cap stop remain documented below.

Target: primary LL game Tower guardian data and offline Balance Harness. Continue the [confirmed floor-4/6 pass](Tower-Floors4-6-Balance-Pass-20260929.md), preserving floors 4–6, the user's repeating gear curve and the supported `affinity-creation-with-benchmark-validation-v1` search. Hypothetical benchmark ownership does not establish ordinary-player acquisition. Selectable dungeon supplies remain withdrawn.

## Prospective scope

| Floors | Characters | Level | Essences per character | Gear |
| --- | ---: | ---: | ---: | --- |
| 1–3 | 5 | 30 | 4 | Tier 1, Rare / Standard / rank 2 |
| 7 | 5 | 40 | 5 | Tier 1, Unique / Exceptional / rank 4 |
| 8–9 | 10 | 40 | 5 | Tier 1, Unique / Exceptional / rank 4 |

Baseline rolls, no styles, unascended/unevolved Essences. Preserve ordinal Essence order and native identity/position semantics; do not search permutations. Three retained compositions × seven gear profiles form the initial 21-cell family. Stronger gear profiles stay in the family. No new acquisition or reward implementation is in scope.

For each floor, in order 1, 2, 3, 7, 8, 9:

1. Prepare the full party through native rules, capture current content, and screen the original setting with 32 fresh seeds per cell (672 fights). Finish application of each floor before capturing the next one.
2. If needed, bracket a nontrivial setting with joint health/offense factors: double an easy setting or halve a hard setting within 0.25–16×; refine by midpoints. Prefer a strongest observed win rate of 15–35%, near 25%. Cap initial selection at twelve screens including the original. Do not assume other floors' factors or gear profiles transfer. These observations are selection evidence, not acceptance evidence.
3. Run the unchanged 528-fight supported affinity search from the three measured references at the strongest gear profile, then evaluate all five nominees on 128 separate seeds (1,168 total fights). Retain both generated finalists at all seven profiles, producing a fixed 35-cell family.
4. Screen that whole family at the candidate setting. If needed, continue bracket/refinement for at most eight expanded-family screens, with the same scalar limits and selection range. Retain every candidate archive, even unsuccessful ones. Freeze the selected setting and all 35 combinations before confirmation.
5. Confirm on 256 fresh seeds per cell (8,960 fights). Use the existing approximate simultaneous 95% Bonferroni-Wilson intervals separately per floor: every upper bound ≤50%, at least one lower bound ≥10%. Report every floor, including failures/inconclusive results. Do not pool phases, extend a confirmation, discard cells, or quietly retry a failed confirmation. A failed/inconclusive floor stays unapplied pending a separately declared follow-up.
6. Apply only a passing setting; check all 8,960 native inputs and one full archived battle per cell against the applied content. Record pacing and composition/gear diversity separately from acceptance. No whole-Tower, universal-build or Essence-necessity claim follows from these bounded families.

Every phase uses a fresh owner/output pair, the existing 900-second process / 840-second native limit, 2 GiB native output cap and at most 20,000 fights. Build via `build/run-tests.ps1` into `TestResults/tower-floors1379-build-20260929`. Preserve all **887,616** inherited exclusions and every subsequent balance-pass reservation, including unused ones. No deployment, shared database, migration, dependency, configuration or search-policy change.

Entry Tower SHA-256: **`44a87b7872265bfc55c4253bbc83a9577f6acd287e5d7cff80100b7776d55830`**. Entry ledger: `TestResults/tower-balance-pass-floor6-calibration-confirmation-owner-20260929/seed-ledger.json`, SHA **`9025d61a1a3a18a58069d4f8ed586c328942908333e9cf9ba6584e68d18b04e8`**. Old archives are immutable and local; later applications must preserve earlier floors' confirmed fields.

## Execution and interpretation

The expanded native regression suite passes **64 tests**, with three intentional opt-in skips and no failures. Projection support now admits floors 1–9 while preserving existing version strings and policies. Preparation tests assert each floor's full party, rank/rarity/quality, unascended Essences and ordinal order before any study fights.

The local [selection driver](../TestResults/tower-floors1379-selection-driver-20260929.py) executes the prospective scalar protocol through the existing bounded owner; it does not change the supported composition search or write production content. Each floor gets a new control directory with the driver hash, per-phase declarations/logs and frozen selection. Expanded-family bracketing starts afresh: a hard setting for the three retained compositions is not assumed hard for generated teams. Final application remains a separate verified step.

Floor 1's original-setting screen completed 672 fights and exposed multiple **32/32** routes. Continue the declared bracket at joint factor **2×**, keeping every profile and composition. This is selection evidence only.

Floor 1's initial bracket selected **1.375×** after five 21-cell screens. At that setting the supported search found generated teams with **82/128 and 60/128** wins, versus a strongest retained result of **29/128**. Six expanded-family screens retained all 35 combinations and selected **1.4609375×**; the strongest screening result was **6/32**. That setting and family were frozen before the 8,960-fight confirmation. Both search finalists remain included, with no Essence-order variants.

### Floor 1 inconclusive result and separate follow-up declaration

The first confirmation completed all 8,960 fights and returned **Inconclusive**. Its strongest result was **26/256 (10.16%)**, which did not establish the 10% lower-bound threshold; the next results were 13/256 and 11/256. No floor-1 data change was applied. Retain the original confirmation, audit, manifest and all reserved seeds. It is not extended, retried or pooled.

Declare one separate follow-up at a **different setting**, starting at joint factor **1.41796875×**, halfway between the earlier easy 1.375× and the inconclusive 1.4609375×. Keep the exact same 35 compositions/profiles. Use at most four new 32-seed screens, narrowing that bracket if needed, preferring **7–10 wins out of 32** rather than the low edge of the earlier window. Freeze one selected setting before at most one additional 8,960-fight confirmation. Report both confirmations regardless of the second verdict. Apply only if that fresh panel passes; otherwise floor 1 remains unapplied. This is a new declared calibration step, not reuse of the closed confirmation.

Before any floor-2/3/7/8/9 study starts, tighten their expanded-family selection window to **7–10/32** to leave more room above the lower acceptance threshold. Initial reference selection remains 15–35%; all phase/fight limits, family sizes, search and confirmation rules remain unchanged. The [v2 local driver](../TestResults/tower-floors1379-selection-driver-v2-20260929.py) implements only that prospective window change. Preserve the original driver and floor-1 evidence.

The four floor-1 follow-up screens tested factors **1.41796875, 1.396484375, 1.4072265625, 1.41259765625**, with strongest rates **6/32, 13/32, 11/32, 9/32** respectively. Freeze the last setting for the single separate follow-up confirmation, retaining all 35 cells and excluding every prior seed. Its two strongest health-and-regeneration teams screened at 9/32 and 7/32; this is not yet acceptance evidence.

The separate floor-1 follow-up confirmation **passed**. The two generated health-and-regeneration teams won **92/256 (35.94%)** and **61/256 (23.83%)**, with adjusted intervals **27.08–45.87%** and **16.44–33.21%**. The first team also establishes viability in armor-and-health gear: **42/256**, interval **10.34–25.04%**. All 35 upper bounds are below 50%. This is three viable cells across two compositions and two profiles; the earlier 26/256 result remains a separate inconclusive setting.

Floor 1 is applied locally at **health 2.5101634336 / offense 2.4508682344**, preserving every other guardian field. The checked application must complete before floor 2 captures its content. The strongest two combinations' mean engine durations are 76.61 and 75.84 seconds across wins and defeats; these are not player-time measurements.

### Floor 2 selection

Floor 1's application check matched all **8,960 inputs and 35 full reports** before floor 2 was prepared. Floor 2 initially had multiple 32/32 routes. Seven initial screens selected **1.28125×**, with retained armor-and-health teams at 7/32. The supported search's separate evaluation produced stronger teams at **63/128 and 56/128**, versus a strongest reference at 35/128. Eight expanded-family screens selected **1.30126953125×**, with strongest results **10/32 and 8/32**. Freeze that full 35-cell family for the independent confirmation.

The local [application/continuation helper](../TestResults/tower-floors1379-apply-and-continue-20260929.py) can apply completed passing studies in floor order. It first requires a passing result, checks that substituting only the target floor's health/offense exactly reproduces the confirmed whole-Tower JSON, records the previous values and source, then invokes the existing input/full-report application verifier before preparing the next floor. It stops on any incomplete, failed or inconclusive study. No guardian is changed solely from a screening result.

Floor 2's fresh confirmation **passed**. Its strongest generated teams won **82/256 (32.03%)** and **63/256 (24.61%)**, intervals **23.57–41.86%** and **17.10–34.05%**. A retained composition also establishes viability at **48/256 (18.75%)**, interval **12.22–27.67%**. All three viable cells use armor-and-health gear; all 35 upper bounds remain below 50%. Apply **health 2.6618600366 / offense 2.5333570679**. All 8,960 native inputs and 35 complete battle replays match. Mean durations for the strongest two are 68.12 / 69.32 seconds.

### Floor 3 selection

Floor 3 also had multiple 32/32 routes at its original setting. Three initial screens selected **1.5×**. The supported search's generated teams won **49/128 and 27/128**, versus the strongest retained team's 26/128. Eight expanded-family screens selected **1.5234375×**, with strongest results **9/32 and 8/32** in health-and-regeneration gear; resistance-and-health also had 6/32. Freeze this setting and all 35 cells before fresh confirmation.

Floor 3's confirmation **passed**, with a strongest result of **46/256 (17.97%)**, interval **11.59–26.80%**, and mean duration **74.77 seconds**. Only that generated health-and-regeneration cell establishes viability; the next two cells won 27/256 and 26/256. Apply **health 1.5394335937 / offense 6.7842480469**. All 8,960 inputs and 35 full reports match. This floor's composition diversity remains unfinished despite passing the declared acceptance rule.

### Floor 7 selection

Five initial screens selected **3.5×** after multiple 32/32 routes at 1×, 2× and 3×. The strongest retained resistance-and-health team screened at 7/32. At that setting, generated teams won **107/128 and 102/128**, compared with the strongest retained result of **29/128**. Eight full-family screens selected **3.6640625×**, with strongest results **7/32 and 6/32**, both resistance-and-health. Freeze all 35 cells and this setting before confirmation. The original regeneration scalar **0.1** and all guardian mechanics remain intact throughout.

Floor 7's confirmation **passed** at **47/256 (18.36%)**, adjusted interval **11.91–27.23%**, with a 69.91-second mean battle duration for the strongest team. The other generated team won 30/256; only one resistance-and-health cell establishes viability. Apply **health 4.213671875 / offense 6.5953125**, retaining regeneration 0.1. All 8,960 native inputs and 35 complete reports match before floor 8's preparation. Broader composition viability remains unfinished here.

### Floor 8 selection and bounded refinement follow-up

Eight initial screens selected **5.75×**. The strongest retained armor-and-health team screened at 5/32. Search evaluations exposed much stronger generated teams: **120/128 and 107/128**, versus the strongest retained result **23/128**. Eight expanded-family screens ended at **6.01953125×**, with a strongest result of **14/32**, outside the prospectively tightened 7–10/32 window. The driver **stopped at its selection cap**, before any confirmation or floor-8 application. Every native phase and audit completed; the control-layer stop is preserved and is not a failed combat run or a retry.

Declare one separate refinement round of **at most four** new 32-seed screens, keeping all 35 cells and the existing search outputs. Start at **6.064453125×**, between 6.01953125× (14/32) and 6.109375× (6/32), and narrow this bracket if needed toward 7–10/32. Freeze one setting before the first and only floor-8 confirmation (8,960 fresh fights). Preserve the closed eight-screen round without extending its archives. If no suitable setting is found, leave floor 8 unapplied and report the limit.

Pacing is a separate open issue: the stronger candidate teams' fights are around **three minutes**, considerably longer than the early-floor leaders. A passing win-rate interval alone will not establish that this duration is desirable. Joint health/offense scaling and unchanged boss mechanics remain the scope of this numerical pass.

The separate floor-8 refinement tested **6.064453125×** (strongest 13/32) and **6.0869140625×** (strongest **9/32**). Freeze the latter setting and all 35 cells before its first confirmation. The strongest armor-and-health team averaged 196.34 seconds in that selection screen. The other generated armor team won 3/32; every other cell had zero wins in this small screen. Do not infer broad viability from the leading team.

Floor 8's confirmation completed all **8,960 fights in 428.13 native seconds** and **passed**. The strongest generated armor-and-health team won **95/256 (37.11%)**, adjusted interval **28.15–47.06%**, averaging **196.01 seconds** per battle. The other generated armor team won **23/256 (8.98%)**; all other cells had zero wins. Only one cell establishes viability. Apply **health 9.4347167969 / offense 8.8260253906**, preserving every other guardian field. This passes the fixed-family win-rate rule while leaving substantial composition-diversity and pacing work.

### Floor 9 selection

Floor 8's application matched all 8,960 inputs and 35 full reports before floor 9 was prepared. Eight initial screens selected **2.4375×**, with the strongest retained resistance-and-health team at 10/32. Search returned **`BenchmarkRetained`**: its internal paired gate had eleven gained wins and nine lost wins. Separate evaluations gave generated teams **39/128 and 33/128**, versus the strongest retained reference at **35/128**. These close observations do not demonstrate search superiority. Both generated finalists and all retained references enter the 35-cell family regardless of the retained benchmark.

Eight expanded-family screens selected **2.4755859375×**, with strongest results **8/32 and 7/32** in resistance-and-health gear, followed by 2/32. Freeze that setting and all 35 cells before the final floor's fresh 8,960-fight confirmation.

Floor 9's confirmation **passed narrowly**. A generated resistance-and-health team won **42/256 (16.41%)**, interval **10.34–25.04%**; the retained reference won **41/256 (16.02%)**, interval **10.03–24.60%**. Both establish viability under this rule, with small lower-bound margins. The other generated team won 28/256. Their close outcomes do not change the supported search or demonstrate generated-team superiority. Apply **health 2.970703125 / offense 4.7036132812**. The strongest two mean durations are **62.67 / 62.79 seconds**.

## Verification, changed files and release implications

All six applications matched 8,960 native inputs and 35 complete battle replays each. Against the entry snapshot, **only twelve Tower data fields changed**: health and offense on floors 1, 2, 3, 7, 8 and 9. Floors 4–6 and 10–15, guardian mechanics, defenses, regeneration and the gear curve are preserved. The final Tower file SHA-256 is **`d6b5f829cd5921d739fb974c20ecf29105b03c732fcf3f94df7d888552e8ea92`**.

Changed source and documentation:

- [Guardian data](../LL/src/API/API.LL/Data/world-tower/tower-floors.json): the twelve confirmed health/offense values. Keep their selected precision for replay reproducibility.
- [Floor projection tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs): admit floors 1–9 without changing the existing version strings or supported search; test six added complete-party projections.
- [Balance-pass tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalancePassTests.cs): assert all nine early-floor party/level/Essence budgets and their correct rank, rarity and quality through native preparation.
- This report, the [handoff](Tower-Continuation-Handoff-20260928.md), [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md) and [harness README](../LL/tools/BalanceHarness/README.md): confirmed results, limitations and next Tower work.

Existing bounded owner, independent auditor and application verifier are reused unchanged. The local selection/application helpers and their frozen declarations are execution artifacts under ignored `TestResults`; they do not add a production algorithm or reward source.

Build/regression command (initial build, followed by the same command with `-NoBuild` after all applications):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-floors1379-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessAffinitySearchTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
```

Both initial and final regressions passed **64 tests**, with **three intentional opt-in skips and zero failures**; logs are linked below. The build used the authorized wrapper escalation for existing NuGet configuration access. The selection-cap stop on floor 8 is an intentional bounded control exit, followed by its separately declared round; no native phase failed or was retried. No command remains blocked. No dependency, migration, environment configuration, shared-database operation, API startup, deployment or replacement supply feature is introduced. The local guardian data will affect gameplay only when included in a later normal content release. Preserve unrelated concurrent edits.

Final static verification covers Python syntax, current documentation links, unchanged gear-curve content and the exact twelve-field Tower delta. `git diff --check` passes. The receipt is `TestResults/tower-floors1379-final-checks-20260929.json`.

## Evidence and continuation exclusions

The [evidence index](../TestResults/tower-floors1379-evidence-20260929.json) records **107 phases**, their manifests/audits/ledgers, six passing applications and the final data hash. Its SHA-256 is **`1e538b3a86421f5af6bf64a15b0c8c595370964216caa9ad6f91a878096d375a`**. The studies completed **152,160 fights**, including **62,720 confirmation fights** across seven separate settings (six passing, one inconclusive). Another **210 exact application replays** bring actual execution to **152,370 fights**; replays add no new statistical samples. Six preparations ran no combat. No phase was pooled, extended or retried.

The latest exclusion union is **893,646**, preserving every older unused reservation. Use [the floor-9 confirmation ledger](../TestResults/tower-balance-pass-floor9-cycle-confirmation-owner-20260929/seed-ledger.json), SHA **`4e015c7cc4b8185680140c9b8845dba22c08d968a7ab7a8cc2f2f6d6956044ae`**, and all linked history plus any later balance-pass ledgers. Never allocate from an older count. `TestResults` is ignored local evidence; verify availability before relying on it from another checkout.

| Applied floor | Confirmation study suffix after `tower-balance-pass-` | Manifest SHA-256 |
| --- | --- | --- |
| 1 | `floor1-cycle-followup-confirmation-study-20260929` | `df2c80383d9bdd699d6edf3f74b5badd586d64a65c5033519b2515e0e20d212f` |
| 2 | `floor2-cycle-confirmation-study-20260929` | `4ea3bd58c2db83e73174b2b05f85c03def3df95b5024d2d9c1e7714cdc59add9` |
| 3 | `floor3-cycle-confirmation-study-20260929` | `b768b0d7d1261adb5dd85fa0c504950bfede03335f9828f6d40c1ef324357b70` |
| 7 | `floor7-cycle-confirmation-study-20260929` | `cb0d565d237ce79f09d2bb68e664e1ab5a10590447824a74052147b716f64332` |
| 8 | `floor8-cycle-refinement-confirmation-study-20260929` | `5e34f4e0a1cb6936d3b08359710c2e0fd7f8884f6c704d162eda6a2ef4b8bff0` |
| 9 | `floor9-cycle-confirmation-study-20260929` | `6e015e2c4fd8b92f58b88dd906c5cfc2b40b5a2d755fb901a395b97829d23e89` |

Application receipts are in `TestResults/tower-floor{floor}-cycle-application-owner-20260929/completion.json`; the evidence index pins their result hashes. Each receipt records the whole-Tower hash at its sequential application. The floor-9 receipt records the final combined file; later floors changed only their own two scalars. Logs: [build/regressions](../TestResults/tower-floors1379-build-20260929.log), [final regressions](../TestResults/tower-floors1379-final-regression-20260929.log).

## What remains

1. **Floors 12–15 need the consistent final-curve pass.** The current cycle budget fixture stops at floor 11. Verify/declare character level, equipment tier, Essence count and party size for each remaining floor before preparing studies; the repeating rarity/quality/rank curve does not determine those budgets. Retain stronger gear carried from floors 10–11. Preserve prior floor-10/11 and historical floor-13/15 evidence within its captured scope.
2. **Build diversity remains narrow.** Floors 3, 5, 7 and 8 each have only one confirmed viable combination in their respective studies. Several other floors establish multiple compositions but only one gear profile. Floor 9's two lower bounds only narrowly clear the threshold. Retain these strong teams and references in future work; do not weaken bosses based only on weaker sampled parties or discard inconvenient strong results.
3. **Review floor-8 pacing.** Its leading team averages about 196 seconds, versus roughly 63–77 seconds on the other floors in this pass. A future pacing change needs a separately declared candidate and fresh confirmation; it is not justified solely by calling this numerical pass complete.
4. **Expected ownership and ordinary-player progression remain separate.** These parties have hypothetical complete gear and Essence ownership. Supply-dependent histories are not normal-economy evidence. Do not restart dungeon work or invent guaranteed supplies as a prerequisite for the Tower queue.
