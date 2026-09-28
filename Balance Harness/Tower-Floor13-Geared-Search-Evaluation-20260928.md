# Floor-13 gear-aware search evaluation — 28 September 2026

**Decision: retain the existing benchmark and supported search.** With the calibrated encounter and resistance-and-health equipment, the best generated finalist won **87/128** separate held-out fights against the benchmark's **86/128**. The other finalist won **76/128**. No meaningful improvement was demonstrated, and the unchanged search validation gate correctly retained the benchmark.

This increment completed the reference check and the conditional search authorized after the [offense calibration](Tower-Gear-Offense-Calibration-20260928.md): **192 historical-seed reference fights, then 528 search fights and 640 held-out fights**, or **1,360 fights total**. Both phases passed native verification and independent readback. No additional confirmation or tuning run follows from this one-win difference.

## Fixed context and reference admission

Floor 13 uses captured guardian offense **4.20**, ten characters, seven level-1 unascended/unevolved Essences each, level 60, Uncommon Fine tier-2/rank-3 equipment, baseline rolls, no styles, hypothetical ownership and no Tower contributions. The resistance-and-health profile is applied consistently to all three retained references and the search's character templates. Attribute rules **18**, equipment release **4** and **healing-v1** remain captured. All five combat assembly hashes match the prior confirmation/calibration runtime.

The three compositions come from the original frozen progression screen. Its later follow-up export contained only floors 3 and 15; the first launch detected that omission before creating a study or running any fights. That failed owner package is retained. The corrected owner authenticates the original six-floor freeze and its three floor-13 scenario files directly; it does not select different recipes after observing results.

The supported search uses canonical Essence-set encoding and common character templates. The screen therefore measured both the retained recipes with their original identities/order and the exact projected references that the search would execute. Projection preserves the composition and equipment budget, is stable when applied again, and carries no transferred strength claim. The observed difference between representations is not an isolated test of Essence order.

| Reference | Retained recipe wins / 32 | Projected search recipe wins / 32 |
| --- | ---: | ---: |
| 1 — calibrated benchmark | 22 | 23 |
| 2 — retained control | 0 | 0 |
| 3 — retained control | 0 | 0 |

All six cells used the same 32 historical seeds. The rule was frozen before combat: reject the context if **any** retained or projected reference exceeds 28 wins; otherwise select the highest-win projected reference, breaking ties by its original reference number, and require at least four wins. Reference 1 passed. All 32 original benchmark inputs and complete reports matched the calibration archive exactly.

This gate checks the three retained reference compositions, not every possible strong team. Neither the historical reference screen nor its combination with earlier screens is fresh confirmation.

## Search and separate held-out results

The search remains `affinity-creation-with-benchmark-validation-v1`: the original affinity proposer, racing policy and benchmark-validation gate, with **528 fights**. One independently allocated generation root supplies the experiment. All generated parties use the same resistance-and-health equipment and seven-Essence budget.

The five nominees were frozen before a separate panel of **128 fresh paired seeds each**. These results never alter the search's selected output:

| Team | Wins / 128 | Clear rate | Gained / lost wins against benchmark |
| --- | ---: | ---: | ---: |
| Generated finalist `607ee60199d9…` | 87 | 67.97% | 26 / 25 |
| Generated finalist `2eb36217b645…` | 76 | 59.38% | 24 / 34 |
| **Benchmark/reference 1 `dc7744250965…`** | **86** | **67.19%** | — |
| Reference 2 `43c5430799d0…` | 0 | 0% | 0 / 86 |
| Reference 3 `176bdc099797…` | 0 | 0% | 0 / 86 |

The best generated finalist changes **one Essence on character slot 8**: `essence.undead` becomes `essence.venomous_spiderling`. The other finalist replaces `essence.flame_imp` and `essence.ravenous_ghoul` on slot 4 with `essence.spider_queen_royal_venom` and `essence.viper`. These are actual composition changes. Exact parties are retained in [heldout-freeze.json](../TestResults/balance/tower-floor13-geared-search-20260928/heldout-freeze.json).

The search nominated the first finalist for its own 60-seed validation panel. It won **46/60** versus **38/60** for the benchmark, with **16 gained and 8 lost wins**. The exact one-sided paired-binomial p-value is **0.075794816**, above the unchanged 0.05 gate. The decision was `BenchmarkRetained`. Its later held-out advantage was just **one win, or 0.78 percentage points**, with nearly balanced discordant pairs. This does not warrant a follow-up confirmation campaign or a changed selection threshold.

## Recommendation

Close this bounded evaluation. Keep reference 1 as the benchmark for this diagnostic encounter and keep the supported search policy. The run shows that the proposer can produce a different composition with a similar observed clear rate, but it does not establish equivalence, superiority, restart reliability or global optimality.

The useful progress is a working gear-aware evaluation at a difficulty with room for improvement, with strong controls and an unchanged promotion gate. Preserve this case for future regression/comparison work rather than extending the current sampling run. Before gameplay tuning, the intended per-floor player budgets still need to be settled. The roughly 67% benchmark result exceeds the gameplay policy's 50% ceiling; this deliberately diagnostic offense setting is not a production balance recommendation.

## Implementation and verification

Target: the primary game's offline Balance Harness tests and analysis tools.

- Added `LL/tests/EssenceSystem.Tests/BalanceHarnessGearReferenceTests.cs`: fixed retained/projected reference screen, preflight and projection-stability checks, ceiling/viability gate, bounded execution and native reconstruction.
- Added `Balance Harness/analysis/screen-tower-geared-references.py`: authenticates original reference recipes and calibrated content, owns the 192-fight process and independently audits its complete results.
- Extended `BalanceHarnessAffinityFloorEvaluationTests.cs` and `run-affinity-floor-evaluation.py` for floor 13 using the authenticated reference screen's captured content. Other floors retain their existing content paths. The floor-13 path requires the completed six-cell gate, an external archive manifest pin and the latest history ledger; it cannot silently substitute production content or a different benchmark.
- Added `Balance Harness/analysis/verify-floor13-geared-search.py`: authenticates the complete search archive, reproduces all reservation derivations, audits all raw outcomes, recomputes the exact validation gate and held-out comparisons, and checks unchanged policy, gear and calibrated content.
- Added this report and updated the calibration report and `LL/tools/BalanceHarness/AFFINITY-SEARCH.md`.

The pre-search backend suite passed **14 checks**, with both scientific opt-ins skipped. A final compatibility check preserves omission of the optional content-root field in existing requests; the final suite passed **15 checks**, with the same two opt-ins skipped. That serialization-only follow-up does not change the calibrated request, which supplies a non-null content root, and ran no additional combat. The actual reference run passed **all six fixture checks**; the actual search run passed **all five evaluation checks**. Tests run through `build/run-tests.ps1`; the build used approved NuGet access and completed with existing warnings. Python command/syntax checks, source/link checks and `git diff --check` passed. No verification remains blocked.

Native search verification reconstructed the complete 528-fight search and all 640 held-out inputs and report bindings. Independent reference readback authenticated **308 files**, all **192 reports**, and exact reproduction of **32 calibration inputs/reports**. Independent search readback authenticated **1,383 files**, all **1,168 reports**, all 237 allocation values, the validation result and all held-out paired comparisons. Both audits ran zero fights.

This increment changes no gameplay or search-policy source. During the final compatibility build, separate Colosseum edits appeared in the shared checkout (`ColosseumService.cs` and `ColosseumBattleTests.cs`), and the rebuilt `Services.LL`/`BalanceHarness` hashes changed. Both completed studies' **captured executables still match all five original combat hashes**, as independently rechecked; their evidence is unaffected. The final shared build is no longer the original experiment runtime and must be qualified before another scientific run. Those separate edits were left untouched.

No migrations, production configuration edits, deployments or shared-database operations were made by this increment. Other existing working-tree changes, including the user's attribute tooltip edits, are preserved.

## Accounting and evidence

The reference owner completed in **17.39 seconds**. The search experiment took **82.50 seconds**, and its owner took **86.44 seconds**. Both owners drained all eight processes. The sealed search archive occupies **156,613,445 bytes**; each phase stayed within its 840-second fixture, 900-second owner and 1-GiB limits.

Exactly **237 fresh values** were allocated: one generation root, 108 search seeds and 128 held-out seeds. They were disjoint from **834,058** historical exclusions, with zero collisions/rejections. The latest exclusion union is now **834,295**. Master `2026092813`, domain `affinity-floor13-baseline-evaluation-v1`. The complete registry was checked before and after execution. No combat retries, seed reuse between search/held-out panels, or data-dependent extensions occurred.

Reference study: `TestResults/tower-geared-reference-screen-final-20260928/`.
Reference owner/audit: `TestResults/tower-geared-reference-owner-final-20260928/`.
Reference manifest SHA-256: `eaeeeabeb78af89fb242419179c2dd17c147634c36f10e119babae4adef15abc`.
Reference result SHA-256: `a5497fdd17328632a6171109e2d8ff0a696db7973e307419614e2b384dbe7e13`.

Search study/latest ledger: `TestResults/balance/tower-floor13-geared-search-20260928/`.
Search owner/audit: `TestResults/tower-floor13-geared-search-owner-20260928/`.
Search manifest SHA-256: `8d8c049a5e284474613964f24174b753ed89a9d06454b6efcfd04e2e611c230e`.
Search result SHA-256: `e07f6f487905fb405770c224c4261dc4e4f08a007514dfd09eda95a0cd761a41`.

The initial setup-only failure is retained in `TestResults/tower-geared-reference-owner-20260928/`; its requested study directory was never created. Zero fights and zero allocations occurred in that launch.

Executed commands, with Python referring to the bundled runtime:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-gear-profiles-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessAffinitySearchTests|FullyQualifiedName~BalanceHarnessGearReferenceTests'
python -B -X utf8 'Balance Harness/analysis/screen-tower-geared-references.py' --package TestResults/tower-geared-reference-owner-final-20260928 --output TestResults/tower-geared-reference-screen-final-20260928 --artifacts TestResults/tower-gear-profiles-build-20260928
python -B -X utf8 'Balance Harness/analysis/run-affinity-floor-evaluation.py' --package TestResults/tower-floor13-geared-search-owner-20260928 --output TestResults/balance/tower-floor13-geared-search-20260928 --artifacts TestResults/tower-gear-profiles-build-20260928 --floor 13 --benchmark-reference 1 --screen TestResults/tower-geared-reference-screen-final-20260928/result.json --screen-manifest-pin eaeeeabeb78af89fb242419179c2dd17c147634c36f10e119babae4adef15abc --master 2026092813 --history TestResults/balance/tower-floor13-gear-confirmation-20260928/seed-ledger.json
python -B -X utf8 'Balance Harness/analysis/verify-floor13-geared-search.py' --owner TestResults/tower-floor13-geared-search-owner-20260928 --manifest-pin 8d8c049a5e284474613964f24174b753ed89a9d06454b6efcfd04e2e611c230e --receipt TestResults/tower-floor13-geared-search-owner-20260928/independent-readback.json
git diff --check
```

Execution requires new output paths and the retained local archives. Reusing completed paths is rejected; audit repetition requires a new receipt path. These commands document the completed experiments rather than authorizing another allocation.
