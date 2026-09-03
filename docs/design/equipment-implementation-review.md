# Equipment progression implementation review

> Historical record — superseded 3 September 2026. Crafting, gathering, queued tempering, profession storage, cohort logic, conversion/refund adapters, and the equipment Forge are now removed. All Forge/Scrap/Blueprint implementation statements below describe an abandoned intermediate design. Use the [current status](equipment-implementation-status.md), [Forge removal](equipment-forge-removal.md), and [cleanup record](equipment-post-alpha-cleanup.md).

## Historical findings and verification

Reviewed: 2 September 2026; reward, administrative, sigil, marketplace and reference-build follow-ups: 3 September 2026. Scope: the primary game in `LL/`, its runtime content, player UI, supporting tools and transition requirements.

**3 September follow-up:** findings 2, 3, 4 and 6 are implemented. Finding 8 now has a dedicated equipment reference generator and deterministic production-combat report; whole-loop evidence and legacy calibration-policy migration remain pending. The exact-equipment market now has canonical filters, rank/style details and a server guard against legacy purchases by characters using the Forge. Sigil income is exclusive by saved cohort, and equipment progression Sigil Traces ranks can be refunded. LiveOps compensation now creates canonical recipient-bound equipment with explicit rank/style, zero initial salvage, audited identities and preview/retry validation; retired-resource issuance is guarded. Reward integration includes: Champion Market preview/purchase, tournament preview/manual/automatic claim and held catalyst caches now use the equipment progression Scrap replacement. Tournament delivery history adds one generated, unapplied migration (six equipment progression migrations total). See the [current ledger](equipment-implementation-status.md) for fresh verification. The original review evidence below remains dated 2 September. Raids are explicitly deferred at the owner's request.

The naming integration now exposes Equipment and Forge throughout the game and tooling. Domain/service/use-case names, API contracts and content/documentation paths use descriptive gameplay terms. Persisted identities and compatibility aliases are intentionally retained; this changes no activation or balance-readiness assessment. Verification passed 2,063 backend tests, 127 affected player browser tests, 19 administrator browser tests and both frontend builds. All 72 reference builds retain their pre-rename results. See the [compatibility notes](../engineering/equipment-naming-and-compatibility.md).

## Assessment

**Consumer follow-up, 3 September:** character profiles, tool equip/slot surfaces and inventory Rank/Style controls now follow equipment progression. A bounded LiveOps section inspects saved equipment provenance/investments, locations, unclaimed protection receipts and acquisition/style state. The [consumer audit](equipment-consumer-audit.md) records these changes, their limits and the confirmed remaining profession/Total Level leaderboard dependency. Earlier broad operator/UI gaps below are narrowed by this milestone; complete conversion and game-wide retirement remain open.

**The core equipment loop is substantially implemented for Tier 1 and Region 1. Integration across the whole game is partial, and migration plus retirement of the old runtime are unfinished. Equipment progression is not ready for activation.**

There is enough code to acquire starter equipment, earn plain and named items, recover missing baseline equipment, invest in ranks, learn/apply styles, salvage, trade eligible discoveries and lend guild equipment. This is a functional implementation, with persistence, APIs, UI and meaningful regression tests.

It is not yet a complete replacement for the previous systems. Some unrelated game entry/reward paths still enforce or generate legacy equipment concepts. Later-tier progression, support tooling, conversion and end-to-end acceptance remain open. A single completion percentage would obscure these different kinds of work; the coverage table below is the more useful measure.

“Implemented” below means implemented in the inspected scope and supported by local tests, not deployed or certified against a live database. The six activation options default to false. The six generated equipment progression migrations remain unapplied according to the task's implementation record; this review did not connect to a shared database.

This review checks the [specification](equipment-specification.md) against code rather than treating the [implementation ledger](equipment-implementation-status.md) as proof of complete integration. Gear-selling merchants remain deliberately excluded. Provisional numbers are acceptable at this stage; integrated balance follows completion of the gameplay loop.

## Coverage

| Area | Status | What exists / what limits completion |
| --- | --- | --- |
| Canonical equipment and combat | Implemented foundation | Frozen descriptors, deterministic evaluation, rank/style/ownership/provenance, paid investment, normalized stats, equipment snapshots and shared combat handling. The ordinary runtime catalog currently defines only Tier-1 Common/Rare gear. |
| Starter access and recovery | Implemented for current onboarding | 31 plain archetypes, legal armor/hand choices, accessory grants, durable entitlements and recovery of missing original or earned plain copies. Recovery does not grant repeat resources. |
| Forge | Implemented for Tier 1 | Rank 0–5 investment, quotes, stale-state checks, 13 styles, Blueprint learning, one free first application, restoration, binding, salvage and operation receipts. Higher-tier prices and runtime definitions are not supported yet. |
| Ordinary acquisition | Implemented for Shenic | All ten areas have ordinary equipment/Scrap rules, elective plain targets and two sigil families. Online/offline batches share the same durable progress path. Random sigils are now legacy-only; equipment progression uses the selected-family counter, with existing held rewards and partial progress preserved. |
| Protected dungeon acquisition | Implemented for six current pools | Goblin Mines and Forgotten Catacombs difficulties I–III; 16 named identities, committed targets, bounded guarantees, progress preservation, frozen pending rewards and repeat-safe claims. All pools are Tier 1. |
| Quests and onboarding | Implemented replacements; transition partial | 15 additive equipment progression quest definitions, first-entry/resource grants, recovery objectives and seven optional profession-quest exclusions. Existing characters retain saved versions; conversion of old unfinished progress is not complete. |
| Ownership and multiplayer | Implemented core lifecycle | Unbound discovery trade/market delivery, equip/investment binding, permanent guild donations, loans, return on membership changes and disband cleanup. Existing legacy holdings and marketplace escrow still need conversion treatment. |
| Prophecies, achievements and guild objectives | Substantially integrated | Profession Prophecies and obsolete achievement goals are retired for the cohort; cache rewards and guild objectives have replacements. Earned history is retained. Impossible unfinished legacy objectives still need transition policy. |
| Guild shop, events and raid vendors | Implemented selected integrations | Cohort-specific Scrap replacements, shared-event combat participation, reusable Blueprint descriptions and preserved prices/limits/history. Champion Market, tournament claims and held catalyst caches were added on 3 September; raid entry remains deferred. |
| Soulstones | Partial integration | Six gathering/crafting constellations plus Sigil Traces are guarded; inactive owned ranks remain visible and mapped refunds preserve the seven active upgrades. Older unmatched upgrade IDs still need conversion policy. |
| Player UI | Main flows implemented; cleanup partial | Forge, inventory metadata/comparison, targets, recovery, binding, guild loans, Soulstone refund and Blueprint descriptions exist. Exact-equipment marketplace filters and buy/sell binding displays are integrated. Broader legacy surfaces still require the consumer audit. |
| Content beyond Region 1 | Missing equipment progression coverage | No higher-tier runtime acquisition/Forge path; no complete equipment progression source design across later regions, region bosses and Tower milestones. Raid style books exist, but raid access remains incompatible. |
| Admin, reference builds and telemetry | Partial foundation | Definition validation, receipts, economy entries and outbox events exist. Canonical admin compensation, exact previews and audited retries are implemented as of 3 September. A separate equipment reference generator and deterministic production-combat report are implemented. Legacy calibration policies, whole-loop evidence, complete item/protection/investment inspection and observed-pacing views remain open. |
| Conversion, release validation and cleanup | Mostly outstanding | Six additive schema migrations exist; administrative compensation adds no further schema change. Comprehensive conversion, reconciliation, restart/recovery rehearsal, PostgreSQL concurrency/rollback acceptance and final legacy-writer retirement remain open. |

Counts above were checked directly against the current JSON catalogs and quest definitions. The detailed implementation files and historical verification results remain in the implementation ledger.

## Confirmed integration gaps

### 1. Raid entry still requires the previous equipment progression

[RaidService.GetLoadoutRequirementErrorAsync](../../LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs) checks armor Tier, legacy rarity and `BlueprintId`. Both authored [raid bosses](../../LL/src/API/API.LL/Data/raids/raid-bosses.json) require Epic-or-better Blueprint armor; their required equipment tiers are 1 and 2.

[JsonStarterEquipmentCatalog](../../LL/src/Infrastructure/Service/Services.LL/Items/JsonStarterEquipmentCatalog.cs) creates Common plain and Rare named definitions, and constrains every runtime archetype to Tier 1. Rank improvement does not raise rarity. Equipment progression does project the active style into `BlueprintId`, but that does not satisfy the Epic requirement.

**Impact:** the currently authored equipment cannot meet these raid armor checks. Completing raid vendor rewards did not complete raid integration. Replace this check with explicit equipment progression readiness requirements; retain separate legacy behavior until conversion. This is a functional blocker, not a balance-tuning issue. Specification: ME-INT-02, AC-13.

### 2. Duplicate sigil income and Sigil Traces — resolved 3 September

[IdleCombatRewardCalculator](../../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/IdleCombatRewardCalculator.cs) now excludes random sigil rolls for the saved equipment progression cohort. Its ordinary processor and selection service exclude legacy characters, even when the global acquisition flag is on. Equipment progression retains the existing selected-family reward every 4,320 eligible victories, family gates, shared progress and replay protection. Paused acquisition, no selection and unsupported areas cannot fall back to random rolls.

Sigil Traces joins the explicit retired-upgrade refund mapping for equipment progression, with purchase/bonus guards and all five historical rank costs. Legacy random drops and their bonus remain supported. Held sigils and calculated pending settlements remain deliverable; existing partial progress is retained. No automatic cohort conversion or unresolved-work cutoff is introduced. Those remain part of the broader transition plan. The earlier Region 1 proposal to combine random and deterministic income is superseded; analysis inputs/report now match the exclusive contract.

### 3. Champion Market, tournament rewards and catalyst containers — resolved 3 September

The original finding identified direct catalyst-cache purchases in [ColosseumService](../../LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs), tournament cache previews/claims, and held containers that opened into retired catalysts.

**Implemented:** character-specific market projections and tournament previews agree with delivery; one cache entitlement becomes two Tempered Scrap under the existing provisional catalog. Already-pending tournament grants use the same manual/automatic claim path, and a nullable delivery record preserves actual claimed amounts without rewriting older claims. Held caches offer Scrap in the inventory inspector and modal, with server validation of the selected option. Prices, limits, rotation, earned entitlement counts, unrelated Essence resources, Blueprint boxes and legacy-cohort behavior remain supported. Loose old catalysts and other holdings still require the broader conversion plan. Specification: ME-LOOT-07, ME-INT-03, RM-13.

### 4. Administrative equipment grants — resolved 3 September

[LiveOpsService.GrantCompensationItemsAsync](../../LL/src/Infrastructure/Service/Services.LL/Administration/LiveOpsService.cs) now requires a canonical definition, supported tier, rank and compatible style for an equipment grant. Each instance uses the deterministic evaluator, is bound to the recipient and carries Administrative provenance with zero base salvage and no fabricated paid receipts. The audit records parameters, a frozen descriptor and all instance IDs. Raw legacy equipment and retired-resource issuance are blocked for this cohort; supported resources and legacy grants retain their paths.

The standalone LiveOps host loads the canonical catalogs and saved-cohort policy. Its dashboard exposes validated choices and exact previews; authorization, actor/reason checks, transactions, request/state hashes and idempotent replay remain enforced. Broader item/protection/investment inspection and conversion/correction workflows remain separate work. Specification: ME-EQP-02; this completes the compensation-grant portion of ME-OPS-01.

### 5. Higher-tier content requires code and content work

The [runtime loader](../../LL/src/Infrastructure/Service/Services.LL/Items/JsonStarterEquipmentCatalog.cs) fixes archetypes to minimum/maximum Tier 1. [EquipmentAcquisitionCatalog](../../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentProgressionAcquisition.cs) and [CombatAcquisitionCatalog](../../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentProgressionOrdinaryAcquisition.cs) reject other tiers. `ForgePolicy.cs` previously guarded rank/style modifications and has been removed.

**Impact:** later-region support is not simply a matter of adding reward rows. Extend the validated tier/source/pricing contract and author usable equipment and recovery paths for every released progression band. The domain supports broader concepts, but the connected runtime is deliberately narrower. Specification: ME-LOOT-02, ME-INT-02.

### 6. Exact-equipment market browsing — resolved 3 September

The [buy component](../../LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-buy/market-place-buy.component.ts) now uses frozen canonical identity/style/rank/tier data for Forge equipment and retains Quality/Potential only in the separate legacy view. Rows and purchase details show canonical ranks, native/active styles and binding consequences. The seller view excludes bound/guild equipment from new sales and displays canonical rank details.

A equipment progression buyer cannot purchase legacy equipment through a direct request. Shared snapshots/history and owner cancellation remain available, and existing exact-instance transfer behavior remains authoritative. Legacy recipients can still buy eligible legacy or canonical equipment. No escrow or holdings conversion is performed. Specification: ME-UI-02; this closes the identified market surface under RM-16, while other consumer contracts still require auditing.

### 7. Existing queued Tempering still has a live resolver

[CharacterActionService](../../LL/src/Infrastructure/Service/Services.LL/CharacterActions/CharacterActionService.cs) still dispatches crafting actions to CraftingService.PerformIdleCrafting (removed: `CraftingService.cs`). That resolver retains old outcomes, profession experience and Soulstone rewards. The equipment progression guards on new craft/learn/start/resume requests do not themselves convert or settle previously queued work.

**Impact:** preventing new legacy requests is only part of the transition. Define a cutoff and settle earned work, return/convert real instances, then retire the old scheduling dependencies. Keeping this resolver while legacy cohorts exist is intentional compatibility; activating a conversion without settling it would be incomplete. Specification: ME-MIG-04, RM-11.

### 8. Equipment reference builds implemented; whole-loop evidence remains pending

[EquipmentReferenceBuildFactory](../../LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs) now creates detached canonical rank/style loadouts. The explicit [reference command](../content-balancing/equipment-reference-builds.md) expands twelve Tier-1 fixtures across ranks 0–5, records exact descriptors and Combat Rating, and executes 72 deterministic production-combat checks. Native, replaced and cleared styles, shield/two-handed/dual-wield hands and snapshot equivalence are covered. The existing CanonicalEquipmentBuildFactory, legacy gear packages and optimizer/calibration policies retain their legacy meaning.

**Impact:** passing existing power/balance tests does not certify the new rank/style economy or intended readiness thresholds. The implemented reference command exercises a fixed synthetic matchup. Run integrated combat/economy checks and migrate calibration policies once source and entry integration is complete. The offline equipment progression proposal analysis is useful input, not certification of live gameplay. Specification: ME-EQP-03, ME-INT-02, AC-11, RM-17.

## What still needs removal from the old systems

The new item model bypasses many retired mechanics, but the repository still contains the old systems for legacy cohorts and historical data. “Removed for equipment progression” and “deleted from the application” are different completion states.

| Previous system | Already addressed | Still required before final retirement |
| --- | --- | --- |
| Gathering professions, tools and nodes — RM-01–03 | equipment progression gathering rewards/XP and source previews are guarded; ordinary content supplies Scrap. | Convert existing tools/materials/history, audit remaining tool gates/equip surfaces, then retire node/profession/tool writers and UI. |
| Equipment manufacture and profession/mastery progression — RM-04–06 | equipment progression crafting and legacy Blueprint learning are blocked; new gear uses content acquisition. | Remove production/recipe/mastery requirements from all remaining consumers and reference builds; convert holdings before deleting runtime services/content. |
| Per-recipe Blueprint unlocks — RM-07 | New Forge learning creates character-wide style rights. | Union old learned recipe/style pairs, define treatment for redundant consumed copies and remove recipe-based learning contracts after compatibility ends. |
| Quality, random production budget, Potential, item XP and rarity progression — RM-08–10 | Canonical stats ignore these mechanics; ranks do not change rarity; shared equipment progression item display suppresses the fields. | Convert legacy items/outliers; remove remaining gates, filters and write paths. Retain old snapshot interpretation until it is safe to remove. |
| Timed Tempering queues and Masterpiece/LevelingItem progression — RM-11–12 | New Forge operations are immediate and produce no legacy attempt/XP/Soulstone rewards. New queue entry is guarded. | Settle existing queues and achievements/history, then remove scheduling, pause/reorder/rarity-stop and associated progression contracts. |
| Equipment materials and catalysts — RM-13 | Combat/dungeon filtering and several guild/event/raid/Prophecy replacements are implemented. | Champion Market/tournament/container paths and administrative issuance guards are integrated; map remaining old inventories and escrow. Preserve unrelated Essence/dungeon resources. |
| Flat one-Scrap salvage — RM-14 | equipment progression salvage uses frozen base entitlement plus recorded paid Scrap. Legacy salvage rejects equipment progression instances. | Retain or convert old holdings under a documented policy; retire the flat policy once no live legacy consumer requires it. |
| Profession objectives, rankings and bonuses — RM-15 | Main quests, several side quests, Prophecies, achievements, guild missions/events and seven Soulstone upgrades have cohort rules. | Complete unfinished-objective conversion and residual title/ranking/bonus audits; Sigil Traces retirement/refund is implemented. Preserve earned rewards/history. |
| Production/queue APIs and client models — RM-16 | Main routes and write guards are in place; Forge replaces the main player actions, and equipment-market controls now separate canonical from legacy fields. | Remove remaining legacy filters/contracts/helpers after supported clients and retained cohorts no longer depend on them. |
| Legacy behavior tests/reference builds — RM-17 | Meaningful equipment progression regression coverage has been added. | Classify old tests as historical compatibility, rewrite or retire. Replace current-game reference loadouts rather than deleting shared stat/Essence/combat tests. |

Do not delete old migrations, renumber shared enums, discard paid/history records, or remove snapshot readers as a shortcut. Those preserve existing equipment, non-equipment rarity, Essences and historical battles. Likewise, do not remove unrelated vendors or the marketplace because gear-selling merchants are excluded.

## Missing transition and validation work

The seven Soulstone refund mappings are one completed transition component. The rest still needs an explicit, versioned and auditable conversion design and implementation:

- Equipment identity/archetype/tier/style/rank mappings, including strong legacy outliers and finite recognition of old work. Old Tempering time/Potential must not be fabricated as paid Scrap receipts.
- A single conversion pass per item across inventory, equipped slots, pending rewards, listings/escrow, transfers and guild loans.
- Learned-style union and duplicate-book/recipe treatment; item-ID-specific tools/materials/catalyst/container conversions; unmatched old Soulstone IDs.
- Earned queue cutoff, pending reward settlement, and impossible unfinished quest/Prophecy/guild/event objective handling while preserving completed history.
- Resumable/idempotent execution, reconciliation of counts/currencies/ownership, failure recovery and an operator recovery plan.
- Complete source/readiness/recipe-consumer audit, equipment progression support tools and observed telemetry for item lifetime, target waits, Scrap supply/spend and trade/binding failures.
- Authenticated full-loop walkthroughs, cross-activity snapshot checks, PostgreSQL transaction/concurrency/rollback validation and conversion rehearsal. Local InMemory concurrency tests are useful but do not replace database acceptance.

Balance remains deliberately deferred: complete builds, hand/set parity, tier replacement, self-found access, investment lifetime and activity reward cadence should be tested after the integrated functionality exists.

## Recommended implementation order

Champion Market, tournaments, catalyst containers, canonical administrative compensation, exclusive sigil income/Sigil Traces refunds and exact equipment-market integration are implemented as of 3 September. Raid readiness remains an identified gap, explicitly deferred by the owner.

1. The shared-tooltip, registered-bonus-provider and non-raid gate audit is now complete for the paths in the consumer audit. Provenance/investment and acquisition inspection is implemented; further LiveOps work is deferred by the owner. The dedicated equipment reference-build command is implemented; legacy balance-policy migration remains later work.
2. Extend tier/source/pricing contracts and author the remaining released-region and special-source coverage.
3. Implement and test the full conversion/compatibility plan, keeping destructive cleanup blocked until holdings and earned work are reconciled.
4. Balance and validate the integrated loop, rehearse conversion/recovery on isolated fixtures or an authorized copy, then complete the coordinated retirement plan. Deployment, shared database changes and activation require separate authorization and remain outside this review.
5. Remove obsolete runtime code and eventually compatibility storage only when no retained cohort, supported client or historical reader needs it.

This order preserves the owner's instruction to finish implementation before balancing. It does not add a gear merchant or a new Soulstone branching system.

## Follow-up verification — 3 September

The reward integration passed 1,991 backend tests through `build/run-tests.ps1`, 34 focused browser tests, the Angular development build, the EF pending-model check and local SQL inspection. All 195 links across six updated Markdown files and whitespace checks passed. These reward-milestone results cover finding 3 and retained behavior. The subsequent administrative milestone closes finding 4; fresh verification is recorded in the current ledger. Finding 2 is closed by the subsequent sigil milestone; other findings remain open or explicitly deferred. The delivery-column migration is generated but unapplied, and activation flags remain off. No live database or deployment validation is claimed.

The administrative follow-up passed **2,012 backend tests**, including **254 focused equipment progression/LiveOps/administration tests**, **17 focused LiveOps browser tests**, the LiveOps production build, the EF pending-model check, all **188 local links** in six updated documents and whitespace validation. It adds no schema change. This verifies compensation grants and local standalone-host content loading, not live PostgreSQL acceptance or an authenticated operator walkthrough.

The sigil follow-up passed **2,030 backend tests**, including **70 focused tests**, **4 focused browser tests**, the EF pending-model check, offline-report regeneration/freshness, all **216 local links** across seven changed Markdown files and whitespace validation. The seven-entry refund mapping is present in API, worker and LiveOps build outputs. No schema change or activation accompanies it; live database acceptance and conversion rehearsal remain outstanding.

The marketplace follow-up passed **2,036 backend tests**, including **68 focused tests**, **20 focused browser tests**, the Angular development build, the EF pending-model check, all **218 local links** across six changed Markdown files and whitespace validation. This closes finding 6 for the implemented market surface. No schema change, activation, deployment or live database acceptance occurred.

The reference-build follow-up passed **2,053 backend tests**, including **24 focused tests**, the 72-build offline command/report, the EF pending-model check, local links across eight updated Markdown files and whitespace validation. Persisted-snapshot equivalence covers two-handed, shield, dual-wield and styled magical builds; repeated seeded production-combat reports match exactly. This completes the reference-generation portion of finding 8. Legacy calibration-policy migration and integrated combat/economy evidence remain pending. No schema change, activation, deployment, content tuning or raid modification occurred.

The leaderboard follow-up implements saved-cohort eligibility for Total Level and the four profession boards, with global Combat Level as the Forge default. Profession data is retained. Filtering precedes ranking, search and paging; publication toggles do not replace saved quest versions. The player waits for cohort access and isolates cached standings across visits. The focused backend build/run passed **30 tests** and browser checks passed **21 tests**; see the [current ledger](equipment-implementation-status.md) for commands and limits. This closes the concrete leaderboard findings in the [consumer audit](equipment-consumer-audit.md), not later-region coverage, conversion or queued-work retirement.

The onboarding/help follow-up fixes guide selection before the journal has loaded, covers the omitted equipment/Essence/Lumo guide paths, and gives the saved modern Lumo quest a combat-focused tour instead of a gathering-panel target. Existing title and Soulstone retirement passed **22 focused backend tests** without backend changes; **34 browser tests**, the player development build, guide/tour assets, documentation links and whitespace checks passed. See the [current ledger](equipment-implementation-status.md) for browser verification and remaining audit limits. Legacy guide/tour content and saved quest versions remain intact.

The retained-dungeon inspection follow-up exposes frozen equipment commitments, bounded saved reward rows and the matching protected receipt, including claimed outcomes. No gameplay resolver is called, current content is not substituted, and overlapping records are not added to holdings or counted as separate awards. The local backend build/run passed **44 tests**, LiveOps browser checks passed **13 tests**, and the LiveOps production build passed. See the [current ledger](equipment-implementation-status.md) and [consumer audit](equipment-consumer-audit.md) for field semantics and remaining full-loot, history and conversion work.

The shared-tooltip and non-raid gate follow-up corrected dungeon gathering/mastery promises, base/instance Blueprint and cache popovers, and canonical comparison metadata. Other registered bonuses and direct non-raid gates were reviewed; no additional profession gate was found. **61 backend tests**, **48 browser tests** and the player development build passed. See the [consumer audit](equipment-consumer-audit.md#shared-tooltips-bonuses-and-activity-gates--3-september-2026). Further LiveOps work is deferred by the owner; later-tier acquisition, conversion and legacy balance-policy migration remain open.

## Verification and limits — original 2 September review

- Fresh review run: `build/run-tests.ps1 -NoBuild` with `VSTestTestCaseFilter` selecting equipment progression, Soulstone, GuildBuilding and StateSyncCommandScopeCatalog tests: **250 passed, 0 failed, 0 skipped**. This used the Release binaries built during the preceding implementation; this review changes no application code.
- Direct content checks confirmed 31 plain archetypes, 13 styles, 16 named identities, six protection pools, ten ordinary areas, two sigil families, 15 equipment progression quest definitions, six Soulstone mappings and five generated migration classes.
- Code-path review verified the gaps above; no live player or shared-environment data was read.
- Prior implementation run, not rerun here: **1,976 backend tests**, **38 focused browser tests**, Angular development build and EF pending-model check passed. Those results do not cover the missing paths identified in this review.
- No new tests, application changes, migrations, configuration activation, database update or deployment were made. This review and a corresponding ledger note are the only source-file changes in this turn.

The right next step is to finish the remaining integration and transition work, not to start over on the equipment model or tune the provisional economy prematurely.
