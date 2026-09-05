# Equipment Recipes, Reusable Blueprints, and Directed Tempering

**Superseded 5 September 2026:** this document is historical. Player crafting, tempering, reusable Blueprint items, and the later equipment Forge have been removed. Current gear comes from authored grants and combat drops; Reinforcement advances rank and consumable Blueprint Variants replace compatible styles. See the [Forge removal](../../docs/design/equipment-forge-removal.md) and [current specification](../../docs/design/equipment-specification.md).

## Outcome

Equipment crafting uses three composable layers:

1. A concrete Recipe determines the physical item, slot, base behavior, base
   stat profile, materials, tier range, and mastery track.
2. An optional reusable Blueprint retains the Recipe's complete base-stat roll,
   adds a separate bonus-stat budget, and extends its name, tempering profile,
   tags, behavior modifiers, and material costs.
3. Directed Tempering develops the finished item's attributes within the
   composed profile.

There are no authored Recipe Variant combinations. A Blueprint is learned once
and can be applied to every compatible Recipe.

## Content Model

### Recipes

`Data/crafting/base-recipes.json` contains one definition for each craftable
equipment base. Every enabled Recipe owns:

- a stable `recipe.*` ID;
- one output item ID and equipment type;
- material requirements and tier range;
- behavior and initial stat profile;
- directed-tempering weights;
- compatibility tags.

The catalog currently contains 35 concrete Recipes.

### Blueprints

`Data/crafting/blueprints.json` contains reusable overlays. Each Blueprint owns:

- a stable Blueprint and physical item ID;
- a display-name format;
- required, alternative, excluded, or exact Recipe compatibility constraints;
- a bonus-stat profile, bonus-budget multiplier, and additive tempering profile;
- optional behavior and material modifiers;
- acquisition metadata.

Broad Blueprints use family tags so a small content catalog produces many
useful designs. Exact Blueprints can require multiple tags or explicit Recipe
IDs. For example, Venom applies to weapons while Hivefang requires a Dagger.

### Composition

`EquipmentCraftingDesignComposer` is the single composition boundary used by
crafting previews, stat rolls, combat behavior, tempering, item metadata, and
crafted names. A crafted item stores:

- `BaseRecipeId`;
- optional `BlueprintId`;
- the derived crafted name and rolled state.

The design is deterministic and is recomposed from those IDs whenever runtime
behavior or metadata is needed.

Stat composition is deliberately additive. The Recipe receives its full normal
allocation first. The Blueprint then receives a separate `20%` bonus budget,
allocated with the finished base roll supplied as the current value for item
and combat-cap enforcement. Blueprint selection therefore never reduces a
Recipe attribute.

## Player Flow

The crafting screen lists concrete equipment Recipes. Selecting one shows:

- the complete item preview and readable attribute ranges;
- quality chances and starting Potential;
- behavior and directed-tempering attributes;
- material costs and tier choices;
- a `No Blueprint` option;
- every compatible Blueprint, including locked acquisition hints.

Learning consumes one physical Blueprint item and creates one permanent
character-level Blueprint unlock. The same unlock immediately appears on every
compatible Recipe.

## Persistence and Migration

Migration `20260723161332_EquipmentRecipesReusableBlueprintsDirectedTempering`:

1. maps legacy crafted items to concrete Recipe IDs by `ItemBaseId`;
2. preserves existing `BlueprintId` provenance;
3. removes obsolete Recipe/form and special-modifier columns;
4. deduplicates unlocks by `(CharacterId, BlueprintId)`;
5. creates the corresponding unique Blueprint unlock index;
6. adds the equipment `xmin` concurrency token.

The migration is generated but must be applied by the normal deployment
process. It is not applied automatically by this implementation.

## Verification Checklist

- [x] 35 concrete Recipes load and validate.
- [x] 11 reusable Blueprints load and have compatible Recipes.
- [x] Venom composes with every weapon Recipe.
- [x] Hivefang composes only with Dagger.
- [x] Craft requests use Recipe plus optional Blueprint.
- [x] Crafted items persist Recipe plus optional Blueprint.
- [x] Combat and Tempering use the composed design.
- [x] Inventory and equipment metadata describe reusable Blueprints/designs.
- [x] Angular crafting UI previews the actual resulting item.
- [x] Legacy Recipe Variant content is no longer loaded.
- [x] Backend tests, EF model check, and Angular build pass.
