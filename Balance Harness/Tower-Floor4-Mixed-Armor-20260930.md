# Floor 4: unchanged-Vaelor mixed armor trial (30 September 2026)

**Subsequent result:** The [Mirror Lance/Hall of Shards trial](Tower-Floor4-Lance-Shards-Trial-20260930.md) closed **`NoEligibleLanceShardsConfirmation`**. It tested magical coefficients **0.60/0.65**, proportional Mirrorbound scaling, and unchanged physical coefficients **2.00/0.50** across all **98 loadouts / five actual compositions**. Best two-character armor variants won **2/128 for both compositions**, below **25/128**. Full armor won **10/128 and 20/128**; every upper bound is below 50% (maximum **29.66%**), but no recipe reaches the 10% lower bound. **12,544 fresh fights / 128 reservations**, one passing native study fixture, zero retries; no confirmation or live application. The full archive and candidate changes were independently verified. **30 candidate Python checks, four Vaelor tests and 108 backend passes/four opt-in skips were authenticated and reused**, not rerun. Exclusions **917,081**. Live game data, approved progression budgets and supported search remain unchanged. The earlier diagnostic and both proposed candidates are now complete; follow the latest linked trial and handoff. Historical scope below remains closed.

## Frozen protocol

Target: primary LL World Tower and its offline Balance Harness. Start from the authenticated floor-4 review and its complete 98-loadout proposal, retaining all 38 original controls and every one-, two-, three- and four-character armor subset for both leading compositions. Count actual compositions by per-slot Essence sets; preserve raw Essence order, identities and every budget field in combat inputs.

Keep Vaelor health **2.3231953125**, offense **5.023125**, defense/resistance **2.33**, penetration/regeneration **1** and all abilities unchanged. The floor-4 budget is **five characters, level 30, four level-1 unascended/unevolved Essences each, T1 Epic/Fine/Rank 3 equipment, attribute roll 1, no styles**.

Require two actual compositions to qualify with armor/health equipment on at most two of five characters (eight specialized items). Three- and four-character variants remain diagnostic controls. Every recipe must have an adjusted upper win-rate bound at most 50%; qualifying partial recipes need lower bounds at least 10%. Use approximate simultaneous 95% Bonferroni-Wilson intervals across all 98 recipes.

- Authenticate both complete accepted floor-2 aggregate panels and all four application replay receipts. Recount all 117,760 saved fights; never repin historical paths or admit one batch alone. Current live content must match the applied catalog.
- Qualify all **9,728** historical floor-4 inputs and **38** full saved-seed replays on the current pinned runtime. No fresh seeds. Failure stops the trial.
- Prepare all **98** exact loadouts on current content without combat or seed allocation.
- Screen **128** fresh shared seeds: **12,544** fights. Integer gates **25–44 wins**. Only a complete passing screen permits confirmation.
- Conditional independent confirmation: **184** fresh shared seeds, **18,032** fights, integer gates **33–68 wins**. Retain the same complete family. On success, verify all confirmation inputs and 98 full replays against live content.
- Maximum **30,576 fresh fights / 312 reservations**. Initially exclude **916,697** seeds and all later reservations. No extensions, dropped controls, pooling, replacement seeds, retries or resumed archives.
- Each native phase: **20,000 fights / 840 seconds / 2 GiB**, bounded owner **900 seconds**. Before screening use doubled historical measured cost after native qualification; before confirmation use doubled current screening cost. Both projections must stay below 80% of time and storage caps. Historical estimates are estimates, not current combat timings.

Use fresh `tower-floor4-mixed-armor-*-20260930` control/qualification artifacts and native `tower-balance-pass-floor4-mixed-armor-{preparation,screen,confirmation}-*-20260929` paths. Preserve completed or failed outputs. Python provenance now admits complete applied aggregate receipts; the native v1 qualification contract is unchanged. This trial edits no gameplay catalogs, search algorithm, progression budgets, acquisition supply or dungeon content.

## Verified result

**The partial-armor target was not established.** This declared trial is closed without changing Vaelor. Screening did not pass the complete gate, so no confirmation or application ran.

The [floor-4 mixed-armor trial](Tower-Floor4-Mixed-Armor-20260930.md) closed **`ScreenDidNotQualifyMixedArmor`**. All **98 loadouts / five actual compositions** were retained. The best at-most-two-character armor variants for A/B won **5/128 and 4/128** in screen; **0** actual partial-armor compositions meet the lower-bound target. The maximum adjusted upper bound is **48.95%**. **12,544 fresh fights / 128 reservations**, plus native qualification of **9,728 historical inputs / 38 full replays**. **80 Python safeguards and 10 native fixture cases passed**; prior **108 backend passes/four skips** were authenticated and reused. Floor 4 remains **five level-30 characters / four Essences / T1 Epic Fine rank 3**; Vaelor and all live catalogs are unchanged. Exclusions **916,825**. 

The table reports the strongest saved subset at each armor count for each parent in **screen**. These are descriptive selections from the complete family, covered by the same 98-recipe correction. Only one- or two-character variants can satisfy the target; three- and four-character results do not count toward it. A is `c1e05561d5e7a7a183a884f98996fa6063801089d0126cab7915acf7c50185bf`; B is `e5b6edf48463597c1f72c72a2ba8b67f22fd5b0596ce496719be794004d21b5f`.

| Composition | Armored characters | Party slots | Wins | Adjusted interval |
| --- | ---: | --- | ---: | ---: |
| A | 5 (full armor) | 1, 2, 3, 4, 5 | 32/128 | 14.26%–40.05% |
| A | 1 | 4 | 3/128 | 0.40%–12.50% |
| A | 2 | 3, 5 | 5/128 | 0.94%–14.82% |
| A | 3 | 2, 3, 4 | 9/128 | 2.36%–19.11% |
| A | 4 | 1, 2, 3, 4 | 18/128 | 6.49%–27.83% |
| B | 5 (full armor) | 1, 2, 3, 4, 5 | 43/128 | 21.07%–48.95% |
| B | 1 | 2 | 1/128 | 0.06%–9.99% |
| B | 2 | 1, 2 | 4/128 | 0.65%–13.68% |
| B | 3 | 1, 2, 5 | 12/128 | 3.63%–22.13% |
| B | 4 | 1, 2, 4, 5 | 23/128 | 9.12%–32.34% |

The ceiling passed; the partial-composition count is **0**, versus the required two. Screening gates remain **25–44/128**, confirmation gates **33–68/184**. No loadouts, seeds or prior outcomes were dropped or substituted.

## Implementation and verification

`tower-catalog-qualification.py` now admits explicitly versioned complete applied-aggregate provenance and floor-4 mixed armor. `tower-balance-aggregate.py` authenticates/recounts both complete four-batch panels, checks their independent seed histories, reconstructs both assessments and requires all four native parity receipts plus the applied-catalog completion. Historical requests and hashes remain untouched: pre-application live files and helper files are historical provenance, while immutable archived contents and replay receipts prove the accepted transition. Current catalogs must then match that applied content. The C# v1 qualification contract and compiled runtime did not change.

`run-tower-balance-pass.py` permits this exact floor-4 family only during seed-free preparation, with no concurrent guardian or family transformations. The shared family validator reconstructs every subset and retains all raw identities, Essence order, original controls and budgets. Tests exercise complete/incomplete aggregate receipts, wrong runtime/catalog, failed or active processes, missing/duplicated subsets and altered recipes.

- **80 focused Python tests passed:** qualification **37**, aggregate **24**, reference coverage **19**. Initial `unittest discover` commands found zero tests because these scripts use hyphenated filenames; direct script invocation ran all tests successfully. The zero-test logs are preserved and not counted.
- Native qualification matched **9,728 inputs and 38 full replays** with no new seeds. Subsequent preparation, screening used `build/run-tests.ps1 -NoBuild` through bounded owners. **10 fresh native fixture cases passed**, including contract theory cases; these are not 10 independent balance trials.
- The unchanged combat assemblies retain authenticated **108 backend regression passes / four opt-in skips** from the prior application. These were reused, not rerun. No C# rebuild was needed for the Python-only changes.
- The independent collector reauthenticated the applied aggregate and qualification, reconstructed every floor-4 outcome and adjusted bound, checked raw loadouts/settings/runtime and seed exclusions, and verified stopped processes and native resource limits. Fresh trial combat: **12,544 fights**. Saved-seed parity combat: **38 full replays**. Recounting archived JSON is not additional combat.
- Live gameplay files remain unchanged, including all **102 JSON catalogs**, floor-2 Gale/Feast **0.35/0.65**, and the accepted floor-5/floor-6 settings. No migrations, configuration changes, database actions or deployment.

## Next work

Use the saved floor-4 screen to compare each leading composition’s best at-most-two-character armor recipe with its full-armor control on the same seeds. Inspect prepared attributes, recipient deaths and damage, then perform a small fixed saved-seed event-timeline diagnostic if the summaries cannot identify the pressure. Only that evidence should define a bounded Vaelor adjustment or targeted composition search; retain all 98 loadouts as controls. Do not extend or repeat this screening panel, pool its results, change the level/Essence/gear budget, or start a blind scalar sweep. No active trial remains. Keep the repeating gear curve and supported search; no dungeon or supply work.

## Evidence and commands

- Evidence: `TestResults/tower-floor4-mixed-armor-evidence-20260930.json`, SHA **`c86fa09a65c8b44b5054b17ca53b8cd53490033ca4710e3394c5f63ba5cab2b3`**.
- Declaration: `TestResults/tower-floor4-mixed-armor-driver-20260930/declaration.json`, SHA **`85d16581830ab483a28a954a5648c635f33007ae5e366d44b1c6760035e2904e`**. The archived `frozen-protocol.md` preserves the prospective text before this result was added.
- Screen archive: `TestResults/tower-balance-pass-floor4-mixed-armor-screen-study-20260929`, manifest **`66cc379b5f226c6925e7b1a240c0f65c8bef6d630bcafde7978ca036140d406d`**.
- Latest ledger: `TestResults/tower-balance-pass-floor4-mixed-armor-screen-owner-20260929/seed-ledger.json`, SHA **`88a33964750807977fe3e5a354215652f2242d65f7693bb021ba2e371c2908f7`**, plus ancestors and any later reservations. Exclusions **916,825**.
- Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**; abilities SHA **`169b61f23c2e1e301e64e176962bbe3dc8386ea941a87a7f5a8205c872963d6a`**.
- Current publication: `TestResults/tower-floor4-mixed-armor-publication-check-20260930.json`; it binds the result, maintained helpers, raw receipts and updated documentation.

Executed with the configured Python runtime and `-B -X utf8`: the three test scripts above, `TestResults/tower-floor4-mixed-armor-driver-20260930.py`, the independent collector and this publisher. Native commands and logs are preserved under the driver and owner directories. `git diff --check` and all updated Markdown links pass. No requested verification remains blocked. Confirmation and application were intentionally not run because screening failed. Completed drivers and studies must not be rerun or resumed.
