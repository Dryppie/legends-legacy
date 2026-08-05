# Ability System

> For current, repository-audited combat terminology and behavior, use the [Combat Lexicon and Catalogue](combat-lexicon/README.md). New abilities should use the typed examples in [Standard Condition Ability Authoring](combat-lexicon/ability-authoring.md). This document contains earlier architecture terminology; known differences are recorded in the lexicon's [catalogue audit](combat-lexicon/catalogue-audit.md).

## Overview

LegendsLegacy uses a data-driven, Essence-owned ability system.

Abilities are authored as strongly typed JSON definitions in:

`LL/src/API/API.LL/Data/essences.json`

The engine compiles those definitions into runtime combat primitives:

- `AbilityDefinition` is authored data.
- `EssenceDefinition` links one active and one passive ability.
- `EssenceSystemService` resolves equipped Essences into scaled combat abilities.
- `CombatAbilityDefinition` and `CombatAbilityInstance` are runtime state.
- `TriggerEngine`, `CombatEffectManager`, `CombatInteractionManager`, and `TargetingManager` execute effects during combat.

Do not add one C# class per ability. Add data that composes existing triggers, conditions, targeting rules, scaling formulas, and effect primitives.

## How Essences Grant Abilities

Each Essence definition owns:

- `activeAbilityId`
- `passiveAbilityId`
- `attributeBonuses`
- `tags`
- progression, drop, ascension, and evolution data

When combat starts, `CombatSetupService` asks `IEssenceCombatLoadoutResolver` for the participant's Essence combat loadout. For players, that resolver reads the active Essence loadout slots. For creatures, combat setup maps the creature's source monster id to its Essence definition so the creature can use the same Essence abilities it drops.

Only equipped Essences grant combat abilities. Unabsorbed, unequipped, and inactive Essences do not.

## Adding An Active Ability

Add an entry to `abilityDefinitions`:

```json
{
  "id": "ability.essence.fire_ant.firefall",
  "kind": "Active",
  "name": "Firefall",
  "description": "Deals fire damage to all enemies and applies Burn.",
  "cooldownSeconds": 20,
  "targeting": "AllEnemies",
  "tags": ["Effect.Ability", "Element.Fire", "Status.Burn"],
  "effects": [
    {
      "id": "effect.damage.fire",
      "type": "Damage",
      "target": "AllEnemies",
      "scaling": { "baseValue": 80, "perLevel": 7, "perAscensionTier": 20 }
    },
    {
      "id": "effect.condition.burn",
      "operation": "ApplyCondition",
      "target": "AllEnemies",
      "condition": "Burn",
      "baseValue": 1
    }
  ]
}
```

Then reference it from exactly one Essence:

```json
"activeAbilityId": "ability.essence.fire_ant.firefall"
```

## Adding A Passive Ability

Passive abilities are authored the same way but use `kind: "Passive"` and usually include `triggers`.

```json
{
  "id": "ability.essence.fire_ant.hot_aura",
  "kind": "Passive",
  "name": "Hot Aura",
  "description": "When hit, has a chance to burn the attacker.",
  "cooldownSeconds": 8,
  "targeting": "Attacker",
  "tags": ["Trigger.OnTakeDamage", "Status.Burn"],
  "triggers": [
    { "type": "Trigger.OnTakeDamage", "internalCooldownSeconds": 8 }
  ],
  "conditions": [{ "type": "ChanceRoll", "value": 25 }],
  "effects": [
    {
      "id": "effect.condition.burn",
      "operation": "ApplyCondition",
      "target": "Attacker",
      "condition": "Burn",
      "baseValue": 1
    }
  ]
}
```

Then reference it from the Essence:

```json
"passiveAbilityId": "ability.essence.fire_ant.hot_aura"
```

## Triggers

Supported authored trigger constants live in `AbilityTriggerType`.

Common triggers:

- `Trigger.OnCombatStart`
- `Trigger.OnHit`
- `Trigger.OnTakeDamage`
- `Trigger.OnKill`
- `Trigger.OnDodge`
- `Trigger.OnAbilityUse`
- `Trigger.OnBasicAttack`
- `Trigger.OnStatusApplied`
- `Trigger.OnStatusExpired`
- `Trigger.OnInterval`
- `Trigger.OnStatusRemoved`
- `Trigger.OnStatusCleansed`
- `Trigger.OnStatusDispelled`

The runtime maps these to `TriggerEvent` values. Event dispatch has a maximum depth guard so recursive triggered effects fail clearly instead of looping forever.

## Conditions

Supported condition constants live in `AbilityConditionType`.

Common conditions:

- `Always`
- `SourceHasTag`
- `TargetHasTag`
- `TargetHasStatus`
- `SourceHasStatus`
- `TargetHasStatusStacksAtLeast`
- `TargetHealthBelowPercent`
- `SourceHealthBelowPercent`
- `SourceHealthAbovePercent`
- `IsSpecies`
- `RandomChance`
- `ChanceRoll`
- `SourceIsSummon`

Multiple conditions on an ability/effect are combined as AND.

`RandomChance` and `ChanceRoll` are converted into an effect chance value. Use a value from `0` to `100`.

## Effects

Supported effect constants live in `AbilityEffectType`.

Current reusable primitives:

- `Damage`
- `Heal`
- `ApplyCondition`
- `ApplyStatus`
- `RemoveStatus`
- `Cleanse`
- `GrantBarrier`
- `ModifyAttribute`
- `RestoreResource`
- `Summon`
- `Taunt`
- `ReflectDamage`
- `AbsorbDamage`
- `TriggerSecondaryEffect`

An ability can contain multiple ordered effects. Each effect can override target and conditions.

## Scaling

Every numeric effect uses `AbilityScalingFormula`:

```json
"scaling": {
  "baseValue": 100,
  "perLevel": 8,
  "perAscensionTier": 25,
  "attributeScaling": [
    { "attribute": "Power", "coefficient": 0.25 }
  ]
}
```

The core numeric formula is:

`baseValue + perLevel * (essenceLevel - 1) + perAscensionTier * ascensionTier`

Attribute scaling is passed to runtime actions where the action supports it.

Essence attribute bonuses use `EssenceAttributeBonusDefinition` and scale from the same Essence level/ascension context.

## Targeting

Supported authored target selectors live in `AbilityTargetSelector`.

Current selectors:

- `Self`
- `CurrentTarget`
- `RandomEnemy`
- `RandomAlly`
- `AllEnemies`
- `AllAllies`
- `TwoEnemies`
- `TwoAllies`
- `LowestHealthEnemy`
- `LowestHealthAlly`
- `HighestHealthEnemy`
- `HighestMaxHealthAlly`
- `Attacker`
- `DamageSource`
- `AbilityUser`
- `SummonedAllies`
- `NonSummonedAllies`

Targeting is converted into runtime `CombatTargeting` values before execution.

## Status Effects

Status definitions are loaded from:

`LL/src/API/API.LL/Data/Statuses/statuses.json`

Runtime statuses use:

- `StatusDefinition`
- `StatusInstance`
- `JsonStatusService`
- status-related effect actions and conditions

`ApplyStatus` is reserved for bespoke mechanics without a standard-condition equivalent. Shared
effects such as Burn, Bleed, Poison, Stun, and the other Combat Lexicon conditions use
`ApplyCondition`.

## Evolution

Essence evolution can add tags and apply ability modifiers. Current supported modifier operations include:

- `AddMultiplier`
- `AddFlat`
- `AddEffect` when an effect payload is supplied

Do not use evolution as a per-ability C# escape hatch. Evolution should modify authored definition data before it is compiled into runtime combat effects.

## Validation

`EssenceDefinitionValidator` runs when JSON definitions are loaded.

It catches:

- duplicate Essence ids
- missing Essence ids/source monster ids
- missing active/passive ability references
- active/passive kind mismatches
- missing ability id/name/kind
- active abilities without cooldown, target selector, or effects
- unknown target selectors, triggers, conditions, effect types, tags, and attributes
- invalid chance values
- status-stack conditions without status and stack values
- negative scaling values

Add validation before adding new authoring vocabulary.

## Testing A New Ability

Add or update tests in `LL/tests/EssenceSystem.Tests`.

Useful coverage:

- the Essence grants the active ability when equipped
- the Essence grants the passive ability when equipped
- unequipped Essence does not grant the ability
- level/ascension changes the scaled value
- target selector maps to the expected `CombatTargeting`
- conditions map to the expected runtime condition
- invalid definitions fail validation clearly
- triggered passives fire only on the intended combat event

Run:

```powershell
dotnet test "LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj" --no-restore
```

For broader backend confidence:

```powershell
dotnet build "LL\src\Infrastructure\Service\Services.LL\Services.LL.csproj"
dotnet build "LL\LegendsLegacy.sln"
dotnet test
```

The full solution build may fail if API output files are locked by a running local API process. Stop the process and rerun.

## Current Limitations

These are known follow-ups:

- explicit resource costs are not yet enforced by the active ability executor
- passive internal cooldown uses runtime ability cooldown rather than separate trigger-level state
- once-per-combat semantics need a dedicated usage primitive in authored definitions
- deterministic target RNG needs an injectable random source
- status stack/refresh/expiration/dispel rules need a richer authored model
- reflection/thorns metadata can be expanded beyond the current event dispatch depth guard
