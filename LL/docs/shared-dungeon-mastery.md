# Shared dungeon mastery and equipment drops

Implemented September 7, 2026 in the primary game service and Angular game client.

Equipment has a 50% chance to drop on dungeon completion at mastery 0. Each mastery level adds 5 percentage points, reaching 100% at mastery 10. The existing run-start mastery snapshot determines the chance, just as it determines Vigor, visibility, and currency benefits. XP gained on that clear improves subsequent runs.

The dungeon's Run Rewards panel lists Equipment, quantity 1, and the server-calculated equipment chance with mastery already included. It appears under Chance Drops below 100% and Guaranteed at 100%, and is included in the reward item count. This row also appears when the dungeon has no other repeatable item rewards. The Mastery tab continues to display the same chance.

Mastery belongs to the dungeon family. Novice, Veteran, and Champion contribute XP and clears to the same record and receive the same benefits. Other dungeon families and other characters have separate progress. The stored `DungeonDefinitionId` is the canonical family ID (for example, `goblin_mines`). Preview responses remain keyed by the requested difficulty ID for existing consumers.

## XP curve

Every cumulative threshold is ten times its previous value. XP earned per clear remains 100 plus 5 per completed room (minimum one room), 50 for defeating the boss, and 25 for each defeated miniboss.

| Mastery | Cumulative XP required | Equipment chance |
| --- | ---: | ---: |
| 0 | 0 | 50% |
| 1 | 1,000 | 55% |
| 2 | 2,500 | 60% |
| 3 | 5,000 | 65% |
| 4 | 9,000 | 70% |
| 5 | 14,000 | 75% |
| 6 | 21,000 | 80% |
| 7 | 30,000 | 85% |
| 8 | 42,000 | 90% |
| 9 | 56,000 | 95% |
| 10 | 75,000 | 100% |

Rarity odds, conditional on receiving equipment, remain 84% / 14% / 2%:

- Novice: Uncommon / Rare / Epic.
- Veteran: Rare / Epic / Unique.
- Champion: Epic / Unique / Legendary.

## Existing progress and release

Migration `20260907140007_ShareDungeonMasteryAcrossDifficulties` sums XP and completion counts across each character's difficulty records, including an existing canonical family record. It then recalculates levels against the new curve. Earned XP is preserved, but displayed levels and associated benefits can decrease. Original earliest creation and latest update times are preserved, along with the most recent non-null awarded run ID.

The new `MaxLevelRewardClaimed` column remembers whether any difficulty had already reached mastery 10. This prevents the 50 / 100 / 200 Soulstone milestone from paying again after a level recalculation. New characters receive that reward once per family, using the tier of the dungeon where they reach mastery 10. Existing achievements remain unlocked.

Per-run mastery award reasons also act as a receipt, preventing duplicate XP if another difficulty has since updated the family's last awarded run. Runs already underway retain their captured mastery benefits and drop chance level.

The migration must accompany the backend update after the preceding equipment-loadout/inventory-fragment migration. The consolidation is transactional. Automatic downgrade is unsupported because the original difficulty split cannot be reconstructed; a rollback requires a pre-migration backup. The migration was generated and its SQL script checked, but was not applied to a database. No services were deployed.

Both regions' `equipment-ordinary.v1.json` dungeon profiles now use `dropChance: 0.5`. No new environment settings are required. Deploy the updated content, backend, and frontend together with the migration through the normal release process.

## Changed files

Paths below are relative to the repository root.

| Files | Purpose |
| --- | --- |
| `LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json` | 50% base chance in both regional dungeon profiles. |
| `LL/src/Core/Domain/Models/Dungeons/Mastery/DungeonMasteryProgression.cs`, `DungeonMasteryBenefits.cs`, `CharacterDungeonMastery.cs` | XP curve, equipment bonus at every level, and persistent milestone reward state. |
| `LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentProgressionOrdinaryAcquisition.cs` | Shared, capped equipment probability calculation. |
| `LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonMasteryService.cs`, `Items/EquipmentAcquisitionService.cs`, `Combat/Layers/Rewards/Dungeon/DungeonCompletionRewardApplier.cs` | Shared progress and preview reads, run-start equipment chance, and one-time milestone rewards. |
| `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Dungeons/CharacterDungeonMasteryRepository.cs` | Include newly tracked mastery rows before transaction save. |
| `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260907140007_ShareDungeonMasteryAcrossDifficulties.cs`, its designer, and `LLDbContextModelSnapshot.cs` | Schema and existing-progress conversion. |
| `LL/src/Core/Application/Interfaces/Services/LL/Dungeons/IDungeonMasteryService.cs`, `UseCases/Dungeons/Dtos/DungeonPreviewDto.cs`, `UseCases/Dungeons/Queries/GetAvailableDungeons/GetAvailableDungeonsQuery.cs` | Reward eligibility and preview contracts, including authoritative equipment chance. |
| `LL/src/Presentation/ll/src/app/shared/models/Dtos/dungeons/dungeonPreviewData.ts`, `shared/components/dungeons/dungeon-card/dungeon-card.component.ts` and `.html` | Shared-mastery explanation, per-level bonuses, and current equipment chance. |
| `LL/tests/EssenceSystem.Tests/DungeonMasteryServiceTests.cs`, `DungeonMasteryBenefitsTests.cs`, `DungeonDtoMappingTests.cs`, `DungeonEssenceRewardTests.cs`, `EquipmentAcquisitionTests.cs`, `EquipmentDungeonConsumerTests.cs`, `CombatAcquisitionTests.cs` | Curve, persistence, reward, probability, and API regression coverage. |
| `docs/design/equipment-specification.md`, `equipment-implementation-status.md`, `equipment-region-one-progression.md`, `equipment-region-two-progression.md`, and this document | Current mechanics and release notes. |

## Verification

- Backend Services and Persistence projects build with `--no-restore`.
- Test project builds with `--no-restore -p:BuildProjectReferences=false`.
- `build/run-tests.ps1 -NoBuild -Configuration Debug` with the dungeon, equipment acquisition/content/blueprint, and leaderboard filters: 230 passed.
- `npm.cmd run build:development`: passed.
- `npm.cmd run test:ci -- --include=src/app/shared/components/dungeons/dungeon-card/dungeon-card.component.spec.ts`: 10 passed in Chrome Headless.
- EF migration SQL generation and `migrations has-pending-model-changes`: passed; no model changes remain outside migrations.
- `git diff --check`: passed.

Backend builds report existing nullable-reference and test-analyzer warnings. No migration was executed against a database.
