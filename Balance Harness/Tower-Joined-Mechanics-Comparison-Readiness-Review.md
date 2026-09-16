# Joined-mechanics comparison: readiness review

15 September 2026. **ReadyForSpecificSeedApproval.** The executable comparison controller, exact protocol and verification scripts are frozen. **40 backend tests passed**, both fixed-order controls prepared successfully without combat, and an independent audit verified shared inputs, the complete **482,371-value** reservation union and **66 interval fixtures**. **Zero fights and zero new values** have been used. No bound study exists.

## What is ready

The baseline composition-only policy and new joined-mechanics policy each receive **44 evaluated teams × 4 shared discovery fights = 176 fights**, with at most 704 proposals and one shared generation-root value. The first 11 teams per policy are fresh constructions. Both policies use the full captured 80-Essence pool, five slots per character and ten characters forming two five-player parties. Gameplay DLLs, content, neutral identities, equipment, attributes, timestamp, scenario identity and combat schedules match. Ability order stays fixed.

Each policy's top two teams are frozen before a shared **8-fight screen**, at most **32 screen fights**. Each policy then nominates one winner. Those two winners and the two fixed controls receive **32 shared confirmation fights**, at most **128 confirmation fights**. Total cap: **512 charged attempts**. Exact shared recipes are merged for screening/confirmation while retaining every origin; freed seats are not refilled. Cross-policy discovery duplicates remain separate measurements charged within the discovery budget.

The native per-policy definition has a conservative 288-fight cost including its two controls. The controller runs discovery separately, then shared screen/confirmation stages; controls are measured only once. The global accounting is **176 + 176 + 32 + 128 = 512**, rather than summing two full native pipelines. Any incomplete search stops the pipeline. All nominations freeze before their later outcomes.

Primary contrast: joined finalist minus baseline finalist. Four secondary contrasts compare each winner with each control. Rates use the existing Wilson family factor 8; paired discordance bounds use factor 20. All five contrasts are retained, including non-improvements. One shallow restart and 32 confirmations provide limited precision. The two policies retain their own specified random streams, so this compares the policies as implemented; it does not isolate joining from all stochastic variation. No reliability, adoption or optimality claim is planned.

## Verification and resource measurements

| Check | Result |
| --- | --- |
| Existing composition tests | 17 passed |
| Joined-mechanics tests | 15 passed |
| New comparison contract/controller tests | 8 passed |
| Repository wrapper | **40 passed, 0 failed, 0 skipped**; 4.078 seconds |
| Native content/identity/registry/preparation check | Passed; 42.594 seconds |
| Fixed controls prepared | 2; no combat |
| Independent input/ledger/interval audit | Passed; 16.406 seconds |
| Numerical interval checks | 66: 0..32 wins, factors 8 and 20 |
| Prior indexed files preserved | 1838 |
| Preparation diagnostic workload | 63.390 / 300 seconds; charged against the new comparison's 1,800-second total |
| Preparation output before publication | 83.00 MiB / 1 GiB |
| New balance values / fights / retries | 0 / 0 / 0 |

The harness and test builds passed. The test build emitted one existing xUnit2031 style warning in the unchanged composition regression fixture; no errors. Build times were 3.312 and 1.531 seconds, recorded separately as engineering time. Tests use synthetic outcomes and a combat-entry guard. Preflight uses 45 already-reserved labels only in explicitly non-runnable fixtures; it does not remove those reservations from the real ledger or allocate replacement values.

The controller reuses native compact discovery/balance archives, incremental accounting and durable attempt journals. Allocation requires a matching protocol-hash authorization, checks the refreshed registry, journals every candidate value before use and preserves pending reservations on failure. Execution enforces a shared 512-attempt cap, at most 900 seconds of native workload within the remaining total allowance, and the 3 GiB study allowance. Preparation/execution evidence share the other 1 GiB. Zero retries, resumes or combat replays. Native archive verification and independent record/selection/interval verification are implemented for post-run execution but have not run against a new combat archive yet.

## Commands and files

These preparation commands ran once in the new package:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915'
& $py -B "$w/workflow.py" bootstrap
& $py -B "$w/workflow.py" build
& $py -B "$w/workflow.py" test-build
& $py -B "$w/workflow.py" tests
& $py -B "$w/workflow.py" check
& $py -B "$w/workflow.py" audit
& $py -B "$w/publish.py"
```

The test phase invokes the repository-required wrapper:

```powershell
./build/run-tests.ps1 -NoBuild `
  -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests|FullyQualifiedName~BalanceHarnessJoinedComparisonTests' `
  -ArtifactsPath 'TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915/tests'
```

After the specific approval, the frozen workflow's `bind`, `run`, `verify` and `audit-execution` modes are the authorized sequence. They write to separate comparison study/execution directories and refuse retries. **None has been invoked.** Do not rerun the preparation phases into the sealed directory; reproduction needs a new package path and pinned source/dependencies.

New files comprise the [protocol](Tower-Joined-Mechanics-Comparison-Protocol.md), this review, and the isolated preparation package's `ComparisonModel.cs`, `ComparisonProgram.cs`, `ComparisonTests.cs`, `workflow.py`, `audit.py`, compiler/source snapshots and evidence. Existing harness/gameplay source was not edited in this preparation. Active search plans and README now point here. The full dirty gameplay build/backend suite was intentionally not run; verification targets captured dependencies. No required preparation command was blocked or failed.

Evidence: [test results](../TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915/control/tests.trx), [preflight](../TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915/check/result.json), [independent audit](../TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915/control/independent-check.json), [completion](../TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915/control/completion.json), [sealed files](../TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915/preparation-files.json).

## Remaining approval and limitations

Request the specific exception for **45 fresh values** (1 generation, 4 discovery, 8 screen, 32 confirmation) and execution within **512 fights, 30 minutes including preparation, 4 GiB and zero retries**. Successful allocation would increase retained reservations from 482,371 to 482,416. The exception is required by the user's original **zero fresh balance seeds** instruction; the earlier 85-value approval applied to the completed previous pilot.

Build strength remains unmeasured for this new policy. Readiness checks do not prove that the full runtime pipeline will finish, nor that larger groups improve combat. Preserve any failure or limit evidence and stop dependent work. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged. No gameplay/content changes, migrations, configuration changes or deployment.
