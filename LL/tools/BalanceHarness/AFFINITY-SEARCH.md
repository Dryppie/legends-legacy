# Supported affinity search

Use `affinity-creation-with-benchmark-validation-v1` as the supported offline search profile. It combines the original `tower-proposal-policy-v3` affinity proposer with `tower-proposal-racing-v5` and the existing benchmark validation gate. This choice establishes a maintainable baseline; it does not establish superiority over the confirmed benchmark.

`TowerAffinitySearch.CreatePlan` takes an admitted racing plan, the frozen affinity inventory and the selected affinity IDs. It copies the inputs and fixes the proposal and validation policies. `RunAsync` executes through the existing native archive boundary and returns a concise result:

| Status | Meaning |
| --- | --- |
| `BenchmarkRetained` | The challenger failed the fresh paired validation gate. Keep the benchmark. |
| `ChallengerNeedsConfirmation` | The challenger passed this search's provisional gate. Independently confirm the exact team before treating it as a replacement. |
| Any incomplete status | No selected team is returned. Retain the failure evidence. |

Each complete search evaluates 17 generated proposals in two waves and spends 528 fights. The five-member nomination panel contains two generated finalists and three references. The supported profile chooses one of the four nonbenchmark nominees, then compares it with the benchmark on 60 fresh paired seeds. The exact win/loss gate, primary-reference tie rule, zero-win health rule and benchmark fallback are unchanged.

## Commands

For a quick, readable review of an already published and audited comparison, run from the repository root with Python 3.12 or newer:

```text
python -B "Balance Harness/analysis/review-affinity-search.py" <completed-comparison> --closeout-pin <trusted-closeout-sha256> --output <new-review-directory>
```

The output parent must exist. Obtain the closeout pin from the retained execution record, independently of the archive being reviewed. The reader supports benchmark-validation, affinity-preservation and affinity-nomination comparisons, selecting the supported profile's arm for each version.

`report.md` shows the comparison, every root's validation and held-out results, named loadouts and required essence copies. `review.json` retains the exact equipment, progression and seed-free scenarios. Essence order stays unchanged; every passing challenger still requires independent confirmation. Output uses a new directory and includes consumed-file hashes and a completion receipt.

This reader authenticates 14 consumed published files and checks agreement of the saved native and independent audits. It does not rerun combat, reconstruct the full archive or check unconsumed battle files. Keep the existing verifier for full reconstruction. Reviewing the completed nomination study took approximately 2.4 seconds on the development machine; that measures report generation, not faster search or equivalent audit work.

With the qualified harness DLL, inspect a bound plan without starting fights:

```text
dotnet <BalanceHarness.dll> tower-affinity-search-check <racing-plan.json>
```

Reconstruct and summarize an already sealed supported search:

```text
dotnet <BalanceHarness.dll> tower-affinity-search-verify <search-archive> <files.json-sha256>
```

The archive must be an individual search root containing `racing/plan.json`; a comparison's `search/root-01/control` is an example. The supplied SHA-256 must come from the retained trusted manifest pin. Verification authenticates the native evidence before returning the summary.

These commands do not allocate seeds or start unowned work. Execution remains inside the existing admitted owner, which handles history exclusions, process and storage limits, evidence and independent audits. There is no new standalone public launch command in this change.

## Runtime profiling

The native search, proposal generation, private copies, archive evaluation and battle writes expose opt-in `TowerPerformanceTrace` stages. Profiling leaves stored search contracts unchanged. Use exclusive times for a breakdown; nested inclusive times overlap.

The optional `BalanceHarnessAffinityRuntimeTests` fixture replays one sealed supported root twice, checks every recipe, seed, input hash and complete battle report against the source, and records timings separately from setup and parity checks. It also compares the old and compact copy implementations on the saved plan. This is 1,056 repeated engineering fights with no new seeds or strength evidence. The fixture is skipped unless explicitly configured:

```powershell
$env:LL_AFFINITY_PROFILE_SOURCE = '<sealed-search-root>'
$env:LL_AFFINITY_PROFILE_PIN = '<externally-retained-root-files-sha256>'
$env:LL_AFFINITY_PROFILE_OUTPUT = '<new-profiling-directory>'
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAffinityRuntimeTests'
```

Each pass writes `timing.json`; `parity.json` appears only after all 528 reports match. A five-minute cancellation deadline and output checks bound the fixture. Retain any incomplete evidence. Timings from one archived floor and team schedule do not establish performance across other encounters.

See the [runtime profile and copy optimization results](../../../Balance%20Harness/Tower-Affinity-Runtime-Profile.md) for measured costs, replay parity and limitations.

## Encounter compatibility

The optional `BalanceHarnessAffinityEncounterTests` fixture prepares three diagnostic references on all 15 released floors, then runs the unchanged supported search on floors 3, 5, 7, 8, 10 and 15. The [fixed case definition](Fixtures/tower-affinity-encounter-coverage.json) covers 5-, 10- and 15-character parties, summons, healing, recovery suppression and damage modifiers. Each complete 528-fight archive must pass native reconstruction.

The fixture reuses an archived root's seed schedule and level-40, five-Essence equipment budget. Smaller parties use the last five existing members so the three references remain distinct; larger parties repeat existing loadouts into new character slots. Essence order stays unchanged. Projected references are labeled diagnostic and carry no transferred confirmation. This checks compatibility, not encounter balance, intended progression or search superiority.

```powershell
$env:LL_AFFINITY_COVERAGE_SOURCE = '<sealed-supported-search-root>'
$env:LL_AFFINITY_COVERAGE_PIN = '<externally-retained-root-files-sha256>'
$env:LL_AFFINITY_COVERAGE_OUTPUT = '<new-coverage-directory>'
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAffinityEncounterTests'
```

The combat fixture is skipped unless configured. All plans and reference preparations precede combat. The fixed limit is 3,168 fights, with a ten-minute cancellation deadline and 1.5 GiB output checks. Per-floor receipts retain archive pins and provisional search summaries; `coverage.json` records completion. No seeds are freshly allocated and no team is confirmed by this fixture.

The [completed coverage run](../../../Balance%20Harness/Tower-Affinity-Encounter-Coverage.md) passed all preparations and six native reconstructions. Its ceiling and zero-win cases limit what it says about search quality; use explicit progression budgets for a future quality evaluation.

## Progression reference screen

The [completed progression screen](../../../Balance%20Harness/Tower-Affinity-Progression-Screen.md) ran 576 reference fights across six existing progression/catalog budgets, using one captured runtime and historical seeds. Floors 3 and 15 had room to observe improvement under the predeclared screening rule. This phase did not run or change the search algorithm.

```text
python -B "Balance Harness/analysis/screen-affinity-progression.py" --output <new-screen-directory> --execute
```

Omit `--execute` to freeze inputs without combat. Run from the repository root on Windows with Python 3.12 or newer and .NET available. The [fixed definition](Fixtures/tower-affinity-progression-screen.json) pins required local historical evidence; a clean checkout alone does not contain it. Each invocation requires a new output directory, retains failures and does not retry or resume.

The screen preserves ordered recipes, authored allies and producing runtime/content. Its output includes all cells and a seed-free follow-up export. The export removes an order-only duplicate among the floor-3 controls by taking the next distinct composition in saved catalog order; this third composition is not covered by the completed screen's results. Floor 15's stronger existing controls must remain visible in any subsequent search evaluation. Neither the screen nor its export confirms a team or approves a progression target.

## One explicit experiment

`tower-affinity-nomination-comparison-v1` compares the supported profile with experimental `tower-proposal-racing-v9`. The experiment permits only the two generated finalists to challenge the benchmark. Its proposer, all first 408 observations and the final validation gate remain identical to the supported arm. Native and independent Python audits enforce those constraints.

The experiment is governed by [the frozen design and stop rule](../../../Balance%20Harness/Tower-Affinity-Search-Consolidation.md). A result below that rule ends this tuning cycle. It does not trigger automatic parameter changes, another seed draw or a new variant.

The [completed comparison](../../../Balance%20Harness/Tower-Affinity-Nomination-Pilot-01-Execution.md) retained the benchmark in both arms on all twelve roots. Its audited decision was `NoObservedOutputDifferentiation`; the tuning cycle is closed and the supported profile is unchanged.

Historical search commands retain their versioned behaviour. Gameplay, APIs and deployment configuration are outside this offline harness change.

See the [implementation and verification record](../../../Balance%20Harness/Tower-Affinity-Search-Implementation.md) for changed components and engineering evidence.
