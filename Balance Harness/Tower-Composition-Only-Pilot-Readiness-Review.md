# Composition-only pilot ready for the seed exception

**Composition-only pilot completed - 15 September 2026:** the [execution review](Tower-Composition-Only-Pilot-Execution-Review.md) records **512 verified fights**, 48 distinct builds, four screened builds and 4 confirmed recipes. The prespecified primary won **0/64**, the second finalist **0/64**. No paired contrast establishes an improvement over its control. **85 explicitly approved new values** bring the preserved ledger to **482,371**; zero retries/resumes. Diagnostic workload 257.45 seconds; fixed order, unchanged gameplay, reliability and adoption Hold.

15 September 2026. The executable small pilot is prepared and independently checked against the unchanged captured-v19 guardian +10% candidate. **24/24 focused tests passed; zero fights and zero newly allocated balance seeds.** The full **482,286-reservation** union remains preserved. The only execution gate is the specific exception to the user's original zero-fresh-seed instruction: **85 new values**, with the existing 512-fight, 30-minute and 4-GiB diagnostic limits unchanged.

## What will run

The [frozen protocol](Tower-Composition-Only-Pilot-Protocol.md) specifies one 48-build composition-only search with four discovery fights per build (192), a four-build screen with 16 fights each (64), and the top two screened builds plus both saved controls with 64 confirmation fights each (at most 256). Total at most **512 fights**. Exact confirmation duplicates merge while retaining all origins; unused seats/fights are not refilled. There are no retries, resumes or replays.

The 85 proposed values comprise one generation root, four discovery values, 16 screening values and 64 confirmation values. No values have been derived or reserved for this pilot. Binding excludes every existing reservation, including the unused historical 512 and 32 values. The expected union after authorized binding is 482,371.

The full captured pool contains 80 Essences. Floor 5 uses ten characters, organized as two five-player parties, with five Essences per character. Equipment, attributes, neutral identities and UTC start time remain captured. Both saved controls use the same fixed ordinal Essence order and non-Essence context as generated candidates. Controls are excluded from generation, parents, module retention and screening. The winner of the fresh screen is the prespecified primary; confirmation cannot trigger more search or retrospective primary selection.

This is an exploratory comparison with fixed-order controls. One shallow restart and four discovery fights cannot establish search reliability, superiority over the old optimizer, full-family balance acceptance or optimality. Every generated recipe and earlier >50% observation remains archived; unsampled confirmation recipes remain unresolved. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold are unchanged.

## Verification and measurements

| Check | Result |
| --- | --- |
| Captured harness plus pilot adapter compilation | Passed, 3.235 s, zero warnings |
| Focused test assembly compilation | Passed, 1.547 s |
| Required `build/run-tests.ps1` focused suite | **24/24 passed**, 3.969 s wrapper time |
| Native real-input check | Passed, 44.937 s; two control preparations, zero fights |
| Independent preparation audit | Passed, 24.406 s |
| Diagnostic command time so far | 69.343 s; initial preservation/setup also measured at 0.203 s |
| Output before publication | 76.08 MiB |

Seven new pilot tests cover exact costs/disjoint schedules, control context drift, deterministic generation and incomplete discovery, screen ordering and partial evidence, duplicate finalist/control merging, paired statistics and interrupted durable attempts. The 17 existing composition-only tests also pass, including historical policy output hashes. All synthetic tests enter no combat. The independent audit checks the exact definition, both canonical control transformations, unchanged gameplay inputs, all 482,286 reservations and 130 Wilson interval rows.

The pilot uses existing compact campaigns, owned storage accounting, cancellation, archive reconstruction and per-attempt durable journals. It scans sealed sibling archives only at the three stage boundaries. Detailed phase, CPU, memory, allocation and TowerPerformanceTrace measurements persist on success/failure. Separate generated/screen/confirmation schedules prevent confirmation feedback. The reusable evaluator also emits its normal scoped assessment, but only the pilot's explicit exploratory interpretation applies; that output cannot promote adoption.

## Reproduction and execution

Preparation commands executed once from the repository root:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-composition-only-pilot-preparation-20260915'
& $py -B "$w/workflow.py" bootstrap
& $py -B "$w/workflow.py" build
& $py -B "$w/workflow.py" test-build
& $py -B "$w/workflow.py" tests
& $py -B "$w/workflow.py" check
& $py -B "$w/workflow.py" audit
& $py -B "$w/publish.py"
```

The tests command calls `build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessCompositionPilotTests' -ArtifactsPath "$w/tests"`. Do not rerun this sealed preparation directory. Reproduction requires a separate workspace and pinned snapshots.

After the explicit 85-value exception is recorded in the separate execution directory, the already prepared commands are `workflow.py bind`, `workflow.py run`, `workflow.py verify`, and `workflow.py audit-execution`. The wrapper charges each diagnostic command against the remaining 1,800-second workload cap, refuses repeated phases and retains failure markers. Native run additionally caps its combat stage at 900 seconds. User waiting time is excluded. The preparation fixture uses marked historical integer labels solely for zero-combat schema/materialization checks; the binder never launches that fixture.

[Test results](../TestResults/balance/tower-composition-only-pilot-preparation-20260915/control/tests.trx), [native check](../TestResults/balance/tower-composition-only-pilot-preparation-20260915/check/result.json), [independent audit](../TestResults/balance/tower-composition-only-pilot-preparation-20260915/control/independent-check.json), [request and input pins](../TestResults/balance/tower-composition-only-pilot-preparation-20260915/control/request.json), [completion](../TestResults/balance/tower-composition-only-pilot-preparation-20260915/control/completion.json), [sealed preparation](../TestResults/balance/tower-composition-only-pilot-preparation-20260915/preparation-files.json).

Only the offline pilot package and active Markdown were added/updated. The implementation uses the previously verified composition-only source snapshot and identical captured gameplay dependencies; it does not rebuild dirty gameplay projects. The full backend suite, actual pilot combat and its post-combat verification were not run in this preparation scope. No build/test/diagnostic attempt failed. Unrelated dirty work is retained. No migrations, application configuration changes, deployment, gameplay tuning or old cap changes.
