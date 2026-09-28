# Floor 10→11 carried-equipment screen — 28 September 2026

**Follow-up completed:** the [four-setting carried-equipment calibration](Tower-Carried-Equipment-Calibration-20260928.md) retained these 228 cells and added 116 armor-and-health progression variants. All **44,032 fights** completed and passed independent audit. No setting qualified: at 2×, the best seven-/six-Essence level-60 parties won 32/32 and 18/32; at 4×, only one intended party won once. The next recommendation is one finer bounded pass between 2× and 4× with all 344 combinations retained. No boss setting was applied.

**Completed and independently verified: all 228 combinations won 32/32 with Legendary / Masterpiece / rank-5 gear.** This includes all 14 six-Essence level-50 parties retained from floor 10, both six-Essence level-60 controls and all 14 hypothetical four-Essence controls. The locally applied floor-11 setting does not meet the intended difficulty or lower-Essence separation goals under this equipment budget. Keep the requested repeating curve, but use retained equipment when calibrating the actual transition.

Target: the primary game's offline Balance Harness. This separately bounded diagnostic checks the [locally applied floor-11 setting](Tower-Floor11-Checked-Application-20260928.md) with equipment retained at the [floor-10 budget](Tower-Floor10-Checked-Application-20260928.md). No live content, equipment ownership, search policy or repeating evaluation curve changes as part of the screen.

## Frozen comparison

Retain the entire **228-cell** floor-11 confirmation family: **198 seven-Essence level-60 combinations**, **14 six-Essence level-50 controls**, **two six-Essence level-60 controls** and **14 four-Essence level-30 controls**. This includes all original team/gear variants and all 114 uniform seventh-Essence additions. Party size stays ten. Levels, tiers, ordered Essences, specializations, rolls, styles and party positions retain their source values.

Compare the original **Rare / Standard / rank-2** reference equipment with **Legendary / Masterpiece / rank-5** equipment at the same tier and specializations. All 14 carried six-Essence level-50 parties must exactly equal the first ten members of their corresponding saved floor-10 parties. The 116 level-up/seventh-Essence variants must be reconstructed from those carried parents through the existing progression helper, preserving their equipment and existing instance identities. There is no party-member selection after observing outcomes.

The equipment evaluator derives gear stats from tier, rarity, quality, rank and rolls; raising character level does not create a higher-level replacement item. The three non-seven-Essence cohorts remain separate. The four-Essence level-30 parties retain tier-1 equipment and are hypothetical strength controls, not demonstrated floor-10-clearing parties. Original seven-Essence reference parties likewise test the declared equipment budget, without proving acquisition or a completed earlier journey. Ownership remains hypothetical throughout.

Rarity conversion uses the existing equipment catalog and preserves archetypes/specializations. Changing rank and quality also changes deterministic reference identities between the old reference budget and the carried budget. Paired baseline observations therefore describe the complete budget change; they are not an isolated stat-only causal estimate. The actual floor-10-to-floor-11 six-Essence links preserve the saved floor-10 build definitions, and the subsequent level/Essence upgrades preserve those carried identities. No Essence ordering changes are introduced.

The schedule uses the **first 32 existing seeds** from the passing floor-11 confirmation, frozen before combat. Each recipe retains its complete archived 256-value seed list for exact baseline input identity; only the declared first 32 values execute. First prepare all **456** baseline/carried recipes with combat disabled and verify all **7,296 baseline input hashes**. Then replay all **7,296 complete baseline reports**. Only after those match may the **7,296 carried-gear fights** begin. Total: **14,592 historical fights**, zero new seeds and zero retries.

Every cell runs. Record separate baseline/carried wins, paired gains/losses, draws, remaining guardian health and duration. Intended wins above **16/32** flag an observed ceiling concern; lower-Essence control wins at or above **4/32** flag an observed separation concern. Both counts remain visible if both occur. A cell's 4–16/32 result is descriptive viability only. Historical seed reuse supports no fresh acceptance, and the screen makes no automatic calibration, extension, content application or search change.

Current content must match the checked floor-10 application, including floor 10 **12.71 / 7.13** and floor 11 **6.525 / 8.37**. Settings remain attributes 18, equipment release 4, `healing-v1`, the captured threat rules and ten ticks per checkpoint. The study uses a newly built current executable, with no historical DLL substitution. Full baseline input/report parity qualifies this retained panel for comparison.

Limits: **1,200 seconds** internally, **1,260 seconds** for the process owner and **2 GiB** output. The owner freezes request, source and runtime hashes and drains its process tree. All combat, preparation and native reconstruction are included in the native budget; builds, ordinary regression tests and the independent read-only audit are separate. Evidence is retained on failure, with no combat retry or resume. The fresh-seed registry remains untouched; its latest exclusion union is **835,063**.

## Implementation and execution

- [BalanceHarnessCarryForwardEquipmentTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessCarryForwardEquipmentTests.cs): opt-in complete-family comparison (`LL_CARRIED_EQUIPMENT_SCREEN`), real floor-10/progression linkage checks, baseline parity gate, per-attempt accounting and native archive reconstruction. Ordinary tests cover complete-family decisions and preservation of carried equipment/identities through level and Essence upgrades.
- [screen-carried-equipment.py](analysis/screen-carried-equipment.py): pinned source/current-content import, frozen schedule and bounded process owner.
- [verify-carried-equipment.py](analysis/verify-carried-equipment.py): independent archive authentication, gear conversion and carry-link reconstruction, all baseline reports, all paired outcomes and final decision.

The protocol above was recorded before combat. The fixed run completed without extensions, retries or changes to its frozen fixture, owner or auditor.

## Results

Every row below describes separate team/equipment combinations, each with 32 historical seeds. Do not pool different parties as independent estimates of one team's win rate. All **7,296 carried fights were victories**, with no draws or lost paired wins. The screen decision is **`ObservedLowerBudgetBreach`**; it also records **198/198 intended combinations above the observed 50% ceiling** and **30/30 lower-Essence controls at or above the observed 10% threshold**.

| Cohort | Combinations | Rare reference wins per combination | Carried-equipment wins per combination | Carried mean-duration range |
| --- | ---: | ---: | ---: | ---: |
| Seven Essences, level 60 / tier 2 | 198 | 0–8/32 | 32/32 throughout | 27.39–50.06 s |
| Six Essences, level 50 / tier 2 | 14 | 0/32 throughout | 32/32 throughout | 37.49–42.40 s |
| Six Essences, level 60 / tier 2 | 2 | 0/32 throughout | 32/32 throughout | 36.69–36.90 s |
| Four Essences, level 30 / tier 1 | 14 | 0/32 throughout | 32/32 throughout | 62.27–81.09 s |

The repeated and alternating **armor-and-health six-Essence parties** are the most relevant transition controls: their saved floor-10 counterparts had 23/256 and 55/256 wins, respectively, in the separate floor-10 confirmation. Here, their first ten members both win **32/32 on floor 11**, at mean durations of **40.05 and 40.50 seconds**, with the same level, equipment and six Essences. They need neither a level increase nor a seventh Essence to clear this panel. Different floors, party sizes and seed panels prevent interpreting those figures as a paired floor-to-floor effect; the exact recipe/equipment linkage is verified.

Only five seven-Essence combinations had any Rare-reference wins on this 32-seed subset: repeated Gnoll Shaman **3**, repeated Lizardfolk Elementalist **2**, repeated Shadow Imp **8**, alternating Lizardfolk Elementalist **1**, and alternating Shadow Imp **3**. Their carried counterparts all won 32. All remaining 223 combinations went from zero to 32 wins. The complete [228-row result](../TestResults/tower-carried-equipment-20260928/result.json) retains every paired count, duration and remaining-health mean; the [carried recipes](../TestResults/tower-carried-equipment-20260928/carried-cells.json) retain the exact builds.

The four-Essence findings are equipment-budget stress tests, not proof that a level-30 party can acquire this equipment or reach floor 11. Likewise, the original seven-Essence parties are not established floor-10 progression paths. None of the 32/32 observations establish a true 100% win probability. These are descriptive results on reused seeds, with **no fresh balance acceptance** and no new search comparison.

## Verification and evidence

The [independent audit](../TestResults/tower-carried-equipment-owner-20260928/independent-audit.json) passed on its first run. It authenticated **15,186 files**, reconstructed all **14,592** outcomes and paired comparisons, verified all **7,296 baseline inputs and complete reports**, and checked all **14 floor-10 party links** and **116 progression links**. Native archive verification also reconstructed all input/cache identities. No audit fight was run.

Native execution took **409.18 seconds**. The owner completed in **412.58 seconds** and drained all **eight processes**, within the declared limits. Sealed output occupies **396,535,557 bytes**. The owner retained the [declaration](../TestResults/tower-carried-equipment-owner-20260928/declaration.json), [process receipt](../TestResults/tower-carried-equipment-owner-20260928/process.json) and [completion receipt](../TestResults/tower-carried-equipment-owner-20260928/completion.json).

Before combat, the focused suite passed **44 tests**, with two opt-in scientific fixtures skipped. The explicitly enabled screen then passed **seven tests**, including the complete scientific run. All backend tests used `build/run-tests.ps1`. The first build caught an invalid call to `ToSnapshot()` on an already captured equipment snapshot; this was corrected before the successful build and before freezing the protocol. The corrected build reported **16 existing warnings and zero errors**. Logs and test receipts remain in [corrected-build-and-tests.log](../TestResults/tower-carried-equipment-preparation-20260928/corrected-build-and-tests.log), [focused-tests.trx](../TestResults/tower-carried-equipment-preparation-20260928/focused-tests.trx), [screen.log](../TestResults/tower-carried-equipment-owner-20260928/screen.log) and [screen-tests.trx](../TestResults/tower-carried-equipment-owner-20260928/screen-tests.trx).

Archive manifest: `76e028152ed3ccfb09e5e3bf60ff5ae3acabcb0a8f6db18a395806ed19f256f1`.
Result SHA-256: `fcd5b5787b2505c29f723fa79066f7a2656fe831851108d47e7a02df9b55c6d0`.
Live floor-file SHA-256 remains `dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124`.

Commands executed from the repository root (Python denotes the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-carried-equipment-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessCarryForwardEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
# Historical execution record; the owner refuses reuse of the completed directories.
python -B -X utf8 'Balance Harness/analysis/screen-carried-equipment.py' --package 'TestResults/tower-carried-equipment-owner-20260928' --output 'TestResults/tower-carried-equipment-20260928' --artifacts 'TestResults/tower-carried-equipment-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-carried-equipment.py' --owner 'TestResults/tower-carried-equipment-owner-20260928' --manifest-pin '76e028152ed3ccfb09e5e3bf60ff5ae3acabcb0a8f6db18a395806ed19f256f1' --receipt 'TestResults/tower-carried-equipment-owner-20260928/independent-audit.json'
git -c core.safecrlf=false diff --check
```

Python syntax/CLI, all **85 local documentation links**, new-file whitespace and `git -c core.safecrlf=false diff --check` passed. The final check also verified every frozen input/source hash and the unchanged live floor file. Archives and build output are local ignored `TestResults` evidence, absent from a clean checkout; further read-only audits require a new receipt path. No required command remains blocked or unrun. This work adds the fixture, owner, auditor and this report, and updates the two application reports, equipment baseline and supported-search guide with the new finding. Earlier sealed studies remain intact.

## Next decision

**Calibrate the floor-11 transition against carried Legendary equipment before spending more effort on search algorithms.** Keep the requested ten-floor reference curve. Treat it as the equipment available for that reference scenario, while retaining stronger equipment already owned in progression scenarios. Keep the current search as the supported implementation.

Use a separately declared, bounded Health/Power calibration with the complete retained family and lower-Essence controls. Include seventh-Essence upgrades of the actual floor-10 armor-and-health parents, with matched six-Essence level-50 and level-60 controls: the existing 114 added-Essence variants all use resistance-and-health, so they do not cover that successful floor-10 gear route. Keep gear and identities fixed through each upgrade. Select no new setting from this saturated 32/32 screen, and require fresh confirmation of any later candidate. If a finite calibration cannot keep seven-Essence parties viable while limiting the matched six-Essence parties, revisit encounter mechanics or the intended checkpoint requirement instead of indefinitely increasing both stats.

The earlier Rare-reference confirmation remains valid within its declared budget; it does not establish a balanced carried-equipment transition. This screen changes no boss values, production equipment rules, search policy, dependencies, configuration or migrations, and performs no deployment, database change or service restart. The previously applied floor-10/floor-11 values remain in the working tree. It allocates zero new seeds and leaves the recorded fresh-seed exclusion union at **835,063**.
