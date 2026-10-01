# Floor 6: higher health bracket with four-piece Restoration teams — 30 September 2026

**Subsequent result:** The [floor-6 health midpoint](Tower-Floor6-Health-Midpoint-20260930.md) completed **34,336 fresh fights** across the retained **116 recipes / seven compositions**. +9%: strongest **40/128**, second four-piece **27/128**. Applied health **6.70690625** after independent confirmation and full local parity. Latest exclusions **913,644**. Ten fresh safeguards and 3 native fixtures passed; prior 97 backend / 47 Python passes were authenticated and reused. Fresh applied regressions: 97 passes / four skips. Floor-6 calibration is complete at the verified setting. The report below preserves the preceding +8/+10/+12% grid and failed +8% confirmation; its proposed midpoint is now completed.

Target: primary LL World Tower and offline Balance Harness. Continue from the completed [+2/+4/+6% health grid](Tower-Floor6-Health-Calibration-20260930.md) using the original [116-recipe search source](Tower-Floor6-Four-Piece-Search-20260930.md), preserving all seven actual compositions and every recipe. The objective is two viable teams with MainHand/Chest/Head/Necklace Restoration while keeping the full family below the established ceiling. Equipment remains hypothetical; no ordinary acquisition or pacing target is established.

## Prospective protocol

1. Authenticate source manifest **`d28d703d42572d1f21897f753998f625a1c5921208866b0a0ab894e2657f3063`**, its audit, preceding publication, qualified runtime and test receipts. Baseline floor-6 health/offense is **6.153125 / 4.365625**. Initial exclusions: **912,796**. The existing 97 backend passes / four opt-in skips and 47 Python passes are reused only after authenticating their unchanged source/runtime/catalog and receipts; do not label them fresh tests.
2. Test exactly three isolated health settings, in this order: **+8% = 6.645375**, **+10% = 6.7684375**, **+12% = 6.8915**. Offense remains **4.365625**; every other floor, ability, mechanic, catalog, combat setting, budget and raw recipe stays fixed. No additional settings, interpolation or searches in this scope.
3. Evaluate all **116 recipes × 128 fresh seeds = 14,848 fights** per candidate, with disjoint panels across candidates and all prior reservations. Use approximate simultaneous 95% Bonferroni-Wilson bounds across the complete 116-recipe family within each exploratory screen. Require every upper bound ≤50% and at least two distinct actual compositions at `partial-restoration-4-mainhand-chest-head-necklace` with lower bounds ≥10%. This corresponds to **25–44 wins / 128**. Gear, identity and Essence-order variants do not create actual compositions. No pooling of panels.
4. Complete all three candidates before selection. Among eligible candidates, maximize the smaller of (a) **44 minus the maximum win count across every recipe** and (b) **the second-best distinct four-piece composition's win count minus 25**. Break ties by the smaller health increase. This selection rule is frozen before outcomes. If none passes, close the scope without confirmation or game changes.
5. Confirm at most one selected setting using **116 × 168 independent fresh seeds = 19,488 fights**, retaining all recipes and the identical family/diversity rules. Confirmation gates: **31–61 wins / 168**. Apply the multiplier to the original unchanged source, avoiding compounding. No retry, extension, alternate winner, second confirmation or candidate after a failed confirmation. Only this independent panel can support acceptance; the exploratory grid cannot.
6. If confirmation passes the full gate, freeze a one-field application declaration and change only floor-6 `guardianScaling.health` in the local Tower catalog, preserving formatting and unrelated edits. Run fresh backend regressions through `build/run-tests.ps1 -NoBuild` using the already authenticated, unchanged combat build. Verify **all 19,488 confirmed native inputs and 116 full replays** against the local current catalog. No deployment or database action. Preserve diagnostics on any failure; never claim application verified until these checks pass.
7. Each native study remains capped at **20,000 fights, 840 seconds, 2 GiB and a bounded 900-second owner**. Before allocation require twice the preceding measured per-fight time/storage projection below 80% of the envelope. Use only fresh output paths, reserve all seeds including unconsumed ones, preserve all failures and frozen artifacts. The three-screen total is **44,544 fights / 384 reservations**; including conditional confirmation the maximum is **64,032 fights / 552 reservations**, plus at most 116 historical-seed application replays. Backend regression combat is separate.

The supported search algorithm stays `affinity-creation-with-benchmark-validation-v1`. The scope neither retries the preceding failed screen nor reimports its nominees. The accepted 38-recipe historical floor-6 family and its current-runtime qualification remain preserved; the new 116-recipe family requires independent confirmation at its selected setting.

The preceding +6% result retained two viable four-piece compositions (30 and 34 wins / 128), while the strongest full-family recipe had 50 / 128 against the 44-win ceiling. This motivates the next finite bracket; it does not establish a feasible health-only window. All three new factors apply to the original health 6.153125. Use the authenticated +6% archive only for resource projections, not as the scaling origin. The preceding evidence SHA is `1c8bda140af6f854827d50331b79859faf5b4d0137a252ee360712fa9b8b5941`; publication SHA is `105d95645cdf27450197d6745895efeb9809053e1a520a7e1cf1d59bd9b2e3e3`.

## Results

**Completed: `HealthSettingNotConfirmed`.** The chosen setting passed its exploratory screen but failed independent confirmation. No local game change or alternate confirmation followed. The whole 116-recipe family remains retained.

| Health increase | Health multiplier | Strongest recipe | Four-piece C | Four-piece D | Ceiling / minimum |
| --- | ---: | ---: | ---: | ---: | --- |
| +8% | 6.645375 | 40/128 | 26/128 | 26/128 | Pass / Pass |
| +10% | 6.7684375 | 42/128 | 23/128 | 26/128 | Pass / Fail |
| +12% | 6.8915 | 45/128 | 19/128 | 25/128 | Fail / Fail |

Screen limits remained **25–44 wins / 128**, with simultaneous bounds across all 116 recipes. The strongest-recipe intervals were +8%: 19.03%–46.78%; +10%: 20.30%–48.36%; +12%: 22.22%–50.71%. A setting needs both gates; lower-bound success alone is not family acceptance. Candidate panels used different fresh seeds, so fluctuations between nearby settings are not evidence that adding health improves player win rates. Do not pool them with each other or with the earlier search screen.

Selected **+8%**. Independent confirmation: strongest **62/168**, four-piece C **38/168**, D **45/168**; complete gate failed. No application was performed.

| Confirmation recipe | Wins | Observed rate | Adjusted interval |
| --- | ---: | ---: | --- |
| Strongest retained recipe | 62/168 | 36.90% | 25.12%–50.48% |
| Four-piece C | 38/168 | 22.62% | 13.37%–35.63% |
| Four-piece D | 45/168 | 26.79% | 16.66%–40.10% |

Confirmation retains its independent result; do not pool it with any exploratory screen. There were **1 ceiling failures**. The leading full-Restorer recipe's adjusted upper bound is **50.48%**, against the 50% limit; its observed rate is lower than 50%. This is insufficient evidence to pass the ceiling, not proof that its true win rate exceeds 50%. Both four-piece compositions clear the 10% lower-bound minimum, but that alone cannot accept the complete family.

Total **64,032 fresh study fights / 552 new reservations**. Exclusions increased from **912,796 to 913,348**. Raw recipes, identities, Essence ordering, gear/level budgets, offense and abilities were unchanged. The supported search was neither changed nor rerun.

## Verification and changed files

**Eight fresh selection/application safeguards and 4 owned native fixtures passed.** The preceding **97 backend passes / four opt-in skips and 47 Python passes** were authenticated and reused, not rerun at entry. No production code or game data changed, so no duplicate baseline regression run or rebuild was needed. The isolated runtime and all relevant source/catalog hashes matched. Every candidate outcome, whole-family interval, actual-composition count, selection margin, process receipt and seed reservation was reconstructed independently. Every native process exited with zero active children and zero retries. No verification command failed or remains blocked in this scope.

Game content and maintained harness code are unchanged. The new report, preceding calibration notice, handoff, balance-status notice and two harness guides were updated. The bounded driver, eight guard tests, declarations, collector and publication receipts are saved under `TestResults`. No migration, application configuration, database, deployment, dungeon or equipment-supply changes.

## Evidence and continuation

- Evidence: `TestResults/tower-floor6-health-bracket-evidence-20260930.json`, SHA **`e96f6128fda5bab0ef72b1fece56c9da2421d155c86cfe53b50899ed09e9383a`**.
- Frozen protocol, declaration, original catalog, per-candidate assessments and resource receipts: `TestResults/tower-floor6-health-bracket-driver-20260930/`.
- +8% screen: `TestResults/tower-balance-pass-floor6-health-bracket-plus08-study-20260929`, manifest **`d1de5f5669ad18a1e07512344dea0d6f48257780ad60d4f2c78278e1f738dfbc`**, audit **`44617b64c7e6300d2064a25ac5691c92edc79146a60d79943d5ac6b5133275ad`**.
- +10% screen: `TestResults/tower-balance-pass-floor6-health-bracket-plus10-study-20260929`, manifest **`c6f0fdd0049d8ba83f23f4e11f6ce1d04c9f7ae7a8a7e72bed6124d642e6d95a`**, audit **`bbf6134aabc2fd69abc7239eea6dd9df4020caf00c8aec0cfdcb87343c36abb7`**.
- +12% screen: `TestResults/tower-balance-pass-floor6-health-bracket-plus12-study-20260929`, manifest **`7689a81ad2adb6c767bdb79f287a7eca593f210cab41197e864f803c982e99ca`**, audit **`aa5d268046477a69c5faa39b5f135730d7cbfb4ffbf6be7aa514d1aaf98c87b6`**.
- +8% confirmation: `TestResults/tower-balance-pass-floor6-health-bracket-confirmation-study-20260929`, manifest **`d6ca97d8bd1131d409828381068118b58aa7b1de0abad9fd4212bf2ea2e0fc1c`**, audit **`bb9b94377d23644b9ce48eb05ae7c096a9b96f0646c4a2130c40e78fe1a139f7`**.
- Latest ledger: `TestResults/tower-balance-pass-floor6-health-bracket-confirmation-owner-20260929/seed-ledger.json`, SHA **`4a8fb9b59555e13b1a19aa86df02e55743762174c815832a3010b97eb97db671`**; include every ancestor and later reservation.
- Runtime: `TestResults/tower-floor6-partial-restoration-build-20260930`.
- Current Tower SHA **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**; abilities SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**.

**Next:** Recommend one separately declared +9% midpoint trial, health 6.70690625 from the original 6.153125 baseline, with offense 4.365625 unchanged. The +8% confirmation misses only the full-Restorer ceiling, while the two four-piece teams have seven and fourteen wins of margin above their minimum; the +10% screen loses the second four-piece minimum. This motivates a midpoint, but does not establish that it will pass. Keep all 116 recipes, use a fresh bounded screen and at most one independent confirmation, and freeze the protocol before combat. Preserve the failed +8% confirmation; do not retry, pool, extend it or confirm a rejected grid setting. If the midpoint fails, stop health-only sweeps and diagnose the gear-dependent combat gap before proposing further changes. The midpoint is a recommendation, not an allocated or tested candidate in this closed scope.

The 116-recipe search source remains the unchanged starting catalog for future candidate multipliers: `TestResults/tower-balance-pass-floor6-four-piece-expanded-screen-study-20260929`, manifest `d28d703d42572d1f21897f753998f625a1c5921208866b0a0ab894e2657f3063`. The historical accepted 38-recipe floor-6 family and its runtime qualification remain preserved; none of these new health settings is accepted or live. The latest applied gameplay change remains the floor-5 ability adjustment.
