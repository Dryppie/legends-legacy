# Floor 7: bounded Springtide refinement — 30 September 2026

**Latest floor-7 penetration trial (30 September):** The [penetration/Power trial](Tower-Floor7-Penetration-Pressure-20260930.md) closed **`NoEligiblePenetrationPressure`** after **46,080 fresh fights / 384 reservations**. All three candidates retained **120 recipes / eight actual compositions**, original abilities and effective penetration **40**. At offense **0.60**, the strongest limited setups specialize one healer and win **12/128 and 18/128** (minimum 25). Both ceiling failures use **Health + Regeneration: 62/128 and 66/128**; full resistance wins only **10/128 and 14/128**. At **0.65 and 0.70**, no ceiling failed but no limited-equipment composition qualified. **No confirmation or gameplay edit.** All outcomes and prepared participants were independently audited; **101 fresh Python checks and three native study tests passed**. Unchanged **138 backend passes / four skips** were authenticated and reused. Exclusions: **920,572**. Next: the frozen, unallocated **96-historical-replay residual-pressure diagnostic** of the rejected 0.60 candidate, using baseline, one-healer specialization and full Health + Regeneration for A/B. Its purpose is to explain the remaining survival/output gap before proposing another balance change. No fresh acceptance fights or seeds; no extension, fourth scalar, dungeon or acquisition work. Floor 4 remains the latest applied change.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Follow the [closed Springtide ramp trial](Tower-Floor7-Springtide-Ramp-20260930.md); preserve every accepted catalog change.

## Prospective protocol

Test exactly two candidates, both starting from the original unchanged mixed-resistance source: Springtide base Power **1.0 → 0.25** with per-Abundance Power **0.20 → 0.45**, then **0.20 → 0.55**. Update the description to match. A separate `tower-springtide-refinement-v1` contract permits only these exact floor-7 changes. Preserve v3's proportional guard and the original ramp contract's exact 0.35 restriction. Preserve all other ability fields, guardian scaling, catalogs and settings.

Authenticate the latest publication, immutable proposal, original 120-recipe source, prior measured screen, live catalogs, runtime and exclusion ledgers. Retain all **120 exact recipes / eight actual compositions**, identities, positions, ordered Essences, level-40 / five-Essence budgets and Unique / Exceptional / rank-4 tier-1 equipment. No recipe selection, substitution or family reduction.

Run fresh refinement, original candidate, family and application safeguards. Authenticate and reuse unchanged **138 backend passes / four intentional skips**. Native panels run through `build/run-tests.ps1 -NoBuild` using the pinned existing runtime. No C# rebuild is needed for this Python-only candidate contract.

Run two independent **128-seed screens** (**15,360 fights each**). Every panel uses new seeds excluded from all historical reservations. A screen passes only if two distinct actual compositions with **at most eight specialized items on two characters** have simultaneous lower win-rate bounds at least 10%, while **every recipe's upper bound is at most 50%**. Use 95% Bonferroni-Wilson bounds over all 120 recipes: screen gate **25–44/128**. Both screens must complete before selection.

Among whole-family passing screens, choose highest second-best limited-composition lower bound, then lowest family maximum upper bound, then lower per-Abundance coefficient. Independently confirm at most one candidate with **160 new seeds** (**19,200 fights**), gate **30–57/160**, with identical complete-family acceptance. No pooling of screens or reuse of screening outcomes as confirmation. Maximum future scope: **49,920 fights / 416 reservations**. Start with **919,932 exclusions**. If neither screen qualifies, close this bracket without another candidate or confirmation.

Before each allocation, project twice the preceding measured time and archive size per fight. Require projected time below **672 seconds**, projected size below **80% of 2 GiB**, and at most **20,000 fights per panel**. Native cap **840 seconds**, owner cap **900 seconds**, archive cap **2 GiB**. Preserve failures and unused reservations; no retries or extensions. Monitor only supervisor output while native files are active.

Independently recount raw outcomes, equipment limits, simultaneous bounds, deterministic selection, candidate deltas, process receipts, resource projections and disjoint seed history. A passing confirmation additionally requires native input/full-replay parity before local application and relevant regression checks after application. No application follows a failed screen or confirmation. No dungeon, search, acquisition, supply, migration, configuration or deployment work.

## Completed result

**Neither declared refinement passed; the bracket is closed with no gameplay change.** Both complete 128-seed screens finished without retries or technical failure. The independent collector recounted all **30,720 raw outcomes** and checked catalog deltas, recipe identity, equipment counts, simultaneous bounds, the deterministic selection rule, process receipts, resources and disjoint seed history.

| Per-Abundance slope, base 0.25 | Qualifying limited-equipment compositions | Recipes exceeding the ceiling | Largest adjusted upper bound |
| ---: | ---: | ---: | ---: |
| 0.45 | 0 | 6 | 62.02% |
| 0.55 | 0 | 0 | 22.38% |

The 0.45 candidate is still too hard for limited-equipment parties while some better-equipped parties remain too strong. The 0.55 candidate suppresses wins across the family; even the strongest retained recipe reaches only **12/128**, below the **25-win minimum**. Neither is eligible for selection, so no confirmation or application verification was allocated.

All following counts are out of 128. A is `dae6cc32…`; B is `0d375ed9…`. The table shows the strongest retained subset at each gear count, selected by wins, remaining guardian Health and recipe ID. Each candidate uses independent seeds; these are separate screening comparisons, not paired effects, pooled evidence or a claim of monotonic outcomes.

| Characters with resistance gear | Previous 0.35 slope, A / B | 0.45 slope, A / B | 0.55 slope, A / B |
| ---: | ---: | ---: | ---: |
| 0 | 13 / 34 | 0 / 0 | 0 / 0 |
| 1 | 56 / 65 | 5 / 4 | 0 / 0 |
| 2 | 89 / 84 | 16 / 11 | 0 / 0 |
| 3 | 122 / 107 | 47 / 34 | 2 / 1 |
| 4 | 124 / 119 | 53 / 49 | 7 / 3 |
| 5 | 126 / 124 | 48 / 60 | 5 / 12 |

At 0.45, the leading two-character setup uses slots **2+3** for both compositions; at 0.55, the tie-break leaders use **3+4** for A and **2+3** for B. Every alternative remains in the family. The earlier 0.35 trial provided limited-equipment wins but failed 41 ceilings; these stronger slopes did not resolve that tradeoff. This rejects the declared points, not every possible ability-only solution. Do not append another slope to this completed experiment.

## Proposed next direction: reduce the resistance advantage

Use the original live Springtide coefficients **1.0 base / 0.20 per Abundance**. Test a separate guardian-scaling hypothesis: **penetration factor 50**, with exactly three reduced offense factors **0.60, 0.65 and 0.70**. Preserve Health, durability, regeneration, all ability definitions, other floors, progression and the entire equipment family. The existing isolated penetration/offense contract can represent this change; no new mechanic is needed.

The native saved guardian starts at **0.8640001 Magic Penetration**. A factor of 50 exceeds the current **40-percentage-point cap**, so the effective proposed value is 40. Current rules subtract penetration after the defense curve. At the recorded light-character initial stats, this reduces net magical mitigation from **22.54% → 0%** in baseline gear and **54.02% → 14.88%** in full resistance gear. With offense factor **0.65**, the resulting unblocked magical-damage ratios are approximately **0.839** and **1.203**: **16.1% less** baseline damage but **20.3% more** against full resistance.

This is **initial-stat arithmetic, not a native candidate run or win-rate prediction**. Equipment also changes Health, Tenacity and regeneration. Dynamic attributes, Corrosion, crits, rounding, barriers, block, general damage reduction, healing and fight duration are not modeled by those ratios. The guardian scaling field affects **both physical and magical penetration**, so retain all physical/basic-attack and high-armor controls. Native prepared Power and penetration must be checked when the proposal is implemented.

The finite future proposal allows three 128-seed screens and at most one independent 160-seed confirmation, with unchanged full-family acceptance and deterministic selection by second-best limited-composition lower bound, maximum upper bound and offense factor. Maximum future scope: **65,280 fights / 544 reservations**. Fresh measured resource admission precedes allocation. No retry, extension, pooling or fourth candidate. **The proposal is not implemented, measured, accepted or applied; zero panels or seeds are allocated.**

## Verification and scope

**160 fresh Python tests passed:** 24 refinement checks covering both slopes, ten original-ramp checks, 37 existing ability-candidate checks, ten single-candidate application checks, 29 reference-coverage checks and 50 catalog-qualification checks. The new tests reuse the complete isolation safeguards for both candidates and additionally reject undeclared slopes, mismatched descriptions and attempts to use the old exact contract. V3 remains proportional; the original ramp contract still permits only 0.35.

Both native frozen-study tests passed through `build/run-tests.ps1 -NoBuild`, using the pinned existing runtime. The previous **138 backend passes / four intentional skips** were authenticated and reused; the broader regression was not rerun because no C# or compiled runtime changed. The independent collector reproduced both whole-family failures and the decision to select nothing.

| Screen | Native seconds | Authenticated archive bytes | Attempts / completed |
| --- | ---: | ---: | ---: |
| 0.45 | 182.637 | 213,003,548 | 15,360 / 15,360 |
| 0.55 | 178.382 | 211,541,697 | 15,360 / 15,360 |

All time and size limits passed. Both owned native processes exited successfully with no timeout or remaining child process. **Zero retries.** Exclusions: **919,932 → 920,188**; all 256 new reservations remain recorded. The mitigation review added **zero fights, replays or seeds**.

Changed maintained files: `analysis/tower-ability-candidate.py`, new `analysis/test-tower-springtide-refinement.py`, this report, preceding ramp report's notice, handoff, status, gear coverage and the two harness guides. No gameplay catalog, C#, migration, configuration, database or deployment change. Floor 4 remains the latest applied balance change. Search, expected gear, Essence budgets and acquisition assumptions are preserved. No verification command remains blocked.

Commands used the bundled Python runtime with `-B -X utf8`:

```text
TestResults/tower-floor7-springtide-refinement-prepare-20260930.py
TestResults/tower-floor7-springtide-refinement-tests-20260930.py
TestResults/tower-floor7-springtide-refinement-driver-20260930.py
TestResults/tower-floor7-springtide-refinement-collect-20260930.py
TestResults/tower-floor7-resistance-pressure-review-20260930.py
```

Exact native argument arrays and test receipts are retained in the driver archive. Publication verifies local Markdown links and `git diff --check`.

## Evidence

- Screen 0.45: `TestResults/tower-balance-pass-floor7-springtide-refinement-1-screen-study-20260929`, manifest **`fa44e93c58145d7f165756187685b7b8911a2d3c5652fd6d1ad3b6b14a5fb964`**.
- Screen 0.55: `TestResults/tower-balance-pass-floor7-springtide-refinement-2-screen-study-20260929`, manifest **`ab4bbfa086480754b8811b0cee02a21d93c0db3122d32609486706346e221314`**.
- Independent evidence: `TestResults/tower-floor7-springtide-refinement-evidence-20260930.json`, SHA **`ec48ca1ba85a4941b418a9ed15c8d79c0a9dd2b02aba07d1721b933bb6d9dea4`**.
- Initial-stat review: `TestResults/tower-floor7-resistance-pressure-review-20260930.json`, SHA **`24cc091cc06f3a08f6fc8b6cf51a63aec32db736df58fc2f6dc433022de8cd1b`**.
- Unallocated proposal: `TestResults/tower-floor7-penetration-pressure-proposal-20260930.json`, SHA **`2ce5893bc13e0334bd078f7824315bb7bc673c70c4f4b4c324760b3ba2779ebe`**.
- Frozen plans, selection, protocol, commands and receipts: `TestResults/tower-floor7-springtide-refinement-driver-20260930`.
- Entry and prior file snapshots: `TestResults/tower-floor7-springtide-refinement-entry-20260930`.
- Publication: `TestResults/tower-floor7-springtide-refinement-publication-check-20260930.json`.
