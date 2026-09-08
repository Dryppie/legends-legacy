# Balance Harness: idle balance workflow

An offline .NET console tool for measuring progression difficulty and explaining the effect of combat code or content changes. It runs real idle combat through production preparation and execution. The reference suite has 12 cells: three progression checkpoints × two builds × two encounters, with 100 fixed seeds per cell (1,200 battles). It saves replayable battles, produces Markdown/JSON scorecards, compares accepted references and evaluates versioned goals.

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

Seeds are derived by `StableRandom.Seed` from `idle-suite-seeds-v1`, the master seed, stage ID, encounter ID and zero-based trial index. Alternative builds share the same encounter seeds. Reordering cells or changing combat coefficients preserves the schedule. Changing stable stage/encounter IDs changes it. Results across builds are paired observations and must not be pooled as independent samples.

The scorecard reports wins/losses/draws, valid sample sizes, a 95% Wilson clear-rate interval, separate win/non-win durations, final player health and tick-limit draws. JSON includes mean, median and interpolated p90 distributions; p90 is unavailable below ten observations. Missing samples remain unavailable rather than becoming zero. Invalid, cancelled and unexecuted battles are counted separately and cannot produce a complete suite. No balance pass/fail policy is applied.

Select a battle ID from `scorecard.md` or `battles.jsonl` to reproduce it with an event log:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/idle-reference-001 --battle starter.mace.ordinary.0001 --detailed > TestResults/balance/starter-replay.json
```

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

The [versioned goals file](Fixtures/idle-goals.json) defines six **draft proposals**, expanded into 60 checks across all 12 cells. It proposes 90% minimum clears for ordinary enemies, a 60–90% challenge clear-rate band, a 60-second mean winning duration limit, and tolerances for baseline movement. These are proposed experience goals, not values established by the observed results. Every shipped goal is `Draft`; no balance gate is enabled by default.

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

Invalid takes precedence over fail, which takes precedence over inconclusive. Malformed/corrupt input writes `failure.json`; valid archives with policy/cohort/evidence issues retain an `evaluation.json`/Markdown report marked invalid. Current `suite`/`compare` exit behavior is unchanged. CI smoke execution and reviewed gameplay enforcement remain a separate integration step.

## Run a single fight

From the repository root:

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- run --output TestResults/balance/starter-001 --seed 1337
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run TestResults/balance/starter-001
```

The single-fight default remains the tutorial starter mace/Goblin essence against a Goblin in Lumo Ruins. Each run requires a new output directory. Add `--detailed` to `run` or `replay` to capture the event log; suites capture compact telemetry and enable detail on replay. Replay writes JSON to stdout and its completion message to stderr. Complete suites and completed single fights (including defeats/draws) return exit code 0. Invalid inputs, execution errors, incomplete suites or replay mismatches return 2; cancellation returns 130.

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

Run relevant backend verification through the repository script:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarness|FullyQualifiedName~CanonicalEquipmentBuildFactoryTests|FullyQualifiedName~EquipmentHandRuleTests'
```

Shared combat/preparation changes also warrant the full `./build/run-tests.ps1` suite.

## Scope

Each trial resolves a fresh single fight. The suite conditions on specific spawns; it does not estimate an area's overall win rate. It excludes spawn-distribution sampling, offline time progression, rewards, account persistence, and multi-encounter carryover. Runs execute sequentially with a fresh executor and mutable combat state per battle. Elapsed wall time is recorded but is not a controlled performance benchmark.

Explicit baseline acceptance, paired comparison and goal evaluation are available. Draft goals cover reliability, challenge, pacing and practical baseline movement; optional enforcement uses reviewed primary/guardrail goals. Gameplay review, CI integration, additional content adapters and rankings remain future work. A completed suite establishes reproducible measurements, not balance acceptance.

No database, running API, hosted workers, migrations, deployment, or production configuration changes are required. The tool adds a `Microsoft.Extensions.Configuration` dependency matching the existing backend's 10.0.5 version.
