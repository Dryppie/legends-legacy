# Floor 6: single +9% health midpoint — 30 September 2026

**Subsequent review:** The [gear-coverage review](Tower-Gear-Coverage-20260930.md) authenticates all fifteen latest accepted families: every floor has at least two viable compositions, while **10/15 floors** still qualify with only one gear profile. **Floor 2 is next**: armor leads at 74/256 and ability haste at 11/256. A **98-recipe mixed-armor proposal** retains all 38 originals and adds 60 exact subsets across the two leading compositions. The target is viability with armor on at most two of five characters. Zero new fights or seeds; six fresh Python safeguards pass, and the latest 97 backend passes were authenticated and reused. Floor-6 health remains **6.70690625**; all game data is unchanged by this review. The accepted floor-6 result below remains unchanged.

Target: primary LL World Tower and offline Balance Harness. Continue the [higher health bracket](Tower-Floor6-Health-Bracket-20260930.md). The +8% independent confirmation retained two viable four-piece Restoration compositions at 38/168 and 45/168, but its leading full-Restorer recipe reached 62/168: adjusted upper bound 50.48%, just above the 50% limit. The +10% screen failed the second-composition minimum. A midpoint is plausible, not established as balanced.

## Prospective protocol

1. Authenticate the preceding evidence SHA `e96f6128fda5bab0ef72b1fece56c9da2421d155c86cfe53b50899ed09e9383a`, publication SHA `b1a583c490c04f83303186c6b6fbb3bff8cf217ce44c97ca1648f8a8d61acd27`, original 116-recipe source manifest `d28d703d42572d1f21897f753998f625a1c5921208866b0a0ab894e2657f3063`, all current catalogs, qualified runtime and earlier regression receipts. Initial seed exclusion union: **913,348**. Reuse the unchanged 97 backend passes / four opt-in skips and 47 Python passes only after authenticating their sources and receipts; do not call them fresh tests.
2. Test exactly **+9% health = 6.70690625**, calculated from the original **6.153125** baseline. Offense remains **4.365625**. Use the original [116-recipe search source](Tower-Floor6-Four-Piece-Search-20260930.md), retaining all seven actual compositions, every gear variant and full-Restorer control, exact IDs, party positions and raw Essence ordering. No other floor, ability, combat setting, gear or level budget changes. Do not compound the multiplier onto an earlier candidate.
3. Run **116 × 128 fresh seeds = 14,848 screen fights**. Apply approximate simultaneous 95% Bonferroni-Wilson bounds over all 116 recipes. Require every recipe's upper bound ≤50% and at least two distinct actual compositions at `partial-restoration-4-mainhand-chest-head-necklace` with lower bounds ≥10%. Integer screen gates: **25–44 wins / 128**. Count composition by each party slot's Essence set; gear, identity and ordering variants do not increase the count.
4. Only if the full screen passes, confirm this one setting with **116 × 168 independent fresh seeds = 19,488 fights**, identical family and gates. Integer confirmation gates: **31–61 wins / 168**. No pooling, retries, extensions, extra settings, alternate winners or second confirmation. Every reservation stays excluded, including unconsumed seeds. Preserve all preceding failures. A passing screen alone cannot support application.
5. If independent confirmation passes, declare and apply only floor-6 `guardianScaling.health` in the local Tower catalog, preserving all other bytes. Run fresh regressions through `build/run-tests.ps1 -NoBuild` on the authenticated combat build. Check **19,488 native inputs and 116 full replays** against the current local catalog. Claim verified application only after both checks pass. No deployment, database or migration action.
6. Each native study is capped at **20,000 fights, 840 seconds and 2 GiB**, under a bounded 900-second owner. Before allocating a phase, twice the measured per-fight time/storage must fit below 80% of the envelope. Use the authenticated preceding +8% confirmation for the first resource projection; it is not the scaling source. Maximum new study work: **34,336 fights / 296 reservations**, plus at most 116 historical-seed application replays. The screen-only total is 14,848 fights / 128 reservations. Use fresh output paths and freeze this protocol and the driver before combat.
7. **Stop health-only sweeps if either gate fails.** Close this midpoint with no gameplay edit, then inspect the retained evidence to identify the gear-dependent gap and a finite diagnostic question. Do not declare another scalar candidate, rerun the failed +8% confirmation or weaken the acceptance rules. Mechanism claims require suitable observations; aggregate whole-fight summaries alone do not establish causality.

The supported search remains `affinity-creation-with-benchmark-validation-v1`. The user's repeating expected progression gear curve, retained stronger owned gear, and withdrawal of guaranteed selectable supplies stay unchanged. Inventories remain hypothetical; ordinary acquisition, broad archetype coverage and a pacing target are not established.

## Results

**Applied locally and verified.** Floor-6 health changed **6.153125 → 6.70690625**. The independently confirmed 116-recipe family preserves two qualifying four-piece compositions and the whole-family ceiling. Across all retained gear variants, **5 recipes / 3 actual compositions / 2 gear profiles** qualify. All **19,488 inputs and 116 full replays** matched the applied catalog; **97 fresh backend tests passed / four opt-in skips**.

| Health increase | Health multiplier | Strongest recipe | Four-piece C | Four-piece D | Ceiling / minimum |
| --- | ---: | ---: | ---: | ---: | --- |
| +9% | 6.70690625 | 40/128 | 31/128 | 27/128 | Pass / Pass |

Screen limits remained **25–44 wins / 128**, with simultaneous bounds across all 116 recipes. The strongest-recipe intervals were +9%: 19.03%–46.78%. A setting needs both gates; lower-bound success alone is not family acceptance. This screen uses a fresh panel, so differences from prior settings also include sampling variation. Do not pool this result with prior screens or its independent confirmation.

Selected **+9%**. Independent confirmation: strongest **57/168**, four-piece C **41/168**, D **35/168**; complete gate passed. Application matched all 19,488 inputs and 116 replays.

| Confirmation recipe | Wins | Observed rate | Adjusted interval |
| --- | ---: | ---: | --- |
| Strongest retained recipe | 57/168 | 33.93% | 22.57%–47.49% |
| Four-piece C | 41/168 | 24.40% | 14.77%–37.56% |
| Four-piece D | 35/168 | 20.83% | 12.01%–33.67% |

The adjusted interval applies to the full 116-recipe confirmation family. The two qualifying four-piece teams are distinct Essence compositions but remain related poison builds; equipment ownership and broader archetype coverage are not established.

Total **34,336 fresh study fights / 296 new reservations**. Exclusions increased from **913,348 to 913,644**. Raw recipes, identities, Essence ordering, gear/level budgets, offense and abilities were unchanged. The supported search was neither changed nor rerun.

## Verification and changed files

**Ten fresh selection/application safeguards and 3 owned native fixtures passed.** The preceding **97 backend passes / four opt-in skips and 47 Python passes** were authenticated and reused, not rerun at entry. After the local catalog edit, **97 fresh backend tests passed / four opt-in skips** through `build/run-tests.ps1 -NoBuild`; application parity also passed. The isolated runtime and all relevant source/catalog hashes matched. Every candidate outcome, whole-family interval, actual-composition count, selection margin, process receipt and seed reservation was reconstructed independently. Every native process exited with zero active children and zero retries. No verification command failed or remains blocked in this scope.

The sole game-data change is `LL/src/API/API.LL/Data/world-tower/tower-floors.json`, floor-6 `guardianScaling.health`. The new report, preceding calibration notice, handoff, balance-status notice and two harness guides were updated. The bounded driver, ten guard tests, declarations, collector and publication receipts are saved under `TestResults`. No migration, application configuration, database, deployment, dungeon or equipment-supply changes.

## Evidence and continuation

- Evidence: `TestResults/tower-floor6-health-midpoint-evidence-20260930.json`, SHA **`cf126932b821b364c33c0e477288514f113e5f728025e20fe1776eea21025f66`**.
- Frozen protocol, declaration, original catalog, per-candidate assessments and resource receipts: `TestResults/tower-floor6-health-midpoint-driver-20260930/`.
- +9% screen: `TestResults/tower-balance-pass-floor6-health-midpoint-plus09-study-20260929`, manifest **`3b5b1e28c1ad4ed1d599ff5fb856eb5be0e60940acddae051a03e6dd990060a1`**, audit **`1aaaec7a77dcc3ec68ef5e9734a0bec830a6d6d3c1cd9afe110145e57a603bc7`**.
- +9% confirmation: `TestResults/tower-balance-pass-floor6-health-midpoint-confirmation-study-20260929`, manifest **`780c2a16362b8e6107fee8d7e639186d5ca157720e35ce53c9922dda73c1d71e`**, audit **`28eaaf6fa03335e99503cb188d013ae08ec6582d96e07ced0485907c19de9b56`**.
- Latest ledger: `TestResults/tower-balance-pass-floor6-health-midpoint-confirmation-owner-20260929/seed-ledger.json`, SHA **`42b7439b355de511dfe86c889e3b7b8a7b790a24a832be03b948c3a2d7a6cb40`**; include every ancestor and later reservation.
- Runtime: `TestResults/tower-floor6-partial-restoration-build-20260930`.
- Current Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**; abilities SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**.

**Next:** Review the remaining floors for comparable gear dependence using their retained complete families, qualifying any older catalog/runtime before new claims. Floor-6 calibration is closed at the confirmed setting; do not keep tuning a passing family.

The independently confirmed 116-recipe floor-6 family now supersedes the earlier accepted 38-cell baseline at the applied health setting.
