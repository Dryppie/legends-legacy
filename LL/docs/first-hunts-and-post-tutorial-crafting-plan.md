# First Hunts and Post-Tutorial Crafting Quest Plan

## Implementation Status

Implemented on 2026-08-09. During implementation the quest lifecycle was
simplified further: definitions remain locked until their level and quest
prerequisites are met, then become Active immediately. The journal no longer
has an Available/acceptance state.

## Goal

Replace the fixed `Training Day` encounter with a meaningful starter choice:

1. the player chooses one of three **First Hunts**;
2. each choice previews the creature and the Active and Passive abilities of
   the Essence it guarantees;
3. the player fights the selected creature in the Training Area;
4. victory completes the quest and grants that creature's unbound Essence;
5. the existing Tutorial chain continues with absorbing and attuning the
   selected Essence.

After the Tutorial chain, expose a separate, automatically activated Crafting quest
that asks the player to craft one armor piece and one jewelry piece in either
order. This quest is normal early-game content, not a Tutorial quest and not a
gate for the Shenic progression chain.

## Recommended Player Flow

### First Hunt

Keep the existing first-login welcome. **Begin First Steps** still opens the
Quest journal and animates into the header tracker. The selected quest should
initially read **Choose Your First Hunt** and show three compact choice cards.

The implemented choices are:

| Hunt | Role taught | Active Essence ability | Passive Essence ability | Guaranteed reward |
| --- | --- | --- | --- | --- |
| Goblin Warrior | Multi-target physical offense | **Raging Cleave** — damages two enemies | **Relentless** — empowers every third Basic Attack | `item.essence.goblin_warrior` |
| Hollow Stag | Magical pressure and defense | **Echoing Antlers** — deals magical damage and applies Weaken | **Hollow Core** — gains Damage Reduction as Health is lost | `item.essence.hollow_stag` |
| Skeleton | Physical offense and protection | **Bone Smash** — deals heavy physical damage | **Calcium** — grants Guard at combat start | `item.essence.skeleton` |

The card shows the creature name, short role description, Active ability,
Passive ability, and the guaranteed Essence reward. Creature portraits are not
shown. Reuse the existing Essence/item presentation so the reward remains
hoverable.

Selecting a Hunt requires confirmation because it determines the first
Essence. After confirmation:

- the choice is immutable;
- the quest title/detail changes to the selected First Hunt;
- the quest stays pinned;
- **Begin Hunt** navigates to the Training Area;
- the Training encounter spawns exactly the selected creature;
- a loss permits retrying the same Hunt;
- a victory completes the quest and grants exactly one selected unbound
  Essence through the existing idempotent quest reward path.

### Continuing the Tutorial

`The Soul Archive` remains the next Tutorial quest, but its copy becomes
choice-neutral:

1. absorb your First Hunt Essence;
2. attune that same Essence in the active loadout.

The objectives must resolve the Essence definition from the player's persisted
First Hunt selection. They should not merely accept any Essence, because that
would let a previously owned or separately acquired Essence advance the
Tutorial instead of the chosen reward.

The remaining Tutorial order stays unchanged:

1. Choose Your First Hunt;
2. The Soul Archive;
3. Forge Your Path;
4. Tools of the Trade;
5. Into the Ruins.

## Content Versioning and Existing Characters

Retain the stable ID `quest.onboarding.training_day`. Version 2 introduced the
choice model; version 3 introduced the first revised roster; version 4 replaces
Treant Sapling with Skeleton.

- New characters receive version 4 and make a First Hunt selection.
- Characters who completed version 1 remain completed and do not repeat it.
- Characters with an active version 1 quest can finish the original Goblin
  encounter and reward without being silently reinterpreted.
- Characters who confirmed an older Hunt retain that selection. An active,
  unconfirmed choice Hunt upgrades safely to the latest definition.
- Add version 2 of `The Soul Archive` for the choice-bound Essence objectives;
  existing active version 1 progress continues to require Goblin Essence.
- The Training Area can retain its existing
  `requiredActiveQuestId: quest.onboarding.training_day` gate.
- The welcome acknowledgment can retain the current Training Day stable-ID
  check.

This uses the quest catalog's existing version support and avoids a risky
backfill of completed player progress.

## Quest Model Extension

### Definition contract

Extend `QuestDefinition` with optional choice metadata. A choice option should
contain enough authored data to drive both the encounter and UI without
frontend hard-coding:

```json
{
  "choice": {
    "selectionTitle": "Choose Your First Hunt",
    "confirmationText": "This creature's Essence will become your first.",
    "options": [
      {
        "key": "goblin_warrior",
        "title": "Hunt the Goblin Warrior",
        "creatureId": "00000000-0000-0000-0000-000000000002",
        "essenceDefinitionId": "essence.goblin_warrior",
        "rewardItemBaseId": "item.essence.goblin_warrior",
        "encounterKey": "first-hunt"
      }
    ]
  }
}
```

The API resolves the creature name and Essence ability details from their
authoritative catalogs. Names, ability descriptions, and item details must not
be duplicated in the quest JSON.

Extend objective filters with a general choice reference for downstream
objectives, for example:

```json
{
  "type": "EssenceAbsorbed",
  "filters": {
    "essenceDefinitionFromChoiceQuestId": "quest.onboarding.training_day"
  }
}
```

Use the same resolved value for `EssenceEquipped`. Keeping this reference
generic makes the extension reusable for later reward or route choices.

### Persisted progress

Add nullable `SelectedOptionKey` to `CharacterQuestProgress` and include it in
the EF configuration. Generate an EF Core migration that adds the nullable
column; no data backfill is required.

Selection rules belong in the quest service:

- only an active quest with authored choices can be selected;
- the option key must exist in the progress row's definition version;
- selection is allowed only while no option is selected and no objective has
  progressed;
- repeated submission of the same option is idempotent;
- selecting a different option after confirmation is rejected;
- the mutation updates `UpdatedAt` and `RowVersion`, saves transactionally,
  and publishes the normal quest-journal-changed message.

### Validation

Extend startup catalog validation to reject:

- duplicate or empty option keys;
- choice quests with fewer than two options;
- missing creature, Essence, reward item, or encounter references;
- an Essence whose `sourceMonsterId` does not match the selected creature;
- an Essence item that does not represent the selected Essence definition;
- missing Active or Passive ability definitions;
- downstream choice references to a missing or non-choice quest;
- a selected-option reward that is also accidentally granted as an ordinary
  base quest reward.

## Backend Work

### Application and API

Add a transaction-backed command and endpoint such as:

```text
POST /api/v1/Quest/{questId}/choice
{ "optionKey": "hollow_stag" }
```

The handler should call the quest service and return the refreshed journal,
matching pin and welcome mutations.

Extend `QuestStateDto` with:

- `selectedOptionKey`;
- `choice`, containing selection copy and resolved option previews;
- each option's creature identity, Essence item, Active ability, Passive
  ability, and selection state.

Use existing Essence ability DTO/mapping types where possible so descriptions
remain consistent with the Soul Archive. The Angular client should not parse
combat ability JSON itself.

### Encounter orchestration

Generalize `QuestEncounterService` so Training Day version 2:

1. loads the character's active quest progress and exact definition version;
2. requires a selected option;
3. resolves that option's creature rather than taking the area's highest
   weighted spawn;
4. runs the existing one-versus-one training combat;
5. advances the quest only on the existing server-produced result;
6. returns the quest-granted Essence in combat loot on victory.

Version 1 retains its current Goblin fallback. Do not modify normal Training
Area spawn ordering or use a query parameter to select the enemy; the server
must derive the creature from persisted quest state.

### Reward resolution

Resolve the selected option's guaranteed Essence as the quest reward before
calling the existing reward grant logic. Keep reward idempotency based on the
quest progress plus option reward key. Never also roll the normal creature
Essence drop for this training encounter, otherwise the player could receive a
duplicate in addition to the guaranteed quest reward.

### Downstream progression

When evaluating `The Soul Archive` version 2:

- read the completed First Hunt progress;
- resolve its selected option using the stored definition version;
- compare `EssenceAbsorbed` events with the selected Essence definition;
- query the active loadout for that same Essence for `EssenceEquipped`;
- fail closed and log a content/progress error if the stored option can no
  longer be resolved.

## Frontend Work

### Quest journal

Add a choice state to the existing detail pane rather than creating a separate
page. Before selection, replace the ordinary objective list with the three
First Hunt cards and a confirmation action. After selection, render the normal
quest detail and objective UI.

Requirements:

- clearly label Active and Passive abilities;
- show full ability descriptions without requiring a separate help page;
- show the guaranteed Essence using the existing item/Essence component so it
  supports hover details;
- disable all choices while the selection request is running;
- display an inline error if selection fails;
- keep keyboard focus, mobile stacking, and reduced-motion behavior usable;
- refresh the shared quest state from the command response so the journal and
  tracker update together.

### Header tracker and welcome transition

While no option is selected, the header tracker should show **Choose Your First
Hunt** with an action that returns to `/game/quests`. It must not offer **Head
to the Training Area** yet. After selection, it shows the selected Hunt and the
normal encounter objective/action.

The existing first-login welcome animation remains unchanged apart from its
destination state being the choice prompt rather than the old fixed Training
Day objective.

## Post-Tutorial Crafting Quest

### Proposed content

Add a normal quest such as:

```text
Title: Armor and Adornment
Stable ID: quest.crafting.armor_and_adornment
Category: Crafting
Activation: automatic when unlocked
Minimum level: 1
Prerequisite: quest.region01.into_lumo_ruins
Objective mode: All
```

Objectives:

1. **Craft one armor piece.** Accept any enabled Tier 1 Head, Chest, or Legs
   armor recipe output.
2. **Craft one jewelry piece.** Accept any enabled Tier 1 Ring, Necklace, or
   Relic recipe output.

Use `objectiveMode: "All"` so both objective circles are active and the player
can craft them in either order. Both objectives navigate to the existing
crafting page.

This quest is intentionally:

- not in the `Tutorial` category;
- activated automatically when its prerequisites are met;
- unlocked only after `Into the Ruins`, which is the final Tutorial quest;
- independent of `Trial of Lumo` and combat-area access;
- available alongside the other first normal quests.

The current `EquipmentCrafted` trigger and item-base filters already support
this content. Author explicit output ID lists initially:

- armor: `heavy_helm`, `medium_helm`, `light_hood`, `cloth_cowl`,
  `heavy_breastplate`, `medium_mail`, `light_vest`, `cloth_robe`,
  `heavy_legplates`, `medium_greaves`, `light_leggings`, `cloth_pants`;
- jewelry: `band`, `amulet`, `vial`.

Explicit IDs fit the current engine and startup validation. A later catalog-tag
filter can replace them if recipe families become a common quest condition.

Reward values should be chosen during balance review. Prefer a small material
refund or crafting component rather than equipment, so the reward does not
invalidate the two items the player just chose to make. Confirm that the
Tutorial and early gathering rewards provide enough Metal, Wood, and Hide for
at least one valid armor-plus-jewelry combination; otherwise adjust the quest
reward timing or starter material supply so the quest is available but not
misleadingly impossible.

## Expected File Areas

The implementation will primarily touch:

- `Core/Domain/Models/Quests/CharacterQuestProgress.cs` and quest constants;
- `Core/Application/Interfaces/Services/LL/Quests/` definition and service
  contracts;
- a new `SelectQuestChoice` command and quest DTO mappings;
- `Infrastructure/Service/Services.LL/Quests/QuestService.cs`;
- `Infrastructure/Service/Services.LL/Quests/QuestEncounterService.cs`;
- `Infrastructure/Service/Services.LL/Quests/JsonQuestDefinitionProvider.cs`;
- quest persistence configuration and a new EF Core migration;
- `API.LL/Controllers/V1/QuestController.cs`;
- version 2 Training Day and Soul Archive definitions plus the new side-quest
  JSON;
- Angular quest models, API/state services, journal detail UI, and header
  tracker;
- backend quest-system tests and focused frontend component/service tests.

No infrastructure-as-code or external-service changes are required.

## Verification Plan

### Backend

- catalog accepts all three valid options and rejects every invalid reference;
- a new character sees version 2 and no selected option;
- selecting one option persists it and excludes changing to another;
- duplicate selection requests are idempotent;
- encounters cannot start before selection;
- each option spawns exactly its configured creature;
- defeat grants no Essence and permits retry;
- victory completes once and grants exactly the selected Essence once;
- duplicate combat/outbox delivery cannot grant another Essence;
- Soul Archive advances only for the selected Essence;
- version 1 active and completed characters retain their old behavior;
- Armor and Adornment becomes available after `Into the Ruins`, not before;
- crafting armor and jewelry in either order completes their corresponding
  objectives;
- weapons, off-hands, tools, and duplicate items from one family do not satisfy
  the other objective.

Run the focused quest tests, relevant crafting/outbox tests, and the broader
backend test project if time permits.

### Frontend

- welcome transitions to the unselected First Hunt tracker state;
- all three cards show the correct creature, Essence, and two abilities;
- reward hover uses the existing Essence details UI;
- loading, error, selected, confirmed, and retry states render correctly;
- selection updates journal and header without a reload;
- mobile layout stacks the choices and keeps the confirmation usable;
- the normal crafting quest is labeled Crafting, never Tutorial;
- header objective hover works for the selected Hunt and crafting quest.

Run formatting, the Angular development build, focused tests where available,
and `git diff --check`.

## Delivery Sequence

1. Add choice definition contracts, startup validation, persisted selection,
   migration, command, and API response fields.
2. Add version 2 First Hunt content and server-selected encounter/reward
   behavior.
3. Add choice-bound Soul Archive version 2 objectives and compatibility tests.
4. Build the journal choice cards and tracker states using resolved backend
   previews.
5. Add `Armor and Adornment` using the existing simultaneous crafting
   objectives.
6. Verify new-character, existing-character, failure/retry, duplicate-event,
   responsive, and accessibility paths.

## Decisions to Confirm During Implementation

- Final player-facing names and short descriptions for the three Hunts.
- The final roster is Goblin Warrior, Hollow Stag, and Skeleton.
- Final reward for `Armor and Adornment` and whether starter materials need a
  small balance adjustment.

These are content/balance decisions only; the architecture above supports
changing them without altering persisted quest semantics after the option keys
and released quest version are fixed.
