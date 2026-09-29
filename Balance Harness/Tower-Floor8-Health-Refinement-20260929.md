# Floor 8: midpoint health refinement — 29 September 2026

**Applied locally and verified:** Kodoku health is **9.9064526367 (+5%)**; offense remains **8.8260253906** and regeneration **1.0**. Fresh confirmation across **67 exact recipes / nine compositions** establishes **2 viable lineups** at **47/160 (29.38%)** and **33/160 (20.62%)**. Every adjusted upper bound is below 50%. All **10,720 native inputs and 67 full replays** match the applied content. **82 backend regressions passed**, with three intentional opt-in skips. The accepted teams remain poison-focused and share armor-and-health gear; broader archetypes, ordinary-player acquisition and pacing targets remain unestablished.

## Prospective scope

The [coarse health calibration](Tower-Floor8-Expanded-Calibration-20260929.md) is closed `NoStableCandidate`: +4% exceeded its stability ceiling, while +6% left only one qualifying composition. This separate scope fixes **one midpoint candidate, +5% health**, before any new combat. There is no new algorithm/search, continued old panel or repeated confirmation.

Keep the full **67 exact recipes / nine compositions** from the unchanged expanded screen `TestResults/tower-balance-pass-floor8-search-challenge-expanded-screen-study-20260929`, manifest SHA **`bb1dcf707139cda5c881a72228369f595ee7d01d44e20ba2e8319ced66bb2509`**. The preceding closed-scope evidence SHA is **`bf0ea22deb987f8aaf4ddc41d6716c65e34ec123356489dd75bf8b3a569b64c0`**. Initial exclusion union is **906,264**; include all prior linked reservations.

Entry health/offense remains **9.4347167969 / 8.8260253906**, regeneration **1.0**, Tower SHA **`9781042377897c33360e2bff0bf85c76a610d39dcccfebe3b8f33cfc8e619a0b`**. Preserve ten level-40 characters, five unascended/unevolved Essences each, tier-1 Unique / Exceptional / rank-4 gear, fixed rolls/positions and canonical order, no styles, and hypothetical ownership. Only floor-8 health is eligible to change. Do not alter dungeon supplies, equipment, combat mechanics or acquisition assumptions.

Freeze before combat:

1. Authenticate the completed source/runtime, predecessor closeout and exclusion union. Perform zero-fight current-content preparation with native settings/catalog parity.
2. Evaluate **health × 1.05**, relative to entry content, on one fresh **67 × 128 = 8,576-fight complete-family selection panel**. Skip another small grid because the preceding scopes already supply the bracket. Require every cell at most **44/128 wins**, and at least two distinct actual per-slot Essence compositions with a cell at least **26/128**. If this fails, close without confirmation or application; there is no alternate candidate in this scope.
3. If eligible, project native time and output size for **160 samples** from that panel. Require both to fit within **80% of 840 seconds / 2 GiB**. Then run exactly one fresh **67 × 160 = 10,720-fight confirmation**. Require every approximate simultaneous 95% Bonferroni-Wilson upper bound at most 50%, and at least two distinct compositions with a lower bound at least 10%. For this frozen family and sample count, qualifying cells need **29–58 wins** and every cell must be at most **58 wins**. Standard one-composition `Pass` alone is insufficient. Do not rewrite raw recipes when counting compositions.
4. Only after acceptance, apply the exact confirmed floor-8 health locally. Require whole-Tower equality to the confirmation snapshot except for that authorized health edit; preserve offense, regeneration and every other floor. Verify all **10,720 inputs and 67 full replays**, allocating no new seeds. Run backend regressions through `build/run-tests.ps1` and reconcile all evidence. Read accepted battle-duration distributions from existing reports, with no extra fights or invented pacing threshold.

Maximum: **19,296 study fights + 67 conditional replays = 19,363 fights**, **288 fresh reservations**. Each phase remains below 20,000 fights, native 840 seconds / process 900 seconds and 2 GiB. No retries, panel extension, pooled outcomes, reused seeds, discarded recipes or fallback confirmation. Preserve every earlier rejection and all unused reservations. This is a local primary-game data calibration, without migration, application-setting change or deployment.

Frozen driver: `TestResults/tower-floor8-health-refinement-driver-20260929.py`. The current owner, compiled runtime and supported search stay unchanged. Archive this protocol before execution; all local reports and audits remain immutable under ignored `TestResults`.

## Completed selection and confirmation

The sole +5% candidate passed its **128-seed complete-family selection** with composition leaders **37, 29, 12, 2, 0, 0, 0, 0, 0 wins**. The frozen resource preflight admitted the separate 160-seed confirmation. Exactly one acceptance confirmation ran; no prior panel's observations were pooled into it.

| Composition | Best measured gear | Wins | Adjusted interval | Mean engine seconds, all outcomes |
| --- | --- | --- | --- | ---: |
| `049002…` | armor-and-health | 47/160 (29.38%) | 18.93%–42.56% | 188.09 |
| `230ac3…` | armor-and-health | 33/160 (20.62%) | 11.97%–33.18% | 188.25 |
| `32e3fc…` | armor-and-health | 17/160 (10.62%) | 4.88%–21.59% | 189.54 |
| `4dca57…` | armor-and-health | 4/160 (2.50%) | 0.54%–10.76% | 197.14 |
| `641411…` | armor-and-health | 1/160 (0.62%) | 0.05%–7.75% | 187.38 |
| `ec0f25…` | armor-and-health | 0/160 (0.00%) | 0.00%–6.63% | 178.62 |
| `92ed6e…` | armor-and-health | 0/160 (0.00%) | 0.00%–6.63% | 172.02 |
| `reference-3` | ability-haste | 0/160 (0.00%) | 0.00%–6.63% | 133.94 |
| `reference-1` | ability-haste | 0/160 (0.00%) | 0.00%–6.63% | 121.66 |

Intervals are approximate simultaneous 95% Bonferroni-Wilson intervals across the complete **67-cell family**. Qualifying cells must have a lower bound at least 10%; every cell's upper bound must be at most 50%. Only **2 cells / 2 distinct compositions** qualify. These are related poison builds using armor-and-health gear. Earlier viability claims remain tied to their earlier settings and families.

## Descriptive pacing review

The read-only pacing review authenticated **320 accepted-cell battle hashes**, reproduced counts and means, and ran no new fights or seeds. Quantiles use nearest ranks; medians use the arithmetic midpoint.

| Accepted composition | All-outcome median seconds | All-outcome p90 seconds | Victory median seconds | Victory p90 seconds |
| --- | ---: | ---: | ---: | ---: |
| `049002…` | 190.0 | 198.0 | 189.0 | 193.0 |
| `230ac3…` | 190.0 | 196.0 | 189.0 | 194.0 |

For context, the historical prior leader's all-outcome median/p90 was **194/208 seconds**. These are descriptive comparisons of different recipes and panels, not a controlled pacing treatment or a claim about real player waiting time. No pacing target was approved and no pacing mechanic changed.

## Applied content and verification

Only **floor-8 `guardianScaling.health` changed: 9.4347167969 → 9.9064526367**. Whole-Tower comparison matched the confirmation snapshot; offense, regeneration, every other guardian field and every other floor remained unchanged. All **10,720 inputs and 67 complete battle reports** matched, with **zero new application seeds**. Final Tower SHA: **`f9a2a8869b461c0f8b0fed2002a10bc6717a2081cb334c6a5b34acba635d7eb1`**.

This refinement completed **3 phases / 19,296 study fights + 67 application replays**, reserving **288 fresh values**. Native study time was **926.751 seconds**, excluding audits and application verification. All **656 immutable input/runtime pins** matched; the authorized live Tower edit was separately matched to its accepted snapshot. Final exclusion union: **906,552**. Across the search, closed coarse calibration and accepted refinement, this continuation ran **54,080 study fights + 67 replays**, reserving **1,274 fresh values**. No active study remains.

Final backend verification passed **82 tests**, with three intentional opt-in skips. The unchanged maintained owner also passed **14 Python tests** in the search scope, and the three scopes passed **30 seed-free selection/reference/budget checks** in total. The closed coarse scope reused the preceding backend receipt because no implementation or content had changed; the final backend suite was rerun after this health edit. No search algorithm or compiled combat implementation changed.

| Artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor8-health-refinement-evidence-20260929.json` | `bbe4ee2b2b928d73017d475f3735aa54ee492da21c70ef7b01dbb0104ab607f6` |
| `tower-balance-pass-floor8-health-refinement-confirmation-study-20260929/files.json` | `0218835433af0414f788d5624b8c67f05b05a6995c7aa7ce8c20cdfc4a2f9947` |
| `tower-balance-pass-floor8-health-refinement-confirmation-owner-20260929/independent-audit.json` | `9ba64bfc28eb80925527f6b2cb947ac6d3a5c6283b5b06617e3a83b77211ad4d` |
| `tower-balance-pass-floor8-health-refinement-confirmation-owner-20260929/seed-ledger.json` | `32b4020799605210b2005cb0aa84a2bdda0af38c1f6c628523597c479b1038fb` |
| `tower-floor8-health-refinement-application-owner-20260929/result.json` | `258be7d2aedd447d0c3e938900cd45a3ad315b3d58bf6b23b813f16556085699` |
| `tower-floor8-health-refinement-final-regression-20260929.trx` | `eb8ff190e6cc59352b20a967db62ecba93e0eb443c19e7b0995d8dac520b4472` |
| `tower-floor8-confirmed-pacing-review-20260929.json` | `7c9a8d577e0da780e727204735f49746063f1caa74e9593dcfeb1a82313e71ba` |

For later floor-8 work, start from this accepted **67-cell confirmation**. Preserve the search's `NoEligibleDiversityConfirmation`, coarse calibration's `NoStableCandidate`, all rejected settings and all old ledgers. Do not fall back to the old 38-cell family or reimport already retained searches. Reports and evidence under ignored `TestResults` are not included in a clean checkout.

Changed maintained files: [Tower content](../LL/src/API/API.LL/Data/world-tower/tower-floors.json), the three floor-8 reports, [current handoff](Tower-Continuation-Handoff-20260928.md), [harness README](../LL/tools/BalanceHarness/README.md), [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md), and follow-up notices in the reference-coverage, floor-1/3/7/9 and floor-7 refinement reports. Existing unrelated edits remain intact.

Completed commands used the bundled Python runtime and fresh output paths:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-tower-reference-coverage.py'
python -B -X utf8 'TestResults/tower-floor8-search-challenge-driver-20260929.py'
python -B -X utf8 'TestResults/tower-floor8-expanded-calibration-driver-20260929.py'
python -B -X utf8 'TestResults/tower-floor8-health-refinement-driver-20260929.py'
python -B -X utf8 'TestResults/tower-floor8-health-refinement-apply-20260929.py'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
python -B -X utf8 'TestResults/tower-floor8-health-refinement-collect-20260929.py'
python -B -X utf8 'TestResults/tower-floor8-confirmed-pacing-20260929.py' --evidence 'TestResults/tower-floor8-health-refinement-evidence-20260929.json' --output 'TestResults/tower-floor8-confirmed-pacing-review-20260929.json'
```

No required command remained blocked. This was a local content-data edit, without database migration, application-setting change, service startup or deployment. The value takes effect when the content is later released through the normal process. Next: **floor-12 build diversity**, then floor 13; broader archetypes and floor-8/14 pacing remain separate open work. Dungeon work and replacement supplies are not prerequisites.
