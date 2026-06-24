# Crafting V2 Implementation Status

Source design documents:

- `C:/Users/HrHoe/Downloads/crafting-v2-design-for-codex.md`
- `C:/Users/HrHoe/.codex/attachments/e0b1d947-283b-491b-9904-78225956f1e5/pasted-text.txt`

Updated against the current repository state on 2026-06-24.

## Status Key

| Status | Meaning |
| --- | --- |
| Implemented | The feature exists in code/content and is wired into the current flow. |
| Partial | The data/schema exists, but runtime behavior, content breadth, tests, or acquisition wiring is incomplete. |
| Not done | The requested feature or design requirement has not been implemented. |

## Executive Summary

Crafting V2 now has two layers implemented:

1. The original vertical slice: quality, potential, recipe mastery, recipe unlocks, JSON-backed definitions, crafting endpoints, tempering endpoints, migrations, and Angular UI.
2. The newer recipe-data refactor: recipes are broad archetypes with forms, while blueprints carry variant/theme identity and legacy-name compatibility.

The latest JSON refactor changes Crafting V2 from exact item recipes into:

- 9 broad base recipes.
- 35 forms under those recipes.
- 11 blueprint families.
- 33 material definitions.
- `recipe-variants.json` intentionally empty, because exact variants moved to blueprint data.

The largest remaining runtime gap is blueprint materialization:

```text
recipe_ring + band form + blueprint_fury = Band of Fury
```

The JSON schema can now represent this, but `CraftItemsCommand` currently accepts only `recipeId`, `formId`, `targetTier`, and `quantity`. It does not yet accept `blueprintId`, enforce blueprint compatibility at craft time, apply blueprint special resource costs, or create blueprint-named generated outputs. Broad recipe + form crafting works; broad recipe + form + blueprint crafting is still partial.

## Current JSON Shape

| JSON file | Current role | Status |
| --- | --- | --- |
| `Data/crafting/base-recipes.json` | Broad craftable archetypes plus forms | Implemented for requested 9 base recipes and 35 forms |
| `Data/crafting/recipe-variants.json` | Legacy exact-variant recipe file | Intentionally empty |
| `Data/crafting/blueprints.json` | Blueprint families, compatibility tags, output naming, legacy aliases | Implemented as data; craft-time use is partial |
| `Data/crafting/materials.json` | Standard material families/tier lookup plus special resources | Implemented through Region One tier 1-3 standard families |
| `Data/crafting/tempering-recipes.json` | Tempering directions | Starter set still |
| `Data/items.json` | Equipment, resources, blueprint items, neutral form outputs | Updated with neutral form outputs, material bases, and blueprint items; obsolete named accessory/relic output items removed |
| `Data/dungeons.json` | Dungeon acquisition and gathering | Partially updated; `iron_ore` mismatch fixed, blueprint/special-resource rewards not wired |
| `Data/recipes.json` | Legacy recipe content | Still legacy; not loaded by Crafting V2 |

## Counts

| Area | Count |
| --- | ---: |
| Broad base recipes | 9 |
| Forms | 35 |
| Blueprint families | 11 |
| Material definitions | 33 |
| Standard material definitions | 30 |
| Special resource definitions | 3 |

## Implemented

### Broad Recipe Refactor

The requested 9 base recipe IDs now exist:

| Base recipe | Forms |
| --- | --- |
| `recipe_head_armor` | `heavy_helm`, `medium_helm`, `light_hood`, `cloth_cowl` |
| `recipe_chest_armor` | `heavy_breastplate`, `medium_mail`, `light_vest`, `cloth_robe` |
| `recipe_leg_armor` | `heavy_legplates`, `medium_greaves`, `light_legwraps`, `cloth_pants` |
| `recipe_ring` | `band` |
| `recipe_necklace` | `amulet`, `charm`, `talisman` |
| `recipe_relic` | `vial`, `heart`, `totem` |
| `recipe_one_handed_weapon` | `shortsword`, `dagger`, `hand_axe`, `mace`, `wand` |
| `recipe_two_handed_weapon` | `greatsword`, `battle_axe`, `maul`, `spear`, `staff`, `longbow`, `crossbow`, `gauntlets` |
| `recipe_offhand` | `towershield`, `spiritward`, `grimoire` |

Important implementation details:

- Exact item names such as `Band of Fury`, `Phoenix Vial`, and `Charm of the Warden` are no longer base recipes.
- Accessory/relic base forms now use neutral outputs: `band`, `amulet`, `charm`, `talisman`, `vial`, `heart`, and `totem`.
- Old exact names are preserved as `legacyNames` and special blueprint output names. Obsolete named accessory/relic IDs were removed from active V2 alias fields after deleting their item records.
- Armor forms include armor-weight tags for Heavy, Medium, Light, and Cloth identities.
- Weapon/offhand forms include tags for blade, dagger, axe, mace, wand, great weapon, ranged, ward, grimoire, shield, and related identity concepts.

### Definition Schema Support

Added minimal schema support while preserving existing fields:

- `CraftingRecipeDefinition.Forms`
- `CraftingRecipeDefinition.RecipeFamily`
- `CraftingRecipeDefinition.Slot`
- `CraftingRecipeDefinition.Tags`
- `CraftingRecipeDefinition.OutputNameTemplate`
- `CraftingRecipeDefinition.LegacyNames`
- `CraftingRecipeDefinition.LegacyItemIds`
- `CraftingRecipeFormDefinition`
- `BlueprintDefinition.BlueprintFamily`
- `BlueprintDefinition.AllowedBaseRecipeIds`
- `BlueprintDefinition.AllowedRecipeTags`
- `BlueprintDefinition.OutputNameTemplate`
- `BlueprintDefinition.SpecialOutputNames`
- `BlueprintDefinition.Tags`
- `BlueprintDefinition.LegacyNames`
- `BlueprintDefinition.LegacyItemIds`

The provider validates:

- duplicate standard material family+tier definitions
- duplicate form IDs inside a recipe
- recipes with neither output item nor forms
- variant base recipe references when variants exist
- blueprint unlock recipe references when supplied
- blueprint compatible base recipe references
- special resource references
- duplicate tempering recipe IDs

### Crafting Runtime

Implemented:

- `CraftItemsRequestDto.FormId`
- `CraftItemsCommand` now resolves `recipeId + formId` into a concrete existing `EquipmentBase`.
- If no `formId` is supplied for a recipe with forms, the first form is used.
- Created equipment keeps `RecipeId` as the broad recipe ID.
- Created equipment keeps `BaseRecipeId` as the broad recipe ID.
- Form tags are added to crafted equipment affinity tags.
- The Angular crafting UI exposes a Form selector.

Still not implemented:

- `CraftItemsRequestDto.BlueprintId`
- craft-time blueprint compatibility checks
- blueprint special resource consumption
- generated blueprint output naming
- generated item definitions for blueprint outcomes

### Blueprint Learning

Implemented:

- Blueprint definitions can now use compatibility fields instead of only exact variant recipe IDs.
- `LearnBlueprintCommand` supports compatibility-style blueprint unlocks by storing the blueprint ID as the unlock key.
- Exact recipe blueprint behavior remains supported for backward compatibility.
- Duplicate blueprint learning still rejects duplicate unlock rows.

Partial:

- Learned compatibility blueprints are persisted, but the recipe list and craft command do not yet generate selectable blueprint outcomes from those unlocks.

### Materials

`materials.json` now covers all recommended material families for Region One tiers 1-3:

| Family | Tiers |
| --- | --- |
| Metal | 1, 2, 3 |
| Wood | 1, 2, 3 |
| Hide | 1, 2, 3 |
| Crystal | 1, 2, 3 |
| Stone | 1, 2, 3 |
| Fiber | 1, 2, 3 |
| Bone | 1, 2, 3 |
| Chitin | 1, 2, 3 |
| Resin | 1, 2, 3 |
| Oil | 1, 2, 3 |

Special resources currently defined:

- `venom_gland`
- `royal_chitin_plate`
- `hive_ichor`

### Item Catalog

Added item bases for:

- neutral form outputs: `band`, `amulet`, `charm`, `talisman`, `vial`, `heart`, `totem`
- missing standard materials: examples include `rawhide`, `thick_hide`, `scaled_hide`, `soulglass_shard`, `woven_fiber`, `silk_thread`, `bone_fragments`, `grave_bone`, `amber_resin`, `living_resin`, and more
- blueprint family items: `blueprint_fury`, `blueprint_arcane`, `blueprint_execution`, `blueprint_aegis`, `blueprint_warden`, `blueprint_endurance`, `blueprint_phoenix`, `blueprint_spirit`, `blueprint_primal`
- previously added blueprint compatibility items: `blueprint_venom_touched_sword`, `blueprint_hivefang_dagger`

Removed obsolete named accessory/relic item records that are now represented by neutral form outputs plus blueprint names:

- `band_of_fury`
- `band_of_arcane`
- `band_of_execution`
- `amulet_of_aegis`
- `charm_of_the_warden`
- `talisman_of_endurance`
- `phoenix_vial`
- `mana_heart`
- `primal_totem`

Those names still exist as `legacyNames` and/or blueprint `specialOutputNames`, but not as active `items.json` records.

### Dungeon Data

Implemented:

- Fixed the `iron_ore` mismatch in `dungeons.json`.
- Goblin Mines now references V2 material IDs:
  - Grade I: `ore`
  - Grade II: `copper_ore`
  - Grade III: `verdant_ore`

Not implemented:

- blueprint drops
- special resource drops
- full Region One gathering/resource acquisition alignment
- global prevention of equipment rewards from all combat/loot paths

## Partially Implemented

### Blueprint Outcome Crafting

Status: Partial.

The data can represent:

```text
recipe_ring + band + blueprint_fury = Band of Fury
recipe_relic + vial + blueprint_phoenix = Phoenix Vial
recipe_one_handed_weapon + dagger + blueprint_venom = Venom Dagger
recipe_head_armor + heavy_helm + blueprint_aegis = Aegis Heavy Helm
```

But the runtime cannot yet craft those blueprint-selected outcomes.

Required next work:

- Add `blueprintId` to `CraftItemsRequestDto`.
- Add blueprint selection to Angular.
- Validate learned blueprint unlocks at craft time.
- Validate blueprint compatibility against base recipe, form, and tags.
- Add blueprint special resource requirements.
- Decide whether blueprint outcomes reuse legacy item IDs or create generated item definitions.
- Apply `specialOutputNames` and `outputNameTemplate` to the crafted item display/output model.

### Recipe Unlock Semantics

Status: Partial.

Base recipes are available. Blueprint unlock rows can be created. However, unlocked blueprint families do not yet appear as craftable derived outcomes in `GetCraftingRecipesQuery`.

The old model was:

```text
unlock recipe variant -> variant appears in recipe list
```

The new model should become:

```text
unlock blueprint family -> compatible forms expose blueprint-derived craft options
```

That query/runtime projection is not implemented yet.

### Tempering Content

Status: Partial.

`tempering-recipes.json` still contains a starter set:

- weapon sharpening
- armor fortification
- jewelry polishing
- venom tempering

Needed for richer Region One identity:

- shield/offhand reinforcement
- caster focusing
- precision/ranged tuning
- heavy/medium/light/cloth armor directions
- Goblin/Mine/Powder directions
- Wraith/Grave/Catacomb directions
- Hive/Chitin/Resin directions
- Blood/Fire/Frost/Shadow directions
- Crystal/Slime/Geode directions
- Nature/Moss/Treant directions
- Undead/Bone/Ghoul directions
- Worm/Leech/Burrow directions

### Dungeon and Region One Acquisition

Status: Partial.

The material catalog now has enough standard families for Region One tier 1-3 crafting, but acquisition is not fully wired.

Still needed:

- blueprint drops or rewards
- special resource drops
- gathering nodes for every material family
- Region One source mapping for each blueprint family
- first-clear or completion reward decisions
- validation that equipment is not dropped from combat or dungeon rewards

### Automated Tests

Status: Not done for Crafting V2-specific behavior.

Existing tests still pass, but no dedicated tests were added for:

- broad recipe + form crafting
- missing/invalid form ID behavior
- blueprint compatibility unlocks
- blueprint-derived craft options
- material family+tier validation
- legacy alias preservation
- neutral accessory/relic base outputs
- `iron_ore` reference regression

## Not Done

### Blueprint Runtime Materialization

Not implemented:

- crafting with `blueprintId`
- generated blueprint item names
- generated blueprint output IDs
- applying blueprint tags to crafted equipment
- consuming blueprint-specific special resources
- showing blueprint choices in UI
- grouping available blueprint outcomes under base recipe + form

### Separate Affix/Special Modifier Definitions

Not implemented:

- `crafting/affixes.json`
- `crafting/special-modifiers.json`
- `crafting/tier-budgets.json`

Affix and special modifier IDs remain embedded in tempering recipe data. Tier/stat budget behavior remains service code.

### Legacy Recipe Retirement or Migration

Not implemented.

`Data/recipes.json` still contains 113 legacy recipes. Crafting V2 does not load that file. A future pass should either:

- migrate useful themes into V2 blueprint/form data, or
- clearly retire the legacy equipment recipe system.

## Acceptance Criteria Status

### Original Crafting V2 Criteria

| Acceptance criterion | Status |
| --- | --- |
| Base recipes are available without blueprint unlocks | Implemented |
| Player can craft base recipe at valid tiers | Implemented |
| Crafting request does not include manual material selections | Implemented |
| Crafting consumes resolved tiered materials automatically | Implemented |
| Crafting always creates Common equipment | Implemented |
| Crafting rolls Quality | Implemented |
| Crafting creates Potential based on Quality and Recipe Mastery | Implemented |
| Crafting increases Recipe Mastery | Implemented |
| Tools cannot be crafted | Implemented |
| Variant recipes are unavailable until blueprint unlock | Replaced by new blueprint-family model; runtime projection is partial |
| Player can craft unlocked variant at valid tiers | Partial |
| Crafting consumes special resources only if selected variant requires them | Partial |

### Blueprint Criteria

| Acceptance criterion | Status |
| --- | --- |
| Blueprint item can be learned | Implemented |
| Learning a blueprint creates a persistent unlock | Implemented |
| Learning duplicate blueprint does not create duplicate unlocks | Implemented |
| Blueprint has compatibility data for recipes/forms | Implemented in JSON |
| Unlocked blueprint-derived outcomes appear in recipe list | Not done |
| Blueprint-derived outcome can be crafted | Not done |

### Tempering Criteria

| Acceptance criterion | Status |
| --- | --- |
| Tempering consumes Potential only | Implemented |
| Tempering does not consume any materials | Implemented |
| Tempering cannot be performed on tools | Implemented |
| Tempering uses selected Tempering Recipe to determine direction | Implemented |
| Tempering can improve item stats | Implemented |
| Tempering can increase rarity progress | Implemented |
| Rarity upgrade adds affix from selected Tempering Recipe pool | Implemented |
| Special modifiers only appear at configured rarities | Implemented |
| Item cannot be tempered when Potential is insufficient | Implemented |

### Material and Data Cleanliness Criteria

| Acceptance criterion | Status |
| --- | --- |
| One standard material per family per tier | Implemented |
| No multiple equivalent Tier 2 metals | Implemented in V2 materials |
| No multiple equivalent Tier 3 woods | Implemented in V2 materials |
| Special resources are separate non-tiered items | Implemented |
| Obsolete named accessory/relic item IDs are removed from `items.json` | Implemented |
| Old exact item names are no longer base recipes | Implemented |
| Broad base recipe count is 9 | Implemented |
| Forms determine physical output within base recipes | Implemented |
| Blueprints define theme/variant identity | Implemented as data |

## Files Changed By This Work

### JSON Content

- `LL/src/API/API.LL/Data/crafting/base-recipes.json`
- `LL/src/API/API.LL/Data/crafting/blueprints.json`
- `LL/src/API/API.LL/Data/crafting/materials.json`
- `LL/src/API/API.LL/Data/crafting/recipe-variants.json`
- `LL/src/API/API.LL/Data/crafting/tempering-recipes.json`
- `LL/src/API/API.LL/Data/items.json`
- `LL/src/API/API.LL/Data/dungeons.json`

### Backend Schema and Runtime

- `LL/src/Core/Domain/Models/Professions/Crafting/V2/CraftingRecipeDefinition.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/V2/CraftingRecipeFormDefinition.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/V2/BlueprintDefinition.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/V2/BlueprintOutputNameDefinition.cs`
- `LL/src/Core/Application/UseCases/Crafting/Dtos/CraftingRecipeDto.cs`
- `LL/src/Core/Application/UseCases/Crafting/Dtos/CraftingRecipeFormDto.cs`
- `LL/src/Core/Application/UseCases/Crafting/Dtos/CraftItemsRequestDto.cs`
- `LL/src/Core/Application/UseCases/Crafting/Commands/CraftItems/CraftItemsCommand.cs`
- `LL/src/Core/Application/UseCases/Crafting/Commands/LearnBlueprint/LearnBlueprintCommand.cs`
- `LL/src/Core/Application/UseCases/Crafting/Queries/GetCraftingRecipes/GetCraftingRecipesQuery.cs`
- `LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/JsonCraftingDefinitionProvider.cs`
- `LL/src/API/API.LL/Controllers/V1/CraftingController.cs`

### Frontend

- `LL/src/Presentation/ll/src/app/shared/models/crafting-v2.ts`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/regular-crafting/regular-crafting.component.ts`
- `LL/src/Presentation/ll/src/app/features/game/professions/crafting/regular-crafting/regular-crafting.component.html`

## Compatibility Aliases

Preserved as form `legacyItemIds` and/or blueprint `legacyItemIds` where the corresponding item records still exist:

- armor forms such as `heavy_helm`, `medium_mail`, `cloth_robe`
- weapon forms such as `shortsword`, `dagger`, `hatchet`, `staff`
- offhand forms such as `towershield`, `Spiritward`, `grimoire`
- old blueprint items `blueprint_venom_touched_sword` and `blueprint_hivefang_dagger`

Removed from `items.json` and from active V2 `legacyItemIds`:

- `band_of_fury`
- `band_of_arcane`
- `band_of_execution`
- `amulet_of_aegis`
- `charm_of_the_warden`
- `talisman_of_endurance`
- `phoenix_vial`
- `mana_heart`
- `primal_totem`

Remaining legacy caveat: `LL/src/API/API.LL/Data/recipes.json` and `LL/src/Presentation/ll/src/app/data/recipes-content.ts` still mention these removed IDs. Those files are legacy/static content and were not cleaned up in this `items.json` removal pass.

## Assumptions

- Neutral accessory/relic base items use empty modifiers for now, because the request said not to invent detailed balance values.
- Form identity is represented through tags and `statProfileId` references instead of full balance tables.
- Blueprint-derived names are represented in JSON but not yet applied to actual crafted instances.
- Compatibility blueprint unlocks currently persist by blueprint ID, while exact recipe unlocks can still persist by recipe ID.
- `recipe-variants.json` is empty by design after this refactor; exact variants should come from blueprint data.

## Verification Performed

Changed JSON validation:

- all changed JSON files parse successfully
- material `itemId` references resolve to `items.json`
- recipe form `outputItemId` references resolve to `items.json`
- non-empty form `legacyItemIds` resolve to `items.json`
- blueprint `itemId` references resolve to `items.json`
- non-empty blueprint `legacyItemIds` resolve to `items.json`
- removed accessory/relic IDs no longer appear in active V2 `legacyItemIds` or `specialOutputNames[].legacyItemId`
- blueprint `allowedBaseRecipeIds` resolve to V2 recipes
- no remaining `iron_ore` references were found

Commands passed:

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

Angular builds passed:

```powershell
cd LL/src/Presentation/ll
.\node_modules\.bin\ng.cmd build --configuration development

cd LL/src/Presentation/dashboard
.\node_modules\.bin\ng.cmd build --configuration development
```

No verification commands were skipped.

## Migration and Deployment Notes

No new EF migration was generated for the recipe/form/blueprint JSON refactor.

Existing Crafting V2 migrations from the earlier implementation still need to be applied before using Crafting V2 in an environment:

- `20260624143520_AddEquipmentItemQuality`
- `20260624145236_AddCraftingV2Progression`

Deployment needs the updated content files:

- `Data/crafting/*.json`
- `Data/items.json`
- `Data/dungeons.json`

The next implementation pass should prioritize blueprint runtime materialization, because the data model now expects the player-facing flow to be:

```text
Choose broad recipe.
Choose form.
Optionally choose learned compatible blueprint.
Craft the resulting named output.
```
