# Attribute redesign implementation record

Scope: the primary LL game, following the 25 September 2026 analysis. No services are deployed and no shared database is changed. Continuation completed on 27 September is documented below. Existing unrelated working-tree changes are preserved.

## Implementation and release status

The subsequent [versioned equipment rebalance implementation](equipment-rebalancing.md) adds independently selected equipment releases, historical upgrade catalogs and repeatable preview/apply/rollback. Release 3 records the reviewed penetration price of 4 as an inactive candidate; equipment release 2 and the live defaults remain unchanged.

The later [penetration price review](penetration-price-review-2026-09-27.md) completed 62,944 study fights and recommends 4 budget per point for the next candidate. This review changes only analysis tooling and records; the source price remains 1.5 and live rules remain 17.

The code and operational tooling for phases A–H are implemented. **Live rules remain 17 by default.** The copied-local-database rehearsal completed for all 87 supported items. Its audit found 469 unversioned items and one retired archetype requiring an explicit migration or retirement decision before activation. Balance acceptance also remains a release gate; this work has not activated the redesign.

| Phase | Delivered | Remaining acceptance work |
|---|---|---|
| A | Stable attribute IDs; explicit 17/18 rules; frozen item/snapshot compatibility; old content catalogs and archive readers | Local-copy rehearsal preserved all 1,082 historical snapshot rows; replay execution is covered separately by archive tests |
| B | Finite defense, haste, Restoration, Tenacity, effective caps, root/target/tick proc scopes and explicit secondary/periodic opt-ins | Review encounter-specific balance findings |
| C | Versioned prices, 70/30 core/specialty allocation, charged style/set reserves and allocation metadata | Calibrate candidate prices and triggered-set reservations against wider encounter coverage |
| D | Data-driven legal specializations, compatible slot/style pools, mutually exclusive premium stats and archetype-preserving drop probabilities | Observe real award and decision cohorts after an approved activation |
| E | Server-authoritative activity/loadout comparisons, raw/effective/cap waste, EHP and cadence metrics, set/hand changes and one-time migrated specialization choice | Product review with real player builds |
| F | Transactional outbox observations, stable IDs, retention, daily cohorts, ordered Essence usage, stat bands, cap waste, pair/triple combinations and decision windows | Review population cohorts; PostgreSQL delivery/reporting and eligible-alternative denominators are now implemented and locally verified |
| G | Bounded production-engine studies, fixed content/execution hashes, equal budgets, held-out seeds, mirrored PvP, raw diagnostics, synthetic tiers and per-context Pareto/ranking outputs | Candidate balance is not certified; see the measured warnings below |
| H | Audit, preview, idempotent apply/rollback, one credit, player UX, EF migration and a local rehearsal script | Seeded and copied-local-data HTTP rehearsals passed for supported items; resolve 469 unversioned instances and one retired archetype before full conversion |

## Changed systems and design decisions

### Domain and combat

`LL/src/Core/Domain/Models/Attributes/{AttributeRules,AttributeRulesSelection,AttributeCatalog,AttributeType}.cs` and `Components/Attributes/AttributeCalculator.cs` define units and version selection. New attributes are appended without renumbering existing IDs. Current defense keeps a finite normalized rating: Corrosion reduces the rating first, then the `0.8R/(R+165)` curve produces mitigation. Following the user's 27 September decision, typed penetration subtracts up to **40 percentage points** from that mitigation, floored at zero. Thus 60% mitigation with 40 penetration becomes 20%. Block and general damage reduction remain separate. Each item's tier normalizes its own rating. Authored percentage defenses are converted once, including bases carrying new zero-valued rating defaults. Changing character level alone does not devalue existing defense gear.

The [penetration follow-up](attribute-penetration-follow-up-2026-09-27.md) records versioned caps, historical compatibility, verification and the new balance warning. Version 17 retains rating-based penetration and its 60% cap. Bootstrap descriptions, effective attribute projections and item allocation caps follow their selected rules version. No schema or activation change accompanies this adjustment.

Whole-loadout legacy CDR converts to haste once. Temporary modifier removal restores raw overcap contributions. Haste affects initial, repeated and reset active cooldowns; passive internal cooldowns remain authored. Restoration affects authored healing/barriers, excluding life steal and regeneration. Tenacity follows condition-specific duration/magnitude rules, including Doom and boss Stagger exceptions.

`AbilitySpec`, the compiler/runtime/scaler and `FastCombatEngine{,.Procs,.Reaper}.cs` carry proc eligibility and finite defenses through execution. Zero proc coefficient leaves damage intact and suppresses downstream listeners. Multi-hit actions do not silently multiply default listener opportunities. New compact telemetry includes direct/periodic health damage, first action, longest control chain, prevented harmful duration, overhealing, barrier overcap and unused barrier expiry.

`CombatSetupService`, snapshot persistence/materialization, `CharacterService` and service DI share the live selector. Old snapshots default to 17. `CombatEngineExecutor` rejects mixed-version competitive battles; legacy-versus-legacy execution remains supported. New optional result fields are omitted when absent to preserve historical archive round trips.

### Equipment and migration

`LL/src/Core/Domain/Models/Items/Equipments/Progression/` contains versioned prices, specialization rules, allocation metadata and pure conversion policy. `EquipmentSetDefinition` and the set resolver retain old sets alongside versioned current identities. Frozen descriptors retain their balance version during reinforcement and transfers. Current descriptors must contain a legal allocation.

`LL/src/API/API.LL/Data/equipment/equipment-{starters,styles,sets}.v1.json` now supplies content balance 2; `*.legacy-v1.json` preserves balance 1. The existing filenames remain loader entry points, while the version is explicit in content and state. Core/specialty budgets, overflow and set reservations are visible rather than hidden in free set bonuses. Existing style premiums still affect total item budget; equal-budget experiments account for them.

`JsonStarterEquipmentCatalog`, award selection and the reference factories support specialization variants without increasing an archetype's probability merely because it has more variants. Historical rating diagnostics deliberately retain their old formula/price contract and are not used as an authoritative upgrade recommendation.

`EquipmentMigrationService`, `EquipmentMigrationRepository`, receipt configuration, individual Application commands/queries and the SuperAdmin LiveOps controller implement conversion. Character/item/run locks and the existing command transaction coordinate writes. A preview hash prevents stale conversions. Receipts retain original/new JSON, identity, actor, hashes and one-time choice use; retries reuse the same operation ID. Changed equipment and spent credits cannot be rolled back blindly. No currency is created as hidden compensation.

The migration, generated SQL and local script are described in [the rollout runbook](attribute-redesign-rollout.md). Inventory, equipped, marketplace, guild and pending dungeon descriptors use the same pure policy. Unversioned gear is an explicit audit blocker. Historical snapshots are retained; active competitive snapshots need a controlled refresh.

### API and presentation

`CompareEquipmentQuery` compares the complete resulting loadout, including hand replacement and set thresholds, under the selected activity's equipment and Essence loadouts. It reports raw/effective values, useful weapon-specific attack-speed caps, unused budget and conditional derived metrics rather than one universal score. Active cooldown estimates include ascension; temporary battle effects and conditional outcomes are identified as limitations.

`ObserveEquipmentComparisonCommand` records a successful authoritative comparison via the outbox. The client supplies a request ID and candidate/context, not trusted stats. State-sync scopes are explicitly registered for migration writes; comparison observations do not cause unnecessary refresh loops.

Both frontend attribute enums were extended. The primary Angular equipment service, item display, inventory modal and grouping pipe expose allocation and comparison details. The new `migrated-specialization` component previews legal choices, keeps a stable operation ID across retries and refreshes inventory/equipment after success. Async comparisons cancel stale subscriptions and offer visible retry states.

### Telemetry

Domain observations use canonical build/equipment hashes and omit player names. Capture points include awards, manual equip/loadout changes, upgrades/dismantles, listings/transfers, migration and committed combat execution. Offline simulations do not emit observations. The outbox consumer deduplicates inserts with a stable primary key.

Daily reports retain distributions, cap waste, stat pairs/triples, ordered Essence/ascension usage, Doctrine/context/tier cohorts and attribute-band outcomes. Repeated fights are grouped by character. Intervals require at least 30 characters and use a conservative bound that does not report certainty from 30 identical winners. Seven-day item cohorts track first equip, sustained use, observed equipped duration and dismantling. The existing daily job recomputes recent reports and enforces 30-day raw / 13-month aggregate retention.

`ItemizationChoiceContext`, `ItemizationChoiceRepository` and the award/equip/loadout/comparison hooks now freeze eligible owned alternatives, including equipped items and valid guild loans. They check level, ownership, current guild membership and available hand/armor slots, deduplicate two-handed equipment and exclude empty/absent inventory rows. Same-batch awards are included. Old observations without snapshots and inventories with unversioned equipment are explicitly incomplete, not empty. Reports distinguish observed awards, eligible awards and choices with alternatives; stat selection is conditioned on recorded availability.

Manual equip observations use the committed slot, including automatic hand selection and the repository's existing fallback behavior, so off-hand-only alternatives cannot inflate a main-hand decision.

These remain observational denominators. Tier, quality, other stats and player selection can differ, so the reports do not establish causal preference or infer demand from drop frequency.

### Balance tooling

`LL/tools/BalanceHarness/AttributeAllocation{Study,Diagnostics}.cs`, `AttributeDungeonBattleRunner.cs` and the `Fixtures/attribute-allocation-*.json` requests run actual preparation and combat execution. Dungeon fixtures use fixed boss rooms rather than incorrectly spawning every entry from an ordinary-room sampling pool. Future-tier projection only widens a frozen offline catalog, never live content.

The earlier screens completed **10,880 fights**, plus four deterministic replay checks. These and the 8,960-fight follow-up below predate the flat penetration change; they establish behavior of the earlier candidate, not balance acceptance for the revised formula. Confirmation uses independent seed clusters, with content/request/execution identities retained. The first detailed current exploration and confirmation replay files matched byte for byte after rerun. See [simulation results](attribute-redesign-simulation-results.md) for artifact hashes, before/after tables and interpretation.

The [27 September follow-up](attribute-redesign-follow-up-2026-09-27.md) adds 8,960 fights covering matched opponents, three-player support, control, summons, three physical weapon layouts, Tenacity and matched set thresholds. Another 8,960 executions from a retained executable reproduced every trial and detailed replay byte for byte. PvP parties support up to five actors per side; preflight validates all Doctrines, and party-health rankings exclude summons.

The healer warning was primarily a win-to-timeout tradeoff: Restoration improved sustain and avoided deaths. It also improved wins in the party-support fixture. Haste/precision remain strong for casters; speed wins basic-attack cases. Candidate prices were retained rather than tuned to make every role win the same encounter. Real-player impact, multi-wave attrition and broader set pricing remain acceptance work; no automatic promotion occurred.

## Verification on 27 September

The broad verification below predates the penetration adjustment. The later change passed a Release build, **124 focused tests**, and **466 related regressions with one expected opt-in PostgreSQL skip** through `build/run-tests.ps1`. A bounded comparison ran 640 fights using each formula (1,280 total); see the penetration follow-up for evidence and limits. The full backend/Angular suites and database rehearsal were not repeated for this formula-only adjustment.

- Full Angular suite: **846 passed** after supplying the app-shell unit test's ActivityDay dependency.
- Seeded PostgreSQL 17.11 integration: passed, including the reusable create/test/stop script. It exercised full schema history, migration Up/Down, item and pending reward conversion/rollback, concurrent replay, interrupted transactions, JSONB, outbox atomicity, deduplication and daily reports. It used only disposable local databases.
- Existing local database copy: **87 supported items** passed HTTP preview/apply/retry/rollback/retry and source-hash restoration. Relational modifier and ownership-link fingerprints and all 1,082 historical snapshots matched afterward. The run found and fixed explicit LiveOps handler registration, native blueprint style conversion, structured unsupported-preview errors and exact receipt timestamp replay at PostgreSQL precision. **40 focused tests** and the strengthened real PostgreSQL integration passed. The remaining 469 unversioned items and one retired `plain.cloth_cowl` are explicit blockers, not silently converted records. See the rollout runbook for coverage and evidence.
- Final telemetry, study-preflight, Stagger/protection and historical-reference checks: **67 passed**. Separate historical fixture checks passed **98 tests**; process publish/audit cases passed after allowing their declared worker budget plus bounded publication time.
- Final standard-layout backend correctness and affected archive checks: **2,873 passed, 2 expected skips**, including the last equip-slot correction and precision archive tests. The skips are the existing AlphaSignet integration test and the opt-in PostgreSQL test, which passed separately. Choice-telemetry checks also passed **7 tests**, including both automatic/fallback equip-slot cases. Precision archive checks passed **12 tests** after freezing an overwhelming guardian only in their temporary content fixture; live win rates no longer determine whether archive verification can run.
- Original historical reference JSON files are now checked in and copied to test output. Their SHA-256 values were verified against the original archive seals; they were not regenerated from current behavior.
- The harness reference reader now loads only the Essence catalog needed for recipe legality, so old snapshots do not require the new equipment files. Content-growth tests validate supported/unsupported control routes rather than assuming every authored control effect supports a Stagger reservation.
- The unrestricted backend sweep, started before the repairs, finished with **6,151 passed, 18 failed and 4 skipped** in 63 minutes. Its TRX is retained at `TestResults/attribute-full-backend-20260927.trx`. Failures covered stale content counts, historical snapshot dependencies, outcome-sensitive archive setup and resource deadlines in large process/archive fixtures. The content/reference repairs passed targeted rechecks. All **8 isolated archive cases passed**, including the 44,000-report process/reconstruction and 16,500-report reconstruction failures; the TRX is `TestResults/attribute-exclusive-archives-20260927.trx`. The final Release build and **58 diagnostic, pressure and process tests passed**, with TRX at `TestResults/attribute-final-harness-repairs-20260927.trx`. Every initial failure passed a targeted recheck. The entire unrestricted suite was not repeated after the repairs; these are a completed initial sweep plus passing affected-suite rechecks, not a fresh unrestricted green run.
- Resource fixtures now run in an exclusive xUnit collection and their outer waits honor the declared worker budgets. Production time/storage limits were not relaxed. The pressure report test verifies the exact frozen catalog area IDs instead of a stale count. A TEMP-output broad test attempt was invalid because older fixtures locate the repository relative to the test assembly; that failure is not counted as product evidence.
- The four final studies contain 8,960 study fights and another 8,960 deterministic verification executions. Mirrors are paired by seed, not independent observations. Detailed requests, hashes, caveats and W/L/D tables are in the follow-up report.

No new database migration is required for the choice snapshots: they are optional versioned fields in existing JSONB telemetry payloads. The generated redesign migration remains the sole schema change. `AttributeRedesign:LiveVersion` remains 17.

Verification logs are retained together under `TestResults/attribute-verification-20260927`. These local artifacts are ignored by Git. Final `git diff --check` passed. The copied-local-data HTTP rehearsal used a read-only snapshot of the user's existing local database; a separately supplied backup was unnecessary. Its private artifacts remain in TEMP, with aggregate evidence at `TestResults/attribute-local-copy-20260927`. The source database, persistent configuration and running game API were not modified by the rehearsal.

Current verification commands (backend execution always uses the repository wrapper):

```powershell
./build/run-tests.ps1 -Configuration Debug -Filter 'Category!=BalanceHarness|FullyQualifiedName~BalanceHarnessTowerStaggerReservationTests|FullyQualifiedName~BalanceHarnessTowerProtectionCompatibilityTests|FullyQualifiedName~BalanceHarnessTowerBossReferenceTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests|FullyQualifiedName~BalanceHarnessGroupCountTests|FullyQualifiedName~BalanceHarnessGroupVariationTests|FullyQualifiedName~BalanceHarnessGroupDiversityTests|FullyQualifiedName~BalanceHarnessTowerPrecisionTests'
./build/run-tests.ps1 -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests|FullyQualifiedName~BalanceHarnessPressureTests|FullyQualifiedName~BalanceHarnessPracticalProcessTests'
./build/run-tests.ps1 -Configuration Release -Filter 'FullyQualifiedName~LiveOpsApplicationRegistrationTests|FullyQualifiedName~EquipmentMigrationTests|FullyQualifiedName~EquipmentProgressionAdministrativeGrantTests|FullyQualifiedName~EquipmentBlueprintTests'
# Supply the path to an existing local PostgreSQL binary distribution:
./build/run-postgres-equipment-rehearsal.ps1 -PostgresBin C:/tools/pgsql/bin
# In LL/src/Presentation/ll:
$env:npm_config_cache = Join-Path $env:TEMP 'll-npm-cache'
npm.cmd exec -- ng test --watch=false --browsers=ChromeHeadlessCI --karma-config=karma.conf.cjs --progress=false
```

Run archive/process resource checks without concurrent builds or test processes. Their exclusive xUnit collection prevents interference inside a test run; it cannot coordinate separate test hosts.

## Earlier verification record — 25 September

All backend tests used `build/run-tests.ps1`. Build outputs and detailed logs were kept in temporary or ignored `TestResults` directories. No package-manager cache was created in the checkout.

| Verification | Result |
|---|---|
| Broad correctness plus affected harness/archive suites | **2,937 passed, 1 pre-existing skip, 0 failed** |
| Final mixed-version guard, migration, snapshot, Arena/tournament paths | **256 passed, 0 failed** (overlaps the broad suite) |
| Equipment Angular tests including migration choice and comparison requests | **64 passed** |
| Angular development build | Passed |
| API.LL and API.LiveOps Release builds | Passed, 0 warnings/errors in final builds |
| EF pending-model check | No changes since the generated migration |
| Idempotent migration SQL generation | Passed; SQL not applied |
| Rehearsal script PowerShell parse | Passed; network/database rehearsal not executed |
| `git diff --check` | Passed |

Representative commands, run from the repository root unless otherwise noted:

```powershell
./build/run-tests.ps1 -Filter 'Category!=BalanceHarness|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBossSearchTests|FullyQualifiedName~BalanceHarnessContentAccountingTests|FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessCompactArchiveTests|FullyQualifiedName~BalanceHarnessTests|FullyQualifiedName~BalanceHarnessComparisonTests'
./build/run-tests.ps1 -Filter 'AttributeRedesignTests|EquipmentMigrationTests|AttributeAllocationStudyTests|EquipmentBlueprintTests|EquipmentComparisonProjectorTests|CombatPreparationPipelineTests|Snapshot|Colosseum|Tournament'

dotnet build LL/src/API/API.LL/API.LL.csproj --configuration Release --no-restore
dotnet build LL/src/API/API.LiveOps/API.LiveOps.csproj --configuration Release --no-restore
dotnet ef migrations has-pending-model-changes --project LL/src/Infrastructure/Persistence/Persistence.LL --startup-project LL/src/API/API.LL --context LLDbContext --configuration Release --no-build
dotnet ef migrations script 20260925111407_AddLeanTelemetry 20260925142600_AttributeRedesignReceiptsAndTelemetry --idempotent --project LL/src/Infrastructure/Persistence/Persistence.LL --startup-project LL/src/API/API.LL --context LLDbContext --configuration Release --no-build --output TestResults/attribute-redesign-migration.sql

dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll attribute-allocation-study LL/tools/BalanceHarness/Fixtures/attribute-allocation-current.json
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll attribute-allocation-study LL/tools/BalanceHarness/Fixtures/attribute-allocation-future.json
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll attribute-allocation-study LL/tools/BalanceHarness/Fixtures/attribute-allocation-transition.json

# In LL/src/Presentation/ll:
$env:npm_config_cache = Join-Path $env:TEMP 'll-npm-cache'
npm.cmd exec -- ng test --watch=false --browsers=ChromeHeadlessCI --karma-config=karma.conf.cjs --include="**/*equipment*.spec.ts" --include="**/migrated-specialization.component.spec.ts" --progress=false
npm.cmd run build:development -- --progress=false
```

Use new output directories before rerunning the study requests: the harness intentionally refuses to overwrite retained evidence.

The 25 September unrestricted backend suite was not certified. That earlier run encountered missing archived reference captures (`reference.json`, `variation-reference.json`, `diversity-reference.json`) and repeated process-worker timeouts; it was stopped after becoming stale while fixes continued. The final selected suites above passed. An earlier unrestricted Angular run passed 843/845 tests; two app-shell tests failed because their ActivityDay/Auth/API dependency chain lacked an HTTP test provider. Those app-shell tests were subsequently repaired; the 27 September full Angular suite is green.

## Configuration, migration and deployment implications

The migration is generated and reviewable, and has been exercised only in disposable local PostgreSQL databases. No shared database or external environment was changed. `AttributeRedesign:LiveVersion` defaults to 17; the environment variable form is `AttributeRedesign__LiveVersion`. Enabling 18 requires the maintenance/audit/snapshot procedure in the runbook. The current code does not automatically pause distributed hosts or decide balance acceptance for the operator.

Unrelated guild endpoint/query/tests and working-tree changes to `LL/tools/BalanceHarness/AFFINITY-SEARCH.md` and `Balance Harness/Tower-Affinity-Floor3-Evaluation.md` were preserved. No commit, deployment or infrastructure change was made.
