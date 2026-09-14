# Captured-v19 ceiling diagnosis — zero combat

Completed **14 September 2026** for the offline `LL/tools/BalanceHarness`. The complete **129,536 saved reports**, **253 recipes** and **58 joint-supported ceiling breaches** were analyzed without running combat, allocating seeds or changing gameplay. The primary command completed in **139.014 seconds**; its measured analysis took **138.204 seconds**. Reliability remains **Fail 1/3**, adoption **Hold**, and the confirmed ordinary/joint family result **Fail / Fail**.

The next scientific design is the [frozen correction screen](Tower-Portfolio-Ceiling-Recalibration-Plan.md), targeting **captured v19**. It is not prepared or executed. The broader retained inventory contains **43,879 recipe/context entries**, so a screen of the 253 challengers cannot establish full-family acceptance.

## Shared patterns and their limits

All 58 supported breaches are new generated recipes. **49** belong to the deep `loadout-composition-joint` root **−1038588442**; **nine** belong to the portfolio's deep component under root **−867718476**. These are closely related search results, not 58 independent replications of a mechanic. All 112 controls remain in the analysis, together with the other 83 generated recipes.

The strongest saved recipe is `team-040e60d3dbc5c127321653c47ed3a9d3`: **396/512 (77.34%)**, with the original joint-adjusted interval **69.42–83.70%**. The fixed anchor remains 0/512; the stronger saved control remains 152/512. Original and screened nominations are unchanged.

Mean equipped copies per complete ten-character party:

| Essence | 58 supported breaches | Other 83 generated recipes | 112 controls |
| --- | ---: | ---: | ---: |
| Pack Howler | 10.000 | 9.518 | 8.509 |
| Venomous Spiderling | 8.517 | 7.518 | 4.884 |
| Spider Queen / Royal Venom | 7.948 | 8.241 | 5.509 |
| Enchanted Fairy | 8.759 | 8.880 | 9.107 |
| Elder Treant / Thornstorm | 2.690 | 1.867 | 0.063 |
| Bark Golem | 1.466 | 0.627 | 0.107 |
| Ravenous Ghoul | 2.379 | 3.205 | 1.964 |

Every supported-breach party has ten Pack Howlers, at least six Venomous Spiderlings and at least seven Royal Venom Essences. However, Pack Howler appears in **all 253 parties**. Royal Venom and Fairy counts are slightly lower in supported breaches than in the other generated teams. Counts alone cannot identify a sufficient correction. The [complete feature tables](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/recipe-features.json) retain all Essence indicators, pair co-occurrences, ordered loadouts, party slots and origins; no feature was removed for having an inconvenient association.

The captured definitions describe a plausible interaction: Coordinated Attack requests basic attacks from non-summoned allies; the Royal Venom status adds Poison on basic attacks; Toxic Opportunity adds Poison against Slowed targets. Fairy supplies Corrosion and recurring control/stagger, while Royal Cocoon, regeneration and other recovery help characters survive. This is a **mechanistic hypothesis from the captured definitions**, not a counterfactual test or a demonstrated engine defect. Pack Howler's attacks may be reported under Basic Attack, so ability-name totals cannot isolate its incremental contribution.

## What the complete saved reports show

Means across all 512 trials per recipe, including losses:

| Saved measure | Supported breaches | Other generated | Controls |
| --- | ---: | ---: | ---: |
| Fight duration, seconds | 94.16 | 94.32 | 83.62 |
| Friendly survivors | 5.00 | 2.75 | 0.28 |
| Guardian health remaining, percent | 6.96 | 12.48 | 38.93 |
| Recorded friendly damage | 22,614.54 | 20,586.77 | 14,294.12 |
| Recorded friendly healing | 5,509.67 | 5,864.02 | 4,135.36 |
| Friendly health regenerated | 6,243.10 | 5,833.04 | 4,642.30 |
| Friendly credited stagger breaks | 2.02 | 1.89 | 1.58 |

In supported breaches, the largest friendly damage labels per trial are **Basic Attack 6,511.25**, **Royal Venom 4,950.08**, **Fae's Corrosion 2,019.14**, **Venom Web 1,995.74**, **Thornstorm 1,793.29** and **Toxic Opportunity 1,745.77**. Royal Cocoon contributes **3,769.77** recorded healing per trial. The [complete report aggregates](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/saved-report-aggregates.json) retain every recipe and ability, not just these examples.

These are saved engine counters, not net guardian health loss or an additive causal decomposition. Damage can exceed the guardian's 19,258 starting health; healing and mitigation counters use their own engine definitions. Duration, survival and group composition depend on outcomes and common ancestry. The reports do not supply a new event timeline or an intervention that separates extra attacks, Poison, recovery and stagger. No new intervals or significance tests were constructed for the descriptive feature comparisons.

## Version decision

Use the captured-v19 gameplay assemblies, content and settings for the correction screen. This isolates the proposed encounter adjustment against the version in which the 58 breaches were confirmed. It does not authorize applying that setting to the current game.

All **16 bound content files** were compared. **Ten differ** from the checkout. The **80 existing Essence definitions**, **234 existing ability definitions**, Kharad creature definition and complete Tower-floor settings remain unchanged; the new Essence/ability entries include five Lizardfolk Essences and ten abilities. Other changed files include creature profiles, statuses, summons, items, region balance, creatures, regions and loot tables. The [semantic differences](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/content-differences.json) retain exact paths and values.

The current combat engine and ability runtime source differ from the v19-start hashes. The inspected git parent did not reproduce the captured engine source exactly. Accordingly, this review uses the retained definitions/reports and captured DLL identity; it does not present the current engine source as v19 provenance. The previous [execution audit](Tower-Portfolio-Confirmation-Implementation-Review.md) retains the four captured gameplay DLL hashes. Current-gameplay acceptance requires a separately frozen baseline, eligibility/content audit and fresh evidence, even where existing content entries match.

## Full-family coverage boundary

The [retained inventory](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/retained-family-inventory.json) unions the old **17,821** full-family recipes, every retained top-level `all-evaluated-recipes.json` package, and all **253** confirmed recipes. It retains **43,879** recipe/context entries and all origins in a deterministic [compressed archive](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/retained-family.json.gz). All 253 confirmed recipes are represented; their inclusion adds no duplicate entry.

Normalization preserves scenario/actor identities, timing, preparation and ordered Essences, while normalizing party/equipment order and equivalent implicit/explicit identity vectors. These are descriptive inventory keys, **not harness `RecipeHash` values**. Strict materialization, context compatibility and a history coverage audit remain required before a confirmation definition. The inventory is not a proof that every possible legal party has been searched.

The existing staged contract permits **20,000 cells** and **500,000 fights**. This inventory exceeds its cell limit. Even 24 trials for every entry would require **1,053,096 fights**, before anchors or a second stage. No existing limit was raised. A new full-family protocol must address capacity and uncertainty explicitly; omitting unshortlisted parties, carrying old outcomes across new scaling, or confirming only the 58 breaches cannot produce a full-family Pass.

## Verification, measurements and preservation

The [read-only protocol](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/protocol.json) froze the script hash, input hashes, source inventory, complete report scope and **zero fights / zero seeds / zero retries**, with **1,800 seconds** and **512 MiB** for the primary analysis. The watchdog was also set to 1,800 seconds. The primary command returned **0**; no limit was reached.

- All **9,026 indexed files** in the completed confirmation study matched their hashes; the complete file set matched its final manifest.
- All **4,048 chunks / 129,536 records** were read once. Every recipe reproduced the exact ordered 512-seed schedule and its saved win/defeat/draw counts.
- All ten inventory sources matched their sealed source inventories, including the stopped allocation archive's `stopped-files.json` binding.
- The primary analysis verified the checkout was unchanged and all six frozen historical manifest markers were preserved. Final source/document preservation is recorded in the new receipt. Ancestors' entire trees were not redundantly rehashed; the study tree and consumed source bindings were checked explicitly.
- **481,219 reservations** remain unchanged, including all 512 unused v19 confirmation values. No seed allocator or combat runner was called.

The primary analysis retained **20,746,960 bytes** before its receipt and later supplementary reporting. Final size and script/document hashes are in [final verification](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/final-verification.json). This is read-only analysis performance, not a gameplay or whole-campaign speedup.

A supplementary source-verification command initially failed because it expected a normal final manifest in the historical stopped allocation archive. Its script and [failure receipt](../TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914/supplement-first-failure.json) are retained. A separate continuation checked the correct stopped manifest and completed successfully; it did not repeat the primary report pass, combat or seed allocation. Exploratory path lookups also encountered unavailable names. No required command remains blocked. Exact captured engine source recovery remains a documented limitation.

Provenance commands, already completed; do not rerun them into this sealed directory:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-portfolio-ceiling-diagnosis-20260914'
& $py "$w/diagnose.py" freeze
# analyze ran through the recorded subprocess wrapper with timeout=1800:
& $py "$w/diagnose.py" analyze
& $py "$w/finish.py"                 # retained supplementary manifest-lookup failure
& $py "$w/finish-continuation.py"    # completed supplementary checks/design
git -c core.safecrlf=false diff --check
```

To reproduce the analysis, copy the script into a new sibling work directory, freeze a new read-only protocol there and retain the same input hashes. The script has no combat invocation and refuses to retry an existing analysis. Input hashes identify which checkout/content comparison is reproduced. Reproduction itself is additional read-only work, not another balance sample.

This task adds the diagnosis review, frozen correction plan and ignored evidence/scripts, and updates six active handoffs. No harness, game, test or catalog source changed. Backend tests were not repeated because no backend implementation changed; the previous 201-test evidence remains scoped to its producing implementation. Future implementation checks must use `build/run-tests.ps1`. There are no migrations, live configuration changes, deployment effects or old-cap changes.
