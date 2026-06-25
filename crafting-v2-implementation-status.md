# Crafting V2 Implementation Status

Source design documents:

- `C:/Users/HrHoe/Downloads/crafting-v2-design-for-codex.md`
- `C:/Users/HrHoe/.codex/attachments/e0b1d947-283b-491b-9904-78225956f1e5/pasted-text.txt`

Updated against the current repository state on 2026-06-25.

## Status Key

| Status | Meaning |
| --- | --- |
| Added | Implemented in code/content and wired into the current player flow. |
| Partially added | The foundation exists, but coverage, polish, migration/application, tests, or content breadth still needs work. |
| Not added yet | The feature/design item is not implemented or not meaningfully wired. |

## High-Level Overview

Crafting V2 is now a working broad-recipe crafting system.

The active shape is:

```text
base recipe + form + optional learned blueprint = crafted equipment instance
```

Example:

```text
recipe_ring + band + blueprint_fury = Band of Fury
```

The old exact-recipe model has been retired from active gameplay. Recipes now define broad equipment categories, forms define the concrete physical output, and blueprints define variant/theme identity.

## Added

### Crafting Data Model

| Area | Added state |
| --- | --- |
| Broad base recipes | 9 active base recipes. |
| Forms | 35 forms across armor, weapons, offhands, accessories, and relics. |
| Blueprint families | 11 blueprint families with compatibility rules and naming. |
| Materials | 33 material definitions, including 30 standard Region One tiered materials and 3 special resources. |
| Tempering recipes | 15 Region One tempering directions. |
| Affixes | 31 standalone tempering affix definitions. |
| Special modifiers | 9 standalone tempering special modifier definitions. |
| Tier budgets | Rarity progress thresholds loaded from `tier-budgets.json`. |

Active Crafting V2 JSON files:

- `LL/src/API/API.LL/Data/crafting/base-recipes.json`
- `LL/src/API/API.LL/Data/crafting/blueprints.json`
- `LL/src/API/API.LL/Data/crafting/materials.json`
- `LL/src/API/API.LL/Data/crafting/tempering-recipes.json`
- `LL/src/API/API.LL/Data/crafting/affixes.json`
- `LL/src/API/API.LL/Data/crafting/special-modifiers.json`
- `LL/src/API/API.LL/Data/crafting/tier-budgets.json`

Retired legacy JSON files:

- `LL/src/API/API.LL/Data/crafting/recipe-variants.json`
- `LL/src/API/API.LL/Data/recipes.json`

### Broad Recipes and Forms

The current broad recipes are:

| Recipe | Forms |
| --- | --- |
| `recipe_head_armor` | Heavy helm, medium helm, light hood, cloth cowl |
| `recipe_chest_armor` | Heavy breastplate, medium mail, light vest, cloth robe |
| `recipe_leg_armor` | Heavy legplates, medium greaves, light legwraps, cloth pants |
| `recipe_ring` | Band |
| `recipe_necklace` | Amulet, charm, talisman |
| `recipe_relic` | Vial, heart, totem |
| `recipe_one_handed_weapon` | Shortsword, dagger, hand axe, mace, wand |
| `recipe_two_handed_weapon` | Greatsword, battle axe, maul, spear, staff, longbow, crossbow, gauntlets |
| `recipe_offhand` | Towershield, spiritward, grimoire |

Added behavior:

- Base recipes are available without blueprint unlocks.
- Forms determine the concrete equipment base.
- Crafting request uses `recipeId`, optional `formId`, optional `blueprintId`, `targetTier`, and `quantity`.
- Crafting does not ask the player to manually select materials.
- The backend resolves material requirements from recipe, tier, form, and blueprint.
- The Angular crafting screen exposes form selection.
- The tier field was replaced with selectable tier buttons based on the recipe tier range.

### Blueprint Learning and Usage

Added behavior:

- Blueprint items can be learned from inventory.
- Blueprint learning opens a modal and shows compatible recipe targets.
- The player chooses which recipe the blueprint should apply to.
- Learning no longer grants the blueprint to all compatible recipes.
- Unlock persistence is now per `(CharacterId, RecipeId, BlueprintId)`.
- Duplicate learning for the same recipe and blueprint is rejected.
- Duplicate blueprint copies remain useful because the same blueprint can be learned for another compatible recipe.
- `GetCraftingRecipes` now returns learned blueprint options under only the specific recipe they were learned for.
- Crafting validates that the selected blueprint is learned for the selected recipe.
- Crafting validates blueprint compatibility against recipe/form/tag data.
- Blueprint special-resource costs are included in material cost resolution.
- Blueprint special-resource costs are consumed by the backend craft flow when crafting a selected blueprint outcome.
- Crafted equipment stores `BlueprintId`.
- Crafted equipment stores generated display name through `CraftedName`.
- Blueprint tags are added to crafted equipment affinity tags.

Added API:

- `GET Crafting/blueprints/{blueprintItemInstanceId}/learning-options`
- `POST Crafting/blueprints/learn` now accepts `blueprintItemInstanceId` and `recipeId`.

### Crafting Output and Inventory Updates

Added behavior:

- Crafting consumes resolved inventory materials.
- Crafting adds created equipment to the inventory state on the frontend.
- Crafted output names can come from blueprint `specialOutputNames` or `outputNameTemplate`.
- Base accessory/relic forms use neutral base items such as `band`, `amulet`, `charm`, `talisman`, `vial`, `heart`, and `totem`.
- Obsolete named accessory/relic item records were removed from `items.json`.
- Inventory rows no longer show leading item icons such as equipment slot icons, essence icons, or `BP` text before item names.

Removed obsolete item IDs:

- `band_of_fury`
- `band_of_arcane`
- `band_of_execution`
- `amulet_of_aegis`
- `charm_of_the_warden`
- `talisman_of_endurance`
- `phoenix_vial`
- `mana_heart`
- `primal_totem`

### Recipe Mastery and Quality

Added behavior:

- Crafting grants recipe mastery experience.
- Recipe mastery now uses an exponential progression curve instead of leveling roughly every few crafts.
- Crafted equipment receives item quality.
- Potential is generated from quality and mastery rules.
- Potential is no longer represented as a separate item data type.
- Tools are excluded from crafting and tempering.

### Tempering

Added behavior:

- Tempering uses the existing `CharacterActions` flow.
- Tempering actions can be queued and progress over time.
- The tempering screen shows the current working item and queued tempering items.
- Tempering costs 1 Potential per action; the UI no longer displays a redundant "Cost: 1 Potential" data field.
- Tempering consumes Potential only.
- Tempering does not consume materials.
- Tempering can improve item stats.
- Tempering can increase rarity progress.
- Rarity upgrade adds affixes from the selected tempering recipe pool.
- Special modifiers are gated by configured rarity rules.
- Rarity/progression uses the existing 7-rarity item model from Common through Legacy.
- Tempering rarity thresholds are loaded from `tier-budgets.json`.

### Region One Content and Acquisition

Added behavior:

- Region One standard material families have tier 1-3 definitions.
- Region One standard materials have dungeon gathering sources.
- Blueprint items have Region One dungeon first-clear acquisition sources.
- Special resources have dungeon gathering or repeat-completion sources.
- Generic dungeon completion rewards support repeat rewards through `rewardTable.completionRewards`.
- Goblin Mines material references were updated to V2 material IDs.

Region One standard material families:

- Metal
- Wood
- Hide
- Crystal
- Stone
- Fiber
- Bone
- Chitin
- Resin
- Oil

Special resources:

- `venom_gland`
- `royal_chitin_plate`
- `hive_ichor`

### DTOs, Mapping, and Layering

Added behavior:

- Crafting DTOs use `IMapFrom`/AutoMapper profiles where practical.
- Manual command/query mapping was reduced.
- Active Crafting V2 command/query handlers now delegate business logic to `ICraftingService`.
- `CraftItemsCommand`, `LearnBlueprintCommand`, `GetCraftingRecipesQuery`, and `GetBlueprintLearningOptionsQuery` are thin MediatR adapters.
- Crafting business rules now live in `CraftingService`: recipe validation, form resolution, blueprint compatibility/unlock checks, material resolution, inventory consumption, crafted item creation, and mastery progression.
- Crafting services use repository/service abstractions rather than handler-level database access.
- Repository support was added for recipe unlocks, blueprint unlocks by recipe, and mastery data.

### Legacy Recipe Retirement

Added cleanup:

- Old `Data/recipes.json` was deleted.
- Old `recipe-variants.json` was deleted.
- Old player-facing `Crafting/CraftItem` endpoint was removed.
- Old static frontend recipe mirror path was retired.
- Old Admin Dashboard recipe editor route/sidebar/API/service surface was removed.
- Old `Recipe` persistence model/table path was removed.
- Migration exists to drop legacy `Recipes` and `Material` tables.

Current distinction:

- `CharacterRecipeUnlock` and `CharacterRecipeMastery` are current Crafting V2 progression data.
- They are not part of the retired old recipe table model.

### Related Frontend Polish

Added:

- Character overview page has a `Refresh` button.
- Refresh pulls fresh overview data without refreshing the whole browser.
- Refresh respects current mode: searched character overview or current character overview.
- Crafting no longer displays raw enum-style labels such as `OffensiveScaling`, `StatusScaling`, or `OneHanded`; labels are split into readable words.
- The crafting panel's old `Affinity` slice was replaced with a clearer `Tempering profile` section.
- `Tempering profile` explains that these tags determine which stat and affix themes the item can temper into.
- Tempering profile tags are grouped by source: base item, selected form, and selected blueprint.

## Partially Added

### Database Migrations and Environment Application

The migration files exist, but they have not been applied by Codex.

Important migrations include:

- `20260624143520_AddEquipmentItemQuality`
- `20260624145236_AddCraftingV2Progression`
- `20260624181244_AddBlueprintCraftingOutputs`
- `20260624183822_DropLegacyRecipes`
- `20260624221539_AddTemperingRecipeIdToCraftingQueueItems`
- `20260625123000_AllowMultipleBlueprintUnlocksPerRecipe`

Remaining work:

- Apply migrations to local/dev databases when ready.
- Verify any existing local data survives or is intentionally reset.
- Confirm deployment packaging removes deleted legacy JSON/static files.

### Region One Content Balance

Region One now has a full functional content plate, but balance is still first-pass.

Remaining work:

- Tune material quantities per recipe tier.
- Tune blueprint special-resource requirements.
- Tune dungeon gathering drop rates.
- Tune repeat-completion reward weights.
- Tune tempering affix weights and rarity outcomes.
- Confirm whether all 11 blueprint families are desirable in Region One or whether some should be delayed to later regions.

### Blueprint Outcome Catalog Strategy

Blueprint outcomes are generated on crafted equipment instances, not represented as separate item base IDs.

Current behavior:

- Neutral item base ID remains the equipment base, such as `band`.
- Instance-level `CraftedName` can become `Band of Fury`.
- Instance-level `BlueprintId` stores the blueprint identity.

Remaining decision:

- Keep instance-generated names only, or add a generated/read-only catalog of blueprint outcomes for tooling, search, admin views, or external references.

### Automated Test Coverage

Added tests cover important pieces, but not every gameplay path.

Covered:

- Blueprint compatibility rules.
- Blueprint output naming rules.
- DTO mapping coverage.
- Crafting mastery progression.
- Region One content/data coverage.
- Dungeon acquisition/source coverage.

Still useful:

- End-to-end `CraftItemsCommand` tests for recipe + form + blueprint crafting.
- Invalid form ID behavior.
- Invalid blueprint compatibility behavior.
- Inventory resource consumption after crafting.
- Blueprint learning option query behavior.
- Duplicate blueprint learning behavior per recipe.
- Tempering queue behavior through `CharacterActions`.
- Rarity upgrade behavior using the full Common-to-Legacy ladder.

### Frontend Runtime Verification

Angular compile/build checks pass, but full browser-level verification is still partial.

Verified:

- Angular TypeScript compile check with `tsc --noEmit` from an earlier pass.
- Angular development build from the latest pass.
- Backend application/service/API builds.
- Focused backend test suite from an earlier pass.

Still useful:

- Manual in-browser pass through crafting, blueprint learning, crafting after learning, inventory updates, tempering queue, and overview refresh.
- Playwright or equivalent browser coverage for the crafting UI flow.
- Full Angular production build once local dependency state is healthy.

### Broader Layering Cleanup

Active Crafting V2 handlers now follow command/query -> service direction for the main crafting and blueprint flows.

Remaining work:

- Audit non-crafting commands/queries for direct `IDbContext` access if this layering rule should become global.
- Audit services outside the recent Crafting V2 scope for direct DB access.
- Add architecture tests if you want this rule enforced automatically.

## Not Added Yet

### Full Browser Automation Coverage

Not added yet:

- Automated UI test for learning a blueprint and choosing a recipe.
- Automated UI test for crafting with a learned blueprint.
- Automated UI test for material counts decreasing after crafting.
- Automated UI test for queued tempering display.
- Automated UI test for the character overview refresh button.

### Production-Ready Balancing

Not added yet:

- Finalized Region One crafting economy numbers.
- Finalized mastery XP curve tuning from real playtesting.
- Finalized quality/potential distribution tuning.
- Finalized tempering affix and special modifier weights.
- Finalized dungeon reward/drop-rate tuning.

### Admin Tooling for Crafting V2

Not added yet:

- Admin editor for base recipes.
- Admin editor for recipe forms.
- Admin editor for blueprints.
- Admin editor for materials.
- Admin editor for tempering affixes/special modifiers.
- Admin validation display for broken crafting JSON references.

The old legacy recipe editor was intentionally removed instead of converted.

### Generated Crafting Outcome Views

Not added yet:

- A UI/admin view that expands every valid recipe + form + blueprint combination into previewed possible outputs.
- A searchable catalog of generated blueprint outcome names.
- A balancing report showing all required materials and special resources by tier.

### Broader Loot Audit

Not fully added yet:

- Global audit to ensure equipment rewards are removed or adjusted from every old combat/loot path if crafting is meant to become the primary equipment source.
- Global audit to ensure all new crafting materials have intentional acquisition paths outside the currently touched Region One content.

### Later-Region Content

Not added yet:

- Region Two+ materials.
- Region Two+ blueprint families.
- Region Two+ special resources.
- Region Two+ tempering directions.
- Late-game rarity/balance tuning through Legacy.

## Acceptance Criteria Snapshot

### Crafting

| Requirement | Status |
| --- | --- |
| Base recipes are available without blueprint unlocks | Added |
| Player can craft base recipe at valid tiers | Added |
| Crafting request avoids manual material selection | Added |
| Crafting consumes resolved tiered materials automatically | Added |
| Crafting creates equipment with quality and potential | Added |
| Crafting increases recipe mastery | Added |
| Recipe mastery uses slower exponential progression | Added |
| Tools cannot be crafted | Added |
| Player can select form instead of exact old recipe | Added |
| Player can select tier without free-text input | Added |
| Player can craft unlocked blueprint outcome | Added |
| Blueprint special resources are consumed only when selected | Added |
| Crafting handlers delegate business rules to service layer | Added |

### Blueprints

| Requirement | Status |
| --- | --- |
| Blueprint item can be learned | Added |
| Learning a blueprint creates persistent unlock | Added |
| Player chooses which compatible recipe receives the blueprint | Added |
| Blueprint does not unlock all compatible recipes at once | Added |
| Duplicate learning is prevented for same recipe + blueprint | Added |
| Duplicate blueprint copies can be used for other compatible recipes | Added |
| Learned blueprints appear under specific compatible recipe | Added |
| Blueprint-derived outcome can be crafted | Added |

### Tempering

| Requirement | Status |
| --- | --- |
| Tempering uses `CharacterActions` flow | Added |
| Tempering can be queued | Added |
| Tempering queue is visible | Added |
| Tempering consumes Potential only | Added |
| Tempering always costs 1 Potential | Added |
| Tempering cost data type removed from item data | Added |
| Tempering can improve stats and rarity progress | Added |
| Rarity upgrade adds affixes | Added |
| Rarity uses existing Common-to-Legacy model | Added |
| Crafting UI explains tempering profile/affinity tags | Added |
| Enum-style labels display as readable words | Added |

### Data Cleanliness

| Requirement | Status |
| --- | --- |
| Broad base recipe count is 9 | Added |
| Forms determine physical output | Added |
| Blueprints define theme/variant identity | Added |
| Old exact recipe variants file removed | Added |
| Old `Data/recipes.json` removed | Added |
| Obsolete named accessory/relic items removed | Added |
| Legacy alias fields removed from active V2 JSON/schema | Added |
| Region One has standard material coverage | Added |
| Region One has blueprint acquisition | Added |
| Region One has special-resource acquisition | Added |

## Files Added or Heavily Changed

### Backend and Domain

- `LL/src/Core/Domain/Models/Professions/Crafting/V2/*`
- `LL/src/Core/Domain/Models/Professions/Crafting/CharacterRecipeUnlock.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/CharacterRecipeMastery.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/CraftingMasteryProgression.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/ICraftingRepository.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/Professions/ICraftingDefinitionProvider.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/Professions/ICraftingProgressionService.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/Professions/ICraftingRequirementResolver.cs`
- `LL/src/Core/Application/UseCases/Crafting/Commands/CraftItems/CraftItemsCommand.cs`
- `LL/src/Core/Application/UseCases/Crafting/Commands/LearnBlueprint/LearnBlueprintCommand.cs`
- `LL/src/Core/Application/UseCases/Crafting/Queries/GetCraftingRecipes/GetCraftingRecipesQuery.cs`
- `LL/src/Core/Application/UseCases/Crafting/Queries/GetBlueprintLearningOptions/GetBlueprintLearningOptionsQuery.cs`
- `LL/src/Core/Application/UseCases/Crafting/Queries/GetAvailableTemperingRecipes/GetAvailableTemperingRecipesQuery.cs`
- `LL/src/Core/Application/UseCases/Crafting/Dtos/*`
- `LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/*`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Professions/Craftings/CraftingRepository.cs`
- `LL/src/API/API.LL/Controllers/V1/CraftingController.cs`

### Frontend

- `LL/src/Presentation/ll/src/app/shared/models/crafting-v2.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/crafting/crafting.service.ts`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/regular-crafting/*`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/tempering/*`
- `LL/src/Presentation/ll/src/app/shared/components/modal-container/item-modals/inventory-item-modal/*`
- `LL/src/Presentation/ll/src/app/shared/components/inventory-item/*`
- `LL/src/Presentation/ll/src/app/shared/components/market-place/market-place-inventory-item/*`
- `LL/src/Presentation/ll/src/app/features/game/character/character-overview/*`

### Content

- `LL/src/API/API.LL/Data/crafting/*.json`
- `LL/src/API/API.LL/Data/items.json`
- `LL/src/API/API.LL/Data/dungeons.json`

### Tests

- `LL/tests/EssenceSystem.Tests/CraftingBlueprintRulesTests.cs`
- `LL/tests/EssenceSystem.Tests/CraftingDtoMappingTests.cs`
- `LL/tests/EssenceSystem.Tests/CraftingMasteryProgressionTests.cs`
- `LL/tests/EssenceSystem.Tests/CraftingRegionOneContentTests.cs`

## Verification Performed

Latest focused verification:

```powershell
dotnet build LL/src/Core/Application/Application.csproj --no-restore --nologo -v:q
dotnet build LL/src/Infrastructure/Service/Services.LL/Services.LL.csproj --no-restore --nologo -v:q
dotnet build LL/src/API/API.LL/API.LL.csproj --no-restore --nologo -v:q -p:OutputPath="C:\repos\Legends-Legacy\legends-legacy\LL\tmp\api-build\"
cd LL/src/Presentation/ll
node .\node_modules\@angular\cli\bin\ng.js build --configuration development
```

Results:

- Application build passed.
- Service build passed.
- API build passed.
- Angular development build passed.

Known caveats:

- Builds still show existing warning noise in unrelated files.
- Full Angular production build was not rerun in the latest pass.
- Focused backend tests were not rerun in the latest pass.
- EF migrations were generated/edited but not applied by Codex.
- Browser-level UI verification is still manual.

## Migration and Deployment Notes

Before using this in a fresh environment, apply the Crafting V2 migrations and deploy the updated JSON content.

Deployment needs:

- Updated `Data/crafting/*.json`
- Updated `Data/items.json`
- Updated `Data/dungeons.json`
- Removed legacy `Data/crafting/recipe-variants.json`
- Removed legacy `Data/recipes.json`
- Removed frontend legacy `recipes-content.ts` output

Important behavior changes:

- Crafting equipment now depends on Crafting V2 JSON definitions.
- Blueprint learning now requires a selected recipe target.
- The `CharacterRecipeUnlocks` unique index now includes `BlueprintId`.
- Old recipe tables are intentionally retired.
- Dungeon repeat rewards can use `rewardTable.completionRewards`.

## Next Recommended Focus

1. Run a full manual browser pass: learn blueprint, select target recipe, craft with blueprint, confirm inventory material decrease, queue tempering, confirm queue display.
2. Apply migrations to local/dev and verify schema/data after reset.
3. Add integration tests for `CraftItemsCommand` and blueprint learning option query.
4. Tune Region One material costs, special resources, and dungeon acquisition rates.
5. Decide whether generated blueprint outcomes should remain instance-only or get a tooling/catalog projection.
