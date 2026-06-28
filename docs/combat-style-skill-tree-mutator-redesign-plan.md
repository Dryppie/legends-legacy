# Combat Style Skill Trees and Essence Mutator Redesign Plan

## Goal

Move Combat Styles from focus-path style bonuses into data-driven skill trees that shape how equipped Essence abilities behave.

The new structure follows `combat_style_skill_trees_essence_mutator_redesign.md`:

- 8 styles.
- Each style has a core passive/resource and core active identity.
- Each tree has 9 major nodes across 3 rows and 3 lanes.
- Each tree has 12 minor nodes.
- A player may select 1 major node per row.
- Row 2 major nodes are Style Mutators unless explicitly marked otherwise.
- Style Mutators alter eligible Essence abilities before combat resolution.

## Implementation Phases

### Phase 1: Data Model Support

Add the schema needed to represent the redesign without forcing every combat behavior to be hard-coded.

- Extend `CombatStyleTreeNodeDefinition` with:
  - `Row`
  - `Lane`
  - `NodeType`
  - `MutatorKind`
  - `MutatorGroups`
  - `Mutator`
  - structured tooltip fields
- Add mutator definitions:
  - mutator groups
  - eligibility conditions
  - transforms
  - tradeoffs
- Extend `AbilitySpec` with explicit mutator-facing metadata:
  - `DeliveryTags`
  - `EffectTags`
  - `TargetingType`
  - `Scaling`
  - `ConversionFlags`
  - `IsHardCrowdControl`
  - `CanEcho`
  - `CanRepeat`
  - `CanTriggerWeaponEffects`

### Phase 2: Skill Tree Rules

Replace the old branch/focus assumptions with row/lane rules.

- Row 1 major nodes are available by default.
- Row 2 availability is determined by selected Row 1 lane:
  - Left unlocks Left + Middle.
  - Middle unlocks Left + Middle + Right.
  - Right unlocks Middle + Right.
- Row 3 availability is determined by selected Row 2 lane using the same rule.
- Minor nodes unlock with their row.
- Major nodes are mutually exclusive per row.
- Minor nodes keep normal rank-up behavior.

### Phase 3: JSON Content Migration

Update `LL/src/API/API.LL/Data/combat-styles/*.json` to represent the new design.

- Keep style ids, resource ids, and existing base combat rules where they still make sense.
- Replace generated focus-path trees with the redesigned major/minor node layout.
- Store Row 2 mutator metadata in JSON.
- Preserve node effect text as authored tooltip content.
- Use Row 3 major nodes as the effective build identity/focus signal.

### Phase 4: Style Mutator Resolver

Add a reusable resolver that applies selected Row 2 mutators to Essence abilities before ability compilation.

Resolver responsibilities:

- Gather active mutators from ranked combat-style nodes.
- Apply at most one mutator per mutator group.
- Check ability/effect tags, damage type, target selector, effect operation, and conversion flags.
- Apply transforms:
  - add ability/effect tags
  - change damage type
  - change scaling attributes/coefficient
  - multiply cooldown/resource cost
  - multiply effect potency
- Apply tradeoffs after transforms.

### Phase 5: Combat Integration

Integrate the resolver into `CombatEngineExecutor`.

- Resolve player/hostile combat-style snapshots before creating runtime combatants.
- Prepare ability specs per combatant:
  - base ability
  - Essence evolution modifiers
  - dungeon/run temporary modifiers
  - combat-style mutators
  - compile modified ability
- Keep `FastCombatEngine` rule-engine integration for runtime resource gain, pending empowerments, damage reduction, and summon modifiers.

### Phase 6: API/UI Support

Expose the new tree metadata through existing combat-style DTOs.

- Add row/lane/node type/mutator summary fields to node models and DTOs.
- Add tooltip sections:
  - Type
  - Affects
  - Changes
  - Tradeoff
  - Does not affect
- Update the Angular model and selected-node panel.

### Phase 7: Validation and Tests

Add focused tests for the new behavior.

- JSON definitions load and every style has 9 major + 12 minor nodes.
- Major node exclusivity per row is enforced.
- Row/lane unlocking works.
- Mutator group conflicts choose one active mutator per group.
- Mutator eligibility respects conversion flags.
- Fighter/Caster representative mutators change eligible ability specs.
- Ineligible abilities remain unchanged.

## First Implementation Scope

This implementation will deliver the framework and data migration needed for the redesign:

- New schema and DTO support.
- Row/lane tree behavior.
- Mutator resolver and combat integration.
- New JSON skill-tree layout for all 8 styles.
- Row 2 mutator metadata for all 8 styles.
- Representative executable transforms for common mutator cases.

Some advanced effects from the document, such as full proxy casting, active style abilities, PvP-specific coefficients, control immunity conversion, and advanced target spreading, remain follow-up combat-resolution features after the base mutator pipeline exists.
