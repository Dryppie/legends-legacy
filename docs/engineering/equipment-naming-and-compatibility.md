# Equipment naming and storage contracts

Updated: 3 September 2026. Scope: the primary LL game, its player/API contracts and offline equipment tools.

Equipment and Forge are the normal game names. Alpha-data preservation is no longer required. The preceding rename's response, route and configuration aliases, cohort fields, retired-content adapters and refund paths have been removed. See the [cleanup record](../design/equipment-post-alpha-cleanup.md) and [implementation status](../design/equipment-implementation-status.md).

## Current contracts

- Domain types use EquipmentState, EquipmentData and other descriptive names under Domain.Models.Items.Equipments.Progression. Services and application commands use Equipment and Forge terminology.
- Player metadata uses progression. Starter routes are equipment/starter-options and equipment/starter-claim. Old response and starter-route aliases are gone; clients must use the current contracts.
- EquipmentProgression is the configuration section. The five capabilities default to enabled. The old configuration section and separate quest-integration flag are gone.
- Runtime content uses equipment-*.v1.json files. API, worker and administrator builds must include the content they load.
- Offline reference generation uses --equipment-reference-builds. Its former switch alias is removed. See the [reference guide](../content-balancing/equipment-reference-builds.md).

## Stored names that remain in use

Current persisted descriptors, operation receipts, content identities, objective/event keys and deterministic salts still use stable identifiers. [EquipmentKeys](../../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentKeys.cs) names those values in code. These belong to the current equipment implementation and do not provide an alternate Alpha gameplay path.

| Current code | Preserved storage contract |
| --- | --- |
| `EquipmentInstance.ProgressionData`, `EquipmentSnapshot.ProgressionData`, `RunReward.ProgressionData` | `ModelEData` database columns and persisted JSON fields |
| `DungeonRun.EquipmentCommitment` | `ModelECommitment` database column and persisted JSON field |
| `StarterEquipmentGrant` | `ModelEStarterGrants` table |
| `ForgeReceipt`, `LearnedEquipmentStyle` | `ModelEForgeReceipts`, `ModelECharacterStyles` tables |
| `CombatAcquisitionProgress`, `CombatAcquisitionSelectionReceipt` | `ModelEOrdinaryProgress`, `ModelEOrdinarySelectionReceipts` tables |
| `PlainEquipmentEntitlement`, `PlainEquipmentRecoveryReceipt` | `ModelEPlainEntitlements`, `ModelEPlainRecoveryReceipts` tables |
| `EquipmentProtectionProgress`, `EquipmentProtectionReceipt`, `BaselineEquipmentRecoveryReceipt` | `ModelEProtectionProgress`, `ModelEProtectionReceipts`, `ModelEBaselineRecoveryReceipts` tables |

Historical EF migrations remain so a fresh database has a runnable schema creation chain. The two post-Alpha cleanup migrations remove obsolete profession/queue storage and saved quest progress; the naming table above does not imply those removed systems are preserved. Shared stat-budget and recipe/tempering simulation helpers still used by offline tools remain active dependencies.

## Startup and verification

The API runs pending migrations during startup. Rebuild and restart the local API after updating. The task generated the cleanup migrations without applying them to the game database; deleting obsolete quest progress is irreversible. Release the matching API and player client together because retired routes and response fields are removed. No deployment was performed.

The [cleanup record](../design/equipment-post-alpha-cleanup.md#verification) contains the latest backend, player and migration checks. Earlier alias/compatibility test counts describe the preceding rename only and do not represent the current contract. No configuration change beyond using the current section is required by the quest-progress fix.
