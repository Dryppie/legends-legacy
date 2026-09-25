# Adaptive racing comparison implementation

This implements the prospective development pilot proposed in the [independent assessment](Tower-Search-Algorithm-Independent-Assessment.md), following the [adaptive search implementation](Tower-Adaptive-Racing-Implementation.md). It is offline Balance Harness work. No campaign, live-history allocation, team adoption or gameplay change was performed.

The protocol is `tower-adaptive-racing-comparison-v1`. Its [machine-readable plan](Tower-Adaptive-Racing-Comparison-Plan.json) is pinned by SHA-256 in the native and independent Python verifiers. The new plan uses LF bytes across checkouts. Earlier comparison versions retain their schedules, resource charges, selectors and serialized result fields.

## Frozen comparison

The pilot has twelve paired construction roots. B is the unchanged three-reference practical baseline, with 46 teams on eight discovery seeds and five nominees on 32 selection seeds. N is `tower-adaptive-beam-racing-v1`, with the five panel shapes 12×8, 7×8, 15×8, 7×8 and 5×40. Each search spends exactly 528 fights. Both preserve `399bc776…` as the positive-tie primary. The supplied `96b94357…` is the adaptive development benchmark R*.

Each root receives 113 distinct allocated values: one shared construction root, 40 baseline combat seeds, then 72 adaptive combat seeds. The two policies share no combat observations or panels. Sharing a construction seed does not imply equivalent proposal sequences. All 1,356 construction/search values precede twelve disjoint 256-seed held-out panels: 4,428 assigned values in total.

The existing single 16,384-word entropy batch and complete-history reservation protocol are reused. Every fresh exposed value, including the unused tail, remains reserved. There is no refill, retry, replacement root, transfer of unused allowance or resume. History collisions are excluded before assignment. Unresolved reservation failures remain visible to the registry.

All 24 selected outputs and all twelve physical evaluation unions must freeze before the first held-out fight. Each union contains the two outputs plus all three references, deduplicated by physical recipe. Equal outputs share rows and have exactly zero observed method contrast, while their absolute benchmark outcomes remain measurable. Search costs 12,672 fights; evaluation costs 9,216–15,360; the total ceiling is 28,032.

## Ownership and evidence

The existing `build/run-reference-exploration-comparison.py` launcher accepts the new version. It keeps Windows Job ownership, a live parent owner, exclusive output/registry leases, captured content/runtime checks, durable attempt accounting, no cache reuse, native reconstruction, independent Python verification and publication only after the process trees have exited.

The new version declares a separate **10,800-second / 6 GiB launch allowance**, with **10,680 seconds / 5.75 GiB** for native execution and closeout; both audits and publication share the remaining enclosing allowance. It carries no historical comparison charge (`priorSeconds: 0`, `priorBytes: 0`). These are software ceilings, not measured runtime/storage estimates or a claim that the pilot will fit. Launcher admission and history checks are inside this allowance. An external preparatory admission task, if needed, must have its own explicit scope and accounting; it is not silently treated as historical work under this request.

The implementation step did not include an executable request with machine-specific paths or current history pins. The subsequent [seed-free admission](Tower-Adaptive-Racing-Comparison-Admission.md) now retains that request, the captured runtime and the complete checked history. The unscheduled template retains the captured scope and has `maximumBattles: 1552` (the legacy single-arm shape: 528 search plus four 256-sample confirmation members); the owned comparison uses the separate 28,032 physical ceiling and deduplicated union.

Existing command names remain the public runner interface:

```text
BalanceHarness tower-reference-exploration-comparison-check <request.json>
python build/run-reference-exploration-comparison.py --request <request.json> --harness <retained BalanceHarness.dll>
BalanceHarness tower-reference-exploration-comparison-verify <completed archive>
```

These are usage examples, not commands executed for this implementation. The read-only check validates the new mechanics and panel shape without drawing entropy or fighting. Direct native scientific runs still require the owning Windows Job launcher. The standalone `tower-adaptive-racing-check` and `-verify` commands retain their earlier single-search meaning.

The comparison uses one global native archive. Each candidate arm saves its exact plan, two generated batches, five panel freezes and terminal adaptive report. Batch and panel files are flushed before dependent evaluation; the existing global attempt journal records starts before dispatch and completions only after validated returned evidence. Local adaptive ordinals map to contiguous global archive trial IDs. This adapter uses the same adaptive kernel and report authenticator as the standalone archive integration, with a global trial offset. It does not create 12 separate owners or copy captured content per search.

The comparison requests callbacks only at panel freezes, avoiding a copy of the growing report after every observation. Its global attempt journal still accounts for every dispatch. Standalone kernel and archive callers keep their original per-observation checkpoints.

Native verification reruns both search trajectories from saved battle rows, reconstructs the adaptive batch/beam/nomination decisions, reproduces prepared input hashes and arm cache keys, and matches the saved artifacts. It checks exact recipe/battle membership, scope, producing runtime, reservations, global output freeze and attempt sequence. Verification never runs combat.

The Python auditor independently recounts baseline ranking, adaptive panels and scores, competitive diversity pruning, common-rung beams, five nominees, incumbent-tie selection, held-out unions and all result arithmetic from saved battle reports. Proposal generation and full prepared-input reconstruction are the native verifier's responsibility. Both checks are required; an outer manifest alone cannot conceal semantic tampering.

## Results and decision rules

Every root reports N−B and N−R*, wins, paired gains/losses, selected identities, all reference rates, novelty and physical recipe count. The `pilot` result adds mean/median/worst absolute gain, counts at or beyond ±3/±5 percentage points, promising novel outputs, paired seed standard errors and the covariance of the two contrasts. Repeated references use shared observations, rather than fabricated independent rows.

Both effects have a two-sided 95% paired root t interval with eleven degrees of freedom, explicitly labeled a small-sample approximation. Separate one-sided 95% conditional Hoeffding lower bounds use all 3,072 evaluation positions plus the finite-population depletion correction `(3072−1)/(2^32−historicalCount−1356)`. They concern these frozen outputs, not future-root variability; they are not simultaneous 95% bounds. Point classifications are descriptive, not qualification probabilities. No individual-fight bootstrap treats roots as interchangeable observations.

Development gates are fixed before allocation:

- `LargerFreshEvaluationWarranted`: mean N−B is at least two points, mean N−R* is nonnegative, and at least three roots select novel recipes at least three observed points above R*.
- `AbandonThisConfiguration`: mean N−B is at most minus two points, or mean N−R* is at most minus two points with fewer than three promising novel outputs. The latter makes the assessment's “materially below” criterion explicit.
- `InconclusiveRetainBaselineAndBenchmark`: all other complete outcomes.

Every result says `DevelopmentCriteriaOnlyNoPolicyPromotionOrTeamAdoption`. Failure preserves evidence and blocks an efficacy result; failed roots are never replaced. A successful development gate calls for a new prespecified fresh study. It cannot promote this policy or adopt a team.

Operator opportunities, rejections, accepted unique recipes, parent IDs, distances, cutoff ties and survival through each rung remain in the retained adaptive reports. They are diagnostics, not alternative strength endpoints. Resource receipts report enclosing/native elapsed time and observed bytes; audit process receipts retain audit elapsed time. This change does not add a combat-only timing benchmark or estimate production throughput.

## Changed files and verification

- `TowerAdaptiveRacingComparison.cs`: new schedule binding, adaptive/global-archive adapter and benchmark-aware pilot metrics.
- `TowerReferenceExplorationComparison.cs`, `TowerReferenceExplorationReservation.cs`, `TowerReferenceExplorationComparisonArchive.cs`, `TowerReferenceExplorationComparisonRun.cs`: version dispatch and resource/count validation in the existing owner and archive workflow.
- `TowerAdaptiveRacingNative.cs`: optional global trial offset in the shared report authenticator; standalone semantics remain unchanged.
- `TowerAdaptiveRacing.cs` and `TowerBatchRacing.cs`: internal freeze-only callback option for the comparison, preserving the existing public checkpoint behavior.
- `build/run-reference-exploration-comparison.py` and `analysis/audit-reference-exploration-comparison.py`: new-version launch envelope and independent reconstruction/arithmetic.
- `BalanceHarnessAdaptiveComparisonTests.cs`, two existing comparison test files, and `build/test-adaptive-racing-comparison.py`: deterministic fixtures, failure boundaries, compatibility and tamper verification.
- This document, the frozen plan, the preceding implementation document and `.gitattributes`: protocol specification, status and exact plan-byte preservation.

Verification uses `build/run-tests.ps1` and literal battle reports guarded against combat. The exported synthetic archive is engineering evidence only. It does not estimate search strength.

**180 backend tests passed, zero failed or skipped:** twelve new pilot cases, 39 adaptive search/archive cases, 47 frozen-kernel cases, and 82 earlier comparison cases. The build succeeded with zero errors; its 41 warnings were in existing code and tests. See the [backend log](../TestResults/adaptive-comparison-final-tests-20260923.log) and [TRX receipt](../TestResults/adaptive-comparison-final-tests-20260923.trx).

**Seven new Python tests passed**, including the complete 24,960-row synthetic archive, native/independent arithmetic agreement, resealed semantic tampering, capture/history/resource authentication, absolute regression when the methods agree, and owned launch accounting. See the [Python pilot log](../TestResults/adaptive-comparison-python-final-20260923.log).

**41 Python compatibility tests also passed**, including independent recounts and complete working-envelope audits of retained baseline, offset and screened literal archives. Together, the Python suites passed 48 tests with no failures or skips. See the [compatibility log](../TestResults/adaptive-comparison-python-compatibility-20260923.log). No required verification command remains blocked.

The commands were:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAdaptiveComparisonTests|FullyQualifiedName~BalanceHarnessAdaptiveRacingTests|FullyQualifiedName~BalanceHarnessBatchRacingTests|FullyQualifiedName~BalanceHarnessReferenceExplorationComparisonTests|FullyQualifiedName~BalanceHarnessReferenceExplorationOffsetComparisonTests|FullyQualifiedName~BalanceHarnessReferenceExplorationScreeningComparisonTests' -ArtifactsPath 'TestResults/adaptive-comparison-final-build-20260923'
python -B build/test-adaptive-racing-comparison.py --fixture TestResults/adaptive-comparison-final-fixture-20260923
python -B build/test-reference-exploration-comparison.py --fixture TestResults/practical-fresh-screening-implementation-20260922/fixtures/tower-reference-exploration-comparison-v1 --offset-fixture TestResults/practical-fresh-screening-implementation-20260922/fixtures/tower-reference-exploration-offset-comparison-v1 --screening-fixture TestResults/practical-fresh-screening-implementation-20260922/fixtures/tower-practical-fresh-screening-comparison-v1
```

For the backend run, `BALANCE_HARNESS_ADAPTIVE_FIXTURE_EXPORT` pointed at the new fixture output path; ordinary test runs can omit it. Export requires a previously nonexistent directory. `python` above denotes the available Python 3 interpreter; verification used the bundled interpreter's absolute path because Python was absent from `PATH`. Backend restore used permission-enabled access to the existing NuGet configuration. Whitespace, Python syntax and local document links passed.

No database migration, application configuration change or deployment is required. The pending fixed-family confirmation and its allowance remain separate. Prospective admission and current-history validation were subsequently completed in the [admission step](Tower-Adaptive-Racing-Comparison-Admission.md). The later authorized [single pilot execution](Tower-Adaptive-Racing-Comparison-Execution.md) stopped after 2,640 search fights because of a launcher storage-monitor race, before held-out evaluation. The monitor was repaired and tested; the failed campaign was not retried and provides no efficacy result.

A separately declared [second pilot](Tower-Adaptive-Racing-Pilot-02.md) subsequently completed 23,680 fights with fresh values and passed both scientific audits. Its prespecified decision was `AbandonThisConfiguration`: N averaged 0.944 percentage points below B and 4.199 points below the strongest reference, with zero promising novel outputs. The implementation remains available for diagnosis; this outcome does not support promotion or a larger evaluation of the unchanged configuration. The first failed study remains preserved and excluded from efficacy estimates.
