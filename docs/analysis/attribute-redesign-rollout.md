# Attribute redesign: rehearsal and activation

## Selected release and manual deployment

### Automatic conversion on game API startup

The game API now ships with `EquipmentConversion:RunOnStartup=true` and `EquipmentConversion:TargetBalanceVersion=4`.
After its normal schema migrations, seeding and content validation, it converts older referenced equipment through
transactional commands, refreshes outdated Arena defenses and verifies the remaining live population before serving requests.
Deploying the rebuilt API runs this automatically; no per-item operator calls are required. Existing receipt tables provide
durable progress, so restarting skips committed conversions. No new EF migration is needed for this startup runner.

This path implements conversion and verification only. It does not pause other hosts or player activity, settle outstanding
combat, stop schedules, or restore them. Pending combat will use whichever build and rules are active when it is resolved.
All combat hosts still need matching release selectors and binaries. The runner serializes concurrent API conversion runs
and uses character command locks plus item/dungeon row locks, but does not make overlapping old and new deployments a single
atomic game-wide cutover.

Unknown owned equipment, unsupported unversioned pending rewards or active legacy tournament snapshots produce an explicit
startup error. Committed conversions remain intact for retry. Arena refresh creates new snapshots and preserves old ones;
frozen tournament snapshots are not rewritten or tournaments silently cancelled. Unreferenced legacy item rows are retained
and counted separately. See [startup conversion details](equipment-startup-conversion.md).

Environment overrides must retain `18 / 4 / healing-v1` and allow startup conversion. Set
`EquipmentConversion__RunOnStartup=false` only when deliberately using the older operator-controlled procedure below.

The 28 September [Tenacity follow-up](tenacity-resistance-2026-09-28.md) changes rules-18 Tenacity from duration reduction to a chance to ignore harmful applications. Rebuild all combat hosts together; earlier duration-based balance evidence does not validate this changed mechanic. It adds no database migration, equipment conversion or configuration selector.

The user selected the new attributes **and healing changes** for local development and the ongoing alpha, then clarified that they will deploy alpha manually. The checked-in settings for the game API, worker, LiveOps and development Admin dashboard now select this release:

| Setting | Value |
|---|---|
| `AttributeRedesign:LiveVersion` | `18` |
| `EquipmentBalance:LiveVersion` | `4` |
| `Combat:AbilityBalanceProfile` | `healing-v1` |

This selects the 40-percentage-point penetration cap, penetration price 4, Restoration price 1.5, Herb Mixture at 105% Power and Sprouting Surge at 125% Power. The game API, background worker and LiveOps host must use this same combination. The selectors are read when services are constructed, so a running host needs a rebuild/restart. Environment variables can override the JSON settings; check all three selectors when manually deploying. Selector changes alone do not convert existing items or refresh competitive snapshots; the API startup conversion described above performs that work.

Migration previews without an explicit target now use the host's configured equipment release, so the selected hosts preview release 4 rather than silently converting to release 2. Legacy staging at rules 17/equipment 1 retains target 2 for compatibility; pass `targetBalanceVersion: 4` explicitly during the alpha migration. The existing rehearsal script also requires `-TargetBalanceVersion 4` because its legacy default remains 2.

LiveOps packaging includes the complete shared combat JSON catalog, including statuses, summons and the ability profile. Local API/worker/LiveOps Release packages are under `TestResults/attribute-rollout-ready`; the selected settings and nine shared combat/equipment content files were checked against source hashes for each package. **107 focused tests passed** before the migration-target correction, followed by **50 passing final registration/migration checks** in `TestResults/attribute-rollout-final-tests.log`. The three packages were refreshed after that correction and again after the legacy-import implementation described below. No source was pushed and no alpha service was deployed.

### Legacy migration and completed local activation

Before the local cutover, the population was **558 equipment instances: 89 release-1 descriptors and 469 unversioned instances**. The unversioned population included **61 owned items** (17 inventory, 43 equipped, one marketplace listing; the inventory count included two guild loans) and **408 records with no inventory, equipment-slot, marketplace, guild-vault or saved-loadout reference**. These unreferenced records remain untouched and visible in the audit. Inventory consumption/removal can retain an `ItemInstances` row after removing its inventory row; lack of a reference is not permission to delete it or invent an owner.

**Legacy conversion is now implemented and rehearsed:** all **150 owned items** (89 versioned + 61 unversioned) passed preview, apply, retry, rollback, retry and source-hash restoration on a fresh, isolated copy of the local database. This includes the retired `plain.cloth_cowl`, a cloth-pants item, three equipped maces with no recorded attributes, the marketplace listing and both guild loans. All 1,082 historical snapshots, ownership links, pending rewards, scalar item metadata, modifier values and original unversioned modifier IDs matched after rollback. The remaining 408 blocked previews were precisely the unreferenced records.

Unversioned imports use a versioned receipt envelope with the original scalar metadata, base modifiers, instance modifier IDs, amounts, modifier types and rarity bonuses. Existing versioned descriptor hashes are unchanged. Retired cloth cowl/pants select the light hood/leggings profiles while preserving the original item-base ID and name. Tier, rarity and quality survive; the old schema contains no reinforcement rank or common attribute roll, so the preview explicitly initializes rank 0 and roll 1.00. Items with no positive recorded attributes show `Before: null`, `OldBudget: 0` and select their authored default profile unless the operator chooses another compatible specialization. Their exact empty state remains reversible. Conflicting ownership, unsupported modifiers and unknown item bases remain blocked.

The default release-1 audit now includes unversioned instance targets. `UnreferencedUnversionedInstances` reports records without any of the five live references; `UnversionedPendingRewards` separately reports unclaimed rewards lacking descriptors. Such pending rewards are counted as blockers, not silently omitted or converted without a stable award identity. `ItemConversionComplete` remains conservative and false while any unversioned records remain. Inspect the explicit counts and classify records; do not discard them to force a green audit.

Verification: **119 focused equipment/registration/healing/penetration regressions passed**, plus the real PostgreSQL transaction/concurrency/rollback test. The full copied-population round-trip rehearsal passed for **150 items**. See [the implementation and rehearsal record](legacy-equipment-migration-2026-09-27.md). No new EF migration is required for the legacy snapshot envelope, which uses the existing JSONB receipt column. The source database already contained `20260927132322_VersionEquipmentRebalances` when the subsequent cutover began; no new migration was authored for the cutover tool.

**Local activation completed on 27 September.** After a full settlement/conversion/resumption rehearsal in a separate copy and a fresh source backup, the local operator settled all **37 schedules**, processing **311,234 retained earned encounters** under the old rules. It converted **483 owned items**, including **333 items earned during settlement**, and resumed all 37 original teams/areas under **18 / 4 / healing-v1**. All **1,082 original historical snapshots** and **408 unreferenced records with their modifiers** matched their pre-cutover fingerprints. The post-cutover total was **891 equipment instances**. The rebuilt local API is running with both health endpoints returning HTTP 200. See [the local cutover record](local-attribute-cutover-2026-09-27.md).

`ReferencedItemConversionComplete` now reports readiness for the referenced population of an audited source release; the strict `ItemConversionComplete` deliberately remains false for the retained 408 unversioned records. The cutover verification checked every release below 4, unclaimed rewards and active competitive snapshots. Alpha still needs its own audit and manual deployment.

The following optional full cutover procedure predates automatic startup conversion and includes combat settlement. Disable
`EquipmentConversion:RunOnStartup` while staging legacy selectors if choosing this procedure instead:

1. Back up the alpha database; enable maintenance and stop all combat/reward writers, including the worker. Preserve the prior image versions and selectors.
2. Stage the code with environment overrides `AttributeRedesign__LiveVersion=17`, `EquipmentBalance__LiveVersion=1`, and an empty `Combat__AbilityBalanceProfile`. The API runs EF migrations at startup, including the receipt revision migration. Keep this staging deployment inaccessible to players.
3. Run the alpha audit and preview conversion to release 4. Review the owned legacy imports, any unreferenced records and unsupported pending rewards; verify the complete population in a copy before converting. The supported versioned conversion flow remains the operator API documented below; keep each operation ID, preview/result hash and receipt.
4. With writers paused, convert supported inventories and pending rewards, then verify a clean full-population audit. Do not increment through a shrinking target set while mutating it; freeze targets first.
5. Remove the staging overrides (or set `18`, `4`, `healing-v1` explicitly) across all hosts. Refresh active Arena defense snapshots and finish/cancel old-rule tournaments; preserve historical snapshots.
6. Verify bootstrap attributes, drops, comparisons, specialization choices and both healing tooltips, then reopen traffic and resume the worker.

The remaining healing/barrier balance findings are documented in [the healing review](healing-balance-review-2026-09-27.md). The user selected that package; those findings remain limitations of the evidence, not a claim of universal balance.

## Original staging record

The initial implementation was staged behind `AttributeRedesign:LiveVersion`, with fallback **17** when host settings are absent. Version **18** selects the new combat formulas and defaults to equipment release 2 only when `EquipmentBalance:LiveVersion` is absent. The selected host configuration above now explicitly uses release 4. The following sections retain the original migration/rehearsal evidence.

Version 18 now uses the user's requested flat penetration rule: subtract up to 40 percentage points from Armor/Resistance mitigation, down to zero, after Corrosion and the defense curve. Version 17 retains its old formula and 60% cap. This requires no additional database migration or configuration key. Earlier balance studies predate this change; the [new bounded comparison](attribute-penetration-follow-up-2026-09-27.md) shows substantially stronger penetration builds, so candidate pricing remains an activation gate. Existing frozen study evidence must be retained with its original executable.

## Schema and content

The subsequent [penetration price review](penetration-price-review-2026-09-27.md) recommends 4 budget per point after 62,944 study fights. This price is authored in equipment releases 3 and 4; release 2 retains its historical 1.5 price. Release 4 is active locally; alpha deployment remains manual.

`20260925142600_AttributeRedesignReceiptsAndTelemetry` follows `20260925111407_AddLeanTelemetry`. It adds the snapshot rules version (existing rows default to 17), conversion receipts, raw itemization observations and daily reports. The generated SQL is at `TestResults/attribute-redesign-migration.sql`. SQL generation and EF's pending-model check passed. Schema Up/Down was exercised in disposable local PostgreSQL databases on 27 September; no shared database was changed.

Retain all three `equipment-*.legacy-v1.json` catalogs. Existing item descriptors remain frozen and readable. Do not rewrite historical battle results or snapshots. The current harness reads older exact content allowlists; executing an old archived result still requires its original execution assemblies. A successful current build does not certify byte-identical execution of a historical archive.

## Original local copied-data rehearsal (superseded by the 150-item rehearsal above)

Completed on 27 September using a read-only snapshot of the user's existing local `legends_legacy` database. A separately supplied backup was unnecessary. All **87 supported items** passed HTTP preview, apply, apply retry, rollback, rollback retry and canonical source-hash restoration. The audit also found **469 unversioned equipment instances** and **one retired `plain.cloth_cowl` descriptor** with no authored conversion. Those were the blockers at the time of this original rehearsal; the later implementation and evidence above supersede the missing-converter finding. Neither rehearsal authorizes discarding unreferenced records.

1. Take a read-only snapshot of the existing local database, or use an available sanitized backup, and restore it to an isolated local PostgreSQL instance. Keep copied data and evidence private. Keep external integrations and every scheduler/worker disabled. Confirm that the local LiveOps application points to that copy; a loopback URL alone cannot prove this. Never use a port forward to a shared environment.
2. Apply the reviewed schema migration **to that copy only** using the project's normal local database procedure. Start the local LiveOps host with rules 17. Keep combat and all item mutations paused.
3. Include inventory, equipped, marketplace-listed, guild-owned, bound/unbound, upgraded, styled and pending unclaimed dungeon rewards. Include missing/unversioned descriptors, which must remain explicit blockers rather than being guessed into a new profile. Include archived snapshots and active Arena/tournament snapshots.
4. Set `LL_REHEARSAL_TOKEN` to a local SuperAdmin bearer token, or pass `-UseDevelopmentOperator` to use the existing loopback-only Development login. The latter verifies the Development identity and uses its cookie and antiforgery token without saving either. Run the script below first without its mutation switch. Its output directory must be new. Receipt files contain operational account/item identifiers and should be treated as private rehearsal artifacts.

```powershell
./build/rehearse-equipment-migration.ps1 `
  -BaseUri http://localhost:5001 `
  -OutputDirectory "$env:TEMP/ll-equipment-preview" -MaximumItems 100 `
  -UseDevelopmentOperator

# Only after verifying the host is bound to the isolated local copy:
./build/rehearse-equipment-migration.ps1 `
  -BaseUri http://localhost:5001 `
  -OutputDirectory "$env:TEMP/ll-equipment-roundtrip" -MaximumItems 100 `
  -ApplyAndRollback -UseDevelopmentOperator
```

The script freezes the audit targets before mutation, saves the operation ID before applying, repeats apply and rollback with that ID, then checks that rollback restores the canonical source hash. Known preview rejections are saved in `blocked-previews.json` and skipped without mutation; unexpected request failures still stop the run. `summary.json` separates previews, completed round trips, blocked previews and unversioned instances. It refuses remote URLs, follows no authentication redirects and never overwrites evidence. It does not change the live selector or apply EF migrations. A network failure may leave a conversion committed: consult the saved operation request, retry that exact request, and roll it back using the same operation ID. Do not create a replacement ID for an uncertain attempt.

5. Exercise concurrent requests for the same item, disconnect after commit, and restart delivery of the outbox. Expect one active receipt per item and one telemetry observation per stable ID. Test the unique-index loser as a normal rejected request, then retry after reading the winning receipt. Compare relational instance modifiers and frozen descriptors before/after rollback, not just the display text.
6. On the isolated copy, enable 18 while still paused, refresh current Arena defenses, and exercise the one-time specialization choice. Verify one credit, stable retry results, inventory/equipment refresh, current ownership checks and no rollback after later edits or credit consumption. Historical snapshots must still read as 17. Stop or finish old tournaments; do not mutate their frozen history.

## Operator contract

All conversion routes require the existing **SuperAdmin** policy:

| Route under `/api/liveops/equipment-migration` | Purpose |
|---|---|
| `GET audit?page=0&pageSize=100` | Counts and paged legacy targets, unversioned blockers, active legacy competitive snapshots |
| `POST preview` | `{ target, definitionId? }`; returns before/after descriptors, source hash, budgets and legal choices |
| `POST apply` | `{ operationId, target, sourceHash, definitionId }`; idempotent conversion receipt |
| `POST {operationId}/rollback` | Restore the receipt's original descriptor if the item is unchanged and the credit is unused |

`target` contains `itemId`, `location` (`Instance` or `PendingDungeonReward`) and a dungeon `containerId` for pending rewards. Use the target returned by the audit. Page zero repeatedly is appropriate for a resumable conversion batch because successful conversions leave the legacy set; incrementing pages while mutating that set would skip items. Freeze all targets first if using numbered pages.

Conversion preserves instance identity, owner/binding, provenance, tier, rank, rarity, quality, roll, weapon behavior and style. It selects the closest legal specialization by historical budget shares and grants one item-specific specialization choice. It does **not** promise unchanged cooldowns, damage or wins. The credit follows the converted item to its current eligible personal owner; guild items cannot consume a personal choice. Later reinforcement is retained when choosing; changing style first blocks the choice. Mutation and outbox writes share the command transaction.

Rollback is conditional compensation, not a time machine. Moved/consumed/edited equipment or a spent choice requires manual reconciliation. Retain receipts and the pre-migration backup. Dropping the receipt table with an EF downgrade would destroy this audit trail; do not use a schema downgrade as an item rollback.

## Activation gate

The authorized local activation is complete. For the user's manual alpha deployment:

- Resolve or explicitly retire the unversioned and unmapped equipment identified by the copied-data audit, then rehearse the complete intended population. Review item/loadout impact and agree on acceptable encounter outcomes. The completed local rehearsal covers its available data; the checked-in simulation screen is exploratory and does not establish universal build viability or certify the set reservation prices.
- Pause player mutations and background writers, settle retained earned combat under the old rules, then pause schedules. Convert all referenced live instances and pending rewards. Check every older equipment release and require zero referenced unversioned items, zero older versioned items and zero unsupported unclaimed rewards. Explicitly classify and preserve unreferenced legacy records; the strict `ItemConversionComplete` remains false while they exist, even when `ReferencedItemConversionComplete` is true. Neither flag replaces review of all releases and competitive snapshots.
- While access remains paused, start all game hosts with version 18. Refresh Arena defenses through the existing defense-snapshot workflow and finish or cancel tournaments holding version-17 combatants. `CompetitiveSnapshotsReady` must be true before reopening. The executor also rejects mixed-version PvP rather than silently matching different formulas.
- Verify comparisons, drops, one-time choices and current snapshot creation. Resume only after the clean audit and operational checks. Keep historical replay readers and catalogs.
- Observe `/api/liveops/analytics/itemization?days=30`, outbox delivery and the existing daily telemetry job. Raw data lasts 30 days; daily aggregates last 13 months, with a seven-day late-delivery recomputation window. Events delayed longer than that window require an explicit reporting backfill.

Award/equip/discard reports are observational. Version-2 observations freeze compatible owned alternatives, equipped gear, valid guild loans and eligibility at awards, comparisons and equip/loadout decisions. Missing or incomplete older snapshots are excluded from eligible denominators and reported separately; they never mean zero alternatives. Same-batch rewards are included at acquisition. These are not randomized denominators, and alternatives can differ in tier, quality or other stats. Auto-selected loadouts are visible through committed battle builds. Do not interpret an apparent win-rate advantage as a causal stat effect without controlling for encounter, progression, Doctrine, ordered Essences and selection bias.

## Completed local database verification — 27 September

`EquipmentPostgresRehearsalTests` ran against PostgreSQL 17.11 using a new, uniquely named database on a disposable loopback cluster. It applied the full schema history through the preceding migration, seeded a legacy inventory item and unclaimed dungeon reward, and applied the redesign migration. Verified paths include JSONB audit selection, relational modifier replacement, interrupted transactions leaving no item/receipt/outbox mutation, two concurrent writers with one receipt, replay and stale-preview rejection, pending reward conversion, stable telemetry deduplication and daily eligible-award reports, item rollback, and schema Down/Up in the disposable database. A second run verified the lifecycle script below and automatic server shutdown.

```powershell
./build/run-postgres-equipment-rehearsal.ps1 -PostgresBin C:/tools/pgsql/bin
```

The script accepts existing PostgreSQL binaries, creates a TEMP cluster bound to `127.0.0.1`, uses a random local password, runs tests through `build/run-tests.ps1`, stops the server and removes the password file. It installs no service and does not download software. The binaries used here came from [EDB's PostgreSQL binary archive](https://www.enterprisedb.com/download-postgresql-binaries), linked by [PostgreSQL's Windows download page](https://www.postgresql.org/download/windows/). Download SHA-256: `4b8db0930c38f6ef845db919551dedda3b6b845aeb0927b3d79a6e8e9e4537cf`.

The seeded integration is separate from the completed local-copy HTTP rehearsal below. No shared database, persistent environment configuration or external service was changed.

### Existing local database copy

The source was the local PostgreSQL 17.5 database on port 5432 (779 MB). `pg_dump` ran with `default_transaction_read_only=on`, producing an 87,664,053-byte private snapshot. The source already contained `20260925142600_AttributeRedesignReceiptsAndTelemetry`; this run did not apply a migration to it. The snapshot was restored to a separate PostgreSQL 17.11 TEMP cluster on `127.0.0.1:55441`. A new LiveOps host used only that copy, rules 17 and the existing Development operator. Its account-risk worker was disabled. Copied schedules for 37 characters were detached in the disposable copy to satisfy maintenance checks; the source API and schedules continued running.

The copied population contained 557 equipment instances: 88 versioned legacy descriptors and 469 unversioned instances. The versioned items included 70 in inventories and 18 equipped, 19 native blueprint styles, nine reinforced items, tiers 1–2 and all five rarities. No versioned marketplace, guild or pending-reward records were present, so this run does not add population evidence for those paths. Of the 88, one `plain.cloth_cowl` archetype predates the retained catalogs and correctly remains unmapped.

All 87 supported items completed the full HTTP round trip. There were 87 rolled-back receipts and zero active receipts afterward. Canonical descriptor hashes matched before/after; database fingerprints also matched for item scalar fields, relational modifiers (excluding regenerated modifier IDs), inventories, equipment slots, marketplace links, guild links, pending rewards and all 1,082 historical snapshot rows. Unauthenticated audit returned 401, cookie mutations without antiforgery returned 400, and authenticated itemization analytics returned successfully. No version-18 activation, snapshot refresh or balance acceptance was performed.

This exercise found and repaired missing explicit LiveOps handler registrations, conversion of native blueprint styles rolled onto plain definitions, and receipt timestamps exceeding PostgreSQL microsecond precision. Unsupported historical previews now return a structured blocker instead of a server error. **40 focused backend tests passed**, followed by the real PostgreSQL integration with exact initial/retry timestamp assertions. The copied-data HTTP run and PowerShell parsing also passed. These rechecks do not constitute a new unrestricted-suite run.

Private dump, restore, logs and per-item evidence remain outside the checkout at `%TEMP%/ll-local-equipment-c648aef19e9948f89d5f12b3eb495ceb`; the successful run is `attempt-094db15d9e5f46e8b4c91621dd1451e0`. Temporary rehearsal servers were stopped and password files removed. Aggregate evidence is retained under `TestResults/attribute-local-copy-20260927`; regression results are `TestResults/attribute-local-copy-regressions-20260927.trx`.
