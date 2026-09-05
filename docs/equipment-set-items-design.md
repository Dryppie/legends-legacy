# Equipment Set Items: Investigation and Technical Design

> Historical implementation design, superseded 5 September 2026. Its crafting paths and reusable Blueprint mechanics no longer describe the game. Current set definitions live in `Data/equipment/equipment-sets.v1.json`; equipment drops and consumable Blueprint Variants assign set identity. Use the [current equipment contract](design/equipment-specification.md) and [Blueprint Variant implementation](design/equipment-blueprints-implementation.md).

Status: Membership foundation implemented on 2026-08-24. Set benefits,
threshold evaluation, aggregate equipped-set state, and combat integration are
deferred to a later implementation phase.

### Implemented foundation

- Static set metadata is loaded from `Data/crafting/equipment-sets.json`.
- `BlueprintDefinition.EquipmentSetId` optionally associates every compatible
  craft from that Blueprint with one set.
- The crafting service stamps the association onto
  `EquipmentInstance.EquipmentSetId`.
- Membership is persisted on live equipment and combat equipment snapshots by
  migration `AddEquipmentSetMembership`.
- Equipment, Blueprint-item, and crafting-preview DTOs expose resolved set
  metadata, and the Angular shared equipment and crafting views display it.
- Startup validation rejects duplicate set IDs, nameless sets, blank Blueprint
  associations, and references to missing sets.

The initial `equipment-sets.json` catalog is intentionally empty because no
production Blueprint-to-set assignments were specified. Adding content later
requires a set entry and an `equipmentSetId` on each participating Blueprint.
No benefit definitions or activation behavior exist yet.

## 1. Executive summary

Blueprints should declare Set Item membership, while a first-class set catalog
owns the shared set metadata and bonus rules. In this repository, a Blueprint
is a reusable crafting overlay that may apply to many recipes and equipment
types. Under the intended product rule, that reuse is desirable: every item
crafted with a set-associated Blueprint belongs to the same set, regardless of
which compatible recipe or equipment type was selected.

The recommended design is a hybrid:

- Add a first-class, static `EquipmentSetDefinition` catalog alongside the
  existing crafting and combat JSON content.
- Add an optional `EquipmentSetId` to `BlueprintDefinition`.
- Stamp that `EquipmentSetId` onto the resulting `EquipmentInstance` when the
  item is crafted.
- Derive active bonuses from authoritative equipped slots whenever stats or
  combat state are built.
- Count distinct equipped item instances per set. Separate copies created from
  the same Blueprint each count; duplicate slot rows for one instance do not.
- Represent bonuses through typed stat modifiers plus references to the existing
  typed combat ability system for conditional, passive, and triggered behavior.
- Do not persist ordinary active-bonus state. Persist it only inside deliberate
  combat snapshots where frozen behavior is required.

This avoids unnecessary piece identity, handles two-handed weapons correctly,
keeps quality and upgrades within the same set, and gives complex bonuses access
to the existing combat trigger/effect engine.

## 2. Current architecture and lifecycle

### Blueprint definition and acquisition

Blueprints are static JSON definitions loaded from
`LL/src/API/API.LL/Data/crafting/blueprints.json`. They contain compatibility
rules, stat profiles, behavior overlays, materials, acquisition sources, and
tags. The main model is
`LL/src/Core/Domain/Models/Professions/Crafting/V2/BlueprintDefinition.cs:5`.

`JsonCraftingDefinitionProvider` loads:

- `materials.json`
- `base-recipes.json`
- `blueprints.json`
- Equipment bases from `items.json`

It validates unique IDs, recipe outputs, compatible Blueprint/recipe
combinations, stat profiles, and material references at startup. See
`LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/JsonCraftingDefinitionProvider.cs:40`.

Blueprint ownership is persisted as `CharacterRecipeUnlock`, unique by
`(CharacterId, RecipeId, BlueprintId)`. A Blueprint is therefore learned per
compatible recipe, with legacy global-unlock support covered by
`LL/tests/EssenceSystem.Tests/CraftingRepositoryBlueprintUnlockTests.cs:10`.

Learning validates that:

1. The physical Blueprint item is in the character's inventory.
2. It maps to an enabled Blueprint definition.
3. The selected recipe exists and is compatible.
4. The unlock does not already exist.
5. The Blueprint item can be consumed.

The consumption, unlock, and outbox event run in the command transaction. See
`LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/CraftingService.cs:316`.

### Crafting and item creation

Crafting revalidates the recipe, Blueprint, compatibility, unlock, tier,
profession level, output item type, and material costs server-side. See
`CraftingService.cs:374`.

`EquipmentCraftingDesignComposer` combines recipe and Blueprint data into a
transient design: name, behavior, base profile, Blueprint bonus profile,
tempering profile, materials, and tags. See
`LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentCraftingDesign.cs:47`.

Each crafted item snapshots:

- `BaseRecipeId`
- `BlueprintId`
- `CraftedName`
- Tier and stat model version
- Quality, rarity, and potential
- Rolled instance modifiers
- Composed affinity tags

Creation occurs at `CraftingService.cs:451`, after which the item is added to
inventory and an `equipment.crafted` outbox event is queued.

### Inventory ownership and persistence

An `ItemInstance` has no direct owner column. Ownership is represented by its
current container:

- Unequipped: an `InventoryItem` row keyed by
  `(InventoryId, ItemInstanceId)`.
- Equipped: one or more `EquipmentSlot` rows belonging to the entity.
- Other systems may hold it through marketplace or guild-vault relationships.

See `LL/src/Core/Domain/Models/Inventories/InventoryItem.cs:5` and
`LL/src/Core/Domain/Models/Items/Equipments/Slots/EquipmentSlot.cs:4`.

Inventory quantities must be positive, but there is no database constraint
spanning inventory and equipment tables that proves an item cannot be in both.
The application transaction and character lock enforce that normal workflow.

### Equip and unequip

The equipment endpoint always uses the authenticated character ID; the client
supplies only the item ID and optional target slot. See
`LL/src/API/API.LL/Controllers/V1/EquipmentController.cs:27`.

The repository validates inventory ownership, item type, level, tool-slot
restrictions, and hand rules. Equipping removes the inventory row and assigns
the item to slots. Unequipping clears the slot and recreates an inventory row.
See
`LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentSlotRepository.cs:103`.

A two-handed item occupies both hand rows using the same
`EquipmentInstanceId`; cleanup explicitly deduplicates it. See
`EquipmentSlotRepository.cs:172` and
`LL/tests/EssenceSystem.Tests/EquipmentHandRuleTests.cs:17`.

Commands run inside a transaction, acquire a per-character database lock, save
once, and commit before realtime delivery. See
`LL/src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs:107`.

### Stat calculation

`EquipmentInstance.AttributeModifiers` combines persisted instance rolls with
base-item modifiers for legacy or directly granted equipment. Crafted recipe
equipment deliberately excludes authored base modifiers to avoid
double-budgeting. See
`LL/src/Core/Domain/Models/Items/Equipments/EquipmentInstance.cs:50`.

`AttributeCalculator`:

1. Deduplicates equipment by instance ID.
2. Collects direct modifiers.
3. Aggregates progression-normalized ratings.
4. Converts ratings based on character level and tier.
5. Applies flat, additive, then multiplicative modifiers.
6. Clamps attributes.

See `LL/src/Core/Domain/Components/Attributes/AttributeCalculator.cs:31` and
`:126`.

Character overview calculation recomputes equipment plus Essence-derived
modifiers rather than storing derived values. See
`LL/src/Infrastructure/Service/Services.LL/Entities/Characters/CharacterService.cs:93`.

### Combat initialization and snapshots

Live combat entity loading uses the `CombatReady` query profile, which loads
slots, item bases, instance modifiers, and tool affixes. See
`LL/src/Infrastructure/Persistence/Persistence.LL/QueryProfiles/EntityQueryProfiles.cs:10`.

`CombatEntity` deduplicates equipped items by instance ID and separately records
main-hand and off-hand equipment. See
`LL/src/Core/Domain/Models/Combat/CombatEntity.cs:47`.

`CombatSetupService` resolves the Essence loadout, applies its tags and
modifiers, and calculates starting combat attributes. See
`LL/src/Infrastructure/Service/Services.LL/Combat/CombatSetupService.cs:112`.

Dungeon and several competitive modes create a `CharacterSnapshot`. Equipment
snapshots preserve slot, item identity, recipe/Blueprint identity, tier,
quality, rarity, and instance modifiers. See
`LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Snapshots/CharacterSnapshotRepository.cs:20`
and `LL/src/Core/Domain/Models/Snapshots/EquipmentSnapshot.cs:9`.

### Passive and triggered effects

The combat ability catalog is already strongly typed:

- `AbilitySpec`
- `AbilityTriggerSpec`
- `AbilityEffectSpec`
- `AbilityConditionSpec`
- `StatusSpec`

It supports passive abilities, conditional maintained effects, triggers,
status effects, attribute modification, summons, healing, barriers, and other
specialized operations. See
`LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs:204`.

The JSON catalog is validated and compiled at startup by
`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/JsonAbilityCatalogProvider.cs:12`.

The engine currently grants abilities from:

- Equipped Essences
- `CombatEntity.NativeAbilityIds`
- Essence tags

See
`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs:453`.

### Frontend and synchronization

Crafting APIs expose Blueprint details and item previews. Equipment DTOs expose
recipe/Blueprint provenance and current crafting metadata. See
`LL/src/Core/Application/UseCases/Crafting/Dtos/CraftingBlueprintDto.cs:6` and
`LL/src/Core/Application/UseCases/Equipments/Dtos/EquipmentInstanceDto.cs:14`.

Angular's shared equipment display already handles crafted design metadata,
attributes, roll ranges, potential, gear value, and equipped comparisons. See
`LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display/equipment-display.component.html:328`.

Equip/unequip responses carry authoritative equipment and inventory snapshots.
`EquipmentStateService` applies them and marks character overview dirty. It also
refreshes when the `equipment` synchronization scope advances. See
`LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-state.service.ts:39`
and `:153`.

Realtime messages therefore remain invalidations and version notifications,
not a competing source of item or bonus truth.

## 3. Relevant files and ownership boundaries

| Boundary | Important files |
| --- | --- |
| Static crafting content | `Data/crafting/blueprints.json`, `Data/crafting/base-recipes.json`, `JsonCraftingDefinitionProvider.cs` |
| Crafting domain/application | `BlueprintDefinition.cs`, `CraftingRecipeDefinition.cs`, `CraftingService.cs` |
| Player-owned item state | `EquipmentInstance.cs`, `InventoryItem.cs`, `EquipmentSlot.cs` |
| Equipment mutation | `EquipmentSlotRepository.cs`, `EquipEquipmentCommand.cs`, `UnequipEquipmentCommand.cs` |
| Stats and comparison | `AttributeCalculator.cs`, `CompareEquipmentQuery.cs` |
| Combat effects | `AbilitySpec.cs`, `AbilityCatalog.cs`, `CombatEngineExecutor.cs` |
| Snapshots | `CharacterSnapshotRepository.cs`, `SnapshotCombatantBuilder.cs` |
| API/frontend | `EquipmentController.cs`, `shared/models/item.ts`, `shared/models/crafting-v2.ts` |
| Synchronization | `StateSyncCommandScopeCatalog.cs`, `state-sync-coordinator.service.ts` |

## 4. Gaps or constraints in the current implementation

- There is no set identity, threshold, or active-set projection.
- A Blueprint can apply to many recipes. That is compatible with the intended
  rule because every resulting equipment instance should inherit the
  Blueprint's set association; recipe-specific piece identity is not needed.
- Recipe and Blueprint metadata are only partially snapshotted. Rolled stats and
  crafted name are persisted, but tooltip metadata and basic-attack behavior are
  recomposed from current JSON. See
  `EquipmentCraftingDesignMetadataDto.cs:44` and
  `CombatEngineExecutor.cs:431`. Deleting a referenced definition can silently
  remove metadata or revert behavior.
- Stat calculation is item-centric. Adding a shared threshold bonus directly to
  each item would apply it once per equipped item instead of once per active
  set threshold.
- The ability engine is generic, but ability acquisition is Essence/native-ID
  centric. Set abilities need a provenance-aware grant channel.
- `EquipmentSlot` has only one Ring slot today. Repeated-slot rules are not
  already solved by the domain.
- Two-handed items deliberately appear in two slot rows. Slot counting would be
  incorrect.
- Equipment changes are not explicitly blocked during a combat action. Some
  modes use live loading; dungeon and competitive modes use snapshots.
- Static JSON definitions cannot have database foreign keys. Referential
  integrity must be enforced by startup validation and stable-ID lifecycle
  rules.
- A character lock serializes normal commands, but ownership is represented
  across separate inventory/equipment tables without a cross-table database
  constraint.

## 5. Design options with concrete trade-offs

| Option | Evaluation |
| --- | --- |
| Set membership and all bonuses directly on Blueprints | Membership fits the crafting workflow, but shared metadata and thresholds would be duplicated across every Blueprint belonging to the same set. Keeping those copies synchronized would be error-prone. |
| First-class set definition referenced by Blueprints | **Recommended.** Each Blueprint declares one optional set ID; the set catalog centrally owns name, thresholds, and effects. This directly expresses the product rule without introducing piece definitions. |
| Membership on `EquipmentBase` | Incorrect for current crafting. Many Blueprint designs share the same recipe output item base, so non-set variants would inherit membership. |
| Derive membership from `EquipmentInstance.BlueprintId` at runtime | Avoids a new instance column, but changing or removing a Blueprint association would retroactively transform existing items. It also couples every equipment read to current crafting content. |
| First-class static set catalog plus stamped instance membership | Completes the recommended design: the Blueprint is the creation-time source, the instance retains stable membership, and active bonuses remain derived rather than persisted. |

The Blueprint owns the creation-time association, but the set definition owns
the shared rules. The relationship is:

> Blueprint declares a set ID; every equipment instance crafted with that
> Blueprint snapshots the same set ID.

## 6. Recommended domain model

Conceptual types:

```csharp
EquipmentSetDefinition
{
    string Id;
    string Name;
    string Description;
    int Version;
    bool CraftingEnabled;
    bool BonusesEnabled;
    IReadOnlyList<EquipmentSetBonusDefinition> Bonuses;
}

BlueprintDefinition
{
    // Existing fields...
    string? EquipmentSetId;
}

EquipmentSetBonusDefinition
{
    string Id;
    int RequiredEquippedItems;
    string Description;
    IReadOnlyList<SetBonusEffectDefinition> Effects;
}
```

Initially support these typed effect definitions:

- `SetAttributeModifierDefinition`
- `SetAbilityGrantDefinition`

Later add:

- `SetAbilityModifierDefinition`
- Other explicitly typed, validated bonus kinds

Do not use arbitrary effect names with free-form JSON.

The preferred content file is `Data/items/equipment-sets.json` or
`Data/crafting/equipment-sets.json`, loaded through a dedicated provider or an
expanded crafting-content provider. Static content matches the repository's
current recipes, Blueprints, abilities, dungeons, raids, and rewards
conventions.

Persist on `EquipmentInstance`:

```text
EquipmentSetId              nullable stable string
```

After validating the selected Blueprint, the crafting service copies
`blueprint.EquipmentSetId` onto the new instance. Runtime activation uses the
stamped value and never re-derives membership from the current Blueprint
definition. Consequently, changing a Blueprint association affects future
crafts only; it does not silently transform existing items.

Required invariants:

- Set IDs are unique case-insensitively and never reused.
- Bonus IDs and thresholds are unique within a set.
- Thresholds are positive and reachable with the equipment slots and recipes
  supported by the set's associated Blueprints.
- A Blueprint references at most one set.
- Every non-null Blueprint set ID references an existing set definition.
- Referenced abilities and statuses exist.
- A V1 equipment instance belongs to zero or one set.
- Membership does not change during tempering, quality changes, rarity upgrades,
  transfer, or renaming.
- Every compatible recipe crafted with a set-associated Blueprint inherits the
  same set ID. If recipe-specific membership is ever required, it should use a
  different Blueprint rather than reintroducing implicit tuple mappings.

Lifecycle policy:

- Renaming a set changes display metadata but not identity.
- Disabling crafting prevents new creation but does not deactivate existing
  items.
- A separate `BonusesEnabled` emergency switch may disable gameplay effects.
- Referenced sets must be tombstoned, not hard-deleted.
- Missing definitions fail closed: preserve persisted identity, apply no
  bonuses, log diagnostics, and show a retired or unknown-set fallback.
- Live characters receive current balance changes to set bonuses.
- Frozen combat snapshots retain the set behavior resolved when the snapshot was
  created.

## 7. Recommended set-counting rules

Thresholds count equipped equipment instances carrying the set ID.

```text
equippedInstances =
    slot rows
    -> non-null equipment
    -> distinct by EquipmentInstanceId

setProgress =
    equippedInstances
    -> items with a valid SetId
    -> group by SetId
    -> count
```

Consequences:

- Separate copies crafted from the same Blueprint count separately.
- A two-handed item occupying both hands counts once.
- Two one-handed instances from the same set count as two, even if both came
  from the same Blueprint and recipe.
- If repeated ring slots are added later, each separately equipped ring instance
  counts.
- Quality, rarity, tier, potential, and upgrades do not affect identity.
- A broken or disabled equipment mechanic should expose an explicit
  `ContributesEquipmentEffects` state. Until then, every equipped item
  contributes.
- V1 should reject items belonging to multiple sets.
- "Full set" means the highest authored threshold; there is no separate list of
  required item identities.
- If equipment loadouts are introduced, only the selected authoritative loadout
  counts.

The resolver must accept the complete equipped collection. It must not be
called per item.

## 8. Bonus evaluation and combat integration

### Simple stat bonuses

Add an `IEquipmentSetBonusResolver` returning an immutable projection such as:

```text
SetState[]
  SetId
  EquippedItemInstanceIds
  EquippedCount
  ActiveThresholds
  AttributeModifiers
  GrantedAbilityIds
  AbilityModifiers
```

The resolver must be pure and idempotent. Calling it repeatedly over the same
equipment returns the same projection and does not mutate equipment, slots, or
modifier tables.

For a two-item `+20 Power` bonus:

1. The resolver finds two distinct equipped instance IDs carrying the set ID.
2. The threshold becomes active.
3. It emits one modifier with a deterministic source key such as
   `set.stormguard/2/power`.
4. Character overview combines it with equipment and other static loadout
   modifiers.
5. Combat initialization includes it once in the starting projection.
6. Unequipping either qualifying item causes the next resolution to omit it;
   nothing is deleted.

The definition must explicitly identify whether an amount is a direct canonical
attribute value or an equipment-style rating requiring level normalization.

### Conditional and triggered bonuses

A four-item triggered bonus should reference a normal passive ability:

```text
SetAbilityGrantDefinition
  AbilityId = "ability.set.stormguard.four_item"
```

That ability lives in the existing validated ability catalog and can use
`OnCombatStart`, `OnDamaged`, `OnHit`, statuses, conditions, internal cooldowns,
or other supported operations.

Combat integration should add a provenance-aware collection such as
`CombatEntity.GrantedAbilityIds`, rather than treating set abilities as native
creature abilities. `CombatEngineExecutor` can merge Essence, native, set, and
future source-specific grants using its existing ID deduplication.

Ability alteration should eventually reuse the typed modifier pipeline currently
represented by `EssenceAbilityModifierDefinition`, but that type should be
generalized before Set Items depend on it.

Recommended projection order:

1. Raw character base attributes
2. Equipment instance and base modifiers
3. Set static modifiers
4. Other static loadout modifiers
5. Encounter or run modifiers
6. Runtime buffs, statuses, and triggered temporary modifiers
7. Attribute caps

Set bonuses must not be stored in `EquipmentInstance.InstanceModifiers`; doing
so would duplicate a shared threshold bonus across items and leave stale data
after unequip.

## 9. Persistence and migration impact

### Static definitions

In the recommended design, set definitions, thresholds, and effects are static
game content, not EF entities. Blueprint definitions carry the optional set
reference. This matches the current crafting and ability architecture.

If live admin editing becomes a requirement, the equivalent relational model
would be:

```text
EquipmentSets
BlueprintDefinitions         optional FK -> EquipmentSets
EquipmentSetBonuses          FK -> EquipmentSets; SetId + Threshold unique
EquipmentSetBonusEffects     FK -> Bonuses; typed discriminator and constrained columns
```

That is not recommended initially because it would create a second content-
authoring model beside the existing JSON catalogs.

### Player state

Add nullable membership columns to `EquipmentInstances`:

- `EquipmentSetId varchar(...)`

Add:

- An index on `EquipmentSetId` for audits and backfills.
- Maximum lengths consistent with recipe and Blueprint IDs.

There should be no foreign key to static JSON content.

### Existing data migration

Backfill existing crafted equipment by looking up its persisted `BlueprintId`.

- Blueprint references a valid set: stamp that set ID.
- Blueprint has no association: remain a non-set item.
- Blueprint or set definition is missing: report the item and leave it unchanged
  rather than choosing arbitrarily.

This makes existing crafted items eligible without rerolling stats.

### Combat snapshots

Add membership identity to `EquipmentSnapshot`. For persistent modes, also
snapshot resolved active set effects or reference an immutable set-definition
version. Otherwise a balance deploy would change an existing dungeon or
defensive snapshot.

This exposes a broader existing weakness: Blueprint behavior and ability
definitions are not fully version-frozen today. Set Items should not deepen that
inconsistency.

Ordinary active set state remains derived and must not be stored in player
tables.

## 10. Backend and API impact

Likely backend changes:

- New set definition types and provider interface under Core and Application.
- JSON loader and cross-catalog validator in `Services.LL`.
- `EquipmentInstance` and EF configuration.
- Crafting service membership stamping.
- Equipment snapshot persistence and rehydration.
- A pure set-state and bonus resolver.
- Character overview stat projection.
- Combat setup and combat entity ability grants.
- Equipment comparison projection.
- Equipment, inventory, crafting, and item DTO mapping.
- Content validation tests and migration coverage.

Backend response models should expose:

```text
EquipmentSetItemMetadataDto
  SetId, SetName
  AllBonuses

EquipmentSetStateDto
  SetId
  EquippedItemInstanceIds
  EquippedCount
  ActiveThresholds
  NextThreshold
  ActiveEffects
```

Recommended API behavior:

- Inventory and equipment item DTOs expose stable set metadata.
- The equipment state endpoint exposes aggregate progress and active thresholds.
- Equip and unequip responses return updated slots, inventory, and set state.
- Crafting Blueprint and preview DTOs show the set associated with the selected
  Blueprint and the set's possible bonuses.
- The comparison endpoint returns before and after set states plus attribute
  differences including gained or lost thresholds.
- Tooltip DTOs return formatted descriptions and structured stat modifiers where
  useful.

The current comparison endpoint correctly models the whole hypothetical loadout
on the backend. Extend that projector rather than recalculating thresholds in
Angular.

Because `GET /equipment` returns a bare slot list while mutations return an
object, migrate toward one `EquipmentStateDto` containing slots and set states.

## 11. Frontend and synchronization impact

### Crafting

The Blueprint and preview panes should show:

- Set name
- Bonus thresholds
- Current equipped-item count
- Active and inactive styling where equipment state is available

The Blueprint selection and crafting integration proposed here was removed. Future set-item
acquisition must use the current authored-drop model or a separately approved design.

### Inventory and equipment tooltips

Extend the shared equipment display with:

- Set name
- Current equipped-item count
- Threshold descriptions
- Active threshold emphasis
- Progress toward the next threshold

Because this component is shared by inventory, equipped slots, modals,
marketplace, and comparisons, using it avoids divergent tooltip behavior.

Marketplace viewers should see set membership and possible bonuses, but not
owner-specific equipped progress unless explicitly supplied by the API.

### Character equipment and stats

Add an active-set summary beneath equipment overview. Character-stat details
should identify set-derived changes separately from raw equipment rolls where
possible.

Comparison tooltips should show transitions such as:

- "Activates 3-item bonus"
- "Loses 2-item bonus"
- "Replaces one Stormguard item with another; count unchanged"

These transitions must come from the comparison API.

### Synchronization

Set state belongs to the existing `equipment` synchronization scope.

Equip and unequip already advance response-handled equipment and inventory
revisions, return authoritative state, mark character overview dirty, and
publish invalidations for other interested views. No new realtime event stream
is required.

Crafting a set item changes inventory only and cannot activate a bonus. If
equipped-item upgrading is added later, it must invalidate equipment and
character overview.

## 12. Correctness and exploit risks

Rules that must remain server-authoritative:

- Set ID is never accepted from the client.
- Membership is copied from the server-loaded, validated Blueprint definition
  during creation.
- The character must own the Blueprint unlock for the selected recipe.
- Only equipped slot rows for the authenticated character participate.
- Only distinct equipment instance IDs are counted.
- Incompatible slot choices are rejected by the backend.
- Thresholds and effects come only from the server catalog.
- Comparison results are calculated server-side.
- Disabled or missing definitions fail closed.

Transactional requirements:

- Material removal, item creation, membership stamping, inventory insertion,
  mastery changes, and outbox enqueue commit together.
- Equip replacement, displaced-item inventory insertion, slot updates, set-state
  response, outbox enqueue, and synchronization revisions share the same
  character transaction.
- Per-character locking remains in place for equip, unequip, transfer, scrap,
  marketplace, guild-vault return, and future loadout changes.

Specific exploit cases:

- Duplicate copies: each separately owned and equipped instance intentionally
  counts toward the threshold.
- Two-handed double count: prevented by distinct `EquipmentInstanceId`.
- Replay equip: the second request fails because the item is no longer in
  inventory.
- Client-supplied set IDs: no mutation request should contain these fields.
- Bonus before craft commit: impossible when bonuses derive from equipped,
  committed state.
- Stale bonus after sell or scrap: derived state disappears when the slot
  changes.
- Outbox ordering: outbox consumers never apply bonuses.
- Missing definitions: no effect application, with diagnostics.
- Conflicting loadout or equip requests: serialize under the character lock.

One structural risk remains: the database does not prove that an item cannot
exist simultaneously in inventory and equipment, and a simple unique equipment
index cannot be added because two-handed items legitimately occupy two rows.
The command lock and invariant tests remain important until equipment occupancy
is normalized.

## 13. Testing strategy

### Unit tests

- Accept valid set definitions and Blueprint set references.
- Reject duplicate set IDs, thresholds, and effect IDs.
- Reject missing set, ability, or status references.
- Reject thresholds that cannot be reached by compatible equipment slots.
- Count distinct equipped instances, including same-Blueprint copies, while
  deduplicating two-handed slot rows.
- Resolve multiple simultaneous sets independently.
- Activate several crossed thresholds exactly once.
- Leave progress unchanged when replacing one item with another item from the
  same set.
- Validate modifier units and ordering.
- Deduplicate ability grants by ID.

### Crafting and application tests

- Non-set combinations produce null membership.
- Set-associated Blueprints stamp the correct set ID.
- Locked Blueprints remain rejected.
- Multiple Blueprints can stamp the same set ID.
- Every compatible recipe used with one set-associated Blueprint stamps the
  same set ID.
- Quality, tier, rarity, and tempering preserve membership.
- Failed material removal creates no item or membership.
- Rollback removes the item, mastery changes, and outbox event.

### Database and integration tests

- Migration backfills existing instances from their Blueprint association.
- Missing Blueprint or set definitions are reported and left unchanged.
- Transfer, marketplace, guild vault, scrap, and reload preserve membership.
- Snapshot persistence retains membership and frozen active effects.
- Existing non-set rows remain unaffected.

### Equipment and API tests

- Equip and unequip return updated authoritative set state.
- Existing one-handed and two-handed rules remain correct.
- Repeated requests do not duplicate progress.
- Comparison includes threshold gain and loss.
- DTOs serialize complete metadata.
- Unknown or tombstoned definitions serialize safely.

### Combat tests

- Simple set stats appear once in initial combat attributes.
- A passive set ability is compiled and registered.
- A trigger fires under the authored condition and respects cooldown.
- Unequipping before combat removes it.
- Mutation after snapshot creation does not alter frozen combat.
- Multiple active sets grant independent effects.
- Ability-ID collisions do not double-register passives.
- Reset and replay do not lose or duplicate static effects.

### Frontend tests

- Crafting preview renders identity and thresholds.
- Inventory and equipped tooltips display active and inactive bonuses.
- Separately equipped copies appear as separate progress.
- A two-handed instance occupying two slot rows appears only once.
- Comparison renders gained and lost thresholds.
- Equip response updates slots, inventory, set state, and character overview.
- Realtime invalidation refreshes authoritative state.
- Reconnect restores progress from HTTP state.

### Concurrency tests

- Simultaneous equip requests serialize.
- Equip versus transfer or scrap cannot count a moved item.
- Failed rollback leaves prior thresholds active.
- Out-of-order HTTP responses are rejected by domain versions.
- Realtime invalidations do not overwrite a newer authoritative response.

## 14. Incremental implementation plan

| Phase | Purpose and affected areas | Tests and risks | Independently shippable |
| --- | --- | --- | --- |
| 1. Definition foundation | Add set types, JSON catalog, optional Blueprint set ID, lookup, and startup validation. No gameplay use. | Catalog and reference validation. Main risk is unreachable thresholds or missing set references. | Yes |
| 2. Membership persistence | Add the instance/snapshot set ID, migration, craft stamping, Blueprint-based backfill, and DTO identity. | Crafting, migration, transfer, inventory, and snapshot tests. Main risk is incorrect legacy association. | Yes, with bonuses disabled |
| 3. Authoritative resolver | Add a pure distinct-instance resolver, equipment API state, and comparison projection. | Same-Blueprint copy, two-hand, multiple-set, and comparison tests. Main risk is missing a read path. | Yes, informational only |
| 4. Frontend presentation | Add models, crafting preview, shared tooltip section, equipment summary, and comparison transitions. | Component, state, and synchronization tests. Main risk is DTO migration across shared views. | Yes |
| 5. Simple stat bonuses | Feed typed modifiers into overview, comparison, power, and combat behind a switch. | Attribute ordering, threshold, overview, power, and combat tests. Main risk is calculation paths diverging. | Yes after projections agree |
| 6. Combat ability grants | Add provenance-aware granted ability IDs and reference existing passive/triggered definitions. | Compiler, trigger, deduplication, reset, and snapshot tests. Main risk is effect versioning and modifier order. | Yes per authored bonus |
| 7. Snapshot/version hardening | Freeze resolved effects or immutable versions and document mid-combat equipment policy. | Dungeon, arena, tournament, reconnect, and content-update tests. | Required before broad complex-bonus release |
| 8. Live-operations hardening | Add diagnostics, tombstones, content audits, balance reporting, and emergency disable. | Missing-definition and rollback tests. | Yes |

No phase should deploy services or apply EF migrations to shared databases during
implementation review.

## 15. Open product decisions requiring input

1. Which Blueprints belong to each initial set, and what thresholds should each
   set provide? Every compatible recipe crafted with one of those Blueprints
   will inherit its set ID.
2. Confirm that one item may belong to at most one set in V1.
3. Should live owned items always receive current set-bonus balance changes? The
   recommendation is yes, while persistent combat snapshots remain frozen.
4. What should happen when equipment changes during combat? Recommended: frozen
   modes remain frozen; live or idle changes apply from the next encounter; an
   already-running encounter is not mutated.
5. Are stat bonuses authored in direct combat units, level-normalized rating
   units, or both through an explicit type?
6. Should disabled sets retain bonuses for existing items? The recommendation is
   to separate `CraftingEnabled` from `BonusesEnabled`.
7. Which complex effect should be the first supported example: a granted passive,
   a triggered status, or alteration of an existing ability? A granted passive
   is the safest first vertical slice.

## Investigation verification

The original investigation used read-only inspection of source, content,
mappings, synchronization behavior, and existing tests. This document was then
updated to reflect the clarified instance-counting rule. The application test
suite was not run because this revision changes documentation only.
