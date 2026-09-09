# Blood Grove fine regional offense experiment — 8 September 2026

Historical protocol: this experiment used the initial 65–75% band. The later [50–90% policy revision](Blood-Grove-Band-Review.md) puts both observed candidate rates inside the accepted range; the mixed pair passes and the Raven pair remains statistically inconclusive. The original findings and recommendation below remain historical. The subsequent [Blood Grove-only implementation and fresh validation](Blood-Grove-Local-Validation.md) record the current candidate and next steps.

This is a separate follow-up to the completed [coarse pressure experiment](Blood-Grove-Pressure-Review.md). Its selected bonus 0.2 overshot the approved 65–75% clear-rate band. The selected level-5 Goblin Warrior, one-handed Shortsword and Heavy Breastplate [recipe and policy](Blood-Grove-Starter-Reference.md) remain fixed. This experiment changes only `gated-region-one-v1.offenseCurve.postTutorialBonus` in local content copies, using the same production validator and existing coupled regeneration scaling. Production's value remains 2.3.

## Plan declared before combat

Experiment ID: `blood-grove-pressure-fine-v1`. Test **0.30, 0.29, 0.28, 0.27, 0.26, 0.25, 0.24, 0.23, 0.22, 0.21 and 0.20**, in that order. All must pass production validation before execution. The original value 2.3 is retained as the confirmation/control reference rather than a selection candidate on this finer grid.

| Stage | Fixed budget | Master seed |
| --- | --- | --- |
| Discovery | 11 candidates × 2 encounters × 500 trials = 11,000 battles | 418091 |
| Reserved confirmation | Original and selected candidate × 2 encounters × 1,000 trials = 4,000 battles | 418092 |
| Original and First Hunt controls | Original and selected candidate × 48 cells × 100 trials = 9,600 battles | 418092 |
| Total | **24,600 battles** | |

Discovery uses more trials than the coarse sweep to reduce selection noise around the steep response. Selection still minimizes the worst absolute per-encounter distance from 70%, then the mean distance, then the size of the change from production. Write the selection before any confirmation outcomes. Confirm that one choice even if discovery has no candidate inside both bands. Complete the declared grid and budget once; do not reselect or extend samples after outcomes. Discovery is exploratory; the existing policy evaluates pointwise 95% Wilson intervals on reserved confirmation, without pooling either earlier experiments or discovery into those estimates.

The preflight checks actual derived starter battle seeds for disjoint discovery/confirmation schedules and excludes the first 1,000 trials per encounter for earlier master seeds 1337, 7331, 940031, 620903, 318091 and 318092. Full fine runs also exclude workflow-check seeds 418093/418094; workflow checks exclude the full run's seeds. Resolving those schedules executes no combat. This preserves the new confirmation set from both prior investigations and development checks.

Retain four detailed replays: confirmation trial index 0 for each encounter under original/candidate content, regardless of outcome. All 16 Lumo control cells must preserve gameplay exactly. Review the 32 later-area control cells and all 14 authored area placements for wider effects. Other builds remain diagnostic; universal Essence viability is not a requirement. Offense and regeneration remain coupled, so this cannot attribute their separate contributions.

The existing content/fixture/settings/execution checks and experimental control manifests apply. A completed investigation may fail the gameplay target; command success is distinct from its nested goal status. No automatic production change or viable-baseline promotion follows a result. The coarse run's artifacts and stopping rule remain unchanged.

## Reproduce

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- investigate-pressure-fine --content-root TestResults/balance/blood-grove-local-reference/original-content --output TestResults/balance/blood-grove-pressure-fine-001
```

Current version-12 production content has a Blood Grove area override and is rejected by this historical protocol. Supply the retained original-content control from the local validation, as above; another checkout must capture that content first. A newer engine run produces new evidence and does not recreate the original executable identity.

Build Release first and use a new output directory. `--samples` is the base/control count from 1 through 100: fine discovery uses five times that count and confirmation ten times. The default 100 executes the full declared budget. `--samples 1` executes a 246-battle workflow check using separate discovery/confirmation seeds 418093/418094; it is not target certification. Schema-2 plans record the experiment ID, discovery count and excluded master seeds explicitly. The coarse command retains its grid, seeds and battle budget.

## Measured result

All **24,600 battles completed validly**: 13,947 victories, 10,629 defeats and 24 draws, with no tick-limit draws, invalid, cancelled or missing battles. These are accounting totals across paired candidates and controls, not a pooled population clear rate. The original reference still lost all 2,000 confirmation battles.

| Bonus | Raven-pair discovery wins / 500 | Mixed-pair discovery wins / 500 |
| --- | ---: | ---: |
| 0.30 | 240 | 281 |
| 0.29 | 240 | 281 |
| 0.28 through 0.22, each value | 240 | 282 |
| **0.21, selected** | **433** | **380** |
| 0.20 | 433 | 380 |

No grid candidate fell inside both 65–75% bands. The selection rule chose **0.21**, tied with 0.20 at a worst distance of 16.6 percentage points and a mean distance of 11.3 points, then preferred the smaller change from production. The other values had a worst distance of 22 points. The choice stayed frozen for confirmation.

| Reserved confirmation | Candidate wins / 1,000 | Clear rate and 95% Wilson interval | Policy result | Mean winning time |
| --- | ---: | --- | --- | ---: |
| Raven + Raven | 885 | **88.5% [86.37%, 90.33%]** | Fail: above 75% | 57.56 s |
| Raven + Blood Zombie | 741 | **74.1% [71.30%, 76.72%]** | Inconclusive: overlaps 75% | 82.71 s |

The mixed pair has 258 defeats and one draw; the Raven pair has 115 defeats. The mixed point estimate is inside the band, but its interval is not wholly inside it. Overall evaluation is **Fail: zero pass, one fail, one inconclusive, zero invalid**, with enforced-evaluation exit code 1. The investigation command itself completed with exit code 0. Paired changes from original content are +88.5 points [85.54, 90.57] and +74.1 points [70.38, 77.08], using the existing approximate 95% paired intervals. No extra samples were added to seek a passing result.

## Why further tiny offense steps are not the next recommendation

Discovery changes abruptly between 0.22 and 0.21: Raven-pair clears go from 48.0% to 86.6%, and mixed-pair clears from 56.4% to 76.0%. The 500 paired trials gain 193 Raven-pair victories and 98 mixed-pair victories, with no lost victories in either comparison. These are exploratory observations from the existing discovery records.

Post-hoc inspection of saved discovery trial index 0, seed 1043018584, finds Raven Power of **17.017** at 0.22 and **16.947** at 0.21. The [engine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) rounds basic attack magnitude and scaled ability magnitude to integers. With the [basic attack coefficient](../LL/src/Core/Domain/Models/Attributes/AttributeCombatRules.cs) of 0.5 and [Piercing Peck's coefficient](../LL/src/API/API.LL/Data/combat/abilities.json) of 1.5, those starting values cross two thresholds together:

| Starting Raven magnitude | Bonus 0.22 | Bonus 0.21 |
| --- | ---: | ---: |
| Basic attack: round(1 + Power × 0.5) | 10 | 9 |
| Piercing Peck: round(Power × 1.5) | 26 | 25 |

These magnitudes precede variance, critical hits, defenses and later modifiers. This alignment supports a rounding-threshold explanation for the sharp change; it does not isolate every cause or prove no value on a denser grid could work. Regeneration also changes slightly. That saved paired example loses at 54.1 seconds under 0.22 and wins at 57 seconds under 0.21. `threshold-review.json` records the inspected files and their hashes as post-hoc analysis; it adds no battles and does not alter selection or confirmation.

## Wider effects and replays

All **16 Lumo control cells** remain identical across 1,600 paired battles. All 32 later-area cells change: eight in the original suite and 24 in First Hunt. No control comparison loses an observed victory. The original suite gains none; First Hunt gains 387 across 3,600 pairs. All First Hunt level-5 cells still have zero wins on these seeds, while all level-10 candidate cells reach 100/100. In particular, level-10 Hollow Stag slime-pair controls rise from 17/100 to 100/100, and Skeleton slime-pair controls rise from 23/100 and 18/100 to 100/100. These wider changes need balance review; they are not automatic improvements or new requirements for other Essences.

The same six early Shenic areas are affected: Blood Grove, Crystal Creek, Moonlit Graves, Twilight Clearing, Old Forest and Thornroot Hollow. Blood Grove and Crystal Creek offense scaling falls from **4.511 to 2.421**, with regeneration scaling approximately **0.73259** of original. Lumo, the last three Shenic placements and all four Meran placements retain their scaling. Other scaling fields stay identical.

All four preselected detailed replays matched. On Raven-pair confirmation seed 1528986190, original content loses at 30 seconds after two Raging Cleaves, while the candidate wins at 54 seconds after four with 58/436 health. On mixed-pair seed −1711973927, original content loses at 39 seconds after two cleaves, while the candidate wins at 78.1 seconds after six with 92/436 health. These outcome-independent examples illustrate longer survival; they do not certify the target.

## Verification and next decision

- Release tool/test build passed with 32 warnings and no errors using `dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore`. The initial repository-runner build could not read the sandbox-restricted user NuGet configuration; using the already restored dependencies resolved that limitation.
- **85 harness tests passed** through `./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarness'`. Coverage includes both complete coarse/fine workflows, the frozen selection before confirmation, distinct two-decimal bonus labels, the fine grid/budget/seeds and unchanged recipe/policy/content. Full backend tests, standalone smoke scripts and hosted CI were not run for this offline-tool increment.
- The full 24,600-battle experiment, four detailed replays, two complete control comparisons and source/fixture/settings/execution checks completed. CLI help includes `investigate-pressure-fine`.
- All 19,123 files in the earlier coarse artifact directory retained the same aggregate path/content digest before and after this run. Production regional content still matches SHA-256 `50418780d995e2b04a878cfbc878cbc113bb5c9779b06ca3596e66f6d3126c8d`.
- Scoped whitespace checks passed and all 72 local links in the six updated Markdown files resolved. Combat evidence belongs to its recorded executable identity; later engine changes require fresh validation.

Evidence is retained under ignored `TestResults/balance/blood-grove-pressure-fine-reference`, including the plan, source/fixture snapshots, candidate content, discovery, frozen selection, confirmation, evaluations, comparisons, detailed replays, summary and threshold inspection. This increment changes offline tooling, tests and documentation; it makes no production balance, reward, configuration, migration or deployment change.

**Do not apply bonus 0.21 or promote it as a viable baseline.** The selected target remains unmet. The next investigation should use a Blood Grove-specific adjustment and a second parameter, such as enemy health alongside offense, with fixed bounds, budget and fresh confirmation seeds. First establish the local content scope so other areas retain their original scaling, then review both encounter rates and pace. The observed thresholds justify changing the experimental approach rather than automatically narrowing this same grid again. Keep the starter recipe and approved 65–75% band unchanged, and preserve both completed experiments.
