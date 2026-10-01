# Floor 6: health calibration with four-piece Restoration teams — 30 September 2026

**Subsequent result:** The [floor-6 higher health bracket](Tower-Floor6-Health-Bracket-20260930.md) completed **64,032 fresh fights** across the retained **116 recipes / seven compositions**. +8%: strongest **40/128**, second four-piece **26/128**, +10%: strongest **42/128**, second four-piece **23/128**, +12%: strongest **45/128**, second four-piece **19/128**. Closed **`HealthSettingNotConfirmed`**; no game change. Latest exclusions **913,348**. Eight fresh safeguards and 4 native fixtures passed; prior 97 backend / 47 Python passes were authenticated and reused. Recommended next: one separately declared +9% midpoint using the original baseline and all retained recipes; if it fails, stop health-only sweeps and reassess the gear gap. The report below preserves the preceding +2/+4/+6% grid; its proposed higher bracket is now completed.

Target: primary LL World Tower and offline Balance Harness. Continue from the [116-recipe search screen](Tower-Floor6-Four-Piece-Search-20260930.md), preserving all seven actual compositions and every recipe. The objective is two viable teams with MainHand/Chest/Head/Necklace Restoration while keeping the full family below the established ceiling. Equipment remains hypothetical; no ordinary acquisition or pacing target is established.

## Prospective protocol

1. Authenticate source manifest **`d28d703d42572d1f21897f753998f625a1c5921208866b0a0ab894e2657f3063`**, its audit, preceding publication, qualified runtime and test receipts. Baseline floor-6 health/offense is **6.153125 / 4.365625**. Initial exclusions: **912,412**. The existing 97 backend passes / four opt-in skips and 47 Python passes are reused only after authenticating their unchanged source/runtime/catalog and receipts; do not label them fresh tests.
2. Test exactly three isolated health settings, in this order: **+2% = 6.2761875**, **+4% = 6.39925**, **+6% = 6.5223125**. Offense remains **4.365625**; every other floor, ability, mechanic, catalog, combat setting, budget and raw recipe stays fixed. No additional settings, interpolation or searches in this scope.
3. Evaluate all **116 recipes × 128 fresh seeds = 14,848 fights** per candidate, with disjoint panels across candidates and all prior reservations. Use approximate simultaneous 95% Bonferroni-Wilson bounds across the complete 116-recipe family within each exploratory screen. Require every upper bound ≤50% and at least two distinct actual compositions at `partial-restoration-4-mainhand-chest-head-necklace` with lower bounds ≥10%. This corresponds to **25–44 wins / 128**. Gear, identity and Essence-order variants do not create actual compositions. No pooling of panels.
4. Complete all three candidates before selection. Among eligible candidates, maximize the smaller of (a) **44 minus the maximum win count across every recipe** and (b) **the second-best distinct four-piece composition's win count minus 25**. Break ties by the smaller health increase. This selection rule is frozen before outcomes. If none passes, close the scope without confirmation or game changes.
5. Confirm at most one selected setting using **116 × 168 independent fresh seeds = 19,488 fights**, retaining all recipes and the identical family/diversity rules. Confirmation gates: **31–61 wins / 168**. Apply the multiplier to the original unchanged source, avoiding compounding. No retry, extension, alternate winner, second confirmation or candidate after a failed confirmation. Only this independent panel can support acceptance; the exploratory grid cannot.
6. If confirmation passes the full gate, freeze a one-field application declaration and change only floor-6 `guardianScaling.health` in the local Tower catalog, preserving formatting and unrelated edits. Run fresh backend regressions through `build/run-tests.ps1 -NoBuild` using the already authenticated, unchanged combat build. Verify **all 19,488 confirmed native inputs and 116 full replays** against the local current catalog. No deployment or database action. Preserve diagnostics on any failure; never claim application verified until these checks pass.
7. Each native study remains capped at **20,000 fights, 840 seconds, 2 GiB and a bounded 900-second owner**. Before allocation require twice the preceding measured per-fight time/storage projection below 80% of the envelope. Use only fresh output paths, reserve all seeds including unconsumed ones, preserve all failures and frozen artifacts. The three-screen total is **44,544 fights / 384 reservations**; including conditional confirmation the maximum is **64,032 fights / 552 reservations**, plus at most 116 historical-seed application replays. Backend regression combat is separate.

The supported search algorithm stays `affinity-creation-with-benchmark-validation-v1`. The scope neither retries the preceding failed screen nor reimports its nominees. The accepted 38-recipe historical floor-6 family and its current-runtime qualification remain preserved; the new 116-recipe family requires independent confirmation at its selected setting.

## Results

**Completed: `NoEligibleHealthSetting`.** None of the three declared health increases passed both gates. No confirmation or local game change followed. All 116 recipes / seven actual compositions remain retained.

| Health increase | Health multiplier | Strongest recipe | Four-piece C | Four-piece D | Ceiling / minimum |
| --- | ---: | ---: | ---: | ---: | --- |
| +2% | 6.2761875 | 67/128 | 39/128 | 39/128 | Fail / Pass |
| +4% | 6.39925 | 60/128 | 32/128 | 47/128 | Fail / Pass |
| +6% | 6.5223125 | 50/128 | 30/128 | 34/128 | Fail / Pass |

Screen limits remained **25–44 wins / 128**, with simultaneous bounds across all 116 recipes. The strongest-recipe intervals were +2%: 37.30%–66.98%; +4%: 32.32%–61.98%; +6%: 25.50%–54.56%. A setting needs both gates; lower-bound success alone is not family acceptance. Candidate panels used different fresh seeds, so fluctuations between nearby settings are not evidence that adding health improves player win rates. Do not pool them with each other or with the earlier search screen.

No confirmation or application replays were allocated.

Total **44,544 fresh study fights / 384 new reservations**. Exclusions increased from **912,412 to 912,796**. Raw recipes, identities, Essence ordering, gear/level budgets, offense and abilities were unchanged. The supported search was neither changed nor rerun.

## Verification and changed files

**Eight fresh selection/application safeguards and 3 owned native fixtures passed.** The preceding **97 backend passes / four opt-in skips and 47 Python passes** were authenticated and reused, not rerun at entry. No production code or game data changed, so no duplicate baseline regression run or rebuild was needed. The isolated runtime and all relevant source/catalog hashes matched. Every candidate outcome, whole-family interval, actual-composition count, selection margin, process receipt and seed reservation was reconstructed independently. Every native process exited with zero active children and zero retries. No verification command failed or remains blocked in this scope.

Game content and maintained harness code are unchanged. The new report, preceding search notice, handoff, balance-status notice and two harness guides were updated. The bounded driver, eight guard tests, declarations, collector and publication receipts are saved under `TestResults`. No migration, application configuration, database, deployment, dungeon or equipment-supply changes.

## Evidence and continuation

- Evidence: `TestResults/tower-floor6-health-calibration-evidence-20260930.json`, SHA **`1c8bda140af6f854827d50331b79859faf5b4d0137a252ee360712fa9b8b5941`**.
- Frozen protocol, declaration, original catalog, per-candidate assessments and resource receipts: `TestResults/tower-floor6-health-calibration-driver-20260930/`.
- +2% screen: `TestResults/tower-balance-pass-floor6-health-calibration-plus02-study-20260929`, manifest **`253a5a6b05f6e861d0057f0fde4cca3f21aa518bbde890350b3e172ee79c8686`**, audit **`a60960107f1476a85f80667091929ba843857de6b585eed0b73b95867b8316b7`**.
- +4% screen: `TestResults/tower-balance-pass-floor6-health-calibration-plus04-study-20260929`, manifest **`0365cdb4517fe63b6b387650eeb1dcd68502082e63be15dea577d8a98e1995fe`**, audit **`934e3c044701de40446bdb84737dc612da837c767fa03b501e6663d081a4323d`**.
- +6% screen: `TestResults/tower-balance-pass-floor6-health-calibration-plus06-study-20260929`, manifest **`7b4c0380ac552809994a390d31ad9f664742db45ea44b3ee63797da465f6397f`**, audit **`7bc10f9384f06e61f68eb61ef06f473faa93ec63007300c95734799dab75233d`**.
- Latest ledger: `TestResults/tower-balance-pass-floor6-health-calibration-plus06-owner-20260929/seed-ledger.json`, SHA **`1e339a07298876d1449eaec8ccf30e296abb551af2dcb6846320f62c317154a7`**; include every ancestor and later reservation.
- Runtime: `TestResults/tower-floor6-partial-restoration-build-20260930`.
- Current Tower SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**; abilities SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**.

**Next:** Declare a separate, moderately higher health bracket: all tested settings still preserve two four-piece teams above the minimum, while the strongest retained loadouts remain above the ceiling. Use the +6% results to inform the bracket, but apply future multipliers to the unchanged original baseline, not to an already scaled candidate. Freeze the new settings before combat and retain the exact 116-family gates. No further settings are declared or tested by this closed scope.

The 116-recipe search source remains the unchanged starting catalog for future candidate multipliers: `TestResults/tower-balance-pass-floor6-four-piece-expanded-screen-study-20260929`, manifest `d28d703d42572d1f21897f753998f625a1c5921208866b0a0ab894e2657f3063`. The historical accepted 38-recipe floor-6 family and its runtime qualification remain preserved; none of these new health settings is accepted or live. The latest applied gameplay change remains the floor-5 ability adjustment.

