# Reward Table System Spec

This document describes the replacement reward table system used for authored loot and rewards. It is the practical reference for editing reward table content, understanding roll behavior, and wiring reward tables into game content.

## Goals

- Keep reward content data-driven and readable.
- Make probability semantics explicit.
- Support zero, one, or many rewards from the same table.
- Support reusable nested tables without EF-backed recursive loot table records.
- Validate content at startup so broken reward definitions fail loudly.
- Keep reward rolling deterministic enough to test through injectable randomness.

## Content Location

Reward tables are authored in:

```text
LL/src/API/API.LL/Data/rewards/reward-tables.json
```

The runtime path is built from `Content:Root`, which defaults to `Data`, then appends `rewards/reward-tables.json`.

## Runtime Types

### RewardTableDefinition

```csharp
public sealed class RewardTableDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<RewardRollDefinition> Rolls { get; set; } = [];
}
```

A reward table is a named container for one or more rolls. A single table can grant nothing, one thing, or many things depending on its rolls.

### RewardRollDefinition

```csharp
public sealed class RewardRollDefinition
{
    public string Id { get; set; } = string.Empty;
    public RewardRollType Type { get; set; }
    public int Rolls { get; set; } = 1;
    public double Chance { get; set; } = 1;
    public double NoDropWeight { get; set; }
    public List<RewardEntryDefinition> Entries { get; set; } = [];
}
```

Each roll describes one reward operation within a table.

- `id`: Unique within the table.
- `type`: How entries are evaluated.
- `rolls`: Number of times this roll is attempted. Must be greater than zero.
- `chance`: Roll-level chance from `0` to `1`. If this fails, no entries from this roll are evaluated for that attempt.
- `noDropWeight`: Extra weighted slot used only by `WeightedWithNoDrop`.
- `entries`: Candidate rewards or referenced tables.

### RewardEntryDefinition

```csharp
public sealed class RewardEntryDefinition
{
    public string Id { get; set; } = string.Empty;
    public RewardEntryType Type { get; set; } = RewardEntryType.Item;
    public string? ItemId { get; set; }
    public string? RewardTableId { get; set; }
    public double Weight { get; set; }
    public double Chance { get; set; } = 1;
    public RewardQuantityRange Quantity { get; set; } = new();
    public List<string> Tags { get; set; } = [];
}
```

An entry is either a concrete reward or a reference to another reward table.

- `id`: Unique enough to identify this entry in traces and validation messages.
- `type`: Reward output type.
- `itemId`: Required for `Item` entries.
- `rewardTableId`: Required for `RewardTableReference` entries.
- `weight`: Required for weighted roll types.
- `chance`: Entry-level chance from `0` to `1`.
- `quantity`: Inclusive min/max range. Defaults to `1`.
- `tags`: Optional labels used by reward bonuses.

### RewardQuantityRange

```csharp
public sealed class RewardQuantityRange
{
    public int Min { get; set; } = 1;
    public int Max { get; set; } = 1;
}
```

Quantities are inclusive. `{ "min": 1, "max": 3 }` can produce `1`, `2`, or `3`.

## Roll Types

### All

Evaluates every entry in order. Each entry still applies its own `chance`.

Use this for guaranteed bundles such as currency plus an item.

### Independent

Evaluates every entry independently. Each entry has its own `chance`, so the roll can produce zero, one, or many rewards.

Use this when several rewards should be able to drop together.

### Weighted

Selects exactly one weighted entry, then applies that entry's `chance`.

This roll type has no explicit no-drop slot. It can still produce nothing if the selected entry's `chance` fails or the resulting quantity is zero.

### WeightedWithNoDrop

Selects exactly one weighted entry or the no-drop slot.

The no-drop probability is:

```text
noDropWeight / (noDropWeight + sum(entry weights))
```

Use this for classic "most kills drop nothing, sometimes one item drops" tables.

### Sequence

Evaluates entries in order. In the current implementation, this behaves the same as `All`, but its name communicates intent when composing table references.

Use this for staged or composed reward tables where order improves readability.

### Reference

Evaluates entries in order. In the current implementation, this behaves the same as `All`, but its name communicates that the roll exists mainly to execute referenced tables.

Use this for tables that only combine other tables.

## Entry Types

### Item

Adds an item reward.

Required fields:

- `itemId`
- positive `quantity.max`

### RewardTableReference

Executes another reward table and merges the result into the current roll result.

Required fields:

- `rewardTableId`

Reference cycles are invalid and are rejected by validation.

### Cinders

Adds cinders to the reward result.

### Soulstones

Adds soulstones to the reward result.

### Experience

Adds experience to the reward result.

## Result Shape

Rolling a table returns a `RewardRollResult` with:

- `Items`: item rewards with item ID, quantity, and source.
- `Cinders`: total cinders.
- `Soulstones`: total soulstones.
- `Experience`: total experience.
- `Trace`: roll trace entries for diagnostics and previews.

Items are not automatically persisted by the roller. Consumers are responsible for applying the result to inventory, pending dungeon rewards, currencies, or other state.

## Bonus Tags

`RewardRollContext` can carry bonuses by tag:

```csharp
public sealed record RewardRollContext(
    string Source,
    IReadOnlyDictionary<string, double>? EntryWeightBonusPercentByTag = null,
    IReadOnlyDictionary<string, double>? QuantityBonusPercentByTag = null);
```

If an entry has matching tags:

- `EntryWeightBonusPercentByTag` increases its effective weighted-roll weight.
- `QuantityBonusPercentByTag` increases the rolled quantity after the base quantity is sampled.

Bonuses are additive across matching tags. Negative bonus values are ignored.

Example:

```json
{
  "id": "rare_pickaxe",
  "type": "Item",
  "itemId": "rare_pickaxe",
  "weight": 1,
  "tags": [ "rare", "tool" ]
}
```

If the context gives `rare = 25` and `tool = 10`, the effective weight is:

```text
baseWeight * (1 + (25 + 10) / 100)
```

## Validation Rules

Reward table validation currently checks:

- Reward table IDs are present and unique.
- Each table has at least one roll.
- Roll IDs are present and unique within a table.
- `rolls` is greater than zero.
- Roll `chance` is between `0` and `1`.
- `noDropWeight` is not negative.
- Each roll has at least one entry.
- Weighted roll types have positive total entry weight.
- Weighted entries have positive weight.
- Entry IDs are present.
- Entry `chance` is between `0` and `1`.
- Quantity min is not negative.
- Quantity max is greater than or equal to min.
- Item, cinder, soulstone, and experience entries have positive max quantity.
- Item entries have an `itemId`.
- Item IDs exist in `Data/items/items.json` when item validation is available.
- Reward table references point at existing reward tables.
- Reward table references do not form cycles.

## JSON Examples

### Weighted Table With No Drop

```json
{
  "id": "reward.dungeon.tier.1",
  "displayName": "Dungeon Tier 1 Loot",
  "rolls": [
    {
      "id": "tier_1_weighted_drop",
      "type": "WeightedWithNoDrop",
      "noDropWeight": 48,
      "entries": [
        {
          "id": "advancement_stone",
          "type": "Item",
          "itemId": "advancement_stone",
          "weight": 32
        },
        {
          "id": "rare_pickaxe",
          "type": "Item",
          "itemId": "rare_pickaxe",
          "weight": 1,
          "tags": [ "rare", "tool" ]
        }
      ]
    }
  ]
}
```

### Independent Multi-Drop Table

```json
{
  "id": "reward.example.independent",
  "displayName": "Independent Example",
  "rolls": [
    {
      "id": "materials",
      "type": "Independent",
      "entries": [
        {
          "id": "ore",
          "type": "Item",
          "itemId": "iron_ore",
          "chance": 0.5,
          "quantity": { "min": 1, "max": 3 }
        },
        {
          "id": "gem",
          "type": "Item",
          "itemId": "rough_gem",
          "chance": 0.05,
          "quantity": { "min": 1, "max": 1 }
        }
      ]
    }
  ]
}
```

This can return zero, one, or both entries.

### Composed Table

```json
{
  "id": "reward.example.composed",
  "displayName": "Composed Example",
  "rolls": [
    {
      "id": "references",
      "type": "Reference",
      "entries": [
        {
          "id": "base",
          "type": "RewardTableReference",
          "rewardTableId": "reward.example.base"
        },
        {
          "id": "bonus",
          "type": "RewardTableReference",
          "rewardTableId": "reward.example.bonus",
          "chance": 0.25
        }
      ]
    }
  ]
}
```

## Current Consumers

### Creatures

Creatures can define `RewardTableId`. `LootService` rolls the referenced reward table when the creature is defeated.

### Dungeons

Dungeon definitions can define:

- `completionRewardTableIds`
- `tierRewardTableIds`

Dungeon completion reward application rolls those tables into pending dungeon rewards.

### Dungeon Gathering

Dungeon gathering nodes can use either inline loot entries or `RewardTableId`. If a reward table ID is present, the reward roller is used.

### Area Gathering

Persistent area gathering nodes can define `RewardTableId`.

## Authoring Guidelines

- Use stable, namespaced IDs such as `reward.dungeon.goblin_mines.completion`.
- Prefer `WeightedWithNoDrop` for rare single-drop tables.
- Prefer `Independent` when multiple entries should be able to drop together.
- Put shared content into referenced tables instead of duplicating large entry lists.
- Use tags for mechanical bonus hooks, not display text.
- Keep no-drop explicit instead of adding dummy item entries.
- Add a `displayName` that explains the table's purpose to designers and diagnostics.
- Keep entry IDs short but meaningful; they appear in traces and validation errors.

## Known Follow-Ups

- Add content tests that verify every `RewardTableId` used by creatures, dungeons, and gathering nodes exists.
- Add more roller tests for quantity ranges, roll-level chance, entry-level chance, scalar rewards, tag bonuses, multiple roll attempts, and validation failures.
- Add a reward-table simulator or expected-value report for tuning.
- Add admin/dashboard support for previewing effective drop rates.
- Verify migration history so old EF `LootTableEntry` tables are not reintroduced by later migrations.
