# Group-owner completion implemented; verification stopped

15 September 2026. The explicit opt-in `independent-group-completion-v1` / `group-completion-joint` policy compiles. Backend verification through `build/run-tests.ps1` executed **95 cases: 94 passed, 1 failed**. All 79 existing cases and 15 of 16 new cases passed. The failed test has an incorrect family identifier. The frozen zero-retry rule stopped the remaining diagnostics; the change is **not fully verified** and no default is promoted.

The constructor now fills missing authored coverage categories on characters bearing the selected mechanic group before invoking the existing coverage filler. It uses the existing diversity schedule and seeded ordering. Providers must fit free slots, case-insensitive family exclusion and shared owned-copy limits. Selection prefers providers reaching the most uncovered group owners, with stable seeded ties. No control recipes, Essence names or combat outcomes influence selection. Traces record categories, evidence, provider choice and per-character before/after insertions. Old policies omit the new trace property.

## Failure and remaining verification

`Family_conflicts_are_case_insensitive` assigns the candidate provider `FAMILY00`. The shared fixture constructs the existing Essence's family as `family0` (`"family" + i` at i=0). These are distinct even ignoring case, so expecting no insertion is incorrect. The production code explicitly uses `StringComparer.OrdinalIgnoreCase`. Static inspection identifies a fixture typo; it does not substitute for executing the corrected test. The frozen source and test remain untouched, including the failing input.

The next engineering step is to correct this fixture to `FAMILY0` (preferably deriving the value from the existing Essence), then freeze a separate zero-combat verification package carrying forward this failure's time/output. Do not rerun this sealed package. No combat comparison should proceed until that verification succeeds.

Completed once:

- All **11,335 indexed files in 28 preceding sealed packages** verified unchanged before and after.
- Isolated candidate and test builds succeeded in **5.844 seconds** combined. One existing xUnit2031 analyzer warning remains in the old composition fixture.
- **94/95 backend cases passed** in the one wrapper invocation; the charged test phase took **4.203 seconds**. Passing cases include existing policy golden outputs, new ownership/full-slot checks, deterministic synthetic search, fixed order and cancellation.
- Frozen source, gameplay inputs and built assemblies retained their recorded hashes.

Not run because the test gate failed: the 76 captured-content construction requests, exact reconstruction of the archived 44-evaluation diversity search, and the independent Python construction/parity audit. Consequently there are **no new captured-content coverage counts, comparative timings or combat-strength findings**. The success-only publisher was not invoked.

## Measurements and reproducibility

New diagnostics before evidence closure: **6.359 seconds**; prior diagnostics: **842.082 seconds**. The [completion receipt](../TestResults/balance/tower-group-completion-implementation-20260915/completion.json) includes measured closure and a conservative one-second seal allowance, plus exact output charges. Bounds remain 300 seconds new / 1,800 cumulative diagnostics, 512 MiB new / 4 GiB cumulative output, zero fights, fresh seeds, preparations, combat replays and retries. Builds are separate engineering time.

The [frozen protocol](Tower-Group-Completion-Implementation-Protocol.md), [source patch](../TestResults/balance/tower-group-completion-implementation-20260915/implementation.patch), [test log](../TestResults/balance/tower-group-completion-implementation-20260915/tests.log), [TRX](../TestResults/balance/tower-group-completion-implementation-20260915/tests.trx) and [static failure diagnosis](../TestResults/balance/tower-group-completion-implementation-20260915/failure-diagnosis.json) retain the exact evidence.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-group-completion-implementation-20260915'
& $python -B "$work/workflow.py" preserve
& $python -B "$work/workflow.py" candidate-build
& $python -B "$work/workflow.py" tests-build
& $python -B "$work/workflow.py" tests
# STOP: test gate failed. Do not execute content, audit, or publish.
```

The test phase dispatched `./build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests" -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests|FullyQualifiedName~BalanceHarnessGroupCountTests|FullyQualifiedName~BalanceHarnessGroupVariationTests|FullyQualifiedName~BalanceHarnessGroupDiversityTests|FullyQualifiedName~BalanceHarnessGroupCompletionTests'`, preserved its TRX and charged the failed invocation. Reproduction requires a separately frozen package; the listed commands document what ran and are not retry instructions. `close-failure.py` only preserved evidence, reconciled accounting and updated documentation; it ran no further tests, construction or combat.

## Changed files and preserved scope

Added `LL/tools/BalanceHarness/TowerGroupCompletionSearch.cs` and `LL/tests/EssenceSystem.Tests/BalanceHarnessGroupCompletionTests.cs`. Updated policy registration/routing in `TowerBossDiscoveryContract.cs`, `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerCompositionSearch.cs`, `TowerLoadoutComposition.cs` and `TowerPartyCoverage.cs`; `TowerGroupCountSearch.cs` integrates the optional completion step and trace. Added the protocol/review and evidence package, and updated six active Markdown handoffs.

No unrelated checkout files were edited by this task. No gameplay/content, configuration, migrations or deployment changes. Candidate/attempt budgets, deterministic ranking, nominations and fixed ordinal ability order retain their existing rules. The new policy remains opt-in and incompletely verified.

Preserve **482,551 reservations**, including v19's unused 512, and all 253 v19 recipes. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, adoption **Hold** remain unchanged. No Kharad tuning or 129,536-fight confirmation. All previous fresh-seed exceptions are exhausted; this task used none.
