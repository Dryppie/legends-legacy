# Consumable equipment blueprints

Implemented 5 September 2026 in the primary LL game.

## Player behavior

- Base and variant equipment both drop. Rarity and archetype rolls are independent of the number of authored variants.
- Variants add a separate 15% stat budget after base allocation. Base attributes are never sacrificed. Replacing a variant can remove the old bonus attributes, but preserves the full base allocation.
- A compatible consumable blueprint and 100 Cinders per item tier guarantee conversion. Quality, rarity, attribute roll, tier, reinforcement and binding are retained.
- Dungeons drop one blueprint directly, independently of equipment, at 25% per completion. After three misses the fourth clear guarantees a blueprint. The guarantee counts completions rather than claims and is shared across a family's grades.
- Goblin Mines selects Fury/Phoenix uniformly; Forgotten Catacombs selects Arcane/Endurance uniformly; Tangled Cave drops Execution only; Great Tree drops Spirit only. These blueprint pools are separate from the broader equipment variant pools. Gravebound and Raidforged definitions and consumables are removed.
- Dungeon Run Rewards shows the actual blueprint names and current per-blueprint probabilities, with a notice for the fourth-clear guarantee. Mastery affects equipment odds, not blueprint odds.
- The equipment panel displays compatible variants, held counts, sources, guarantee progress, exact before/after stats and a confirmation action.
- The introductory Soul Archive reward now includes Fury, compatible with every Arms Chest weapon. The existing Cinder grant funds an optional first conversion.

## Changed areas

The domain evaluator and frozen state carry additive-versus-historical variant evaluation. The upgrade policy, service, repository, DTOs, controller and command pipeline add conversion using existing quote fingerprints, character locks, operation receipts and state synchronization. Equipped conversion settles earned combat through the existing reinforcement path before changing stats.

The blueprint catalog supplies prices, sources, variant probabilities and consumable definitions. Area and dungeon acquisition select bases and then apply compatible themed variants. Dungeon blueprint awards use a separate deterministic selection so completion retries keep the same item. Dungeon progress uses its own persisted counter and an existing run-state JSON marker to prevent duplicate processing. The general legacy selection box and unrelated Essence/Arms Chest choices retain the existing inventory flow.

Player UI changes are confined to the equipment API client and upgrade panel, plus the onboarding help text. Tests cover additive base preservation across current content, conversion order, variant replacement, historical descriptors, payment validation, receipt retry behavior and dungeon guarantees. The backend test runner now accepts an optional `-Filter` for focused checks.

## Release requirements

The original `AddEquipmentBlueprintProgress` migration adds the counter table. The 7 September `DirectDungeonBlueprintDrops` migration converts existing dungeon choice stacks and pending rewards into direct blueprints, retaining quantity. Two-blueprint families select once per existing stack using its immutable ID. It merges inventory stacks while retaining favorites/unseen state, removes retired blueprint consumables and their listings, and refunds unfilled buy orders for removed consumables/choices. Completed trades and economy history remain intact; referenced obsolete item definitions are kept bound and excluded from Bazaar trading. Unreferenced obsolete definitions are deleted. Ship the migration and updated content through the normal release process. No migration has been applied and no service has been deployed.

Existing descriptors retain their old allocation mode until explicitly converted to a different variant. No inventory-wide stat rewrite or compensation migration is included. Existing unclaimed quest rewards use the updated authored reward list; already-claimed quests do not grant a second introductory reward.

The disposition of already-owned Gravebound/Raidforged equipment is still pending a user decision: delete those instances, or remove their variants while preserving the underlying gear. The current migration handles blueprint consumables and choices only. Finish that equipment migration before releasing the retired style catalog.

The 15% bonus increases variant power relative to the previous allocation model. The additive budget and set bonuses require play-balance monitoring; functional tests are not a combat-balance signoff. Equipment variant pools carry early families into region two; direct blueprint sources follow the narrower dungeon pools above.

## Verification

7 September direct-drop update: 80 focused backend tests passed through `build/run-tests.ps1`; 16 dungeon-card tests passed through `npm.cmd run test:ci`; the Services/Persistence/test-project builds and `npm.cmd run build:development` passed. EF generated the migration SQL and reported no pending model changes. PostgreSQL execution of the data migration was not performed. `git diff --check` passed.

Original 5 September implementation verification:

- `build/run-tests.ps1 -Configuration BlueprintVerification -Filter 'FullyQualifiedName~Equipment|FullyQualifiedName~CombatAcquisition|FullyQualifiedName~QuestSystem|FullyQualifiedName~StateSync'`: 301 passed, including conversion payment/receipt persistence and dungeon guarantee tests.
- `npm run build:development`: passed.
- Player `npm run test:ci`: 630 passed before the three new panel tests were added. npm's argument handling expanded the intended focused run to the full suite.
- `npm.cmd exec -- ng test --watch=false --browsers=ChromeHeadlessCI --karma-config=karma.conf.cjs --include=src/app/shared/components/equipment/equipment-upgrade-panel/equipment-upgrade-panel.component.spec.ts --include=src/app/core/services/api/equipment/equipment.service.spec.ts`: all six checks passed, including the three new panel tests.
- EF `migrations has-pending-model-changes` with `--configuration BlueprintVerification --no-build`: no pending changes at migration verification time.
- `git diff --check`: passed.

Full-suite verification is incomplete. The isolated full run was stopped after 1,823 passing tests to switch to the faster Release configuration. The final Release rebuild then encountered concurrent equipment cleanup in the shared checkout: services still referenced removed `EquipmentInstance`/`EquipmentSnapshot` members, including `BaseRecipeId` and `IsLevelingItem`. Those cleanup edits were left intact. The focused results above precede that concurrent cleanup; repeat the final build after it is complete.
