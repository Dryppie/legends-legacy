# Automatic equipment conversion at startup

The game API performs the release-4 conversion after its existing EF migrations, seeding and content validation, before it
starts serving requests or its hosted workers. The independently deployed worker is not paused or changed by this runner.
The selected scope is conversion and verification; no old-rule combat settlement or maintenance orchestration is performed.

## Deployment

Deploy the rebuilt game API with its shipped configuration:

```text
AttributeRedesign:LiveVersion = 18
EquipmentBalance:LiveVersion = 4
Combat:AbilityBalanceProfile = healing-v1
EquipmentConversion:RunOnStartup = true
EquipmentConversion:TargetBalanceVersion = 4
```

Keep the API, worker and LiveOps binaries and rule/profile selectors aligned. Environment variables override the JSON file;
the conversion keys use `EquipmentConversion__RunOnStartup` and `EquipmentConversion__TargetBalanceVersion` in that form.
The runner rejects mismatched attribute/equipment selectors or healing profile. The explicit target is limited to this release; changing a
future balance catalog does not silently authorize another equipment rebalance.

Startup can take longer while converting a populated database. If a deployment startup timeout restarts the API, it resumes
from the remaining population. A successful startup logs `Equipment release 4 verified`, with conversion, Arena refresh and
retained-record counts. Subsequent restarts verify again without reapplying completed conversions.

## Conversion and verification

- Selects older releases and unversioned equipment referenced by inventory, equipped slots, marketplace, guild vault or saved
  loadout. Includes versioned unclaimed dungeon rewards. Ignores already-current/newer items and retained unreferenced rows.
- Uses a dedicated PostgreSQL advisory lock to serialize API startup runners. Each item uses the normal command transaction,
  character command locks and item/dungeon row locks. Eligibility and current version are rechecked after acquiring locks.
- Reuses the existing authored migration mapping, ownership checks, before/after receipts, specialization allowance and
  outbox invalidations. Preview and apply happen inside the same transaction. A committed item is its own durable progress
  marker; a rolled-back transaction leaves no partial item/receipt/outbox update.
- Reads the first remaining page repeatedly, so successful conversions cannot cause later items to be skipped.
- Refreshes valid Arena defenses whose rules or frozen equipment are old through the existing defense snapshot command.
  This appends a snapshot and updates the active reference; it does not rewrite historical combat snapshots.
- Verifies that no targeted gear, unsupported unversioned pending rewards, old Arena defenses or active legacy tournament
  snapshots remain. Counts retained unreferenced records separately rather than treating them as failed owned conversions.

Failures name the blocked item or remaining category in the API startup log and stop that API instance from starting.
Resolve the reported data issue and restart; successful earlier item transactions remain committed. Frozen active tournament
snapshots require finishing/cancelling those tournaments through the existing workflow. The runner does not cancel them.
The previous LiveOps preview/apply/rollback endpoints retain their normal combat mutation safeguards; the internal startup
command that skips settlement is not exposed as an HTTP endpoint.

## Operational scope

This is not a global maintenance or version barrier. Existing API/worker processes can keep running; combat between item
transactions can observe partially converted builds. Pending encounters are not guaranteed to use their pre-update build.
The final audit reflects the committed database state when it runs, not a guarantee against old binaries creating older
equipment afterward. Deploying matching current binaries across game hosts remains necessary.

No new schema migration or external infrastructure changes are introduced. Alpha has not been inspected, modified or
deployed by this implementation.

## Verification

Run backend checks through `build/run-tests.ps1`. Focused tests cover resumable paging, duplicate/no-op conversion, preserving
scheduled combat, disabled/mismatched startup configuration, failed Arena refresh and every final-audit blocker.
`build/run-postgres-equipment-rehearsal.ps1` also runs the startup rehearsal in a disposable local PostgreSQL database using
the actual command pipeline, alongside the existing migration/rollback rehearsal.

Verified on 28 September 2026:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~EquipmentStartupConversionTests|FullyQualifiedName~EquipmentMigrationTests|FullyQualifiedName~AttributeRolloutRegistrationTests|FullyQualifiedName~StateSyncCommandScope'
./build/run-postgres-equipment-rehearsal.ps1 -PostgresBin 'C:/Program Files/PostgreSQL/17/bin' -Port 55443
```

The focused run passed **99 tests**; the disposable PostgreSQL run passed **both integration rehearsals**. The startup
rehearsal exercised real command transactions, concurrent distinct operation IDs, transaction interruption, the runner lock,
release-1/2/3 and unversioned gear, pending rewards, an unchanged active schedule, Arena refresh from rules-18 snapshots with
legacy gear, verification blockers, restart/retry, and preservation of old snapshots, current items and unreferenced rows.
Builds completed with existing repository warnings. Logs are in `TestResults/equipment-startup-tests.log` and
`TestResults/equipment-startup-postgres.log`. No test required alpha access; no full backend-suite or alpha verification is
claimed.
