# Tower equipment supplies — 28 September 2026

**Withdrawn on 29 September at the user's direction.** Guaranteed selectable, pre-ranked dungeon supplies were an unsupported economy choice. Normal dungeon completion no longer invokes this reward path, its issuer/catalog are no longer registered by the game, and the legacy option defaults off. The [withdrawal report](Tower-Supply-Withdrawal-20260929.md) supersedes the gameplay and rollout recommendations below. This document and the frozen studies remain a historical record; supply-funded progression results must not be presented as ordinary player acquisition evidence.

For continuation, start with the [current handoff](Tower-Continuation-Handoff-20260928.md). This report records implemented local behavior and its original verification. The subsequent [acquisition and earned-progression study](Tower-Acquisition-Progression-20260928.md) now supplies a reproducible conditional model and production preparation; observed normal-player pace remains unresolved.

Implemented a production acquisition path for the user's **expected progression gear** curve in the primary LL game service. Eligible completed dungeons now award one bound equipment selection chest. The player chooses the item and specialization in the existing inventory opening flow. This closes the absence of a deterministic acquisition route identified in the [earlier acquisition audit](Tower-Expected-Gear-Acquisition-20260928.md); it does not establish realistic elapsed acquisition time or a new combat acceptance result.

## Reward rules

| Preparation for floors | Server prerequisite | Character level at dungeon entry | Dungeon source | Awarded tier | Rarity / quality / rank |
| --- | --- | ---: | ---: | ---: | --- |
| 1–3 | None | 30 | Region 1 | 1 | Rare / Standard / 2 |
| 4–6 | Floor 3 cleared | 30 | Region 1 | 1 | Epic / Fine / 3 |
| 7–9 | Floor 6 cleared | 40 | Region 1 | 1 | Unique / Exceptional / 4 |
| 10 | Floor 9 cleared | 50 | **Region 1** | **2** | Legendary / Masterpiece / 5 |
| 11–13 | Floor 10 cleared | 60 | Region 2 | 2 | Rare / Standard / 2 |
| 14–15, currently released | Floor 13 cleared | 60 | Region 2 | 2 | Epic / Fine / 3 |

The rarity/quality/rank policy repeats every ten floors. Tier, source region and minimum acquisition level remain separate authored fields. Milestones cover all currently released floors (1–15); later releases must supply their own tier/level/source entries. The floor-14 minimum level is an acquisition eligibility choice, not a newly calibrated combat budget.

The floor-10 source is deliberate: **region 2 dungeons require floor 10 to be cleared**, so placing its preparation gear there would create a circular requirement. Region 1 supplies the tier-2 Legendary chest after floor 9, at the legal tier-2 equipment level of 50.

Each completion awards only the highest eligible supply for that dungeon's region and the character's captured entry level. All dungeon grades qualify. Random equipment and blueprint rewards retain their existing rolls. Failed, active, retreated and already-claimed runs do not issue supplies. The released-floor catalog is checked as well as the configured server's progress.

Chests fix the band and tier when awarded; subsequent Tower progress does not promote an unopened chest. Equipment has a baseline attribute multiplier of 1.0, no style, and personal bound ownership. Existing stronger equipment remains owned and usable when the cycle repeats. Region 1 can still provide its unlocked Legendary chest after floor 10; it is not downgraded when region 2 begins.

## Effort and economy decision

The initial cadence is **one chest per successful eligible dungeon completion; one chosen item per chest**. A full eight-slot set therefore takes seven completions with a two-handed weapon, or eight with a one-handed weapon and off-hand. No additional Cinders or Reinforcement Parts are charged when opening: the awarded item already has its declared rank. Existing dungeon entry costs still apply.

For wholly new sets, five characters require 35–40 successful character-completions, ten require 70–80, and fifteen require 105–120. These totals are across characters, not shared party clears. Every character earns and chooses their own equipment, including players joining an expanded expedition later. No trading or donor stock is assumed. Owned pieces can be reused; these are not compulsory replacement bills at every floor.

This is an authored starting cadence, **not a measured normal-player acquisition time**. Dungeon success rates, sigil supply, entry costs and completion duration still need a progression study. Existing reinforcement, blueprint application and dismantling rules remain applicable to the awarded equipment, so the new source also changes potential material income and should be included in that study.

## Implementation and safeguards

- [TowerEquipmentSupplyCatalog.cs](../LL/src/Core/Domain/Models/Items/Equipments/Progression/TowerEquipmentSupplyCatalog.cs) owns the repeating band, validates milestone content, lists legal plain/specialized equipment and constructs bound rewards through the normal evaluator.
- [tower-equipment-supplies.v1.json](../LL/src/API/API.LL/Data/equipment/tower-equipment-supplies.v1.json) declares the six milestones; [items.json](../LL/src/API/API.LL/Data/items/items.json) adds their six bound chest bases. Treat existing chest IDs' tier/band contracts as immutable; use new IDs for future incompatible changes.
- [TowerEquipmentSupplyService.cs](../LL/src/Infrastructure/Service/Services.LL/Items/TowerEquipmentSupplyService.cs), its [interface](../LL/src/Core/Application/Interfaces/Services/LL/Items/ITowerEquipmentSupplyService.cs), [JSON loader](../LL/src/Infrastructure/Service/Services.LL/Items/JsonTowerEquipmentSupplyCatalog.cs), [registration](../LL/src/Infrastructure/Service/Services.LL/DependencyInjection.cs), and [completion integration](../LL/src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs) connect the policy to existing pending dungeon rewards. Stable run reward IDs and the persisted [run-state marker](../LL/src/Core/Domain/Models/Dungeons/Runs/DungeonRunState.cs) prevent retries from duplicating rewards or revisiting a no-award decision after a new floor unlocks. Missing chest content throws before marking the decision processed.
- [SelectionCrateService.cs](../LL/src/Infrastructure/Service/Services.LL/Inventories/SelectionCrateService.cs) validates the owned chest, legal selection and item base before consumption. The existing opening command supplies the character lock and transaction. Each opening from a stack creates a distinct equipment identity. The existing loot/inventory notification path is reused.
- [SelectionCrateMetadataDto.cs](../LL/src/Core/Application/UseCases/Items/Dtos/SelectionCrateMetadataDto.cs) exposes specialization names and tier/band descriptions. The Angular [inventory panel](../LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.html) and [item modal](../LL/src/Presentation/ll/src/app/shared/components/modal-container/item-modals/inventory-item-modal/inventory-item-modal.component.html), their component logic, [shared filtering helper](../LL/src/Presentation/ll/src/app/shared/utils/inventory/selection-container.utils.ts), and [item model](../LL/src/Presentation/ll/src/app/shared/models/item.ts) provide searchable choices with explicit selection. Changing the search clears the old choice so a hidden reward cannot accidentally be opened. Lists scroll within the existing compact game panels and stack on narrow screens.

## Verification

**143 backend tests passed; 31 focused Angular tests passed.** Backend checks include prerequisite and level boundaries, release and server gates, reward retries, integration before the ordinary drop roll, claiming a bound chest, every offered slot/specialization, invalid selections, ownership, missing content, unique stack-opening identities, metadata mapping, and floor-10 gear preservation when opening a floor-11 chest. Existing acquisition, inventory transfer, reinforcement and dungeon consumer checks also passed.

New backend coverage is in [TowerEquipmentSupplyTests.cs](../LL/tests/EssenceSystem.Tests/TowerEquipmentSupplyTests.cs), with existing helpers shared through `EquipmentAcquisitionTests` and metadata coverage added to `SelectionCrateMetadataDtoMappingTests`. Angular coverage spans the new filtering-helper spec and both existing inventory component specs.

Commands executed from the repository root unless noted:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~SelectionCrateServiceTests|FullyQualifiedName~SelectionCrateMetadataDtoMappingTests|FullyQualifiedName~EquipmentDungeonConsumerTests|FullyQualifiedName~DungeonRunReward|FullyQualifiedName~EquipmentUpgrade|FullyQualifiedName~InventoryTransfer|FullyQualifiedName~EquipmentIntegrationTests|FullyQualifiedName~DungeonCompletion' -ArtifactsPath 'TestResults/tower-supply-build-20260928'
# From LL/src/Presentation/ll, with npm cache beneath $env:TEMP:
npm.cmd run test:ci -- --include=src/app/shared/utils/inventory/selection-container.utils.spec.ts --include=src/app/shared/components/modal-container/item-modals/inventory-item-modal/inventory-item-modal.component.spec.ts --include=src/app/features/game/character/inventory/inventory.component.spec.ts
git diff --check
```

Retained local logs: [backend](../TestResults/tower-supply-build-20260928/verification-complete.log), [frontend](../TestResults/tower-supply-build-20260928/frontend-tests-complete.log). The first backend attempt was blocked by the sandbox's NuGet configuration access; the authorized wrapper run succeeded. An initial frontend fixture typing error was corrected; `npm.cmd` was used to preserve CLI argument forwarding on Windows. No required verification command remains blocked. No live database-backed gameplay session, production deployment, fresh Tower combat study or elapsed acquisition-time study was run. TestResults artifacts are local and ignored by Git.

## Configuration and rollout implications

The new `EquipmentProgression:TowerSupplyAcquisitionEnabled` option defaults to `true` and is also gated by the existing `ProtectedAcquisitionEnabled` option. Turning issuance off does not invalidate earned chests. These defaults are in [EquipmentProgressionOptions](../LL/src/Core/Application/Interfaces/Services/LL/Items/IStarterEquipmentService.cs); no environment-specific settings were edited.

No schema migration or new table is required: eligibility decisions use the dungeon run's existing JSON state, rewards use pending-reward rows, and chests use inventory items. A future deployment must include both the new catalog and updated item seed data, plus the API and frontend changes. The existing API startup seed path installs the chest bases; no startup, migration, seeding or deployment was executed here.

Tower guardian content remains byte-identical (`tower-floors.json` SHA-256 `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`). No search algorithm, combat formula, old benchmark fixture, or archived result was changed. The earlier studies describe their captured builds; their source hashes should not be presented as hashes of this newer working tree.

## Subsequent acquisition continuation

The [completed conditional study](Tower-Acquisition-Progression-20260928.md) now accounts for production sigil drop/assembly rates, entry costs on failed attempts, declared completion-time scenarios, bound personal inventories, party growth and floor-10→11 carryover. Its authored and up-front reference paths each earn **280 items through floor 10**, then reuse **70 exact items at floor 11**. Exact production equipment-descriptor parity and preparation passed for the strongest retained floor-10 and floor-11 references. No fresh combat, win-rate claim, cadence adjustment or boss retuning followed.

One successful clear is still one chest. The new evidence shows why that count is not an acquisition-time estimate: with perfect idle and dungeon victories and no stock or other source credit, a seven-item set requires **84 expected eligible idle hours** through random regional sigils alone. The study's other success/activity/duration scenarios are assumptions, not player measurements. Older-region supply farming remains a source of stronger gear and higher dismantling value; a tier-2 Legendary two-handed supply item returns **314 Parts**, versus **34** from a floor-11 Rare item. None of the ledger's retained equipment was dismantled or treated as income.

Production issuance now calls the catalog's shared `Candidates(region, entryLevel)` method; ordering and eligibility behavior are unchanged. This modifies the earlier uncommitted catalog/service source bytes, so the original feature receipt remains historical rather than being relabeled. The continuation's **146 focused backend tests**, current source hashes and independent accounting audit are recorded in its report and handoff. No frontend, application configuration, migration, seeding, database or deployment change was made by that continuation. The rollout requirements above still apply to the original supply feature.

## Subsequent full-dungeon diagnostic

The [bounded qualification](Tower-Dungeon-Acquisition-Qualification-20260928.md) subsequently ran 416 attempts and 52 matching replays through production dungeon actions/combat, including Vigor and failure. Previous supply gear supported the tested later transitions, including floor-9 tier-1 Unique gear used to earn the floor-10 tier-2 band. Starter-only controls failed, leaving the first Rare set's acquisition route unresolved. Their gear and Essence budgets changed together, so this does not isolate the cause. Eight layouts per role/source and simulated combat seconds do not establish acquisition time. No cadence, eligibility, dismantling, equipment curve or boss change followed.

Final regression verification passed 360 tests with one intentional opt-in skip; the bounded study passed separately. Its report retains the original runtime and audit, and records later concurrent administration-source drift rather than relabeling the study as current-build qualification. No deployment or database action occurred; the original supply rollout requirements remain unchanged.

## Subsequent matched first-supply progression

The [first-supply study](Tower-First-Supply-Progression-20260928.md) now follows one production supply award at a time from conditional pre-dungeon inventories, retaining every owned item and comparing gear at matched quest-token Essence budgets. Full Common sets completed **16/16 Goblin Mines paths** in seven attempts each; sparse quest armor plus one ordinary weapon completed **0/16**. Those outcomes establish a conditional route, not the time needed to obtain the starting gear and sigils. Random quest outcomes, ordinary-drop prerequisites and joint gear/sigil acquisition still need a realistic activity history.

The new harness-only continuation passed 364 regression tests, its bounded study and an independent audit. It recorded 2,374 attempts, 18,472 room combats, 80 full-run matching replays and 1,330 earned items. No production code, supply cadence, eligibility, dismantling, boss or gear-curve setting changed. Existing implementation rollout requirements and historical receipts remain unchanged; no deployment, API startup or database operation occurred.

## Subsequent joint idle activity — 29 September

The [joint equipment/sigil study](Tower-Joint-Activity-Progression-20260929.md) now feeds production ordinary rewards into the same personal inventory that funds dungeon entry and receives supply awards. It removes exact-profile and chosen quest-armor assumptions, retains unused equipment and Catacombs sigils, and counts failures. The fixed Mines-only policy completed seven-item purchases in 13/16 perfect-idle and 9/16 four-in-five histories; unfinished histories had no Goblin sigils but did retain the other family's entry resources. The next comparison is a both-family policy, not a boss or cadence change.

The harness continuation passed 367 regression tests, its owned study and independent audit. No production code, configuration, migration or deployment implication changed; the original item-seed rollout requirements still apply. Activity checkpoints remain conditional scenarios with zero measured player samples. All earlier archives and concurrent work remain preserved.

## Subsequent paired source-policy comparison — 29 September

The [two-source continuation](Tower-Two-Source-Progression-20260929.md) compares both-family spending with a fresh Mines-only baseline on matched personal reward histories. Seven-item completion improved **13/16 → 16/16** under perfect assumed idle victories and **8/16 → 14/16** under four-in-five victories. The older 9/16 Mines result is historical, not this comparison's baseline. Both-family histories paid for 95 Catacombs entries, of which 36 failed, including ten Vigor-attrition failures. Early entry readiness is the next comparison; no farming-time target or supply-cadence change was inferred.

The harness-only extension passed **372 regression tests**, its owned study and independent audit of 533 acquisition attempts and 5,805 room combats. It preserves personal inventory, historical archives and concurrent work. No production code, configuration, migration, database or deployment implication changed; the original item-seed rollout requirements above still apply. The repeating gear curve, supported search and Tower bosses remain unchanged.

## Subsequent visible entry-readiness comparison — 29 September

The [readiness continuation](Tower-Entry-Readiness-20260929.md) waits for all equipped slots to be covered before spending either family's sigils. It reduced failed entries **83 → 14** compared with its fresh immediate baseline, retained 16/16 perfect-idle completions and improved four-in-five completion **14/16 → 15/16**. All fourteen remaining readiness failures were Catacombs Vigor attrition. The unfinished character cleared six attempts and exhausted both entry stocks, leaving an entry-source shortfall under the declared exclusions. Neither cadence checkpoints nor combat seconds are measured acquisition time.

The harness/test/documentation continuation passed **385 regression tests**, its owned study and independent audit of 540 acquisition attempts and 6,160 room combats. No production code, configuration, migration, database or deployment implication changed. The original item-seed rollout requirements still apply, and all archives and concurrent work remain preserved. Ordinary dungeon rewards and costed entry sources are the next model extension; the gear curve, supported search and bosses remain unchanged.

## Subsequent ordinary dungeon equipment extension — 29 September

The [loot continuation](Tower-Dungeon-Loot-Progression-20260929.md) adds production ordinary equipment acquisition/claiming and unspent blueprint state to retained inventories. Pending miniboss loot is lost on terminal failure; it cannot improve an entry snapshot mid-run. A zero-fight projection changed eight first-attempt loadouts and prepared all 32, warranting a fresh bounded comparison. The included-loot histories retained 106 equipment items, but seven-supply completion stayed **16/16 perfect-idle and 14/16 four-in-five**, matching the new exclusion baseline. The target remains seven selected supplies, not a minimally sufficient loadout or a time estimate.

**388 regression tests passed**, plus the projection, bounded study and corrected independent audit of 466 acquisition attempts and 6,025 room combats. An auditor-only PRNG correction is separately pinned with native test vectors; no game runtime or study output changed. No production code, configuration, migration, shared database or deployment implication changed; the original rollout requirements remain. Costed entry sources are next. All archives, concurrent work, the repeating gear curve, supported search and bosses remain preserved.

## Subsequent costed entry-source projection — 29 September

The [source-affordability continuation](Tower-Costed-Entry-Sources-20260929.md) adds native prophecy progress/claims and fragment assembly to personal ledgers. Under explicitly conditional daily offers and continuous activity, ten supplied days fund three extra Mines sigils; a completed weekly kill prophecy raises that to four. Each alternative changes four archived entry paths, including both unfinished ones. Production preparation passes, but subsequent combat outcomes remain untested. Duplicate quest rewards, unopened-cache means and unfunded shop purchases supply no credit.

**456 regression tests passed**, eight opt-in studies skipped, with the seed-free export and independent audit passing separately. Zero new fights or combat reservations were used. No production source, configuration, migration, database or deployment implication changed; the original supply rollout requirements remain. Next, validate production offer availability and a concrete source policy before evaluating changed routes. All historical archives and concurrent work remain preserved.

## Subsequent native prophecy offer comparison — 29 September

The [native-offer continuation](Tower-Native-Prophecy-Progression-20260929.md) implements production offer generation and a fixed daily kill-choice policy without rerolls. A seed-free projection passed five changed entry preparations. The fresh paired comparison completes **16/16 perfect-idle histories in both arms** and improves four-in-five completion **15/16 → 16/16** with funded prophecy sigils. The older 14/16 loot result is not this comparison's baseline. One previously short character earns its seventh item; no already-completing history reaches an earlier checkpoint. Leveling/mastery, other objective channels, cache choices and measured attendance remain excluded; zero player acquisition-time samples were measured.

The harness/test/documentation changes passed **459 regression tests**, with ten intentional opt-in skips, plus the separately owned projection, combat study and both independent audits. Combat used 449 acquisition attempts and 5,408 room combats. All 6,240 new seed reservations remain excluded, including 5,824 unconsumed values, bringing the union to 870,695. No retries, boss adjustments, supply-cadence changes, production source changes, migrations, shared database access or deployment occurred. The two new fixtures configure only the local study; original supply rollout requirements still apply. The next model gap is earned character progression and its effect on retained gear and eligibility. Historical archives and concurrent work remain preserved.

## Subsequent earned-growth qualification — 29 September

The [growth continuation](Tower-Earned-Growth-Qualification-20260929.md) applies native character XP and activity-selected Essence training through each first archived dungeon entry, including level-dependent prophecy offers and earned Essence-XP objectives. Under explicit one-creature/zero-bonus conditions, the 32 characters reach levels **31–44** and Essence levels **2–10**. All 64 baseline/grown preparations preserve exact owned equipment and Essence identities. A separate mastery-only replay stops before changed entry benefits in 21 histories. No old outcome is transferred to grown characters, and the previous 16/16 completion result is not updated by this qualification.

**499 regression tests passed**, eleven opt-in skips, plus the owned qualification and independent audit. No new combat or seeds were used; the exclusion union remains 870,695. No production source, deployment configuration, migration, database or deployment implication changed; the new fixture is harness-only and original rollout requirements still apply. A fresh acquisition comparison must integrate dungeon XP claiming, persistent mastery and an explicit activity clock. Player pace remains unmeasured; archives, stronger carried gear and unrelated concurrent work remain preserved.

## Subsequent growing-activity comparison — 29 September

The [growing-activity continuation](Tower-Growing-Activity-Progression-20260929.md) integrates native dungeon XP loss/claiming, earned character/Essence growth, persistent mastery and a declared serial activity clock. Fresh fixed and grown arms both complete **32/32** seven-supply histories at identical coarse checkpoints, with one paid Catacombs attrition failure per arm. Grown characters reach levels **35–45** and retain 101 ordinary dungeon items versus 93 fixed. All 32 grown paths use fewer engine combat seconds; this does not establish player acquisition time or justify a supply-cadence change.

**504 regression tests passed**, twelve intentional opt-in skips, plus the owned study and amended independent audit of 450 attempts and 5,705 room combats. The auditor-only amendment corrects a failed-run cap-reward flag expectation; frozen runtime/results remain unchanged. All 6,240 reservations remain excluded, including 5,824 unused, raising the union to 876,935. Next is legal Tower party preparation from these actual retained inventories and earned progression through later gates. No production source, deployment configuration, dependency, migration, shared database or deployment implication changed; original supply rollout requirements still apply. The repeating gear curve, stronger carried gear, supported search, bosses, historical archives and unrelated concurrent work remain preserved.


## Subsequent earned-party validation — 29 September

The [earned-party continuation](Tower-Earned-Party-Progression-20260929.md) exports actual retained inventory prefixes into production Tower preparation (224 parties, 16 personal owners) and completes a separately admitted floor-one diagnostic (1,024 trials and 64 exact replays). At the 72-hour assumed idle checkpoint, grown parties win 87/128 versus 58/128 fixed; some win with only seven supply items across the entire party. Seven successful purchases per character are not an established entry requirement, and acquisition time remains unmeasured.

**578 regression tests passed**, fourteen intentional opt-in skips, plus both owned studies and independent audits. Preparation-verifier representation fixes were explicitly archived before combat; runtime/results were unchanged. The 512 fresh seeds raise the exclusion union to 877,447, with all previous unused reservations retained. This adds harness/test/documentation files only. Production supply cadence, bosses, search, deployment configuration, dependencies and migrations remain unchanged; no shared database or deployment action occurred. Original rollout requirements still apply. Later level/Essence acquisition and server-wide unlock progression remain the next model gaps.


## Subsequent chronological early unlocks — 29 September

The [early-unlock continuation](Tower-Early-Unlock-Progression-20260929.md) runs 32 bounded paths from actual earned checkpoint parties, with 148 fresh attempts and 78 exact replays. Fifteen clear floor 3 and gain native server-wide Epic supply eligibility, including an eligible nonparticipant. Earlier pending decisions remain Rare. These probes award no owned equipment and do not establish acquisition time. Native Tower finalization preserves earned XP and rejects early/repeated completion; tokens and cosmetic titles are granted exactly once.

**580 regression tests passed**, sixteen intentional opt-in skips, plus both owned studies and independent audits. A preserved verifier-only amendment handles JSON configuration comments and native timestamp precision. The new ledger reserves 384 seeds, including 236 unconsumed, bringing the exclusion union to 877,831. This continuation changes harness/test/analysis/documentation files only. No production source/configuration/dependency change, migration, database access or deployment occurred; original rollout requirements still apply. Next is funded individual dungeon acquisition after the earned unlock, then further XP, Essence sources and larger parties.


## Subsequent funded-entry qualification — 29 September

The [post-unlock entry qualification](Tower-Upgrade-Entry-Qualification-20260929.md) restores 32 personal alternatives and evaluates 512 owner/server combinations: 192 have funding and satisfy the existing coverage policy, including 92 on Epic-eligible servers. Of those 92, 49 are nonparticipants. The other combinations are held by personal sigil stock or a conservative policy, not by a new production restriction. Eighteen assembly grant units are reconciled with actual spent stock; blueprint items, exact equipment, XP, Essence training, mastery and source evidence remain preserved. These copied entry previews spend nothing and earn no gear.

**584 regression tests passed**, seventeen intentional opt-in skips, plus the owned qualification and independent audit without amendment. No fresh combat or seeds were used; the exclusion union stays 877,831. Full mutable prophecy/journey runtime must be reconstructed and checked before continuing reward-producing runs. This adds harness/test/analysis/documentation files only; production behavior, cadence, bosses, search, dependencies and deployment configuration remain unchanged. No migration, real database access or deployment occurred; original rollout requirements still apply.

## Subsequent runtime restoration and funded next entries — 29 September

The [funded-entry follow-up](Tower-Funded-Next-Entry-20260929.md) restores complete native prophecy/growth/mastery state and spends actual remaining sigils on one additional dungeon per ready server/owner combination. Runtime replay corrected thirteen future offer observations at a checkpoint timestamp while preserving all earlier resources and entry decisions. The original failed evidence and the corrected audit's separate timestamp-parser amendment remain archived.

Ten of twelve distinct fresh personal cases complete; paired across server alternatives this yields 160 completions, 32 paid failures, 76 opened useful Epic supplies and 84 retained surplus Rare chests. Both failed Catacombs cases also fail supplied Epic controls through Vigor attrition. All stronger owned equipment is retained, and all 192 resulting loadouts pass production preparation. Seven purchases are not a production cap or a minimum loadout, and no player acquisition-time forecast is established.

**592 regression tests passed**, nineteen intentional opt-in skips, plus the corrected runtime qualification and funded study. The funded audit passes without amendment: 2,412 hashes and 2,214 room preparations. All 780 fresh reservations remain excluded, including 645 unused, bringing the union to 878,611. Next is legal Tower preparation from these actual post-wave inventories, followed by further earned levels, Essences and party growth. These are harness/test/analysis/documentation changes only. Production source, cadence, bosses, supported search, dependencies, deployment configuration and migrations remain unchanged; no shared database, API host or deployment was used. Original supply rollout requirements still apply.


## Subsequent earned Tower return — 29 September

The [earned-return follow-up](Tower-Earned-Return-20260929.md) qualifies all 32 post-acquisition Tower parties and preserves 512 personal runtime/inventory/pity alternatives plus native server history. Thirty-five participant loadouts changed. All fifteen floor-4 entrants clear within four attempts; only two of seventeen earlier stopped servers advance. Actual floor-4 parties win 15/18 paired attempts versus 12/18 pre-wave, while outcomes on floors 1–3 match their pre-wave controls. Supplied Epic controls win all 83 attempts but earn nothing. These conditional model results support an acquisition benefit on floor 4 and leave early acquisition problems open; they do not establish reliable-clear rates or player acquisition time.

**594 regression tests passed**, twenty-one intentional opt-in skips, plus seed-free preparation and bounded combat. Both independent audits pass without amendments; 83 actual attempts, 166 controls and 96 exact replays total 345 fights. The latest reservation union is 878,739, retaining 45 newly unused values and all earlier reservations. Fifteen servers now unlock ten-member floor-5 parties; characters remain level 34–36 with four Essences. Next is actual party growth and native Essence/resource progression, preserving stronger owned gear through later floors. This continuation changes only harness/tests/analysis/documentation. Production bosses, cadence, supported search, dependencies, configuration and migrations remain unchanged; no shared database, API host or deployment was used. Original supply rollout requirements still apply.


## Subsequent ten-member floor-5 expansion — 29 September

The [floor-5 continuation](Tower-Floor5-Expansion-20260929.md) qualifies fifteen ten-owner parties while preserving all bound gear and historical five-person rewards. Native rally admission and 720 disposable Essence loadout decisions validate actual four-Essence ownership and the separate level-40/fifth-ownership gates. No later-budget level or Essence is granted. All sixty earned floor-5 attempts fail, as do sixty full-Epic controls at the same actual growth. The thirty exact replays match, and native scouting caps at 30 with no floor-6 unlock or new rewards. This leaves earned growth and additional Essence acquisition/training as the next testable dependencies; equipment cadence or boss retuning is not justified by this panel.

**689 regression tests passed**, twenty-three intentional opt-in skips, plus both bounded studies and independent audits without amendments. All sixty new seeds are consumed, bringing the reservation union to 878,799 while preserving every earlier unused value. Seventeen stopped server paths remain unchanged; all 512 personal alternatives survive. Historical ordinary Essence-drop/resonance state is missing and must be explicitly handled before any future drop-based fifth-Essence award. No player acquisition-time measurement, production/source-cadence change, new dependency/configuration, migration, database write or deployment was made. Original supply rollout requirements remain applicable.

## Subsequent Old Forest growth and quest source — 29 September

The [Old Forest follow-up](Tower-Old-Forest-Growth-20260929.md) applies production XP, four owned Essence training, prophecy claims and ordinary equipment/sigil rewards to the fifteen floor-5 servers. All 240 owner/server states reach level 40 with four level-8 Essences under the existing one-creature/perfect-or-four-of-five assumptions. All 8,915 prior equipment references are retained; 6,870 new items change 164 loadouts, and fifteen native party preparations pass. The additional 54.51–104.29 cadence hours are conditional model equivalents, not measured player time. All sixteen owners per server and the seventeen held server paths remain preserved.

Native quest/token/absorption probes establish an Old Forest fifth-Essence source without relying on missing resonance history. Probe rewards are withheld. The prospective source leaves historical Mines completion uncredited; reconstruct the recorded quest/completion order if possible before paying for a conservative fresh run. No fifth Essence or new Tower/dungeon success is granted. The original failed cohort-label qualification and frozen auditor are preserved, with separate versioned implementation and audit corrections documented in the report.

**802 regression tests passed**, twenty-four intentional opt-in skips, plus the corrected owned study and independent audit; twelve corrupted copies are rejected. Zero new fights or seeds; the exclusion union remains 878,799. Changes are limited to harness/tests/analysis/reports. Production source, repeating gear curve, supported search, dependencies, configuration and migrations remain unchanged; no shared database or deployment was used. Original supply rollout requirements remain applicable.


## Subsequent retained fifth-Essence acquisition — 29 September

The [retained fifth-Essence follow-up](Tower-Retained-Fifth-Essence-20260929.md) uses recorded paid Mines completions and earned Old Forest victories to retain 143 native quest/token/absorption receipts under an explicit active-and-unclaimed quest assumption. The fifths preserve their actual IDs and begin at level 1 with zero XP. Ninety-seven states still need a paid Mines success; each retains 4–11 Mines sigils. All 25,682 current equipment references across 512 owner/server alternatives survive. Fifteen production party preparations pass, with 100 fifth owners among 150 participants. No new combat, resource debit, XP or player-time evidence was created.

**808 regression tests passed**, twenty-five intentional study skips, plus the owned qualification. The independent audit checks 4,332 hashes; sixteen corrupted copies are rejected. Its new-day prophecy-bookkeeping correction is separately pinned, preserving original frozen code, failed logs, binaries and outputs. Source evidence is in `TestResults/tower-fifth-study-20260929`, manifest **`e0c1e2f53dcd3c0778e032c97a9904b695b8ba753de0979aa2d05b0701a49f25`**. The latest seed union remains 878,799. Next is native access qualification and bounded paid Mines acquisition for pending quests before changed Tower combat. Bosses, cadence, repeating gear curve, supported search, production code, dependencies, configuration and migrations remain unchanged; no shared database or deployment was used. Original supply rollout requirements remain applicable.


## Subsequent paid Mines acquisition for pending fifths — 29 September

The [paid Mines follow-up](Tower-Pending-Mines-Acquisition-20260929.md) qualifies and spends one existing personal sigil on each of 97 pending quest states. All 97 actual runs and their supplied controls complete, and all 97 exact replays match. The wave retains 97 new fifth Essences, 97 useful Epic/Fine/rank-3 supplies, 52 ordinary equipment items, 24 blueprints and 961,500 dungeon XP. All stronger previously owned gear remains; total equipment references grow from 25,682 to 25,831 across the 512 alternatives. The earlier 143 completed quests and seventeen held servers are unchanged. All 240 eligible owners now have five Essences; all 150 current Tower members pass production preparation. New fifths have level 1 and zero XP.

**811 regression tests pass**, with 26 intentional opt-in skips, plus the two owned stages. Both independent audits pass without amendments, checking 3,234/3,239 frozen hashes; 22 corrupted detached copies are rejected. The bounded wave uses 2,448 room fights and leaves 5,392 of its 6,305 reservations unused; all **885,104** historical and new reservations remain excluded. Combat manifest: **`963fd340840d7b1f7d9f3e16eef9f57a97fec4445b322ef7d157e6adbd5f37c7`**. No floor-5 Tower outcome is inferred from these preparations, and 152.4–619.4 engine seconds are not measured acquisition time. Next is native Tower history restoration/admission followed by fresh bounded combat. Production source, bosses, cadence, repeating gear curve, supported search, dependencies, configuration and migrations remain unchanged; no shared database or deployment was used. Original supply rollout requirements still apply.


## Subsequent five-Essence Tower return — 29 September

The [earned Tower return](Tower-Fifth-Essence-Return-20260929.md) qualifies all fifteen current parties, preserves native server rewards and 128 prior attempts, and verifies all 240 owners' actual five-Essence loadouts. The fresh bounded panel yields **0/60 actual wins and 0/60 supplied Epic wins**, with thirty exact replays (150 fights). Supplied gear reduces remaining guardian health but does not clear the panel. All 25,831 equipment references and 512 personal/server alternatives survive; sixty new failures are retained. No floor-6 unlock, new equipment or personal growth is awarded.

**813 regressions pass**, with 27 intentional study skips, plus both owned stages. Both independent audits pass without amendments (3,370/3,390 hashes); twenty corrupted detached copies are rejected. Combat manifest: **`dd754b30a0ce0a26032fb0149d64984c2ef41b55f458212a0a4e2529d9adfd50`**. All sixty seeds are used; the complete exclusion union is 885,164, retaining all older unused reservations.

Production source inspection shows that plain Essence levels do not scale combat abilities; ascension requires level 10 and six Lesser Cores at these ownership counts. Prior scenarios withheld completion core rewards, so the next work must qualify those sources and native ascension instead of assuming fifth-level training alone increases strength. This result does not establish player acquisition time or justify boss/cadence tuning. Production source, gear curve, supported search, dependencies, configuration and migrations remain unchanged; no shared database or deployment was used. Original supply rollout requirements remain applicable.

## Subsequent core-source and native ascension qualification — 29 September

The [core-source follow-up](Tower-Core-Source-Qualification-20260929.md) reconciles 1,116 paid historical attempts, including 1,100 successes and 16 failures, with each retained owner's mastery. Production grade-I core rewards have a nonuniform 3–6 distribution with mean 4.25 plus six per first family completion. Under explicit first-history/unspent-core assumptions, 120 of 240 alternatives have a guaranteed thirty-core budget for five first ascensions, 23 more depend on missing draws, and 97 have only 9–12. These projections are not credited inventory or measured player acquisition pace.

Four native completion/claim controls verify first/repeat rewards, treasure progress and duplicate-claim rejection. All 1,200 native training/ascension controls enforce ownership, exact level-10 XP and six-core spending. Original level-8 Essences need 178,064–246,631 additional XP; each fifth needs 1,296,004. The probes remain disposable. All fifteen parties and 150 members prepare unchanged; all 25,831 owned equipment references, 512 alternatives and 188 current Tower attempts survive.

Manifest **`0f492878ef0e90e67c0ade6f2553d94798f43920247459759b85e3208cb9e4fc`** in `TestResults/tower-core-source-study-20260929`. **821 regressions pass**, 28 intentional skips, plus the owned qualification; independent audit checks 4,905 hashes without amendment, and 22 detached corruptions are rejected. No seeds or combat are added; the exclusion union remains 885,164. Next is explicitly funded forward training/ascension with conserved rewards before changed combat. Historical random draws and associated treasure-progress benefits must not be invented. No production source/content, boss/cadence change, dependency, configuration, migration, shared database or deployment occurs. Original supply rollout requirements remain applicable.
