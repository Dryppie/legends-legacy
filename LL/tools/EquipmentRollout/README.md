# Local equipment cutover

This console tool is for a stopped **local** game database. It rejects remote hosts, mismatched database/port arguments, concurrent operators and other connected database clients. It does not start hosted workers or deliver outbox messages. The connection is supplied only through `LL_LOCAL_ROLLOUT_CONNECTION`; credentials are not written to evidence.

Build with `dotnet build LL/tools/EquipmentRollout/EquipmentRollout.csproj -c Release`. Use a fresh backup and rehearse on a separate local PostgreSQL copy before touching the source. Stop the game API, worker and database clients. Every invocation requires these arguments:

```powershell
dotnet LL/tools/EquipmentRollout/bin/Release/net10.0/EquipmentRollout.dll `
  --mode settle --api-root <absolute-API.LL-project-path> `
  --evidence <private-evidence-directory> --database <local-database> `
  --port <local-postgres-port> --backup <existing-backup-file>
```

Run the modes in order with the same evidence directory:

1. `settle`: captures the original combat areas, teams and schedule generations, and freezes a UTC cutover boundary. Resolves all retained earned combat through the normal CQRS resolver using rules 17, release 1 and baseline healing. Existing offline-retention limits remain in effect. Commits bounded batches; stops each action through the normal command only after no work remains due. Failed runs can resume without re-awarding committed combat.
2. `apply`: refuses active schedules, applies pending EF migrations to the selected local database, freezes all conversion targets and operation IDs, then applies release 4 with source/result hash checks and receipts. Awards earned during settlement are included. Records with no inventory, equipped-slot, marketplace, guild or saved-loadout reference are retained and listed separately. Unknown owned items or unversioned pending rewards abort planning before item conversion begins.
3. `verify`: checks owned equipment, pending rewards, competitive snapshots and the healing coefficients under rules 18/release 4/healing-v1. Confirms that every original historical snapshot and unreferenced item/modifier fingerprint is unchanged.
4. `resume`: restores the captured teams and areas under the new rules and executes each first encounter through the normal combat service. Invalidates character state for clients. Already resumed schedules are skipped on retry.
5. `verify`: repeat after resumption, which may award new equipment.

The evidence directory contains private account/item IDs, preservation hashes, schedule state, conversion plans and receipts. Keep it outside source control. `settled.json` reports encounters processed in the successful settlement run for each character; if a process died between database commit and evidence save, its retry count may underreport prior committed work. Database schedule boundaries remain the authority for avoiding duplicate rewards.

The old strict `ItemConversionComplete` audit remains false for retained unversioned records. `ReferencedItemConversionComplete` separately describes the referenced population for the selected source release. A deployment audit must inspect every older release and pending rewards; the tool's `verify` mode checks all releases below 4. Neither readiness flag certifies gameplay balance.

Use migration receipts in reverse order for item rollback while writers are paused. Combat settlement and earned rewards are ordinary committed gameplay, so undoing the entire cutover requires the pre-cutover database backup. Do not restart old binaries while imported `LegacyImport` descriptors remain. All game hosts must use the selected `18 / 4 / healing-v1` settings when play resumes. This tool does not deploy alpha or apply changes to any external environment.
