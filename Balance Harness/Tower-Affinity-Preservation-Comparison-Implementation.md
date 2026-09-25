# Comparing original and preserving affinity proposals

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

The separately versioned comparison is implemented. It compares the original v3 affinity proposer with preserving v4 while holding racing, benchmark-validation selection, reference retention and search budgets fixed. The [concrete development-context plan](Tower-Affinity-Preservation-Comparison-Plan.json) passes native design validation and remains **not admitted**. No actual combat, production entropy draws, production reservations or policy promotions are part of this implementation step.

## Frozen comparison

`tower-affinity-preservation-comparison-v1` requires the exact `BenchmarkAffinityCreation(ids)` control and `BenchmarkPreservingAffinityCreation(ids)` candidate, with identical selected affinity IDs and inventory. Control runs under `tower-proposal-racing-v5`; candidate runs under the new `tower-proposal-racing-v6`. Both use the unchanged `tower-racing-benchmark-validation-v1` selector. Earlier racing contracts still reject preserving v4; defaults remain unchanged.

| Fixed item | Both arms |
| --- | --- |
| Proposal roots | 12 paired roots; no screening or replacement |
| Generated candidates | 9 first-wave and 8 second-wave unique recipes |
| Racing panels | Four shared panels of 8 values |
| Nomination | Same 16 values, same order |
| Benchmark validation | Same 60 values, disjoint from training and held-out values |
| Search charge | 528 fights per arm per root, including shared requests |
| Independent evaluation | 256 fresh held-out values per root |
| Maximum campaign | 21,888 fights; 10,800 seconds; 6,442,450,944 bytes |

Each paired search uses 109 unique values: one proposal root, 32 racing values, 16 nomination values and 60 validation values. With twelve separate 256-value held-out panels, the design requires **4,380 fresh values**. The existing owner retains the full 16,384-value exposed entropy block, including the unused tail, and forbids refills.

Both arms pass native preflight before either spends its search budget. All 24 search outputs must complete and freeze before any held-out observation. An incomplete generation or search invalidates the study; it is never discarded or replaced. Held-out evaluation deduplicates identical physical recipes within a root while retaining absolute benchmark results and the existing references. Shared physical search requests must agree in recipe and outcome across arms, while proposal pools, beams and nominees may differ.

Before allocation, plan recreation counts the distinct legal preserving edits from the fixed benchmark parent and requires at least seventeen. This check uses the legal neighborhood, without drawing prospective roots. It does not guarantee that every random stream will fill both waves within the existing attempt limit. Exhaustion remains a recorded failure.

The earlier benchmark-validation pilot's endpoints and decision rules are retained: equal-root held-out candidate-minus-control gain, a candidate-minus-fixed-benchmark guardrail, all twelve root contrasts and descriptive uncertainty. There is no novelty requirement for proceeding. The inherited thresholds and resource ceilings are design choices, not demonstrated power or efficacy. Outcomes can justify a larger fresh evaluation, abandonment or an inconclusive result; they cannot adopt a policy.

## Execution and audit

The existing owned study runner accepts the new comparison version. It retains admission pinning, process ownership, registry and output leases, reservation accounting, the global output barrier, native reconstruction, independent Python audit and publication verification. Results now include `controlValidation` as well as candidate `validation`, so both arms expose all twelve gate decisions and pass/fallback counts. The existing result retains candidate novel-output frequency and each arm's selected party per root; control novelty can be derived by comparing those selections with the frozen references. The additional diagnostics field is omitted for earlier contracts.

Native reconstruction regenerates the proposal streams and authenticates the exact protected endpoint sets and legal edit counts against the captured inventory. The Python auditor independently checks the comparison contract, value layout, evidence, paired outcomes, validation arithmetic, held-out results, decisions and accounting. It also checks preservation metadata structure, endpoint/removal consistency and count bounds; exact legal-set reconstruction remains the native auditor's responsibility. Rehashing a modified archive does not bypass either audit's semantic checks.

The plan was created from the retained [preservation preview request](../TestResults/affinity-creation-preservation-preview-20260924/root-01/request.json), preserving its development inventory, benchmark parent and captured scope. Its canonical native plan hash is `8a961c57bdefb329222bf238ffdf6880d30cb44059e9633a5cf41ce059ce5c9d`. This context contains an earlier captured runtime identity. It is a concrete design artifact, not a qualified current-runtime admission package.

```powershell
dotnet <BalanceHarness.dll> tower-affinity-preservation-comparison-plan <preserving-export-request.json> <new-plan.json>
dotnet <BalanceHarness.dll> tower-proposal-comparison-check <plan.json>
```

## Changed files

- [TowerAffinityPreservationComparison.cs](../LL/tools/BalanceHarness/TowerAffinityPreservationComparison.cs): new fixed design, feasibility check, shared-panel binding and paired observation validation.
- [TowerProposalComparison.cs](../LL/tools/BalanceHarness/TowerProposalComparison.cs), [TowerBenchmarkValidationComparison.cs](../LL/tools/BalanceHarness/TowerBenchmarkValidationComparison.cs) and [Program.cs](../LL/tools/BalanceHarness/Program.cs): version dispatch, allocation count and planning command.
- [TowerProposalPolicies.cs](../LL/tools/BalanceHarness/TowerProposalPolicies.cs), [TowerBenchmarkValidation.cs](../LL/tools/BalanceHarness/TowerBenchmarkValidation.cs) and [TowerProposalRacingNative.cs](../LL/tools/BalanceHarness/TowerProposalRacingNative.cs): v6 admission to the existing validation kernel and evidence format.
- [TowerProposalStudy.cs](../LL/tools/BalanceHarness/TowerProposalStudy.cs): complete-study checks and diagnostics for both gates.
- [run-proposal-affinity-study.py](../build/run-proposal-affinity-study.py) and [audit-proposal-affinity-study.py](analysis/audit-proposal-affinity-study.py): owned execution and independent verification of the new version.
- [BalanceHarnessAffinityPreservationComparisonTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityPreservationComparisonTests.cs), [BalanceHarnessProposalStudyTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessProposalStudyTests.cs), [test-proposal-affinity-study.py](../build/test-proposal-affinity-study.py) and [test-proposal-affinity-study-owned.py](../build/test-proposal-affinity-study-owned.py): design rejection, generation, gate, barrier, archive and owned-process fixtures.
- This report, the plan, nine current-status lines and retained engineering receipts. Historical scientific archives and document bodies remain unchanged.

## Verification

**167 backend tests passed, including 18 new comparison cases**, through `build/run-tests.ps1` with `-ArtifactsPath TestResults/affinity-preservation-comparison-build-20260924`. The filter selected `BalanceHarnessAffinityPreservationComparisonTests`, `BalanceHarnessAffinityPreservationTests`, `BalanceHarnessProposalPolicyTests`, `BalanceHarnessAffinityCreationTests`, `BalanceHarnessAffinityCreationNativeTests`, `BalanceHarnessBenchmarkValidationTests`, `BalanceHarnessBenchmarkValidationNativeTests`, `BalanceHarnessBenchmarkValidationStudyTests` and `BalanceHarnessBenchmarkTieSelectionTests`. Build completed with zero errors and 43 pre-existing warnings. The [TRX and verification receipts](../TestResults/affinity-preservation-comparison-verification-20260924/closeout.json) retain results and pins.

The exported twelve-root native fixture includes both gates with six passes and six fallbacks each. Its accepted proposal positions differ across arms, but its deliberately literal outcomes produce identical final outputs, `NoObservedOutputDifferentiation`, 12,672 charged search reports and 4,608 held-out reports. It exercises every root and the global barrier; it is not an effectiveness estimate. Additional cases reject changed design fields, unpaired or reused values, an insufficient legal neighborhood, old/new version relabeling, rehashed protection metadata and inconsistent shared observations. Failed second-arm preflight dispatches neither arm; an incomplete pair cannot reach held-out evaluation.

`python -B -X utf8 build/test-proposal-affinity-study.py -v` ran 21 tests with 1 fixture-class skip. Rerunning with `--fixture TestResults/affinity-preservation-comparison-literal-20260924` ran 37 tests with 0 skips and passed all applicable cases. These cover independent arithmetic, Windows Job cleanup, literal archives and resealed semantic tampering. The updated auditor also successfully verified the retained published v5 benchmark-validation fixture, preserving legacy interpretation.

`python -B -X utf8 build/test-proposal-affinity-study-owned.py` passed both complete and `--mode attempt-failure` runs using the new fixture-host build, the exported literal source, separate synthetic output directories and `--resource-envelope tower-proposal-resource-envelope-v2`. The [complete owned fixture](../TestResults/tower-proposal-owned-fixture-affinity-preservation-20260924/verification.json) verified 17,280 literal reports through native worker, native reconstruction, independent Python audit, publication barrier and post-publication native verification. The [failure fixture](../TestResults/tower-proposal-owned-fixture-affinity-preservation-failure-20260924/verification.json) retained one started attempt and the full 16,384-value synthetic exposed block, with 4,380 selected, and published no completed result. These fixtures use isolated test registries and literal entropy, not production admission or allocation.

Complete owned archive manifest SHA-256: `e8025ce3cbec2339f4c89d5b951c6fb2b2b73fb283a123fc6798e3eb1e4752f0`. Closeout SHA-256: `6d5d44bb91f20f279bbc8be2b76ca53c3a3b01a28a8daf7a82737116421e0cad`. No required verification remains blocked. The initial sandbox limitation reading NuGet configuration was resolved by an authorized build retry. The prior 45 handoff members were authenticated before editing; 41 historical pins and nine historical document bodies were checked afterward.

## Accounting and next step

Builds, literal fixtures, archive checks and plan generation are engineering verification. Literal reports do not count as new combat evidence. The scientific accounting from the preceding preview is unchanged: recorded cumulative work **33,534.953 seconds /27,153,095,523 bytes**, declared cumulative maxima **76,980 seconds /48,469,377,024 bytes**. The last complete history scan remains 743,892 values across 256 files and was not repeated here. The old recognition diagnostic remains `CompleteDiagnosticOnly`; the old benchmark-validation pilot remains `Inconclusive`.

Next, qualify the current captured runtime and resource envelope, bind the qualified context, refresh complete history and prepare a pinned admission package before allocating any production values. A runtime-identity change requires recreating the context-bound plan and recording its new hash, while retaining the fixed comparison design. The first comparison should retain the same affinity inventory and benchmark parent. These implementation fixtures do not establish improvement from preservation.

No migrations, application configuration changes, deployments or default-policy changes are required. Unrelated working-tree changes were preserved.
