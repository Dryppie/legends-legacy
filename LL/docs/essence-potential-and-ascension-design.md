# Essence Potential and Ascension Design

## Purpose

This document defines a scalable Essence progression model that keeps the current Ascension system, does not add Attunement, and introduces one new system whose only purpose is to raise an Essence's stat ceiling.

The desired player-facing model is:

```text
Essence Level = more stats
Essence Potential = higher stat cap
Essence Ascension = improved abilities
Essence Evolution = ability mutation or identity change
```

This keeps the existing Ascension fantasy intact while still allowing early-region Essences to remain viable through the full game.

## Design Goals

- Preserve the current Essence absorption, duplicate handling, Soul Dust, Ascension, Evolution, and loadout concepts.
- Avoid adding Attunement as a second regional tier term.
- Make Essence scaling understandable: level for stats, ascend for abilities.
- Let low-region Essences remain viable if invested in.
- Let high-region Essences start at a higher floor without having a higher final ceiling.
- Keep dungeon rewards relevant as the source of stat-cap progression materials.
- Do not require duplicate Essence drops for any progression upgrade.

## Current System Summary

The existing implementation already supports:

- Essence drops as inventory items.
- Absorbing an unbound Essence into the Soul Archive.
- Rejecting duplicate absorption.
- Dismantling duplicate Essence items into Soul Dust.
- Spending Soul Dust to grant Essence XP.
- Granting combat XP to equipped Essences.
- Equipping absorbed Essences through loadouts.
- Ascending Essences with Monster Cores.
- Evolving Essences with Ascension requirements and catalysts.

Current `PlayerEssence` progression fields:

| Field | Current meaning |
|---|---|
| `Level` | Essence level |
| `CurrentXp` | XP toward the next level |
| `AscensionTier` | Current ability/progression tier, 0-3 |
| `IsEvolved` | Whether the Essence has evolved |
| `IsFavorite` | Soul Archive UI preference |

Current Ascension also controls the level cap:

| Ascension Tier | Current Level Cap |
|---:|---:|
| 0 | 10 |
| 1 | 30 |
| 2 | 60 |
| 3 | 60 |

That level-cap responsibility is the part this design changes.

## New Core Rule

Ascension should no longer be responsible for stat caps.

Instead:

```text
Essence Potential controls the level cap.
Essence Level controls stat strength.
Ascension controls ability strength and evolution access.
```

This gives each system one clean purpose.

## Essence Potential

Essence Potential is the new stat-cap system.

Potential represents how much raw stat growth an absorbed Essence can hold. It does not change ability text, cooldowns, evolution, or special mechanics. It only raises the maximum level an Essence can reach.

Recommended display names:

- Essence Potential
- Potential Tier
- Potential Rank
- Soul Potential

Recommended choice: **Essence Potential**.

## Progression Model

Each absorbed Essence should have:

| Field | Purpose |
|---|---|
| `Level` | Current stat level |
| `CurrentXp` | XP toward next level |
| `PotentialTier` | Determines level cap |
| `AscensionTier` | Determines ability refinement |
| `IsEvolved` | Determines evolved mechanics |

Potential Tier level caps:

| Potential Tier | Level Cap |
|---:|---:|
| 1 | 10 |
| 2 | 20 |
| 3 | 30 |
| 4 | 40 |
| 5 | 50 |
| 6 | 60 |
| 7 | 70 |
| 8 | 80 |
| 9 | 90 |
| 10 | 100 |

If zero-based storage is preferred, store `PotentialTier` as 0-9 and display it as 1-10. Player-facing UI should use 1-10.

## Native Region and Starting Potential

Each Essence needs a Native Region, either on the Essence definition or in a source catalog.

Native Region affects:

- where the Essence drops;
- starting Potential Tier;
- initial level cap;
- optional upgrade cost category;
- catalog grouping.

Native Region must not permanently limit maximum power.

Starting rules:

| Essence origin | Starting Potential Tier | Starting Level Cap |
|---:|---:|---:|
| Region 1 | 1 | 10 |
| Region 2 | 2 | 20 |
| Region 3 | 3 | 30 |
| Region 10 | 10 | 100 |

Example:

```text
Wolf Essence
Native Region: 1
Potential Tier: 1
Level Cap: 10

After investment:
Potential Tier: 10
Level Cap: 100
```

```text
Ancient Dragon Essence
Native Region: 10
Potential Tier: 10
Level Cap: 100
```

Both Essences can reach the same level cap. The higher-region Essence starts closer to the ceiling, but does not exceed it.

## Essence Leveling

Essence Level remains the main stat-growth track.

Rules:

- Only absorbed Essences can gain Essence XP.
- Equipped Essences can gain combat XP.
- Soul Dust can still be spent to grant Essence XP.
- Duplicate Essence items must not grant required progression.
- Level cannot exceed the cap from Potential Tier.
- XP overflow can be preserved or capped according to existing implementation preference.

Recommended change:

```text
Current cap = GetLevelCapForPotential(essence.PotentialTier)
```

instead of:

```text
Current cap = GetLevelCap(essence.AscensionTier)
```

The existing XP curve can remain initially:

```text
XP required = 100 * 1.18^(level - 1)
```

Later, if Level 100 takes too long or too little, rebalance the curve without changing the system structure.

## Stat Scaling

Essence stats should scale from Level. Potential only permits higher levels.

Recommended formula:

```text
Base Stat Value = Essence stat profile at current Level
Final Stat Value = Base Stat Value
```

If Ascension is allowed to affect stats, keep it very small and optional. The cleaner design is:

```text
Ascension does not increase raw stats.
Ascension improves abilities.
```

This keeps the UI promise honest:

```text
Level up for stats.
Ascend for abilities.
```

### Current Attribute Bonus Compatibility

The current Essence definitions have fixed `AttributeBonuses` with base values.

Short-term compatible scaling:

```text
Scaled Attribute = BaseValue * level scaling multiplier
```

Longer-term preferred scaling:

- add Essence stat profiles;
- distribute a level-based stat budget across weighted attributes;
- let Level 100 represent Region 10/endgame stat budget;
- keep Native Region out of final stat calculation.

## Ascension

Ascension remains the ability-improvement system.

It should keep the current 0-3 shape:

| Ascension Tier | Purpose |
|---:|---|
| 0 | Base Essence abilities |
| 1 | First ability refinement |
| 2 | Major ability refinement and common evolution gate |
| 3 | Final ability refinement or capstone gate |

Ascension should affect:

- active ability values;
- passive ability values;
- cooldown reductions;
- effect durations where safe;
- summon scaling;
- status strength where safe;
- evolution requirements;
- achievements.

Ascension should not affect:

- level cap;
- required duplicate Essence copies;
- Native Region ceiling;
- whether an Essence can eventually reach Level 100.

## Ascension Requirements

Ascension should still require investment, but the requirements should reference Level and Potential rather than granting the cap itself.

Recommended requirements:

| Ascension | Requirement | Material |
|---|---|---|
| 0 -> 1 | Level 10, Potential 1+ | Lesser Monster Core |
| 1 -> 2 | Level 30, Potential 3+ | Greater Monster Core |
| 2 -> 3 | Level 60, Potential 6+ | Primal Monster Core |

This preserves the current material identity while separating stat cap from ability improvement.

Important detail:

Requiring Level 30 for Ascension 2 means the Essence must already have Potential Tier 3. This makes Potential the stat-cap gate, and Ascension the reward for building into that cap.

## Evolution

Evolution can remain mostly unchanged.

Current evolution requirements can continue to use Ascension Tier:

```text
Requires Ascension Tier 1
Requires Ascension Tier 2
Requires Ascension Tier 3
```

If evolved abilities become too strong too early, add Level or Potential requirements:

```text
Requires Ascension Tier 2 and Potential Tier 5
```

Recommended rule:

Evolution should require Ascension because it changes what the Essence does, not because it changes raw stats.

## Dungeon Reward Design

Dungeons should support both progression tracks, but with different reward identities.

```text
Potential materials raise stat caps.
Monster Cores support Ascension.
```

This means the old dungeon role does not disappear. It becomes more specific.

### Potential Materials

Region dungeons should drop Potential materials used to raise the stat cap.

Recommended material names:

| Source Region | Material | Used For |
|---:|---|---|
| 1 | Region 1 Potential Core | Potential 1 -> 2 |
| 2 | Region 2 Potential Core | Potential 2 -> 3 |
| 3 | Region 3 Potential Core | Potential 3 -> 4 |
| 4 | Region 4 Potential Core | Potential 4 -> 5 |
| 5 | Region 5 Potential Core | Potential 5 -> 6 |
| 6 | Region 6 Potential Core | Potential 6 -> 7 |
| 7 | Region 7 Potential Core | Potential 7 -> 8 |
| 8 | Region 8 Potential Core | Potential 8 -> 9 |
| 9 | Region 9 Potential Core | Potential 9 -> 10 |

Region 10 does not need a Potential 10 -> 11 material.

### Potential Upgrade Costs

Initial cost table:

| Upgrade | Material | Quantity |
|---|---|---:|
| 1 -> 2 | Region 1 Potential Core | 3 |
| 2 -> 3 | Region 2 Potential Core | 4 |
| 3 -> 4 | Region 3 Potential Core | 5 |
| 4 -> 5 | Region 4 Potential Core | 6 |
| 5 -> 6 | Region 5 Potential Core | 8 |
| 6 -> 7 | Region 6 Potential Core | 10 |
| 7 -> 8 | Region 7 Potential Core | 12 |
| 8 -> 9 | Region 8 Potential Core | 15 |
| 9 -> 10 | Region 9 Potential Core | 20 |

These numbers should live in a centralized cost table or seed/config data.

### Monster Cores

Monster Cores stay as Ascension materials.

| Material | Used For |
|---|---|
| Lesser Monster Core | Ascension 0 -> 1 |
| Greater Monster Core | Ascension 1 -> 2 |
| Primal Monster Core | Ascension 2 -> 3 |

Because Ascension no longer grants level caps, Monster Core costs may need to be reduced or made less central than before.

## Dungeon Grade and Region

The current dungeon system has `Tier` and `Grade`. For this design, add or infer dungeon Region.

Recommended meanings:

| Dungeon property | Meaning |
|---|---|
| Region | Which Potential Core drops |
| Grade | Difficulty and reward quantity |
| Tier | Local dungeon variant or progression step |

Example:

| Dungeon | Region | Grade | Main Potential drop |
|---|---:|---|---|
| Goblin Mines I | 1 | Grade I | Region 1 Potential Core, low quantity |
| Goblin Mines II | 1 | Grade II | Region 1 Potential Core, medium quantity |
| Goblin Mines III | 1 | Grade III | Region 1 Potential Core, high quantity |

All Region 1 dungeon grades prepare Essences for Potential 2. Higher grades simply do it faster and with better bonus rewards.

## Potential Upgrade Flow

Input:

```text
CharacterId
PlayerEssenceId
```

Validation:

1. Essence must be absorbed.
2. Potential Tier must be less than 10.
3. Character must have access to the next Potential Tier.
4. Required regional Potential Core must exist in inventory.
5. Required quantity must be available.
6. Duplicate Essence items must not be consumed.

Result:

1. Consume required Potential Cores.
2. Increase Potential Tier by 1.
3. Raise level cap immediately.
4. Preserve current Level and XP.
5. Return updated Essence progression.

Failure examples:

- "Essence is already at maximum Potential."
- "You have not reached the required region."
- "Missing Region 3 Potential Cores."

## Progression Gate

Players should not raise Potential beyond their reached content.

Recommended rule:

```text
Maximum Potential Tier = highest unlocked region
```

Alternative:

```text
Maximum Potential Tier = highest cleared region + 1
```

For the current codebase, this likely needs a dedicated progression helper because region unlock state is not currently exposed as one obvious method.

Recommended service shape:

```csharp
int GetMaximumEssencePotentialTier(Guid characterId);
```

## API Shape

Keep existing endpoints where possible.

Recommended additions:

```text
POST /api/v1/essence/{playerEssenceId}/potential/upgrade
```

Existing endpoints remain:

```text
POST /api/v1/essence/items/{inventoryItemId}/absorb
POST /api/v1/essence/items/{inventoryItemId}/dismantle
POST /api/v1/essence/{playerEssenceId}/spend-dust
POST /api/v1/essence/{playerEssenceId}/ascend
POST /api/v1/essence/{playerEssenceId}/evolve
GET  /api/v1/essence/archive
```

Soul Archive DTO should include:

| Field | Purpose |
|---|---|
| `level` | current stat level |
| `currentXp` | current XP toward next level |
| `xpRequiredForNextLevel` | next level threshold |
| `potentialTier` | current stat cap tier |
| `potentialLevelCap` | current level cap |
| `ascensionTier` | ability improvement tier |
| `canUpgradePotential` | whether stat cap can be raised |
| `canAscend` | whether ability tier can be raised |
| `potentialUpgradeInfo` | material and progression requirements |
| `ascendInfo` | Monster Core and ability-upgrade requirements |

## Frontend UX

The Soul Archive should visually separate the two upgrades.

Recommended labels:

```text
Level
Potential
Ascension
Evolution
```

Essence card example:

```text
Goblin Ambusher Essence
Level 20 / 20
Potential II
Ascension I

Upgrade Potential
Requires: 4 Region 2 Potential Cores
Effect: Raises level cap to 30

Ascend
Requires: Level 30, Potential III, 12 Greater Monster Cores
Effect: Improves ability values and cooldowns
```

UX rule:

Never describe Ascension as raising the level cap once Potential exists.

## Migration Plan

### Phase 1: Add Potential fields

Add to `PlayerEssence`:

```text
PotentialTier
NativeRegion
```

Recommended defaults for existing data:

| Existing state | Potential Tier |
|---|---:|
| Level 1-10 | 1 |
| Level 11-20 | 2 |
| Level 21-30 | 3 |
| Level 31-40 | 4 |
| Level 41-50 | 5 |
| Level 51-60 | 6 |

If existing Level is capped at 60, no existing Essence needs to start above Potential 6.

### Phase 2: Move level caps to Potential

Change progression service behavior:

```text
GetLevelCap(ascensionTier)
```

becomes:

```text
GetLevelCapForPotential(potentialTier)
```

Any old Ascension-based cap method should either be removed or kept only as a compatibility wrapper during migration.

### Phase 3: Rework Ascension requirements

Update Ascension validation:

- remove "Ascension raises cap" logic;
- require Level and Potential;
- continue consuming Monster Cores;
- continue recording Ascension achievements;
- continue enabling Evolution gates.

### Phase 4: Add Potential upgrades

Add:

- Potential cost provider;
- Potential upgrade service method;
- command/API endpoint;
- DTO fields;
- frontend controls.

### Phase 5: Update dungeon rewards

Add Region Potential Cores to item seed data.

Update dungeon completion rewards:

- main regional progression reward: Potential Cores;
- secondary ability progression reward: Monster Cores;
- grade affects quantity and bonus chance;
- region determines which Potential Core drops.

### Phase 6: Rebalance stats

Initially, keep current stat scaling and extend the level cap to 100.

Then evaluate:

- Level 1-100 stat budget curve;
- whether existing `AttributeBonuses` scale too aggressively;
- whether stat profiles are needed;
- whether Essence stats compete properly with gear.

## Tests Required

Add or update tests for:

- Potential Tier determines level cap.
- Essence cannot level beyond Potential cap.
- Ascension does not raise level cap.
- Ascension improves ability scaling.
- Potential upgrade consumes regional Potential Cores.
- Potential upgrade does not consume duplicate Essence items.
- Region 1 Potential Core upgrades Potential 1 -> 2.
- Region 9 Potential Core upgrades Potential 9 -> 10.
- Dungeons award Potential Cores based on region.
- Dungeon grade affects Potential Core quantity, not material region.
- Existing Monster Cores still support Ascension.
- Evolution requirements still respect Ascension Tier.

## Non-Goals

Do not include these in the first implementation:

- Attunement.
- Duplicate Essence upgrade requirements.
- Trading.
- Full economy rebalance.
- Region 10 post-cap progression.
- Reauthoring every Essence ability.
- Full stat-profile migration if current attribute bonus scaling can bridge the first version.

## Acceptance Criteria

The design is complete when:

- Essence Level means stat growth.
- Essence Potential means stat cap.
- Essence Ascension means ability improvement.
- Ascension no longer raises level caps.
- Dungeons drop Potential materials for stat-cap progression.
- Monster Cores remain useful for Ascension.
- Duplicate Essence drops are optional economy items only.
- Region 1 Essences can eventually reach Level 100.
- Region 10 Essences start closer to Level 100 but do not exceed it.
- The Soul Archive clearly separates Potential and Ascension actions.

## Open Questions

- Should Potential upgrades require the Essence to be at its current level cap?
- Should Soul Dust remain the only direct XP material?
- Should Monster Cores become rarer once they no longer unlock level caps?
- Should Potential Tier be shown as Roman numerals or plain numbers?
- Should Native Region live directly on Essence definitions or in the Essence catalog/source map?

Recommended answers:

- Require current level cap before Potential upgrade for clearer pacing.
- Keep Soul Dust as the main XP material.
- Make Monster Cores less common than Potential Cores because Ascension is now ability-focused.
- Use plain numbers for Potential Tier to match level caps.
- Store Native Region on Essence definitions long-term; use catalog/source inference only as a migration bridge.
