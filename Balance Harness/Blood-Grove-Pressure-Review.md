# Blood Grove regional offense experiment — 8 September 2026

This experiment tests one possible cause of the selected starter's Blood Grove losses: the regional offense bonus applied after Lumo. The [starter recipe and reviewed target](Blood-Grove-Starter-Reference.md) remain fixed: level-5 Goblin Warrior, one-handed Shortsword and Heavy Breastplate, aiming near 70% clears with a 65–75% working band for each selected pairing. The initial recipe lost all 400 measured battles.

## Declared adjustment and limits

The only authored value varied is `gated-region-one-v1.offenseCurve.postTutorialBonus` in [region-combat-balance.json](../LL/src/API/API.LL/Data/progression/region-combat-balance.json). Its original value is 2.3. The declared grid runs from 2.3 through 0 in steps of 0.1. Production validation accepts **23 values, 2.3 through 0.1**; zero is rejected because the active post-tutorial floor requires a positive bonus. That rejection is recorded before combat, and the validator/floor rules are preserved.

The [scaling provider](../LL/src/Infrastructure/Service/Services.LL/Regions/RegionCreatureScalingProvider.cs) applies this bonus to the post-tutorial offense floor. It does not isolate Blood Grove: other early Shenic areas can change. The [creature scaler](../LL/src/Infrastructure/Service/Services.LL/Entities/Creatures/CreatureScaler.cs) also derives health regeneration from the square root of health × offense scaling. Reducing offense therefore changes regeneration as an existing coupled effect. Enemy maximum health, defenses, attack-speed growth, abilities, spawn recipes and the player's loadout remain fixed. The report audits all 14 authored region placements and records the offense and regeneration changes.

The command creates local copies of the 14 allowlisted combat data files and writes only the selected nonsecret combat settings needed to read them. It never copies the API's complete `appsettings.json`. Each candidate changes only the declared JSON value; the real content file is untouched. Source hashes, fixture/policy hashes and execution identity are checked throughout the run.

## Fixed budget, selection and stopping rule

| Stage | Budget | Purpose |
| --- | --- | --- |
| Discovery, master seed 1337 | 23 candidates × 2 encounters × 100 trials = 4,600 battles | Measure every legal value on paired encounter seeds |
| Confirmation, reserved master seed 318091 | Original and selected candidate × 2 encounters × 1,000 trials = 4,000 battles | Evaluate the frozen choice against the working band and report paired movement |
| Existing controls, master seed 318091 | Original and selected candidate × 48 cells × 100 trials = 9,600 battles | Check both complete original/First Hunt suites, including later-area effects |
| Total | **18,200 battles** | Fixed complete experiment |

After all discovery runs finish, selection minimizes the largest absolute per-encounter distance from 70%. Ties use the mean distance, then the largest bonus (the smallest change). The command writes `selection.json` before running confirmation. It confirms that choice even if no candidate is close to both targets. Discovery and confirmation seeds must be disjoint within an encounter. There is no adaptive grid refinement, candidate reselection or sample extension after seeing results.

The original and selected candidate each retain confirmation trial index 0 for both encounters: **four detailed replays**, chosen without reference to outcomes. All 16 Lumo opening-area control cells must preserve gameplay exactly. The remaining 32 control cells measure possible collateral effects; their existing draft targets do not become newly approved gates. Whole-area difficulty and other builds remain outside the selected starter target.

Policy failure is a valid experiment finding. The investigation command returns success when the declared experiment completes with valid evidence, while its nested evaluation reports retain the selected candidate's actual enforced pass/fail/inconclusive status. Invalid input, incomplete runs, source changes, control-integrity failures and cancellation cannot produce a completed experiment report. Local original-content baseline manifests are explicitly labeled experimental controls, with no approval of a viable gameplay baseline. No production change or baseline promotion occurs automatically.

## Reproduce

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-pressure --output TestResults/balance/blood-grove-pressure-001
```

Build Release first and use a new output directory. `--samples 1` checks the complete workflow with 182 battles; confirmation uses ten times the supplied discovery/control count. Small workflow checks use separate confirmation master seed **318092**, preserving the full run's reserved set. Sample counts below 100 are workflow checks, not target certification.

Retain the plan, captured fixtures and source content, candidate content copies, discovery bundles, frozen selection, confirmation bundles, both control comparisons, goal evaluations and detailed replays together. Source recipes and the reviewed goal policy are unchanged, so ordinary content comparisons remain compatible. The full experiment is a local investigation, not an added CI matrix.
