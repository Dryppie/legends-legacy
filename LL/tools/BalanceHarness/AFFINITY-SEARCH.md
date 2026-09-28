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

## September 25 floor-3 evaluation

The [September 25 floor-3 evaluation](../../../Balance%20Harness/Tower-Affinity-Floor3-Evaluation.md) ran the unchanged 528-fight search and measured both generated finalists plus all three references on 128 separate seeds each. All five won 0/128 on that build, so the benchmark was retained. A matched 128-fight earlier-runtime check gave that same benchmark 88/128 wins with identical recipe, seeds, content and settings. These are historical measurements, preceding the September 28 attributes/equipment/healing alignment.

`BalanceHarnessAffinityFloorEvaluationTests` is opt-in via `LL_AFFINITY_FLOOR_EVALUATION`, a pinned request file. Build and run deterministic checks through `build/run-tests.ps1` with an isolated `-ArtifactsPath` first. `Balance Harness/analysis/run-affinity-floor-evaluation.py` prepares the fixed request and owns the no-build test process; it requires new `--package` and `--output` paths plus the tested `--artifacts` directory. This command allocates 237 fresh values against the complete balance registry and allows one 1,168-fight evaluation. It does not retry, confirm teams or modify search policy. The linked execution record contains the commands, pins and limitations.

## Current balance selection

Tower execution, preparation, search inventories and replay now carry `TowerSettings.Balance`: independent attribute rules, equipment release and optional ability balance profile. Current repository settings select **18 / 4 / healing-v1**. New normal Tower inputs also capture the selection; loadout and compact archives capture it in their scope. Cache identity includes it. Snapshot schema 4 adds the equipment release registry, releases 3/4 and the healing override; older snapshot allowlists and absent-selection serialization remain readable. Historical replay still requires its original executable.

The [September 28 evaluation record](../../../Balance%20Harness/Tower-Current-Balance-Evaluation-20260928.md) reports the current reference screen and search outcome. `screen-current-tower-balance.py` freezes three distinct references on floors 3, 7 and 15 at the existing gear budgets, then runs 32 paired historical seeds per reference. It preserves equipped Essence order. Its `--settings` file contains only the combat, checkpoint, attribute-version and equipment-version settings, never the full application configuration. All recipes, settings, content and executable hashes precede combat. The optional `--continue-frozen` flag only recovers the recorded verifier-casing failure by rechecking completed cells; it cannot rerun a partial or failed battle process.

For a subsequent bounded floor-3 evaluation, pass `--screen <completed-result.json>` and `--history <latest-complete-seed-ledger.json>` to `run-affinity-floor-evaluation.py`, with a predeclared `--master`. The fixture authenticates current screen eligibility, runtime, balance selection and content before allocating seeds. The native registry scan must exactly match the supplied ledger. The historical source supplies only the unchanged search policy and schedule template; mechanics are rebuilt from current captured content and fully validated before allocation. Both generated finalists and all three references receive a separate 128-seed panel; those measurements never change the selected team.

## September 28 gear specialization evaluation

The [completed gear evaluation](../../../Balance%20Harness/Tower-Gear-Specialization-Evaluation-20260928.md) held the retained floor-15 benchmark's Essences fixed and compared six equal-budget specialization bundles. Armor heads/chests/legs plus health necklaces won **256/256** separate confirmation fights against the original gear's **83/256**, with 173 gained wins and no lost wins. This is the working gear benchmark for that exact team and captured budget; it does not establish search-algorithm superiority or a universal equipment default.

`Fixtures/tower-gear-specialization-screen.json` defines the bundles. The optional `IdentityEquipment` reference-build field preserves character, item and Essence instance IDs while actual equipment still drives stats and legality. The new executable reproduced all 512 prior baseline input hashes before fresh allocation. The opt-in `BalanceHarnessGearScreenTests` fixture and `Balance Harness/analysis/run-tower-gear-screen.py` own the bounded 1,408-fight evaluation and separate confirmation. The linked report retains commands, archive pins, exact seed-free variants and the independent audit.

## Reusable gear profiles

`TowerGearProfiles.Apply` applies a profile to a copied scenario or party using the selected production equipment catalog. Replacements keep archetype, rarity, tier, rank, quality, roll multiplier, positions and Essence order. Equal item budgets and unchanged character/item/Essence instance IDs are checked. Existing identity pins remain in force. Missing specializations, requested equipment slots and styled gear are rejected. Empty `partySlots` targets every member; named positions apply where present in a smaller party, with at least one matching position required. Reapply to the original scenario to compare profiles independently.

Export a scenario without fighting:

```text
dotnet <BalanceHarness.dll> tower-gear-profile-apply <scenario.json> <profile-catalog.json> armor-and-health <content-root> <new-scenario.json>
```

`TowerAffinitySearch.WithGearProfile` applies the chosen profile consistently to a supported plan's templates and all three references, then rebuilds generation mechanics. Existing compositions, ownership constraints, seeds, search policy and benchmark validation stay fixed. Reference provenance records the gear change and explicitly drops any transferred strength claim. The full changed plan and combat inputs give it a different archive/cache identity, even though composition-only party IDs remain the same.

```text
dotnet <BalanceHarness.dll> tower-affinity-search-gear <plan.json> <profile-catalog.json> armor-and-health <content-root> <new-plan.json>
dotnet <BalanceHarness.dll> tower-affinity-search-check <new-plan.json>
```

Both commands require a new output file and run zero fights. Search-plan application requires current matching settings, content, inventory and executable. It is an explicit choice before Essence search, not automatic selection from evaluation results or a joint gear/Essence optimizer. The resulting plan still needs admission through the existing owner. The command retains the input plan's seed schedule; it does not make archived seeds fresh. Gear comparisons have their own declared fight costs, separate from the unchanged 528-fight Essence search.

The [completed encounter coverage](../../../Balance%20Harness/Tower-Gear-Profile-Integration-20260928.md) prepared 42 combinations and ran 1,344 diagnostic fights using historical seeds. Armor-and-health won 32/32 on floor 15 but 12/32 on floor 13; resistance-and-health won 32/32 on floor 13. Every tested encounter/budget had at least one 32/32 profile. That screen confirmed no new team and established no search-algorithm improvement.

The [separate floor-13 confirmation](../../../Balance%20Harness/Tower-Floor13-Gear-Confirmation-20260928.md) then tested the exact resistance profile on 512 fresh paired seeds. It won **512/512**, versus **194/512** for original gear and **260/512** for armor-and-health. Both comparisons passed the frozen practical-gain and adjusted exact-test criteria. Retain resistance-and-health for that floor-13 team and budget, alongside the separately confirmed floor-15 armor profile. Keep gear choices explicit and encounter-specific.

The [completed progression/benchmark review](../../../Balance%20Harness/Tower-Gear-Aware-Benchmark-Review-20260928.md) found that the tested levels already match minimum Essence-slot unlock levels, while per-floor gear/ownership budgets remain provisional. All six screened cases have a 32/32 gear profile. It froze an offline floor-13 calibration with all seven measured gear profiles retained, five offense settings and a 1,120-fight cap. Intended-progression acceptance still requires explicit player budgets and the existing 10–50% policy.

The [completed offense calibration](../../../Balance%20Harness/Tower-Gear-Offense-Calibration-20260928.md) ran all 1,120 historical-seed fights. Offense **4.20** was the sole eligible setting: resistance-and-health won **22/32**, and the other six profiles won zero. Original offense 3.36 had a 32/32 profile; offense 5.04, 6.72 and 10.08 had no wins. Selection uses the best profile's 4–28/32 range, not a weaker reference. All 224 baseline inputs and full reports matched the old coverage archive. This is a diagnostic benchmark candidate, not fresh confirmation, search improvement or balance acceptance. Production content is unchanged.

`BalanceHarnessGearCalibrationTests` is opt-in through `LL_GEAR_CALIBRATION`; `Balance Harness/analysis/run-tower-gear-calibration.py` owns the fixed run, and `verify-tower-gear-calibration.py` independently audits it. The calibration itself did not run a search, allocate seeds, or transfer the original encounter's team confirmations.

The [completed gear-aware floor-13 evaluation](../../../Balance%20Harness/Tower-Floor13-Geared-Search-Evaluation-20260928.md) screened all three retained references and their exact search projections in 192 historical-seed fights. The benchmark won 22/32 retained and 23/32 projected; both other references won zero. No reference exceeded the 28/32 ceiling. The subsequent unchanged 528-fight search retained that benchmark: the challenger gained 16 wins and lost 8 in validation, failing the exact 0.05 gate. On a separate 128-seed panel, the two generated finalists won **87/128** and **76/128**, versus **86/128** for the benchmark. Close this evaluation without another confirmation campaign for the one-win difference. Keep the supported policy and benchmark; this is a diagnostic comparison, not a production balance decision.

`BalanceHarnessGearReferenceTests` and `screen-tower-geared-references.py` own the reference gate. The floor-evaluation owner now accepts `--floor 13` with `--screen-manifest-pin`, a completed eligible geared-reference screen and the latest history ledger. It imports that screen's projected recipes and captured content, preserving other floors' existing behavior. The independent `verify-floor13-geared-search.py` audits all outcomes, fresh allocation and the unchanged validation gate. The latest completed exclusion union is 834,295; use the new search ledger alongside the full registry for later allocation.

## Progression budget preview

The current [user-defined repeating equipment curve](../../../Balance%20Harness/Tower-Repeating-Equipment-Curve-20260928.md) uses Rare / Standard / rank 2 on positions 1–3, Epic / Fine / rank 3 on 4–6, Unique / Exceptional / rank 4 on 7–9, and Legendary / Masterpiece / rank 5 on position 10. It repeats on floors 11–20, 21–30 and onward. Level, tier and Essence count are separate declarations. The new version-2 fixture prepares the eleven currently declared checkpoints with all seven gear choices and whole-party reinforcement costs.

```text
dotnet <BalanceHarness.dll> tower-progression-budget-preview <Fixtures/tower-progression-budget-cycle.json> <content-root> <fixtures-root> <Fixtures/tower-gear-specialization-screen.json> <new-preview.json>
dotnet <BalanceHarness.dll> tower-progression-gear-apply <scenario.json> <Fixtures/tower-progression-budget-cycle.json> <content-root> <new-scenario.json>
```

These commands perform production preparation without combat and export seed-free parties. The completed current preview contains 77 legal parties, and 19 retained checkpoint base parties have been converted separately. The conversion preserves level, tier, ordered Essences, positions and specializations, while replacing rarity, quality and rank. It removes old seeds and transfers no strength claims. The [new-curve baseline](../../../Balance%20Harness/Tower-Equipment-Cycle-Baseline-20260928.md) has now re-established checkpoint results before calibration. Hypothetical ownership does not establish acquisition feasibility or balance acceptance.

That baseline ran 224 old-budget parity checks followed by 4,256 new-budget fights. Floor 10 won 32/32 throughout; 82/84 intended floor-11 combinations exceeded 50%. All six-Essence floor-11 variants won 32/32, and the four-Essence primary reached 32/32 with resistance-and-health, versus 5/32 previously. These historical-seed diagnostics motivated the floor-11 calibration below. They do not confirm a setting or change production. `BalanceHarnessEquipmentCycleScreenTests`, opt-in through `LL_EQUIPMENT_CYCLE_SCREEN`, is owned by `screen-tower-equipment-cycle.py`; `verify-tower-equipment-cycle.py` independently authenticated 4,759 files and every prior-budget pair. The repeating curve defines reference budgets; players' carried-over gear is not downgraded by this work.

The [completed floor-11 linked Health/Power calibration](../../../Balance%20Harness/Tower-Floor11-Linked-Calibration-20260928.md) kept all 112 team/gear combinations across factors 1, 1.125, 1.25, 1.5, 2 and 3. Its 21,504 historical-seed fights included 3,584 baseline input/full-report parity checks. **No setting qualified**: factor 1.5 put the strongest seven-Essence team at 11/32 but left a six-Essence control at 16/32; factors 2 and 3 produced no wins. No production setting changed. `BalanceHarnessFloor11CalibrationTests` is opt-in through `LL_FLOOR11_CALIBRATION`; `calibrate-floor11-linked.py` owns the fixed grid, and `verify-floor11-linked.py` reconstructs it independently. The next useful comparison is controlled seven-Essence upgrades of the strong six-Essence parties, including a level-matched six-Essence control. No retained seven-Essence finalist is already a complete extension of either six-Essence party.

The [earlier provisional draft](../../../Balance%20Harness/Tower-Progression-Budget-Draft-20260928.md) and `tower-progression-budget-draft.json` remain version 1 for historical reproduction. Older slot-based presets and the following checkpoint screen use their original declared budgets; they are not the current intended equipment curve.

The [completed floor-10/11 checkpoint screen](../../../Balance%20Harness/Tower-Progression-Checkpoint-Screen-20260928.md) uses that equipment curve for an explicit offline pass: three six-Essence floor-10 teams, all twelve retained seven-Essence floor-11 finalists, and separate four-/six-Essence floor-11 controls, each with all seven gear choices. It ran 4,256 historical-seed fights, including 224 exact input/report parity checks before the remaining cells. All floor-10 combinations won 32/32; 80 of 84 intended floor-11 combinations exceeded 50%, and six-Essence controls also reached 32/32. This motivates boss calibration with strong controls retained, not another search-policy experiment.

`BalanceHarnessProgressionCheckpointTests` is opt-in through `LL_PROGRESSION_CHECKPOINTS`; `screen-tower-progression-checkpoints.py` freezes and owns the fixed screen, and `verify-tower-progression-checkpoints.py` independently checks its imported recipes, gear, raw reports and runtime parity. The completed audit verified 4,494 files. No fresh seeds, balance acceptance, automatic calibration or production changes follow from this diagnostic.

## One explicit experiment

`tower-affinity-nomination-comparison-v1` compares the supported profile with experimental `tower-proposal-racing-v9`. The experiment permits only the two generated finalists to challenge the benchmark. Its proposer, all first 408 observations and the final validation gate remain identical to the supported arm. Native and independent Python audits enforce those constraints.

The experiment is governed by [the frozen design and stop rule](../../../Balance%20Harness/Tower-Affinity-Search-Consolidation.md). A result below that rule ends this tuning cycle. It does not trigger automatic parameter changes, another seed draw or a new variant.

The [completed comparison](../../../Balance%20Harness/Tower-Affinity-Nomination-Pilot-01-Execution.md) retained the benchmark in both arms on all twelve roots. Its audited decision was `NoObservedOutputDifferentiation`; the tuning cycle is closed and the supported profile is unchanged.

Historical search commands retain their versioned behaviour. Gameplay, APIs and deployment configuration are outside this offline harness change.

See the [implementation and verification record](../../../Balance%20Harness/Tower-Affinity-Search-Implementation.md) for changed components and engineering evidence.
