# Blood Grove regional offense experiment — 8 September 2026

Historical protocol: this experiment used the initial 65–75% band. The later [50–90% policy revision](Blood-Grove-Band-Review.md) re-evaluates its saved candidate as Raven-pair Inconclusive and mixed-pair Pass. Its original results and decision below are retained as history. The subsequent [Blood Grove-only implementation and fresh validation](Blood-Grove-Local-Validation.md) record the current candidate and next steps.

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
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-pressure --content-root TestResults/balance/blood-grove-local-reference/original-content --output TestResults/balance/blood-grove-pressure-001
```

Current version-12 production content has a Blood Grove area override and is rejected by this historical protocol. Supply the retained original-content control from the local validation, as above; another checkout must capture that content first. A newer engine run produces new evidence and does not recreate the original executable identity.

Build Release first and use a new output directory. `--samples 1` checks the complete workflow with 182 battles; confirmation uses ten times the supplied discovery/control count. Small workflow checks use separate confirmation master seed **318092**, preserving the full run's reserved set. Sample counts below 100 are workflow checks, not target certification.

Retain the plan, captured fixtures and source content, candidate content copies, discovery bundles, frozen selection, confirmation bundles, both control comparisons, goal evaluations and detailed replays together. Source recipes and the reviewed goal policy are unchanged, so ordinary content comparisons remain compatible. The full experiment is a local investigation, not an added CI matrix.

## Measured result

The full experiment completed **18,200 valid battles**, with no invalid, cancelled or missing battles. Accounting totals are 8,219 victories, 9,970 defeats and 11 draws; these include paired controls and must not be pooled into a population clear rate. One draw occurred in the selected candidate's mixed-pair confirmation; the other ten are unchanged opening controls counted on both sides of their comparisons. None was a tick-limit draw.

No discovery candidate landed inside the 65–75% band for both encounters. The response became steep between bonuses 0.3 and 0.2:

| Offense bonus | Discovery Raven-pair wins / 100 | Discovery mixed-pair wins / 100 |
| --- | ---: | ---: |
| 2.3 through 0.8, each value | 0 | 0 |
| 0.7 | 8 | 7 |
| 0.6 | 9 | 8 |
| 0.5 | 26 | 19 |
| 0.4 | 38 | 50 |
| 0.3 | 47 | 62 |
| **0.2, selected** | **91** | **80** |
| 0.1 | 92 | 83 |

The declared selection rule chose **0.2**, whose worst distance from 70% was 21 percentage points, compared with 23 points at 0.3. It was the closest grid candidate, not a target-passing discovery result. Selection remained frozen for confirmation.

| Reserved confirmation | Original wins / 1,000 | Candidate wins / 1,000 | Candidate clear rate, 95% Wilson interval | Policy result | Mean winning time |
| --- | ---: | ---: | --- | --- | ---: |
| Raven + Raven | 0 | 884 | **88.4% [86.27%, 90.24%]** | Fail: above 75% | 57.28 s |
| Raven + Blood Zombie | 0 | 786 | **78.6% [75.95%, 81.03%]** | Fail: above 75% | 82.86 s |

Paired clear-rate changes from the original content are +88.4 points [85.44, 90.48] and +78.6 points [75.05, 81.36], respectively, using the existing approximate 95% paired interval. The working band is **not confirmed**. This is a valid completed investigation with two failed gameplay checks, not an execution failure. Winning pace remains diagnostic because the user approved only a clear-rate band.

## Control and replay findings

All **16 opening-area control cells** preserve gameplay exactly across 1,600 paired battles. All 32 later-area control cells change, as expected from the regional parameter: eight in the original suite and 24 in First Hunt. No control pair loses a previously observed victory, but higher clear rates and altered pace/health are still balance changes rather than automatic improvements. In the First Hunt level-10 control cases, every candidate cell reaches 100/100; the Hollow Stag and Skeleton slime-pair controls rise from 14/100 to 100/100 on these seeds. Those builds are diagnostics, not newly required targets.

The scaling audit identifies six affected areas: Blood Grove, Crystal Creek, Moonlit Graves, Twilight Clearing, Old Forest and Thornroot Hollow. Lumo, the last three Shenic placements and all four authored Meran placements retain their scaling. For Blood Grove and Crystal Creek, offense scaling falls from **4.511 to 2.411**, and the regeneration scaling factor becomes approximately **0.731** of the original. Other scaling fields remain identical. These are scaling-factor changes, not a claim that final rounded damage changes by the same percentage.

All four preselected detailed replays matched. For Raven-pair seed 998133253, the original dies at 30 seconds after two Raging Cleaves; the candidate wins at 57 seconds after four, with 21/436 health. For mixed-pair seed −340578642, the original dies at 39 seconds after two cleaves; the candidate wins at 84 seconds after six, with 31/436 health. These paired examples show the build surviving long enough to complete additional attacks and remove both enemies; they do not isolate offense from its coupled regeneration effect or explain every battle.

## Verification and decision

- Release tool/test build passed with 26 warnings and no errors. Sandboxed attempts could not write generated MSBuild/assembly files; the build succeeded with expanded permissions.
- **83 harness tests passed** through `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`. Four new tests cover grid legality and selection ties, exact overlay scope/nonsecret settings, full workflow evidence and controls, output preservation and cancellation. The full-workflow test runs 182 battles on the separate smoke confirmation set.
- The complete 18,200-battle command, four replays, two control-suite comparisons, goal evaluations and source/fixture/execution checks completed. Existing suites and reviewed goals are unchanged; no baseline was promoted as viable gameplay evidence.
- CLI help includes `investigate-pressure`. Full backend tests, standalone smoke scripts and hosted CI were not run for this offline-tool increment.
- Scoped `git diff --check` passed, and all 62 local links in the five updated Markdown files resolved.

Evidence is retained locally under ignored `TestResults/balance/blood-grove-pressure-reference`; its `summary.md` and `results.json` record the findings alongside the fixed plan, frozen selection, full runs, comparisons and evaluations. The production regional balance file still matches the captured original SHA-256 `50418780d995e2b04a878cfbc878cbc113bb5c9779b06ca3596e66f6d3126c8d`. This increment makes no production balance, reward, configuration, migration or deployment change.

**Do not apply bonus 0.2.** It exceeds the selected clear-rate band and changes six areas. The next investigation should be a separately predeclared finer sweep between **0.2 and 0.3**, with fresh confirmation seeds and the same control review. This completed run's grid, sample budget and selection stay unchanged. A shared regional value should only be proposed for production if the selected target and its wider effects are acceptable; otherwise investigate a more local adjustment. Keep the selected build and 65–75% target fixed.

Follow-up completed: the [fine pressure review](Blood-Grove-Fine-Pressure-Review.md) records a separate 24,600-battle experiment. Bonus 0.21 confirmed at 88.5% Raven-pair clears (Fail) and 74.1% mixed-pair clears (Inconclusive). Discovery shows a sharp step aligned with Raven damage rounding thresholds. No value was promoted; the next proposed approach is a Blood Grove-specific adjustment with a second parameter. This coarse experiment and its retained artifacts remain unchanged.
