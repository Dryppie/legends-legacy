# New Essence Ability System Plan

## Current Architecture Summary

LegendsLegacy currently has a combat runtime under `LL/src/Infrastructure/Service/Services.LL/Combat` and domain combat primitives under `LL/src/Core/Domain/Models/Combat`.

Combat entry points and flow:

- `CombatService` starts idle combat actions and delegates orchestration/resolution.
- `CombatOrchestrationCoordinator` routes combat modes to idle or dungeon orchestrators.
- `IdleCombatResolutionSession` and `DungeonCombatResolutionSession` load encounter entities, build runtime participants, run resolution, and hand results to reward processors.
- `CombatEngineExecutor` and `DefaultCombatEncounterResolver` execute combat through `ICombatContext`.
- `CombatContext` owns the simulation loop, initializes `CombatEntityManager`, publishes combat events, ticks effects, runs active abilities, basic attacks, recovery, and produces `CombatResult`.
- `TriggerEngine` subscribes to `ICombatEventBus`, evaluates ability/status triggers, selects targets, and queues effects.
- `CombatEffectManager` ticks effect instances and applies effect execution through `CombatInteractionManager`.
- `CombatStatsAggregator` derives summary stats from `CombatLogItem` events.

Combat participant models:

- `Entity` is the persisted base domain model for characters and creatures.
- `CombatEntity` is a non-persisted runtime participant with current health, attributes, equipment, tags, statuses, source monster id, and runtime `CombatAbilityInstance` objects.
- `SimpleCombatEntity`, `CombatLogItem`, `CombatResult`, `EntityStats`, and `AbilityStats` are combat output/read models.

Existing Ability System pieces:

- `AbilityDefinition` and related classes in `Domain.Models.AbilityDefinitions` are data definitions used by Essence JSON.
- `CombatAbilityDefinition`, `CombatAbilityInstance`, `Trigger`, `EffectDefinition`, `EffectInstance`, `ICondition`, `IEffectAction`, `IUsage`, durations, intervals, filters, and status definitions are generic runtime primitives.
- There is no EF-backed `PlayerAbility`, standalone ability equipment slot, or persisted direct player ability ownership model found in the current codebase.
- `Entity.Abilities` and `CombatEntity.Abilities` are `[NotMapped]` runtime lists.
- `BasicAttackLoader` still creates a runtime basic attack ability for every combatant.

Existing active/passive execution:

- Active abilities are `CombatAbilityInstance` objects with `CombatAbilityType.Active`. `CombatContext` uses them automatically when `RemainingTimeUntilUse <= 0`.
- Using an active ability publishes `TriggerEvent.OnAbilityUsed`; the active ability's mapped trigger filters by ability id.
- Passive abilities are `CombatAbilityType.Passive` and fire through `TriggerEngine` when their trigger matches a combat event.
- Passive cooldown is represented by `RemainingTimeUntilUse`.

Existing effect/modifier/status logic:

- Reusable effect actions already exist for damage, healing/resource restoration, status application/removal, cleanse, attribute modification, summon, secondary effects, and self-destruction.
- Existing status definitions are JSON-backed through `JsonStatusService` and runtime `StatusInstance`.
- Attribute modifiers are generic through `AttributeModifierBase`, `AbilityAttributeModifier`, `EssenceAttributeModifier`, `ItemAttributeModifier`, and `InstanceAttributeModifier`.
- Scaling from Essence level is currently implemented in `EssenceSystemService.MapCombatEffect` and attribute bonus helpers.

Existing Essence system:

- `essences.json` contains progression templates, data-driven ability definitions, and Essence definitions.
- `EssenceDefinition` owns exactly one active ability id, one passive ability id, attribute bonuses, tags, drop data, ascension, and evolution.
- `JsonEssenceDefinitionRepository` loads JSON, resolves ability references, and validates definitions during startup.
- `PlayerEssence` stores absorbed Essence ownership, level, current XP, ascension tier, and evolution state.
- `EssenceLoadout` and `EssenceLoadoutSlot` represent equipped Essences.
- `EssenceSystemService` handles absorption, dismantling, loadout save/activate/delete, favorite, dust spending, ascension, evolution, combat XP, drop rolls, and Essence-derived combat loadout resolution.
- `IEssenceCombatLoadoutResolver` returns active abilities, passive abilities, attribute modifiers, tags, and source Essence metadata.
- `CombatSetupService` applies Essence loadout bonuses and abilities when preparing combat entities. It also resolves monster source Essences for creatures.

EF Core model:

- Current EF persistence includes `PlayerEssences`, `EssenceLoadouts`, `EssenceLoadoutSlots`, and `MonsterResonances`.
- Ability definitions are not persisted as EF rows; they are JSON/static content loaded into strongly typed definitions.
- Runtime combat abilities, effects, statuses, and modifiers are not persisted as ability ownership.

Existing tests:

- `LL/tests/EssenceSystem.Tests` covers Essence absorption, loadouts, combat ability mapping, progression, rewards, validator behavior, creature Essence rewards, and combat loadout integration.
- No old standalone player ability ownership tests were found.

## Old Ability System Pieces To Remove

No persisted standalone ability ownership/equipping model was found. The following should remain removed/unsupported as a gameplay source of truth:

- Player-owned standalone abilities.
- Standalone ability equipment slots.
- Ability unlock progression separate from Essence progression.
- Combat queries that grant abilities from anything other than equipped Essences, source monster Essences, or the universal basic attack.
- API/UI affordances that imply standalone ability equipment as the primary combat power model.

The current `[NotMapped]` `Entity.Abilities` runtime list should be treated as legacy-compatible runtime plumbing only. It must not become a persistence or player-equipment model.

## Old Pieces Reused As Generic Primitives

The following remain valuable and should be retained:

- `CombatAbilityDefinition` and `CombatAbilityInstance` as runtime compiled ability state.
- `Trigger`, `TriggerEvent`, trigger filters, and `CombatEventBus` as event hook infrastructure.
- `EffectDefinition`, `EffectInstance`, effect actions, conditions, intervals, durations, and usages.
- `CombatInteractionManager` damage/healing application and combat event emission.
- `StatusDefinition`, `StatusInstance`, and `JsonStatusService`.
- `AttributeCalculator` and generic modifier models.
- `CombatStatsAggregator` and combat session output.
- `BasicAttackLoader` for universal basic attacks, not as player ability ownership.

## New/Strengthened Abstractions

The implementation should continue using the existing project naming and layering:

- Definition data: `AbilityDefinition`, `AbilityEffectDefinition`, `AbilityConditionDefinition`, `AbilityTriggerDefinition`, `AbilityScalingFormula`, and `EssenceDefinition`.
- Runtime state: `CombatAbilityInstance`, `CombatEntity`, `StatusInstance`, `EffectInstance`, cooldown counters, usage state, and temporary modifiers.
- Resolution/ownership: `IEssenceCombatLoadoutResolver`, `EssenceCombatLoadout`, `ResolvedCombatAbility`.
- Execution: `CombatContext`, `TriggerEngine`, `CombatEffectManager`, `CombatInteractionManager`, and `TargetingManager`.
- Validation: `IEssenceDefinitionValidator` and `EssenceDefinitionValidator`.

Near-term additions should focus on reusable vocabulary rather than ability-specific classes:

- More target selectors mapped into `CombatTargeting`.
- More condition constants and validation.
- Trigger aliases mapped into existing `TriggerEvent`.
- Event dispatch recursion/depth guard.
- Clear documentation for authoring new Essence abilities in JSON.

## Essence Ability References

Each `EssenceDefinition` references:

- `ActiveAbilityId`
- `PassiveAbilityId`

`JsonEssenceDefinitionRepository` resolves those ids from the document's `abilityDefinitions` collection into:

- `ActiveAbility`
- `PassiveAbility`

Validation requires both references and checks that the referenced definitions are the correct kind.

## Combat Ability Resolution

Combat ability ownership is Essence-centric:

- For players, `EssenceSystemService.ResolveAsync(characterId)` reads active `EssenceLoadoutSlot` rows, loads their `PlayerEssence`, resolves definitions, scales effects from Essence level and ascension, and returns runtime combat abilities.
- For snapshots, `CombatSetupService` resolves the snapshot's equipped Essence list without querying current loadouts.
- For creatures, `CombatSetupService` derives `SourceMonsterId` from the creature, looks up the matching Essence definition, and resolves a synthetic runtime `PlayerEssence` so monsters can use the same Essence ability pipeline as players.
- `CombatSetupService.PrepareEntitiesForCombat` applies modifiers, tags, and active/passive abilities before attributes are calculated.

## Data Model Changes

No database model changes are required for the current pass because:

- Ability definitions are authored as JSON content, not EF entities.
- Player ability ownership/equipping tables do not exist in the current model.
- Existing Essence persistence already models absorbed Essences and equipped Essence slots.

If future work moves ability definitions into EF, create a single fresh `BaseMigration` only after deleting the current migration set, because there is no production database.

## Migration Strategy

Current migration strategy:

- Keep existing `BaseMigration`.
- Do not add migrations unless EF entities change.
- Treat old standalone ability ownership as already absent.
- If future EF ability definition tables are introduced, regenerate one new base migration and update seed/import tooling.

## Test Plan

Current and near-term tests should prove:

- Equipped Essence grants active abilities.
- Equipped Essence grants passive abilities.
- Unequipped Essence grants no ability.
- Unabsorbed Essence grants no ability.
- Essence level and ascension scale ability effects and attribute bonuses.
- Creature source monster Essences grant abilities to creatures.
- Active ability cooldowns and ability-use events execute through the trigger engine.
- Damage, healing, barrier, status, multi-effect, conditional, and summon primitives map from data.
- Passive triggers execute on the right event and ignore the wrong event.
- Random chance is deterministic in unit tests where practical.
- Combat stats ignore non-stat ability-use rows.
- Invalid Essence/ability definitions fail validation with clear errors.
- Existing combat and Essence tests continue passing.

## Risks And Assumptions

- The existing runtime engine has limited support for some requested advanced concepts such as once-per-combat, internal trigger cooldown separate from ability cooldown, explicit resource costs, dead ally corpse targeting, ally-death targeting context, and recursion-safe reflection metadata.
- `AbilityTriggerDefinition.InternalCooldownSeconds` is currently validation/documentation data but is not separately enforced from passive `RemainingTimeUntilUse`.
- Target selection currently uses `Random` directly; deterministic tests may need a random provider abstraction later.
- Status effects exist but need a deeper refactor to fully model stack behavior, refresh rules, dispel rules, and periodic effect definitions.
- Some requested effect primitives can be represented through existing generic actions, but others need new actions before content authors can use them safely.
- Frontend Essence UI already displays Essence abilities, but broader UI cleanup may be needed if any old ability screens exist outside the inspected surfaces.

## Implementation Notes For This Pass

Implemented/targeted in this pass:

- Preserve Essence-derived abilities as combat source of truth.
- Keep generic runtime ability/effect primitives.
- Harden validation and mapping vocabulary for scalable data authoring.
- Document the ability authoring workflow.
- Add focused tests for Essence ownership, creature Essence abilities, stat summary cleanup, and validator behavior.

Deferred follow-up:

- Full status stack/refresh/expiration refactor.
- Explicit resource cost engine.
- Full once-per-combat and internal cooldown primitives.
- Deep frontend cleanup beyond compile-safe DTO/contracts.
- EF-backed ability definition authoring, if desired later.

## Implementation Completed In This Pass

Implemented:

- Added this plan document before behavior changes.
- Added `docs/ability-system.md` as an authoring guide for Essence ability definitions.
- Expanded authored target selector vocabulary for two-target, trigger-source, summoned ally, and non-summoned ally targeting.
- Expanded authored condition vocabulary for chance aliases, source health above percent, status stack requirements, always, living ally/outnumber placeholders, and summon source checks.
- Mapped the new supported target selectors and conditions into existing runtime combat primitives.
- Added a non-mutating status-stack condition primitive.
- Added a summon-state condition primitive.
- Added a combat event dispatch depth guard to catch recursive trigger loops.
- Strengthened Essence/ability definition validation.
- Added test coverage for extended target/condition mapping, event recursion protection, stricter validation, and real authored JSON validation.

Old ability code removed:

- No EF-backed or API-backed standalone ability ownership/equipping model was present to remove.
- No old standalone ability source was found feeding combat state.

Old ability code that remains:

- `CombatAbilityDefinition`, `CombatAbilityInstance`, triggers, effects, conditions, statuses, and usages remain as generic runtime primitives.
- `Entity.Abilities`/`CombatEntity.Abilities` remain as `[NotMapped]` runtime containers.
- `BasicAttackLoader` remains for universal basic attack behavior.

Remaining risks:

- Resource costs, true once-per-combat semantics, trigger-specific internal cooldown state, rich status stack/refresh/dispel authoring, and deterministic target RNG remain follow-up engine work.
- Some condition constants are validated authoring vocabulary but still need dedicated runtime semantics before content should depend on them broadly.
- Existing `AbilityTriggerDefinition.InternalCooldownSeconds` is not a separate runtime cooldown yet.

Manual follow-up needed:

- Decide whether ability definitions should remain JSON-authored or move to EF/admin tooling later.
- Expand the representative Essence catalog gradually, with one test per new primitive or behavior pattern.
- Add frontend cleanup only if old ability screens or routes still exist outside the Essence UI.
