# Strongholds Mechanical Progression Revision

**Status:** Revised product direction  
**Date:** 14 September 2026  
**Companion to:** [Stronghold Game-Design Review](strongholds-game-design-review.md)

## 1. Revised Verdict

The earlier recommendation protected the Stronghold from mandatory chores and power creep so aggressively that it removed the reason to keep investing in it. A primarily cosmetic estate may be pleasant to visit, but it is too weak to carry the development cost of a major feature in LegendsLegacy.

The Stronghold should instead be a **bounded, mechanical progression headquarters**. Its main purpose is to let the player improve and direct rewards from systems they already play. The best benefits are not generic `+Power` bonuses. They are persistent upgrades and explicit priorities such as:

- favoring Weapons, Armor, or Jewelry when equipment drops;
- favoring Light, Medium, or Heavy armor within eligible armor drops;
- favoring an eligible Equipment Variant such as Fury, Arcane, or Warden;
- increasing Sigil acquisition and directing Sigils toward a selected Dungeon family;
- strengthening existing Creature Focus, Essence drop, pity, and duplicate-dismantle mechanics;
- improving combat experience and Dungeon reward retention through the existing Soulstone upgrades.

Cosmetic growth remains valuable, but it becomes the visible expression of mechanical investment rather than the feature's primary reward.

This gives the Stronghold a clear promise:

> **Build a headquarters that makes the rest of the game more intentional. Invest in the systems you care about, choose what you want to find next, and see those choices reflected in the estate.**

## 2. Design Boundary

The Stronghold may improve rewards, but only within four boundaries:

1. **Benefits require normal play.** Buildings may improve or direct drops earned from combat and Dungeons. They do not generate items while the player is absent and never add collection buttons.
2. **Progression is capped.** Each research line has authored ranks and a cap. “Long-term” means several meaningful paths and future extensions, not an uncapped multiplier.
3. **Most choice changes composition, not total volume.** Loot targeting is safer and more expressive than stacking universal quantity bonuses.
4. **Activity state is snapshotted.** A directive selected before an idle-combat schedule or Dungeon begins applies to that activity. Changing it cannot reroll completed or already-determined rewards.

The following remain out of scope:

- passive building income;
- daily Stronghold tasks or rotating orders;
- construction timers and speed-ups;
- temporary blessings that must be refreshed;
- universal attack, health, defense, or critical-stat bonuses;
- paid mechanical plots or paid upgrade speed;
- moving baseline inventory, equipment, or Essence management behind the Stronghold.

## 3. The New Progression Model

The Stronghold has three connected layers.

### Main Hall restoration

The Main Hall is upgraded with **Cinders plus accomplishments**. It unlocks facilities, additional research ranks, and directive options. It does not provide a percentage bonus merely for having a higher Hall level.

Recommended five-stage structure:

| Hall stage             | Functional unlock                             | Player-facing change                                                                 |
| ---------------------- | --------------------------------------------- | ------------------------------------------------------------------------------------ |
| I — Reclaimed Outpost  | Stronghold overview and Arsenal               | Choose a broad equipment category priority                                           |
| II — Fortified Hold    | Essence Conservatory                          | Existing Essence-related Soulstone constellations move into the Stronghold           |
| III — Established Seat | Cartographer's Lodge                          | Select a Dungeon-family Sigil survey and invest in Sigil research                    |
| IV — Great Stronghold  | War College and advanced Arsenal directives   | Select armor role or weapon handedness; manage combat-progression research           |
| V — Legendary Seat     | Highest research ranks and final visual state | Variant priority reaches its full authored strength; all facilities visibly complete |

Accomplishment gates should be broad alternatives, such as character level, regional progress, Dungeon mastery, Tower progress, or collection milestones. The Hall must not demand one narrow activity from every build.

### Facility research

**Soulstones become the Stronghold's research currency.** Existing ranks remain owned and effective. Cinders construct the physical estate; Soulstones improve what its facilities do. This reuses two currencies with clear identities and avoids a new Stronghold-only token.

Use the current five-rank Soulstone cost curve as the starting standard: `25 / 75 / 150 / 300 / 600`, or 1,150 Soulstones per completed research line. The exact economy requires telemetry, but one consistent curve makes the system readable.

### Persistent directives

A directive is an explicit player choice within a facility. Examples are “Prioritize Armor,” “Favor Heavy armor,” or “Survey Great Tree Sigils.” Directives are not bought repeatedly and do not expire.

The player may change a directive freely at the Stronghold. The choice applies only to future activity snapshots. This produces meaningful planning without a reset timer, weekly lockout, or recurring Cinder tax.

## 4. Functional Building Set

### Main Hall

Role: progression spine, current-directive summary, facility unlocks, and estate overview.

The overview should answer four questions immediately:

- What am I currently prioritizing?
- What effect does each priority have?
- What can I afford to improve next?
- What accomplishment unlocks the next facility or rank?

The Hall should not be an additional place to claim rewards. It records upgrades and configuration only.

### Arsenal

Role: direct the composition of Equipment rewards.

The Arsenal is the strongest launch building because the current acquisition path already rolls an Equipment category, then handedness for weapons, then a concrete definition. Armor archetypes also already expose `Heavy`, `Medium`, and `Light` roles. Variant application already selects among source-compatible Equipment styles.

Recommended directives:

1. **Acquisition Commission** — choose Weapons, Armor, or Jewelry.
2. **Craft Commission** — when the selected category is eligible, choose one of:
   - One-Handed or Two-Handed for Weapons;
   - Light, Medium, or Heavy for Armor;
   - Ring, Necklace, or Relic for Jewelry.
3. **Variant Commission** — choose one discovered Variant family, such as Fury, Arcane, Execution, Aegis, Warden, Endurance, Phoenix, Spirit, Primal, Venom, or Hive.

The initial tuning envelope should alter **selection weight**, not guarantee a result:

| Arsenal rank | Broad category weight | Subtype weight | Variant weight |
| -----------: | --------------------: | -------------: | -------------: |
|            1 |                 1.25x |         Locked |         Locked |
|            2 |                 1.50x |          1.25x |         Locked |
|            3 |                 1.75x |          1.50x |          1.50x |
|            4 |                 2.00x |          1.75x |          2.00x |
|            5 |                 2.25x |          2.00x |          3.00x |

These are starting values for simulation, not approved balance values. Weights are renormalized across eligible choices. With the current `0.40 / 0.35 / 0.25` category distribution, a 2.25x Armor weight produces roughly a 54.8% Armor share rather than a guarantee. The rest of the pool remains alive.

Rules:

- A priority never creates an item that the activity cannot normally award.
- If a target is absent from the source pool, its extra weight is redistributed among eligible options, matching the current selection behavior.
- Variant priority affects which compatible Variant is selected when a Variant roll succeeds; it does not initially increase total Variant chance.
- Guaranteed named rewards and hand-authored quest rewards ignore Arsenal directives.
- Area and Dungeon random discoveries use the same directive semantics.
- Directives are captured when the idle schedule or Dungeon run starts.

The first version should not add quality, rarity, or total Equipment drop-rate bonuses. Those multiply power and economy much faster than changing the composition of drops.

### Essence Conservatory

Role: house permanent Essence research and make the existing Essence hunt more directed.

The Conservatory absorbs the four currently active Essence Soulstone constellations without changing their IDs, ranks, or effects:

| Existing constellation |                            Existing maximum effect | Stronghold presentation |
| ---------------------- | -------------------------------------------------: | ----------------------- |
| Essence Resonance      |                  +15% relative Essence drop chance | Conservatory research   |
| Echo Memory            |                         +25% pity progression gain | Conservatory research   |
| Duplicate Echoes       |   10% extra-material chance on duplicate dismantle | Conservatory research   |
| Archive Focus          | +25% relative drop chance for the focused creature | Conservatory research   |

Creature Focus itself remains one source of truth. The Conservatory may show and change the same selected creature, but it must call the existing Creature Archive/Focus capability rather than creating a second focus setting.

The current base focus multiplier is already `3x`; the Stronghold should not add another independent focus multiplier. Its long-term investment already exists in Archive Focus and Echo Memory. Future Conservatory research should favor new decisions—such as choosing between stronger focus and broader family coverage—before adding more flat drop chance.

### Cartographer's Lodge

Role: improve and direct Sigil acquisition for Dungeons.

Recommended directives and research:

1. **Active Survey** — select one discovered Dungeon family in the current region.
2. **Sigil Tracing** — a capped relative increase to ordinary Sigil acquisition.
3. **Route Familiarity** — increases the share of eligible Sigil rewards converted to the surveyed family.

Starting tuning envelope:

| Lodge rank | Relative Sigil chance | Surveyed-family share when two families are eligible |
| ---------: | --------------------: | ---------------------------------------------------: |
|          1 |                   +5% |                                                  55% |
|          2 |                  +10% |                                                  60% |
|          3 |                  +15% |                                                  65% |
|          4 |                  +20% |                                                  70% |
|          5 |                  +25% |                                                  75% |

The current regional Sigil chance is approximately `0.00023148` per victorious eligible encounter and a region currently contains two Sigil families. The proposed bonus is **relative**, not percentage points. Rank 5 therefore multiplies that chance by `1.25`; it does not turn it into 25%.

Before implementation, this must be reconciled with the saved equipment-progression cohort's selected-family counter described in the Soulstone analysis. The Stronghold must have one Sigil acquisition policy:

- if deterministic progress is the chosen live design, Lodge ranks shorten its authored threshold and the Active Survey selects its family;
- if random regional drops remain live, Lodge ranks modify the chance and family selection shown above.

Do not run both systems simultaneously. That would silently double Sigil income and make the feature impossible to balance.

### War College

Role: hold bounded combat-progression research without adding raw combat stats.

Move the two current combat Soulstone constellations here:

- **Battle Lessons:** up to +7.5% combat experience from idle and Dungeon victories.
- **Survival Notes:** up to 50% idle-defeat experience retention.

The War College should also surface where each effect applies. It should not offer Strength, Power, Health, Armor, critical chance, or damage multipliers. Future research should continue to improve recovery, learning, testing, and planning rather than base combat throughput.

### Cartographer's Rest Site research

Move the current **Rest Site Satchel** constellation into the Cartographer's Lodge. It already provides up to +10% Dungeon Rest Site reward retention. That creates a coherent “prepare routes and preserve expedition value” identity without another building.

### Gallery of Legacy

Role: optional prestige, history, and visible completion.

The Gallery still belongs in the long-term feature, but it is no longer the mechanical center. Research ranks, mastered Variant commissions, surveyed Dungeon families, and Essence discoveries should visibly decorate their respective buildings. The estate becomes impressive because it has been developed, not because the player separately grinds decoration progress.

## 5. Removing the Standalone Soulstones Page

The standalone Soulstones page should be removed from navigation, but its currency and progression should not be deleted.

### Player-facing change

- Remove **Soulstones — Permanent upgrades** from the Character sidebar.
- Add **Stronghold** as its own primary destination.
- Group Soulstone research inside the facility that owns the mechanic.
- Show the Soulstone balance and total affordable upgrades in the Stronghold header.
- Preserve upgrade, reset, refund, requirement, and applicability explanations.
- Redirect the old `/game/character/soulstone-archive` route to the Stronghold overview or its Research view.
- Update Prophecy navigation that currently points Soul Archive objectives at the old Soulstone route.

### Data-preserving migration

The safest first release is a **UI and information-architecture migration**, not a persistence rewrite:

- keep `CharacterSoulstoneUpgrade` rows;
- keep all current definition IDs;
- keep the existing purchase/reset API during transition;
- add facility ownership metadata to definitions or map the current branches in the Stronghold client;
- reuse the existing upgrade-card behavior inside Stronghold facility views;
- do not refund active upgrades merely because their presentation moved.

The existing active mapping is straightforward:

| Current branch               | Stronghold facility  |
| ---------------------------- | -------------------- |
| EssenceArchive               | Essence Conservatory |
| CombatProgression            | War College          |
| Dungeons / Rest Site Satchel | Cartographer's Lodge |

Retired constellations remain retired and refundable under their existing rules. Do not reactivate the retired `Sigil Traces` definition as a shortcut. Add a new Stronghold research definition only after the live Sigil acquisition model is resolved.

Once the Stronghold schema is stable, the API naming may change from `SoulstoneUpgrade` to `StrongholdResearch`. That cleanup is optional and should not be bundled with the player-facing move.

## 6. How Players Keep Investing

The system supports sustained focus through breadth, rank caps, and changing goals:

- A player chasing a set invests in Arsenal ranks and selects a Variant commission.
- A player preparing for a Dungeon surveys its Sigil family and develops the Lodge.
- A player hunting a rare Essence invests in Conservatory research and sets Creature Focus.
- A leveling player invests in War College research.
- A completionist eventually develops all facilities, but still changes directives as goals change.

This is healthier than an infinite Stronghold level. Infinite numerical growth eventually becomes either negligible or the dominant source of power. New regions and parent systems can add authored research lines and visible building additions over time.

At the current five-rank cost curve, the seven active Soulstone lines contain 8,050 Soulstones of investment. Four additional five-rank lines for Arsenal and Sigil functionality would add another 4,600, for 12,650 total before future expansion. This is already a meaningful long-term track; actual pacing must be tested against Soulstone income.

## 7. Building Slots and Specialization

Functional buildings should **not** compete for active plots. Once benefits are real, disabling a completed Arsenal to activate an Essence building becomes punishment and encourages pre-activity swapping.

Use two different concepts:

- **Functional facilities** are permanently active once constructed. Investment choices and directives create specialization.
- **Showcase plots** determine which buildings or wings dominate the estate's visual presentation. They have no effect on mechanics.

This deliberately revises the earlier two-active-wing recommendation. Limited functional plots work for cosmetics; they are poor design for persistent loot benefits.

## 8. Core Loop

The revised loop is:

```text
Play combat, Dungeons, Tower, and collection systems
→ earn Cinders, Soulstones, unlocks, and normal rewards
→ restore a facility or buy a research rank
→ choose a persistent priority for the next goal
→ start an activity with that Stronghold configuration snapshotted
→ receive a more directed or modestly improved reward stream
→ change focus when the player's goal changes
```

The player visits when making a progression decision, not to clear a notification. A good revisit cadence is irregular: several visits while planning a new build, then none while that plan runs.

## 9. Power and Economy Position

The revised recommendation is **controlled indirect power**, not “no power.”

Stronghold progression will increase player effectiveness by improving acquisition, experience, and retention. That is intentional. The balance safeguard is to keep the strongest identity in **agency**:

- selected drops become more likely, but other drops remain possible;
- total equipment volume, rarity, and quality are unchanged at launch;
- Essence bonuses preserve their existing capped magnitudes;
- Sigil acquisition receives a modest relative improvement, not a direct percentage-point bonus;
- no building generates resources passively;
- no direct combat-stat multiplier is introduced.

This makes the Stronghold important without requiring every balance formula to include a Stronghold combat coefficient.

## 10. MVP Recommendation

The smallest version that proves the new idea is:

1. **Stronghold shell and Main Hall** with current directives, upgrades, facility access, and a simple five-stage visual state.
2. **Soulstone rehoming**: move the seven active constellation lines into Essence Conservatory, War College, and Cartographer's Lodge; remove the standalone sidebar page and redirect its route.
3. **Arsenal vertical slice**:
   - build/unlock state;
   - one five-rank targeting research line;
   - broad category commission;
   - one subtype commission;
   - both area and Dungeon random Equipment selection;
   - activity-start snapshot semantics.
4. **Cartographer's Lodge vertical slice** after choosing the canonical Sigil model:
   - selected Dungeon family;
   - one five-rank Sigil acquisition line;
   - one family-targeting line or deterministic-threshold improvement.
5. **No Gallery system, visitors, expeditions, building timers, or public estate in the first mechanical release.** A simple visual response to facility rank is enough to validate the headquarters fantasy.

This MVP is intentionally narrower in art and broader in actual utility than the earlier proposal.

## 11. Implementation Shape

The feature should follow existing boundaries:

- **Core:** Stronghold aggregate/rules, directive value objects, eligibility and weight-adjustment rules, snapshot contract.
- **Application:** queries and commands for overview, Hall upgrades, facility research, and directive selection.
- **Infrastructure:** EF persistence, integration into Equipment and Sigil acquisition, projection of existing Soulstone bonuses.
- **API:** thin endpoints over commands and queries.
- **Presentation:** Stronghold shell, facility views, directive selectors, reused Soulstone research cards, and old-route redirect.

Likely new persistent state:

```text
CharacterStronghold
- CharacterId
- HallStage
- Version

CharacterStrongholdFacility
- CharacterId
- FacilityId
- Rank / construction state

CharacterStrongholdDirective
- CharacterId
- DirectiveKind
- SelectedValue
- Revision
```

If Hall/facility ranks are definition-driven, store stable IDs and ranks rather than one database column per building. Do not copy Soulstone levels into these tables in the first migration.

Reward processors should consume a read-only, resolved snapshot such as:

```text
StrongholdRewardDirectiveSnapshot
- EquipmentCategory
- EquipmentSubtype
- EquipmentVariantStyleId
- EquipmentWeightMultipliers
- SigilFamilyId
- SigilChanceMultiplier or deterministic-threshold modifier
- StrongholdRevision
```

Store the relevant snapshot or revision on long-running activities. Deterministic reward identities must incorporate the captured configuration only if the configuration changes the roll outcome; never query today's directive while resolving an activity started under an older one.

## 12. Verification and Balance Requirements

Before release, verify:

- existing Soulstone ranks, costs, refunds, and bonuses are unchanged after the page move;
- old route and Prophecy links resolve to the new destination;
- category, subtype, and Variant weights renormalize correctly;
- ineligible priorities fall back without exceptions or empty pools;
- area and Dungeon paths interpret priorities identically;
- an ongoing idle schedule or Dungeon cannot be rerolled by changing a directive;
- guaranteed rewards ignore targeting;
- the Sigil system has exactly one random or deterministic acquisition policy;
- no Stronghold benefit applies when its facility or rank is unavailable;
- effect copy reports relative percentages accurately.

Run seeded simulations for every region/source combination. Measure both the selected result share and the total item/Sigil volume. The selected share should move materially; total Equipment volume must remain unchanged.

Key telemetry:

- percentage of eligible players selecting each directive;
- directive change frequency;
- actual versus expected targeted-drop share;
- Soulstones earned and spent per week;
- time to each facility rank;
- Sigils and Equipment per active hour by Stronghold rank;
- whether one Variant or armor role becomes the permanent default for most players.

If one directive exceeds roughly 60–70% adoption across unrelated builds, inspect whether the underlying item balance—not the Stronghold—is forcing the choice.

## 13. Migration, Configuration, and Deployment Implications

The UI-only Soulstone move can ship without a database migration if the new Stronghold shell initially derives research from existing state.

Mechanical facilities will require at least one EF Core migration for Stronghold ownership and directives. The migration should:

- create one Stronghold per existing character lazily or deterministically;
- preserve all `CharacterSoulstoneUpgrade` rows;
- avoid assigning a directive that silently changes historical rewards;
- default new players to no priority until they make an explicit choice;
- keep stable IDs for future building renames.

Configuration additions should be versioned and data-driven, including facility definitions, Hall gates, directive multipliers, and eligible targets. Equipment and Sigil integrations should launch behind independent feature flags so their economy effects can be observed separately.

No shared or production migration should be applied as part of design work.

## 14. Final Revised Vision

The Stronghold is the player's permanent progression headquarters. Cinders restore and expand it; Soulstones research its capabilities. Each functional building improves a real part of play, primarily by letting the player say what they are trying to obtain next.

The Arsenal directs Equipment category, archetype, and Variant selection. The Essence Conservatory houses and clarifies permanent Essence research. The Cartographer's Lodge helps pursue a chosen Dungeon and its Sigil. The War College carries bounded combat-learning benefits. Existing Soulstone investment moves into these places intact, and the standalone Soulstones page disappears.

The player does not visit to collect rent, restart a timer, or refresh a blessing. They visit to make a decision. The estate then matters everywhere else in the game because that decision travels with them.

That is the balance to protect: **mechanically valuable, persistently investable, player-directed, and free of maintenance chores.**

## Repository Evidence Used for This Revision

- `Data/progression/soulstone-upgrades.json` currently defines seven active five-rank constellations: four Essence, two combat, and one Dungeon line.
- `SoulstoneBonusProvider` already projects owned ranks into the shared bonus system.
- `EquipmentSelectionWeights` already performs eligible, renormalized category and handedness selection.
- Equipment archetypes already encode Light, Medium, and Heavy roles.
- `CombatAcquisitionRewardProcessor` and `EquipmentAcquisitionService` provide the area and Dungeon selection points for Stronghold directives.
- `EquipmentBlueprintCatalog.RollVariant` already selects compatible Variant styles and supplies the insertion point for Variant weighting.
- `EssenceSystemService` already combines base chance, Creature Focus, resonance, Soulstone drop-rate research, and pity research.
- `CreatureFocusRules` already provides a `3x` focused-creature base chance, so another independent Stronghold focus multiplier is unnecessary.
- `equipment-ordinary.v1.json` currently defines regional equipment weights and Sigil chances.
- The Angular Character routes, sidebar, and Prophecy navigation contain direct references to `/game/character/soulstone-archive` that must be redirected.
