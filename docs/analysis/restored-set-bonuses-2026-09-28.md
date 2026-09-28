# Original set bonuses restored

The user requested the original set bonuses and their clearer descriptions, then explicitly selected the current attribute names: Restoration, equivalent Ability Haste and Tenacity. The retained legacy catalog was checked against the original Git version and matched exactly. It supplies the reference amounts, modifier types, piece thresholds, descriptions and granted abilities.

The restored stat bonuses in equipment releases 2, 3 and 4 are:

| Set | 2 pieces | 4 pieces | 6 pieces |
|---|---|---|---|
| Fury | +5% Crit Chance | Original Fury trigger | — |
| Arcane | +5% Magic Penetration; +3.09% Ability Haste | Original active-ability trigger | — |
| Execution | +6% Armor Penetration; +8% Crit Damage | Original low-health damage bonus | — |
| Aegis | +10% total Armor and Resistance rating | Original starting barrier | Original barrier damage reduction |
| Warden | +10% total Max Health | +25% total Health Regen; +5% Tenacity | Original Renewal trigger |
| Endurance | +25% total Health Regen | +8% total Max Health; +5% Tenacity | +10% Tenacity |
| Phoenix | +8% Restoration | Original healing-received bonus | Original rebirth |
| Spirit | +25% Restoration | +5% Ability Haste; +10% total Resistance rating | Original ally barrier trigger |
| Primal | +6% total Power and Max Health | Original summon Power bonus | — |
| Venom | Original Poison on Basic Attack | — | — |
| Hive | Original Attack Speed trigger | — | — |

Spirit's two-piece bonus was subsequently increased to a fixed +25% Restoration at the user's request. Its four-piece bonus was then set to exactly +5 Ability Haste, including the actual modifier and tooltip. Phoenix remains at +8% Restoration.

All triggered-ability descriptions, IDs and thresholds retain their original values. The original restoration converted 3% and 5% cooldown reduction using `100 × reduction / (100 − reduction)`. Arcane retains 3.09278351 Ability Haste (displayed as 3.09%); Spirit now uses the explicitly requested 5 instead of the equivalent 5.26315789. Both stack under the current additive haste rules. The old +10% Status Resistance and +10% Crowd Control Resistance within Endurance's six-piece bonus become one +10% Tenacity modifier, following the existing compatibility conversion rather than doubling it to 20%.

Fixed means the authored modifier no longer depends on item allocation. An original modifier such as +10% total Max Health remains a percentage of the character's health; it is not replaced with a flat amount. Penetration and other attributes continue using the approved current combat formulas.

## Changes and verification

Updated `equipment-sets.v1.json`, `equipment-sets.v3.json` and `equipment-sets.v4.json` under `LL/src/API/API.LL/Data/equipment`. Existing `.v2` set IDs are retained so equipped items continue referencing the same sets. The historical `equipment-sets.legacy-v1.json` is unchanged. `EquipmentSetBonusResolverTests.cs` now checks all restored sets against the original catalog through the current attribute projection, along with the earlier fixed-value, item-strength and two-handed tests.

**107 backend tests passed**, zero failures/skips, through:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~EquipmentSetBonusResolverTests|FullyQualifiedName~EquipmentBlueprintTests|FullyQualifiedName~EquipmentComparisonProjectorTests|FullyQualifiedName~AttributeRedesignTests|FullyQualifiedName~HealingBalanceCandidateTests|FullyQualifiedName~AttributeRolloutRegistrationTests|FullyQualifiedName~EquipmentMigrationTests'
```

Log: `TestResults/restored-set-bonuses-backend-tests.log`. The build had existing nullable/analyzer warnings and zero errors. `git diff --check` passed. All three restored catalogs match, and none uses the retired attribute identifiers or allocation-scaling descriptions. Frontend tests/build were not repeated because this follow-up only changes server content, backend tests and documentation; the prior Gear Power/profile changes remain intact. No required command was blocked.

No database migration, item conversion or configuration change is required. Rebuild/restart backend hosts to load the restored catalogs, then refresh the client for updated descriptions. The existing Visual Studio debug session was not stopped. No external environment was deployed or database records changed.

For the subsequent Spirit adjustment, all three current catalogs and the parity expectations now use 25 Restoration. The mixed-strength and two-handed regression test explicitly covers both Spirit at 25 and Phoenix at 8. **23 backend tests passed**, zero failures/skips, using `./build/run-tests.ps1 -Filter 'FullyQualifiedName~EquipmentSetBonusResolverTests|FullyQualifiedName~HealingBalanceCandidateTests'`. Log: `TestResults/spirit-restoration-tests.log`. The build reported 16 existing warnings and zero errors; `git diff --check` passed. No required verification was blocked, and the restart/migration implications above still apply.

For the exact +5 Ability Haste follow-up, the same three catalogs and the parity expectations were updated. The shared `EssenceDescriptionFormatter` protects the complete Ability Haste attribute name from the separate Haste condition's keyword tooltip, including case and whitespace variants, while preserving genuine Haste and Renewal tooltips. **27 backend checks passed** through `build/run-tests.ps1` (set resolver, blueprints and comparison projector), and **24 frontend checks passed** through `npm.cmd run test:ci` (description formatter, description component and set progress). Logs: `TestResults/spirit-haste-backend-tests.log` and `TestResults/spirit-haste-ui-focused-tests.log`. Initial frontend attempts failed to launch Chrome or lost arguments through PowerShell's npm wrapper; the final run used npm.cmd and the configured headless CI runner outside the sandbox. No required check remains blocked. No production build was repeated for this follow-up. Rebuild/restart the backend and serve the updated frontend; no new migration or configuration change is required.
