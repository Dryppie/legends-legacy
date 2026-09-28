# Legacy equipment import and copied-database evidence

The local implementation now converts every owned item in the audited local population to equipment release 4. This is implementation and rehearsal evidence, not a claim that the source database or alpha has been activated.

## Conversion rules

- Versioned equipment keeps its recorded rank, roll, provenance, ownership, quality, rarity, tier, item-base ID, display name and behavior. The two explicit retired archetype aliases are `plain.cloth_cowl` → `plain.light_hood` and `plain.cloth_pants` → `plain.light_leggings`.
- Unversioned equipment derives ownership from inventory, equipped slots, marketplace seller or guild property. A guild loan remains guild-owned. Conflicting locations, quantities or owners are rejected. A saved loadout alone does not prove ownership.
- Imports preserve tier, rarity and quality. The old schema has no reinforcement rank or shared attribute roll: rank starts at 0 and the shared roll at 1.00. Original affix amounts, including rarity upgrades, inform specialization selection and are retained exactly in the rollback receipt; the new stats follow the target release's budget.
- Equipment with no positive stored attributes uses the authored default specialization, unless another compatible choice is requested. Preview reports the missing old descriptor and zero old budget instead of inventing old stats.
- `LegacyImport` provenance preserves the acquisition source without pretending an old crafted item was a random drop. Existing trade/binding status survives. All hosts must use the updated code before such descriptors are activated.
- Specialization choices retain a retired item's original item-base ID and name. The usual single conversion credit applies; ordinary later rebalances grant no additional credit.

## Receipt and transaction behavior

The existing `BeforeJson` JSONB column supports a `LegacyImportVersion: 1` envelope. It captures the raw legacy identity, ownership, acquisition metadata, favorites, affinity tags, base modifiers and instance modifiers. Modifier snapshots include original IDs, types, amounts and rarity-bonus amounts. No schema addition is needed. Existing versioned receipt hashes and their serialization contract remain unchanged.

Apply requires the preview's source and result hashes for an unversioned import. Ownership and item mutation locks, scheduled-combat protection, receipt retries and outbox notifications use the existing command transaction. Guild borrowers and saved-loadout users are included in affected-character locks and notifications.

Rollback rejects changed descriptors, spent specialization credits, changed ownership, changed legacy metadata or changed authored base modifiers. A valid rollback clears the progression descriptor and explicitly reinserts the original modifier IDs. The database integration test exposed and fixed EF's tendency to treat supplied, nonempty IDs as existing rows rather than inserts.

Roll back the most recent receipt first. If rolling back to an older application build, restore imported items through the new operator code before starting old binaries that cannot read `LegacyImport` provenance. Keep writers paused throughout.

## Local population and rehearsal

A new read-only `pg_dump` of local `legends_legacy` was restored to a separate PostgreSQL 17 cluster on loopback port 55441. The temporary LiveOps host ran on port 55442 with rules 17, equipment release 1, the healing profile disabled and background account-risk processing disabled. The source database was never passed to the mutation host. The receipt revision migration was applied to the copy. Only copied schedules were detached to meet the maintenance precondition.

| Population | Count | Result |
|---|---:|---|
| Release-1 descriptors | 89 | Converted and rolled back |
| Owned unversioned items | 61 | Converted and rolled back |
| Unversioned records without any live item reference | 408 | Rejected; retained unchanged |
| Historical character snapshots | 1,082 | Unchanged |
| Unclaimed unversioned dungeon rewards | 0 | None in this population |
| Active legacy Arena defenses / tournament snapshots | 0 / 0 | None in this population |

The owned unversioned population includes 17 inventory items, 43 equipped items and one marketplace listing. Two inventory items are guild loans. Three equipped maces had no base or instance attributes; the default-profile import and empty-state rollback were exercised for all three.

All 150 owned items passed HTTP preview, apply, apply retry, rollback, rollback retry and canonical source-hash restoration. Anonymous operator requests returned 401 and cookie mutations required antiforgery validation. All 150 receipts were rolled back, with zero active receipts remaining in the copy. Both temporary servers were stopped afterward.

Before/after fingerprints matched for scalar item rows, modifier values, original unversioned modifier IDs, inventory rows, equipped slots, marketplace listings, guild-vault rows, saved-loadout slots, pending dungeon rewards and historical character snapshots. These checks do not certify byte-for-byte preservation of unknown fields in old versioned JSON descriptors; their established canonical hash contract is the versioned rollback guarantee.

Private backup and per-item evidence remain under `%TEMP%/ll-equipment-full-copy-19c69940a74142c8b69eda87818d562d`; successful full-population evidence is in `attempt-713bc22254d24c9495fc603f64e068e7`. No credentials were copied into repository artifacts. Aggregate evidence is in `TestResults/legacy-equipment-copy-20260927`.

## Verification and subsequent local activation

- Backend tests ran through `build/run-tests.ps1`: 119 equipment, migration, registration, comparison, penetration and healing checks passed, including unknown, negative and multiplicative legacy-modifier rejection.
- `build/run-postgres-equipment-rehearsal.ps1` passed the real PostgreSQL schema/transaction/concurrency/retry/rollback test, including exact legacy modifier restoration and metadata-drift rejection.
- Full copied-data rehearsal: 150 successful round trips; exactly 408 unreferenced-item rejections.
- Local API, worker and LiveOps release packages were republished under `TestResults/attribute-rollout-ready`; this did not start or deploy any service.
- `git diff --check` and PowerShell parsing of the updated rehearsal script passed. No required verification command remained blocked; the initial PostgreSQL rollback failure was fixed and rerun successfully.
- Legacy pending dungeon rewards without a stable descriptor are audited as blockers. Their reconstruction is not implemented or claimed as covered by this population.

The actual source cutover was subsequently completed after a full settlement/resumption rehearsal on a fresh copy. The operator settled 37 schedules through the normal rules-17 CQRS flow, processed 311,234 retained earned encounters, converted 483 owned items (the original 150 plus 333 settlement awards), and resumed all 37 original teams/areas under `18 / 4 / healing-v1`. All 1,082 original historical snapshots and 408 unreferenced records with their modifiers were preserved. The strict audit continues reporting those records; the new referenced-population readiness flag does not delete or claim them converted. The receipt revision migration was already present in the source database at cutover preflight. The rebuilt local API passed both health endpoints. See [the completed cutover record](local-attribute-cutover-2026-09-27.md) for backup, verification and operational details. Alpha deployment remains the user's manual responsibility and needs a separate population audit.
