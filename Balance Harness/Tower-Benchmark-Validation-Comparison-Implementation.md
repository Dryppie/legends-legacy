# Benchmark validation: prospective comparison and study implementation

Current status (2026-09-25): Original affinity creation with benchmark validation is the supported baseline. The one nomination experiment completed 15,744 actual fights and all audits; both arms retained the benchmark on all 12 roots. This tuning cycle is closed. See [completed result and next work](<Tower-Affinity-Nomination-Pilot-01-Execution.md>).

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The separately versioned v4-versus-v5 study path is implemented, with a prospective plan and synthetic qualification.** The [saved JSON design](Tower-Benchmark-Validation-Comparison-Plan.json) is `tower-benchmark-validation-comparison-v1`. It is planned, not admitted. No production input preparation, entropy allocation, combat or scientific launch occurred. The preceding pilot remains `AbandonThisConfiguration`.

This follows the [v5 native qualification](Tower-Benchmark-Validation-Native-Implementation.md). The comparison tests the complete fixed-budget final-stage change: shorter nomination plus independent benchmark validation. It does not isolate the gate from the reduced nomination sample size, and it does not test alternative splits or thresholds against prior held-out results.

## Frozen design

Both arms use the existing affinity-creation generator, the same three selected affinity IDs, fixed benchmark and captured physical context. The control uses `tower-proposal-racing-v4` with benchmark preference on positive ties. The candidate uses `tower-proposal-racing-v5` and the previously fixed exact validation gate. Other references remain eligible challengers; the candidate is not required to select a novel recipe.

There are 12 retained roots. Every search arm is charged for all 528 observations. Each pair shares its proposal root and all 32 values in the first four racing panels. It must produce the same proposals, pruning decisions, beam and five nominees. The candidate's 16 nomination values are the first 16 of the control's 40 selection values, in frozen order. Shared physical observations must agree even though both arms are separately charged. No control result is supplied to candidate generation or nomination.

The candidate's 60 validation values are disjoint from the entire control selection panel. This prevents the control's additional 24 selection values from being reused as validation evidence. All eventual held-out values are disjoint from both arms and every other root.

| Values within a root pair | Relative positions, zero-based | Count |
| --- | --- | ---: |
| Shared proposal root | 0 | 1 |
| Shared two-wave racing panels | 1–32 | 32 |
| Control selection | 33–72 | 40 |
| Candidate nomination, shared with control | 33–48 | 16, already counted |
| Candidate validation | 73–132 | 60 |
| Distinct search values per pair | | **133** |
| Separate held-out panel per root | After all 12 search blocks | **256** |

The allocation therefore requires **12 × (133 + 256) = 4,668 fresh values**. Control and candidate separately use 73 and 109 values, including their shared proposal root; neither number is the size of the pair's union. The held-out region starts at offset 1,596. Old 3,948-value allocations fail the new contract. A single fixed batch of 16,384 entropy words is classified without refill; every exposed fresh value, including the unused tail, remains permanently excluded.

Before either arm runs, both plans must pass native preflight. After all 24 searches complete, the existing durable all-root barrier freezes selected physical recipes for control, candidate and benchmark. Held-out evaluation uses 256 common values per root, deduplicating identical physical recipes within that root. There can now be one, two or three physical outputs per root; equality never removes the root from analysis. Any incomplete root, failed validation panel or failed audit prevents a complete result. No replacement, retry, extension or refill is permitted.

## Endpoints, diagnostics and resources

The primary endpoint remains the equal-root mean held-out candidate-minus-control win-rate difference. The guardrail is the equal-root candidate-minus-fixed-benchmark difference. All twelve root contrasts, paired-seed standard errors/covariance, descriptive root intervals, medians and worst roots are retained. They do not establish future-root efficacy.

The fixed decision order carries forward the selector study's prospective thresholds without novelty as a gate:

1. Incomplete evidence cannot produce a complete study result.
2. Method or benchmark mean at or below −2 percentage points means `AbandonThisConfiguration`.
3. Otherwise, zero differing roots means `NoObservedOutputDifferentiation`.
4. Method mean at least +2 points, benchmark mean at least zero, and at least three differing roots means `LargerFreshEvaluationWarranted`.
5. Other complete results are `Inconclusive`.

No decision authorizes adoption. Novel-root and promising-novel counts remain descriptive. A v5-only `validation` result field also retains all twelve exact gate decisions, passed-root count and fallback-root count. Thus fallback and novel-output frequencies can be reported alongside held-out effects and costs. Earlier result serialization omits this field.

Search costs remain **12,672 fights**. At most 9,216 held-out fights give the unchanged hard cap of **21,888 fights**, **10,800 seconds** and **6 GiB**. Deduplication reduces measured costs without authorizing extra evaluation. Resource-envelope versions remain separate from study identity; the owned fixtures qualify the existing v2 partition of 9,000 native seconds plus 1,800 audit seconds, and 5.5 GiB native plus 0.5 GiB audit storage.

The last recorded scientific charges remain **27,191.671 seconds /22,349,413,892 bytes**, under cumulative declared maxima of **55,020 seconds /35,769,024,512 bytes**. Charging the full proposed study allowance would yield **37,991.671 seconds /28,791,864,836 bytes**, leaving **17,028.329 seconds /6,977,159,676 bytes** before additional admission or review work. These are prospective arithmetic bounds, not a resource admission or a new charge. Final executable retention, measured feasibility and a separately declared admission allowance remain required.

## Implementation and compatibility

- [TowerBenchmarkValidationComparison.cs](../LL/tools/BalanceHarness/TowerBenchmarkValidationComparison.cs) contains the new design, fixed allocation, binding and paired-trajectory checks. [TowerProposalComparison.cs](../LL/tools/BalanceHarness/TowerProposalComparison.cs) dispatches explicitly by version. The optional `validationProtocol` field is rejected in old designs and omitted from their serialization.
- [TowerProposalStudy.cs](../LL/tools/BalanceHarness/TowerProposalStudy.cs) accepts the candidate's six-panel completion, preserves the all-root barrier and emits exact validation diagnostics. [TowerProposalStudyProtocol.cs](../LL/tools/BalanceHarness/TowerProposalStudyProtocol.cs) binds the version-specific allocation count through intent, reservation and reconstruction.
- [Program.cs](../LL/tools/BalanceHarness/Program.cs) adds a zero-combat planning command. The [owned launcher](../build/run-proposal-affinity-study.py) recognizes the new identity while preserving admission pinning, Windows Job ownership, leases, limits and failure retention.
- The [independent study auditor](analysis/audit-proposal-affinity-study.py) checks the new design, shared prefix, disjoint validation, all six candidate panels, exact gate, held-out binding and diagnostics. It remains a single retained script, with no unpinned runtime imports. Native reconstruction still verifies proposal RNG, typed content, prepared-input bindings and canonical .NET hashes.
- [New backend tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessBenchmarkValidationStudyTests.cs), [shared literal fixtures](../LL/tests/EssenceSystem.Tests/BalanceHarnessProposalStudyTests.cs), the [fixture host](../LL/tests/BalanceHarness.ProcessFixture/ProposalStudyFixtureHost.cs), [Python archive tests](../build/test-proposal-affinity-study.py) and [owned-process fixture](../build/test-proposal-affinity-study-owned.py) qualify the new path. Test entropy and literal reports are supplied only through the separate fixture boundary.

The planning commands are:

```powershell
dotnet BalanceHarness.dll tower-benchmark-validation-comparison-plan <creation-generation-request.json> <new-plan.json>
dotnet BalanceHarness.dll tower-proposal-comparison-check <plan.json>
```

The creation-generation request retains the existing two-to-four-policy format; the planner selects its single creation candidate and fixes that generator in both arms. The saved plan uses a pinned previously captured context, changing only `scope.executionHash` to the tested producing runtime. [Input provenance](../TestResults/benchmark-validation-study-verification-20260924/input-provenance.json), [plan creation](../TestResults/benchmark-validation-study-verification-20260924/plan-create.log) and [plan check](../TestResults/benchmark-validation-study-verification-20260924/plan-check.log) are retained. The complete live history was not rescanned for this engineering task.

## Next step

Create the separately versioned runtime/resource admission declaration and preparation path for this exact design and final executable. Refresh the full live-history exclusions, retain and pin all dependencies, and measure feasibility under a declared admission allowance before any scientific value allocation. Existing admission packages remain consumed and cannot admit this study. There are no migrations, application configuration changes, deployments or gameplay-default changes.

## Verification

**132 backend tests and 52 Python tests passed, with zero failures or skips in the final suites.** Backend coverage includes the eleven new comparison cases, all 32 v5 evaluator cases, the earlier study versions, native adapters, reservation/resource boundaries and runtime retention. Python coverage comprises 34 unit/archive cases, five resource cases and thirteen standalone v5 native-audit regression cases. Both implementations check all 1,891 valid gain/loss combinations, and archive tests reject altered selectors, gate decisions, challenger freezes, allocations, held-out order, creation metadata and accounting.

The full C# fixture executed the real study/controller and native persistence code around literal report callbacks, checked all 24 searches before any held-out measurement, reconstructed all twelve roots and exported compressed evidence. The owned success fixture then independently exercised the Windows Job worker, leases, fixed synthetic entropy reservation, both audits, publication barrier and verification after publication. It produced **17,280 literal reports**, six gate passes, six fallbacks, six differing outputs and `Inconclusive` with zero measured effect. These are engineered outcomes, not scientific evidence. The workflow took **649.140 seconds** and retained **627,373,478 bytes**, including preparation and verification after publication.

The separate owned failure fixture retained one charged attempt and all 16,384 exposed test values without a published result or closeout. It took **4.516 seconds** and retained **45,178,663 bytes**. Both fixtures recorded zero actual combat and zero production entropy draws. Their receipts are [success](../TestResults/tower-proposal-owned-fixture-benchmark-validation-20260924/verification.json) and [failure](../TestResults/tower-proposal-owned-fixture-benchmark-validation-failure-20260924/verification.json). The success closeout pin is `e12607a95e6144a86c43a882696227c259eb37a270570f53121437cab711d0eb`.

All backend tests ran through `build/run-tests.ps1`, with isolated artifacts at `.artifacts/benchmark-validation-study-20260924`. The final filter covered `BalanceHarnessBenchmarkValidationStudyTests`, `BalanceHarnessProposalStudyTests`, `BalanceHarnessAffinityCreationStudyTests`, `BalanceHarnessBenchmarkTieStudyTests`, `BalanceHarnessProposalResourceTests`, `BalanceHarnessProposalRuntimeRetentionTests`, `BalanceHarnessBenchmarkValidationTests` and `BalanceHarnessProposalNativeTests`. The first sandboxed build could not access the user NuGet configuration; the same wrapper succeeded with approved filesystem access. An initial command test supplied one policy instead of the existing two-policy minimum; its fixture was corrected, and the final full regression run passed. The final incremental build had zero errors and thirteen existing warnings. No required verification command remains blocked.

```powershell
python -B -X utf8 build/test-proposal-affinity-study.py --fixture TestResults/benchmark-validation-study-literal-fixture-20260924 -v
python -B -X utf8 build/test-proposal-resource-envelope.py -v
python -B -X utf8 "Balance Harness/analysis/test-benchmark-validation-audit.py" TestResults/benchmark-validation-native-verification-20260924/fixtures -v
python -B -X utf8 build/test-proposal-affinity-study-owned.py --fixture-host .artifacts/benchmark-validation-study-20260924/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll --source-fixture TestResults/benchmark-validation-study-literal-fixture-20260924 --output <fresh-tower-proposal-owned-fixture-directory> --resource-envelope tower-proposal-resource-envelope-v2
```

The last command also ran in a separate fresh directory with `--mode attempt-failure`. A read-only independent recount of the preserved v4 fixture passed. Evidence includes the [backend log](../TestResults/benchmark-validation-study-regression-20260924.log), [TRX](../TestResults/benchmark-validation-study-regression-20260924.trx), [Python archive log](../TestResults/benchmark-validation-study-python-archive-20260924.log) and [completion/preservation record](../TestResults/benchmark-validation-study-verification-20260924/completion.json). The initial failure logs remain available.

The prior native qualification manifest, thirteen historical scientific/admission pins and all consumed source records of the preceding saved-stage diagnosis were preserved. Only seven status banners changed in prior documents. No scientific manifest, old prospective plan, reservation registry or resource declaration was modified. The last published exclusion history remains 721,365 values across 252 files; this task did not rescan or rewrite it. Engineering fixtures do not change scientific resource charges.
