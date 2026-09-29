# Floor 8: expanded-family health calibration — 29 September 2026

**Later floor-8 result:** the [midpoint refinement](Tower-Floor8-Health-Refinement-20260929.md) applies health **9.9064526367 (+5%)**, preserving offense and regeneration. The complete 67-cell confirmation establishes **47/160 (29.38%)** and **33/160 (20.62%)**; all 10,720 inputs and 67 full replays match. The report below retains its own historical setting and results. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for **906,552 exclusions**, current content and the floor-12 queue.

## Prospective scope

The [supported-search challenge](Tower-Floor8-Search-Challenge-20260929.md) is closed `NoEligibleDiversityConfirmation`: its separate complete-family screen found leading compositions at **91/128, 88/128 and 75/128**, exceeding the unchanged setting's ceiling. This new scope tests guardian health against every retained recipe. It does not extend the closed screen or pool its observations into acceptance.

Source: `TestResults/tower-balance-pass-floor8-search-challenge-expanded-screen-study-20260929`, manifest SHA **`bb1dcf707139cda5c881a72228369f595ee7d01d44e20ba2e8319ced66bb2509`**. Retain all **67 exact recipes / nine actual compositions**, including all 38 original cells and every exact finalist/reference/gear variant. Current Kodoku health/offense is **9.4347167969 / 8.8260253906**, regeneration **1.0**. Entry Tower SHA is **`9781042377897c33360e2bff0bf85c76a610d39dcccfebe3b8f33cfc8e619a0b`**; initial exclusion union **905,944**, with preceding ledger SHA **`6c2b330e9b4b5e8cba851cba88cb10b16200d094d7a563e9da49638ce9cfada3`**.

Keep ten level-40 characters, five unascended/unevolved Essences each, tier-1 Unique / Exceptional / rank-4 gear, fixed rolls and positions, canonical Essence order, and no styles. Ownership is hypothetical. No new search, dungeon work, replacement supplies, combat mechanics or equipment changes are part of this primary-game content calibration. Only guardian health may change after confirmation.

Freeze the sequence before combat:

1. Authenticate source/runtime and perform zero-fight current-content preparation with native settings/catalog parity.
2. Evaluate health factors **1.02, 1.04 and 1.06**, each relative to entry health, on separate **64-seed complete-family panels**. Keep all other fields fixed. Grid eligibility: every cell at most **28/64**, and at least two distinct compositions with a cell at least **12/64**.
3. Rank by number of qualifying compositions, second composition's wins, strongest rate closest to 30%, least health increase, then label. Advance at most two settings to separate **128-seed complete-family stability panels**. Require every cell at most **44/128** and at least two compositions at least **26/128**; select at most one using the same ranking. These margins accommodate the shorter confirmation without changing the final acceptance band.
4. Before confirmation, require projected native time and size from the selected stability panel to fit within **80% of 840 seconds / 2 GiB**. If admitted, run exactly one fresh **67 × 160 = 10,720-fight confirmation**. The sample count is fixed prospectively: the prior 128-seed screen took 427.59 native seconds, so 160 samples leave more room than the earlier scope's conditional 192 samples. Require every approximate simultaneous 95% Bonferroni-Wilson upper bound at most 50%, and at least two distinct actual compositions with a lower bound at least 10%. Standard one-composition `Pass` alone is insufficient. Count actual per-slot Essence sets without changing raw recipes.
5. Only upon acceptance, apply the confirmed floor-8 health value locally and require whole-Tower equality to the confirmation snapshot. Verify all **10,720 native inputs and 67 full replays**, allocating no new seeds. Run relevant backend tests through `build/run-tests.ps1` and reconcile immutable pins, accounting, family coverage and the sole authorized health edit.

Maximum: **40,736 study fights + 67 conditional replays = 40,803 fights**, **608 fresh reserved values**. Each phase remains below 20,000 fights, native 840 seconds / process 900 seconds and 2 GiB. Allocation is sequential against the complete inherited exclusion union. Stop on technical failure, resource refusal, no eligible setting or failed confirmation. No retries, seed reuse, panel extension, dropped recipe, pooled result or fallback confirmation. Preserve all rejected settings and unused reservations.

Duration is a secondary descriptive result, with no approved pacing target. Review saved battle-duration distributions without additional fights. New lineups remain poison-focused; multiple qualifying compositions do not establish broad archetype or gear diversity. No migration, application-setting change or deployment is included.

Frozen local driver: `TestResults/tower-floor8-expanded-calibration-driver-20260929.py`. The existing owner and compiled combat/search runtime remain unchanged. Native evidence, protocol snapshot and guarded application/reconciliation helpers are retained under ignored `TestResults` and must not be overwritten.

## Closed result

This scope closed **`NoStableCandidate`**, without confirmation, application replay or content change. Every panel retained all 67 exact recipes / nine compositions.

| Health increase | Grid leaders by composition, wins / 64 | Grid decision | Stability leaders, wins / 128 | Stability decision |
| --- | --- | --- | --- | --- |
| +2% | 42, 30, 24, 10, 3, 1, 0, 0, 0 | Rejected: ceiling | — | Not admitted |
| +4% | 27, 20, 20, 2, 0, 0, 0, 0, 0 | Advanced | 53, 49, 39, 10, 0, 0, 0, 0, 0 | Rejected: maximum exceeds 44 |
| +6% | 12, 7, 5, 1, 0, 0, 0, 0, 0 | Rejected: only one qualifying composition | — | Not admitted |

The selected +4% candidate did not satisfy the more conservative stability ceiling required before the shorter confirmation; +6% left too few viable choices. These outcomes bracket a narrower health range. The separately declared [midpoint refinement](Tower-Floor8-Health-Refinement-20260929.md) tests +5% on a new panel; no observations or seeds are reused.

Completed: **five phases / 21,440 fights**, **320 fresh reservations**, final exclusion union **906,264**. Native study time was **1,046.282 seconds**, excluding audits/wrapper overhead. All **719 immutable input/runtime pins** matched, all processes drained and all native phases completed without retry. The live Tower file stayed byte-identical at the entry hash; its floor-8 health remains **9.4347167969**. No confirmation seeds were reserved.

**Eleven seed-free calibration checks passed.** Since no game data or compiled implementation changed, this closeout reuses the preceding search scope's verified **82-pass backend receipt**, with three intentional skips, rather than rerunning identical regressions. That receipt's content/runtime bindings remain verified. Its 14 Python tests also remain the latest maintained-owner checks. Final regressions will run again if a later scope applies a health change.

Evidence index: `TestResults/tower-floor8-expanded-calibration-evidence-20260929.json`, SHA **`bf0ea22deb987f8aaf4ddc41d6716c65e34ec123356489dd75bf8b3a569b64c0`**. The original frozen source is still the unchanged 67-cell expanded screen, manifest **`bb1dcf707139cda5c881a72228369f595ee7d01d44e20ba2e8319ced66bb2509`**. Preserve this scope's rejection and all ledgers; do not treat any candidate snapshot as applied content.
