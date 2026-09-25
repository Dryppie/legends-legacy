# Strongholds: Mechanical Progression Revision

**Date:** 22 September 2026  
**Supersedes conflicting recommendations in:** [Stronghold Game-Design Review](strongholds-game-design-review.md)

## Decision

Strongholds should be a **bounded progression and loot-targeting system**, not mainly a cosmetic estate.

Players invest in facilities, choose what they want to pursue, and receive more focused rewards from normal play. Visual upgrades reflect that investment but are not the main reward.

## Design Rules

- Benefits require normal combat or Dungeon play. Buildings never generate passive income.
- Research has authored ranks and caps. There is no infinite Stronghold level.
- Targeting changes reward composition more than total reward volume.
- Directives affect future activities only. They cannot reroll existing rewards.
- No daily tasks, construction timers, temporary blessings, or claim buttons.
- No universal damage, health, defense, or critical-stat bonuses.
- No paid mechanical plots, research speed, or exclusive facilities.
- Inventory, Equipment, and Essence management remain available outside the Stronghold.

## Progression and Economy

- **Cinders** construct and upgrade the Main Hall and facilities.
- **Soulstones** fund permanent facility research.
- **Accomplishments** gate major Hall stages but are not spent.
- No new Stronghold currency is needed.

Keep the current five-rank Soulstone curve as a starting point: `25 / 75 / 150 / 300 / 600`. One completed research line costs 1,150 Soulstones.

| Hall stage             | Unlock                                     |
| ---------------------- | ------------------------------------------ |
| I — Reclaimed Outpost  | Overview and Arsenal category targeting    |
| II — Fortified Hold    | Essence Conservatory                       |
| III — Established Seat | Cartographer's Lodge                       |
| IV — Great Stronghold  | War College and advanced Arsenal targeting |
| V — Legendary Seat     | Final research ranks and visual state      |

Hall requirements should accept several accomplishment paths, such as level, regional progress, Dungeon mastery, Tower progress, or collections.

## Facilities

### Arsenal

The Arsenal directs random Equipment rewards without increasing their rarity, quality, or drop rate.

Players select:

1. **Category:** Weapons, Armor, or Jewelry.
2. **Subtype:** One-Handed/Two-Handed; Light/Medium/Heavy; or Ring/Necklace/Relic.
3. **Variant:** a discovered compatible style such as Fury, Arcane, Aegis, Warden, or Spirit.

Starting balance targets:

| Rank | Category weight | Subtype weight | Variant weight |
| ---: | --------------: | -------------: | -------------: |
|    1 |           1.25x |         Locked |         Locked |
|    2 |           1.50x |          1.25x |         Locked |
|    3 |           1.75x |          1.50x |          1.50x |
|    4 |           2.00x |          1.75x |          2.00x |
|    5 |           2.25x |          2.00x |          3.00x |

Weights are normalized across eligible rewards. With the current `40% / 35% / 25%` category distribution, a 2.25x Armor weight produces about 54.8% Armor—not a guarantee.

Rules:

- Targeting applies only to eligible random discoveries.
- An unavailable target falls back to the normal eligible pool.
- Variant targeting changes which Variant is selected after a successful Variant roll.
- Guaranteed and authored rewards ignore Arsenal directives.
- Area combat and Dungeons use the same targeting rules.

### Essence Conservatory

Move the four active Essence Soulstone upgrades here without changing their IDs or ranks.

| Research          |                                       Maximum effect |
| ----------------- | ---------------------------------------------------: |
| Essence Resonance |                    +15% relative Essence drop chance |
| Echo Memory       |                           +25% pity progression gain |
| Duplicate Echoes  | 10% extra-material chance from duplicate dismantling |
| Archive Focus     |   +25% relative drop chance for the focused creature |

Creature Focus remains one shared setting with the Creature Archive. The Stronghold may expose that setting but must not create a second one.

The existing focus multiplier is already `3x`. Do not add another independent multiplier.

### Cartographer's Lodge

The Lodge improves and directs Dungeon Sigil acquisition.

- **Active Survey:** select one discovered Dungeon family.
- **Sigil Tracing:** modestly improve Sigil acquisition.
- **Route Familiarity:** favor the surveyed family's Sigil.
- **Rest Site Satchel:** move the existing Dungeon reward-retention upgrade here.

Starting balance targets:

| Rank | Relative Sigil increase | Selected-family share with two eligible families |
| ---: | ----------------------: | -----------------------------------------------: |
|    1 |                     +5% |                                              55% |
|    2 |                    +10% |                                              60% |
|    3 |                    +15% |                                              65% |
|    4 |                    +20% |                                              70% |
|    5 |                    +25% |                                              75% |

The increase is relative. Rank 5 multiplies the current chance by `1.25`; it does not add 25 percentage points.

The codebase describes both random Sigil drops and a saved-cohort deterministic counter. Choose one model before implementation:

- For random drops, improve chance and family weighting.
- For deterministic progress, shorten the threshold and select its family.

Do not run both models together.

### War College

Move the two active combat Soulstone upgrades here:

- **Battle Lessons:** up to +7.5% idle and Dungeon combat experience.
- **Survival Notes:** up to 50% idle-defeat experience retention.

Future War College research should improve learning, recovery, or planning—not raw combat stats.

### Gallery of Legacy

Keep the Gallery as optional prestige content after the mechanical core works. Facility ranks and completed collections should automatically improve the estate's appearance.

## Removing the Soulstones Page

Remove the standalone Soulstones page, but keep Soulstones and all owned upgrades.

### UI changes

- Remove **Soulstones — Permanent upgrades** from the Character sidebar.
- Add **Stronghold** as a primary destination.
- Show research inside its owning facility.
- Show Soulstone balance and affordable upgrades in the Stronghold header.
- Redirect `/game/character/soulstone-archive` to the Stronghold.
- Update Prophecy links that use the old route.

### Safe migration

- Keep `CharacterSoulstoneUpgrade` rows and existing definition IDs.
- Keep purchase, reset, refund, and requirement behavior.
- Reuse the current upgrade cards inside facility views.
- Do not refund active upgrades merely because their UI moved.
- Keep retired upgrades retired and refundable.
- Do not reactivate the retired `Sigil Traces` definition.

| Current branch    | New facility         |
| ----------------- | -------------------- |
| EssenceArchive    | Essence Conservatory |
| CombatProgression | War College          |
| Dungeons          | Cartographer's Lodge |

Renaming the backend API to `StrongholdResearch` can happen later. It should not block the UI move.

## Building Availability and Player Choice

Completed functional buildings remain active. Making them compete for active plots would encourage players to swap facilities before each activity.

Specialization comes from:

- which research the player funds first;
- the active Arsenal and Sigil directives;
- the player's current Creature Focus.

Visual showcase plots may remain limited because they have no mechanical effect.

## Activity Snapshot Rule

Capture relevant directives when an idle-combat schedule or Dungeon run begins. Later changes apply only to the next activity.

This prevents retroactive reward changes and protects deterministic reward seeds.

## Recommended MVP

1. Add the Stronghold shell, Main Hall, and simple visual stages.
2. Move the seven active Soulstone upgrades into their facilities.
3. Remove the old Soulstones navigation entry and redirect its route.
4. Implement Arsenal category and subtype targeting for area and Dungeon drops.
5. Resolve the canonical Sigil model, then add Active Survey and Sigil research.

Defer public estates, visitors, expeditions, complex decoration, timers, and the Gallery system.

## Implementation Shape

Follow existing architecture:

- **Core:** Stronghold rules, directives, eligibility, weighting, and snapshot models.
- **Application:** overview, construction, research, and directive commands/queries.
- **Infrastructure:** persistence and reward-pipeline integration.
- **API:** thin command/query endpoints.
- **Presentation:** Stronghold shell, facility views, and reused research cards.

Likely new persisted state:

```text
CharacterStronghold: CharacterId, HallStage, Version
CharacterStrongholdFacility: CharacterId, FacilityId, Rank
CharacterStrongholdDirective: CharacterId, DirectiveKind, SelectedValue, Revision
```

Do not copy existing Soulstone ranks into these tables. Continue reading them from `CharacterSoulstoneUpgrade`.

Integrate directives at the existing selection points:

- `EquipmentSelectionWeights.Roll` for Equipment category and subtype.
- `EquipmentBlueprintCatalog.RollVariant` for Variant weighting.
- `CombatAcquisitionRewardProcessor` for area Equipment and Sigils.
- `EquipmentAcquisitionService` for Dungeon Equipment.
- `EssenceSystemService` and the existing bonus provider for Essence research.

## Verification and Deployment

Verify that:

- all existing Soulstone ranks, costs, refunds, and bonuses survive the move;
- old routes and Prophecy links redirect correctly;
- weights normalize and unavailable targets fall back safely;
- area and Dungeon targeting behave consistently;
- changing a directive cannot reroll an active activity;
- guaranteed rewards ignore targeting;
- only one Sigil acquisition model is active;
- total Equipment volume remains unchanged.

The UI move can initially ship without a database migration. Mechanical facilities require an EF Core migration for ownership and directives. Preserve all Soulstone data, use stable facility IDs, and place Equipment and Sigil integrations behind separate feature flags.

## Final Vision

The Stronghold is the player's progression headquarters. Cinders build it, Soulstones improve it, and directives tell the game what the player wants to pursue next.

It matters because it improves normal play—not because it adds chores. Players visit to make decisions, then benefit from those decisions across combat and Dungeons.
