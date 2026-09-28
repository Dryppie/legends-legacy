# Completed local attribute, equipment and healing cutover

The local `legends_legacy` database on localhost:5432 now uses rules **18**, equipment release **4** and **healing-v1**. The rebuilt Development API is running at `https://localhost:7060`; `/healthz/live` and `/healthz/ready` both returned HTTP 200 with `Healthy`. No alpha or other external environment was changed.

## Applied results

| Check | Result |
|---|---:|
| Original combat schedules settled and resumed | 37 |
| Retained earned encounters resolved under old rules | 311,234 |
| Owned equipment converted to release 4 | 483 |
| Converted items earned during settlement | 333 |
| Equipment instances after cutover | 891 |
| Unreferenced legacy records retained unchanged | 408 |
| Original historical snapshots retained unchanged | 1,082 |
| Referenced unversioned equipment remaining | 0 |
| Versioned equipment below release 4 remaining | 0 |
| Unsupported or older unclaimed equipment rewards | 0 |
| Active legacy competitive snapshots | 0 |

These counts describe the verified cutover boundary; normal gameplay can produce new items afterward. Settlement used the production CQRS resolver and existing offline-retention limits. It committed bounded batches before pausing each schedule through the normal delete command. Resumption restored each captured team and area and executed its first encounter through the production combat service under the new rules.

The active healing coefficients are **105% Power for Herb Mixture** and **125% Power for Treant Saplings' Sprouting Surge**, before Restoration and other applicable effects. Release 4 prices Restoration at **1.5 budget per point** and Penetration at **4**. Rules 18 subtract Penetration from final typed mitigation in percentage points, capped at **40**, with a zero mitigation floor.

## Implementation and preservation

- `LL/tools/EquipmentRollout` adds the local operator workflow: settle, apply, verify, resume, verify. It requires an explicit local database/port, an existing backup, no other connected database clients and an advisory lock. It freezes targets and operation IDs, saves receipts and supports interrupted-run retries. See its README for invocation and recovery limitations.
- `EquipmentMigration.cs` adds `ReferencedUnversionedInstances` and `ReferencedItemConversionComplete`; `EquipmentMigrationTests.cs` verifies that unreferenced records remain visible and unsupported pending rewards remain blockers. The original strict readiness flag is unchanged.
- The rollout and legacy-migration reports now record completed local activation and retain the earlier round-trip evidence.

All originally captured historical snapshot hashes and unreferenced item/modifier hashes matched after conversion and resumption. Unreferenced records were not deleted or assigned invented owners. Their presence keeps the strict `ItemConversionComplete` flag false. Verification separately checked referenced raw items, every older release, pending rewards and competitive snapshots.

## Verification

- `dotnet build LL/tools/EquipmentRollout/EquipmentRollout.csproj -c Release`: passed, zero warnings/errors.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CharacterAction|FullyQualifiedName~IdleCombat|FullyQualifiedName~EquipmentMigrationTests|FullyQualifiedName~AttributeRolloutRegistrationTests|FullyQualifiedName~HealingBalanceCandidateTests'`: **98 passed**, zero failed/skipped.
- Full copied-database settlement, conversion, verification and resumption: passed. This copy converted 497 items; its distinct cutover boundary yielded a different amount of earned loot. An initial resume failure exposed a missing creature navigation load; it was fixed and the entire copied workflow passed before source execution.
- Actual source workflow: every mode passed, including verification after all 37 schedules resumed.
- `dotnet build LL/src/API/API.LL/API.LL.csproj -c Debug --no-restore`: passed with 28 existing warnings and zero errors.
- Both local API health endpoints: HTTP 200, `Healthy`.
- `git diff --check`: passed. No required verification command remained blocked.

Logs are under `TestResults/equipment-local-cutover-source.log`, `TestResults/equipment-local-cutover-copy-final.log`, `TestResults/equipment-cutover-regressions.log` and `TestResults/equipment-local-api-build.log`. Anonymous aggregate evidence is in `TestResults/equipment-local-cutover-20260927`.

## Backup, configuration and deployment

The fresh source recovery backup is `C:/Users/HrHoe/AppData/Local/Temp/ll-equipment-cutover-450f1ed2ebc14ff7a3ddda7d30cd5af2/source-live-before.dump`. Private per-item receipts, schedule manifests and preservation fingerprints are in that directory's `source-evidence` subdirectory. These contain operational identifiers and remain outside source control. Retain the backup and receipts before temporary-directory cleanup.

No new EF migration was authored for this workflow. `20260927132322_VersionEquipmentRebalances` was already present in the source database at preflight. The legacy snapshot envelope uses the existing JSONB receipt column.

The local API's ignored runtime configuration retains its existing connection and development credentials, with the three selectors set to `18 / 4 / healing-v1`. The running process also explicitly receives those selectors. Source API, worker, LiveOps and Admin settings already select the same combination. No standalone worker was started during this cutover.

Item receipts permit conditional item rollback while writers are paused. They do not undo earned combat, rewards or subsequent gameplay. Undoing the entire cutover requires restoring the fresh database backup in a maintenance window; do not restart old binaries against imported descriptors.

Alpha deployment remains manual and needs its own backup, population audit, old-rule settlement, item conversion and competitive-snapshot checks. The tool is intentionally local-only. The selected healing package has the remaining balance limitations documented in `healing-balance-review-2026-09-27.md`; successful rollout verification does not establish universal gameplay balance.

## Player-facing integration follow-up

A subsequent frontend review found and fixed two integration gaps after database activation:

- The character overview's section lists still contained only the retired healing/cooldown/resistance attributes. `character-overview.component.ts` now includes Restoration, Ability Haste and Tenacity and uses the server's projected attribute keys to choose visible rows. It preserves zero-valued projected stats and legacy projections, while excluding retired raw aliases from modern character displays. No combat formula is duplicated in the frontend.
- `inventory-equipment-modal.component.html` restricted the free specialization panel to equipment release 2. It now asks the server for any personal progression item; the existing endpoint decides whether an unused migration credit is available. This supports release 4 and later releases without another frontend version gate. Borrowed guild equipment stays excluded.

`migrated-specialization.component.ts` also uses the canonical equipment label/value pipes for previews, so Armor is presented as a rating and Restoration displays its percentage unit. Focused tests were added in the overview, modal and specialization component spec files, including rendered modal/choice integration, absent credits and guild ownership restrictions.

Verification from `LL/src/Presentation/ll`, with the npm cache under `%TEMP%`:

```powershell
npm.cmd run test:ci -- --include="**/character-overview.component.spec.ts" --include="**/inventory-equipment-modal.component.spec.ts" --include="**/migrated-specialization.component.spec.ts" --include="**/attribute-format.pipe.spec.ts" --include="**/essence-description*.spec.ts" --progress=false
npm.cmd exec -- ng build --configuration production --progress=false
```

All **41 focused Angular tests passed**. The production bundle built successfully to `LL/src/Presentation/ll/dist/ll`, with warnings for the initial bundle (978.06 kB against a 500 kB warning threshold) and dungeon stylesheet (22.07 kB against a 20 kB warning threshold); both remained below their error thresholds. Logs are `TestResults/attribute-rollout-ui-tests.log` and `TestResults/attribute-rollout-ui-build.log`. `git diff --check` passed. No required command was blocked. Backend tests were not repeated for these frontend-only changes; no migration, backend configuration change or further database conversion is required. Include the updated frontend in the manual alpha deployment. No external deployment or authenticated browser playtest was performed in this follow-up.
