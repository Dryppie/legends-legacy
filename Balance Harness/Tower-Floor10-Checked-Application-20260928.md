# Floor-10 checked local application — 28 September 2026

**29 September follow-up:** the [consolidated balance review](Tower-Balance-Status-20260929.md) preserves this historical evidence and identifies floor 10 as the next diversity priority. Its accepted family has one viable composition; floor 11 has four across seven qualifying recipes. The review adds no fights or current-runtime replay claim. The [current handoff](Tower-Continuation-Handoff-20260928.md) supersedes older queues and supply recommendations.

**Applied locally: Health 12.71 / Power 7.13**, the exact 7.75× setting from the [passing fresh confirmation](Tower-Floor10-Family-Confirmation-20260928.md). The current build prepared all **21** confirmed recipes, matched **all 5,376** archived input hashes and reproduced **23 complete battle reports**. The independent audit verified the application. No deployment or service restart occurred.

## Content change

Only the floor-10 fields `guardianScaling.health` and `guardianScaling.offense` changed in [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json): **1.64 → 12.71** and **0.92 → 7.13**, respectively. Defense, resistance, penetration, regeneration, guardian kit, rewards, release state and every other floor remain as before this operation. The earlier floor-11 Health **6.525** / Power **8.37** application is preserved. All 28 other captured content files match the confirmed source.

The complete resulting floor document matches the confirmation's isolated content structurally. The edit preserves the original formatting and changes only two numeric tokens. Applied file SHA-256: `dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124`. The [saved original](../TestResults/tower-floor10-application-owner-20260928/floor-before.json) has SHA-256 `32ece402099702d29e34977c507adbb53c5d2a0a5994f82fc9a4507f7b4d6e8e`.

This targets the primary game's Tower content and offline Balance Harness. The requested repeating ten-floor equipment curve and supported search policy remain in place. No dependency, appsettings, migration or database change is required. The Tower definition provider loads its catalog into a singleton, so an already running service needs its normal restart/content reload to use the updated file. This task does not perform that restart or change any external environment.

## Compatibility check

The [application owner](analysis/check-floor10-application.py) requires the pinned, independently audited passing confirmation and exact pre-application file hash. Before writing locally, it verifies source/current content, constructs and checks the precise two-field delta, saves both file versions, and freezes the request, source/executable hashes, diagnostic limits and deterministic replay choices in [protocol.json](../TestResults/tower-floor10-application-owner-20260928/protocol.json).

The [native fixture](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10ApplicationTests.cs), enabled by `LL_FLOOR10_APPLICATION`, uses newly built current game assemblies. All five current binary hashes differ from the captured confirmation build; none were replaced with historical binaries. The [recorded scope](../TestResults/tower-floor10-application-20260928/scope.json) captures the actual execution identity. Effective settings still match attributes 18, equipment release 4, `healing-v1`, the captured threat rules and 10 ticks per checkpoint; only sanitized settings enter the output.

Before diagnostic combat, the fixture prepares all 21 recipes with combat disabled and reconstructs every input from the original 256-seed schedule. Exact recipes, party positions, ordered Essences, actor/item identities and equipment remain preserved. After all **5,376** hashes match, it replays:

- The first archived win and first archived loss for each of the repeated and alternating armor-and-health parties: four reports.
- The first archived seed for each of the remaining 19 combinations: 19 reports.

The **23** replays cover all **21** recipes and include both observed outcomes for the two recipes with wins. Complete reports match, including participants, checkpoints, statistics and outcomes, at the original report detail level. Input parity covers the full family; battle parity covers the 23 declared representatives. These are integration checks using historical seeds, not new strength observations. They add **zero fresh seeds** or statistical samples and leave the **835,063** exclusion union unchanged.

The fixed diagnostic limits are 23 fights, **300 seconds** internally, **360 seconds** for the process owner, **128 MiB** of output and zero retries. Builds and ordinary regression simulations are separate. On a failed check, the owner retains the evidence and local edit for inspection; it cannot automatically resume or retry. The independent Python `verify` mode authenticates the output inventory, checks content and runtime hashes, validates the full input journal and complete reports, verifies coverage of all recipes/outcomes and confirms process drainage. It runs no fights.

## Results and verification

The [independent audit](../TestResults/tower-floor10-application-owner-20260928/independent-audit.json) passed on its first run. Native execution took **29.46 seconds**; the owner completed in **31.44 seconds** and drained all **eight processes**. All 23 attempts completed, with no retries. The sealed output occupies **12,057,611 bytes**.

The relevant backend suite passed **223 tests before and 223 after application**, covering Tower services and preparation, saved-report integrity, prepared-runtime reuse, progression equipment/preview, gear profiles, calibration decision rules and the region-boss catalog. Three opt-in scientific/application fixtures were skipped in each ordinary run. The application fixture was separately enabled and passed. The historical calibration and fresh confirmation were not rerun. All tests used `build/run-tests.ps1`; the build reported **45 existing warnings and zero errors**.

Python syntax/CLI, documentation links, new-file whitespace and `git -c core.safecrlf=false diff --check` passed. No required verification command remains blocked or unrun. Logs are retained in [build-and-tests.log](../TestResults/tower-floor10-application-preparation-20260928/build-and-tests.log), [after-tests.log](../TestResults/tower-floor10-application-preparation-20260928/after-tests.log) and [application.log](../TestResults/tower-floor10-application-owner-20260928/application.log).

Application manifest: `b2bb16c69e25412488caed6a09916c336012ca2322db69f9d39a45f7addc90d7`.
Result SHA-256: `bc2b08c124fd23b35f1b876c8001b5b23b8ec183b85d4707388787ef165a32d6`.
Source confirmation manifest: `95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944`.

```powershell
$filter = 'FullyQualifiedName~WorldTower|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessFloor10FamilyConfirmationTests|FullyQualifiedName~BalanceHarnessRefinedFloor10CalibrationTests|FullyQualifiedName~BalanceHarnessFloor10ApplicationTests|FullyQualifiedName~RegionBossDefinitionProviderTests|FullyQualifiedName~Mad_king|FullyQualifiedName~mad_king'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor10-application-build-20260928' -Filter $filter
# Historical execution record: apply intentionally refuses to rerun on the now-applied baseline.
python -B -X utf8 'Balance Harness/analysis/check-floor10-application.py' apply --package 'TestResults/tower-floor10-application-owner-20260928' --output 'TestResults/tower-floor10-application-20260928' --artifacts 'TestResults/tower-floor10-application-build-20260928'
python -B -X utf8 'Balance Harness/analysis/check-floor10-application.py' verify --package 'TestResults/tower-floor10-application-owner-20260928' --manifest-pin 'b2bb16c69e25412488caed6a09916c336012ca2322db69f9d39a45f7addc90d7' --receipt 'TestResults/tower-floor10-application-owner-20260928/independent-audit.json'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-floor10-application-build-20260928' -Filter $filter
git -c core.safecrlf=false diff --check
```

Python denotes the bundled runtime. Study archives, build output and application evidence remain local under ignored `TestResults` and are absent from a clean checkout. Further read-only audits need a new receipt path. This operation modifies no earlier sealed fixture, script or archive; unrelated equipment-migration work is preserved.

Changed files: the two floor-10 content values, the new opt-in application fixture, the guarded Python application/audit script and this report. The confirmation, finer-calibration, equipment-baseline, floor-11 application and search guides now point to the completed application and next progression question.

## What this establishes and what comes next

The fresh confirmation remains the balance evidence: the alternating armor-and-health party won **55/256 (21.48%)**, with approximate simultaneous adjusted bounds **14.75–30.20%**. The repeated party won **23/256 (8.98%)**; the other 19 combinations won zero. Only one tested combination establishes 10% viability. This application adds integration evidence, not broader build diversity or proof that six Essences are necessary.

Both floor-10 and floor-11 calibrated settings are applied locally under their declared reference budgets. The subsequent [carried-equipment screen](Tower-Carried-Equipment-Screen-20260928.md) completed and independently verified **14,592 historical fights**: all **228 floor-11 combinations won 32/32** with Legendary / Masterpiece / rank-5 gear, including the first ten members of both floor-10 armor-and-health six-Essence parties. The subsequent carried-equipment calibration and fresh confirmation addressed that transition; the [checked local application](Tower-Carried-Equipment-Checked-Application-20260928.md) now sets floor-11 Health **17.94375** / Power **23.0175**, with all **88,064 inputs** and **470 complete reports** matched. It retains the requested repeating curve and matching seventh-Essence upgrades of those floor-10 parties. Lower-Essence separation on floor 10 and broader build diversity also remain untested or limited. The supported search does not need another algorithm experiment for this next step.
