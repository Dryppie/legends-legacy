# Placement accounting: shared content-provider reads

The offline Tower harness now supplies an optional counting reader to the shared content providers it uses. The change covers content reads and parsing that previously bypassed the harness counters, including repeated Tower floor loads and nested equipment-item parsing. Verification passes 140 distinct backend tests and 109 distinct Python tests. This extends the [native-read and owner work](Tower-Loadout-Placement-Accounting-Coverage.md); it does not establish whole-process coverage or a current resource forecast.

The necessary resource gate remains **1,806 seconds against a 1,800-second cap**. Both compressed launch guards remain closed. No cost experiment, scientific reservation, production entropy, native study preparation, qualification or combat was performed. Correctness-test durations do not reduce historical floors or charges.

## Reader boundary and dependency direction

`Services.LL.Content.ContentJsonReader` provides the file and JSON operations already used by these providers: text reads, read streams, string/stream deserialization and document parsing. Its default implementation calls the same `File` and `System.Text.Json` APIs with the provider's existing options. Each affected constructor or loader accepts an optional reader; existing service callers use the default without registration or configuration changes. Provider validation, null fallbacks, read order and catalogs are unchanged.

The counting subclass, `TowerContentJsonReader`, lives entirely in the offline harness. It forwards those operations to `TowerWorkAccounting`; no Infrastructure or Core code depends on the harness. `TowerWorkAccounting.ParseDocument` accounts for the document parser used to select equipment items. The reader records the actual reads and parses, including failures, instead of estimating provider work from file sizes after loading.

| Shared provider | Counted boundary supplied by the harness |
| --- | --- |
| `JsonEssenceDefinitionRepository` | Essence and ability text reads, followed by both parses |
| `JsonCreatureEssenceLootTableRepository` | Loot-table read and parse |
| `JsonCreatureAbilityDefinitionProvider` | Creature ability-profile read and parse |
| `RegionCreatureScalingProvider` | Region combat-balance read and parse |
| `JsonAbilityCatalogProvider` | Abilities, statuses and summons reads and parses |
| `JsonStarterEquipmentCatalog.Load` | Starter, named, style, set and item files; item document parsing and each selected equipment object's reparsing |
| `JsonCombatStyleCatalogProvider` | Style catalog read and parse, including repeated materialization/validation loads |
| `JsonWorldTowerDefinitionProvider` | Existing stream read and stream deserialization of the floor catalog |

The harness supplies this reader from `OfflineContent`, `TowerBattleRunner`, `TowerBenchmark`, `TowerBossDiscoveryContract`, `TowerBossInventory` and `EssenceMechanicsInventory`. Existing countable `HarnessJson` reads remain in place. No cache, changed file format, content edit, search-policy change or scientific launch activation is introduced. The economic `LoadOrdinary` path and unrelated providers are outside this Tower study scope.

Text and stream behavior remains distinct. Text loading retains BOM detection; the Tower floor provider retains stream deserialization. Parse counters measure UTF-8 bytes of text handed to the parser, or bytes delivered by a stream. Raw file bytes can differ when encoding or BOM handling differs. Nested equipment parsing counts its exact original object text again as parser work, without pretending that it caused another file read.

## Verification and captured evidence

The [verification package](../TestResults/loadout-placement-content-accounting-verification-20260924) retains source snapshots, logs, TRX results, the native/Python exchange, a content fixture and six small diagnostic process observations. Its captured content fixture contains the 16 source files used by the test composition and input readers, an explicit read sequence, the exact equipment fragments and a native counter receipt. The [fresh plain/compressed study fixtures](../TestResults/loadout-placement-content-accounting-fixtures-20260924) are separate. All packages are immutable after sealing; future mutation tests must generate new directories.

The content fixture establishes **13 file reads**, including the ability catalog twice, with **779,372 returned file bytes**. Its 13 initial parses plus 30 selected equipment-object parses produce **43 completed parses** and **790,826 parser-input bytes**. An independent Python check authenticates the fixture's external manifest pin, sums the actual captured files, and extracts the original equipment-object spans without reserializing them. The native receipt uses explicit synthetic request/producer test bindings; the surrounding manifest and source snapshots bind this verification. These are fixture-specific operation counts, not time coefficients or a full-study workload bound.

The 23 new backend cases compare default and injected provider outputs for all eight providers, reject malformed input with the same exceptions, preserve partial work when a later file is missing, preserve completed parsing before validation fails, exercise configured content roots with UTF-16/BOM text, preserve floor-stream BOM behavior, and verify handle cleanup. Composition tests verify repeated ability reads, nested equipment parsing, repeated floor/guardian reads during real input materialization, and both style-catalog reads during materialization and validation. A guard forbids combat in every content fixture. Existing service-registration and catalog validation tests also pass with the default reader.

Backend verification comprises 138 focused tests and two full study cases. Python verification comprises three new content checks, 38 collector/process/owner tests, 20 codec tests, 25 owner/arithmetic regressions, seven provenance tests, 14 storage integration cases and two complete-study counter checks. All 60 encoded members match their plain logical bytes and hashes, and both complete study results match with `Inconclusive` as expected. The independent audit reconstructs all 18,816 literal observations.

The full study fixtures retain their narrower native callbacks; they do not claim to exercise every production-audit content path. The new content tests separately exercise the real providers and `CreateInput` paths. Native production-audit coverage across an entire study still needs its own fresh fixture and coverage proof.

The initial backend build hit the sandbox's NuGet.Config access restriction. The approved retry built successfully but exposed one test assertion that required exactly `JsonException`; the existing document parser throws its derived `JsonReaderException`. The test now compares the default and instrumented exception type and message, preserving existing behavior. The final run passes with existing unrelated warnings and no build errors. No required verification command remains blocked.

```text
build/run-tests.ps1 -Filter '((FullyQualifiedName~BalanceHarnessContentAccountingTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessEvidenceCodecTests|FullyQualifiedName~BalanceHarnessEvidenceStorageTests|FullyQualifiedName~BalanceHarnessProposalNativeTests)&FullyQualifiedName!~Complete_placement_study)|FullyQualifiedName~EquipmentContentRegistrationTests|FullyQualifiedName~WorldTowerTests.CatalogReleases|FullyQualifiedName~CombatStyleFoundationTests.Catalog_|FullyQualifiedName~CombatStyleFoundationTests.Current_catalog_requires' -ArtifactsPath .artifacts/loadout-content-accounting-20260924
build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessEvidenceStorageTests.Complete_placement_study' -ArtifactsPath .artifacts/loadout-content-accounting-20260924
python -B -X utf8 build/test-proposal-content-work.py --fixture TestResults/loadout-placement-content-accounting-verification-20260924/content-fixture --manifest-pin <externally-checked-files.json-SHA256> -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 "Balance Harness/analysis/test-proposal-evidence-codec.py" -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
python -B -X utf8 build/test-proposal-evidence-storage.py --fixtures TestResults/loadout-placement-content-accounting-fixtures-20260924 -v
python -B -X utf8 build/test-proposal-study-work.py --fixtures TestResults/loadout-placement-content-accounting-fixtures-20260924 --output TestResults/loadout-placement-content-accounting-verification-20260924/full-study-work -v
```

These commands record completed verification, not permission to mutate the now-sealed fixtures. Test-only `LL_CONTENT_ACCOUNTING_EXPORT`, `LL_WORK_ACCOUNTING_EXPORT`, `LL_EVIDENCE_STORAGE_EXPORT`, `LL_WORK_ACCOUNTING_EXCHANGE` and `LL_WORK_ACCOUNTING_OBSERVATIONS` named fresh test destinations or exchanges. The codec regressions read the earlier immutable codec exchange through `LL_EVIDENCE_CODEC_EXCHANGE`.

## Remaining work and operational effect

Next, account for native and owner writes, copied content/runtime files, logs, and scratch creation/deletion over their full lifetimes. Add bound opt-in worker receipt emission and wire it into an enclosing owner that retains receipts through final publication and process drain. The diagnostic owner from the preceding stage remains available but the scientific launcher does not activate it. In-memory parsing and materialization beyond the explicit content boundaries also remain part of the whole-process coverage audit.

The immediate native write boundaries are `HarnessJson.WriteNew`, the battle and trial-journal writes in `TowerLoadoutArchive`, atomic pending-file publication in `TowerCompleteReservation.Storage`, the durable attempt journal in `TowerPracticalSearch.Attempts`, racing journals in `TowerAdaptiveRacingNative.Evidence`, and content copying in `TowerBundle.CopyContent`. The owner adds source copies, process logs and completion/closeout files. Accounting must preserve existing flush, sharing, atomic replacement and failure-retention behavior, including temporary bytes that disappear after a rename.

All receipts continue to declare `wholeProcessCoverage=false` and `usableForAdmission=false`. Complete instrumentation alone cannot supersede the inherited resource floor: a separately justified prospective replacement model is still required before any measurement can be considered.

There are no migrations, application configuration changes, deployments, live-history scans or historical accounting adjustments. The shared provider signatures gain optional parameters, so any future runtime packaging must rebuild dependent assemblies together; no retained runtime was replaced. All 264 inherited evidence pins were authenticated before editing and rechecked at closeout. Only line 3 of nine current-status documents is updated; historical bodies and unrelated checkout changes are preserved.
