# New Player Tutorial Implementation Plan

## Goal

Create a short, guided onboarding quest that teaches the first combat, essence, loadout, and crafting loops before the player is expected to fight normal Lumo Ruins monsters.

The tutorial should make a brand-new account feel capable without weakening regular monsters too much or granting invisible starter power. The player should earn their first essence, learn how to equip it, receive a small set of tutorial equipment, and then graduate into normal idle combat.

## Design Principles

- Do not silently grant combat power on account creation.
- Keep normal Lumo Ruins monsters meaningful.
- Teach one game system at a time.
- Every tutorial reward should be visible and connected to the system it unlocks.
- The player should always have a clear next destination.
- Normal combat areas should stay unavailable until the tutorial is completed.
- The tutorial must be completable by a blank level 1 character with no gear and no essence.
- Tutorial rewards should be useful, but not better than ordinary early progression rewards.

## Player Flow

### Step 1: Account Created

The player creates a new account or guest account.

Initial state:

- Character is level 1.
- Character has normal base attributes.
- Character has no equipped gear.
- Character has no absorbed essence.
- Character has no active essence loadout slots filled.
- The tutorial quest is automatically available and active.

Player sees:

- A tutorial quest panel near the main game layout.
- Quest title: `First Steps`
- Current objective: `Defeat the creature in the Training Area.`
- Action button: `Go to Training Grounds`

Expected navigation:

- Clicking the quest action sends the player to the tutorial combat area.

### Step 2: Enter Tutorial Combat Area

The player arrives at a tutorial-only area.

Area:

- Name: `Training Grounds`
- Type: Idle combat area
- Level requirement: 1
- Difficulty tier: tutorial or tier 0/1
- Visible separately from normal Shenic areas, or pinned above Lumo Ruins as a tutorial area
- Contains only the tutorial monster

Monster:

- Name: `Training Dummy` or `Restless Wisp`
- Purpose: extremely easy first victory
- Health: low enough for a blank character to defeat reliably
- Damage: low enough that the player cannot realistically die unless something is broken
- Experience reward: very small
- Essence source: tutorial essence

Player sees:

- Quest objective: `Defeat the Training Area creature.`
- Action button: `Go to Training Area`

Expected action:

- Player starts idle combat in Training Grounds.
- Player defeats the tutorial creature.

### Step 3: Tutorial Creature Defeated

After victory, the player sees the normal combat summary.

Rewards:

- Guaranteed tutorial essence item
- Small amount of cinders
- Optional tiny amount of character XP

Required guarantee:

- The first tutorial creature kill must always drop the tutorial essence.
- The essence should not depend on normal essence drop RNG.
- If the player already has the tutorial essence, do not grant duplicates unless intentionally supported.

Quest progression:

- Objective completes: `Defeat the Training Grounds creature.`
- New objective: `Absorb the dropped essence.`

Player sees:

- Quest action button: `Go to Essences`

Expected navigation:

- Clicking the quest action sends the player to the Essences page.

### Step 4: Absorb First Essence

The player arrives at the Essences page.

Player state:

- Inventory contains the tutorial essence item.
- Soul Archive does not yet contain the tutorial essence.

Quest objective:

- `Absorb your first essence.`

Expected action:

- Player selects the tutorial essence item.
- Player absorbs it into the Soul Archive.

On success:

- Essence item is removed from inventory.
- PlayerEssence is created.
- Soul Archive displays the absorbed essence.

Quest progression:

- Objective completes: `Absorb your first essence.`
- New objective: `Equip the essence in your active loadout.`

Player sees:

- Quest action button: `Open Loadout`

### Step 5: Equip Essence In Active Loadout

The player remains on, or is guided to, the essence loadout UI.

Player state:

- Tutorial essence exists in the Soul Archive.
- At least one essence slot is unlocked at level 1.
- Active loadout exists, or the tutorial flow creates one when needed.
- No essence is equipped yet.

Quest objective:

- `Place the essence into your active loadout.`

Expected action:

- Player drags/selects the tutorial essence into slot 0.
- Player saves or activates the loadout.

On success:

- Active loadout has the tutorial essence equipped.
- Character now receives that essence's passive bonuses and abilities in combat.

Quest progression:

- Objective completes: `Place the essence into your active loadout.`
- Rewards are granted:
  - 10 Ore and 3 Wood for one Tier 1 one-handed weapon
- New objective: `Craft a Tier 1 one-handed weapon of your choice.`

Player sees:

- Quest action button: `Go to Crafting`

Expected navigation:

- Clicking the quest action sends the player to the Crafting page.

### Step 6: Receive Crafting Materials

The quest grants tier 1 materials so Crafting is not an empty destination after the player equips their first essence.

Recommended reward package:

- Enough common tier 1 materials to experiment with early recipes
- Enough cinders to support early crafting costs, if costs exist

Material reward should be deterministic.

Recommended material types:

- Wood
- Rough Stone
- Rawhide
- Woven Fiber
- Ore

The exact mix should match the selected starter recipes.

### Step 7: Craft A Starter Weapon

The player is now on the Crafting page.

Quest objective:

- `Craft a Tier 1 one-handed weapon of your choice.`

Expected action:

- Player navigates to Crafting, selects any offered Tier 1 one-handed weapon recipe, and crafts it.

On success:

- Quest progress completes when the weapon is actually crafted.
- The crafted weapon appears in inventory with its normal quality and crafting metadata.

Implementation note:

- The normal equipment-crafted event advances the tutorial.
- Only an allowed Tier 1 weapon craft counts.

Quest progression:

- Objective completes: `Craft a Tier 1 one-handed weapon of your choice.`
- New objective: `Equip the weapon you crafted.`

Player sees:

- Quest action button: `Go to Inventory`

### Step 8: Equip The Crafted Weapon

The player arrives at Inventory.

Quest objective:

- `Equip the weapon you crafted.`

Expected action:

- Player selects and equips the Tier 1 weapon created in the previous step.

Recommended requirement:

- Require one equipped Tier 1 weapon with crafting metadata.
- Accept any one-handed weapon recipe offered by the tutorial.

On success:

- Character has visible equipment power.
- Tutorial confirms that gear and essences are now contributing to combat.

Quest progression:

- Objective completes: `Equip your crafted gear.`
- Tutorial quest completes.
- Normal combat areas become available.

Player sees:

- Quest complete state: `You are ready to explore Shenic.`

### Step 9: Graduate To Lumo Ruins

After the tutorial is completed, the player can enter the normal first combat area.

Area:

- Name: `Lumo Ruins`
- Normal level 1 area
- Contains Goblin, Goblin Warrior, Goblin Archer, and Large Rat as normal

Expected action:

- Player starts normal idle combat.
- Player defeats normal first-area creatures as regular progression.

Recommended final reward:

- Small cinder amount
- Small tier 1 material bundle
- Optional title/achievement later, if desired

Player sees:

- Quest complete state: `You are ready to explore Shenic.`
- Optional button: `Continue Fighting`

## Tutorial Quest State Machine

Recommended quest id:

- `tutorial.first_steps`

Recommended steps:

1. `defeat_training_creature`
2. `absorb_essence`
3. `equip_essence`
4. `craft_equipment`
5. `equip_equipment`
6. `start_lumo_ruins`
7. `complete`

Each step should store:

- Step key
- Display title
- Display objective
- Current amount
- Required amount
- Destination route/action
- Completion timestamp

## Backend Implementation Plan

### 1. Remove Silent Starter Power

If any starter essence or starter equipment grant exists on character creation, remove it.

New characters should begin without hidden power so the tutorial is responsible for onboarding rewards.

### 2. Add Tutorial Domain Models

Add a lightweight tutorial progress model.

Recommended table:

- `CharacterTutorialProgress`

Recommended fields:

- `CharacterId`
- `TutorialId`
- `CurrentStep`
- `CraftedTierOneEquipmentCount`
- `EquippedTierOneEquipmentCount`
- `CompletedTrainingCombatAtUtc`
- `AbsorbedEssenceAtUtc`
- `EquippedEssenceAtUtc`
- `CompletedAtUtc`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Use a unique key on:

- `CharacterId`
- `TutorialId`

### 3. Create Tutorial Service

Add an application/service abstraction for tutorial progress.

Responsibilities:

- Get current tutorial state for a character
- Start tutorial progress if missing
- Advance progress when relevant game events happen
- Grant step rewards exactly once
- Return frontend-friendly tutorial state

Recommended service methods:

- `GetTutorialStateAsync(characterId)`
- `RecordTrainingCreatureDefeatedAsync(characterId)`
- `RecordEssenceAbsorbedAsync(characterId, essenceDefinitionId)`
- `RecordEssenceLoadoutSavedAsync(characterId)`
- `RecordCraftingPageVisitedAsync(characterId)`
- `RecordEquipmentChangedAsync(characterId)`

### 4. Add Tutorial Combat Area

Add a tutorial area seed.

Recommended id:

- `tutorial_area_training_grounds`

Recommended area:

- Name: `Training Grounds`
- LevelRequirement: `1`
- DifficultyTier: `0` or `1`
- SpawnProbabilities: one creature only

Recommended creature id:

- `tutorial_training_dummy` or `tutorial_restless_wisp`

Creature tuning:

- Very low health
- Very low power
- No dangerous active ability
- Small XP reward

Important:

- Tutorial creature should not pollute normal Lumo Ruins spawn tables.
- Tutorial area should be available only while tutorial is incomplete, or visually marked as tutorial-only.

### 5. Add Guaranteed Tutorial Essence Drop

The tutorial creature should grant a deterministic essence item on first victory.

Recommended essence:

- New essence: `essence.tutorial.spark`
- Or existing essence: `essence.legacy.goblin`

Recommendation:

- Prefer a new tutorial essence if the goal is full control over balance and text.
- Prefer existing `essence.legacy.goblin` if the goal is less content work.

If creating a new tutorial essence:

- Give it a simple active damage ability.
- Give it a small passive or stat bonus.
- Keep it useful but replaceable.

### 6. Hook Combat Completion

On combat completion:

- If defeated creature is the tutorial creature, call `RecordTrainingCreatureDefeatedAsync`.
- Normal idle combat should be blocked until the tutorial is completed.

The tutorial service should decide whether the event matters.

### 7. Hook Essence Absorption

After `AbsorbUnboundEssenceAsync` succeeds:

- Call tutorial progress for the absorbed essence.
- If the current step is `absorb_first_essence`, advance to `equip_first_essence`.

### 8. Hook Essence Loadout Save/Activation

After a loadout is saved or activated:

- Check whether any active slot contains the tutorial essence, or any essence if the tutorial accepts any first essence.
- If the current step is `equip_essence`, grant crafting materials and advance to `craft_equipment`.

### 9. Hook Equipment Crafting

When the player crafts equipment:

- Listen for the normal equipment-crafted event.
- If the current step is `craft_equipment`, validate the item base and tier.
- Advance to `equip_equipment`.

### 10. Hook Equipment Changes

After equipment is equipped or unequipped:

- Detect whether the crafted Tier 1 weapon is equipped.
- If current step is `equip_equipment`, advance to the first Lumo Ruins expedition.

### 11. Add Tutorial Rewards

Reward grants must be idempotent.

Suggested reward moments:

- Training creature defeated:
  - Tutorial essence item
- Essence loadout saved:
  - Exactly 10 Ore and 3 Wood, enough for one offered Tier 1 one-handed weapon
- Tutorial complete:
  - Small cinder/material reward

Store enough progress state to avoid duplicate reward grants.

### 12. Add API Endpoints

Recommended endpoints:

- `GET /api/v1/tutorial`
- `POST /api/v1/tutorial/visit-crafting`

Optional:

- `POST /api/v1/tutorial/restart` for development only

Tutorial DTO should include:

- Tutorial id
- Current step
- Current objective
- Current amount
- Required amount
- Destination route
- Whether tutorial is complete

### 13. Add Frontend Tutorial State Service

Create a frontend service that:

- Loads tutorial state on app start
- Refreshes after combat, essence, crafting, and equipment actions
- Exposes current tutorial step as a signal/observable

Recommended location:

- `LL/src/Presentation/ll/src/app/core/services/api/tutorial`

### 14. Add Tutorial Quest Panel

Add a compact quest panel to the game layout.

Panel should show:

- Quest title
- Current objective
- Progress amount if applicable
- One primary action button

Panel should not block normal play.

### 15. Add Navigation Actions

Tutorial action buttons should route to:

- Training Grounds
- Essences
- Essence loadout
- Crafting
- Inventory
- Lumo Ruins

If a route needs query params or UI focus, include those in the destination payload.

### 16. Add Contextual Highlights

Optional but useful:

- Highlight absorb button for the tutorial essence.
- Highlight empty essence loadout slot.
- Highlight recommended tier 1 recipes.
- Highlight crafted equipment in inventory.

Keep this lightweight for the first implementation.

## Frontend Player-Facing Copy

Quest title:

- `First Steps`

Step copy:

- `Enter the Training Grounds.`
- `Defeat the creature in the Training Grounds.`
- `Absorb the essence it dropped.`
- `Equip the essence in your active loadout.`
- `Craft a Tier 1 one-handed weapon of your choice.`
- `Equip the weapon you crafted.`
- `You are ready to explore Shenic.`

Button labels:

- `Go to Training Grounds`
- `Start Training Fight`
- `Go to Essences`
- `Open Loadout`
- `Go to Crafting`
- `Go to Inventory`
- `Continue Fighting`

## Balance Targets

Tutorial creature:

- Blank level 1 character should win reliably.
- Fight should last long enough to show combat summary, but not long enough to feel like normal grinding.
- Target duration: 5-15 seconds in simulated combat.

Post-tutorial Lumo Ruins:

- Character with tutorial essence plus the three tutorial items should defeat a normal Goblin reliably.
- Goblin Warrior and Goblin Archer can still be more dangerous.
- Large Rat can remain a defensive variant.

Tutorial items:

- Should be weaker than equivalent base recipe items.
- Should make the player feel stronger without invalidating additional crafting.

## Data And Migration Notes

This feature will likely need a migration for tutorial progress persistence.

Expected schema additions:

- `CharacterTutorialProgress`

No existing production data needs destructive changes.

Existing characters:

- If they do not have tutorial progress, initialize them as completed or eligible depending on design preference.
- Recommended production behavior: existing characters above level 1 or with completed combat history should be marked completed.
- Recommended development behavior: allow reset/restart through a development-only endpoint or admin action.

## Testing Plan

Backend tests:

- New character has tutorial progress available.
- Tutorial creature victory grants essence once.
- Absorbing the tutorial essence advances the quest.
- Equipping the tutorial essence advances the quest and grants crafting materials once.
- Visiting Crafting grants tutorial gear once and advances the quest.
- Equipping required gear advances the quest.
- Equipping the required tier 1 gear completes the quest.
- Rewards cannot be duplicated by replaying events.
- Existing characters can be handled without null reference errors.

Frontend tests:

- Tutorial panel renders current step.
- Action button routes to correct page.
- Progress updates after combat summary refresh.
- Progress updates after essence absorb/loadout save.
- Progress updates after visiting Crafting.
- Progress updates after equipment changes.
- Completed tutorial panel disappears or shows a compact completed state.

Manual QA:

1. Create a fresh guest account.
2. Verify tutorial appears immediately.
3. Go to Training Grounds.
4. Start tutorial fight.
5. Win and receive essence.
6. Navigate to Essences through quest action.
7. Absorb essence.
8. Equip essence in active loadout.
9. Receive crafting materials.
10. Navigate to Crafting.
11. Select any offered Tier 1 one-handed weapon recipe and craft it.
12. Navigate to Inventory.
13. Equip the crafted weapon.
14. Verify the tutorial completes and Training Grounds is no longer available.
15. Navigate to Lumo Ruins.
16. Defeat one normal creature.
17. Verify rewards are not duplicated by refreshing or repeating actions.

## Implementation Order

1. Remove any automatic starter essence/equipment grants.
2. Add tutorial progress domain model and migration.
3. Add tutorial service and DTOs.
4. Seed tutorial area and tutorial creature.
5. Add deterministic tutorial essence reward.
6. Hook tutorial service into combat completion.
7. Hook tutorial service into essence absorption and loadout save/activation.
8. Hook tutorial service into Crafting page visit.
9. Hook tutorial service into equipment equip/unequip.
10. Add tutorial API endpoint.
11. Add frontend tutorial state service.
12. Add tutorial quest panel.
13. Add route/action button behavior.
14. Add contextual UI highlights if time allows.
15. Add backend and frontend tests.
16. Perform full fresh-account manual QA.

## Open Decisions

- Should the tutorial use an existing essence, such as `Goblin's Essence`, or a new tutorial-only essence?
- Should Training Grounds remain visible after completion?
- Should existing accounts be auto-completed or allowed to run the tutorial?
