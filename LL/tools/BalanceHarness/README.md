# Balance Harness: idle balance workflow

An offline .NET console tool for measuring progression difficulty and explaining the effect of combat code or content changes. It runs real idle combat through production preparation and execution. The original control suite has 12 cells (1,200 battles); the separate First Hunt cohort has 36 cells (3,600 battles), including two-enemy encounters. It saves replayable battles, produces Markdown/JSON scorecards, compares accepted references and evaluates versioned goals.

The current workflow is:

1. Run the reference suite and review its assumptions, completeness and scorecard.
2. Accept a complete run as a comparison baseline with a written reason.
3. Run a candidate with matching fixtures, sample count and seeds after a code/content change.
4. Compare the runs and evaluate the candidate against the draft goals.
5. Inspect failed or inconclusive checks and replay selected battles before deciding on a gameplay or policy change.

All shipped goals remain draft. An accepted baseline records a reference state; approving the desired player experience is a separate design decision. See the [development plan](../../../Balance%20Harness/Balance-Harness-Plan-With-Benchmarking.md) for milestones and remaining work.

## Run the suite

From the repository root:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --output TestResults/balance/idle-reference-001 --seed 1337
```

For a quick 36-battle smoke run, use a new output directory and add `--samples 3`. `--samples` overrides the per-cell sample count, not the total. `suite` also accepts `--suite <json>` and `--content-root <API.LL-directory>`. Its default fixture is [idle-reference.json](Fixtures/idle-reference.json). Preflight validates every cell before executing; local limits are 10,000 samples per cell, 1,000 cells, and 100,000 total battles.

| Checkpoint | Builds | Fixed encounters | Acquisition assumptions |
| --- | --- | --- | --- |
| Level 1, Lumo Ruins | Mace / shortsword | Goblin / Goblin Warrior | First Weapon complete; one starter weapon, Goblin essence |
| Level 5, Blood Grove entry | Mace + medium chest / dagger + light chest | Raven / Blood Zombie | Trial of Lumo complete; one acquired chest, Goblin essence |
| Level 10, Crystal Creek entry | Greatsword + medium armor / staff + light armor | Blue Slime / Frost Imp | Blood in the Grove complete; four equipment items, two unlocked essence slots; Goblin + Goblin Warrior / Goblin + Vampire Bat |

All gear is tier 1, rank 0, standard quality, with baseline attribute rolls and no active styles. All selected essences are level 1, unascended and unevolved. Two-handed weapons occupy both hands. These are explicit ownership hypotheses; drop rates, acquisition time, quest completion, and essence training are not simulated. Builds within a stage use equal equipment counts, not a claimed equal power budget. The fixture retains the full assumptions in each run. “Challenge” is an encounter-selection hypothesis, not a guaranteed difficulty ordering.

The [8 September policy review](../../../Balance%20Harness/Idle-Policy-Review.md) found that Goblin is not one of the current First Hunt choices, later equipment ownership is unverified, and Blood Grove/Crystal Creek normally spawn two enemies. Keep this fixture as a fixed control; it does not certify an immediate post-tutorial build or ordinary area difficulty. The separate First Hunt cohort below addresses those selections while leaving training and exact reward outcomes as explicit assumptions. All numerical goals remain draft.

Seeds are derived by `StableRandom.Seed` from `idle-suite-seeds-v1`, the master seed, stage ID, encounter ID and zero-based trial index. Alternative builds share the same encounter seeds. Reordering cells or changing combat coefficients preserves the schedule. Changing stable stage/encounter IDs changes it. Results across builds are paired observations and must not be pooled as independent samples.

The scorecard reports wins/losses/draws, valid sample sizes, a 95% Wilson clear-rate interval, separate win/non-win durations, final player health and tick-limit draws. JSON includes mean, median and interpolated p90 distributions; p90 is unavailable below ten observations. Missing samples remain unavailable rather than becoming zero. Invalid, cancelled and unexecuted battles are counted separately and cannot produce a complete suite. No balance pass/fail policy is applied.

Select a battle ID from `scorecard.md` or `battles.jsonl` to reproduce it with an event log:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/idle-reference-001 --battle starter.mace.ordinary.0001 --detailed > TestResults/balance/starter-replay.json
```

## First Hunt cohort and enemy groups

[idle-first-hunt.json](Fixtures/idle-first-hunt.json) crosses Goblin Warrior, Hollow Stag and Skeleton with mace/wand choices at levels 1, 5 and 10. It uses one enemy at level 1 and fixed duplicate/mixed pairs at levels 5 and 10. Equipment follows quest-reward counts: one weapon, then one Armor Chest item, then one Jewelry Chest item. Medium Mail and Amulet are fixed possible box outcomes, not guaranteed selections. All Essences remain untrained at level 1; level 10 adds a Goblin selected through the earlier Lumo Token. See the [cohort review and measured results](../../../Balance%20Harness/First-Hunt-Cohort.md).

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/idle-first-hunt.json --output TestResults/balance/first-hunt-001 --seed 1337
```

The suite has 36 cells, 3,600 battles by default, or 108 with `--samples 3`. Use [idle-first-hunt-goals.json](Fixtures/idle-first-hunt-goals.json) with `evaluate --goals`; it expands six draft goals into 180 checks. These entry goals have no upper clear-rate ceiling. A baseline must come from this same cohort and use matching sample counts/seeds. Neither its targets nor baselines are interchangeable with the original controls.

Schema 2 scenarios and encounter definitions retain `creatureId` for the first enemy and may append one or two `additionalCreatureIds`, in order. The input archives matching `additionalCreatures` snapshots. Every occurrence receives an independent combat slot/state, including duplicate species. All members must belong to the selected area and the count must be possible there. Changes to count, order or IDs make a cell non-comparable; changed frozen companion coefficients are reported as resolved-input changes. Schema 1 remains single-enemy and its hashes stay unchanged because unused extension fields are omitted. Replay retains its original binary/runtime checks.

## Investigate training and reinforcement

The [Blood Grove review](../../../Balance%20Harness/Blood-Grove-Progression-Review.md) checks actual progression costs and reports a fixed 14,400-battle experiment. All six starter builds still lost both pairings at ranks 0, 1 and 5 on both seed sets. Unascended Essence training from level 1 to 10 changed no combat summaries; ability growth occurs at Ascension. These are conditional progression probes, not approved player budgets or balance targets.

```powershell
./build/investigate-blood-grove.ps1 -OutputDirectory TestResults/balance/blood-grove-001
```

The script writes its fixed plan before execution, generates six recipes from the Blood Grove stage, runs 100 samples per cell on seeds 1337 and 7331, and retains per-cell evidence plus detailed replays. Add `-NoBuild` after building or `-SamplesPerCell 2` for a small workflow smoke. It requires a new output directory and does not run goal evaluation or promote a gameplay baseline.

Schema-2 stages and single scenarios optionally accept `"essenceLevels": { "essence.goblin_warrior": 10 }`. Keys must name selected Essences (across the stage's builds for a stage map); omitted selections stay level 1. Values must be unascended levels 1–10. Ascension/evolution recipes are outside this extension. Training maps participate in fixture/scenario identity and are checked against frozen snapshots. Absent maps are omitted from JSON, preserving both existing cohorts' hashes. Different progression recipes remain incompatible with ordinary regression comparison, even when their combat outcomes match.

## Selected Blood Grove starter reference

[idle-blood-grove-starter.json](Fixtures/idle-blood-grove-starter.json) pins the user-selected Goblin Warrior + Sword + Heavy Chest build for Blood Grove. Sword maps to the one-handed Shortsword and Heavy Chest to Heavy Breastplate. Character level is 5; the one Essence stays level 1, unascended and unevolved. Both items are common, Standard, tier 1, rank 0 with baseline rolls and no Fury. The exact chest is a fixed possible reward outcome. This two-cell suite preserves the earlier cohorts and uses the existing command:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --suite LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter.json --seed 1337 --output TestResults/balance/blood-grove-starter-001
```

The [starter reference review](../../../Balance%20Harness/Blood-Grove-Starter-Reference.md) records the recipe, fixed 400-battle discovery/confirmation measurement and two selected replays. Both encounters produced 0/100 wins on both seed sets. The user subsequently approved a **70% aim with an initial 65–75% band for each encounter**. [idle-blood-grove-starter-goals.json](Fixtures/idle-blood-grove-starter-goals.json) carries this reviewed goal and pins the unchanged recipe. Its pre-policy fixture wording remains historical; the goals file records current approval. A viable gameplay baseline remains open.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- evaluate --goals LL/tools/BalanceHarness/Fixtures/idle-blood-grove-starter-goals.json --run TestResults/balance/blood-grove-starter-reference/confirmation --output TestResults/balance/blood-grove-starter-evaluation-001
```

This absolute-only policy needs no baseline argument. It enforces the working band when explicitly selected: current saved results return two failed checks and exit 1; overlapping confidence intervals return 3. At least 100 trials are required, but 70/100 remains inconclusive because its 95% Wilson interval crosses the band. A predeclared 1,000-trial confirmation budget per encounter can resolve a result near 70%; select fresh reserved seeds before tuning. CI continues to smoke-test the two existing draft cohorts, while harness tests cover this selected recipe and policy. Do not pass the enforced policy to the advisory smoke script.

## Investigate attainable Blood Grove equipment

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- investigate-entry --output TestResults/balance/blood-grove-entry-001
```

This fixed experiment crosses six First Hunt builds with all nine possible Armor Chest items and plain/Fury weapons: 216 cells, 100 samples each, on discovery seed 1337 and confirmation seed 940031 (43,200 battles). `--samples 1` runs the full matrix with a small sample budget. Each build retains one weapon and one armor item; Fury spends one guaranteed Blueprint and 100 of the 500 starting Cinders. The Lumo Token is retained.

The command derives the production chest candidate list, records the resource ledger and stopping rule before execution, verifies both archived suites, reports paired substitutions, and saves 24 predeclared detailed replays. It rejects reused output directories, incomplete runs, changed content/execution between seed sets and mismatched paired schedules. It does not change regression-comparison compatibility, promote a baseline or approve goals. No new fixture schema is needed; existing style/slot support supplies the builds.

The [entry review](../../../Balance%20Harness/Blood-Grove-Entry-Review.md) records zero wins in every tested cell on both seed sets, survival differences and selected replay diagnostics. Existing fixtures/goals remain unchanged. This is a local investigation; CI continues to run correctness tests and the two small cohort smoke workflows.

## Accept a baseline and compare changes

After reviewing a complete run, explicitly record it as a reference with a written reason:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- baseline accept --run TestResults/balance/idle-reference-001 --output TestResults/balance/baselines/idle-v1.json --reason 'Reviewed reference builds and encounter assumptions; advisory comparison reference.'
```

The manifest records the reason, acceptance time, metrics version, accepted scorecard, relative run location and a SHA-256 fingerprint of the evidence. It never overwrites an existing manifest. Acceptance requires a complete run: the reader verifies content/input hashes, recomputes the scorecard from individual observations, and checks each completed observation against its saved battle. The fingerprint pins the input, manifest, JSON/Markdown scorecards, battle index and all battle records; archived content is pinned through its verified manifest hashes. Checksums detect altered evidence but do not authenticate its author or prove gameplay parity.

Retain the original run directory with its baseline manifest. The small manifest can be kept in source control if desired; full bundles remain ignored artifacts. Moving the manifest and its referenced run together preserves the relative link. This command accepts a comparison reference; it does not approve difficulty targets or make an incomplete run acceptable.

Generate a candidate run with the same fixtures and seeds, then compare it:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- suite --output TestResults/balance/idle-candidate-001 --seed 1337
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- compare --baseline TestResults/balance/baselines/idle-v1.json --run TestResults/balance/idle-candidate-001 --output TestResults/balance/idle-comparison-001
```

Comparison reads saved evidence without executing combat or requiring the historical binaries. Replay continues to require the original binary/runtime/platform identity. A new executable or content hash is an expected comparison input and is listed prominently in the report.

The comparison writes `comparison.json` and `comparison.md` in a new output directory. It shows:

- Per-cell baseline/candidate clear rates, gained/lost wins on the same seeds, and clear-rate change in **percentage points**.
- Winning duration change for seeds won in **both** runs, alongside each run's winning median and eligible pair count. Newly won/lost fights affect clear rate rather than being mistaken for faster/slower wins.
- Mean remaining-health change over all paired attempts, including defeats.
- Outcome and gameplay-record change counts. Preparation, terminal state and statistics can change even when headline metrics do not.
- Changed content/assembly hashes and resolved inputs, plus up to three changed-battle examples per cell with saved-record links and replay instructions.

Compatibility is checked per stable cell ID. Changed scenario assumptions, loadout recipes/selections, essence progression, rules/cadence, tick units or trial schedules are non-comparable. Added/removed cells are listed. Reordering cells or trials is supported; partial seed overlap is not used. Derived character coefficients and encounter data can change under the same recipe and are reported explicitly. Runtime/platform differences exclude all matched cells so environmental changes are not mistaken for a controlled content experiment. Valid unchanged cells still compare when other cells are non-comparable or incomplete.

All changes are candidate minus baseline. Clear-rate uncertainty combines two 97.5% Wilson intervals for gained/lost win probabilities using a Bonferroni adjustment, giving a conservative approximate 95% interval for the paired difference. Zero changed wins does not imply zero sampling uncertainty. Duration/health mean intervals use an exploratory normal approximation with at least 30 eligible pairs and nonzero observed variance; otherwise the interval is unavailable with a reason. These intervals are not corrected across cells/metrics. No result is labeled balanced, improved or regressed automatically: easier content may be undesirable, and targets/practical thresholds are still design decisions.

Exit code **0** means a complete advisory comparison, regardless of the measured direction. **2** means an incomplete/non-comparable comparison or invalid evidence; well-formed partial runs retain a report, while corrupt/missing evidence writes `failure.json`. Cancellation returns **130**. There is no automatic promotion, default baseline replacement, resume or CI balance gate.

## Evaluate balance goals

The [default goals file](Fixtures/idle-goals.json) defines six **draft proposals**, expanded into 60 checks across all 12 cells. It proposes 90% minimum clears for ordinary enemies, a 60–90% challenge clear-rate band, a 60-second mean winning duration limit, and tolerances for baseline movement. These are proposed experience goals, not values established by the observed results. The default and First Hunt policies remain `Draft`; the separately selected Blood Grove starter policy has a reviewed 65–75% enforced band. No balance gate is enabled by default or added to hosted CI.

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- evaluate --run TestResults/balance/idle-candidate-001 --baseline TestResults/balance/baselines/idle-v1.json --output TestResults/balance/idle-evaluation-001
```

Use `--goals <json>` for another policy. `--baseline` is required whenever the selected goals include change metrics; it can be omitted for an absolute-only policy. The evaluator reads verified saved evidence, recomputes any requested baseline comparison, and writes a new directory containing `goals.json`, `evaluation.json`, `evaluation.md`, and the comparison JSON/Markdown when supplied. It preserves the input archives and baseline. The report pins the exact goals hash, evaluator/metrics versions, fixture contract and run fingerprints.

Supported metrics and required units:

| Metric | Unit | Eligible observations |
| --- | --- | --- |
| `ClearRate` | `percent` | Wins / all valid attempts; 95% Wilson interval |
| `WinDurationMean` | `seconds` | Duration of victories only |
| `RemainingHealthMean` | `percent` | Final health fraction × 100, including defeats |
| `ClearRateChange` | `percentage points` | Paired gained/lost wins against the baseline |
| `SharedWinDurationChange` | `seconds` | Duration difference only for seeds won in both runs |
| `RemainingHealthChange` | `percentage points` | Health difference over all paired attempts |

Each goal declares its exact cells, metric/unit, primary/guardrail/diagnostic role, draft/enforced status, minimum eligible sample count, rationale and at least one inclusive `minimum`/`maximum` bound. The file lists all `requiredCells`; each needs a primary goal. Unknown fields, invalid bounds/units, duplicate goals/cells or missing primary coverage are rejected. The declaration permits up to 1,000 goals and 100,000 expanded checks.

The `fixtureHash` pins normalized suite recipes, assumptions, encounter selections and starting conditions. Sample count and enumeration order are excluded so a smoke sample is still the same cohort. Code/content coefficient changes can be evaluated under the same recipes. Added/removed cells or a changed cohort are invalid until reviewed; the report provides the actual fixture hash to support an intentional policy update. Changing that hash is a review decision, not an automatic fix.

A three-sample smoke run checks execution, but does not meet the default goals' sample minimums. Its paired comparison also needs a baseline with the same three trials per cell: comparing it with a 100-sample reference is incompatible, even though both have the same fixture hash. Use smoke runs for workflow verification and a predeclared reference sample budget for policy review.

| Check result | Meaning |
| --- | --- |
| `Pass` | The whole interval is inside the inclusive bounds and the sample minimum is met |
| `Fail` | The whole interval is outside a disallowed boundary |
| `Inconclusive` | An interval overlaps a boundary, sample count is too small, or uncertainty is unavailable |
| `Invalid` | Required cells/evidence are missing, simulations are incomplete, or the comparison/cohort is incompatible |

Duration/health means share the comparison's normal-approximation policy: at least 30 nonconstant eligible samples are needed for an interval. A stricter per-goal `minimumSamples` still applies. No victories/shared victories give an inconclusive conditional-duration result. Constant observed differences are not assumed to establish zero population uncertainty. Intervals are evaluated per check without a correction across goals/cells; review this limitation before enforcing many goals. Duration must always be interpreted beside clear rate.

The report separates **assessment** (all findings, including drafts) from **enforcement** (reviewed primary/guardrail checks). A draft failure or inconclusive result remains advisory. To enforce a goal after review, explicitly change that goal to `Enforced` and provide a nonempty `reviewReason`; diagnostics cannot be enforced. Mixed policies show both draft and enforced counts, and an enforcement pass applies only to the latter. The CLI never edits the policy or promotes a baseline.

Evaluation exit codes:

| Code | Meaning |
| --- | --- |
| `0` | Valid advisory evaluation, or all enforced checks pass |
| `1` | At least one enforced check fails |
| `2` | Invalid configuration/evidence/coverage, even when all goals are draft |
| `3` | No enforced failure, but at least one enforced check is inconclusive |
| `130` | Evaluation cancelled |

Invalid takes precedence over fail, which takes precedence over inconclusive. Malformed/corrupt input writes `failure.json`; valid archives with policy/cohort/evidence issues retain an `evaluation.json`/Markdown report marked invalid. Current `suite`/`compare` exit behavior is unchanged. The CI smoke workflow below verifies advisory execution; reviewed gameplay enforcement remains a separate decision.

## Verify the workflow locally and in CI

From the repository root:

```powershell
./build/smoke-balance.ps1 -OutputDirectory TestResults/balance/smoke-001
```

Add `-NoBuild` after building the tool or backend test project. The script runs the reference fixture twice with three samples per cell, creates a disposable same-revision repeatability manifest, checks a complete comparison with zero changed evidence/gameplay, replays a battle with detailed logging, and evaluates all 60 draft checks. It requires a new output directory and writes `summary.md` plus both complete bundles, comparison/evaluation reports and `replay.json`. It does not replace a reviewed baseline or compare with another Git revision.

`-SamplesPerCell 100` runs the same workflow at reference size; `-Seed` and `-Configuration` are also supported. Choose another cohort with `-SuitePath` and its matching `-GoalsPath`. The default control smoke has 72 total battles and 60 inconclusive checks; the First Hunt smoke has 216 total battles and 180 inconclusive checks. Goal sample minimums are preserved. Group cohorts preferentially replay a multi-enemy cell. Draft failures or inconclusive findings do not fail the workflow; execution, integrity, repeatability and replay failures do. The script refuses a policy containing enforced goals so a later promotion cannot silently turn this small advisory sample into a gameplay gate.

[The GitHub Actions workflow](../../../.github/workflows/balance-harness.yml) runs the harness tests through `build/run-tests.ps1` and then smoke workflows for both cohorts on relevant backend/tool pull requests or manual dispatch. It publishes job summaries and retains both cohorts' evidence/test results for seven days, including partial artifacts on failure. The job timeout is 15 minutes, with three minutes for each smoke step. Local smoke/reference validation has passed; the first hosted execution remains to be observed after push. Gameplay targets and branch-protection requirements are not enabled by this change.

## Run a single fight

From the repository root:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- run --output TestResults/balance/starter-001 --seed 1337
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/starter-001
```

The single-fight default remains the level-1 mace/Goblin control against a Goblin in Lumo Ruins. Its `tutorial-starter` profile name is historical; Goblin is not a current First Hunt reward choice. Each run requires a new output directory. Add `--detailed` to `run` or `replay` to capture the event log; suites capture compact telemetry and enable detail on replay. Replay writes JSON to stdout and its completion message to stderr. Complete suites and completed single fights (including defeats/draws) return exit code 0. Invalid inputs, execution errors, incomplete suites or replay mismatches return 2; cancellation returns 130.

`run` also accepts `--content-root <API.LL-directory>` and `--scenario <json>`. The default content root is discovered relative to the built tool. A scenario may use `tutorial-starter` or an explicit `build` matching its `characterProfile` ID. Build recipes use `EquipmentReferenceBuildDefinition`; the shared factory enforces equipment types, hand rules, tier eligibility, essence slots and distinct monster families. The fixed creature must belong to the area and the character must meet its level requirement.

## Saved artifacts

Single-fight and suite runs contain:

| Artifact | Contents |
| --- | --- |
| `suite-input.json` | Suite recipe, assumptions, materialized cell inputs, explicit rules/settings, versioned seed schedule and every battle ID/seed |
| `input.json` (single fight) | Scenario assumptions, materialized character attributes, frozen equipment descriptors, essence progression, authored creature/area, seed, explicit rules and combat settings |
| `manifest.json` | Input/content checksums, hashes of the tool and game assemblies, .NET runtime and platform |
| `content/Data/` | One shared snapshot of 14 allowlisted catalog files per run |
| `battles/<battle-id>.json` / single-fight `result.json` | Prepared participants, engine/content outcome, ticks and seconds, terminal state, per-entity/ability statistics, compact telemetry, optional event log |
| `battles.jsonl` | Compact outcome/error index, flushed as each battle completes |
| `scorecard.json`, `scorecard.md` | Suite completeness, per-cell metrics, assumptions and replay examples |
| `failure.json` | Preflight, cancellation or bundle failure details when execution cannot complete normally |

Later workflow commands create separate artifacts:

| Command | Artifacts |
| --- | --- |
| `baseline accept` | The requested manifest JSON, pointing to the retained suite bundle and pinning its accepted evidence |
| `compare` | `comparison.json` and `comparison.md` with compatibility, paired changes, uncertainty and battle examples |
| `evaluate` | Frozen `goals.json`, `evaluation.json` and `evaluation.md`; also `comparison.json`/`comparison.md` when a baseline is supplied |

These commands require a new output file or directory and preserve their source bundles. Corrupt or missing comparison/evaluation evidence produces `failure.json` in the new output directory; an invalid report is never a passing balance result.

`TestResults/` is already ignored by Git. Only the required non-secret combat settings are selected from `appsettings.json`; the settings file itself, account data, connection strings, and credentials are not copied. Environment-specific setting overrides are not applied.

Replay uses the saved materialized inputs and archived content, without consulting the current API content root. It rejects changed inputs/content or a different runtime/platform/game/tool binary identity. Preserve the original checkout/build if a run must remain replayable; the bundle fingerprints executable code but does not archive source patches or binaries. Debug and Release builds are different replay identities.

If a saved result exists, replay compares preparation and gameplay summaries, allowing event logging to differ. Failures after the input and manifest have been saved can be re-executed; failures before that point contain diagnostics but not a complete replay bundle. Suite cancellation during execution writes a partial scorecard and preserves completed battles. Replaying a failed/unexecuted trial does not rewrite its original status or scorecard. There is no resume command.

## Gameplay parity

The harness uses `CombatPreparationPipeline`, `CombatSetupService`, the authored region scaling provider, `CombatEngineExecutor.ExecuteSimulationAsync`, and the ordinary encounter result factory. Normal idle rules are explicit: 6,000 maximum ticks, 30-tick base attack interval, active abilities starting on cooldown, and compact telemetry enabled. The engine supplies the tick rate.

Small production seams make the file-only composition possible:

- `EssenceCombatLoadoutFactory` contains the existing selected-loadout calculation; `EssenceSystemService` delegates to it.
- `CombatPreparationPipeline` accepts live-only construction without a persisted-snapshot builder. Snapshot requests still require that dependency.
- `EquipmentReferenceBuildFactory` optionally permits incomplete equipment, preserving full-loadout validation for existing callers. Both recipes and frozen inputs use its slot validation; materialized two-handed weapons share a single instance across both hands.

`BalanceHarnessTests` rebuilds equivalent sources independently, then uses `IdleCombatResolutionSessionFactory` and the normal executor path to compare preparation, outcomes, duration, terminal state and statistics for multiple seeds and progression builds, including armor and two-handed/multiple-essence cases. It also checks repeated/concurrent execution, compact versus detailed logging, replay integrity, invalid inputs, and sensitivity to incorrect opening cooldowns. `BalanceHarnessSuiteTests` covers matrix expansion, paired seeds, legal partial gear, statistical calculations, victory/defeat replay and partial cancellation artifacts. `BalanceHarnessComparisonTests` checks baseline immutability, archived-build comparison, known paired statistics, win-duration selection, incompatible/partial runs, corrupted evidence, and an actual enemy-offense change applied only to temporary content.

`BalanceHarnessGoalTests` covers inclusive and one-sided bounds, zero-win numerical endpoints, small samples, fixture/policy validation, draft versus reviewed enforcement, distinct exit codes, missing/incompatible baseline evidence and immutable evaluation artifacts.

`BalanceHarnessFirstHuntTests` covers the authored starter choices, legal quest-reward budgets, 36-cell/180-check contract, independent duplicate enemies, group validation, archived group replay/comparison/evaluation and unchanged legacy hashes. Production parity cases also cover duplicate and mixed enemy groups with the actual First Hunt Essences.

`BalanceHarnessProgressionTests` verifies training recipes and frozen progression. `BalanceHarnessEntryTests` verifies the armor/style matrix, production Forge-quote equivalence, reward budgets, complete experiment/replay evidence, paired schedules, output preservation and cancellation. Two additional `BalanceHarnessTests` parity cases cover the selected Goblin Warrior/Shortsword/Heavy Breastplate reference against both Blood Grove pairs. `BalanceHarnessGoalTests` also pins its reviewed policy and checks uncertainty inside and outside the working band. The current harness filter covers 79 passing tests across seven classes.

Run relevant backend verification through the repository script:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarness|FullyQualifiedName~CanonicalEquipmentBuildFactoryTests|FullyQualifiedName~EquipmentHandRuleTests'
```

Shared combat/preparation changes also warrant the full `./build/run-tests.ps1` suite.

## Scope

Each trial resolves a fresh single fight. The suite conditions on specific spawns; it does not estimate an area's overall win rate. It excludes spawn-distribution sampling, offline time progression, rewards, account persistence, and multi-encounter carryover. Runs execute sequentially with a fresh executor and mutable combat state per battle. Elapsed wall time is recorded but is not a controlled performance benchmark.

Explicit baseline acceptance, paired comparison, goal evaluation and advisory CI configuration for both original cohorts are available. The First Hunt cohort improves starter/reward/encounter coverage. The selected Blood Grove starter now has a reviewed working clear-rate band; other numerical goals remain draft. A viable starter baseline, first hosted CI validation, broader progression coverage, additional content adapters and rankings remain open. A completed suite establishes reproducible measurements, not balance acceptance.

No database, running API, hosted workers, migrations, deployment, or production configuration changes are required. The tool adds a `Microsoft.Extensions.Configuration` dependency matching the existing backend's 10.0.5 version.
