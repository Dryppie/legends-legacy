# Consumable equipment blueprints

Implemented 5 September 2026 in the primary LL game.

## Player behavior

- Base and variant equipment both drop. Rarity and archetype rolls are independent of the number of authored variants.
- Variants add a separate 15% stat budget after base allocation. Base attributes are never sacrificed. Replacing a variant can remove the old bonus attributes, but preserves the full base allocation.
- A compatible consumable blueprint and 100 Cinders per item tier guarantee conversion. Quality, rarity, attribute roll, tier, reinforcement and binding are retained.
- Dungeon blueprint choices roll independently of equipment, at 25% per completion, with a choice guaranteed on the fourth miss. The guarantee counts completions rather than claims and is shared across a family's grades.
- The equipment panel displays compatible variants, held counts, sources, guarantee progress, exact before/after stats and a confirmation action.
- The introductory Soul Archive reward now includes Fury, compatible with every Arms Chest weapon. The existing Cinder grant funds an optional first conversion.

## Changed areas

The domain evaluator and frozen state carry additive-versus-historical variant evaluation. The upgrade policy, service, repository, DTOs, controller and command pipeline add conversion using existing quote fingerprints, character locks, operation receipts and state synchronization. Equipped conversion settles earned combat through the existing reinforcement path before changing stats.

The new blueprint catalog supplies prices, sources, variant probabilities and consumable definitions. Area and dungeon acquisition select bases and then apply compatible themed variants. Selection containers reuse the existing inventory choice flow. Dungeon progress uses its own persisted counter and an existing run-state JSON marker to prevent duplicate processing.

Player UI changes are confined to the equipment API client and upgrade panel, plus the onboarding help text. Tests cover additive base preservation across current content, conversion order, variant replacement, historical descriptors, payment validation, receipt retry behavior and dungeon guarantees. The backend test runner now accepts an optional `-Filter` for focused checks.

## Release requirements

The generated `AddEquipmentBlueprintProgress` migration adds only the new counter table. Apply it through the normal release process before this code handles dungeon completions. The current item-content seed must include the new blueprint and choice-container items. No migration was applied and no service was deployed.

Existing descriptors retain their old allocation mode until explicitly converted to a different variant. No inventory-wide stat rewrite or compensation migration is included. Existing unclaimed quest rewards use the updated authored reward list; already-claimed quests do not grant a second introductory reward.

The 15% bonus increases variant power relative to the previous allocation model. The additive budget and set bonuses require play-balance monitoring; functional tests are not a combat-balance signoff. Initial source pools deliberately carry early families into region two so blueprint replacement does not require returning to trivial content.

## Verification

- `build/run-tests.ps1 -Configuration BlueprintVerification -Filter 'FullyQualifiedName~Equipment|FullyQualifiedName~CombatAcquisition|FullyQualifiedName~QuestSystem|FullyQualifiedName~StateSync'`: 301 passed, including conversion payment/receipt persistence and dungeon guarantee tests.
- `npm run build:development`: passed.
- Player `npm run test:ci`: 630 passed before the three new panel tests were added. npm's argument handling expanded the intended focused run to the full suite.
- `npm.cmd exec -- ng test --watch=false --browsers=ChromeHeadlessCI --karma-config=karma.conf.cjs --include=src/app/shared/components/equipment/equipment-upgrade-panel/equipment-upgrade-panel.component.spec.ts --include=src/app/core/services/api/equipment/equipment.service.spec.ts`: all six checks passed, including the three new panel tests.
- EF `migrations has-pending-model-changes` with `--configuration BlueprintVerification --no-build`: no pending changes at migration verification time.
- `git diff --check`: passed.

Full-suite verification is incomplete. The isolated full run was stopped after 1,823 passing tests to switch to the faster Release configuration. The final Release rebuild then encountered concurrent equipment cleanup in the shared checkout: services still referenced removed `EquipmentInstance`/`EquipmentSnapshot` members, including `BaseRecipeId` and `IsLevelingItem`. Those cleanup edits were left intact. The focused results above precede that concurrent cleanup; repeat the final build after it is complete.
