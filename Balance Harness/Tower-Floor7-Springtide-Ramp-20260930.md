# Floor 7: isolated Springtide ramp trial — 30 September 2026

**Latest floor-7 refinement (30 September):** The [two-slope Springtide refinement](Tower-Floor7-Springtide-Refinement-20260930.md) closed **`NoEligibleSpringtideRefinement`** after **30,720 fresh fights / 256 reservations**, with all **120 recipes / eight actual compositions** in both screens. At base **0.25** and slope **0.45**, the best two-character resistance setups won **16/128 and 11/128** (minimum 25), while **six recipes exceeded the ceiling** (largest upper **62.02%**). At slope **0.55**, those setups won **0/128 each**; no ceiling failed, but no limited-equipment composition qualified. **No candidate was selected, confirmed or applied.** Independent raw-outcome recount and selection checks passed; **160 fresh Python tests and two native study tests passed**. Prior **138 backend passes / four skips** were authenticated and reused. Exclusions are **920,188**. Close this ramp bracket without extension. Next is a separate, unallocated penetration/offense proposal using original live abilities: penetration factor **50**, offense factors **0.60 / 0.65 / 0.70**, preserving all recipes and ceilings. Initial-stat arithmetic supports reducing the resistance advantage, but predicts no win rate. Use the existing isolated penetration contract, measured resource admission and independent confirmation. No new proposal panel or seed is allocated; no dungeon, supply or acquisition work.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Follow the [pressure diagnostic](Tower-Floor7-Pressure-Diagnostic-20260930.md). Preserve all earlier accepted catalog changes.

## Prospective protocol

Test exactly one isolated candidate: Springtide base Power **1.0 → 0.25** and per-Abundance Power **0.20 → 0.35**, plus the accurate description. The separate `tower-springtide-ramp-v1` contract allows only this floor-7 effect and exact tradeoff. The existing v3 proportional-scaling guard remains unchanged. Reject altered targeting, timing, status identity, source/target attributes, healing, unrelated catalogs and settings.

Authenticate the preceding publication, diagnostic proposal, current catalogs, compiled runtime, source archive and exclusion ledgers. Preserve all **120 exact recipes / eight actual compositions** from the completed mixed-resistance screen, including all original controls and full-resistance loadouts. Preserve identities, positions, ordered Essences, level-40 / five-Essence budgets and Unique / Exceptional / rank-4 tier-1 equipment.

Before allocating seeds, run fresh candidate, family and application safeguards; authenticate and reuse the unchanged **138 backend passes / four intentional skips**. Execute native panels through `build/run-tests.ps1` with the verified existing runtime. No C# rebuild is required for this offline Python candidate contract.

Run one **128-seed screen**: 15,360 fresh fights. Only a passing screen admits one independent **160-seed confirmation**: 19,200 additional fights. No retries, pooling, extension, candidate selection or changes after seeing outcomes. Each phase retains the complete family. Acceptance requires at least **two distinct actual compositions with no more than eight specialized items on two characters**, each with simultaneous lower win-rate bound at least 10%, and **every recipe's upper bound at most 50%**. Use 95% Bonferroni-Wilson bounds across all 120 recipes: screen gate **25–44/128**, confirmation **30–57/160**.

For each phase, project twice the preceding measured time and archive size per fight; require projections below **672 seconds** and **80% of 2 GiB** before allocation. Native cap **840 seconds**, owner cap **900 seconds**, archive cap **2 GiB**, maximum **20,000 fights per panel**. Maximum scope: **34,560 new fights / 288 new reservations**. Start with **919,804 exclusions**. Preserve failures and all unused reservations. Read only supervisor output while native files are active.

Independently recount raw outcomes, recipe identity, equipment counts, simultaneous bounds, source/candidate delta, native receipts, resources and seed history before publication. A passing confirmation additionally requires native input/replay parity before any local application. A failed screen closes without confirmation or game changes. This scope makes no dungeon, search, supply, acquisition, migration, configuration or deployment change.

## Completed result

**The candidate failed the full-family acceptance gate and was not applied.** All **15,360 fights** completed, with **128 fresh seeds**, no retry and no technical failure. An independent collector authenticated and recounted every saved outcome, reconstructed the content delta and equipment counts, and recomputed all simultaneous confidence bounds.

Two actual compositions satisfy the limited-equipment minimum, but **41 of 120 recipes exceed the ceiling**. The strongest full-resistance recipe wins **126/128**, with adjusted upper bound **99.81%**, far above 50%. No confirmation or application verification was admitted.

All counts below are out of 128. A is `dae6cc32…`; B is `0d375ed9…`. Each row shows the strongest retained subset for that equipment count, selected by wins, remaining guardian Health and recipe ID. The old and candidate screens use independent seeds; these are separate panel comparisons, not paired seed effects or pooled acceptance evidence.

| Characters with resistance gear | Original A / B wins | Candidate A / B wins |
| ---: | ---: | ---: |
| 0 | 0 / 0 | 13 / 34 |
| 1 | 0 / 0 | 56 / 65 |
| 2 | 1 / 1 | 89 / 84 |
| 3 | 9 / 11 | 122 / 107 |
| 4 | 27 / 20 | 124 / 119 |
| 5 | 48 / 25 | 126 / 124 |

The strongest two-character sets remain slots **3+4** for A and **2+3** for B. The change opens limited-equipment routes but also makes many of those routes too easy: their leading **89/128 and 84/128** exceed the screen's **44-win ceiling**, as do the full-resistance controls. Baseline B reaches **34/128**, inside the screen gate; baseline A reaches **13/128**, below its minimum. One successful row cannot override a failed whole-family ceiling.

The measured result supports the softer opening as a useful control, but rejects this exact damage ramp. The original 0.25/0.35 proposal does not produce stronger authored damage than live until **more than five Abundance stacks**. That arithmetic did not ensure sufficient fight pressure. This study does not measure exact stack trajectories or prove that any stronger slope will pass.

## Next finite proposal

Keep the candidate base at **0.25** and test exactly two stronger per-Abundance coefficients: **0.45** and **0.55**. Their authored damage equals live at **3** and approximately **2.14** stacks respectively, preserving a softer first hit while bringing stronger damage forward. These are untested hypotheses.

Start both candidates from the original unchanged source, not the rejected candidate archive. Add a separate, explicit refinement contract and rejection tests; retain the existing v3 proportional rule and exact `tower-springtide-ramp-v1` contract unchanged. Preserve all 120 recipes and every other catalog setting. Run at most two 128-seed screens after measured resource admission. Select only among whole-family passes by highest second-best limited-composition lower bound, then smallest maximum upper bound, then lower coefficient. Independently confirm at most one selected candidate with 160 new seeds. Maximum future scope: **49,920 fights / 416 reservations**. If neither screen qualifies, close the bracket; do not append another candidate, retry, pool or extend it.

The refinement proposal has **zero allocated panels, seeds or fights**. It is not implemented, measured, accepted or applied. The current completed screen remains closed.

## Verification and scope

**136 fresh Python tests passed:** ten new Springtide safeguards, 37 existing ability-candidate checks, ten single-candidate application checks, 29 reference-coverage checks and 50 catalog-qualification checks. The new tests reject non-proportional v3 changes, wrong floor/identity/coefficients, hidden timing/target/status changes, shared ownership, unrelated catalogs and settings. The candidate helper writes only the two declared coefficients and matching description in an isolated content copy.

The native frozen-study test passed through `build/run-tests.ps1 -NoBuild`, using the pinned existing runtime. The prior **138 backend passes / four intentional skips** were authenticated and reused; the broader regression was not rerun because no C# or runtime changed. The independent collector verified all raw outcomes, recipe identity, counts, bounds, catalog delta, process receipts, resource projections and disjoint seed history.

Native elapsed time: **186.041 seconds**. Authenticated archive: **214,681,541 bytes**. **15,360 attempts / 15,360 completed fights / zero retries**. The native owner exited successfully with no timeout or remaining child process. Every declared time and size limit was met. Exclusions: **919,804 → 919,932**.

Changed maintained files: `analysis/tower-ability-candidate.py`, new `analysis/test-tower-springtide-ramp.py`, this report, preceding diagnostic notice, handoff, status, gear coverage and the two harness guides. No gameplay catalog, C#, migration, application configuration, database or deployment change. Floor 4 remains the latest applied balance change. Search, Essence progression, expected gear and acquisition assumptions are preserved. No verification command remains blocked.

Commands used the bundled Python runtime with `-B -X utf8`:

```text
TestResults/tower-floor7-springtide-ramp-prepare-20260930.py
TestResults/tower-floor7-springtide-ramp-tests-20260930.py
TestResults/tower-floor7-springtide-ramp-driver-20260930.py
TestResults/tower-floor7-springtide-ramp-collect-20260930.py
TestResults/tower-floor7-springtide-ramp-propose-refinement-20260930.py
```

The driver records the exact native command and invokes the repository test wrapper. `git diff --check` and local Markdown links were checked during publication.

## Evidence

- Closed screen: `TestResults/tower-balance-pass-floor7-springtide-ramp-screen-study-20260929`, manifest **`3d52aa25de14734590257fc55c6526e53dad825e605f9dfb0deb77a933105658`**.
- Independent audit: **`7022718928d5b4c1c430ab5ea73b11e19ce81e044c6b648ed40d23d1a8acc862`**.
- Evidence: `TestResults/tower-floor7-springtide-ramp-evidence-20260930.json`, SHA **`fb4dda75e0ef2fe8ff01211f1c1f754d735d3039694f26a35e73bf880939ce87`**.
- Candidate and frozen protocol: `TestResults/tower-floor7-springtide-ramp-driver-20260930`.
- Entry and earlier file snapshots: `TestResults/tower-floor7-springtide-ramp-entry-20260930`.
- Unallocated refinement proposal: `TestResults/tower-floor7-springtide-refinement-proposal-20260930.json`, SHA **`c9053164c5f3f50e087ba4c8d7b26c5677275da39588aa7aa33317409c8523e2`**.
- Publication: `TestResults/tower-floor7-springtide-ramp-publication-check-20260930.json`.
