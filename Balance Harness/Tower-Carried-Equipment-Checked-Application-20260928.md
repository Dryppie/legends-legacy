# Checked local application of carried-equipment floor 11 — 28 September 2026

**Current progression follow-up:** the [supply-chest path](Tower-Equipment-Supplies-Implementation-20260928.md) is implemented locally. Continue with acquisition pace and earned-inventory validation using the [handoff](Tower-Continuation-Handoff-20260928.md). The application evidence below retains its original scope and pins.

**Applied locally and independently verified.** Floor 11 now uses **Health 17.94375 / Power 23.0175**. The current build matches all **88,064 confirmed inputs** and **470 complete reports**, covering every one of the 344 recipes and both observed outcomes where available. Only the two intended floor-11 fields changed.

Target: the primary game's Tower content and offline Balance Harness. Apply the [freshly confirmed carried-equipment setting](Tower-Carried-Equipment-Confirmation-20260928.md) to the local checkout, preserving the supported search and repeating ten-floor equipment curve.

## Frozen application protocol

Change exactly two numeric tokens in `LL/src/API/API.LL/Data/world-tower/tower-floors.json`: floor-11 guardian Health **6.525 → 17.94375** and Power (`offense`) **8.37 → 23.0175**. Preserve the rest of the document byte-for-byte, including floor 10's **12.71 / 7.13**, and all other 28 captured content files. The complete parsed floor document must equal the confirmed isolated document.

Require the independently audited **Pass**, confirmation manifest `6a0d98f41362a3428816679efc9d0e8a90a88a37789030fbc38afa0e4652427d`, result `d5334add4af7c7d334842e9ecef1779aa7447f02dbcfc312898556f7c459dbc9`, and local preimage `dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124`. Freeze the request, current runtime and source hashes, before writing the two values. Build and run relevant ordinary regressions before application.

Using a separate current build, prepare all **344 recipes** with combat disabled and reconstruct all **88,064 confirmed input hashes**. Preserve each recipe's characters, positions, Essence order, equipment and 256 historical seeds. Replay the first archived win, where observed, followed by the first archived loss for every recipe. The 126 mixed-outcome recipes and 218 loss-only recipes yield exactly **470 replays: 126 wins and 344 losses**. Compare every complete report, not just outcomes. No fresh seeds, retries, extensions or additional balance inference are permitted.

Bounds: **900 seconds** native execution, **960 seconds** owned process, **512 MiB** output, **470 maximum fights**. Compilation, ordinary tests and independent read-only audit are separate. Any failure stops and retains the edit and evidence for inspection; no automatic retry or rollback. The owner must drain its process tree. Independently authenticate the output, reconstruct the first-occurrence replay selection, verify all input/report matches and the exact two-field change, then rerun the ordinary regressions against applied content.

The existing **835,319-value exclusion union** remains unchanged. This integration does not add statistical evidence. Acceptance remains limited to the confirmation's fixed family and ownership assumptions; acquisition feasibility, broad build diversity and later ten-floor blocks are separate questions.

## Implementation

- [BalanceHarnessCarriedApplicationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessCarriedApplicationTests.cs): opt-in current-build preparation, all-input reconstruction and full-report replays, enabled only by `LL_CARRIED_APPLICATION`.
- [check-carried-application.py](analysis/check-carried-application.py): guarded two-token edit, frozen request and bounded owner, plus a read-only independent verifier.

This protocol was written before the local application. The completed execution and verification results are recorded below. No dependency, application-setting or migration change is required. No database operation, deployment or service restart is authorized by this application.

## Preparation verification

The separate current build succeeded with **45 warnings and zero errors**. **238 regression tests passed**, with six opt-in studies skipped, through `build/run-tests.ps1`. The filter covers World Tower, production preparation, equipment profiles, progression preview/upgrades, carried-equipment calibration/confirmation, region bosses and Mad King. The [build/test log](../TestResults/tower-carried-application-preparation-20260928/build-and-tests.log) and [preserved pre-application TRX](../TestResults/tower-carried-application-preparation-20260928/before-application.trx) retain the results.

The Python entry point passed syntax and CLI checks. A read-only check admitted the exact intended two-field delta and rejected both an unrelated floor change and an incorrect scaling value. The pinned live preimage remained unchanged until the owner launched.

## Application and independent audit

The [owner protocol](../TestResults/tower-carried-application-owner-20260928/protocol.json) freezes **10,360 input/source/runtime hashes**, the exact two-token edit and all 470 replay IDs before mutation. The [saved preimage](../TestResults/tower-carried-application-owner-20260928/floor-before.json) preserves the previous content. All five game assemblies differ from the confirmation build; the check therefore establishes compatibility by reconstructing the inputs and full reports, using the newly built current assemblies. [The execution scope](../TestResults/tower-carried-application-20260928/scope.json) records the actual hashes and sanitized settings: attribute rules 18, equipment balance 4, `healing-v1`, the captured threat rules and checkpoint interval 10.

The [preflight](../TestResults/tower-carried-application-20260928/preflight.json) confirms **344 prepared recipes**, **88,064 input matches** and **zero preparation fights**. The [coverage receipt](../TestResults/tower-carried-application-20260928/replay-coverage.json) confirms 126 wins and 344 losses. Every selected complete report matched, including participants, checkpoints, statistics and outcome. The [native result](../TestResults/tower-carried-application-20260928/result.json) is **Verified** after **245.60 seconds**, with 470 completed replays, zero fresh seeds and zero retries.

The [process owner](../TestResults/tower-carried-application-owner-20260928/process.json) finished in **251.39 seconds**, exited successfully and drained all **eight processes**. The enabled fixture passed its single test; the [application log](../TestResults/tower-carried-application-owner-20260928/application.log), [preserved TRX](../TestResults/tower-carried-application-owner-20260928/application-tests.trx) and [completion receipt](../TestResults/tower-carried-application-owner-20260928/completion.json) retain the execution evidence.

The [independent audit](../TestResults/tower-carried-application-owner-20260928/independent-audit.json) passed on its first run. It authenticated the complete output inventory, all input matches, all 470 full reports, and independently reconstructed the first archived win/loss selection for every recipe. It verified exactly two changed fields, parsed equality with the complete confirmed floor document, and byte equality of the other **28 content files**. The sealed output occupies **170,574,352 bytes**, below 512 MiB. This audit executed zero fights.

Applied floor-file SHA-256: `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`.
Application archive manifest: `bf09ba2a1bc65482da0a6a48685fa798551ba64042d697974addfe02268d1127`.
Application result SHA-256: `ee8e271c446cda0fd1b2761de0dcc78a69ee03d128062b2ae3476370d152f77c`.

## Regression verification and commands

The same **238 regressions passed after application**, with the same six opt-in studies skipped. The [post-application log](../TestResults/tower-carried-application-preparation-20260928/after-application.log) and [TRX](../TestResults/tower-carried-application-preparation-20260928/after-application.trx) retain that run. These are the same tests run before and after, not 476 distinct tests. The application fixture ran separately with its opt-in environment enabled and passed once.

Final checks passed for **138 local documentation links**, Python syntax/CLI, whitespace in all **10 scoped files**, and `git -c core.safecrlf=false diff --check`. All **10,360 frozen hashes** remained intact after the regression run. The audit authenticated 508 manifest entries plus the manifest itself. No required command remains blocked or unrun.

Commands executed from the repository root, with `python` denoting the bundled runtime:

```powershell
$filter = 'FullyQualifiedName~WorldTower|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessCarryForwardEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~BalanceHarnessCarriedCalibrationTests|FullyQualifiedName~BalanceHarnessRefinedCarriedCalibrationTests|FullyQualifiedName~BalanceHarnessCarriedConfirmationTests|FullyQualifiedName~BalanceHarnessCarriedApplicationTests|FullyQualifiedName~RegionBossDefinitionProviderTests|FullyQualifiedName~Mad_king|FullyQualifiedName~mad_king'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-carried-application-build-20260928' -Filter $filter
# Historical execution record: completed owner/output directories cannot be reused.
python -B -X utf8 'Balance Harness/analysis/check-carried-application.py' apply --package 'TestResults/tower-carried-application-owner-20260928' --output 'TestResults/tower-carried-application-20260928' --artifacts 'TestResults/tower-carried-application-build-20260928'
python -B -X utf8 'Balance Harness/analysis/check-carried-application.py' verify --package 'TestResults/tower-carried-application-owner-20260928' --manifest-pin 'bf09ba2a1bc65482da0a6a48685fa798551ba64042d697974addfe02268d1127' --receipt 'TestResults/tower-carried-application-owner-20260928/independent-audit.json'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-carried-application-build-20260928' -Filter $filter
git -c core.safecrlf=false diff --check
```

Changed files: the two floor-11 content values, the new opt-in application fixture, the guarded Python owner/auditor and this report. Six related reports/guides now link to the completed application: carried confirmation, finer carried calibration, equipment baseline, earlier floor-11 application, floor-10 application and supported-search guide. Unrelated equipment-migration and LiveOps work is preserved.

## Scope and remaining progression work

Both new values multiply the previous floor-11 Health/Power by **2.75**, retaining their ratio. The edit belongs to Serevin's floor-specific scaling and leaves the shared creature definition intact. The requested ten-floor equipment curve remains Rare/Standard/rank 2 for positions 1–3, Epic/Fine/rank 3 for 4–6, Unique/Exceptional/rank 4 for 7–9, and Legendary/Masterpiece/rank 5 for position 10. Equipment already owned remains available across the transition. The supported search remains `affinity-creation-with-benchmark-validation-v1`.

The fresh confirmation supplies the balance evidence: the strongest intended combination won **93/256 (36.33%)**, with simultaneous adjusted bounds **25.92–48.19%**; the strongest lower-Essence control won **1/256**, upper bound **6.05%**. Seven related combinations established 10% viability. This application adds integration evidence only. It does not establish broad build diversity, universal seven-Essence necessity, equipment acquisition feasibility or balance in subsequent ten-floor blocks.

The subsequent [floor-10 lower-Essence screen and acquisition audit](Tower-Floor10-Lower-Essence-Controls-20260928.md) completed **8,736 historical fights**. Every one of the **252 five-Essence controls won 0/32**, including 126 at the same level and tier as the six-Essence references. The strongest references won 6/32 and 5/32. Independent audit preserved all recipes and verified the acquisition arithmetic: the full 105-item party costs **36,000 Parts and 80.28 million Cinders** to reinforce from dungeon rank 1 to rank 5. Gear availability is now the main unresolved progression assumption. These are historical diagnostics, not universal Essence necessity or new balance acceptance. Another scalar sweep or search-algorithm experiment is not indicated by these results.

No dependency, application configuration or migration changed. No database operation, deployment or restart occurred. The Tower definition provider loads content into a singleton; a running service needs its normal restart/reload as part of a separately authorized deployment to observe this edit. No such operation was performed.

Earlier sealed studies and application evidence retain their original pins and dated scopes. Their live-content preconditions describe the pre-application checkout; do not repin those studies to the new file. The saved before/after documents and this application record explain the intentional state transition. Ignored `TestResults` archives are local and absent from a clean checkout; a later read-only audit requires a new receipt path.
