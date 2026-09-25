# Benchmark tie preference: prospective comparison and owned execution

Current status (2026-09-25): The twelve-root placement comparison completed 16,000 actual fights and all audits. Frozen decision: Inconclusive; no demonstrated improvement and no default promotion. See [completed comparison and diagnosis](<Tower-Loadout-Placement-Plain-Cost-Reconciliation.md>).

23 September 2026. Target: the offline `LL/tools/BalanceHarness`.

The [opt-in selector](Tower-Benchmark-Tie-Selection-Implementation.md) now has a separate prospective comparison contract, owned execution support and independent Python audit. The [saved plan](Tower-Benchmark-Tie-Comparison-Plan.json) is **planned and checked, not admitted**. Its canonical plan hash is `94e9cdaea2ef5ac61a0f60eb00390559ffd12c91dc34b0e1b0b55ea3811b8ffa`.

Both arms use the existing affinity-creation generator and the same roots and training panels. The candidate differs only by preferring the fixed benchmark when it shares the positive maximum selection score. The earlier creation pilot remains `Inconclusive`; this new contract neither changes that result nor satisfies its unmet larger-evaluation gate.

## Frozen contrast

| Property | Control | Candidate |
| --- | --- | --- |
| Comparison version | `tower-benchmark-tie-comparison-v1` | Same |
| Proposal policy | `benchmark-affinity-creation-v3` | Exact same policy and selected affinity IDs |
| Racing version | `tower-proposal-racing-v3` | `tower-proposal-racing-v4` |
| Final selection | Existing incumbent-tie rule | `tower-racing-benchmark-positive-tie-v1`, then the existing rule |
| Scope, benchmark, mechanics and inventory | Frozen | Identical |
| Root and training panels | Shared per root | Shared per root, separately charged |

The optional `selectionContrast` record binds both selector identities in the design. Legacy designs omit this record and retain their serialized shape. A selector plan with differing generators, missing or relabeled selectors, changed analysis or a different root count is rejected. Existing preservation and creation comparisons retain their own policies and selectors.

Both native execution and the Python auditor enforce identical proposal attempts, accepted recipes, training observations, pruning, common-panel ranking and nominees across selector arms. Only version/hash bindings, feedback panel hashes and the final selected output may differ. This check occurs before held-out evaluation; independently valid arms cannot silently turn into a generator comparison.

The benchmark and legacy primary retain their existing identities in the scope. In particular, changing the primary designation is not an implementation of this contrast. The benchmark override applies only at the positive maximum, preserving the inherited selector elsewhere.

## Prospective observations and decisions

The design retains twelve paired roots, 528 search observations per arm and 256 held-out observations per physical selected recipe per root. All 24 search outputs are durably frozen before any held-out observation. Identical physical recipes share one held-out measurement within that root. No root screening, replacement, refill, retry or incomplete-root exclusion is permitted.

The primary endpoint is the equal-root mean candidate-minus-control held-out win-rate difference. The guardrail is candidate-minus-fixed-benchmark performance. Every root, including identical-output roots, remains in both endpoints. Paired-seed standard errors and covariance describe the frozen outputs; descriptive root intervals do not establish future-root efficacy.

The existing effect thresholds are retained prospectively, with novelty removed as a selector decision gate:

1. Incomplete execution or failed verification prevents a complete result.
2. A method mean at or below −2 percentage points, or a benchmark mean at or below −2 points, yields `AbandonThisConfiguration`.
3. Otherwise, zero differing roots yields `NoObservedOutputDifferentiation`.
4. A method mean of at least +2 points, a nonnegative benchmark mean and at least three differing roots yields `LargerFreshEvaluationWarranted`.
5. All other complete results are `Inconclusive`.

No result authorizes adoption. Novel-output counts and the inherited +3-point promising-novel statistic remain descriptive. Their two decision-gate counts are explicitly zero in the new analysis contract and are not used by its decision function. Legacy studies retain their original novelty gates. None of these settings was fitted to a new pilot result or a subset of the old twelve roots.

The fixed search cost is 12,672 observations. A selector difference selects the benchmark, so at most two physical recipes need held-out measurement per root: at most 6,144 held-out observations and 18,816 total. The generic study's conservative hard cap remains 21,888, with 10,800 seconds and 6 GiB; this does not authorize extra panels or roots. The fixed allocation remains 3,948 fresh values. All exposed entropy values, including the unused tail, remain excluded.

## Execution and audit

The study version is carried through requests, input checks, allocation, intent, launch, pair binding, global freeze, worker receipt, both audits, publication and final closeout. Existing resource-envelope versions remain independent of the study version. The owned launcher still acquires leases, starts a suspended process in a hidden Windows Job, enforces cumulative limits and preserves failures without retry.

Native verification reconstructs the complete generated trajectories and verifies .NET canonical plan/request hashes, affinity derivation and saved battle evidence. Python independently validates the literal policy/selector contract, recounts all panel scores and selection, compares the two training trajectories, and verifies held-out membership, arithmetic, reservations and publication accounting. Python's limited canonical hash helper is not used for full plans containing .NET-specific encoding; full typed-plan hash verification remains native.

The public planning command is:

```powershell
dotnet BalanceHarness.dll tower-benchmark-tie-comparison-plan <creation-generation-request.json> <new-plan.json>
dotnet BalanceHarness.dll tower-proposal-comparison-check <plan.json>
```

The first command takes the single creation policy from the existing versioned generation request and freezes that same policy in both arms. It does not export proposals, allocate values or start a study. The saved plan was created from the previously captured physical context with only its producing execution identity updated to the tested build. [Input provenance](../TestResults/benchmark-tie-study-verification-20260923/input-provenance.json), [plan creation](../TestResults/benchmark-tie-study-verification-20260923/plan-create.log) and [plan check](../TestResults/benchmark-tie-study-verification-20260923/plan-check.log) are retained. The complete live history has not been rescanned for admission.

## Verification

The relevant backend suite passed **68/68 tests** through `build/run-tests.ps1`, including both legacy comparison versions, selector boundaries, native reconstruction and the global held-out barrier. The [retained TRX](../TestResults/benchmark-tie-study-tests-20260923.trx) and [build/test log](../TestResults/benchmark-tie-study-tests-20260923.log) identify the exact cases. Two new test-only nullability warnings in that build were then fixed with an explicit non-null assertion; the [ten affected contract cases passed again](../TestResults/benchmark-tie-study-nullability-tests-20260923.trx), with no remaining warnings in the new test file. The follow-up build had zero errors and thirteen warnings in other test files; the original build also reported warnings from dependency projects. Production harness and fixture-host hashes remained identical. All seven final C# source files match their producing portable symbols; [original](../TestResults/benchmark-tie-study-verification-20260923/compiled-source-files.json) and [final DLL/PDB bindings](../TestResults/benchmark-tie-study-verification-20260923/compiled-source-files-final.json) are retained, along with the original test DLL/PDB pair.

Python verification passed **57 cases**, with three inapplicable archive cases skipped: 16 unit/process, five resource-envelope, eleven preservation-archive, twelve creation-archive and thirteen selector-archive cases. Archive tests reject changed selector identities, substituted selected outputs and mismatched training evidence. An initial regression run exposed an invalid Python full-plan hash assertion caused by .NET encoding differences; that assertion was removed, native typed-plan checks retained, and all three final archive runs passed. Both initial failure logs and final results are retained.

The owned success fixture exercised preparation, allocation, search, the global barrier, held-out evaluation, native reconstruction, independent audit, publication and native verification after publication. It produced 18,816 literal reports, twelve differing outputs, identical proposals in both waves and `Inconclusive` with zero measured effect. This is an engineered test case, not evidence of selector performance. Its [receipt](../TestResults/tower-proposal-owned-fixture-benchmark-tie-20260923/verification.json) records 324.484 seconds and 657,284,364 retained bytes for the complete fixture workflow. The archive closeout records 297.218 charged seconds and 635,533,647 bytes; the workflow total also includes fixture preparation and verification after publication. A separate [audit of the published archive](../TestResults/benchmark-tie-study-verification-20260923/published-independent-audit.json) passed against closeout pin `3e500ab42003cd51793f25baea58486d691a98d1c06c626beb2a95648a7c1935`.

The [failure fixture](../TestResults/tower-proposal-owned-fixture-benchmark-tie-failure-20260923/verification.json) retained one charged attempt and all 16,384 exposed fixed test values without publishing a result or closeout. It took 4.718 seconds and retained 45,077,897 bytes. Both fixtures report zero actual combat and zero production entropy draws; neither qualifies production runtime or consumes a scientific admission.

Verification commands, artifact hashes, unchanged historical pins and source checks are retained in the [completion receipt](../TestResults/benchmark-tie-study-verification-20260923/completion.json) and [manifest](../TestResults/benchmark-tie-study-verification-20260923/files.json). The backend invocation was:

```powershell
$env:TOWER_SELECTOR_STUDY_FIXTURE_EXPORT = [IO.Path]::GetFullPath('TestResults/benchmark-tie-study-literal-fixture-20260923')
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessBenchmarkTieStudyTests|FullyQualifiedName~BalanceHarnessBenchmarkTieSelectionTests|FullyQualifiedName~BalanceHarnessProposalStudyTests|FullyQualifiedName~BalanceHarnessAffinityCreationStudyTests' -ArtifactsPath 'TestResults/benchmark-tie-study-build-20260923'
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessBenchmarkTieStudyTests&FullyQualifiedName!~Complete_selector_fixture' -ArtifactsPath 'TestResults/benchmark-tie-study-build-20260923'
```

Required verification completed. NuGet configuration access required approved elevated execution of the repository test wrapper; no verification command remains blocked. Scientific execution and a fresh history scan were outside this implementation verification.

## Changed files and next step

- `TowerProposalComparison.cs`, `TowerProposalStudy.cs` and `TowerProposalStudyProtocol.cs`: versioned contrast, binding, trajectory equality, decision rules and request reconstruction.
- `Program.cs`: zero-combat selector-plan command routing.
- `build/run-proposal-affinity-study.py` and `analysis/audit-proposal-affinity-study.py`: owned study identity and independent selector/trajectory verification.
- `BalanceHarnessBenchmarkTieStudyTests.cs`, `BalanceHarnessProposalStudyTests.cs` and `ProposalStudyFixtureHost.cs`: contract rejection, all-root replay, literal export and owned fixture support.
- `build/test-proposal-affinity-study.py` and `build/test-proposal-affinity-study-owned.py`: Python boundaries, admission mismatch, archive tampering and full owned lifecycle assertions.
- The prospective JSON plan, this report and current-status links in the harness guides and previous assessment.

Next, qualify the final executable and captured dependencies, add a separately versioned selector admission declaration/preparation path, measure resource feasibility and refresh complete live-history exclusions before allocating any scientific values. Existing admission packages remain consumed and cannot admit this build. There are no migrations, application configuration changes, deployments or gameplay-default changes.
