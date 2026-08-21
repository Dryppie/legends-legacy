# Essence Target Clarity Plan

## Current behavior

### All allies

`AllAllies` targets every living combatant on the ability user's team. This includes:

- The ability user
- Other living allied combatants
- Living allied summons

It excludes dead allies and all enemies.

### Current target

`CurrentTarget` targets a living enemy selected using threat. Selection is threat-weighted, so it does not necessarily choose the enemy with the highest absolute threat. Taunt and Stealth affect the weighting.

For an active ability, the selected enemy is locked when the ability starts, and every `CurrentTarget` effect in that use resolves against the same enemy. A triggered or passive effect without an active-ability target lock selects a threat-weighted living enemy when the effect resolves.

`CurrentTarget` does not inherently mean the attacker or recipient associated with the triggering combat event. Those meanings are represented separately by `EventSource` and `EventTarget`.

The runtime behavior is implemented in `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`.

## Problems to solve

- The API exposes a singular `targeting` value based only on an ability's first effect.
- Eleven current abilities contain more than one distinct effect target, so the singular value can be incomplete or misleading.
- The frontend targeting enum does not match the selectors currently defined by the backend.
- Raw names such as `CurrentTarget`, `EventTarget`, and `AllAllies` do not explain their runtime rules.
- Target phrases in ability descriptions do not use the tooltip glossary already available for combat keywords.
- Quest-choice essence previews render descriptions as plain text and bypass the shared description formatter.

## Implementation plan

### 1. Correct the targeting data contract

Replace the singular targeting value with an ordered, deduplicated list containing every distinct target used by an ability's effects.

Update:

- `LL/src/Core/Application/UseCases/Essences/Dtos/EssenceAbilityDto.cs`
- `LL/src/Core/Application/UseCases/Essences/Dtos/EssenceAbilityMappingProfile.cs`
- `LL/src/Core/Application/UseCases/Essences/Dtos/SoulArchiveMappingProfile.cs`
- `LL/src/Presentation/ll/src/app/shared/models/essence-system.ts`
- `LL/src/Presentation/ll/src/app/core/services/api/essences/essence-item-view.service.ts`

Preserve authored effect order while deduplicating selectors. Ensure abilities that affect both the user and enemies advertise both targets.

### 2. Add an exhaustive target glossary

Replace the stale frontend targeting model with a selector type matching every value of `AbilityTargetSelector` in `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`.

Create a data-driven glossary entry for every selector. Each entry should contain:

- The raw selector
- A readable label
- A concise, exact description
- Natural-language aliases used in authored descriptions

Descriptions must explicitly clarify relevant rules, including:

- Whether the ability user is included
- Whether allied summons are included
- Whether only living targets qualify
- Whether selection is random or threat-weighted
- Whether Health means current Health, Max Health, or current Health percentage
- Whether the target is relative to the ability user or the triggering event

Recommended copy for the two most ambiguous selectors:

- **All allies:** "Every living combatant on the ability user's team, including the user and allied summons."
- **Current target:** "A living enemy selected using threat. Active abilities lock this enemy for the entire ability use; triggered effects select one when they resolve."

### 3. Display targets separately from ordinary tags

Add a dedicated **Targets** row beneath each ability. Render every distinct selector as a readable, keyboard-focusable pill with an explanation on hover or focus.

Extend `AbilityTagsComponent` or add a focused target-tag component alongside it:

- `LL/src/Presentation/ll/src/app/shared/components/essences/ability-tags/`
- `LL/src/Presentation/ll/src/app/shared/directives/ability-tooltip-container/`
- `LL/src/Presentation/ll/src/app/shared/components/custom-components/tooltips/ability-tooltip/`

Keep targeting separate from mechanical tags such as Magical, Area, Healing, or Defensive.

### 4. Explain target phrases inside descriptions

Extend the shared description formatter so natural phrases such as "all allies," "current target," "random enemy," and "lowest-health ally" use the same accessible tooltip behavior as Barrier, Poison, and other combat keywords.

Use the target glossary as the source for aliases and explanation text instead of maintaining separate tooltip copy.

Update:

- `LL/src/Presentation/ll/src/app/shared/components/essences/essence-description/essence-description-formatter.ts`
- `LL/src/Presentation/ll/src/app/shared/components/essences/essence-description/combat-keyword-glossary.ts`, or add a neighboring target glossary

Phrase matching must be case-insensitive, prefer the longest alias, avoid matching inside unrelated words, and preserve the authored visible text.

### 5. Cover every player-facing ability surface

Wire the target list and inline explanations into:

- Shared essence details and item tooltips
- Soul Archive ability cards
- Marketplace essence previews
- Quest-choice essence previews
- Essence search

The first three already use the shared description component. Replace the quest journal's plain-text ability descriptions with that component and pass the associated effects.

Relevant files include:

- `LL/src/Presentation/ll/src/app/shared/components/essences/essence-details/essence-details.component.html`
- `LL/src/Presentation/ll/src/app/features/game/character/essences/essences.component.html`
- `LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-commodity/market-place-commodity.component.html`
- `LL/src/Presentation/ll/src/app/features/game/quests/quest-journal-page.component.ts`
- `LL/src/Presentation/ll/src/app/features/game/quests/quest-journal-page.component.html`
- `LL/src/Presentation/ll/src/app/shared/search/essence-search.ts`

Search should index both readable labels and raw selectors so existing and new terminology remain discoverable.

### 6. Add regression coverage

#### Backend

- Verify target lists preserve first-use order and remove duplicates.
- Verify a multi-target ability exposes every distinct target.
- Verify `AllAllies` includes the ability user and allied summons.
- Verify `AllAllies` excludes dead allies and enemies.
- Verify an active ability's `CurrentTarget` is selected using threat and remains locked across its effects.
- Verify triggered `CurrentTarget`, `EventSource`, and `EventTarget` remain semantically distinct.

#### Frontend

- Require a glossary entry for every supported backend selector.
- Verify readable labels and descriptions for `AllAllies` and `CurrentTarget`.
- Verify target pills are ordered, deduplicated, and accessible by keyboard.
- Verify multi-target abilities display every target.
- Verify target phrases are decorated without substring collisions.
- Verify target tooltips expose useful accessible labels.
- Verify quest-choice previews use the shared description behavior.

### 7. Verification

Run the relevant checks after implementation:

```powershell
./build/run-tests.ps1
```

From `LL/src/Presentation/ll`:

```powershell
npm test -- --watch=false
npm run build:development
```

## Design decisions

- Keep target semantics data-driven so every ability surface uses consistent wording.
- Preserve raw selectors in the API while translating them into player-facing labels in the presentation layer.
- Show all distinct effect targets rather than guessing one primary target.
- Reuse the existing accessible ability-tooltip system.
- Avoid rewriting every authored ability description solely to explain targeting.

## Operational impact

- No database migration is required.
- No configuration change is required.
- No infrastructure or external deployment change is required.
- The API and primary Angular frontend should be released together if the targeting DTO shape changes.
