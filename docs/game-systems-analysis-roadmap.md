# Game Systems Analysis Roadmap

## Executive Summary

The current direction for the Essence and ability systems is strong. Essences reference shared authored ability definitions by id, while runtime combat still uses separate executable combat primitives. That is the right split for scaling toward hundreds of abilities without creating one C# class per ability.

The main risks are now around system safety and long-term maintainability:

- Dungeon preview and dungeon start validation can drift because access requirements are checked in more than one place.
- Dungeon events are scaffolded but not meaningfully active.
- Creature combat stats are generated from build profiles, archetypes, and area scaling rather than authored directly in creature JSON.
- Creature-to-Essence combat behavior still depends on a name-derived monster id convention.
- Content validation is not yet broad enough to protect a large authored catalog.
- Ability compilation has been split into focused collaborators, but the catalog still needs smoke-test coverage and support reporting before it is comfortable at 500+ abilities.

The best next step is not another feature layer. It is a hardening pass: centralize repeated rules, validate authored content at startup, add ability smoke-test coverage, and make dungeon progression/content easier to reason about.

## Implementation Status

Completed:

- Added a shared `DungeonAccessPolicy` used by both dungeon preview and dungeon start.
- Added `DungeonAccessResult` for `CanEnter`, `MissingRequirements`, `CurrentCombatRating`, and `MinimumCombatRating`.
- Added startup validation for dungeon definition shape and progression ordering.
- Added startup validation for the shared authored ability catalog.
- Centralized the creature-to-Essence source id convention in `CreatureEssenceSource`.
- Added dungeon preview metadata for `FamilyId`, `FamilyTitle`, and `Difficulty` so the frontend can group difficulty variants without parsing ids.
- Added creature build-profile diagnostics that report generated attributes, Combat Rating, and resolved Essence source information after archetype/area scaling.
- Split `EssenceCombatAbilityFactory` into focused compiler, effect mapper, trigger mapper, condition mapper, and evolution modifier collaborators.
- Added an ability catalog smoke-test helper that compiles every authored ability through the runtime factory, plus evolved Essence scenarios where evolution modifiers exist, and runs a generic combat simulation for each compiled scenario.
- Added an executable ability support matrix through `AbilityCatalogValidator`, listing known and currently supported effects, triggers, conditions, and target selectors.
- Added a creature build-profile diagnostic report service that checks every creature across representative area tiers and reports unresolved Essence source ids, invalid Combat Rating, and missing Max Health.
- Added admin diagnostics queries and endpoints for ability catalog smoke-test results and creature build-profile reports.
- Added explicit dungeon reward preview categories for completion loot, tier loot, recurring Monster Cores, and first-completion rewards.
- Centralized default first-completion dungeon rewards in `DungeonRewardCatalog` so preview and reward granting use the same source.
- Added combat summary diagnostics for active ability attempts, passive trigger attempts/activations, and failed effect conditions.
- Added startup validation wiring for generated creature build-profile diagnostics. Invalid generated Max Health or Combat Rating now fails API startup; unresolved creature-to-Essence source ids remain surfaced as diagnostic warnings until expected Essence drops are explicit.

Not started:

- Dungeon event room completion.
- Targeted ability combat simulations for specialized triggers and conditions.

## Current System Snapshot

### Essences And Abilities

- There are currently `31` Essence definitions.
- Those Essences reference `62` shared ability definitions: one active and one passive per Essence.
- The authored ability catalog contains `69` effect definitions.
- Essences are catalog/content data. Player-owned Essence state remains runtime/database state.
- Essence level affects attribute bonuses.
- Ascension affects ability performance such as damage, healing, barrier values, status values, cooldown, duration, and summon scaling.
- Evolution modifies the Essence's referenced abilities through authored modifiers.

This is a good foundation for a 500+ ability goal because it keeps ability identity in content while preserving reusable runtime combat execution.

### Dungeons

- Dungeon content is currently `9` definitions.
- Those definitions represent `3` dungeon families with `3` grades each.
- All `9` dungeon definitions have Combat Rating requirements.
- Grade-specific definitions are useful internally, but the frontend should continue presenting them as one dungeon family with Novice, Veteran, and Champion difficulty choices.

The internal model is workable, but it needs stronger grouping and validation so dungeon families do not accidentally split into separate visible dungeons again.

### Creatures

- There are currently `14` creature definitions.
- Creature combat stats are not sourced directly from the JSON base attribute rows. They are built through creature build profiles, archetypes, damage/defense profiles, and area scaling.
- Creatures do receive Essence abilities in combat, but not through an explicit `essenceDefinitionId` field in creature JSON.
- `CombatSetupService` derives a source monster id from creature name using the pattern:

```text
monster.<normalized creature name>
```

That id is matched against `EssenceDefinition.SourceMonsterId`. This convention works, but it is fragile unless validated. Creature validation should focus on generated combat profiles and Essence source-id resolution, not on requiring populated combat stats in JSON.

### Combat Runtime

- Dungeon combat uses `CharacterSnapshot` and snapshot-equipped Essences.
- Idle/live combat uses the current active Essence loadout.
- Combat setup resolves Essence attribute modifiers, tags, and generated active/passive combat abilities before combat attributes are calculated.
- Runtime combat still uses `CombatAbilityDefinition`, `CombatAbilityInstance`, triggers, conditions, effects, statuses, and combat events.

That separation should remain. Authored ability data should compile into runtime combat primitives; authored data should not become runtime state directly.

### Frontend

- Player-facing naming has been clarified:
  - `Power` is a primary attribute.
  - `Combat Rating` is total calculated strength.
- Dungeon cards now have the right concept available for locked difficulty requirements.
- Essence ability tooltip descriptions should continue being generated from effect data so UI text does not drift away from combat behavior.

## Main Findings

### Dungeons

Keep grade-specific dungeon definitions internally for now, but introduce a stable display/grouping model so the UI treats definitions such as `Goblin Mines I`, `Goblin Mines II`, and `Goblin Mines III` as one dungeon family with Novice, Veteran, and Champion difficulties.

Move dungeon access checks into a shared policy. Preview and start-run validation currently need to agree on Combat Rating and previous-difficulty requirements. A shared policy would prevent one path from allowing entry while the other rejects it.

Finish dungeon event room support. `DungeonRunFactory` contains event-room scaffolding, but event weight is effectively disabled and event resolution is placeholder logic. Keeping half-active systems makes dungeon behavior harder to reason about.

Add dungeon content validation for:

- Missing or duplicate dungeon ids.
- Invalid previous-dungeon chains.
- Missing boss room definitions.
- Missing encounter ids.
- Invalid loot table ids.
- Invalid first-completion item ids.
- Invalid room weights.
- Combat Rating ordering across dungeon grades.
- Dungeon family grouping consistency.

Clarify recurring Monster Core rewards versus first-completion rewards. The player should understand whether a Monster Core is a repeat completion reward, a first completion reward, or both.

### Essences

Keep Essences as catalog data that reference one active ability id and one passive ability id. That model is clean and should scale.

Strengthen startup validation around:

- `sourceMonsterId` references.
- Active/passive ability existence.
- Active ability kind versus passive ability kind.
- Evolution modifier targets.
- Unknown tags.
- Unknown status ids.
- Unknown effect ids.
- Unknown attributes.
- Missing attribute bonuses.

Document and preserve the progression split:

- Essence level increases attribute bonuses.
- Ascension improves ability performance.
- Evolution changes or enhances ability behavior.

The creature-to-Essence relationship should either become explicit in creature content or be validated through the current name-derived convention. The current convention is acceptable only if the app fails clearly when a generated creature source id and Essence source id drift apart.

### Abilities

Keep shared authored `AbilityDefinition` separate from runtime `CombatAbilityDefinition`. That boundary is valuable and should not be collapsed.

The old `EssenceCombatAbilityFactory` shape mapped authored abilities, applied evolution modifiers, mapped triggers, mapped conditions, mapped effects, applied scaling, resolved targeting, and created runtime ability instances in one class. That was too much policy in one place for a 500+ ability system.

Recommended split:

- Ability compiler.
- Effect mapper.
- Trigger mapper.
- Condition mapper.
- Scaling applicator.
- Evolution modifier applier.

**Implementation status:** done for the current runtime mapping shape. The factory now delegates to focused collaborators for compilation, effect mapping, trigger mapping, condition mapping, and evolution modifiers. Scaling remains colocated with effect mapping and `EssenceProgressionConstants`; extract it into a dedicated policy only if tier-scaling rules keep expanding.

Add a validation or smoke-test harness that compiles every authored ability into runtime combat primitives. **Partially done:** `IAbilityCatalogSmokeTester` compiles every authored ability through the same runtime factory combat uses, compiles evolved Essence scenarios where evolution modifiers exist, and runs a generic combat simulation for each compiled scenario. Targeted simulations for specialized trigger/condition patterns are still future work.

Add support status reporting for authored primitives. **Done for core primitive categories:** `AbilityCatalogValidator.GetSupportMatrix()` reports known and currently supported effect types, trigger types, condition types, and target selectors, including computed unsupported lists.

### Combat

Keep dungeon combat snapshot-based. Dungeon runs should use the character's state at dungeon entry, including equipped Essences, not the current live loadout.

Keep idle/live combat current-loadout-based. Live combat should reflect the active Essence loadout at the time combat starts.

Add combat summary diagnostics for:

- Active ability attempts.
- Active ability successful uses.
- Passive trigger attempts.
- Passive trigger successful activations.
- Failed condition counts.
- Basic attack usage.

**Partially done:** active ability attempts, passive trigger attempts, passive trigger activations, failed effect conditions, and existing basic attack usage are now aggregated into combat summaries. This still needs richer reason text if the frontend should explain exactly which authored condition failed.

This will make balance debugging far easier, especially when an ability appears not to fire or a passive condition silently blocks it.

Review the Combat Rating formula after generated creature profiles and item scaling are checked. Combat Rating depends on final combat stats, so validation should inspect generated creature output rather than raw creature JSON attributes.

### Frontend

Continue using clear player-facing labels:

- `Power` for the primary stat.
- `Combat Rating` for total strength.

Dungeon cards should show locked difficulty requirements consistently. A disabled entry button should always be paired with clear missing requirements.

Essence ability descriptions and tooltips should stay effect-driven. Avoid hand-written ability descriptions becoming the only source of numeric truth.

## Prioritized Roadmap

### Priority 1: Safety And Consistency

Add a shared dungeon access policy used by both dungeon preview and dungeon start. **Done.**

Add startup validators for dungeon, Essence, ability, and creature profile output. **Partially done:** dungeon and ability catalog validators are implemented; Essence validation already exists; creature profile diagnostics/report generation, admin visibility, and startup fatal checks for invalid generated Max Health/Combat Rating are in place. Creature-to-Essence source mismatches are still warnings until expected drop behavior is explicit.

Add creature-to-Essence reference validation. Either validate the current name-derived convention or add an explicit creature content id/source monster id. **Partially done:** the source id convention is centralized in `CreatureEssenceSource`, and the diagnostic report flags unresolved creature Essence source ids across representative area tiers. Those unresolved ids are intentionally warnings for now because the content does not yet declare which creatures are expected to drop/use Essences.

Add creature build diagnostics that show archetype, area scaling, final combat attributes, Combat Rating range, and attached Essence ability source. **Done as a service report and admin diagnostics endpoint, with startup fatal checks for invalid generated Max Health and Combat Rating.**

### Priority 2: Dungeon Feature Completion

Implement one authoritative dungeon family/difficulty model for frontend grouping. **Done:** dungeon previews now expose family id, family title, and difficulty metadata so the frontend no longer has to infer grouping from ids or grade labels.

Finish dungeon event rooms or remove them from generated runs until they are real.

Clarify dungeon reward categories. **Done for dungeon preview and granting defaults:** rewards now carry explicit preview categories, recurring Monster Cores are shown separately from first-completion rewards, and default first-completion rewards are centralized in `DungeonRewardCatalog`.

- Completion rewards.
- Grade/tier rewards.
- Monster Core rewards.
- First-completion rewards.

Make the frontend communicate those categories without duplicate-looking rewards. **Done for dungeon cards:** reward cards now show category and source labels.

### Priority 3: Ability System Scalability

Split ability compilation and mapping responsibilities out of `EssenceCombatAbilityFactory`. **Done for the current runtime mapping shape.**

Add an authored ability smoke-test harness that compiles all abilities. **Partially done:** all authored abilities compile through the runtime factory, evolved modifier scenarios are checked where relevant, and generic combat simulations run. Targeted simulations for specialized trigger and condition patterns are still pending.

Add a support matrix for:

- Effect types.
- Trigger types.
- Condition types.
- Target selectors.
- Status ids.
- Attribute ids.
- Scaling behavior.

**Partially done:** effect types, trigger types, condition types, and target selectors are executable metadata through `AbilityCatalogSupportMatrix`. Status ids, attribute ids, and scaling behavior still need richer reporting.

### Priority 4: Balance And UX

Review Combat Rating weights against real dungeon outcomes.

Add combat analytics for ability usage and passive trigger behavior.

**Partially done:** combat summaries now expose active attempts, successful uses, passive activations, failed conditions, damage, and healing per ability.

Improve dungeon preview explanations and Essence tooltip consistency.

Use combat summaries to compare expected ability value against actual damage, healing, mitigation, and uptime.

## Recommended Next Implementation Tasks

1. Add `DungeonAccessPolicy`.
   - Inputs: character combat state, dungeon definition, completion history.
   - Outputs: `CanEnter`, `MissingRequirements`, `CurrentCombatRating`, `MinimumCombatRating`.
   - Use it from both `GetAvailableDungeonsQuery` and `StartDungeonRunCommand`.
   - Status: **Done.**

2. Add `DungeonDefinitionValidator`.
   - Validate dungeon ids, previous chains, room definitions, boss presence, reward item ids, loot table ids, encounter ids, room weights, and Combat Rating ordering.
   - Status: **Partially done.** Current implementation validates self-contained dungeon definition shape, chains, room definitions, boss presence, reward grant shape, entry costs, room weights, and Combat Rating ordering. Repository-backed item/loot table existence validation remains pending.

3. Add `AbilityCatalogValidator`.
   - Validate the whole shared ability catalog independently of Essence validation.
   - Ensure every authored primitive is supported by runtime mapping.
   - Status: **Done for current authored primitives.**

4. Add creature build-profile validation.
   - Validate generated final combat attributes instead of raw JSON base attributes.
   - Report archetype, damage profile, defense profile, area scaling, final Combat Rating, and resolved Essence source id.
   - Fail clearly when a creature expected to use an Essence cannot resolve its `EssenceDefinition.SourceMonsterId`.
   - Status: **Partially done.** The source id convention has been centralized, and `ICreatureBuildProfileDiagnostics.CreateReportAsync` reports scaled creature attributes, Combat Rating, and resolved Essence source data across representative area tiers. Admin diagnostics endpoint wiring is done, and startup now fails on invalid generated Max Health or Combat Rating. Unresolved Essence source ids remain warnings until expected drops are explicit.

5. Split `EssenceCombatAbilityFactory`.
   - Keep public behavior the same.
   - Extract focused collaborators for mapping, scaling, evolution modifiers, triggers, conditions, and effects.
   - Status: **Done for the current runtime mapping shape.** Scaling remains colocated with effect mapping and progression constants until it needs its own policy object.

6. Add an ability catalog smoke-test helper.
   - Compile all authored abilities.
   - Report unsupported content clearly.
   - Include representative combat simulations for common effect patterns.
   - Status: **Partially done.** `IAbilityCatalogSmokeTester` compiles every authored ability through the runtime factory, compiles evolved modifier scenarios where relevant, and runs generic combat simulations. Targeted simulations for common specialized effect/trigger/condition patterns have not been added yet.

## Public Interfaces And Types

This roadmap document does not change public APIs by itself.

Recommended future additions:

```csharp
public sealed record DungeonAccessResult(
    bool CanEnter,
    IReadOnlyList<string> MissingRequirements,
    int CurrentCombatRating,
    int MinimumCombatRating);
```

Recommended validator concepts:

- `DungeonDefinitionValidator`
- `AbilityCatalogValidator`
- `AbilityCatalogSupportMatrix`
- `IAbilityCatalogSmokeTester`
- `CreatureBuildProfileDiagnostic` / `ICreatureBuildProfileDiagnostics`
- Creature build-profile validator or surfaced diagnostic report.
- Expanded `EssenceDefinitionValidator`

Recommended creature-source improvement:

- Add explicit creature content ids or source monster ids, or formally validate the existing name-derived id convention.

## Test Plan For Future Work

Add tests or validation scenarios proving:

- Every authored Essence resolves exactly one active and one passive ability.
- Every authored ability compiles into runtime combat primitives.
- Unsupported effects, triggers, targets, conditions, statuses, and attributes fail at startup.
- Every dungeon reward item id resolves.
- Every dungeon loot table id resolves.
- Every dungeon previous-difficulty requirement points to a valid dungeon.
- Dungeon preview and dungeon start use the same access policy.
- Creature Essence abilities resolve for every creature that should drop an Essence.
- Dungeon combat uses snapshot Essences.
- Idle/live combat uses current active loadout.
- Combat summaries include basic attacks, active ability usage, passive trigger usage, and failed condition counts.

## Assumptions

- The current goal remains support for 500+ abilities through data-driven authored definitions.
- Do not add one C# class per ability.
- Authored ability definitions remain separate from runtime combat ability definitions.
- Services must not return DTOs.
- Services must use repositories rather than direct `IDbContext` access.
- Application handlers map domain/service results to DTOs using `IMapper`.
- Application commands and queries stay split by feature folder.
- This document is advisory and implementation-ready; it does not itself implement the recommended code changes.
