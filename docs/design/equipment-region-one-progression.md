# Equipment progression: Region 1 progression without merchant gear

Updated: 3 September 2026. Target service: the primary game under LL/.

Status: the Tier 1 / Region 1 loop is implemented with provisional source/economy values. The five equipment capabilities default to enabled. This document supplies design inputs, not deployed-state or full-game balance certification. Gear-selling merchants remain excluded.

Implementation order was subsequently updated: build the complete loop using these provisional inputs, then balance content. The initial analysis and its limitations remain useful evidence; they do not block implementation. Current progress is tracked in [implementation status](equipment-implementation-status.md).

The [equipment specification](equipment-specification.md) remains the overall contract. [Design inputs](equipment-region-one-inputs.json) hold the candidate numbers and catalogs; the [generated report](equipment-region-one-balance-report.md) contains complete archetype, area, style, named-target, cost and pacing tables. The report is an offline proposal analysis, not an implementation ledger. Runtime catalogs now exist under `API.LL/Data/equipment` for starters, styles, Forge prices, named items, protection pools, ordinary acquisition and the retirement inventory.

Current implementation includes starter grants, Forge ranks/styles, six protected dungeon pools with 16 named identities, recovery, unbound transfers and ordinary equipment/Scrap/selected-family sigil income across the ten Shenic areas. Current quests, guild and vendor rewards, inventory and help use this loop directly. Crafting/gathering, queued tempering and Alpha compatibility have been removed. Later-region content and integrated balance remain open; raid redesign and further LiveOps work remain deferred. See [implementation status](equipment-implementation-status.md) and [cleanup details](equipment-post-alpha-cleanup.md).

## 1. Scope and decisions

- Cover all 31 currently enabled combat archetypes, all 10 Shenic areas, Goblin Mines and Forgotten Catacombs, and their four core Blueprint styles.
- Keep Shenic equipment at Tier 1, equip level 1. Area difficulty 1–10 and dungeon difficulty I–III do not produce equipment Tiers 1–10 or 1–3.
- Give a player a complete usable plain loadout through onboarding choices. Ordinary combat supplies alternate archetypes and investment resources; dungeons supply recognizable named rewards and styles.
- Retain the existing Essence first hunt, Essence rewards, dungeon access items and non-equipment currencies. Blueprint knowledge remains character-wide and reusable.
- Use the current recipe output IDs, slot roles, initial stat profiles, hand rules and existing style names as content anchors. These do not imply that new equipment remains attached to production recipes.
- Keep random gear sparse. Baseline coverage, changes of weapon/armor family, and recovery from losing equipment cannot depend on a lucky drop or another player's shop listing.

This is the Region 1 slice. Meran sources, remaining styles, later-tier prices/recovery and wider economy coverage still need their own contracts and tables. Alpha conversion mappings are no longer planned work.

## 2. Starter equipment and recovery

All baseline rewards are Common, Tier 1, rank 0, plain, personally bound, with zero base salvage and zero paid investment. A character receives one selected loadout, not all 31 archetypes. Every ordinary baseline piece can still be tempered to rank 5 and use compatible learned styles.

| Point in the existing quest chain | Implemented equipment progression behavior                                                                                                  | Prerequisite and completion rule                                                                                                                             |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `quest.onboarding.training_day`   | Preserve the selected first hunt and its Essence reward                                                                      | Existing accessible training encounter; no new gear requirement                                                                                              |
| `quest.onboarding.soul_archive`   | Replace its ore/wood reward with 500 Cinders and the First Weapon choice becoming available                                  | Absorb and attune the first Essence as today                                                                                                                 |
| `quest.onboarding.first_weapon`   | On claiming the equipment choice, grant legal hands plus Head, Chest and Legs; completion grants 10 Scrap and the three accessories once | Complete Soul Archive; equip the chosen hands and armor. The grants precede the equip objectives. Remove `EquipmentCrafted` and `mustBeCrafted` requirements |
| `quest.onboarding.tools_of_trade` | Ready for the Road: equip the `band`, `amulet` and `vial` automatically granted when First Weapon completes                           | Complete First Weapon; equip these three accessories. Replace gathering-tool objectives. Keep the stable quest ID for existing area references               |
| `quest.region01.into_lumo_ruins`  | Preserve the first Lumo victory                                                                                              | Complete Ready for the Road; the full baseline loadout is already available                                                                                  |

Hand choices must support all existing legal families: any one-handed weapon with an offhand, two one-handed weapons (including two copies of the same eligible archetype), or one two-handed weapon. A two-handed weapon is one instance occupying both hand slots. Armor choices are independent per slot and cover Heavy, Medium, Light and Cloth; do not force a hidden class or uniform armor set. Use sensible suggested kits in the UI while retaining these choices.

Before Lumo, the player has seven actual items with a two-handed weapon or eight with separate hands. The initial 10 Scrap and 500 Cinders cover two rank-1 purchases; no first improvement requires scrapping the only usable weapon. Forge onboarding can recommend an improvement, but leaving training does not require spending these grants.

**Recovery contract:** expose a reclaim action alongside earned baseline equipment rewards in the quest journal. The saved equipment grant establishes entitlement even before the quest's equip objectives are complete. Recovery restores the originally chosen baseline archetype at rank 0, plain, with no cost and no combat requirement. It does not restore a lost rank, style, named identity or paid basis. Reclaim only when an equivalent personally owned baseline copy is absent, counting inventory, equipped and pending rewards; two copies are allowed only where the original dual-wield choice granted two. Serialize the entitlement check with the pending award so retries and concurrent requests cannot multiply copies. Persist which items were granted; never infer the entitlement from the current quest definition after a content update.

**Implemented backend scope:** the recovery API uses the saved starter grant as entitlement, including when equipment is lost before the later equip objectives can be completed. It restores only missing original copies, preserves frozen baseline stats and records an operation receipt for retries. Earned plain targets now retain separate recovery rights, exposed beside starter recovery in Equipment & Forge. The Quest Journal links to equipment rewards and recovery; objectives lead to the reward tab and inventory. Equipment help and profession navigation now use the equipment progression path; broader shared-system help remains cutover work.

The same rule can recover plain archetypes subsequently earned by the elective route below. Recovery itself grants no XP, Cinders, Scrap, quest progress, sale value or guild donation rights. A free piece that later receives real paid investment can refund only that actual paid Scrap fraction. This closes the reclaim/salvage loop while allowing players to recover from a completely empty loadout. It is a quest reward recovery action, not merchant stock or a new currency exchange.

## 3. Ordinary content and alternate builds

The report lists every archetype and area explicitly. Each area offers the full plain Tier 1 archetype list, with no native style and no later-band gear. Named targets and Blueprint books belong to their authored dungeon source instead.

| Route                     | Candidate award                                                                                         | Eligibility and protection                                                                                                                                                        |
| ------------------------- | ------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Random ordinary discovery | One random plain Tier 1 rank-0 item; uniform choice among the 31 authored archetypes; 0.03% per victory | Regional combat only, excluding training. Unbound until use; base salvage 1 Scrap                                                                                                 |
| Elective plain target     | One exact chosen archetype, Tier 1 rank 0                                                               | Select after entering Lumo; 360 eligible Shenic victories, no random requirement. Bound, zero base salvage. Selection is optional; no active target means no automatic extra gear |
| Ordinary Scrap            | 36/day in Lumo, rising by 2 per area to 54/day in Duskmire at perfect ten-second victories              | Each eligible victory accrues `areaScrapPerPerfectDay / 8640`; persist the fractional remainder so online/offline batching gives the same total                                   |
| Area quest milestone      | 2 Scrap once for each of the 10 Shenic area quests                                                      | Replace ordinary ore/wood/hide/equipment catalyst rewards; retain Essence Tokens, Monster Cores and unrelated rewards                                                             |

The plain target uses a Region 1/Tier 1 protection record and the selected exact archetype. Switching archetypes preserves progress within that pool; the choice at an eligible encounter's commitment boundary determines its award. Freeze a pending reward descriptor and consume exactly 360 victories once; unrelated random drops do not cancel the request. Pause this elective selection after securing one item and let the player choose another, avoiding repeated unwanted equipment while offline. Later victories still earn ordinary rewards.

All 31 archetypes are selectable, including a second one-handed copy for dual wield. This supports build changes without making players clear dungeons to obtain a basic wand, shield or staff. At perfect wins a new plain target takes one credited combat hour; rank/style investment remains separate.

Carry the existing 24-hour offline cap into modeling as a runtime default, not a proposed increase. Low-frequency claims must retain earned fractional Scrap, target progress and pending items; they do not bypass the existing cap.

**Implemented backend scope:** ordinary discoveries, one-shot 360-victory targets and fractional Scrap now run through the shared idle reward pipeline. Source selection settles due combat under the old choices before changing them; a remaining backlog blocks the change. Frozen target descriptors/thresholds, operation receipts and a per-pool processed-boundary checkpoint prevent rerolls, duplicate awards and accidental rearming on retries. Combat summaries preserve individual equipment progression item IDs and binding. The ten versioned area quests now grant two Scrap each, and secured elective targets record durable recovery rights. Characters using the Forge no longer earn gathering rewards/XP or retired equipment/materials from combat and dungeon reward paths. Legacy characters retain their original paths. Subsequent shared-reward integrations are recorded in the ledger; the 3 September milestone removes random-sigil overlap and preserves the counter and earned sigils.

Increasing rewards by area discourages farming Lumo at equal win rates, but does not prove later areas are always optimal. For example, 54 Scrap/day at a 60% win rate loses to Lumo's 36/day at 100%. Content difficulty, obsolete-content eligibility and the Region 2 reward scale need combat validation before the anti-backtracking requirement can be considered satisfied. Do not invent a broad reward penalty in this first slice.

## 4. Dungeon access and protected named rewards

Repeat sigils come from selected-family ordinary-combat progress. The superseded random-sigil path and Sigil Traces constellation/refund support have been removed. First-entry grants and current pending rewards retain their normal behavior.

| Family              | Proposed first access                                                                                         | Styles already awarded by difficulty-I first clear | New named pool                           |
| ------------------- | ------------------------------------------------------------------------------------------------------------- | -------------------------------------------------- | ---------------------------------------- |
| Goblin Mines        | Level 5 and completed `quest.shenic.trial_of_lumo`; grant one `sigil_goblin_mines` with that quest            | Fury, Phoenix                                      | Eight exact targets in the inputs/report |
| Forgotten Catacombs | Level 15 and completed `quest.shenic.crystal_currents`; grant one `sigil_forgotten_catacombs` with that quest | Arcane, Endurance                                  | Eight exact targets in the inputs/report |

These level/quest gates are now authored in `equipment-protection-pools.v1.json` and enforced on previews, target selection and server entry when protected acquisition is enabled. Ordinary sigil selection uses the same level/quest requirements from `equipment-ordinary.v1.json`. Existing sigil consumption, previous-difficulty and Tower checks remain. The deterministic repeat-access floor below is implemented; first-access sigil grants are implemented in the versioned equipment progression quests.

Difficulty II requires a completed I of the same family, and III requires II. Keep all existing non-equipment entry rules. Each difficulty offers that family's same Tier 1 rank-1 named pool; harder difficulty does not secretly grant a stronger equipment tier or exclusive ordinary cap. Preserve unrelated authored rewards, subject to retirement of gathering/catalyst outputs.

**Repeat access candidate:** while a player chooses one unlocked family, every 4,320 eligible Shenic victories awards one bound sigil for it. This is 12 credited hours at perfect wins. Persist progress, freeze the selection for each eligible encounter, and award through the existing inventory/pending-reward transaction patterns. Switching between eligible Region 1 families preserves partial access progress, but a locked family cannot be selected. No new tradeable currency or profession is introduced. This access progress is separate from equipment pity: failed dungeon attempts never count as successful equipment clears. One selected family receives the deterministic income; the model does not grant that rate independently to both families.

The planned first-entry grant enables the initial attempt; ordinary combat funds another after failure. The implemented repeat route requires one selected unlocked family and preserves a shared counter across family changes. It produces bound sigils every 4,320 eligible victories and continues after each award. First-entry grants and the repeat route are implemented; no legacy cohort or quest publication flag is required.

Each named definition uses the stable content ID `model_e.r1.<familyId>.<itemId>.<styleId>` with the exact IDs in the input file, preserving capitalization. This historical key remains for saved-item compatibility; it is not a display name. See the [naming and compatibility notes](../engineering/equipment-naming-and-compatibility.md). Its display name is composed from the existing archetype and style name format. All 16 are Rare, Tier 1, rank 1. These names carry no extra budget beyond active style/rank.

- Select one named target before starting the run. Freeze definition version and eligibility at run commitment.
- The first successful difficulty-I clear per family grants that chosen item and both existing core Blueprint books once. This uses the one matching equipment award opportunity; it does not also roll a second matching item. Clear any corresponding difficulty-I progress when durably awarded.
- Subsequent successful clears have a 20% matching chance, with one matching item guaranteed on clear 8. Do not add an independent second named-equipment roll. Unrelated Essence/resource rewards continue.
- Keep protection separate by character/family/difficulty/equipment tier. Switching targets in the same pool preserves progress; switching difficulties retains each record separately.
- First-clear and protected copies bind on award and have zero base salvage. A randomly earned matching copy remains unbound until use and has base salvage 2 Scrap. All awarded rank-1 copies start with zero refundable paid investment.
- Pending awards retain identity, rank, native/active style, provenance, binding, salvage policy and the protection consequence. Repeated claims must not award twice or reset newer progress.
- Each eligible completion grants 4 Scrap, independently of the matching-item roll. No raid, guild, Tower or PvP participation is required.

**Implemented backend scope:** the six pools follow these named-reward rules using frozen run commitments and durable completion receipts. Selection is optional: a run committed without a target secures only the four completion Scrap and does not advance or reset equipment protection. First-clear equipment requires a selected target; the existing first-clear Blueprint books are unaffected. Claims restore the frozen item and leave subsequent protection progress intact. New commitments require the feature flag; already committed rewards remain claimable after it is disabled.

The report computes 4.1611 mean clears and eight maximum successful clears for repeat targets. Calendar days are conditional on successful clears and actual access; no claim is made that failed attempts or a slow player's sessions fit inside eight days. Prophecy fragments and their ten-fragment assembly route can accelerate access but are not required by the deterministic route. Equipment progression analysis now assumes zero random sigils; no other provisional rate is changed.

## 5. Styles, sets and other sources

Retain Fury and Phoenix from Goblin Mines, Arcane and Endurance from Forgotten Catacombs. The full compatibility and within-style stat weights are generated from the existing Blueprint definitions. A learned book applies across every compatible archetype, even if that archetype is absent from the dungeon's named pool. This makes all compatible set pieces deterministically accessible through plain gear plus learning the style.

Use 85% archetype / 15% style allocation for the prototype. Do not carry forward the old +20% additive Blueprint budget. Unstyled gear uses 100% archetype allocation. Each rank adds 4% of the tier/slot baseline through the same allocation, with actual caps/rounding/redistribution evaluated by the future canonical evaluator.

Fury and Arcane have 2/4-piece thresholds; Phoenix and Endurance have 2/4/6. The validator confirms threshold reachability with the compatible slots, counting a two-handed weapon once. Existing set attributes and abilities are reference candidates, not combat-approved extra power. Style changes replace membership rather than stack original and new sets. Native-style restoration remains available on the original named item without learned knowledge.

| Other source                             | Decision for this Region 1 ordinary-equipment slice                                                                                                                                  |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Tangled Cave / Great Tree                | Currently authored as Region 2. Their seven styles are deferred to that region; do not require Aegis, Warden, Spirit, Primal, Execution, Venom or Hive for Shenic baseline viability |
| Region boss: The Mad King                | Current `rewardsEnabled` is false and requires Tower Floor 10. Do not count this boss in baseline access or Scrap income                                                             |
| World Tower                              | Preserve current progression/Tokens; propose no additional ordinary gear or Scrap output in this first slice. Rebalance its reference loadouts for equipment progression before cutover            |
| Raids                                    | Preserve optional trophy/Blueprint routes; Raidforged and Gravebound remain outside ordinary Region 1 completion requirements. No new gear-selling merchant is introduced            |
| PvP / guilds / achievements / Prophecies | No new equipment/stat requirement or mandatory material source. Audit retired gathering/crafting objectives and rewards during integration; existing unaffected rewards remain       |

## 6. Economy recommendation and limits

Keep rank Scrap prices **5 / 10 / 20 / 40 / 80**, and prototype the higher ordinary supply **36–54/day across Shenic**, rather than locking the original 20–30/day candidate. Price Cinder ranks at **250 / 500 / 1,000 / 2,000 / 4,000**; price a real style change at **250 Cinders**, with the specification's one free first application per learned style and free no-op behavior. These are Tier 1 prices only.

At a reference 45 Scrap/day, eight rank-3 pieces take 6.22 days from rank 0. Six rank-3 pieces plus two rank-5 favorites take 11.56 days; maxing all eight takes 27.56 days. A daily player clearing one dungeon per day receives another 4 Scrap/day, reducing the mixed goal to about 10.61 days before finite grants or salvage. This fits selective investment better than the original supply's 17–26 days for the mixed loadout.

There are important limits:

1. **The current level curve is faster than a complete investment cycle.** The optimistic XP-only model reaches level 30 in 54.67 credited hours, with about 100 ordinary Scrap. It reaches level 50 in 205.77 hours, with about 424. These are not measured completion times: chapter gates, player delays, dungeon time and extra XP change them. The focused-beta chain currently ends at Heart of the Hollow around level 30. Do not make rank 3 across the loadout a requirement for that encounter. Prototype starter/rank-1 builds, selective rank-2/3 investments and four available styles first.
2. **Low-frequency play still takes longer.** A modeled 48-hour claimant with 80% wins gets 40% of perfect daily idle income under the current cap. With 0.25 dungeon clears/day, the mixed goal takes about 27.37 days. Its content progression also slows; selective ranks should remain optional. The existing cap is not changed by this proposal.
3. **Cinder affordability is conditional.** The model reserves 75% of Lumo's configured Cinder income for other uses. The remaining 6,480/day funds the 26,000-Cinder mixed eight-item loadout in about four ideal days, so Scrap dominates under that assumption. This is not a measured household budget. Compare concurrent Essence spending, marketplace fees and existing sinks before accepting prices.
4. **Hand economics need review.** A seven-item two-handed loadout is cheaper to improve under equal per-item prices than an eight-item loadout. Compare combined hand budgets, set counts and outcome parity before deciding whether prices should follow hand budget share. No new price multiplier is assumed here.
5. **The 25% next-band growth is still unvalidated.** Tier 2 rank 0 would be only 4.17% above Tier 1 rank 5 in budget. The existing curve grows by about 35.3%, and normalized ratings complicate actual combat. Keep the Level-50 Meran entry/Tier-2 equip boundary as a follow-on input; also resolve the current expected-tier helper switching at level 51. Do not adopt a new growth curve solely from the ratio.

Refund exactly `floor(actual paid Scrap * 0.5)` plus eligible base salvage. A rank-1 find upgraded to rank 3 pays 30 Scrap and recovers 15; its free rank does not add another refund. Reclaim/first-clear/protected items have no base value. Cinders and style fees are never refunded. The report excludes salvage income so it does not depend on destroying the player's only gear.

## 7. Quest integration and remaining dependencies

Current quest IDs remain stable, with one retained version per quest. Superseded Alpha versions and profession quests were deleted. The one-time cleanup migration removes their saved progress and objectives; current versions retain their progress.

| Existing dependency                                                                      | equipment progression replacement                                                                                                                                                                                                                |
| ---------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| First Weapon craft + crafted-only equip                                                  | Grant choices before equip; accept the chosen canonical item                                                                                                                                                                        |
| Tools of the Trade gathering-tool equip                                                  | Ready for the Road accessory grant/equip                                                                                                                                                                                            |
| Soul Archive material reward                                                             | Starter Cinders; retain Essence objectives                                                                                                                                                                                          |
| Restless Dead `forge_an_answer` and chapter-II crafting copy                             | Request a selected plain item through ordinary combat and equip that replacement; accept an already completed valid acquisition/equip rather than require wasteful repeats. Preserve its Moonlit Graves wins and level-20 objective |
| Blood in the Grove dungeon gate                                                          | Retain Goblin Mines I clear; fund initial access from Trial of Lumo, before the gated quest                                                                                                                                         |
| Heart of the Hollow dungeon gate                                                         | Retain Forgotten Catacombs I clear; fund initial access from Crystal Currents, before the gated quest                                                                                                                               |
| Area material/catalyst grants                                                            | Two Scrap per area quest; preserve unrelated Essence Tokens and Core rewards                                                                                                                                                        |
| Removed profession content | Seven crafting/gathering side quests, profession Prophecies/achievements and obsolete caches are deleted. Current guild and event rewards use supported objectives and resources directly. Blood Grove Veteran v2 awards Scrap. The quest cleanup migration deletes obsolete saved quest versions; no Alpha conversion remains pending. |

The current catalog contains 29 regular quests, including the equipment-based main path and Blood Grove Veteran v2. Profession quests, Prophecies/achievements and obsolete Soulstone content are removed; current rewards are authored directly. The quest-integration flag, cohort replacements and refund adapters are gone. The [quest flow guide](../../LEGENDSLEGACY_QUEST_FLOW.md) describes the current behavior and saved-progress cleanup.

## 8. Verification and next implementation milestone

Run with Python 3.10+ and the standard library only:

```powershell
python build/analyze-equipment.py
python build/analyze-equipment.py --check
```

If Python is not on PATH, use the bundled runtime's full path returned by workspace dependency discovery. No packages, databases, network or production services are needed. The generator only writes the report; `--check` is read-only and fails on invalid references, missing coverage, incompatible targets, unreachable set thresholds or stale report content.

The analysis checks existing item/recipe/style/set references, complete Region 1 area/family coverage, baseline quest anchors, first-clear Blueprint sources, and the first-access quest for an immediate circular dungeon dependency. It calculates exact capped-geometric target waits, full-loadout costs, scenario income and an XP-only progression cross-check. It does not prove the whole quest graph or encounter viability. Source fingerprints make the content snapshot reviewable.

Runtime evidence anchors:

- [Equipment tier curve](../../LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentTierBudgetCurve.cs): current budget growth and level boundaries.
- [Area reward rates](../../LL/src/Infrastructure/Service/Services.LL/Regions/JsonAreaExperienceBalanceProvider.cs): configured XP/Cinder targets and per-encounter rounding.
- [Character XP formula](../../LL/src/Core/Domain/Models/Progression/CharacterExperienceCurveSettings.cs): upward-rounded quadratic level costs.
- [Combat acquisition service](../../LL/src/Infrastructure/Service/Services.LL/Items/CombatAcquisitionService.cs): current selected-family sigil progress; the superseded random-sigil calculator was removed.
- [Idle options](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/IdleCombatProgressionOptions.cs): default encounter cadence/offline cap; deployed overrides are not inspected.
- [Dungeon materialization](../../LL/src/Infrastructure/Service/Services.LL/JsonDefinitions/Dungeons/DungeonDefinitionMaterializer.cs) and [access policy](../../LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonAccessPolicy.cs): difficulty progression and current entry checks.
- [Set resolver](../../LL/src/Core/Domain/Models/Items/Equipments/Sets/EquipmentSetBonusResolver.cs): unique-instance set counts.

**Next implementation work:** extend later-region/tier sources, prices, styles and recovery coverage, then validate the integrated loop. Compare plain/rank-1 starter kits against early areas and Goblin Mines, rank-2/3 builds against Catacombs and the level-30 endpoint, and later investments against late Shenic/Tower. Exercise hand configurations, armor roles, core styles, caps and set effects. Use this evidence to revise provisional supply and prices; run backend correctness checks through build/run-tests.ps1.

Integrated validation remains open for combat outcomes, measured Cinder sinks, quest-gated time to each endpoint, later-band content and anti-backtracking behavior. The acquisition/economy tables remain provisional inputs.

The original analysis generated no migration. Later equipment schema and cleanup migrations are listed in the [status ledger](equipment-implementation-status.md). The API applies pending migrations on startup. The task did not apply them to the game database or deploy changes; Alpha conversion rehearsal is no longer a prerequisite.

**3 September cleanup follow-up:** Champion Market, tournament, guild and other supported rewards now grant authored current resources. Held catalyst-cache conversion and its adapters were removed with the obsolete content. Tournament storage records TemperedScrap directly. See the [cleanup record](equipment-post-alpha-cleanup.md).
