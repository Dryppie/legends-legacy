# Explicit incumbent preset for practical search

22 September 2026. Target: offline BalanceHarness. `tower-practical-search-incumbent-tie-preset` prepares a new practical-search template and request with an explicitly designated incumbent. It enables `tower-staged-incumbent-tie-v1` while preserving the incumbent generator, recipes, budgets, historical exclusions and runtime/content bindings. Existing defaults, old definitions and archive verification remain unchanged.

The [completed prospective comparison](Tower-Practical-Incumbent-Tie-Comparison-Execution.md) supports this optional selector for its captured scope and frozen outputs. This preset is a workflow integration step, not another scientific run or a broader quality claim. The completed experiment remains closed, with 538,326 permanent exclusions recorded at close.

## Usage

Start with a newly declared `tower-practical-allocated-search-v1` request pointing to an unscheduled incumbent template. Supply the exact reference ID from that template, including case. Either supplied team can be designated; the command never infers the primary from reference order, a party hash or historical results.

```powershell
dotnet BalanceHarness.dll tower-practical-search-incumbent-tie-preset allocation-request.json confirmed-399bc7760fb0cf790a5d8ac4 preset-next-search
dotnet BalanceHarness.dll tower-practical-search-allocation-check preset-next-search/request.json
```

The reference ID above illustrates the previously confirmed team; use the ID explicitly present in the new template. The preset directory must be new, have an existing parent, and sit outside both the complete history registry and content root. The intended search output in the source request must also be absent. The source request may be given by relative path; its internal paths retain the ordinary absolute-path contract.

Successful preparation writes exactly:

| File | Contents |
| --- | --- |
| `template.json` | Source template with only `stages.selectionPolicyVersion` and `stages.selectionPrimaryReferenceId` changed. |
| `request.json` | Original allocated-search request with only `definitionPath` and `definitionHash` updated to the new template. |
| `preset.json` | `tower-practical-incumbent-tie-preset-v1` receipt with source/output file hashes, explicit reference and canonical party identity, declared cost, `PreparedNeedsAdmission`, `admissionRequired=true`, zero new values and zero fights. |

The receipt is published last. A partial directory has no successful receipt and must not be treated as prepared input. Existing source and destination files are never overwritten. The command does not allocate a master or domain, change prior time/storage charges, refresh exclusions, copy gameplay assemblies, prepare native combat inputs, derive values or start the worker.

The subsequent `allocation-check` validates the producing runtime, native recipes and complete live history using the existing admission path. A successful preset receipt is not admission. A stale source runtime or history is still rejected there; no field is silently rebound. The supplied allocation master/domain, sample counts and cumulative limits remain the caller's prospective choices. This documentation supplies no authorization or resource allowance for a new search.

## Scope and compatibility

The preset accepts `improve-supplied` templates using `retained-composition-incumbents-v1`. Construction seeds, discovery, selection, confirmation, diagnostics and reference-scenario seeds must all be empty; feedback schedules must be absent. The existing allocation validator checks the actual requested sample counts, candidate/proposal limits, confirmation floor, complete-history shape and ordinary practical scope before output is created.

The source selector can be `tower-staged-zero-win-health-v1`, or the incumbent-tie selector with the same already-declared primary. An existing designation cannot be switched through this command. Scheduled inputs, independent-mode inputs, other generators, missing/unknown/ambiguous references, changed source hashes and existing search outputs are rejected. These guards keep the preset limited to the incumbent-generator workflow tested by the prospective comparison.

No new property was added to `TowerPracticalRequest` or `TowerBossDiscoveryDefinition`. Execution, allocation, recovery and archive formats keep their existing versions. Existing callers retain their behavior. The ordinary allocator preserves both selection fields when it binds fresh values, and reconstruction rejects a changed designation. The selector itself is unchanged: strict leaders win; only a positive maximum tie retains the designated incumbent; zero-win health and stable-order rules remain intact.

## Changed files and verification

- `LL/tools/BalanceHarness/TowerPracticalPreset.cs`: preparation API, supported-scope validation and final receipt.
- `LL/tools/BalanceHarness/TowerPracticalSearchRun.cs`: dispatch the new command before existing execution routes.
- `LL/tools/BalanceHarness/Program.cs`: CLI help.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessPracticalPresetTests.cs`: command, preservation, rejection, cancellation and literal allocation/selection integration fixtures.
- This review and the practical-search/status Markdown files: usage and the completed integration handoff.

The integration fixture uses fixed test values and literal battle reports. It follows the real allocator, saved binding verifier and selection/study logic with a combat guard. It checks that the second supplied reference can remain the designated winner on a positive tie, and that changing the saved designation fails allocation reconstruction. Separate regression coverage protects the old default, selection fallback and saved-archive behavior.

**180 tests passed, with zero failures or skips: 26 new preset cases and 154 regressions.** The final build had zero errors and nine existing warnings in other test files. CLI help, scoped whitespace and local Markdown links were checked. Evidence: [test results](../TestResults/incumbent-tie-preset-verification-20260922/tests.trx), [final build/test log](../TestResults/incumbent-tie-preset-tests-final-20260922.log) and [verification receipt](../TestResults/incumbent-tie-preset-verification-20260922/verification.json).

The initial sandboxed restore could not read the user NuGet configuration; the repository wrapper was rerun with approved access. The first compile then exposed an ambiguous `Program` name in the new CLI fixture, which was corrected to `BalanceHarness.Program`. The final source was rebuilt and all 180 tests passed. No required verification command remains blocked.

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/incumbent-tie-preset-build-20260922 -Filter 'FullyQualifiedName~BalanceHarnessPracticalPresetTests|FullyQualifiedName~BalanceHarnessPracticalSearchTests|FullyQualifiedName~BalanceHarnessPracticalAllocationTests|FullyQualifiedName~BalanceHarnessIncumbentTieTests|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessIncumbentTieComparisonTests'
```

## Next use

The subsequent [captured-runtime admission](Tower-Practical-Incumbent-Tie-Preset-Admission.md) prepared one concrete request and passed the public `allocation-check` with 538,326 exclusions and zero allocation/combat. The authorized [execution then completed all 3,496 fights and both audits](Tower-Practical-Incumbent-Tie-Preset-Execution.md), returning `ImprovementNotDemonstrated`. The challenger won 695/1,000 versus the designated incumbent's 689/1,000; both references remain recommended. Permanent exclusions now total 539,367 and this request is closed.

For other future searches, prepare a current request/template with the intended incumbent and complete current history, create the preset, and pass the existing no-allocation admission check. Keep any runtime compatibility work separate from scientific allocation. Default-policy promotion remains a separate reviewed decision; this command is explicit opt-in.

No production request or fresh scientific reservation was created by implementing this feature. No gameplay code, application configuration, database schema or migrations changed. There is no service deployment.
