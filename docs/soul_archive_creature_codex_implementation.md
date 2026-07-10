# Soul Archive Creature Archive and Essence Codex Implementation

## Goal

Evolve the Soul Archive from a mostly Essence-management page into a broader collection and discovery hub.

The page should support three player questions:

- Which Essences have I absorbed and can I attune?
- Which creatures have I discovered, defeated, and connected to Essence sources?
- Which Essence collection milestones have I unlocked?

Absorbing unbound Essences should remain available, but it should become an action surface inside the archive rather than the page's main conceptual tab.

## Product Direction

Keep Absorb as a workflow, but demote it from a primary top-level tab.

Recommended top-level sections:

1. **Essences**
   - Existing absorbed Essence archive.
   - Existing loadout/attunement actions.
   - Compact unbound Essence action panel when absorbable items exist.
2. **Creatures**
   - Discovered creature archive / bestiary.
   - Kill counts and first/last defeated timestamps.
   - Creature Essence source status where known.
   - Later: Archive Focus selection.
3. **Codex**
   - Essence collection sets and milestone benefits.
   - Small account/character progression benefits.
   - Clear progress and unlocked state.
4. **Constellations**
   - Existing Soulstone Constellations page can stay separate or be cross-linked.

## V1 Scope

### Creature Archive

Backend:

- Track creature defeat counts per character and creature definition key.
- Store first defeated and last defeated timestamps.
- Return a read model grouped for the Soul Archive UI.
- Include Essence source information where a creature maps to an Essence definition.

Frontend:

- Add `Creatures` view to the Soul Archive.
- Show discovered creatures with kill count, last defeated, and Essence status.
- Keep layout dense and scannable.

### Essence Codex

Backend:

- Return deterministic Codex entries derived from current absorbed Essences.
- V1 entries should be read-only and should not require migrations.
- Benefits should be small, clear, and collection-oriented.

Suggested V1 entries:

- First Echo: absorb 1 unique Essence.
- Beast Studies I: absorb 3 Beast-tagged Essences.
- Regional Survey I: absorb Essences from 3 native regions.
- Attunement Practice: own at least 1 active loadout slot with an Essence.

Frontend:

- Add `Codex` view to the Soul Archive.
- Show each entry's progress, requirement, benefit text, and unlocked state.

### Absorb Workflow

Keep the existing absorb component and state methods.

Frontend V1:

- Remove `Absorb` as a primary tab button.
- Render the absorb component as an action panel in the `Essences` view when the player has unbound Essences.
- Preserve tutorial selectors where possible so existing onboarding does not break abruptly.

## Deferred

These should not be forced into V1:

- Archive Focus selection and daily cooldown.
- Codex bonuses that change combat stats.
- Premium-only Codex bonuses.
- Creature archive filters/search beyond simple grouping.
- Full bestiary lore pages.
- New visual assets for every creature.

## Data Model

Recommended domain entity:

```text
CharacterCreatureArchiveEntry
CharacterId
CreatureDefinitionId
CreatureName
KillCount
FirstDefeatedAtUtc
LastDefeatedAtUtc
```

Use the creature source key where available. If only a runtime creature name is available, normalize through existing creature/essence source helpers before persisting.

## API Shape

Prefer adding to the existing Essence API boundary:

```http
GET /api/v1/essence/creatures
GET /api/v1/essence/codex
```

Possible DTOs:

```text
CreatureArchiveDto
CreatureArchiveEntryDto[]

CreatureArchiveEntryDto
CreatureId
Name
KillCount
FirstDefeatedAtUtc
LastDefeatedAtUtc
EssenceDefinitionId?
EssenceName?
IsEssenceAbsorbed
Tags[]

EssenceCodexDto
EssenceCodexEntryDto[]

EssenceCodexEntryDto
Id
Title
Description
BenefitText
Current
Required
IsUnlocked
Category
```

## Implementation Notes

- Creature kill tracking should be updated from existing combat reward or outbox flows.
- Codex entries should be derived from absorbed Essence state in V1.
- No visible Codex entry should claim a gameplay benefit that is not actually applied, unless it is explicitly cosmetic/descriptive.
- Absorb should remain reachable with minimal clicks when the player has an unbound Essence.

## Acceptance Criteria

- The Soul Archive no longer treats Absorb as a primary conceptual tab.
- Players can still absorb unbound Essences.
- Players can see creature kill history after combat victories.
- Players can see Essence Codex progress derived from absorbed Essences.
- Backend APIs return stable read models for creatures and Codex entries.
- Frontend builds successfully.
- Relevant backend tests cover creature archive recording and Codex derivation.
