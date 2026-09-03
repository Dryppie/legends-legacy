# Soulstones Page Analysis and Plan

## Current implementation — 3 September 2026

This document's original analysis below describes an older Soulstone catalog and remains historical. Its 100-level upgrades, old IDs, costs and proposed fixes are not the current implementation or the next equipment progression milestone. The current source is [progression/soulstone-upgrades.json](../src/API/API.LL/Data/progression/soulstone-upgrades.json); the [equipment progression implementation ledger](../../docs/design/equipment-implementation-status.md) records current verification and remaining work.

The current catalog has 14 constellations with five ranks each. For characters in the saved equipment progression quest cohort, seven remain active and these seven are retired:

| Branch | Retired constellations |
| --- | --- |
| Gathering | Careful Harvest, Gathering Lessons, Rare Node Sense |
| Crafting | Crafting Lessons, Steady Temper, Blueprint Study |
| Dungeons | Sigil Traces |

Unowned retired constellations are hidden. Owned ranks remain visible with no active effect and their refund value; purchase and bonus guards enforce retirement on the backend. Active-progress counts exclude retired ranks. Legacy characters retain their catalog and bonuses.

The [versioned refund mapping](../src/API/API.LL/Data/equipment/equipment-soulstones.v1.json) records the seven current IDs and their historical per-rank costs: `25, 75, 150, 300, 600`. Cumulative refunds for ranks 1–5 are `25, 100, 250, 550, 1150`. These mappings do not cover the different IDs from the older analysis below; unknown historical upgrades still require explicit conversion decisions.

`POST soulstoneUpgrade/RefundRetired` removes only mapped retired ranks and credits their refund in the character-locked command transaction. Retries cannot credit the same ranks again. The page provides a separate confirmation so active upgrades remain intact. Full Reset also uses these mappings for retired ranks. Reading the archive does not trigger a refund or conversion.

Persistence now belongs to [SoulstoneUpgradeRepository](../src/Infrastructure/Persistence/Persistence.LL/Repositories/Soulstones/SoulstoneUpgradeRepository.cs); the service uses the existing command transaction pipeline. The client view adds `isRetired`, and refund responses use the existing Soulstone/character state revisions.

Sigil Traces remains active for legacy characters. Equipment progression now earns ordinary sigils only through its selected-family counter; no additional random roll occurs, and its Sigil Traces ranks are inactive and refundable. Existing sigils and partial deterministic progress are retained. No constellation branching redesign or new bonus system is included. All six equipment progression activation flags remain off, and six earlier equipment progression migrations remain unapplied; this change adds no schema migration.

## Historical analysis

## Scope

This document analyzes the Soulstones page, its upgrade definitions, and the backend paths that make those upgrades meaningful.

Primary areas reviewed:

- Angular page and card components under `LL/src/Presentation/ll/src/app/features/game/character/soulstone-archive/`
- Angular state/API services under `LL/src/Presentation/ll/src/app/core/services/api/soulstone-upgrade/`
- Upgrade definitions in `LL/src/API/API.LL/Data/progression/soulstone-upgrades.json`
- API boundary in `LL/src/API/API.LL/Controllers/V1/SoulstoneUpgradeController.cs`
- Upgrade purchase/reset logic in `LL/src/Infrastructure/Service/Services.LL/Soulstones/SoulstoneUpgradeService.cs`
- Bonus projection in `LL/src/Infrastructure/Service/Services.LL/Bonuses/SoulstoneBonusProvider.cs`
- Reward consumers in combat, dungeon, gathering, crafting, and loot services

## Current Feature Shape

The Soulstones feature is built on a good data-driven foundation:

- Upgrade definitions live in JSON.
- The backend exposes a `SoulstoneUpgradeView` containing definition, current level, and next cost.
- Purchase and reset actions are routed through the API and MediatR commands.
- The bonus provider flattens purchased upgrade levels into generic `BonusKind` values.
- The Angular page groups upgrades by type and renders the same card UI for every upgrade.

This keeps the feature easy to extend, especially for a solo developer. Adding another upgrade is mostly a matter of adding a JSON definition and making sure the named stat is consumed somewhere in gameplay logic.

The main weakness is that the page does not currently help the player make informed decisions, and several upgrades appear to be defined without a confirmed gameplay effect.

## Upgrade Catalog

### Combat

#### Essence Drop Rate

- ID: `combat.essence.drop.rate`
- Max level: `100`
- Per level: `+0.5%`
- Max effect: `+50%`
- Cost curve: linear, `1` through `100`
- Total max cost: `5050` Soulstones

Intent: increase essence drop rate from defeated creatures.

Concern: `CombatEssenceDropRate` is present in `BonusKind`, but I did not find it being consumed by the essence drop flow. Essence drops currently appear to use base drop chance plus resonance in `EssenceSystemService`.

Recommendation: either wire this into `RollMonsterEssenceDropAsync` / `RollEssenceDropsAsync`, or remove/hide the upgrade until it has a real effect.

#### Double Exp Chance

- ID: `combat.double.exp.chance`
- Max level: `100`
- Per level: `+0.2%`
- Max effect: `+20%`
- Cost curve: linear, `1` through `100`
- Total max cost: `5050` Soulstones

Intent: chance to double combat experience.

Status: this is consumed in idle combat and dungeon combat reward calculators.

Recommendation: keep it, but clarify whether it applies to idle combat, dungeon combat, or all combat. The page should say where it works.

### Gathering

#### Double Drop Chance

- ID: `gathering.double.drop.chance`
- Max level: `100`
- Per level: `+0.1%`
- Max effect: `+10%`
- Cost curve: linear, `1` through `100`
- Total max cost: `5050` Soulstones

Intent: chance to double gathered loot.

Concern: `GatheringDoubleDropChance` is defined in `BonusKind`, but current gathering reward processing appears to use tool bonuses, not soulstone bonuses.

Recommendation: wire this into `CombatGatheringRewardProcessor` if soulstone upgrades should affect combat-adjacent gathering. If there is a separate gathering action path, verify and wire it there too.

#### Double Exp Chance

- ID: `gathering.double.exp.chance`
- Max level: `100`
- Per level: `+0.1%`
- Max effect: `+10%`
- Cost curve: linear, `1` through `100`
- Total max cost: `5050` Soulstones

Intent: chance to double gathering experience.

Concern: `GatheringDoubleExpChance` is defined but does not appear to be consumed. `CombatGatheringRewardProcessor` currently awards fixed experience per successful gathering result.

Recommendation: either apply this to gathering experience awards or remove/hide it until gathering progression supports it.

### Crafting

#### Double Item Exp Chance

- ID: `crafting.double.item.exp.chance`
- Max level: `100`
- Per level: `+0.1%`
- Max effect: `+10%`
- Cost curve: linear, `1` through `100`
- Total max cost: `5050` Soulstones

Intent from page text: chance to double item experience while tempering.

Observed behavior: the value is passed into tempering as `doubleProfessionExperienceChance`, and it doubles crafting profession experience in `TemperingService`. It does not appear to double item XP directly.

Recommendation: choose one of these directions:

- Rename it to `Crafting Exp Surge` or `Double Crafting Exp Chance` if it should remain a profession XP modifier.
- Rework tempering so it doubles equipment item XP if the design intent is item progression.

#### Negative Outcome Chance

- ID: `crafting.negative.outcome`
- Max level: `100`
- Per level: `-0.1%`
- Max effect: `-10%`
- Cost curve: linear, `1` through `100`
- Total max cost: `5050` Soulstones

Intent: reduce negative tempering outcomes.

Concern: the bonus is read in `CraftingService` and placed into the `temperingBonuses` dictionary, but `TemperingService` does not use it when rolling outcomes. Outcome probabilities are currently rolled by `TemperingMechanicsService` without receiving the soulstone modifier.

Recommendation: thread this modifier into the outcome roll and reduce `pNegative` by a clamped percent value, or remove the upgrade until that logic exists.

### Miscellaneous

#### Soulstone Drop Rate

- ID: `misc.soulstone.drop.rate`
- Max level: `100`
- Per level: `+0.1%`
- Max effect: `+10%`
- Cost curve: capped linear, `1` through `50`, then `50` per level
- Total max cost: `3775` Soulstones

Intent: improve Soulstone generation.

Status: used by idle combat and tempering Soulstone reward calculations.

Concern: dungeon combat currently returns a fixed `5` Soulstones and does not use this modifier.

Recommendation: decide whether dungeon Soulstone rewards should use the same modifier. If not, the page should say that this applies to idle combat and tempering only.

#### Soulstone Double Drop Chance

- ID: `misc.soulstone.double.drop.chance`
- Max level: `100`
- Per level: `+0.1%`
- Max effect: `+10%`
- Cost curve: capped linear, `1` through `50`, then `50` per level
- Total max cost: `3775` Soulstones

Intent: chance to double Soulstone drops.

Status: used by idle combat and tempering Soulstone reward calculations.

Concern: `LootService.GenerateSoulstoneLoot` compares `rng.NextDouble()` directly to `doubleChance`. Since the bonus system represents values as percentages elsewhere, this may make `0.1` mean `10%` instead of `0.1%`. The Poisson calculator used elsewhere divides by `100`.

Recommendation: verify expected units. If this is a percent value, divide by `100` in `LootService.GenerateSoulstoneLoot`.

## Cost and Progression Analysis

Most upgrades use the same cost curve:

- Level 1 costs `1`
- Level 2 costs `2`
- Level 100 costs `100`
- Total from 0 to 100 costs `5050`

The two Soulstone economy upgrades cap at `50` cost per level:

- Level 1 costs `1`
- Level 50 costs `50`
- Levels 51 through 100 each cost `50`
- Total from 0 to 100 costs `3775`

Approximate total cost to max all current upgrades:

- Six standard upgrades: `6 * 5050 = 30300`
- Two capped Soulstone upgrades: `2 * 3775 = 7550`
- Total: `37850` Soulstones

The long-term track is large enough to support meaningful progression, but the current choice texture is thin because every upgrade is a small incremental chance modifier. The player is mostly choosing between percentages, not playstyle changes.

## Page UX Analysis

The current page is clean and readable:

- Four category sections.
- One card per upgrade.
- Level progress bar.
- Current value.
- Per-level value.
- Next cost.
- Status badge.
- Upgrade button.
- Summary cards for available Soulstones, total levels, affordable upgrades, and maxed upgrades.

The main issue is that the page answers "Can I buy this?" better than "Should I buy this?"

Current pain points:

- The page does not show `current -> next` value, so the immediate impact of a purchase is hidden.
- The page does not explain where each upgrade applies.
- The page does not warn when an upgrade affects only specific loops such as idle combat, dungeon combat, tempering, or combat gathering.
- The page does not expose source guidance for earning Soulstones.
- The page does not recommend upgrades based on the player's current activity or bottleneck.
- The reset action is immediate and free, with no confirmation or refund preview.
- There is no bulk purchase flow, so early levels require repeated clicks.
- There is no "max affordable" or "buy until milestone" option.
- Upgrade names and descriptions are too generic for mechanics that are subtly different.
- The page does not distinguish working upgrades from upgrades that may currently be disconnected from gameplay.

## Frontend Implementation Risks

### Stale Upgrade State

`SoulstoneUpgradeStateService.load()` returns early if upgrades are already loaded. This can go stale if the active character changes or if another action modifies upgrade state.

Recommendation: make the state character-aware, or expose a forced reload path when the active character changes.

### Optimistic Next Cost Drift

The frontend computes the next cost after purchase. This duplicates backend cost logic and can drift. The max-level check also appears to use `nextLevel > maxLevel`, which can leave a next cost after buying the final level.

Recommendation: have purchase return authoritative updated upgrade and character data, or at minimum use `nextLevel >= maxLevel` when setting `nextCost` to null.

### Error and Refund Message Lifecycle

Errors and refund messages can linger after later actions.

Recommendation: clear stale errors on successful load/purchase/reset, and clear refund messages after the next purchase or reload.

### Reset Safety

Reset is free and immediate, but it still changes the whole build.

Recommendation: add a confirmation modal with total refund and current invested levels.

## Backend and Gameplay Risks

### Defined But Unused Bonus Kinds

The biggest risk is that some purchasable upgrades may do nothing. The obvious candidates are:

- `CombatEssenceDropRate`
- `GatheringDoubleDropChance`
- `GatheringDoubleExpChance`
- `CraftingNegativeOutcome`

Recommendation: prioritize a mechanic truth pass before adding more upgrade types.

### Dungeon Soulstone Reward Inconsistency

Dungeon combat currently sets `totalSoulstones = 5`, while idle combat and tempering use Soulstone drop modifiers.

Recommendation: make an explicit design call:

- Dungeon rewards are fixed and unaffected by Soulstone upgrades.
- Dungeon rewards use the same Soulstone drop system.
- Dungeon rewards use a separate dungeon reward modifier.

Then make the page copy match the decision.

### Percent Unit Consistency

Most bonus values are represented as percent values and divided by `100` before being compared to `NextDouble()`. `LootService.GenerateSoulstoneLoot` appears inconsistent for double chance.

Recommendation: normalize percent handling and add tests for expected Soulstone drop/double-drop rates.

## Recommended Page Changes

### Immediate UI Improvements

Add to each upgrade card:

- Current effect.
- Next effect.
- Max effect.
- Applies-to tags.
- Cost to next milestone.
- Affordable levels with current Soulstones.

Example card details:

```text
Current: +3.0%
Next: +3.5%
Max: +50.0%
Applies to: Idle combat, Dungeon combat
Next milestone: Level 10, costs 24 Soulstones
```

### Better Summary Area

Replace or extend the current summary cards with:

- Available Soulstones.
- Invested Soulstones.
- Refund value.
- Affordable upgrades.
- Best affordable milestone.
- Soulstone income modifiers.

### Purchase Controls

Add purchase options:

- `+1`
- `+5`
- `To milestone`
- `Max affordable`

Keep the default interaction simple. A segmented purchase mode or compact dropdown is enough.

### Filtering and Sorting

Useful filters:

- All
- Affordable
- Not maxed
- Combat
- Crafting
- Gathering
- Economy

Useful sort options:

- Recommended
- Lowest cost
- Highest level
- Closest milestone
- Category

### Recommendation Cues

Add lightweight recommendation labels based on player context:

- "Good if you are farming essences."
- "Good if you are tempering often."
- "Improves future Soulstone income."
- "Useful for dungeon leveling."

Avoid making these too verbose. They should be quick decision nudges, not tutorial text blocks.

## Recommended Gameplay Changes

### Fix or Hide Nonfunctional Upgrades

Before expanding the tree, make every visible upgrade mechanically true.

Suggested implementation order:

1. Wire `CombatEssenceDropRate` into essence drop rolls.
2. Wire `CraftingNegativeOutcome` into tempering outcome rolls.
3. Wire gathering upgrades into combat-gathering and any standalone gathering flow.
4. Normalize Soulstone double-drop percent handling.
5. Decide dungeon Soulstone reward behavior.

### Add More Distinct Upgrade Types

The current upgrade set is heavily chance-based. Add upgrades that change planning or utility:

- Essence resonance gain.
- Essence drop pity growth.
- Soul Dust gain from dismantling.
- Crafting queue size.
- Tempering potential efficiency.
- Rare gathering material chance.
- Gathering node success chance.
- Dungeon reward chest bonus.
- Dungeon checkpoint reward bonus.
- Prophecy reward bonus.
- Marketplace listing fee reduction.
- Extra daily prophecy choices.
- Loadout or preset slots.

### Add Milestones

Milestones make long linear tracks feel better.

Examples:

- Level 10: small named milestone.
- Level 25: visible breakpoint.
- Level 50: stronger named milestone.
- Level 100: capstone badge or minor special effect.

This can remain data-driven by adding optional milestone metadata to upgrade definitions.

## Suggested Implementation Phases

### Phase 1: Truth and Consistency

Goal: every visible upgrade does what it says.

Work:

- Audit all `BonusKind` consumers.
- Wire missing bonus effects.
- Fix percent handling.
- Align crafting upgrade name/description with real behavior.
- Add tests around reward modifiers.

### Phase 2: Decision-Support UI

Goal: help players know what to buy next.

Work:

- Add current/next/max effect display.
- Add applies-to tags.
- Add milestone cost preview.
- Add reset confirmation with refund preview.
- Add stale state cleanup in the Angular state service.

### Phase 3: Bulk Spending and Filters

Goal: reduce repetitive clicking and improve scanability.

Work:

- Add purchase amount modes.
- Add affordable/not-maxed filters.
- Add sort modes.
- Return authoritative state from purchase/reset endpoints if practical.

### Phase 4: Upgrade Expansion

Goal: make Soulstones feel like build expression, not only passive percentages.

Work:

- Add new upgrade definitions.
- Add optional prerequisites or milestone metadata if desired.
- Add category summary totals.
- Add recommendation cues based on current player activity.

## Design Principles

- Keep the system data-driven.
- Do not add new upgrade definitions until the existing ones are mechanically reliable.
- Prefer clear player-facing wording over internal stat names.
- Show where each upgrade applies.
- Show immediate next-purchase impact.
- Keep reset forgiving but confirmed.
- Use shared frontend primitives from the existing design system.
- Avoid turning the page into a tutorial; use compact tags, rows, and tooltips.

## Verification To Run When Implementing

Relevant commands will depend on which phase is implemented, but likely include:

```powershell
dotnet build
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

For frontend changes, also run the Angular build or project-specific check command from `LL/src/Presentation/ll` once confirmed.

## Migration and Deployment Notes

No database migration should be required for UI changes, bonus wiring, or JSON definition changes.

Potential deployment implications:

- Changing `soulstone-upgrades.json` affects live upgrade definitions loaded by the API.
- Renaming upgrade IDs would break existing character upgrade rows unless a migration or compatibility mapping is added.
- Changing bonus behavior can affect player economy and progression rates immediately.
- Fixing percent handling may reduce or increase Soulstone income depending on the current unintended behavior.

