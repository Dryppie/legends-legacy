# Same-group variation implemented; verification stopped on fixture setup

15 September 2026. **ImplementedVerificationIncomplete.** The new opt-in `independent-group-variation-v1` / `group-variation-joint` route compiles. The one allowed test invocation completed **63 cases: 56 passed, 7 failed, zero skipped**. All **48 existing composition/joined/group-count cases passed**, including exact prior-policy output. Seven new cases failed because their shared synthetic fixture supplied only one base core, yielding an empty joined catalogue. The fixture is corrected in the checkout, **but that correction is untested**. The captured-content driver and independent recount were not run. **Zero fights, replays, preparations or fresh values; all 482,461 reservations preserved. Adoption Hold.**

## Implementation and tradeoff

The [completed trajectory diagnosis](Tower-Group-Count-Trajectory-V2-Review.md) found that different groups were tried at different counts, with little same-group variation. The new route spends existing fresh requests in bundles. For ten characters, one selected content-derived group receives counts **1, 5, 10, then 5 with a second filler draw**. Count anchors are deduplicated for one/two-character fixtures. Each later catalogue sweep rotates counts; every eighth fresh request remains uniform. Rejected/duplicate requests consume ordinals, and each arm starts a new schedule. No candidate or attempt budget increases.

Within a bundle, placement uses one derived stream, so reserved targets are intended to be nested across counts and identical for the two middle-count filler draws. Baseline count variants share a filler-stream label; the last variant changes it. Both streams derive from the existing generation label, group and visit, without measured fitness, controls or named Essence rules. Different reserved counts can still change filler-stream consumption. Filler draws can duplicate or reject; there is no repair/resampling loop or guaranteed causal isolation.

The current policy remains available unchanged. New variation trace fields are omitted for historical policies. The new route reuses existing atomic reservation, coverage/filler, exact-count drift rejection and the proposal/candidate charging path. No rank, nomination, combat, accounting, cancellation or archive-verification implementation was changed. This is an opt-in construction change, not adoption or a demonstrated strength improvement. Its main tradeoff is fewer distinct groups per shallow run; the four-request bundle would spend 17 guided fresh requests on five groups instead of 17. Captured-content coverage remains unmeasured because its diagnostic was not started.

## Test failure and correction

The new fixture originally used `Cores = [Core("e00", "e01")]`. `TowerJoinedMechanics.Create` builds strict unions of **two distinct overlapping base cores**; a single core produces zero joined groups. The affected tests therefore entered `empty-catalogue-uniform`, with zero requested group owners and null variation metadata. This explains the observed zero/null values and why the ownership fixture returned a uniformly filled party instead of a failed group reservation. This is a test setup defect I introduced, not evidence from a combat run.

Failed cases:

- `Uniform_positions_and_empty_catalogues_remain_usable`
- `One_group_receives_three_counts_and_a_second_middle_count_filler_draw`
- `Ownership_failure_rolls_back_the_complete_reservation`
- `Accidental_count_drift_remains_a_rejection`
- `Cancellation_keeps_the_charged_request_and_checkpoint`
- `Complete_variants_keep_nested_targets_and_change_fillers_at_fixed_count`
- `Each_arm_starts_its_own_variant_schedule`

The correction supplies `Core("e00", "e01")` and `Core("e00", "e02")`, then asserts that the catalogue contains exactly one group before returning the fixture mechanics. It is applied only to the current repository test source and saved separately in [fixture-correction.patch](../TestResults/balance/tower-group-variation-implementation-20260915/fixture-correction.patch). The failed copied test source, binaries, test logs and source freezes remain unchanged. [Correction hashes](../TestResults/balance/tower-group-variation-implementation-20260915/fixture-correction.json) distinguish the tested and corrected versions. No correction was compiled or tested again.

The eight passing new cases include schedule rotation/order boundaries and old group/count parity. Some other passing tests also used the empty catalogue, so their passing result **does not validate the intended guided route**. Do not treat 56 passes as completed new-policy verification. The seven failures share a clear setup problem, but further failures in the corrected fixture or implementation remain possible.

The [frozen protocol](Tower-Group-Variation-Implementation-Protocol.md) specifies zero retries and stopping dependent diagnostics on failure. Accordingly there was no test rerun, captured-content construction or independent recount. Final preservation and failure publication still ran. No tool approval blocked a command.

## Measured verification

| Check | Result |
|---|---|
| Reference build | Passed |
| Candidate harness build | Passed |
| Isolated test build | Passed; one existing xUnit2031 style warning |
| Compilation wall time, separately measured | 6.859 seconds |
| Old group/count reference | 128 synthetic evaluations / 171 proposals |
| Candidate old group/count full-result parity | Exact `b91e655af93f09f35a1d2d9a8373c3814eced62ce91aa4f21e1b102753aae554` |
| Existing composition / joined / group-count tests | 17 / 15 / 16 passed |
| New variation cases | 8 passed / 7 failed |
| Required `build/run-tests.ps1` invocation | Exit 1; 63 executed, 56 passed, 7 failed, 0 skipped; 4.766 seconds |
| Captured 1,956-position schedule and 19 constructions | Not run after test failure |
| Independent content recount | Not run |
| Historical evidence verified before/after | 5,928 indexed files across 15 sealed packages |
| Additional diagnostic time before publication | 7.969 / 300 seconds |
| Cumulative diagnostic time before publication | 262.410 / 1,800 seconds |
| Fights / new seeds / retries | 0 / 0 / 0 |

The failing test invocation's time is charged once, not dropped or double-counted from its result/failure receipts. Final publication time and output totals are in [completion.json](../TestResults/balance/tower-group-variation-implementation-20260915/completion.json). The new-output limit is 512 MiB within the existing 4-GiB allowance. These timings measure engineering fixtures, not v19 throughput or combat improvement.

## Commands and evidence

The isolated builds use copied harness/test source with captured gameplay dependencies from the completed group/count preparation. The shared pre-existing TRX was preserved before the test wrapper wrote its result. Exact commands and limits are stored in per-phase started/result/failure receipts. Executed once from the repository root:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-group-variation-implementation-20260915'
& $py -B "$w/freeze.py"
& $py -B "$w/workflow.py" bootstrap
& $py -B "$w/workflow.py" reference-build
& $py -B "$w/workflow.py" reference
& $py -B "$w/workflow.py" candidate-build
& $py -B "$w/workflow.py" tests-build
& $py -B "$w/workflow.py" tests  # exit 1; preserved, no retry
& $py -B "$w/workflow.py" preserve
& $py -B "$w/freeze-publication.py"
& $py -B "$w/publish.py"
```

The tests phase invoked:

```powershell
./build/run-tests.ps1 -NoBuild `
  -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests|FullyQualifiedName~BalanceHarnessGroupCountTests|FullyQualifiedName~BalanceHarnessGroupVariationTests' `
  -ArtifactsPath 'TestResults/balance/tower-group-variation-implementation-20260915/tests'
```

These are archived single-use commands, not permission to overwrite or rerun this package. [TRX](../TestResults/balance/tower-group-variation-implementation-20260915/tests.trx), [test log](../TestResults/balance/tower-group-variation-implementation-20260915/tests.log), [all outcomes](../TestResults/balance/tower-group-variation-implementation-20260915/test-summary.json), [failure receipt](../TestResults/balance/tower-group-variation-implementation-20260915/tests-failure.json), [exact implementation patch against the starting checkout](../TestResults/balance/tower-group-variation-implementation-20260915/implementation.patch), [preservation receipt](../TestResults/balance/tower-group-variation-implementation-20260915/preservation-result.json), [file seal](../TestResults/balance/tower-group-variation-implementation-20260915/files.json).

## Changed files and remaining work

New harness source: `TowerGroupVariationSearch.cs`. Integration changes: `TowerGroupCountSearch.cs`, `TowerCompositionSearch.cs`, `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerBossDiscoveryContract.cs`, `TowerPartyCoverage.cs`, and `TowerLoadoutComposition.cs`. New tests: `BalanceHarnessGroupVariationTests.cs`, including the explicitly untested fixture correction. The new protocol/review/evidence package and four active Markdown handoffs record this stopped verification. The exact patch isolates this work from the dirty checkout; unrelated source remains intact.

Next complete one separately frozen verification pass using the corrected fixture, retaining the same zero-combat scope and prior failure/time/output charges. First establish real guided-route tests, then run the already specified schedule/construction driver and independent recount. Until that succeeds, do not launch a balance comparison or describe the new policy as verified. No larger fight or seed budget is needed for that work.

The full backend suite and combat/archive execution were not run. No gameplay/content changes, Kharad tuning, ability-order optimization, configuration changes, migrations or deployment. No old caps increased, sealed v19 changed or 129,536-fight confirmation started. All 482,461 reservations, including v19's unused 512 confirmation values, remain preserved. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.
