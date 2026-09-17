# Paired practical search allocation comparison

17 September 2026. Prospectively specified before the first allocation or fight. Target: offline BalanceHarness, using the captured gameplay/content of the confirmed fixed-team study. Current-family calibration is a separate experiment.

**Follow-up completed:** the [saved candidate-coverage analysis](Tower-Practical-Search-Candidate-Coverage-Review.md) reconstructed all six trajectories without new combat. It recommends a separately versioned [balanced supplied-anchor neighborhood policy](Tower-Practical-Anchored-Neighborhood-Proposal.md); the comparison and its no-promotion decision below remain closed.

## Question and fixed scope

Does `retained-composition-racing-v1` produce stronger selected teams than `retained-composition-incumbents-v1` at the same search ceiling, starting from the exact confirmed `399bc776…` team and Anchor040e (`8287f779…` canonical party)? Neither method receives historical fitness. All equipment, character identities, ordering, content and gameplay assemblies remain fixed.

Three restart pairs are declared. Each pair shares its construction root, the first eight discovery values, 32 selection values, and 1,000 confirmation values. Different pairs have disjoint values. Baseline evaluates 46 candidates on eight discovery trials. Racing admits 16 candidates in two rounds with the implemented 8/16 screening/promotion panels. Both have a 256-proposal-attempt limit, 368 discovery-fight ceiling, four nominees and one selected finalist. Selection reserves 128 fights per method. Both supplied teams remain confirmation controls, giving at most 3,000 confirmation fights per method and **20,976 fights overall**. Unused capacity is not spent elsewhere.

One 4,096-word cryptographic batch supplies 3,243 distinct fresh values (3 × [1 construction + 48 discovery + 32 selection + 1,000 confirmation]). Previously reserved and duplicate values are skipped. Every fresh unused tail value also becomes a permanent exclusion. Pending is durable before entropy is drawn; the complete batch is durably retained before completion. No refill, retry, resume, extension or outcome-dependent replacement is permitted.

All six search outputs and their confirmation families must be written before the first confirmation fight. The common barrier has no outcome inputs. A failed search or failed global freeze cancels the comparison; incomplete pairs cannot be dropped. Real fights are serialized and all attempts journaled. Each study retains its ordinary archive and is reconstructed by the existing native verifier without new combat.

## Endpoint and interpretation

The primary endpoint is the equally weighted mean of three paired confirmation win-rate differences, racing minus baseline. Draws and defeats both count as non-wins. Every restart, its selected party IDs, gains/losses and actual discovery cost are reported. Confirmation outcomes cannot change the selected outputs.

Support requires a mean observed gain of at least **5 percentage points**, a positive one-sided 95% lower confidence bound for the conditional mean, and positive point differences in at least two pairs. Otherwise the decision is `DoNotPromoteRacing`; this version is closed rather than retuned or given extra samples. A positive decision supports these frozen output pairs in this scope. Three restarts do not establish general reliability across future search roots, floors or gameplay versions. This comparison does not replace fixed-team adoption or balance acceptance gates.

For the bound, each of 3,000 seed-level paired differences lies in [-1,1]. Conditional on the observed search data, let `M = 2^32 - historicalCount - 243`, `n = 3000` and `delta = n(n-1)/(2M)`. Coupling uniform distinct sampling to independent uniform draws charges at most `delta` collision probability. Apply Hoeffding with `alpha = 0.05 - delta`: `lower = mean - sqrt(2 log(1/alpha)/n)`, truncated below at -1. At the initial 497,371 exclusions the margin is about 4.49 points. Thus the five-point observed threshold dominates the positive-bound condition. This distribution-free bound sacrifices precision; it does not model search-to-search variability or guarantee that the search can achieve a particular effect. The theoretical conditional probability of passing the aggregate five-point threshold is at least `1-exp(-n*(effect-.05)^2/2)` for a true conditional effect above five points; the additional two-positive-pairs requirement can further reduce power.

## Execution and accounting

The owned native run, including live-history checks, freezing, combat and verification, has a new **2,400-second / 2-GiB** ceiling. Native work receives 2,380 seconds and 1,920 MiB, leaving enclosing-process headroom. The Windows Job owner kills the process tree if ownership ends. The command also has a hard deadline. Preparation, builds, fixture tests, the additional saved-evidence review and documentation are separately measured or disclosed engineering work, outside that operational ceiling. Prior experiment allowances remain closed and unchanged.

Inputs and logs: `TestResults/allocation-comparison-20260917/`. Native output: `TestResults/balance/tower-allocation-comparison-20260917/`. The [orchestration script](analysis/practical-allocation-comparison.py) has separate `prepare` and create-new `execute` commands. Preparation allocates no scientific values and runs no fights. No application configuration, migration or deployment is involved.

## Outcome

**Completed and verified: `DoNotPromoteRacing`.** The [native result](../TestResults/balance/tower-allocation-comparison-20260917/result.json) and [independent saved-evidence audit](../TestResults/allocation-comparison-20260917/independent-audit.json) agree. The original prospective document is preserved in [the execution package](../TestResults/allocation-comparison-20260917/prospective-design.md).

| Restart | Existing search wins | Racing wins | Racing minus existing | Discovery fights: existing / racing |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 730 / 1,000 | 708 / 1,000 | -2.20 points | 368 / 288 |
| 2 | 687 / 1,000 | 687 / 1,000 | 0.00 points | 368 / 288 |
| 3 | 705 / 1,000 | 705 / 1,000 | 0.00 points | 368 / 288 |

The equally weighted mean difference is **-0.73 percentage points**, with the predeclared one-sided 95% lower bound **-5.22 points**. There were no positive restart differences. The result misses the five-point endpoint and does not establish that racing is stronger. It also does not establish a population-level regression or general failure of all adaptive evaluation methods.

Racing returned the confirmed `399bc776…` team in all three restarts. The existing search returned that same team in restarts two and three, producing identical outcomes on the paired values. In restart one it selected `7a249fb5…`, whose observed 73.0% exceeded the confirmed team's 70.8% on that panel. This **2.2-point observation is not a fixed-team adoption result**. Its [frozen recipe](../TestResults/balance/tower-allocation-comparison-20260917/study-0/finalists.json) remains available, while the previously confirmed team recommendation stays unchanged.

Racing spent 288 rather than 368 discovery fights per restart (80 fewer, 21.7%). A lower-cost noninferiority endpoint was not declared, so this saving does not reverse the strength decision. Keep the incumbent policy as default and close this racing version's evaluation; do not extend confirmation, retune its sampling ratio or substitute an efficiency claim after observing the result. Any later search work should start with a distinct hypothesis and prospective comparison.

All **15,736 attempted fights completed**: 1,968 discovery, 768 selection and 13,000 confirmation. The global freeze preceded all confirmation after exactly 2,736 completed fights. Six full native reconstructions passed with zero added fights. The independent audit checked archive membership/hashes, entropy and stage assignments, every attempt, frozen outputs, shared-team reproducibility and result arithmetic; it agreed in **2.67 seconds**, without combat.

The owned run took **759.08 seconds (12.65 minutes)**; its sampled combined storage high-water was **820,516,127 bytes (782.5 MiB)**, within the declared limits. The owner reported exit 0, no timeout and zero remaining processes. The native run included its history and archive verification. Seed-free package preparation took 1.16 seconds; preflight/build/test/documentation costs remain separately disclosed engineering costs. There is no per-method wall-time claim because confirmation shared a serialized executor.

All 4,096 entropy words were fresh and distinct: 3,243 assigned values and 853 permanently excluded unused values. The historical union is now **501,467**. There was one launch, no retry, no refill and no sample extension. Historical calibration and V19 decisions remain unchanged.

## Implementation and verification

Changed code: [TowerAllocationComparison.cs](../LL/tools/BalanceHarness/TowerAllocationComparison.cs) implements reservation, paired definitions, the freeze barrier, serialized accounting and aggregation; [TowerBossStudy.cs](../LL/tools/BalanceHarness/TowerBossStudy.cs) and [TowerBossStudyArchive.cs](../LL/tools/BalanceHarness/TowerBossStudyArchive.cs) accept an optional asynchronous callback immediately after the existing confirmation freeze; [Program.cs](../LL/tools/BalanceHarness/Program.cs) exposes the command. Existing callers retain their previous behavior. No generator, operator or selector was altered for the comparison.

The five new [comparison tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessAllocationComparisonTests.cs) pass. They exercise entropy collisions and unused reservations, stage separation and cost equality, the actual six-study barrier, cancellation, failed freeze writes, paired arithmetic and rejection of missing/reused panels. The existing [practical test helper](../LL/tests/EssenceSystem.Tests/BalanceHarnessPracticalSearchTests.cs) now forwards the optional hook. **89 distinct relevant tests pass in total:** 84 existing policy/practical/study regressions and five new cases. Earlier failures exposed reference schedules being attached too early and a test using collection reference equality; both were corrected before execution. Retained results are in `TestResults/allocation-comparison-verification-20260917/`.

Commands used the repository wrapper:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/allocation-comparison-build-20260917 -Filter 'FullyQualifiedName~BalanceHarnessAllocationComparisonTests|FullyQualifiedName~BalanceHarnessEvaluationAllocationTests|FullyQualifiedName~BalanceHarnessPracticalSearchTests|FullyQualifiedName~BalanceHarnessTowerBossStudy'
./build/run-tests.ps1 -ArtifactsPath TestResults/allocation-comparison-build-20260917 -Filter 'FullyQualifiedName~BalanceHarnessAllocationComparisonTests'
```

The initial sandboxed build could not read the user NuGet configuration; the approved wrapper invocation resolved that access issue. The build passed with nine existing warnings. A native allocation-shape preflight returned `ReadyNoReservation`, validating captured content, both supplied recipes and all 497,371 prior exclusions without allocation or combat. The producing harness hash matched its tested build. No required command remains blocked.

Final code review also corrected an exact-threshold floating-point edge: the five-point requirement now compares net paired wins to the integer threshold 150, so differences of 100, 25 and 25 wins cannot round below the endpoint. The added boundary fixture passes. [Reassessment by the corrected code](../TestResults/allocation-comparison-20260917/boundary-fix-reassessment.json) matches the saved result hash exactly, without combat. The producing executable and pre-correction source remain retained, and no experiment was rerun.

The [owned execution script](analysis/practical-allocation-comparison.py), [independent saved-evidence audit](analysis/audit-allocation-comparison.py), this review, the proposal/implementation checkpoint and the two harness usage guides complete the scoped changes. No migration, application configuration change or deployment is required.
