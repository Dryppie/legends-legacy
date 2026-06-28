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

## Current Implementation Status

Implemented:

- Combat Style definitions are JSON-backed under `LL/src/API/API.LL/Data/combat-styles/*.json`.
- All 8 styles have the redesigned 9-major / 12-minor row-lane tree structure.
- Tree nodes expose row, lane, node type, mutator kind, mutator groups, effects, and structured tooltip sections through DTOs.
- Row/lane unlock rules and one-major-per-row selection rules are enforced.
- Row 2 mutators are resolved through `CombatStyleAbilityMutatorResolver` before ability compilation.
- The Combat Styles page renders redesigned row/lane trees as a unified skill-tree map rather than separate branch cards.
- The skill-tree UI now uses the shared game design system: `--ll-*` tokens, texture surfaces, compact bordered controls, restrained gold accents, and shared radius/shadow language.

Partially implemented:

- Representative mutator transforms exist for the current tree content.
- Advanced combat-resolution features from the source redesign document still need separate work where they require new engine behavior.
- Browser-level visual verification is limited by the isolated local browser session not having signed-in game state; Angular build and route smoke checks pass.

## Implementation Phases

### Phase 1: Data Model Support - Implemented

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

### Phase 2: Skill Tree Rules - Implemented

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

### Phase 3: JSON Content Migration - Implemented

Update `LL/src/API/API.LL/Data/combat-styles/*.json` to represent the new design.

- Keep style ids, resource ids, and existing base combat rules where they still make sense.
- Replace generated focus-path trees with the redesigned major/minor node layout.
- Store Row 2 mutator metadata in JSON.
- Preserve node effect text as authored tooltip content.
- Use Row 3 major nodes as the effective build identity/focus signal.

### Phase 4: Style Mutator Resolver - Implemented

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

### Phase 5: Combat Integration - Implemented

Integrate the resolver into `CombatEngineExecutor`.

- Resolve player/hostile combat-style snapshots before creating runtime combatants.
- Prepare ability specs per combatant:
  - base ability
  - Essence evolution modifiers
  - dungeon/run temporary modifiers
  - combat-style mutators
  - compile modified ability
- Keep `FastCombatEngine` rule-engine integration for runtime resource gain, pending empowerments, damage reduction, and summon modifiers.

### Phase 6: API/UI Support - Implemented

Expose the new tree metadata through existing combat-style DTOs.

- Add row/lane/node type/mutator summary fields to node models and DTOs.
- Add tooltip sections:
  - Type
  - Affects
  - Changes
  - Tradeoff
  - Does not affect
- Update the Angular model and selected-node panel.
- Render row/lane tree data as one connected skill-tree map.
- Keep the legacy branch renderer as a fallback for older tree data.
- Match the Combat Styles page to the rest of the game frontend design system.

### Phase 7: Validation and Tests - Partially Implemented

Add focused tests for the new behavior.

- JSON definitions load and every style has 9 major + 12 minor nodes.
- Major node exclusivity per row is enforced.
- Row/lane unlocking works.
- Mutator group conflicts choose one active mutator per group.
- Mutator eligibility respects conversion flags.
- Fighter/Caster representative mutators change eligible ability specs.
- Ineligible abilities remain unchanged.
- Angular development build passes through the local Angular CLI binary.

## First Implementation Scope

This implementation delivered the framework and data migration needed for the redesign:

- New schema and DTO support.
- Row/lane tree behavior.
- Mutator resolver and combat integration.
- New JSON skill-tree layout for all 8 styles.
- Row 2 mutator metadata for all 8 styles.
- Representative executable transforms for common mutator cases.
- Unified Angular skill-tree renderer for the redesigned row/lane trees.
- Design-system-aligned skill-tree styling.

Some advanced effects from the document, such as full proxy casting, active style abilities, PvP-specific coefficients, control immunity conversion, and advanced target spreading, remain follow-up combat-resolution features after the base mutator pipeline exists.

## Recent UI Update

The Combat Styles page now avoids showing the redesigned tree as three independent `Left`, `Middle`, and `Right` branch panels. Redesigned trees are flattened into a single row/lane map:

- Major nodes anchor the left, middle, and right lanes.
- Minor nodes offset beside their related lane instead of forming boxed subtrees.
- The old branch renderer remains available for non-row/lane data.
- Visual styling uses the same texture, compact borders, muted panels, and gold accent system as the rest of Legend's Legacy.

Verification:

- `LL/src/Presentation/ll/node_modules/.bin/ng.cmd build --configuration development` passed.
- A temporary Angular dev server was started for route smoke checking and stopped afterward.
