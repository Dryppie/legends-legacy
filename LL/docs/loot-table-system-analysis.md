# Loot Table System Analysis

## Summary

The current loot implementation works for small amounts of authored content, but it is not a strong foundation for a larger game economy. It has useful pieces worth keeping: a centralized `InventoryItem` creation path, JSON-authored dungeon rewards, dungeon definition validation, and a clear separation between reward calculation and reward persistence. The weakest part is the old EF-backed recursive `LootTable` model and `LootService` roller. It has unclear probability semantics, limited validation, non-deterministic random sources, incomplete service/repository methods, and content split between database seed code and JSON definitions.

Recommendation: replace the current loot-table model with a new data-driven reward table system instead of trying to polish the existing one. The new system should make each roll type explicit, define probabilities in one consistent format, use injectable randomness, support simulation/preview tooling, and keep reward definitions close to content files rather than hard-coded EF seed methods.

Implementation note: the replacement system now exists in the codebase. Dungeon completion/tier loot has moved to `LL/src/API/API.LL/Data/reward-tables.json`, dungeon definitions reference string reward table IDs, dungeon completion and preview use `IRewardRoller`, and dungeon gathering no longer creates temporary `LootTable` objects at runtime. Area gathering and creatures now have optional `RewardTableId` fields, the Blood Grove Bloodwood Tree node is authored through the new JSON reward table file, and `LootService` can roll creature `RewardTableId`s while preserving legacy EF loot tables as fallback compatibility.

## Target Service

This analysis targets the main `LL` game application:

- Core domain/application reward models under `LL/src/Core`.
- Infrastructure reward generation and persistence under `LL/src/Infrastructure`.
- API content files under `LL/src/API/API.LL/Data`.
- Relevant frontend/admin usage only where it affects preview or authoring expectations.

## Current Implementation

### Domain Model

The legacy loot table model is very small:

- `LootTableEntry` has `Id` and `Weight`.
- `LootTable` inherits `LootTableEntry` and owns `ICollection<LootTableEntry> Entries`.
- `LootTableItem` inherits `LootTableEntry` and adds `ItemId`, `Item`, `MinQuantity`, `MaxQuantity`, and `IsRare`.

Relevant files:

- `LL/src/Core/Domain/Models/LootTables/LootTableEntry.cs`
- `LL/src/Core/Domain/Models/LootTables/LootTable.cs`
- `LL/src/Core/Domain/Models/LootTables/LootTableItem.cs`
- `LL/src/Core/Domain/Models/LootTables/LootContext.cs`
- `LL/src/Core/Domain/Models/LootTables/LootSource.cs`

The inheritance model allows recursive tables, but it does not encode important reward intent. A nested table can mean "choose a rarity bucket", "roll another pool", or "group these items", but that meaning only exists implicitly in how content is shaped.

### Roller

`LootService` is the main roller:

- `GenerateGatheringLootAsync(...)`
- `GenerateDungeonLoot(...)`
- `GenerateIdleCombatLootAsync(...)`
- `GenerateCinderLoot(...)`
- `GenerateSoulstoneLoot(...)`
- `GetRandomLoot(...)`

Relevant file:

- `LL/src/Infrastructure/Service/Services.LL/Loots/LootService.cs`

The main item roller uses this flow:

1. Roll once per requested roll.
2. Select one entry from the table with `GetRandomEntryBasedOnWeight`.
3. If the entry is an item, convert it to an `InventoryItem`.
4. If the entry is another table, recurse once into that nested table.

Important behavior:

- The selection roll is `RandomGenerator.NextDouble() * 100`.
- The total table weight is not normalized to 100.
- If the roll is greater than the sum of effective weights, no item drops.
- If a table has weights summing to 45, it behaves like a 45% chance to drop anything.
- If a table has weights summing to 100, it behaves like exactly one drop per roll.
- If a table has weights summing above 100, the entries after the 100th effective weight are effectively unreachable because the roll never exceeds 100.

This is closer to a "weighted chance table with implicit no-drop" than a normal weighted table. That can be valid, but it is not obvious from the names or model.

There is also a direct marker in the code: `// TODO: Redo Loot Generation`.

### Data Sources

The project currently has several reward-data styles.

#### EF-Backed Loot Tables

Dungeon completion/tier loot tables are seeded in code:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Seeds/JsonSeeding/DbJsonSeeder.cs`

That file currently contains 80 `CreateLootTableItem(...)` calls for dungeon loot tables.

Area/creature/gathering legacy content is also seeded in code:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Seeds/Seeding/SeedCreatures.cs`

That file creates many `LootTable` instances with `Guid.NewGuid()`, including many empty creature loot tables and at least one gathering table.

#### JSON Dungeon Rewards

Dungeon definitions are authored in JSON:

- `LL/src/API/API.LL/Data/dungeons.json`

The current file has:

- 9 dungeon definitions.
- 9 `rewardTable` sections.
- 9 `completionLootTableId` references.
- 9 `tierLootTableId` references.
- 21 dungeon gathering nodes.
- 80 dungeon gathering loot entries.

Each dungeon can contain:

- `rewardTable.firstClearRewards`
- `rewardTable.completionRewards`
- `completionLootTableId`
- `tierLootTableId`
- `monsterLootModifiers`
- `gatheringNodes[].loot[]`

This means dungeon rewards are split between JSON grant lists and EF-backed seeded loot tables.

#### Hard-Coded Dungeon Progression Rewards

Dungeon completion also awards potential cores and monster cores through code:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonCompletionRewardApplier.cs`
- `LL/src/Core/Domain/Models/Dungeons/Definitions/DungeonRewardCatalog.cs`

These are deterministic-ish progression rewards with small random ranges and grade-based tables.

### Persistence

Loot tables are stored with EF Core using table-per-hierarchy:

- `LootTableEntry` table with a `LootTableType` discriminator.
- `LootTable` and `LootTableItem` inherit from `LootTableEntry`.
- `LootTable` has many child entries with `DeleteBehavior.Restrict`.

Relevant files:

- `LL/src/Infrastructure/Persistence/Persistence.LL/LLDbContext.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/LootTables/LootTableEntryConfiguration.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/LootTables/LootTableConfiguration.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Extensions/LootTableQueryExtensions.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/LootTables/LootTableRepository.cs`

`LootTableRepository.GetLootTableByIdAsync(...)` loads entries and nested entries, but only to a fixed depth. `GetMonsterLootTableAsync(...)` and `GetProfessionTaskLootTableAsync(...)` are not implemented in both repository and service layers.

### Consumers

Main consumers include:

- Idle combat rewards: `IdleCombatRewardCalculator`.
- Dungeon combat rewards: `DungeonCombatRewardCalculator`.
- Dungeon completion rewards: `DungeonCompletionRewardApplier`.
- Gathering rewards: `CombatGatheringRewardProcessor`.
- Dungeon preview rewards: `DungeonPreviewRewardService`.

The consumers generally converge on `InventoryItem` output, which is good. The problem is that they feed the roller from different models and different data sources.

## What Works

### Inventory Item Creation Is Centralized

`LootService` uses `IInventoryItemFactory` instead of constructing item instances by hand. Dungeon completion grants also use the same factory. This is worth keeping because stackable/non-stackable behavior belongs in one place.

### Reward Persistence Is Separate From Reward Rolling

Dungeon rewards are calculated, converted to pending rewards, then claimed later. That separation is healthy:

- Combat calculators produce outcomes.
- Pending reward writers store pending rewards.
- Claiming applies inventory/currency/experience.

This helps support dungeon checkpoint and withdrawal logic.

### Dungeon JSON Content Is Moving In The Right Direction

The JSON dungeon definitions are more maintainable than hard-coded EF seed blocks. The newer `rewardTable` and `gatheringNodes` shapes are easier to review, diff, and author than C# seed methods.

### Basic Definition Validation Exists

`DungeonDefinitionValidator` validates dungeon reward grants and gathering node loot:

- Required item IDs.
- Positive quantities.
- Max quantity not below min.
- Chance between 0 and 1.
- Gathering weight greater than 0.

That validation should be expanded, not discarded.

### The System Already Supports Multipliers

There are hooks for reward modifiers:

- `LootContext.TypeMultipliers`.
- `RareEntryWeightBonusPercent`.
- Dungeon `monsterLootModifiers`.
- Tool bonuses such as rare material chance, bonus roll chance, yield, and double gather chance.

The idea is good. The implementation needs clearer semantics.

## Pain Points

### 1. Probability Semantics Are Ambiguous

The word `Weight` suggests normalized weighted choice, but the roller uses a fixed 0-100 roll and treats total effective weight below 100 as a no-drop chance.

Example:

- Entries with weights `45`, `30`, `20`, `5` sum to `100`, so one item always drops.
- Entries with weights `10`, `8`, `5`, `1`, `0.75` sum to `24.75`, so the table has a 75.25% no-drop chance.
- Entries summing over `100` would not behave as a normal weighted table. Later entries can become unreachable.

This is not inherently wrong, but it is risky because content authors must know that `Weight` means "percentage points on a 100-sided roll" and not "relative weight".

### 2. Nested Tables Have No Explicit Meaning

Recursive `LootTable` entries can be powerful, but the model does not say whether a nested table is:

- A rarity bucket.
- A sub-pool.
- A guaranteed group.
- A conditional table.
- A no-drop wrapper.

The only signal is the nested table's `Weight`, and the include path only loads a fixed nesting depth. This is fragile as the reward model grows.

### 3. Some Seeded Loot Tables Are Empty

`SeedCreatures.cs` creates many creature loot tables with an empty nested "legendary" table. Rolling one of those tables can select the nested table, recurse, and produce nothing. That is mechanically allowed, but it makes the content look more complete than it is.

The comments beside some weights also appear misleading. Several nested tables have comments like `// 0.02%`, while the current roller treats `Weight = 5` as a 5% chance to enter that empty nested table, not 0.02%.

### 4. Content Is Split Across JSON And C# Seed Code

For dungeons, the same reward surface is divided across:

- `dungeons.json` direct reward grants.
- `dungeons.json` gathering node loot.
- `dungeons.json` loot table IDs.
- `DbJsonSeeder.cs` hard-coded loot table contents.
- `DungeonCompletionRewardApplier.cs` hard-coded core reward rolls.
- `DungeonRewardCatalog.cs` fallback first-clear reward grants.

That makes it hard to answer simple design questions:

- What can this dungeon drop?
- What is the expected reward per completion?
- What changes when I move from Grade I to Grade II?
- Is a reward preview complete?
- Which file should a designer edit?

### 5. The Repository/Service Contract Is Partially Unimplemented

These methods exist but throw:

- `LootTableRepository.GetMonsterLootTableAsync(...)`
- `LootTableRepository.GetProfessionTaskLootTableAsync(...)`
- `LootTableService.GetMonsterLootTableAsync(...)`
- `LootTableService.GetProfessionTaskLootTableAsync(...)`

Most current consumers avoid these methods by loading creatures with included loot tables, but the public contract advertises capabilities that do not exist.

### 6. Randomness Is Not Injectable In The Core Loot Roller

Some reward services use `IRandomSource`, but `LootService` uses a static `Random` plus `Random.Shared`. `DungeonCompletionRewardApplier` also uses `Random.Shared` directly for grant quantities and core rewards.

This makes statistical tests and deterministic dungeon replay harder. It also prevents using a dungeon run seed consistently across all random reward outcomes.

### 7. Preview And Actual Roll Logic Can Drift

`DungeonPreviewRewardService` flattens `LootTableItem`s to show possible rewards. It does not expose:

- Real drop chance.
- Quantity range.
- No-drop chance.
- Nested effective probability.
- Modifier effects.
- Expected value.

Because preview code only flattens items, it can make very rare rewards look equivalent to common rewards.

### 8. Validation Does Not Cover Cross-File Reward Integrity

The dungeon JSON validator checks local numeric shape, but not all important integrity rules:

- Item IDs exist.
- Loot table IDs exist.
- Effective weights do not exceed expected caps.
- A weighted table has reachable entries.
- A table is not accidentally empty.
- Completion rewards do not duplicate or conflict with seed-table drops.
- Preview and runtime can both load the same definitions.

Some missing item IDs are silently dropped when building dungeon gathering tables because only known item bases are converted into `LootTableItem`s.

### 9. Weight Multipliers Can Create Unintended No-Drop Or Over-Cap Behavior

`TypeMultipliers` and rare gathering bonuses multiply/add to weights, but the table still rolls against a fixed 100. This means bonuses affect both:

- Which item wins among entries.
- Whether anything drops at all.

That may be intended for some bonuses, but it should be explicit. A "rare material chance" bonus probably should not accidentally make the full table exceed 100 and change unrelated drop math in surprising ways.

### 10. Reward Types Are Mixed Together

The reward system currently blends several concepts:

- Item drops.
- Progression currencies/materials.
- Soulstones.
- Cinders.
- Experience.
- Essence drops.
- Dungeon mastery.
- First-clear rewards.
- Completion table rolls.
- Tool/gathering bonuses.

Some of these are tables, some are hard-coded roll methods, and some are separate services. The separation is not always domain-driven; it often reflects implementation history.

### 11. Tests Do Not Directly Protect Loot Table Math

There are tests around dungeon essence rewards and dungeon state, but there does not appear to be a direct test suite for:

- Weighted loot selection.
- No-drop behavior.
- Nested table effective probabilities.
- Quantity rolls.
- Multipliers.
- Gathering rare bonuses.
- Simulation/expected value.

This is especially risky for economy work because small probability mistakes can compound heavily.

## Design Direction

The current system is serviceable for prototypes and small content batches, but I would not build the long-term game economy on it. The best path is a new reward table system that can coexist with the current one during migration.

The new system should use explicit reward definitions and explicit roll semantics instead of recursive EF entities.

## Proposed New System

### Core Concepts

Introduce a content-level reward table model, likely JSON-authored first:

```json
{
  "id": "reward.dungeon.goblin_mines.grade_1.completion",
  "rolls": [
    {
      "id": "blueprint_bonus",
      "type": "independent",
      "chance": 0.05,
      "rewards": [
        { "type": "item", "itemId": "blueprint_fury", "quantity": { "min": 1, "max": 1 } }
      ]
    },
    {
      "id": "tool_drop",
      "type": "weighted",
      "rolls": 1,
      "noDropWeight": 75.25,
      "entries": [
        { "itemId": "pickaxe", "weight": 8, "quantity": { "min": 1, "max": 1 } },
        { "itemId": "fishing_rod", "weight": 5, "quantity": { "min": 1, "max": 1 } },
        { "itemId": "rare_pickaxe", "weight": 1, "quantity": { "min": 1, "max": 1 } },
        { "itemId": "rare_fishing_rod", "weight": 0.75, "quantity": { "min": 1, "max": 1 } }
      ]
    }
  ]
}
```

The important part is not the exact JSON shape. The important part is that table types are explicit.

Recommended roll types:

- `all`: grant every child reward.
- `independent`: each child has its own chance.
- `weighted`: choose one child by relative weight.
- `weightedWithNoDrop`: choose one child or no-drop by relative weight.
- `sequence`: execute child rolls in order.
- `reference`: execute another named reward table.

This removes the ambiguity of "is `Weight` a percent or a relative weight?"

### Reward Result Model

Have the roller return a neutral reward result before creating inventory rows:

```csharp
public sealed record RewardRollResult(
    IReadOnlyList<ItemReward> Items,
    int Cinders,
    int Soulstones,
    int Experience,
    IReadOnlyList<RewardRollTrace> Trace);
```

Then a separate applier/writer converts results into inventory, currency, experience, pending dungeon rewards, and telemetry.

This keeps `InventoryItemFactory` centralized while avoiding a loot service that only understands items.

### Random Source

Use an injected random abstraction everywhere reward rolls happen:

- `IRandomSource.NextDouble()`
- `IRandomSource.NextInt(minInclusive, maxExclusive)`
- Optional seeded implementation per dungeon run.
- Optional deterministic implementation for tests.

Dungeon runs already have a `Seed`. Reward rolling should be able to use a run-scoped random stream so replays/debugging can explain outcomes.

### Definition Provider

Add a provider similar to existing JSON definition providers:

- `IRewardTableDefinitionProvider`
- `JsonRewardTableDefinitionProvider`
- `RewardTableDefinitionValidator`

Suggested content files for a larger split:

- `LL/src/API/API.LL/Data/reward-tables.json` for the current small version.
- `LL/src/API/API.LL/Data/reward-tables/dungeons.json` later if the single file gets too large.
- `LL/src/API/API.LL/Data/reward-tables/gathering.json` later if gathering grows its own table family.
- `LL/src/API/API.LL/Data/reward-tables/monsters.json` later when monster loot is migrated.

Or one file per feature if that is easier to maintain as a solo developer.

### Authoring Rules

Definitions should support:

- Stable string IDs, not GUIDs, for authored content.
- Item ID references.
- Quantity ranges.
- Roll counts.
- Explicit no-drop.
- Tags, categories, and source labels for preview and telemetry.
- Conditions such as first clear, dungeon grade, region, monster type, and tool type if needed.
- Modifiers that specify whether they affect drop chance, entry weight, quantity, or extra rolls.

Avoid making the first version too clever. The immediate goal is clarity, not a full MMO reward scripting language.

### Validation Rules

The new validator should check:

- Duplicate reward table IDs.
- Duplicate roll IDs inside a table.
- Referenced item IDs exist.
- Referenced reward table IDs exist.
- No circular references.
- Quantities are valid.
- Chances are between 0 and 1.
- Weights are positive.
- Weighted tables have at least one reachable entry.
- No-drop is explicit when a table can drop nothing.
- Effective table previews can be calculated.

This should run at startup and in tests.

### Preview And Simulation

Add a small analysis service:

- `GetPossibleRewards(tableId)`
- `GetExpectedValue(tableId, context)`
- `Simulate(tableId, context, iterations)`

This would power:

- Dungeon previews.
- Admin diagnostics.
- Economy balancing.
- Regression tests.

The current preview service should eventually stop flattening EF tables and instead ask this analysis service for possible rewards and approximate chances.

### Modifier Semantics

Modifiers should target explicit knobs:

- `dropChanceMultiplier`
- `entryWeightMultiplier`
- `quantityMultiplier`
- `extraRollChance`
- `guaranteedMinimum`
- `rarityWeightBonus`

For example, a tool's `RareMaterialChancePercent` should clearly modify only entries tagged `rare`, and the table definition should decide whether that increases total drop chance or only shifts selection within successful drops.

### Compatibility Layer

To reduce migration risk, add an adapter:

- `LegacyLootTableAdapter` converts current EF `LootTable` instances into new reward definitions.
- Preserve the current fixed-100/no-drop semantics in the adapter.
- Use it only while migrating old tables.

This allows consumers to move to the new roller before all content is rewritten.

## Suggested Migration Plan

### Phase 1: Freeze And Characterize Current Behavior

Add focused tests around the existing roller before changing it:

- Table with total weight below 100 can drop nothing.
- Table with total weight 100 always drops one item.
- Table with total weight above 100 has unreachable tail entries.
- Nested table probabilities multiply as expected.
- Gathering rare bonus changes effective weight.
- Type multipliers affect weight and no-drop chance.

This creates a safety net and documents current behavior.

### Phase 2: Introduce New Reward Definitions Beside Existing Loot Tables

Add new domain/application models and JSON provider without deleting the old EF tables. Start with dungeon completion/tier rewards because the content count is manageable.

### Phase 3: Move Dungeon Completion And Tier Loot To JSON Reward Tables

Replace `completionLootTableId` and `tierLootTableId` GUID references in `dungeons.json` with string reward table IDs.

Keep current rewards numerically equivalent at first. For every legacy table whose weights sum below 100, represent the missing chance as explicit no-drop.

### Phase 4: Move Dungeon Gathering Nodes To The New Roller

Dungeon gathering nodes already live in JSON and have a clean shape. Convert them to use the new reward table engine directly instead of building temporary `LootTable` objects with `Guid.NewGuid()`.

Status: implemented for dungeon gathering. The runtime now builds inline `RewardTableDefinition` objects from dungeon JSON node loot or uses an explicit node `RewardTableId`.

### Phase 5: Move Area And Monster Loot

Replace empty creature loot tables with either:

- No table reference, if the creature should not drop items.
- A real monster reward table ID, if it should.

This is also the right time to remove unimplemented `GetMonsterLootTableAsync` and `GetProfessionTaskLootTableAsync`, or implement them against the new provider.

Status: partially implemented. Creatures and persistent area gathering nodes can now reference reward table IDs. The seeded Blood Grove gathering node uses `reward.gathering.blood_grove.bloodwood_tree`; old EF loot table references remain nullable fallback fields so existing databases and old content continue to run.

### Phase 6: Retire EF Loot Tables

Once all runtime consumers use the new reward system:

- Remove EF `LootTableEntry`, `LootTable`, and `LootTableItem` persistence.
- Remove hard-coded dungeon loot table seeding from `DbJsonSeeder`.
- Generate an EF migration to drop obsolete loot-table tables/columns if no longer needed.

This migration should not be applied to shared or production databases without a separate deployment plan.

## Proposed Shape For Services

```csharp
public interface IRewardRoller
{
    RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context);
}

public interface IRewardTableDefinitionProvider
{
    RewardTableDefinition GetById(string id);
    IReadOnlyList<RewardTableDefinition> GetAll();
}

public sealed record RewardRollContext(
    Guid CharacterId,
    string Source,
    IReadOnlyDictionary<string, double> Modifiers,
    IRandomSource Random);
```

Feature services should not know about weighted selection details. They should build a `RewardRollContext`, ask the roller for a result, then apply the result through existing writers/factories.

## What To Keep

Keep:

- `InventoryItemFactory`.
- Pending dungeon reward flow.
- Dungeon reward claim separation.
- JSON dungeon definitions.
- Startup/content validation pattern.
- Tool bonus concepts.
- `DungeonPreviewRewardService` as a public-facing concept, but change its backend.
- `DungeonRewardCatalog` concepts for progression rewards, though the data should move into reward definitions where practical.

## What To Replace

Replace:

- Recursive EF `LootTable` as the primary authoring/runtime model.
- Fixed 0-100 implicit no-drop semantics hidden behind `Weight`.
- Static/random-shared reward rolling.
- Hard-coded reward table seeding in C#.
- Preview logic that only flattens possible item IDs.
- Partially implemented loot table service/repository contract.

## Immediate Low-Risk Improvements

If a full replacement is not started immediately, these smaller fixes would still help:

1. Rename or document legacy `Weight` as `ChanceWeightOutOf100` behavior in code comments and docs.
2. Add direct `LootService` tests for current behavior.
3. Inject `IRandomSource` into `LootService`.
4. Add validation that effective top-level weight does not exceed 100 for legacy chance tables.
5. Remove or implement unused `GetMonsterLootTableAsync` and `GetProfessionTaskLootTableAsync`.
6. Add a diagnostics command/test that prints effective dungeon reward chances.
7. Stop silently dropping unknown item IDs when building dungeon gathering loot.

## Final Recommendation

Build a new reward table system.

The current implementation is not broken in the sense that it cannot produce rewards. It is broken in the sense that it makes reward intent hard to see, hard to validate, hard to test, and hard to balance. The codebase already has enough newer JSON-driven dungeon infrastructure to support a cleaner system. The new model should be explicit, data-driven, testable, and previewable, with a compatibility layer for legacy EF tables during migration.

The highest-value first move is to add tests that characterize the old roller, then move dungeon completion/tier loot from GUID-seeded EF tables into JSON reward tables with explicit no-drop. That gives quick design clarity without forcing every creature and gathering table to migrate in one pass.
