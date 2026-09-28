# Fixed set bonuses and equipment presentation

The intermediate numeric values recorded below were subsequently superseded by the user's request to restore the original set bonuses. See [the final restoration record](restored-set-bonuses-2026-09-28.md). The fixed modifier rule, Gear Power label and removal of the profile breakdown remain in effect.

Set attribute bonuses now grant their authored amounts once the required piece count is reached. Phoenix's two-piece bonus always grants 6.666667 Restoration, displayed as 6.67. Item tier, rarity, quality, reinforcement, rolls and allocation no longer multiply set modifiers. Distinct-item counting, two-handed weapons counting as two slots, cumulative threshold activation and granted set abilities retain their existing behavior.

## Changed files and decisions

- `LL/src/Core/Domain/Models/Items/Equipments/Sets/EquipmentSetBonusResolver.cs`: applies each authored amount directly and removes the unused aggregate reservation calculations. `EquipmentSetDefinition.cs` clarifies that the remaining reservation flag controls authoring-budget validation only.
- `LL/src/API/API.LL/Data/equipment/equipment-sets.v1.json`, `equipment-sets.v3.json` and `equipment-sets.v4.json`: remove the misleading allocation-scaling sentence. Authored amounts and thresholds are preserved.
- `LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display/equipment-display.component.html`: restores **Gear Power** in all four display/comparison locations and removes the specialization/profile, Core, Specialization, Style stats and Reserved for set breakdown.
- `LL/tests/EssenceSystem.Tests/EquipmentSetBonusResolverTests.cs`: adds fixed-value coverage across releases 2–4, all reserved-identity sets, 1/2/3/4/6 equipped pieces, widely different item strengths, mixed gear and duplicate references to a two-handed Phoenix weapon.
- `docs/analysis/equipment-rebalancing.md`: records the revised set rule and player-facing terminology.

Internal item allocation and specialization-choice functionality remain available to the systems that use them. This change does not reprice or regenerate existing equipment. Set amounts stay data-driven in their catalogs rather than being hardcoded by set name.

## Verification

The backend wrapper passed **90 tests**, including set resolution, blueprint conversion, comparisons, attribute rules, healing and migration behavior:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~EquipmentSetBonusResolverTests|FullyQualifiedName~EquipmentBlueprintTests|FullyQualifiedName~EquipmentComparisonProjectorTests|FullyQualifiedName~AttributeRedesignTests|FullyQualifiedName~HealingBalanceCandidateTests|FullyQualifiedName~EquipmentMigrationTests'
```

The focused frontend suite passed **15 tests**:

```powershell
# From LL/src/Presentation/ll; npm cache under %TEMP%.
npm.cmd run test:ci -- --include="**/equipment-display.spec.ts" --include="**/equipment-set-progress.component.spec.ts" --include="**/inventory-equipment-modal.component.spec.ts" --progress=false
```

Logs are retained in `TestResults/fixed-set-bonuses-backend-tests.log` and `TestResults/fixed-set-bonuses-ui-tests.log`. An initial test-build failure due to a missing namespace import was fixed before the successful run. The backend build reports existing nullable/analyzer warnings.

`npm.cmd exec -- ng build --configuration production --progress=false` passed. It reported the existing warning thresholds for the initial bundle (976.88 kB versus 500 kB) and dungeon stylesheet (22.07 kB versus 20 kB), both below their error limits. The log is `TestResults/fixed-set-bonuses-ui-build.log`. `git diff --check` passed, and static inspection confirmed the removed profile block and all four Gear Power labels. No required verification command remained blocked.

## Local and manual deployment

No database migration, item conversion, configuration selector change or external deployment is needed for this correction. Rebuild/restart the backend hosts and update the frontend together so the displayed fixed amounts match combat. The local Debug API was running under Visual Studio's debugger and was not terminated; restart that debug session to load the updated backend. No database records or historical snapshots were edited.

This is a change to the shared set-resolution rule, so archived balance-study outputs remain evidence for their original captured executable/content. No broad rebalance simulation was repeated or claimed for the fixed amounts.
