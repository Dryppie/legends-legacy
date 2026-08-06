# Attack Speed Implementation Analysis

## Goal

Properly add Attack Speed to combat so:

- All basic attacks, regardless of weapon, default to one attack every 3 seconds.
- In engine terms, the baseline cadence is 30 ticks.
- Attack Speed can be buffed or debuffed on entities during battle.
- The implementation uses the existing combat attribute and modifier pipelines instead of weapon-specific timing.

## Current State

Basic attacks already default to 30 ticks in `FastCombatEngineOptions`:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- `BasicAttackIntervalTicks = 30`

The current `FastCombatEngine` stores a fixed integer timer per runtime combatant:

- `_basicAttackTimers[combatant] = GetBasicAttackIntervalTicks()`
- each eligible tick decrements the timer by `1`
- when the timer reaches `0`, the entity performs `Basic Attack`
- the timer then resets to `BasicAttackIntervalTicks`

That means the actual live engine already ignores weapon type and attacks every 30 ticks by default.

There is an old attack speed design comment in `CombatEntity`:

- `LL/src/Core/Domain/Models/Combat/CombatEntity.cs`
- `NextBasicAttackIn = 300`
- comment describes decrementing by `BaseAttackSpeed`

That path is not what the current engine uses. The current battle runtime is built around `RuntimeCombatant` in `AbilityRuntime.cs`, and `FastCombatEngine` drives combat from those runtime combatants.

## Important Finding

Attack Speed is not currently a real combat attribute.

`AttributeType` has no `AttackSpeed`:

- `LL/src/Core/Domain/Models/Attributes/AttributeType.cs`

`EquipmentBase` does have an `AttackSpeed` property:

- `LL/src/Core/Domain/Models/Items/Equipments/EquipmentBase.cs`

But equipment combat aggregation uses `AttributeModifiers`, not `EquipmentBase.AttackSpeed` directly:

- `EquipmentInstance.AttributeModifiers`
- `AttributeCalculator.CalculateBaseCombatAttributes`

So the `attackSpeed` values in `items.json` appear to be mostly dead combat data unless another unrelated display path reads them.

## Existing Pipelines That Can Support Attack Speed

Attack Speed can fit cleanly into the existing systems because most of the necessary plumbing already exists.

Pre-combat modifiers already flow through:

- equipment attribute modifiers
- essence loadout attribute modifiers
- dungeon boon modifiers
- dungeon enemy and boss modifiers
- creature base attributes
- character base attributes

In-combat buffs and debuffs already flow through:

- `AbilityEffectOperation.ModifyAttribute`
- `RuntimeCombatant.AdjustAttribute`
- timed `RuntimeEffect` expiration that reverses the modifier
- status effects that can apply timed attribute changes

So the main missing piece is making Attack Speed a real `AttributeType` and teaching `FastCombatEngine` how to consume it for basic attacks.

## Recommended Stat Semantics

Use Attack Speed as percent haste or slow:

- `0` means baseline, one attack every 30 ticks.
- `+100` means double rate, about one attack every 15 ticks.
- `+50` means 1.5x rate, about one attack every 20 ticks.
- `-50` means half rate, about one attack every 60 ticks.

This matches the current modifier model, where many secondary stats are represented as percentages and modified with flat/additive/multiplicative modifier types.

## Recommended Engine Model

Replace the fixed countdown timer with attack progress accumulation.

Conceptually:

```csharp
progress += Clamp(1 + AttackSpeed / 100f, minRate, maxRate);

if (progress >= 30)
{
    progress -= 30;
    PerformBasicAttack();
}
```

This is better than recalculating a fixed interval only after every attack because mid-fight buffs and debuffs affect the current swing immediately.

Example:

- an entity is halfway to its next basic attack
- it receives `+100 AttackSpeed`
- its progress now fills twice as fast from that point forward

A reset-based timer would not feel as responsive and would make temporary short buffs harder to reason about.

## Suggested Safety Clamps

Use a bounded rate multiplier:

- minimum rate: `0.25x`
- maximum rate: `4x`

That would mean:

- extremely heavy slows cannot permanently freeze basic attacks
- extreme haste cannot generate unreasonable event spam
- authored content has room to be expressive without breaking the simulator

Also allow at most one basic attack per combatant per tick. Even if progress exceeds the threshold by more than 30, carry the extra progress forward instead of resolving multiple attacks in one tick.

## Stun And Action Blocking

The current engine skips active abilities and basic attack ticking while `IsActionBlocked` returns true.

Attack progress should follow the same rule:

- stunned entities should not gain basic attack progress
- attack speed buffs should not bypass stun
- this preserves the existing behavior where control effects pause action output

## Backend Changes Required

### Domain Attributes

Add `AttackSpeed` to `AttributeType`.

Important: append it at the end of the enum. Do not insert it in the middle, because enum values are persisted as integers in tables such as `EntityAttributes` and `EntityAttributeSnapshot`.

Update:

- `LL/src/Core/Domain/Models/Attributes/AttributeType.cs`
- `LL/src/Core/Domain/Models/Attributes/AttributeCatalog.cs`
- `LL/src/Core/Domain/Helpers/EntityBaseAttributeHelper.cs`
- `LL/src/Core/Domain/Models/Entities/Creatures/CreatureBaseStats.cs`

Baseline value should be `0`.

### Combat Engine

Update:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`

Replace:

- `_basicAttackTimers`
- fixed decrement by `1`
- fixed reset to `BasicAttackIntervalTicks`

With:

- `_basicAttackProgress`
- progress increment based on `actor.GetAttribute(AttributeType.AttackSpeed)`
- threshold of `30` baseline ticks
- safe rate clamp

Summons should initialize with the same baseline progress behavior as regular combatants.

### Runtime Attribute Snapshots

`CombatEngineExecutor.CreateAttributeSnapshot` already copies existing combat attributes and only explicitly ensures a few required attributes. Once `AttackSpeed` exists and defaults to `0`, missing values are fine because `RuntimeCombatant.GetAttribute` returns `0` when absent.

It is still reasonable to explicitly `TryAdd(AttributeType.AttackSpeed, 0)` for clarity.

### Equipment And Crafting

Do not let `EquipmentBase.AttackSpeed` affect the basic attack cadence directly if the design goal is that all weapons attack every 3 seconds.

Options:

1. Leave `EquipmentBase.AttackSpeed` as legacy/display-only data and ignore it in combat.
2. Remove or hide it in future cleanup.
3. Convert meaningful attack speed equipment into normal `AttributeModifiers` using `AttributeType.AttackSpeed`.

If crafted gear should be able to roll Attack Speed, update:

- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentAttributeRules.cs`

Suggested roll rule:

```csharp
[AttributeType.AttackSpeed] = Percent(min: 1, max: 8)
```

Tune the range after combat balance testing.

### Creature Scaling

There is already an unused `MonsterScalingConstants.AttackSpeedPerTier` constant:

- `LL/src/Core/Domain/Helpers/Constants/MonsterScalingConstants.cs`

Do not automatically scale monster Attack Speed unless that is intended for balance. The user requirement says all basic attacks should default to 30 ticks, so creature baseline should remain `0`.

If later desired, creature archetypes or explicit stat overrides can add Attack Speed through the normal attribute system.

## Content Changes

Once `AttackSpeed` exists as an attribute, content can buff or debuff it through existing ability/status JSON.

Example timed buff:

```json
{
  "id": "effect.haste",
  "operation": "ModifyAttribute",
  "target": "Self",
  "attribute": "AttackSpeed",
  "baseValue": 50,
  "durationTicks": 50
}
```

Example slow:

```json
{
  "id": "effect.slow",
  "operation": "ModifyAttribute",
  "target": "CurrentTarget",
  "attribute": "AttackSpeed",
  "baseValue": -30,
  "durationTicks": 40
}
```

The existing `AbilityCatalog` validation should already accept this once the enum value exists, because `ModifyAttribute` requires an attribute and parses `AttributeType`.

## API And Frontend Changes

Angular duplicates the backend enum:

- `LL/src/Presentation/ll/src/app/shared/models/enums/attributeType.ts`

Add `AttackSpeed`.

Update formatting and grouping:

- `LL/src/Presentation/ll/src/app/shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe.ts`
- `LL/src/Presentation/ll/src/app/shared/pipes/attributes/group-attributes-by-category/group-attributes-by-category.pipe.ts`
- `LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display.ts`

Recommended display:

- label: `Attack Speed`
- value format: percentage
- category: `Offensive` or `Utility`

`EquipmentBaseDto` currently does not expose `AttackSpeed`, even though the Angular `Equipment` model expects `attackSpeed`.

If the legacy `EquipmentBase.AttackSpeed` remains visible in equipment displays, update:

- `LL/src/Core/Application/UseCases/Items/Dtos/EquipmentBaseDto.cs`

But if the long-term direction is to retire `EquipmentBase.AttackSpeed`, prefer showing Attack Speed only via attribute modifiers.

## Tests To Add

Add focused tests in:

- `LL/tests/EssenceSystem.Tests/AbilitySystemTests.cs`

Recommended coverage:

1. Baseline Attack Speed `0` still attacks every 30 ticks.
2. Attack Speed is the only character attribute that affects basic attack cadence.
3. `+100 AttackSpeed` doubles basic attack frequency.
4. `-50 AttackSpeed` halves basic attack frequency.
5. Timed `ModifyAttribute` buff changes attack progress during combat.
6. Timed `ModifyAttribute` debuff slows attack progress during combat.
7. Stunned entities do not gain attack progress while action-blocked.
8. Attack Speed is clamped and does not generate multiple basic attacks in one tick.

Existing useful anchor:

- `Engine_uses_fixed_basic_attack_cadence_regardless_of_precision`

That test should remain, but its name may become something like:

- `Engine_uses_attack_speed_not_precision_for_basic_attack_cadence`

## Migration And Deployment Implications

Likely no schema migration is required solely for adding `AttributeType.AttackSpeed`, because `AttributeType` is persisted as an integer enum and the relevant tables already exist.

However:

- append the enum value at the end to avoid remapping existing integer values
- consider a data backfill only if explicit `EntityAttribute` rows are required for all characters
- otherwise missing `AttackSpeed` can safely mean `0`

No external deployment or database apply step should be done from this repository.

## Verification Performed During Analysis

Baseline test command:

```powershell
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

Result:

- Passed: 304
- Failed: 0
- Skipped: 0

Existing nullable/compiler warnings were present before any Attack Speed changes.

## Implementation Sequence

1. Add `AttributeType.AttackSpeed` to backend and frontend enum/catalog/display helpers.
2. Add default baseline values to character, creature, simulator, and diagnostic attribute setup.
3. Refactor `FastCombatEngine` from fixed timers to progress accumulation.
4. Add tests for baseline, haste, slow, timed modifier, stun interaction, and clamping.
5. Decide whether legacy `EquipmentBase.AttackSpeed` should be hidden, removed later, or converted into normal attribute modifiers.
6. Add authored JSON content for haste/slow effects only after the engine behavior is covered by tests.

## Design Decision Summary

Attack Speed should be an entity combat attribute, not a weapon property.

The baseline should remain 30 ticks for every entity and every weapon. Buffs and debuffs should modify how quickly entities accumulate basic attack progress. This gives temporary effects immediate, intuitive behavior while staying inside the existing attribute, modifier, status, dungeon, and essence systems.
