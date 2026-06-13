# Combat Essence Ability Integration Plan

## Current architecture

- Combat entry points are the character action and dungeon orchestration flows under `Services.LL.Combat.*`.
- Combat resolution ultimately runs through `ICombatContext.InstantiateAndRunCombat`, which ticks `CombatEntity` instances and auto-uses active `CombatAbilityInstance`s when cooldowns are ready.
- Passive abilities already use the trigger engine. `OnCombatStart`, attack, damage, kill, dodge, and ability-use events are published to the combat event bus and matched by ability triggers.
- `CombatSetupService` builds `CombatEntity` objects, applies Essence attribute modifiers, appends Essence combat abilities, and then calculates combat attributes.
- Reusable authored ability definitions live in `Domain.Models.AbilityDefinitions`.
- Runtime combat ability definitions live in `Domain.Models.Combat.Abilities`.
- Essence definitions already own exactly one active ability id/definition and one passive ability id/definition, plus attribute bonuses, tags, progression, ascension, and evolution metadata.
- Player Essence ownership is stored in `PlayerEssence`; equipped Essences are represented by active `EssenceLoadout`/`EssenceLoadoutSlot` rows. Dungeon snapshots persist equipped Essence state as `EquippedEssenceSnapshot`.
- `EssenceSystemService` currently acts as Essence service, bonus provider, ability provider, and resonance service. It maps Essence-authored ability definitions into runtime combat abilities with level/ascension/evolution scaling.

## What will change

- Add an explicit combat loadout resolver so combat receives a single resolved Essence combat loadout rather than separately asking for ability and bonus details.
- The resolved loadout will contain equipped Essences, active abilities, passive abilities, attribute bonuses, and tags.
- `CombatSetupService` will consume this resolver and apply the resulting loadout to each player combat entity before attribute calculation.
- Player combat entities will receive Essence tags from equipped Essences, matching the existing monster-tag behavior.
- Existing reusable ability definitions and the existing combat trigger/effect system will remain in place.

## Files/classes to modify

- `Application.Interfaces.Services.LL.Essences`
  - Add `IEssenceCombatLoadoutResolver`.
  - Add resolved loadout model records.
- `Services.LL.Essences.EssenceSystemService`
  - Implement `IEssenceCombatLoadoutResolver`.
  - Reuse existing Essence ability and bonus mapping logic.
- `Services.LL.Combat.CombatSetupService`
  - Replace direct ability/bonus provider usage with resolved Essence combat loadouts.
  - Apply resolved Essence tags to combat entities.
- `Services.LL.DependencyInjection`
  - Register the resolver interface.
- `EssenceSystem.Tests`
  - Add/update focused tests for resolved loadout behavior and combat setup integration.

## New abstractions

- `EssenceCombatLoadout`
  - Equipped Essences, active abilities, passive abilities, attribute modifiers, and tags.
- `ResolvedCombatAbility`
  - Ability definition id, source player Essence id, source Essence definition id, ability kind, Essence level, tags, cooldown, and runtime combat ability.
- `IEssenceCombatLoadoutResolver`
  - Resolves the active loadout for a character id or already-materialized equipped Essence snapshots.

## Risks

- Combat ability definitions are cloned at runtime; metadata must be carried outside the runtime ability unless the combat model is extended later.
- Some test code appears stale around `GrantCombatXpToEquippedEssencesAsync` vs `GrantCombatXpToAttunedEssencesAsync`; verification may expose existing compile issues.
- Ability effect mapping still supports only the authored effect/trigger primitives currently implemented in `EssenceSystemService`.
- Existing standalone `Entity.Abilities` still feed basic/legacy abilities into combat; this change makes Essences the combat source of Essence abilities, but it does not delete reusable combat effect infrastructure.

## Test plan

- Prove an equipped Essence contributes one active and one passive ability to the resolved combat loadout.
- Prove Essence attribute bonuses are included and scale from Essence level.
- Prove unequipped Essences do not contribute abilities, bonuses, or tags.
- Prove combat setup applies active/passive Essence abilities, Essence attribute bonuses, and Essence tags to player combat entities.
- Keep existing Essence, ability definition, and combat tests passing.

## Assumptions

- The active Essence loadout is the single source of combat Essence powers.
- Ability definitions remain reusable authored data and are referenced by Essence definitions.
- Essence level/ascension/evolution scaling remains centralized in `EssenceSystemService` for now.
- No EF schema change is required because Essence definitions and ability references are JSON-authored, while player ownership/equipping tables already exist.
