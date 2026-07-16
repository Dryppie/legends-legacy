# Offline Combat Single-Pass Performance

## Context

Idle combat now processes every due encounter, up to the configured 24-hour
offline limit, in one orchestration pass. At the current ten-second cadence this
can represent 8,641 encounters. The combat engine is intentionally invoked once
per encounter so that battle outcomes and random rolls retain their existing
semantics, but shared persistence data must not be queried once per encounter.

## Audit findings

### 1. Gathering reward item bases

`CombatGatheringRewardProcessor` performs reward rolls for every eligible
victory and gathering node. Each successful roll previously loaded its item
bases independently. A full offline window could therefore issue thousands of
repeated item-base queries.

The processor should retain individual random rolls, collect their distinct item
IDs, load the corresponding item bases once, and then materialize every result.

### 2. Regular combat-loot item bases

`IdleCombatRewardCalculator` invokes `GenerateIdleCombatLootAsync` for every
victory. Successful reward-table rolls then query item bases independently.

The loot service should accept all encounters as a batch, perform the same
ordered reward rolls, load all distinct item bases once, and return loot grouped
by encounter.

### 3. Creature resonance

Every essence-eligible defeated creature requests its resonance row. EF tracking
avoids repeated SQL after a row has been loaded, but distinct creature types are
still fetched independently.

All resonance rows for the distinct eligible creature IDs should be loaded in
one query before encounter reward calculation. Missing rows can then be created
and tracked in memory.

### 4. Successful essence item bases

Each successful essence drop resolves its item base separately. The possible
essence item IDs are known from the eligible creatures' loot-table variants and
can be loaded once before rolls are processed.

### 5. Essence focus

Essence focus was previously checked through one `SELECT EXISTS` call per
encounter invocation. A request-scoped cache reduced this to one lookup per
distinct creature type. Because a character can have only one focused creature,
the stronger solution is one scalar query for the focused creature ID followed
by in-memory comparisons for every roll.

### 6. Reward-fact hostile entity loading

The idle resolution session already preloads every source entity and exposes
them through `CombatOrchestrationResult.SourceEntitiesById`. The reward fact
builder should reuse that catalog instead of issuing a second bulk entity query.

## Already aggregated

- Combat source entities and templates are loaded once before simulation.
- Creature archive kill counts are grouped and queried once.
- Prophecy progress loads applicable prophecy state once for the complete event batch.
- Inventory stacking queries all rewarded stackable item types together.
- Experience, currency, guild contribution, sigils, and outbox progress are applied once per session.

## Implementation principles

- Preserve one combat simulation and the existing number of random reward rolls
  per encounter.
- Preserve random-number consumption order where it affects outcomes.
- Bulk-load immutable/shared persistence data before materializing rewards.
- Keep encounter-specific loot grouped so API/session behavior remains stable.
- Do not introduce an offline processing batch cap; the complete 24-hour window
  remains one orchestration operation.

## Implementation status

Completed:

- Regular combat reward rolls are materialized through one item-base load.
- Gathering reward rolls are materialized through one item-base load while
  retaining per-roll outcomes and tool-bonus behavior.
- Eligible creature resonance rows are loaded together and missing rows are
  created in the request's tracked state before rolls begin.
- The focused creature ID is loaded once and compared in memory.
- Possible essence item bases are preloaded from the eligible loot-table variants.
- Item-base discriminator normalization uses one set-based update per requested
  ID set instead of one update per ID.
- Reward facts reuse the resolution session's preloaded source-entity catalog.
