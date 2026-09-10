# Combat Styles implementation status

Updated 10 September 2026. Gameplay authority: [Combat Styles game design](game-design.md). Delivery contract: [implementation plan](implementation-plan.md).

## Delivered behavior

Each character has one optional **global Combat Style**, used for every battle type. Bastion, Conduit and Reaper are available immediately. Each retains its own XP, progression through level 10, automatic bonuses at every mastery level, three refinements, three upgrades, and upgrade slots at levels 5 and 8. These styles do not add equipment attributes or Essence slots. Shepherd and Gambler remain unimplemented design proposals.

Reaper is delivered in catalog `combat-styles.v7`. Every qualifying direct Essence hit consumes one future tick from each eligible owned Bleed, Burn and Poison stack. Soul Siphon converts that amount to self-healing; Death Sentence banks it as independent 15-second Magical Doom. Last Rites instead consumes all future ticks at or below 35% enemy Health. Closing Hand, Crosscut, Deep Roots, their mastery effects and the level-7 Grave Seed opening use captured tuning. Reaper encounters process due conditions before active attacks. Other encounters keep their historical ordering, and older snapshots omit the optional Reaper tuning. This addition requires no schema migration.

Reaper balance update in `combat-styles.v8`: Death Sentence now applies its additional **flat +10%** before banking Doom. The bonus is captured in tuning, validated, exposed through the DTO, and included in combat, the API preview and the frontend mastery panel. Soul Siphon and Last Rites retain their existing behavior. Closing Hand already checks the opponent's current Health, including opponents already below the threshold and Barrier-only triggering hits; its preview wording now matches that rule. See the [Reaper guide](styles/reaper.md) for current values.

Grave Seed's catalog tuning supplies **Poison(5)** to new battle snapshots. The existing opening runtime and preview read this value; the tuning model's fallback default remains two stacks, and committed battles retain their captured value. Older snapshots without the Death Sentence bonus retain zero extra bonus and omit that field when reserialized. Release the API catalog/runtime and frontend together. No schema migration, environment configuration change or deployment is performed.

Reaper verification on 10 September 2026, before the latest catalog edits described above:

- `./build/run-tests.ps1`: 2,457 backend tests passed, including 52 Reaper cases.
- `npm.cmd run test:ci -- --include=src/app/features/game/character/combat-styles/combat-styles.component.spec.ts --reporters=dots`: 19 page tests passed, including Reaper rendering and mastery.
- `npm.cmd run build:development`: passed.
- `npm.cmd run test:ci -- --reporters=dots`: 771 passed; one existing Essence-page test fails because it expects “your battle loadout” where the committed copy says “your loadout.” Both the expectation and copy mismatch are present in HEAD, outside this change.
- `git diff --check` and Reaper documentation-link checks passed. The sandbox's initial NuGet configuration read was blocked; backend verification succeeded with local configuration/cache access. No services were deployed or databases changed.

Bastion converts all healing received during combat, including healing from other characters and their summons, through the recipient's Fortification configuration. The 25% Health / 75% Barrier allocation, mastery level, refinements, upgrades, and mastery apply to incoming healing from every source. Healing target selection and prioritization remain unchanged and do not account for Barrier; existing cast conditions and self-recovery usefulness checks are preserved. Non-Bastion recipients receive ordinary healing, summons do not inherit Fortification, and out-of-combat recovery is unchanged.

Bastion's **Reprisal** refinement replaces Counterweight in current content. It stores 25% of actual enemy damage absorbed by the character's own Barrier, from any source, up to 10% of Max Health. The next normally cast damaging Essence uses the stored damage once on its first direct enemy attack attempt. Reprisal does not spend Barrier, add an active ability, or add equipment attributes. The existing refinement and preview layout shows its authored rule and the 200-absorbed-to-50-bonus example.

Current saved Counterweight selections and remembered choices resolve to Reprisal for previews, saves, and new battles. Historical committed Counterweight snapshots retain their explicitly captured behavior and replay identity. This compatibility transition preserves progression and requires no schema migration.

The Activity Loadout, Practice introduction, and Saved Builds cards and their supporting commands, services, models, persistence, and client state have been removed. The independent equipment and Essence loadout systems retain their established behavior.

The page uses the game's shared header, sidebar icon, navigation tabs, and colors, with core mechanic beside the compact Milestones timeline, refinement cards, and upgrade choices. Mastery level, its current bonus, and slots appear inside the header. Conduit's mechanic panel explains that the first Essence in your battle loadout is your Channeled Essence. Save/Discard, preview feedback, and the help guide remain available; Page tour and Refresh buttons are removed. Earned XP refreshes through the existing state synchronization without losing the current draft.

Level 7 automatically grants an **Opening Technique** at the beginning of each new battle: Bastion starts with 5% of maximum Health as Barrier, and Conduit starts with 1 Charge. Level 9 unlocks **Upgrade Mastery**, allowing one equipped upgrade to be empowered through the existing Save/Discard flow. Each style remembers its mastery choice. Removing the mastered upgrade clears mastery, and reaching level 9 never makes an automatic selection.

The committed-battle warning has been removed from the editor and character overview. Its client/domain/DTO fields, overview/preview activity lookups, and dedicated captured-style repository query are removed. The activity query used to validate actual mutations remains, along with frozen combat snapshots and ordinary save-error feedback.

Conduit channels the first occupied slot in each battle's actual Essence loadout. The separate Channeled Essence dropdown and remembered global Channeled Essence are removed. While Conduit is equipped, the Essence page marks that slot with a Channeled badge, explains an unsuitable first Essence, and offers Channel Essence to swap an eligible equipped Essence into that position in one save. Slot position does not introduce a casting sequence. The Cost panel, separate Tradeoff field/catalog copy, and negative-only preview/help/tour callouts remain removed. Numerical tuning, resource conversion, progression, and XP requirements are unchanged.

The separate Combat Style display has been removed from the combat summary page. The shared combat component now goes directly from its header to ordinary combat statistics. This removes the display component/spec/model, parent bindings, result and replay response fields, frontend playback copies, and checkpoint summary capture. Existing saved results containing the old metadata remain readable; new result serialization omits it. Actual style configuration, mechanics, XP, and offline balance diagnostics remain intact.

## Bonuses at every mastery level

The separate Core Rank system is replaced by direct mastery-level scaling from 0 through 10. Bastion adds **1% of the base converted Barrier portion per level**; Conduit adds **a flat +1% to charged Channeled Essence effects per level**. Level 0 uses the full base mechanic, level 1 gains the first bonus, and level 10 retains +10% of base converted Barrier and a +10% flat increase to charged Channeled Essence strength. Zero-Charge Conduit effects and milestone unlock levels remain unchanged.

Current content uses `combat-styles.v6`, introducing the Channeled Essence terminology while retaining the first-slot capture policy from v5 and numerical tuning from v4. New snapshots capture level-based tuning; historical committed snapshots use their captured rank-scaled tuning through the compatibility fallback. No earned progression is reset and no schema migration is needed. Current UI and API descriptions use mastery levels and their bonuses rather than a separate rank count. Healing selection, targeting priorities and cast conditions are unchanged.

## Changed areas

| Area | Files and responsibilities |
| --- | --- |
| Domain | `LL/src/Core/Domain/Models/CombatStyles/`: one selection keyed by character, individual progression, definitions and immutable snapshots. Removed combined-preset entities and Character practice/introduction fields. |
| Application/API | `Application/Interfaces/Services/LL/CombatStyles`, `Application/UseCases/CombatStyles`, `CombatStylesController`: global overview, preview and selection contracts. Removed practice/introduction and Character Builds use cases, controller, DTOs and synchronization scope. |
| Services | `Services.LL/CombatStyles/CombatStyleService.cs`: global selection resolution, immediate availability, remembered choices and mastery, captured style XP and milestone preview facts. Removed practice runner and combined-build service. |
| Persistence | Combat Style repository/configuration, DbContext and registrations; `20260908194022_MakeCombatStylesGlobal` and updated model snapshot. Removed preset repository/configuration and preset-only explicit Essence default flag/index. |
| Client | Combat Styles component/state/models/API client, overview widget, generated state scopes and help assets. Deleted practice and preset components/services/models and their obsolete tests. |
| Verification | Global selection, availability, Channeled Essence, progression, migration, boundary, reward and service-provider tests; updated harness request contract and availability assumptions. |
| Documentation | Game design, implementation plan, this status report, and the historical balance-evidence report. |

## Important decisions

- Overview and preview expose both styles without writing database rows. Selection or rewarded combat creates the required progression row within the established transaction. Switching or clearing the global style preserves earned levels and remembered choices.
- Global style editing and numeric previews do not load or validate an alphabetical default Essence loadout. New battles derive Conduit's Channeled Essence from the first occupied slot of their actual activity-assigned or captured loadout. Empty or ineligible first slots produce a clear configuration error; later eligible Essences are not silently selected. The same prepared-ability resolver supplies battle eligibility and the Essence page's eligibility flag, including evolution.
- Legacy current-selection Channeled Essence IDs cannot override slot order and are cleared on normal authorized saves. Historical snapshot Channeled Essence IDs remain immutable. The Essence page's Channel Essence action swaps positions atomically through one existing loadout save and uses its normal failed-save rollback.
- Activity context remains relevant to independent equipment/Essence selection and combat rules; it cannot select another Combat Style.
- Pending idle work settles under the previous configuration before a change is saved. Existing committed snapshots remain immutable, including old snapshots with no style. Newly captured battles use the global selection. Raid and World Tower signup snapshots continue to use their existing refresh/commitment rules.
- Snapshots carry resolved tuning, mastery level, choices, Channeled Essence identity and content version. Live preparation and snapshot creation share the same style resolution, including both PvP sides.
- Opening and mastery tuning travel in a separate snapshot value with inert defaults for historical JSON. Existing committed battles retain their captured rules. Openings apply before combat-start abilities, respect resource caps, do not repeat between continuous waves, and do not trigger Barrier-gain reactions.
- Mastery broadens Prepared Wall to zero Barrier, adds 20% allocated Health to Hold the Breach at zero Barrier, converts Measured Recovery's excess Health healing to Barrier, enables Full Circuit at two or more Charge, enables Partial Flow at one or two Charge, or enables charged Emergency Channel at any Health. Only the selected equipped upgrade receives its enhancement. Normal Shelter distribution, resource caps, and immediate self-recovery rules still apply.
- Idle XP is awarded between encounters from outcome-eligible unbonused combat XP, inside the existing character/action transaction. Dungeon pending/secured rewards retain captured style identity and base XP until the existing claim boundary. Modes without ordinary eligible combat XP do not invent separate style XP rewards.
- Progression, selection and settlement caches follow persistence tracking generations. Clearing tracked entities reloads fresh state; ordinary offline encounters retain cached access.
- The combat summary page has no Combat Style panel. Its component, client data/state, result DTO fields, and playback-frame/checkpoint copies have been removed. Ordinary combat statistics and logs remain. Final engine diagnostics are retained for the offline Balance Harness and engine verification, outside player-facing response contracts.

## Verification

### Creature Focus and Channeled Essence naming — 10 September

- Renamed the Creatures-tab feature to **Creature Focus** across its domain rules, archive entities and DTOs, command/request files, service interfaces and implementations, cooldown state, spawning and reward callers, quests, events, frontend controls, and help. The current endpoint is `POST essence/creature-focus`; the former `creatures/focus` route remains an alias.
- Renamed Conduit's mechanic to **Channeled Essence** across the resolver, effective-style and tuning DTOs, preview formulas, engine state, Essence eligibility, frontend models/state, loadout controls, catalog descriptions, harness code, tests, and guides. The badge reads **Channeled** and the action reads **Channel Essence**. The first occupied battle-loadout slot remains the source.
- Kept existing database columns/index names, queued event wire identities, and saved quest objective keys. Both old and current quest trigger names remain readable. Immutable battle and harness JSON retain their historical serialized members and property order; current code uses Channeled aliases, and public API DTOs expose the current names. Captured content versions v1–v5 retain their original combat-log wording for deterministic replay.
- Catalog version `combat-styles.v6` identifies the new combat-log wording. Structural comparison confirms that all IDs, XP requirements, and numerical tuning match the baseline. Creature Focus's drop/spawn multipliers and eight-hour cooldown are unchanged.
- Full `./build/run-tests.ps1`: **2,393 backend tests passed**, none failed or skipped. The Release build passed with 33 existing compiler/analyzer warnings and no errors. Coverage includes current API names, preserved database column/index mappings, old queued quest events and trigger names, exact default/Web JSON shapes and harness hashes, and historical/current Conduit combat-log wording. Results: `TestResults/tests/combat-style-terminology-full.trx`.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts --include=src/app/core/services/api/essences/*.spec.ts --include=src/app/features/game/character/essences/essences.component.spec.ts`: **83 frontend tests passed**. TypeScript spec compilation and `npm.cmd run build:development` passed without build warnings. Checks include Creature Focus routing/cooldowns, response rendering, and Channeled Essence swaps/rollback/layout. Npm used its cache under TEMP.
- Catalog, fixture, quest and help JSON parse successfully; scoped whitespace and terminology checks pass; **69 local documentation links** resolve. All required verification completed. No authenticated live-backend check, migration, database operation, environment configuration change, or deployment was performed. API/worker/catalog and frontend changes should be released together. The harness's existing requirement to replay against matching installed content remains unchanged.

### First-slot Conduit Channeled Essence — 9 September

- `CombatStyleService.cs` now derives Channeled Essence from the first occupied Essence slot of the actual battle loadout. `EssenceSystemService.cs` orders live loadouts by `SlotIndex`; snapshot creation and hydration already preserve that ordering. Different activities can supply different Channeled Essences while retaining one global style. Empty or ineligible first Essences are rejected at battle preparation without skipping to a later slot. Existing committed snapshots retain their captured Channeled Essence IDs.
- Removed manual Channeled Essence from current selection/entry DTOs, the overview options list, frontend selection state, and the Combat Styles dropdown. Style saves and numeric previews are independent of the alphabetical fallback loadout. Stale client or persisted Channeled Essence selections cannot override slot order; normal saves clear legacy choice columns without changing historical snapshots or migrating the schema.
- Added `IChanneledEssenceResolver` and `ChanneledEssenceResolver` to share prepared-ability eligibility between battle resolution and `PlayerEssenceDto.IsChanneledEssenceEligible`, including evolution. Updated the Essence mapping converter and game/standalone LiveOps registrations. On the Essence page, a Channeled badge follows the first occupied slot while Conduit is equipped. Channel Essence swaps another eligible equipped Essence into that position in one existing loadout save, preserves holes and all equipped Essences, prevents concurrent clicks, and uses normal failed-save rollback. Removed obsolete Essence/equipment invalidations of Combat Style previews.
- Catalog version `combat-styles.v5` identifies the new capture policy. Structural comparison confirms that only the version and Conduit core description changed; tuning, XP, refinements, and upgrades are unchanged. Updated current guides/design/plan, frontend help/tour, and the offline harness. All **16 Conduit fixture builds** now put their intended Channeled Essence first; legacy recipe Channeled Essence IDs are assertions only and cannot override the slot.
- Final full `./build/run-tests.ps1`: **2,382 tests passed**, none failed or skipped. The Release build passed with five existing test compiler/analyzer warnings and no errors. New coverage includes every activity, supplied snapshot order, stale client/persisted IDs, loadout-independent save/preview, empty/ineligible first slots, unsorted persisted slots and gaps, evolution-aware eligibility, DTO mapping, snapshot capture, standalone registrations, and deterministic harness replay. Two initial fixture issues (target-typed params arguments and omitted ability IDs for evolution) were corrected before this passing run. Results: `TestResults/tests/combat-styles-first-slot-focus-full.trx`.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts --include=src/app/core/services/api/essences/*.spec.ts --include=src/app/features/game/character/essences/essences.component.spec.ts`: **80 frontend tests passed**. Coverage includes one-request swaps, pending-save protection, failed-save rollback with an unsaved name, loadout switching, saved-style changes, canonical eligibility, and real markup at 320, 390, and 1440 pixels. `npm.cmd run build:development` and TypeScript spec compilation passed without errors; the development build reported no warnings. Npm used its cache under TEMP.
- Catalog, fixture, and help JSON; canonical frontend description; scoped whitespace; and **40 local documentation links** passed verification. No required command remains blocked. No authenticated live-backend check, database operation, migration, or deployment was performed. No environment configuration change is required; API, worker, catalog, and frontend changes should be released together.

### Flat bonus wording — 9 September

- Updated six Conduit catalog descriptions, the service preview formatter, frontend mastery labels, help, and current Combat Styles guides to use “+X% flat increase” or “adds a flat +X%.” Existing backend/frontend fixtures use matching text. Multiplicative bonuses retain labels such as “+20% Health recovery (×1.2),” and Bastion mastery still explicitly scales its base converted Barrier.
- Catalog comparison confirmed that only six description fields changed; all tuning, IDs, XP, and content version are unchanged. No gameplay calculations, selection behavior, response contracts, or layout styles changed. Historical verification entries and unimplemented style proposals retain their original wording.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyleFoundationTests'`: **66 tests passed**, none failed or skipped. The Release build passed with five existing test compiler/analyzer warnings and no errors. Results: `TestResults/tests/combat-styles-flat-bonus-copy.trx`.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts`: **45 frontend tests passed**, including responsive layout checks. `npm.cmd run build:development` passed without warnings or errors. Npm used its cache under TEMP.
- Catalog/help JSON, matching frontend fixtures, scoped whitespace, and current display-copy checks passed. All **41 local documentation links** resolve, and all **nine Conduit refinement/upgrade/mastery descriptions** match the catalog. All required verification completed.
- No migration or environment configuration change is required. No database operation or deployment was performed. The API/catalog and frontend/help copy should be released together.

### Conduit wording review — 9 September

- Rewrote Conduit's core description, three refinements, and Emergency Channel/mastery copy in the catalog. The text now defines Channeled Essence and Charge first, explains which Essences can build Charge and how often, and describes percentages as normal effect strength. Full Circuit, Partial Flow, and the opening description were already clear and retain their approved wording.
- Updated `CombatStyleService.cs` preview labels from “effect amount” to “of normal strength,” replaced “Maximum preparation”/“contributors” with a Charge limit and plain charging rules, and clarified upgrade conditions. `CombatStyleRules.cs` now explains which equipped Essence to choose in its Channeled Essence validation message. The Angular component, help guide, current Conduit documentation, and existing fixtures use matching terminology. No eligibility rules, formulas, Charge behavior, state logic, or layout styles changed.
- The catalog comparison confirmed exactly **six changed Conduit description fields**. Bastion content, all tuning/IDs, XP requirements, and content version are unchanged.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyleFoundationTests'`: **66 tests passed**, none failed or skipped. The Release build passed with six existing compiler/analyzer warnings and no errors. Results: `TestResults/tests/combat-styles-conduit-copy.trx`.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts`: **45 frontend tests passed**, including the full Conduit description and longer helper/mastery text at 320, 390, and 1440 pixels. `npm.cmd run build:development` passed without warnings or errors. Npm used its cache under TEMP. Catalog/help JSON, matching catalog fixtures, and scoped whitespace checks passed.
- The Conduit guide, matching game-design passages, and overview directory line were reviewed together. The three refinement descriptions and six upgrade/mastery descriptions match the catalog; **35 local documentation links** resolve, and updated headings have no stale incoming links. All required verification completed.
- No migration or environment configuration change is required. No database operation or deployment was performed. The API/catalog and frontend/help copy should be released together.

### Additive and multiplicative preview bonuses — 9 September

- Updated `CombatStyleService.cs` preview facts with the approved Prepared Wall and Hold the Breach condition sentences. Their values now show **+7.5 percentage points (additive)** alongside **+15 Barrier** for the existing 200-point healing example. The percentage is derived from the same captured conversion and upgrade tuning as the amount.
- Measured Recovery and Hold the Breach mastery show **+20% Health recovery (×1.2)** alongside their example totals. Selected Conduit upgrades expose **+5 percentage points (additive)** and their eligibility. Measured Recovery and Conduit's Full Circuit/Partial Flow identify bonuses already included in the main examples. No catalog tuning, combat mechanics, progression, or response-contract changes were made.
- Updated the Bastion/Conduit guides, game-design preview explanations, help text, and existing frontend/backend preview fixtures and checks. The examples distinguish `75% + 7.5 points = 82.5%` Barrier conversion, `25% × 1.2 = 30%` Health recovery, sequential recovery multipliers reaching 36%, and `150% + 5 points = 155%` Channeled Essence effects.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyleFoundationTests'`: **66 tests passed**, none failed or skipped. The Release build passed with five existing test compiler/analyzer warnings and no errors. Existing coverage now checks additive Barrier bonuses across mastery levels, 60/72/240 Health examples, and unchanged Conduit outputs across 0–3 Charge. Results: `TestResults/tests/combat-styles-preview-bonuses.trx`.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts`: **45 frontend tests passed**. Existing responsive coverage now checks the long additive/multiplier values and their conditions at 320, 390, and 1440 pixels; rows remain inside the mechanic panel without overlap. Two stale assertions were aligned with the current header/opening text, preserving existing production behavior. Npm used its cache under TEMP. No template, CSS, or production TypeScript changes were needed, so a separate frontend build was unnecessary.
- Service/test/documentation whitespace and help JSON checks passed, and all **24 local links** in the updated guides/design resolve. All required verification completed. No migration or environment configuration change is required; no database operation or deployment was performed. The updated preview values are served by the API, alongside the frontend help update.

### Clearer upgrade descriptions — 9 September

- Applied the approved wording to all six upgrade descriptions and all six Upgrade Mastery descriptions in the current catalog. Updated the shared desktop/mobile section introductions, Bastion's conversion note, existing frontend fixtures, help guide, overview, game design, and both implemented style guides.
- The catalog comparison confirmed that only those twelve description fields changed. IDs, content version, XP, tuning, and progression remain unchanged. The clearer 7.5%-of-healing wording is the same Barrier bonus as the existing `75% × 10%` calculation; conversion and sharing rules still apply.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyleFoundationTests'`: **66 tests passed**, none failed or skipped. The Release build passed with five existing test compiler/analyzer warnings and no errors. Results: `TestResults/tests/combat-styles-upgrade-copy.trx`.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts`: **45 frontend tests passed**, including the approved longer descriptions, reserved selection-status space, and desktop/mobile layout checks. `npm.cmd run build:development` passed without warnings or errors. Npm used its cache under TEMP. Both layouts receive one shared introduction, with their existing selection counts preserved.
- Scoped catalog/documentation whitespace checks passed. All **12 approved descriptions** match between the catalog and current guides/design, and **39 local documentation links** resolve. No new tests were added for this wording change.
- All required verification commands completed. No migration or environment configuration change is required. No database operation or deployment was performed. The approved descriptions come from the API's catalog, so the updated catalog and frontend/help assets belong in the same release.

### Reprisal replaces Counterweight — 9 September

- Updated `FastCombatEngine.CombatStyles.cs` and `FastCombatEngine.cs` to store actual hostile Barrier absorption and apply one reserved bonus to the next qualifying Essence attack attempt. The bank preserves fractions, shares its current-Max-Health cap with reserved damage, and carries across continuous waves within the same battle. Healing conversion, target selection, and cast conditions are unchanged.
- Updated `CombatStyleSnapshot.cs`, `CombatStyleCombatSummary.cs`, `CombatStyleRules.cs`, and `CombatStyleService.cs` for captured Reprisal tuning, offline diagnostics, current-selection compatibility, validation, and preview facts. Saved Counterweight choices resolve to Reprisal without database writes during reads; the next save stores the current ID. Historical committed Counterweight snapshots preserve their behavior and serialized identity.
- Catalog content version `combat-styles.v4` authors 25% absorption storage and a 10%-of-Max-Health cap. Updated the help guide and Combat Styles documentation. Existing data-driven refinement cards and preview facts display Reprisal without component or state changes.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyle'`: **239 tests passed**, none failed or skipped. The Release build passed with five existing test compiler/analyzer warnings and no errors. The initial test constructor ambiguity and listener-order fixture issue were corrected before this passing run. Results: `TestResults/tests/combat-styles-reprisal-focused.trx`.
- Full `./build/run-tests.ps1 -NoBuild`: **2,348 tests passed**, none failed or skipped, covering the shared damage pipeline and the complete backend suite. Results: `TestResults/tests/combat-styles-reprisal-full.trx`. All required verification commands completed.
- Added coverage in `CombatStyleReprisalEngineTests.cs` and updated `CombatStyleEngineTests.cs`, `CombatStyleFoundationTests.cs`, and `CombatStyleHarnessTests.cs`. Checks include Barrier ownership, hostile absorption, caps and fractions, cast reservation timing, blocked/no-target casts, first-attempt misses, multi-hit/area isolation, critical/Lifesteal/reaction isolation, continuous waves, current selections, immutable historical snapshots, authored tuning, and deterministic replay.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts`: **45 frontend tests passed**. `npm.cmd run build:development` passed without warnings or errors. Npm used its cache under TEMP. Scoped whitespace, catalog/help JSON, and **69 local documentation links** passed verification.
- No schema migration or environment configuration change is required for Reprisal. No database operation, backend startup, or deployment was performed. API, worker, catalog, and frontend help changes should ship together; the stored-damage runtime belongs to the active encounter and does not add checkpoint persistence.

### Mastery bonus at every level — 9 September

- Changed the progression/snapshot rules, shared combat calculations, service previews, entry DTOs, idle encounter advancement, and the catalog. Content version `combat-styles.v3` introduced 0.01 per mastery level for both implemented bonuses. Existing saved progression is preserved; historical snapshots retain their captured tuning and explicit rank values, including zero, through serialization.
- Updated the Combat Styles page, overview widget, shared frontend models, help/tour copy, and matching tests. The header and Milestones card show the actual mastery bonus; the timeline includes every level from 0 through 10. Core Rank counters and pips are removed. Live XP updates and draft preservation remain covered.
- Updated all five style guides, the overview, design, and plan. Proposed styles retain their former level-10 totals with per-level scaling. Historical balance results remain marked as historical; current fixture notes acknowledge that levels 6/7 and 8/9 also differ by one mastery bonus.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyle|FullyQualifiedName~IdleCombatResolutionSessionFactory'`: **216 tests passed** before the final legacy-zero serialization regression was added. The final full `./build/run-tests.ps1` rebuilt successfully and passed **2,319 tests**, with none failed or skipped. The build reported 33 existing compiler/analyzer warnings and no errors. Results: `TestResults/tests/combat-styles-mastery-bonus-focused.trx` and `TestResults/tests/combat-styles-mastery-bonus-full.trx`.
- Backend coverage includes level 0, odd/even levels, unchanged level-10 totals, Conduit refinement curves and zero-Charge output, authored tuning, service previews, idle advancement between encounters, immutable captured builds, and legacy snapshot/hash compatibility. Healing targeting remains unchanged.
- `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts`: **45 tests passed**, including odd-level values, level-9/10 live updates, refinement overrides, and existing desktop/mobile layout checks. `npm.cmd run build:development` passed without warnings or errors. Npm used its cache under TEMP.
- Scoped whitespace, catalog/help JSON, and documentation link checks passed. All required verification completed. No schema migration or environment configuration change is needed, and no database operation or deployment was performed. API, worker, catalog, and frontend changes should ship together.

The entries below record completed checks against their stated versions. References to Core Ranks, even-level scaling, or fixture pairs with equal rank are historical and superseded by continuous mastery-level bonuses. Historical Counterweight behavior is superseded by Reprisal for current selections and retained only for committed legacy snapshots. These counts and outcomes remain historical evidence; they do not verify later scaling or refinement changes.

### Bastion: all healing received — 9 September

- Runtime changes are limited to `FastCombatEngine.CombatStyles.cs` and its recovery call sites in `FastCombatEngine.cs`. Actual conversion checks the recipient; the existing self/owned-summon gate remains specific to cast usefulness. Healing modifiers and Conduit scaling apply once before conversion, and converted Barrier retains the receiving Bastion's attribution and reaction rules.
- Updated the content catalog's Bastion/Rebuild descriptions, `CombatStyleService.cs` preview label, the frontend rank examples and matching tests, the help guide, and the Bastion/overview/game-design documentation.
- `./build/run-tests.ps1 -Filter 'FullyQualifiedName~CombatStyle'`: Release build passed with zero warnings or errors; **180 tests passed**, none failed or skipped. The initial sandboxed build could not read the user's NuGet configuration; the approved rerun completed successfully.
- Full `./build/run-tests.ps1 -NoBuild`: **2,271 tests passed**, none failed or skipped. Results are retained locally in `TestResults/tests/bastion-received-healing-focused.trx` and `TestResults/tests/bastion-received-healing-full.trx`.
- Regression coverage includes external players and summons, unchanged lowest-Health targeting despite substantial Barrier, authored Health conditions, unaffected non-Bastion/summon recipients, Rebuild, recipient ranks/upgrades/mastery, Shelter caps without reconversion, full-Health group healing, and single Conduit scaling. Existing regeneration and Lifesteal coverage also passed.
- Frontend `npm.cmd run test:ci -- --include=src/app/core/services/api/combat-styles/*.spec.ts --include=src/app/features/game/character/combat-styles/*.spec.ts`: **45 tests passed**. `npm.cmd run build:development` passed. Npm used its cache under TEMP.
- Scoped whitespace, catalog/help JSON, and 64 local documentation link-target checks passed. All required verification completed. No new migration or configuration change is required; no database operation, backend startup, or deployment was performed. The engine, catalog, and frontend changes belong in the same later release.

### Opening Technique and Upgrade Mastery — 9 September

- Backend Release test-project build with `--no-restore --verbosity quiet` passed with zero warnings or errors. Full `./build/run-tests.ps1 -NoBuild`: **2,221 passed, zero failed or skipped**. Results: `TestResults/tests/combat-styles-milestones-full.trx`.
- Engine coverage includes the level 6/7 and 8/9 boundaries, each of the six enhancements, invalid or unequipped mastery, Rebuild interactions, Shelter recipient caps, initial checkpoint statistics, continuous waves, summon exclusion, and historical snapshot compatibility.
- Service/API/migration coverage checks remembered choices, clearing mastery, manual selection after the unlock, snapshot tuning, preview facts, and the additive migration. Offline harness recipes capture and validate mastery and reject tampering; paired level 6/7 and 8/9 fixture builds isolate the new milestones at equal core rank.
- **31 focused frontend tests passed**, including live XP unlocks without a page refresh, preserving drafts, Save/Discard, switching styles, upgrade removal, locked choices, and mobile layout checks at 320px and 390px. `npm.cmd run build:development` passed. The later legend correction to match the concurrent level-zero work was text-only.
- Persistence Release build and `dotnet ef migrations has-pending-model-changes --no-build` passed. The additive mastery migration and concurrent level-zero migration are compatible in the model snapshot. The first focused run exposed one ordering assumption and three progression expectations; these were corrected before the successful full run.
- Scoped whitespace checks passed for tracked changes and 60 new Combat Style files, accounting for the repository's Windows line endings. All required verification commands completed.
- No database migration, API restart, or deployment was performed. New API/schema behavior was verified through automated checks; no authenticated end-to-end check against the new schema was run. Numerical milestone tuning remains subject to balance playtesting.

### Battle warning removal — 9 September

- Removed the banner from `combat-styles.component.html` and `combat-style-overview.component.ts`, plus its frontend model fields and obsolete state-test setup.
- Removed `CommittedStyle`/`CommittedActivity` from overview domain/DTO contracts and the corresponding service lookups/dependency. Removed `GetCommittedStyleAsync` from the interface/repository; retained the activity query used by the existing mutation boundary.
- Backend Release build with `--no-restore` passed. `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~CombatStyle|FullyQualifiedName~WorkerServiceProvider'`: **103 passed, zero failed or skipped**. Results: `TestResults/tests/combat-styles-warning-removal.trx`.
- Focused frontend style/component/API suite: **17 passed**. Existing save-error and successful-save coverage remains; the obsolete banner test setup was removed.
- Frontend development build and scoped whitespace checks passed. All required verification commands completed.
- No migration or game configuration change is required. No database update, service restart, or deployment was performed.

### Channeled Essence dropdown and presentation wording

- Backend Release build with `--no-restore` passed. `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~CombatStyle'`: **101 passed, zero failed or skipped**, including mapping/preview contracts and unchanged style mechanics.
- Focused frontend suite: **20 passed**, covering shared-dropdown keyboard selection, disabled/ineligible options, a missing saved Channeled Essence, pending-save guards, retained layout, and removed wording.
- `npm.cmd run build:development` and scoped whitespace checks passed. All required verification commands completed.
- The versioned catalog's IDs, tuning, and XP requirements were compared before/after and are unchanged.
- Authenticated browser check confirmed the textured shared dropdown, name-only options, Channeled Essence selection updating the draft/preview, and removal of the Cost panel. Discard restored the initial empty selection; no character changes were saved.
- The browser used the already-running backend, which still served its old preview/choice wording. The updated backend wording is covered by the contract tests; no backend restart or deployment was performed.
- No new migration or configuration setting is required. The backend and its catalog must be reloaded together to serve the updated response/copy.

### Combat summary display removal

- Frontend development build passed.
- 67 affected combat UI, playback, and API frontend tests passed.
- Backend Release build with `--no-restore` passed. Full `./build/run-tests.ps1 -NoBuild`: **2,112 passed, zero failed or skipped**. This includes result/replay compatibility, ordinary statistics, continuous waves, actual style mechanics, and the offline Balance Harness. Results: `TestResults/tests/combat-styles-display-removal-full.trx`.
- Scoped whitespace and removed-reference checks passed. All required commands completed; the first frontend test attempt encountered a transient generated-file write error, and the unchanged retry passed.
- No new migration or configuration change is required for this display removal. No database operation or deployment was performed. Updated API/client contracts omit the former optional display metadata.

### Previous global-selection cleanup

| Check | Result |
| --- | --- |
| Backend Release build with `--no-restore` | Passed; existing compiler/analyzer warnings remain. |
| Focused repository-wrapper backend run | **105 passed; zero failed or skipped.** Combat Style, service-provider and idle-sharing tests. |
| Full `./build/run-tests.ps1 -NoBuild` | **2,104 passed; zero failed or skipped.** |
| Frontend generated state-scope contract | Passed; obsolete `character-builds` scope removed. |
| Frontend ChromeHeadlessCI suite | **706 passed.** Includes global transport/state and page DOM/control-placement regressions. |
| Final focused frontend run | **16 passed.** Includes the final regression allowing Save after cached committed-battle status becomes stale; the server still enforces active commitments. |
| Frontend development build | Passed. |
| EF forward/down SQL generation and inspection | Passed without applying a database migration. |
| EF `migrations has-pending-model-changes --no-build` | No pending model changes. |
| Offline migration regression tests | Passed in the backend focused run: final model, default-row retention/order, and downgrade schema defaults. |
| Scoped whitespace checks | Passed for tracked changes and new Combat Style files. |

Backend commands:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore
./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~CombatStyle|FullyQualifiedName~WorkerServiceProvider|FullyQualifiedName~IdleCombatExperienceSharing'
./build/run-tests.ps1 -NoBuild
```

The ordinary test-wrapper build initially could not read the sandbox-restricted user NuGet configuration. An explicit Release build with `--no-restore` succeeded using existing restored packages; all backend tests ran through the repository wrapper with `-NoBuild`. Frontend verification used npm only, with its cache outside the checkout. The intended focused frontend command ran the entire suite because of PowerShell argument forwarding; the full suite passed.

The backend results are retained locally at `TestResults/tests/combat-styles-global-focused.trx` and `TestResults/tests/combat-styles-global-full.trx`.

## Migration and runtime implications

`20260909082202_AddCombatStyleUpgradeMastery` adds nullable `MasteredUpgradeId` columns to the global selection and each style's remembered choices. Existing records retain no mastery until the player chooses one. Catalog content version `combat-styles.v2` includes opening descriptions, all six mastery descriptions, and numerical milestone tuning; the catalog remains at its existing file path. This additive migration was generated, not applied. A later coordinated schema, API/worker, catalog, and frontend release is required.

The historical `20260908171158_AddCombatStylesAndSavedBuilds` migration is retained for compatibility with environments that already have it. The new `20260908194022_MakeCombatStylesGlobal` migration keeps the former default (`Activity = None`) selection as the global choice, deletes activity overrides, and changes the selection key to `CharacterId`. Characters with no former default keep an empty selection. Earned style progression and frozen combat snapshots are preserved.

The new migration drops combined preset tables, introduction/practice columns, and the preset-only Essence default flag/index. Removed override/preset data cannot be recovered by its downgrade; the downgrade restores schema shape only.

No database migration was applied, backend started, or service deployed during this change. API, worker, frontend, and schema contracts require a coordinated later release. Application startup migration behavior must be accounted for during that release.

No live PostgreSQL migration or authenticated end-to-end check against the new API/schema was performed. Offline migration/model checks and frontend tests do not substitute for those checks. Earlier numerical balance experiments remain documented in [initial balance evidence](verification.md); those historical experiments predate Opening Technique and Upgrade Mastery and do not establish balance for the new effects.
