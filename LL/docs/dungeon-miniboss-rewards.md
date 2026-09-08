# Dungeon miniboss rewards

Miniboss victories now have a 25% chance to add one equipment item to Pending Loot.
The chance is configured as `dungeonEquipment.miniBossDropChance` in each region's
`equipment-ordinary.v1.json` profile. This uses the existing dungeon equipment pool,
difficulty rarity distribution, rank, quality, and variant rules. Mastery does not
modify this separate room reward. Essence chances and completion rewards are unchanged.

Equipment is rolled once per room with a stable character/run/seed/room identity.
Retries preserve misses and cannot duplicate a successful award. The award requires
a completed miniboss room in an active run with Vigor remaining after the battle.
Equipment remains pending until completion or safe retreat; run failure loses it.
The existing `ProtectedAcquisitionEnabled` option also controls this reward.

Authored miniboss Vigor ranges increased by approximately 25%:

| Room | Authored range | Battle cost before mastery |
| --- | --- | --- |
| Bone Keeper | 16–29 → 20–36 | 14–25 → 17–31 |
| Brood Guard, Web Weaver's Den, Guardian Bough | 17–30 → 21–38 | 14–26 → 18–32 |

Battle costs retain the 0.85 combat scaling, remaining-health calculation, mastery
discount, and 35-point cap. Route forecasts use the same authored values. Existing
runs retain their captured Vigor ranges; new runs use the increased costs.

Ship both content JSON files with the backend and restart content-loading processes.
No migration or new environment setting is needed. No deployment was performed.

## Changed files

- `src/API/API.LL/Data/equipment/equipment-ordinary.v1.json`: regional miniboss equipment chances.
- `src/API/API.LL/Data/dungeons/dungeon-delves.json`: higher authored miniboss Vigor ranges.
- `src/Core/Domain/Models/Items/Equipments/Progression/EquipmentProgressionOrdinaryAcquisition.cs`: configurable chance and validation.
- `src/Core/Application/Interfaces/Services/LL/Items/IEquipmentAcquisitionService.cs`: miniboss reward contract.
- `src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs`: deterministic per-room equipment awards.
- `src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs`: award after surviving a miniboss victory.
- `tests/EssenceSystem.Tests/EquipmentAcquisitionTests.cs`, `DungeonVigorStateTests.cs`, and `DungeonRunHardeningTests.cs`: drop rates, eligibility, retries, battle costs, forecasts, and retreat coverage.
- This document records the balance changes and release implications.

## Verification

- `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Debug --no-restore --verbosity quiet`: passed with existing warnings.
- `build/run-tests.ps1 -NoBuild -Configuration Debug -Filter 'FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~DungeonVigorStateTests|FullyQualifiedName~DungeonRunHardeningTests|FullyQualifiedName~DungeonEssenceRewardTests|FullyQualifiedName~DungeonRunFactoryLayoutTests|FullyQualifiedName~DungeonCatalogTests'`: 159 passed.
- `git diff --check`: passed.

The initial test-script build with restore failed because the sandbox could not
read the user NuGet configuration. Building against already restored dependencies
and running tests through the repository script succeeded.
