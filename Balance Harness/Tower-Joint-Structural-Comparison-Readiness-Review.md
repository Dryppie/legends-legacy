# Joint structural comparison: preparation handoff

15 September 2026. **AdapterVerifiedAwaitingSeedAndTimeException**. The comparison adapter compiled and all **eight backend tests passed**, including both policies with synthetic scores. Independent fixture-budget/input checks passed. Zero fights, battle preparations, fresh seed values or retries.

The concrete [protocol](Tower-Joint-Structural-Comparison-Protocol.md) compares allocation and joint structural search with **16 teams each**, four discovery fights per team, top-two screening on eight shared seeds, and each policy winner plus two fixed controls on 32 shared confirmation seeds. Maximum **288 fights**; a finalist plays 44 in total. Both policies have the same evaluated-team budget; proposal caps remain 256 for allocation and 16 for the finite structural policy. No search settings, outcomes or controls leak between them.

The isolated adapter is adapted from the previously verified completion/allocation comparison model and native runner. It retains durable seed intent/journals, attempt charging, cancellation, storage accounting, archive verification, nomination/screen rules, exact duplicate merging and paired interval calculations. The seed namespace is new. The candidate's 12 tests / seven old-policy parity fixtures are reused from the sealed adapter. Captured gameplay DLLs/content/settings are unchanged. Fixed ordinal ability order remains.

Eight tests through `build/run-tests.ps1` cover costs, isolated inputs, context drift, nominations, incomplete discovery, screen ties, duplicate merging, incomplete evidence, paired intervals and interrupted attempt charging. Synthetic fixtures allocate no seed reservations and invoke no combat. The independent audit checks exported definitions, 64 discovery fights per policy, 288 global maximum, exact inputs and runner cap changes. Build outputs, source hashes, command logs and TRX are retained.

| Preparation phase before publication | Seconds | Accounting |
| --- | ---: | --- |
| audit | 0.172 | diagnostic |
| build | 3.625 | engineering build |
| freeze | 0.750 | diagnostic |
| test-build | 1.734 | engineering build |
| tests | 2.641 | diagnostic |

The full registry and preservation-chain checks, two real control preparations, and native/independent live preflight have **not run**. They cannot safely fit in the existing remaining time. After a specific time exception, these are mandatory before allocating a single value. This is tested adapter readiness, not completed live preflight or permission to fight. No claims of new combat strength or live-registry verification are made.

Requested exception: **45 fresh values and 600 additional diagnostic seconds**, increasing the cumulative time ceiling from 1,800 to 2,400 seconds, solely for this at-most-288-fight pilot and its preflight/binding/verification/audit/closure. The cumulative 4 GiB output cap, zero retries/resumes/replays, and all old experiment caps stay fixed. No value has been allocated. Successful binding would extend the 482,641 reserved values to 482,686. Approval must name both the seed and time exceptions and bind the protocol/preparation hashes; a generic historical seed approval is exhausted and cannot be reused.

Preparation uses only the remaining original allowance. The [completion receipt](../TestResults/balance/tower-joint-structural-comparison-preparation-20260915/control/completion.json) reports actual remaining time and output, including a conservative one-second closure allowance and 1 MiB temporary-test charge. All current harness source and unrelated dirty tracked files are preserved. Relevant predecessor/input hashes were checked; full chain/registry verification is explicitly deferred. No configuration, migrations, gameplay changes or deployment implications. Full gameplay build/backend suite not run.

## Commands

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-joint-structural-comparison-preparation-20260915'
& $python -B "$work/assemble.py"
& $python -B "$work/prepare_audit.py"
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Do not rerun sealed preparation. `assemble.py` generates the new comparison model/runner/tests from the specified prior snapshots. `prepare_audit.py` derives the independent archive audit. Tests run `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests" -Filter FullyQualifiedName~BalanceHarnessJointStructuralComparisonTests`.

Only after the specific approval is recorded in the new execution `authorization.json`:

```powershell
& $python -B "$work/execute.py" check
& $python -B "$work/execute.py" audit-check
& $python -B "$work/execute.py" bind
& $python -B "$work/execute.py" run
& $python -B "$work/execute.py" verify
& $python -B "$work/execute.py" audit-execution
& $python -B "$work/finish.py"
```

The wrapper rejects missing approval, stops dependencies on failure, reserves closure time and enforces the cumulative cap. Native binding independently verifies the seed/time permit and unchanged live registry before durable allocation. All execution remains pending. The approval receipt requires user wording, freshValues=45, maximumFights=288, additionalDiagnosticSeconds=600, maximumDiagnosticSeconds=2400, maximumOutputBytes=4294967296, retries/resumes/replays=0, protocolHash and preparationSeal; these fields must never be generated as approval without an actual user response.

Changed files: the new comparison test class, frozen comparison model/native runner and workflow/audit scripts, protocol/review and six active handoffs. Existing harness implementation is unchanged. V19 keeps 253 recipes and unused 512 confirmation values. Reliability Fail 1/3, deep recovery 0/3, v19 Unresolved and adoption Hold.
