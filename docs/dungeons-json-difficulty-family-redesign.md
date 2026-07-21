# `dungeons.json` Difficulty-Family Redesign

## Purpose

`dungeons.json` previously stored each difficulty as a complete `DungeonDefinition`. Goblin Mines I, II, and III therefore repeated the same family identity, sigil, entry cost, encounter catalog, loot modifiers, and other configuration. Forgotten Catacombs had the same problem.

The catalog now authors one dungeon family with a small list of difficulty variants. Loading still produces the existing flattened `DungeonDefinition` objects, so gameplay services, API contracts, saved dungeon IDs, achievements, and persistence do not need to understand inheritance.

The intended outcome is:

- Shared content is authored once.
- Difficulty-specific balance remains easy to read in one place.
- Existing IDs such as `goblin_mines_ii` remain stable.
- Every difficulty in a family uses the same rooms and creature roster.
- Runtime code continues consuming ordinary, fully materialized `DungeonDefinition` objects.

## Legacy-schema findings

The three difficulties currently differ mainly in:

- `id`, displayed Roman numeral, `grade`, and `tier`.
- `recommendedCombatRating`.
- `minRooms` and `maxRooms` preview values.
- Previous-difficulty requirements.
- First-clear and completion rewards.
- Reward-table IDs.
- Difficulty-specific gathering nodes and their loot.

The following data is substantially duplicated within each family:

- Base dungeon name and region.
- Sigil and entry costs.
- Monster loot modifiers.
- Combat, Miniboss, and Boss room templates.
- Rest-site capability.
- General unlock metadata.

The retired Hives of the Abyss definitions were also present as three complete objects. They are now retained as one family with three compact difficulty entries. `JsonDungeonDefinitions` still filters their materialized definitions after validation.

### Room count does not currently control map length

`DungeonDefinition.MinRooms` and `MaxRooms` are currently used by dungeon preview DTOs and validation. `DungeonRunFactory` does not use them to create the map.

Actual node count, Depth count, Sections, and topology come from `dungeon-delves.json`. `JsonDungeonDelveDefinitionProvider` matches a base dungeon ID and its suffixed difficulties by prefix, so all difficulties in a family currently receive the same authored delve definition.

Consequently, changing only `minRooms` and `maxRooms` changes the advertised range but does not make a generated run longer. Difficulty-family cleanup should not accidentally preserve this ambiguity.

## Implemented design

Use a versioned family-authoring document in `dungeons.json`, then materialize it into the existing runtime model during startup.

```text
dungeons.json
  -> DungeonCatalogDocument
  -> DungeonDefinitionMaterializer
  -> IReadOnlyList<DungeonDefinition>
  -> existing DungeonDefinitionValidator
  -> existing IDungeonDefinitions consumers
```

Inheritance should exist only at the content-loading boundary. Domain and gameplay code should never perform fallback lookups such as "read Grade III, otherwise read the family base." Every consumer receives a complete definition.

### Top-level shape

```json
{
  "schemaVersion": 2,
  "families": [
    {
      "id": "goblin_mines",
      "name": "Goblin Mines",
      "region": 1,
      "sigilItemId": "sigil_goblin_mines",
      "entryCosts": [
        { "itemId": "sigil_goblin_mines", "amount": 1 }
      ],
      "monsterLootModifiers": {
        "Essence": 25
      },
      "hasRestSites": true,
      "roomTemplates": [
        {
          "id": "combat.main",
          "type": "Combat",
          "weight": 1,
          "encounterIds": [
            "goblin",
            "goblin_archer",
            "goblin_warrior",
            "hobgoblin"
          ]
        },
        {
          "id": "miniboss.foreman",
          "type": "MiniBoss",
          "weight": 1,
          "featuredEncounterId": "goblin_warrior",
          "encounterIds": [
            "goblin_warrior",
            "goblin_archer",
            "goblin"
          ]
        },
        {
          "id": "boss.warden",
          "type": "Boss",
          "weight": 1,
          "featuredEncounterId": "hobgoblin",
          "encounterIds": ["hobgoblin"]
        }
      ],
      "difficulties": [
        {
          "difficulty": 1,
          "id": "goblin_mines",
          "recommendedCombatRating": 250,
          "minRooms": 10,
          "maxRooms": 12,
          "rewardTable": {
            "firstClearRewards": [],
            "completionRewards": []
          },
          "gatheringNodes": []
        },
        {
          "difficulty": 2,
          "id": "goblin_mines_ii",
          "recommendedCombatRating": 750,
          "minRooms": 11,
          "maxRooms": 13,
          "rewardTable": {
            "firstClearRewards": [],
            "completionRewards": []
          },
          "gatheringNodes": []
        },
        {
          "difficulty": 3,
          "id": "goblin_mines_iii",
          "recommendedCombatRating": 2000,
          "minRooms": 12,
          "maxRooms": 14,
          "rewardTable": {
            "firstClearRewards": [],
            "completionRewards": []
          },
          "gatheringNodes": []
        }
      ]
    }
  ]
}
```

The abbreviated reward and gathering arrays above are illustrative. Migration should preserve the current authored values exactly.

## Field ownership

### Family fields

Author a value at family level when it normally describes the dungeon rather than its difficulty:

- `id` and base `name`.
- `region`.
- `sigilItemId` and `entryCosts`.
- Area or quest requirements shared by the family.
- `monsterLootModifiers`.
- `hasRestSites`.
- `roomTemplates`.
- Default completion-reward conventions, if introduced later.

### Difficulty fields

Author only values that genuinely vary:

- Stable flattened `id`.
- Numeric `difficulty`.
- `recommendedCombatRating`.
- `minRooms` and `maxRooms` until preview and actual topology are unified.
- First-clear and completion rewards.
- Gathering nodes and their difficulty-scaled loot.

An optional `delveId` can be added in a future schema version if a difficulty later uses a distinct topology.

### Derived fields

The materializer can safely derive current conventions while allowing explicit overrides:

| Runtime field | Default derivation |
| --- | --- |
| `Name` | Family name plus Roman difficulty, such as `Goblin Mines III`. |
| `Grade` | Difficulty 1-3 maps to `GradeI`-`GradeIII`. |
| `Tier` | Defaults to the numeric difficulty. |
| `RequiredPreviousDungeonId` | Previous entry in the same family's ordered difficulty list. |
| `RequiredPreviousDungeonGrade` | Previous difficulty's derived grade. |
| `TierRewardTableIds` | `reward.dungeon.tier.{difficulty}`. |
| `CompletionRewardTableIds` | `reward.dungeon.{difficultyId}.completion`. |
| `MinRooms` / `MaxRooms` | Copied from the difficulty's `minRooms` and `maxRooms`. |

Do not derive stable dungeon IDs from Roman-numeral formatting. IDs are referenced by saves, achievements, prerequisites, and APIs, so every difficulty should continue authoring its exact `id`.

## Encounter ownership

Room templates and encounter rosters belong exclusively to the dungeon family. Difficulty entries must not add, remove, replace, or reorder encounters. Goblin Mines I, II, and III therefore always draw from the same Combat, Miniboss, and Boss templates.

`featuredEncounterId` remains useful on the family-owned Boss and Miniboss templates. The materializer should place it first in the flattened `RoomDefinition.EncounterIds`, preserving the current reward rule in which only the featured Boss or Miniboss creature receives enhanced Essence tuning.

Keeping encounters family-owned makes the content contract simple: selecting a difficulty changes challenge length, progression requirements, and rewards, but not which creatures belong to that dungeon.

## Materialization rules

Introduce authoring-only models, for example:

- `DungeonCatalogDocument`.
- `DungeonFamilyDefinition`.
- `DungeonDifficultyDefinition`.
- `DungeonRoomTemplateAuthoringDefinition`.
- `DungeonDefinitionMaterializer`.

`JsonDungeonDefinitions` should deserialize the document, materialize every family/difficulty pair, pass the resulting definitions through the existing `DungeonDefinitionValidator`, and finally build its ID lookup.

Materialization order should be deterministic:

1. Deep-copy family-owned lists and dictionaries.
2. Derive difficulty identity, grade, tier, prerequisite chain, and conventional reward-table IDs.
3. Apply scalar difficulty overrides.
4. Normalize and deduplicate family encounter IDs while preserving order.
5. Put the family-owned featured encounter first for Boss and Miniboss templates.
6. Produce an independent `DungeonDefinition` object.
7. Validate the complete flattened catalog.

Every materialized difficulty must own independent mutable collections. Sharing the same `Rooms`, `EntryCosts`, or dictionaries between difficulties would allow runtime mutation of one difficulty to affect another. Difficulty-owned gathering-node definitions must also be copied into independent runtime collections.

## Delve and length handling

The family redesign can ship without changing `dungeon-delves.json`, but the authoring model should make the relationship explicit.

Recommended short-term rule:

- Delve selection continues using the existing family-ID matching behavior.
- `minRooms` and `maxRooms` must match the actual reachable room-count range, or be removed from the UI.

Recommended future rule for a genuinely longer hard mode:

- Allow a difficulty to override `delveId`, or add typed difficulty patches to the delve definition.
- Do not treat `minRooms` and `maxRooms` as generators unless `DungeonRunFactory` is deliberately changed to use them.
- Validate preview length against the chosen delve so advertised and actual length cannot drift.

The simplest maintainable option is an explicit alternate delve for a materially different hard mode. Patching individual route node indexes across difficulties would recreate the same duplication and fragility this redesign is intended to remove.

## Validation requirements

Validate the authoring document before or during materialization:

- `schemaVersion` is supported.
- Family IDs and flattened difficulty IDs are globally unique.
- Each family has at least one difficulty and difficulty numbers are unique.
- Difficulty ordering is deterministic.
- Every room-template ID is unique within its family.
- Every Combat, Miniboss, and Boss template has at least one encounter.
- Every Miniboss and Boss template has exactly one featured encounter and it exists in that template.
- Difficulty definitions cannot contain room or encounter overrides.
- Every materialized difficulty has the same room templates and ordered encounter IDs as its family.
- Derived prerequisite IDs and reward-table IDs resolve.
- Materialized collections are not reference-equal across difficulties.
- The existing `DungeonDefinitionValidator` accepts every flattened result.

Validation messages should include family ID, difficulty ID, and room-template ID so content errors can be fixed without tracing the materializer.

## Migration performed

The catalog was migrated atomically instead of permanently supporting two formats.

1. Added the authoring models, materializer, and authoring validator.
2. Converted Goblin Mines, Forgotten Catacombs, and the retired Hives content into families.
3. Changed `JsonDungeonDefinitions` to load schema version 2.
4. Kept the existing runtime `DungeonDefinition`, `IDungeonDefinitions`, DTOs, and consumers unchanged.
5. Removed the old top-level-array reader path.
6. Added materialization, encounter-sharing, collection-isolation, override-rejection, and content regression tests.

Keep creature composition at family level throughout the migration so equivalence failures remain easy to diagnose.

## Test plan

Add focused tests for:

- Every legacy dungeon ID still resolves after materialization.
- Materialized names, grades, tiers, ratings, prerequisites, preview lengths, rewards, gathering nodes, loot modifiers, and room templates match the pre-migration catalog.
- Every difficulty in a family materializes identical room templates and ordered encounter IDs.
- Difficulty data containing a room or encounter override is rejected as invalid.
- Family-level featured Boss and Miniboss creatures remain first in their materialized encounter lists.
- Materialized definitions do not share mutable collections.
- Existing `DungeonRunFactoryLayoutTests` still pass.
- Existing dungeon access, preview, reward, and definition-validation tests still pass.
- Delve selection remains stable for every existing difficulty ID.
- Existing preview room-count values materialize unchanged.

## Alternatives not recommended

### Copy every complete difficulty object

This is the current design. It is easy to deserialize but makes shared changes error-prone and obscures the few meaningful differences.

### Generic `extends` plus recursive JSON merge

For example, `goblin_mines_iii` could extend `goblin_mines_ii`. This saves space but introduces unclear array behavior, multi-level inheritance, ordering sensitivity, and difficult validation. Family base plus independent difficulty variants is flatter and easier to reason about.

### Runtime inheritance

Making gameplay services resolve base values dynamically spreads content semantics throughout the application and risks partially resolved definitions. Materialize once at startup instead.

### Difficulty-specific room overrides

Allowing difficulties to patch room templates or encounter lists complicates validation and weakens the family contract. All creature composition should remain family-owned.

## Definition of done

The redesign is complete when:

- Each dungeon family authors shared rooms, sigil costs, and modifiers once.
- Each difficulty block contains only genuine differences and explicit stable identity.
- Existing public dungeon IDs and runtime behavior are preserved.
- Every difficulty in a family uses identical room templates and creature rosters.
- Boss and Miniboss featured-creature identity remains explicit.
- Existing delve selection remains unchanged; topology authoring remains a separate follow-up concern.
- Retired content no longer needs three fully duplicated live definitions.
- All dungeon definition, layout, access, preview, reward, and Essence tests pass.
