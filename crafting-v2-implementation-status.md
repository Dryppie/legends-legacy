# Crafting V2 Implementation Status

Source design document: `C:/Users/HrHoe/Downloads/crafting-v2-design-for-codex.md`

Generated against the current repository state on 2026-06-24.

## Status Key

| Status      | Meaning                                                                                                            |
| ----------- | ------------------------------------------------------------------------------------------------------------------ |
| Implemented | The feature exists in code/content and is wired into the current flow.                                             |
| Partial     | The main path exists, but some design detail, content breadth, validation, UI detail, or test coverage is missing. |
| Not done    | The feature or design requirement has not been implemented.                                                        |

## Executive Summary

Crafting V2 is implemented as a working vertical slice across backend, persistence, static JSON definitions, API endpoints, and Angular UI.

The implemented flow supports:

- Base recipes available without unlock rows.
- Blueprint-unlocked variant recipes.
- Batch crafting with `recipeId`, `targetTier`, and `quantity`.
- Automatic material resolution with no manual material selection.
- Crafted equipment always starting at `Common`.
- Per-item quality rolls.
- Per-item potential calculation.
- Recipe mastery persistence and XP gain.
- Tempering directions selected by the player.
- Tempering consuming only item Potential.
- Tempering progress, rarity upgrades, stat improvements, affix IDs, and special modifier IDs.
- Tool crafting and tool tempering rejection in the new V2 paths.

The largest remaining gaps are:

- No dedicated automated tests were added for Crafting V2.
- Static content is a starter set, not a full conversion of every equipment item in `items.json`.
- Dungeon rewards were not updated to award blueprints/special resources or globally prevent equipment drops.
- Some recommended configurability is still hardcoded in services rather than separate JSON files.
- Some suggested API endpoints and UI details are not present.

## Implemented

### Core Crafting Rules

| Requirement                                                  | Status      | Notes                                                                                                     |
| ------------------------------------------------------------ | ----------- | --------------------------------------------------------------------------------------------------------- |
| Crafting request does not include manual material selections | Implemented | `CraftItemsCommand` accepts recipe, tier, and quantity. The frontend sends those fields only.             |
| Crafting is batch-friendly                                   | Implemented | `CraftItemsCommand` supports quantity, clamped to 1-100.                                                  |
| Base recipes are available to all players                    | Implemented | Base recipes do not require unlock rows.                                                                  |
| Blueprint variants require unlocks                           | Implemented | Variant recipes check `CharacterRecipeUnlocks`.                                                           |
| Crafted equipment always starts as Common                    | Implemented | New V2 craft command sets `Rarity.Common`. Legacy crafting path was also adjusted to stop rolling rarity. |
| Quality is rolled during crafting                            | Implemented | `IItemQualityRollService` / `ItemQualityRollService` rolls quality per created item.                      |
| Potential is generated when crafting                         | Implemented | `IItemPotentialService` calculates starting and max potential.                                            |
| Tools cannot be crafted                                      | Implemented | V2 craft command rejects outputs where `EquipmentType == Tool`.                                           |
| Tools cannot be tempered                                     | Implemented | V2 temper command rejects tools.                                                                          |
| Tempering consumes Potential only                            | Implemented | V2 temper command spends item Potential and does not query or consume material inventory.                 |
| Rarity changes through tempering                             | Implemented | V2 temper command advances progress and upgrades rarity when thresholds are reached.                      |

### Static Definition System

| Requirement                                      | Status      | Notes                                                                          |
| ------------------------------------------------ | ----------- | ------------------------------------------------------------------------------ |
| JSON-backed definitions                          | Implemented | Added `Data/crafting/*.json`.                                                  |
| Material definitions                             | Implemented | Added `materials.json`.                                                        |
| Base recipe definitions                          | Implemented | Added `base-recipes.json`.                                                     |
| Recipe variant definitions                       | Implemented | Added `recipe-variants.json`.                                                  |
| Blueprint definitions                            | Implemented | Added `blueprints.json`.                                                       |
| Tempering recipe definitions                     | Implemented | Added `tempering-recipes.json`.                                                |
| Definition provider service                      | Implemented | Added `ICraftingDefinitionProvider` and `JsonCraftingDefinitionProvider`.      |
| One standard material per family+tier validation | Implemented | Provider validates duplicate standard tiered materials.                        |
| Variant base recipe validation                   | Implemented | Provider validates variant `BaseRecipeId`.                                     |
| Blueprint unlock target validation               | Implemented | Provider validates blueprint `UnlocksRecipeId`.                                |
| Special resource validation                      | Implemented | Provider validates special resource requirements against material definitions. |

### Persistence

| Requirement                                       | Status      | Notes                                                           |
| ------------------------------------------------- | ----------- | --------------------------------------------------------------- |
| Add item quality to equipment instances           | Implemented | Added `ItemQuality` and `EquipmentInstance.Quality`.            |
| Add recipe identity fields to equipment instances | Implemented | Added `RecipeId` and `BaseRecipeId`.                            |
| Add tier to equipment instances                   | Implemented | Added `Tier`.                                                   |
| Add max potential to equipment instances          | Implemented | Added `MaxPotential`.                                           |
| Add tempering progress to equipment instances     | Implemented | Added `TemperingProgress`.                                      |
| Add affinity tags to equipment instances          | Implemented | Added `AffinityTags`.                                           |
| Add special modifiers to equipment instances      | Implemented | Added `SpecialModifiers`.                                       |
| Character recipe unlock persistence               | Implemented | Added `CharacterRecipeUnlock` and EF configuration.             |
| Character recipe mastery persistence              | Implemented | Added `CharacterRecipeMastery` and EF configuration.            |
| EF migrations                                     | Implemented | Added `AddEquipmentItemQuality` and `AddCraftingV2Progression`. |

### Commands and Queries

| Design item                         | Status      | Notes                                                                                                                               |
| ----------------------------------- | ----------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `CraftItemsCommand`                 | Implemented | Resolves costs, consumes materials, rolls quality, creates Common equipment, assigns potential, updates mastery.                    |
| `LearnBlueprintCommand`             | Implemented | Consumes blueprint item and creates persistent unlock. Duplicate unlocks are rejected.                                              |
| `TemperItemCommand`                 | Implemented | Applies selected tempering recipe, spends Potential, rolls outcome, progresses rarity, applies stat/affix/special modifier effects. |
| `GetCraftingRecipesQuery`           | Implemented | Returns base recipes plus unlocked variants, with resolved costs and owned material counts.                                         |
| `GetRecipeMasteriesQuery`           | Implemented | Returns persisted recipe mastery rows.                                                                                              |
| `GetAvailableTemperingRecipesQuery` | Implemented | Returns tempering directions applicable to the selected item.                                                                       |

### API Endpoints

| Endpoint from design                                 | Status      | Implemented route                                    |
| ---------------------------------------------------- | ----------- | ---------------------------------------------------- |
| `GET /api/crafting/recipes`                          | Implemented | `GET Crafting/recipes?targetTier=...`                |
| `POST /api/crafting/craft`                           | Implemented | `POST Crafting/craft`                                |
| `GET /api/crafting/mastery`                          | Implemented | `GET Crafting/mastery`                               |
| `POST /api/crafting/blueprints/learn`                | Implemented | `POST Crafting/blueprints/learn`                     |
| `GET /api/crafting/items/{itemId}/tempering-options` | Implemented | `GET Crafting/items/{itemId:guid}/tempering-options` |
| `POST /api/crafting/temper`                          | Implemented | `POST Crafting/temper`                               |

### Frontend

| Requirement                                 | Status      | Notes                                                                    |
| ------------------------------------------- | ----------- | ------------------------------------------------------------------------ |
| Crafting UI uses recipe/tier/quantity       | Implemented | Regular crafting UI now uses V2 recipes and batch quantity.              |
| No manual material selection in crafting UI | Implemented | Material slots/manual picks were removed from the V2 craft path.         |
| Required and owned materials shown          | Implemented | UI displays material costs and owned counts for the selected batch.      |
| Current recipe mastery shown                | Implemented | Craft panel displays mastery level.                                      |
| Locked variants hidden from craftable list  | Implemented | Backend only returns base recipes and unlocked variants.                 |
| Tempering UI selects item and direction     | Implemented | Tempering screen loads available directions for selected equipment.      |
| Tempering UI has no material slots          | Implemented | Tempering UI spends Potential only.                                      |
| Item models updated for V2 fields           | Implemented | Player and dashboard item contracts include quality/V2 equipment fields. |

### Content Added

| Content area                 | Status      | Notes                                                                                                       |
| ---------------------------- | ----------- | ----------------------------------------------------------------------------------------------------------- |
| Standard material item bases | Implemented | Added missing resource item bases such as `ore`, `wood`, `copper_ore`, `verdant_ore`, `crystalline_powder`. |
| Special resource item bases  | Implemented | Added `venom_gland`, `royal_chitin_plate`, and `hive_ichor`.                                                |
| Blueprint item bases         | Implemented | Added `blueprint_venom_touched_sword` and `blueprint_hivefang_dagger`.                                      |
| Starter base recipes         | Implemented | Added recipes including shortsword, dagger, towershield, staff, heavy breastplate, and band of fury.        |
| Starter variant recipes      | Implemented | Added venom/hive-themed variants.                                                                           |
| Starter tempering directions | Implemented | Added generic and poison-oriented tempering recipes.                                                        |

## Partially Implemented

### Design Rule Enforcement Outside Crafting V2

| Requirement                                                  | Status  | Gap                                                                                                                                                           |
| ------------------------------------------------------------ | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Equipment is never dropped from combat                       | Partial | Crafting V2 creates equipment correctly, but the global combat/loot system was not fully audited or changed to enforce "no equipment from combat" everywhere. |
| Equipment can only be received through crafting              | Partial | V2 crafting follows this rule. Existing reward/loot systems may still need review.                                                                            |
| Tools can only be received from dungeons                     | Partial | Tools are blocked from V2 crafting and tempering, but dungeon reward tables were not updated as part of this pass.                                            |
| Dungeons can reward tools, blueprints, and special resources | Partial | Blueprint/special resource item bases and definitions exist, but dungeon reward integration was not wired.                                                    |

### Recipe and Material Content Breadth

| Requirement                                                               | Status  | Gap                                                                                                                                                                   |
| ------------------------------------------------------------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Base recipes should match currently implemented equipment in `items.json` | Partial | A starter set was added, not a complete recipe for every valid equipment base in the catalog.                                                                         |
| Recommended material families                                             | Partial | Implemented families include Metal, Wood, Hide, Crystal, Stone, Chitin, Resin, and Oil. Fiber and Bone are not currently used.                                        |
| Multi-tier material coverage                                              | Partial | Starter materials cover only the tiers needed by the starter recipe set. This is not a complete tier 1-10 economy.                                                    |
| Variant library                                                           | Partial | Two variants were added. The broader examples from the design, such as Flame-Scarred, Wraithbound, Mineforged, Royal Carapace, Gravebound, etc., are not implemented. |

### Configurability

| Requirement                                                   | Status  | Gap                                                                                                                                           |
| ------------------------------------------------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Quality odds should be configurable                           | Partial | Quality odds are in `ItemQualityRollService`, not external JSON/config.                                                                       |
| Potential formula should be configurable                      | Partial | Potential multipliers and weights are in `ItemPotentialService`, not external JSON/config.                                                    |
| Tier budgets should be configurable                           | Partial | Tier/stat budget logic is service code, not `crafting/tier-budgets.json`.                                                                     |
| Affix and special modifier pools should be separately defined | Partial | Affix/special modifier IDs are embedded in tempering recipe definitions. Separate `affixes.json` and `special-modifiers.json` were not added. |

### Crafting Operation Details

| Requirement                                  | Status  | Gap                                                                                                                                                                |
| -------------------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Character existence validation               | Partial | V2 commands validate inventory/ownership/output conditions, but there is no explicit character existence check in `CraftItemsCommand`.                             |
| Explicit transaction boundary                | Partial | The command uses existing services and DbContext flow, but no explicit transaction wrapper was added around material removal, item creation, mastery XP, and save. |
| Crafting level affects Potential             | Partial | Potential considers target tier, item type, quality, and recipe mastery. A separate global crafting level is not currently used.                                   |
| Recipe definition affects Potential directly | Partial | Potential uses the item/equipment type and tier. It does not currently read recipe-specific potential modifiers.                                                   |

### Tempering Architecture

| Requirement                              | Status  | Gap                                                                                                      |
| ---------------------------------------- | ------- | -------------------------------------------------------------------------------------------------------- |
| `ITemperingOutcomeRollService`           | Partial | Outcome rolling exists inside `TemperItemCommand`, not in a separate service.                            |
| `ITemperingProgressService`              | Partial | Progress logic exists inside `TemperItemCommand`, not in a separate service.                             |
| `IAffixRollService`                      | Partial | Affix selection exists inside `TemperItemCommand`, not in a separate service.                            |
| `ISpecialModifierRollService`            | Partial | Special modifier selection exists inside `TemperItemCommand`, not in a separate service.                 |
| Existing `ITemperingService` integration | Partial | The new V2 tempering path is command-driven and does not deeply refactor the existing tempering service. |

### API Coverage

| Endpoint from design                   | Status   | Gap                                                                                      |
| -------------------------------------- | -------- | ---------------------------------------------------------------------------------------- |
| `GET /api/crafting/recipes/{recipeId}` | Not done | No dedicated recipe details endpoint was added.                                          |
| `GET /api/crafting/tempering-recipes`  | Not done | Tempering recipes are exposed as item-specific options, not as a standalone list.        |
| `GetCraftingRecipeDetailsQuery`        | Not done | Not added.                                                                               |
| `GetItemTemperingOptionsQuery`         | Partial  | Functionally covered by `GetAvailableTemperingRecipesQuery`, but not named as suggested. |

### Frontend UX

| Requirement                      | Status   | Gap                                                                                                                                                 |
| -------------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Recipe list grouped by base item | Partial  | Recipes are listed and filterable, but not shown as nested groups under each base recipe.                                                           |
| Expected quality odds displayed  | Not done | UI shows mastery level but not quality probability distribution.                                                                                    |
| Possible outcomes displayed      | Partial  | Tempering recipe data is available, but the UI mainly shows direction and potential cost. It does not fully present outcome probabilities/progress. |
| Possible affixes displayed       | Partial  | Tempering option data exists, but the UI does not fully display affix and special modifier pools.                                                   |
| Craft result sentence            | Partial  | The UI updates inventory/results, but the exact design sentence format is not fully implemented.                                                    |

## Not Done

### Dedicated Test Coverage

No Crafting V2-specific automated tests were added.

The design requested tests for:

- Base recipe crafting creates Common item.
- Variant crafting creates Common item.
- Crafting never creates Rare/Epic/Legendary item.
- Quality roll behavior.
- Potential assignment.
- Correct tiered material consumption.
- Variant special resource consumption.
- Locked variant failure.
- Invalid tier failure.
- Tool craft failure.
- Recipe mastery increase.
- Higher mastery quality odds.
- Blueprint unlock behavior.
- Duplicate blueprint behavior.
- Tempering Potential consumption.
- Tempering material non-consumption.
- Affinity-gated tempering.
- Rarity progress and affix gain.
- Tool tempering failure.
- Zero-Potential tempering failure.
- Material definition validation.

Current verification only confirms that existing tests still pass.

### Separate Static Files

The following recommended definition files were not added:

- `crafting/affixes.json`
- `crafting/special-modifiers.json`
- `crafting/tier-budgets.json`

The current implementation embeds those concerns in `tempering-recipes.json` and service code.

### Dungeon Integration

Not implemented:

- Add blueprint drops to dungeon rewards.
- Add special resource/catalyst drops to dungeon rewards.
- Verify tools are only dungeon rewards.
- Globally prevent equipment from combat/dungeon rewards.
- Add the design's dungeon-specific reward examples.

### Optional Later Features

Not implemented, and listed as later/optional in the design:

- Research Notes from duplicate blueprints.
- `SalvageItemsCommand`.
- `BatchTemperItemCommand`.

## Acceptance Criteria Status

### Crafting

| Acceptance criterion                                                       | Status      |
| -------------------------------------------------------------------------- | ----------- |
| Base recipes are available without blueprint unlocks                       | Implemented |
| Variant recipes are unavailable until blueprint unlock                     | Implemented |
| Player can craft base recipe at valid tiers                                | Implemented |
| Player can craft unlocked variant at valid tiers                           | Implemented |
| Crafting request does not include material selections                      | Implemented |
| Crafting consumes resolved tiered materials automatically                  | Implemented |
| Crafting consumes special resources only if selected variant requires them | Implemented |
| Crafting always creates Common equipment                                   | Implemented |
| Crafting rolls Quality                                                     | Implemented |
| Crafting creates Potential based on Quality and Recipe Mastery             | Implemented |
| Crafting increases Recipe Mastery                                          | Implemented |
| Tools cannot be crafted                                                    | Implemented |

### Blueprints

| Acceptance criterion                                             | Status      |
| ---------------------------------------------------------------- | ----------- |
| Blueprint item can unlock a variant recipe                       | Implemented |
| Learning a blueprint creates persistent recipe unlock            | Implemented |
| Learning a duplicate blueprint does not create duplicate unlocks | Implemented |
| Unlocked variants appear in available recipe list                | Implemented |
| Base recipes are available without unlock records                | Implemented |

### Tempering

| Acceptance criterion                                            | Status      |
| --------------------------------------------------------------- | ----------- |
| Tempering consumes Potential only                               | Implemented |
| Tempering does not consume any materials                        | Implemented |
| Tempering cannot be performed on tools                          | Implemented |
| Tempering uses selected Tempering Recipe to determine direction | Implemented |
| Tempering can improve item stats                                | Implemented |
| Tempering can increase rarity progress                          | Implemented |
| Rarity can only increase through tempering                      | Partial     |
| Rarity upgrade adds affix from selected Tempering Recipe pool   | Implemented |
| Special modifiers only appear at configured rarities            | Implemented |
| Item cannot be tempered when Potential is insufficient          | Implemented |

Note on "Rarity can only increase through tempering": the V2 craft path and adjusted legacy craft path follow this, but the whole codebase was not exhaustively audited for every possible rarity mutation.

### Materials and Market Cleanliness

| Acceptance criterion                                                        | Status                             |
| --------------------------------------------------------------------------- | ---------------------------------- |
| There is only one standard material per family per tier                     | Implemented                        |
| No multiple equivalent Tier 2 metals                                        | Implemented in current definitions |
| No multiple equivalent Tier 3 woods                                         | Implemented in current definitions |
| Special resources are separate non-tiered items                             | Implemented                        |
| Special resources are only consumed by recipes that explicitly require them | Implemented                        |

## Files Added or Changed

### Backend

- `LL/src/API/API.LL/Controllers/V1/CraftingController.cs`
- `LL/src/API/API.LL/Data/crafting/materials.json`
- `LL/src/API/API.LL/Data/crafting/base-recipes.json`
- `LL/src/API/API.LL/Data/crafting/recipe-variants.json`
- `LL/src/API/API.LL/Data/crafting/blueprints.json`
- `LL/src/API/API.LL/Data/crafting/tempering-recipes.json`
- `LL/src/API/API.LL/Data/items.json`
- `LL/src/Core/Application/UseCases/Crafting/**`
- `LL/src/Core/Domain/Models/Professions/Crafting/V2/**`
- `LL/src/Core/Domain/Models/Professions/Crafting/CharacterRecipeUnlock.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/CharacterRecipeMastery.cs`
- `LL/src/Core/Domain/Models/Items/ItemQuality.cs`
- `LL/src/Core/Domain/Models/Items/Equipments/EquipmentInstance.cs`
- `LL/src/Core/Application/UseCases/Equipments/Dtos/EquipmentInstanceDto.cs`
- `LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/**`
- `LL/src/Infrastructure/Service/Services.LL/DependencyInjection.cs`

### Persistence

- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Professions/Crafting/CharacterRecipeUnlockConfiguration.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Professions/Crafting/CharacterRecipeMasteryConfiguration.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260624143520_AddEquipmentItemQuality.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260624143520_AddEquipmentItemQuality.Designer.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260624145236_AddCraftingV2Progression.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260624145236_AddCraftingV2Progression.Designer.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/LLDbContextModelSnapshot.cs`

### Frontend

- `LL/src/Presentation/ll/src/app/shared/models/crafting-v2.ts`
- `LL/src/Presentation/ll/src/app/shared/models/item.ts`
- `LL/src/Presentation/ll/src/app/shared/models/enums/itemQuality.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/crafting/crafting.service.ts`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/regular-crafting/regular-crafting.component.ts`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/regular-crafting/regular-crafting.component.html`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/tempering/tempering.component.ts`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/tempering/tempering.component.html`
- `LL/src/Presentation/dashboard/src/app/shared/models/item.ts`
- `LL/src/Presentation/dashboard/src/app/shared/models/enums/itemQuality.ts`

## Verification Performed

These commands passed:

```powershell
dotnet build LL/LegendsLegacy.sln --nologo -v:q
dotnet test LL/LegendsLegacy.sln --no-build --nologo -v:q
```

The test command reported:

```text
Passed: 161
Failed: 0
Skipped: 0
```

Both Angular builds passed:

```powershell
cd LL/src/Presentation/ll
.\node_modules\.bin\ng.cmd build --configuration development

cd LL/src/Presentation/dashboard
.\node_modules\.bin\ng.cmd build --configuration development
```

A targeted crafting JSON reference check was also run and passed. It verified:

- No duplicate item IDs in `items.json`.
- Every crafting material `itemId` exists in `items.json`.
- Every recipe `outputItemId` exists in `items.json`.
- Every blueprint `itemId` exists in `items.json`.

## Migration and Deployment Notes

The migrations were generated but not applied.

Before using Crafting V2 in a deployed environment:

1. Apply the new EF migrations.
2. Deploy the new `Data/crafting` JSON files with the API content files.
3. Deploy the updated `items.json`.
4. Add dungeon reward integration for blueprint and special-resource drops if players need to acquire the new variant unlocks naturally.
5. Add dedicated tests before expanding the recipe/material economy further.
