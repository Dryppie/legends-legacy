# Owner-offset comparison: implementation and captured-runtime admission

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **Admission outcome: `ReferenceExplorationOffsetComparisonAdmittedNoReservation`.** The [prospective comparison](Tower-Practical-Reference-Exploration-Offset-Comparison-Plan.md) was implemented, tested and admitted against the preserved gameplay capture. Admission itself made no scientific launch, fight or reservation. The subsequent [single execution and both audits](Tower-Practical-Reference-Exploration-Offset-Comparison-Execution.md) are now verified and closed with `DoNotPromoteReferenceExplorationOffset`: **51,672 fights**, **+0.175 percentage points**, eleven identical-output pairs and one positive pair. All promotion gates failed.

## Admitted comparison

| Item | Value |
| --- | --- |
| Protocol | `tower-reference-exploration-offset-comparison-v1` |
| Baseline | `retained-composition-three-references-v1` |
| Candidate | `retained-composition-three-reference-exploration-offset-v1` |
| Selector | `tower-staged-incumbent-tie-v1`, with the original `399bc776…` tie designation |
| Controls | Exact recipes `399bc776…`, `8287f779…`, `96b94357…` in both arms |
| Search | Twelve paired roots; two independent 46-candidate searches per root |
| Selection | Three controls plus two challengers; 32 trials each; one output per arm |
| Freeze | All 24 outputs after exactly 12,672 search fights, before confirmation |
| Confirmation | 1,000 common trials per distinct physical recipe in each pair's three-to-five-recipe union |
| Scientific fight range | 48,672–72,672 |
| Future allocation | One 16,384-word batch; assign 12,492 fresh values; permanently retain all fresh unused tail values |
| History | 567,789 exclusions across 234 files |
| Scientific output | `TestResults/balance/tower-reference-exploration-offset-comparison-20260922`, still absent |

The [frozen JSON](Tower-Practical-Reference-Exploration-Offset-Comparison-Plan.json) retains the five-percentage-point integer gate (600 net wins), positive conditional lower bound and seven-positive-pair requirement. The candidate version changes; search budgets, controls, selector and evidence rules remain fixed. This comparison evaluates offset exploration against the practical baseline, rather than isolating offsets against original exploration v1.

The captured context remains floor 5, ten level-40 characters, two groups of five, five Essences each, rank 2/tier 1, fixed equipment and actor identities, canonical Essence order and `OwnedCopies=null`. The reconciled original capture's gameplay dependencies, effective settings and 16 content files remain intact. Defaults and confirmed-team recommendations are unchanged.

## Versioned implementation

The existing comparison engine now recognizes two exact protocol declarations. Each fixes its candidate generator, plan hash and support/negative decision labels. The chosen version is bound through request, allocation, search, global freeze, study scope, result, launch and native receipts. The independent auditor and owned launcher use the same explicit version boundary. Unknown versions, swapped plan hashes and relabeled archives fail validation.

The original `tower-reference-exploration-comparison-v1` remains bound to its original generator and frozen plan. Its closed 53,672-fight result and captured packages are unchanged. Shared mechanics avoid a duplicate comparison implementation; there is no arbitrary generator, endpoint or resource override. The underlying offset generator itself did not change during this work.

Production changes are confined to [comparison execution and assessment](../LL/tools/BalanceHarness/TowerReferenceExplorationComparison.cs), [reservation and admission](../LL/tools/BalanceHarness/TowerReferenceExplorationReservation.cs), [archive reconstruction](../LL/tools/BalanceHarness/TowerReferenceExplorationComparisonArchive.cs) and [owned execution/verification](../LL/tools/BalanceHarness/TowerReferenceExplorationComparisonRun.cs). The [shared backend fixtures](../LL/tests/EssenceSystem.Tests/BalanceHarnessReferenceExplorationComparisonTests.cs), [independent auditor](analysis/audit-reference-exploration-comparison.py), [launcher](../build/run-reference-exploration-comparison.py), [Python fixtures](../build/test-reference-exploration-comparison.py), [planning checker](analysis/practical-reference-exploration-offset-design.py) and [admission helper](analysis/prepare-reference-exploration-admission.py) implement or verify the new boundary. The existing source/symbol/JIT helper is reused unchanged.

## Engineering verification

- **124 scoped backend tests passed**, with zero failures or skips, covering both comparison versions, existing generator compatibility, legal proposals, nomination, selection, shared controls, allocation interruption, freeze integrity and native archive reconstruction.
- **Two strengthened protocol tests passed** after tightening their launch fixture so version substitution was the sole invalid field. No producing harness source changed after the 124-test regression.
- **27 Python tests passed**, including independent recounts of both literal archives, swapped-version rejection, changed source/plan rejection, cumulative resource accounting and owned-process failure handling.
- **14 planning/arithmetic tests passed**, including the unchanged fixed denominator, integer gates, shared-control costs, depletion bound and rejection of a weaker gate or larger allowance.
- **Nine offset admission-helper tests passed**. The seven existing helper checks also passed in original mode. These tests validate the new source receipt, history declaration, output boundary, preserved dependencies, settings and process ownership.

The comparison fixtures use literal outcomes with a combat-entry guard, and their native/independent results agree. The included generator archive tests also execute temporary native engineering battles. These workloads are separately disclosed engineering evidence, not scientific trials. Neither supplies strength evidence for the offset policy.

The first backend build completed in 3:00.11 with **38 pre-existing warnings outside the changed files and zero errors**; tests took about 4 minutes 23 seconds. The focused follow-up build had nine warnings outside the changed files and zero errors; both tests passed. Python archive checks took 445.225 seconds. The full repository suite was not run. No required verification command remains blocked.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/reference-exploration-offset-comparison-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessReferenceExploration|FullyQualifiedName~BalanceHarnessThreeReferenceTests'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/reference-exploration-offset-comparison-build-20260922' -Filter 'FullyQualifiedName~Protocol_cannot_substitute_the_other_generator_plan_or_allocation'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'build/test-reference-exploration-comparison.py' --fixture 'TestResults/reference-exploration-offset-comparison-fixtures-20260922/tower-reference-exploration-comparison-v1' --offset-fixture 'TestResults/reference-exploration-offset-comparison-fixtures-20260922/tower-reference-exploration-offset-comparison-v1'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/practical-reference-exploration-offset-design.py' --output 'TestResults/reference-exploration-offset-comparison-implementation-20260922/arithmetic.json'
```

These commands record completed work; the create-new evidence outputs must not be overwritten. The [sealed implementation receipt](../TestResults/reference-exploration-offset-comparison-implementation-20260922/verification.json), [source pins](../TestResults/reference-exploration-offset-comparison-implementation-20260922/source-files.json), [regression TRX](../TestResults/reference-exploration-offset-comparison-implementation-20260922/regression-tests.trx), [focused TRX](../TestResults/reference-exploration-offset-comparison-implementation-20260922/protocol-tests.trx) and [Python log](../TestResults/reference-exploration-offset-comparison-implementation-20260922/python-envelope-tests.log) retain the evidence. The implementation manifest is `0a546dbd9e4969cd4ac69445a4572910fa2ad9b9f999685914d0c55b71b1abf2`.

## Native admission and remaining allowance

The producing DLL and portable symbols matched **201 source documents**. **627 relevant methods**, including comparison, reservation, generation and verification paths, resolved against the preserved dependencies without invoking their bodies. The public native check returned `ReadyNoReservation` and prepared three reference inputs under each policy, **six preparations total**, with the combat-entry guard active. Temporary validation labels were not scientific values.

Independent full-history scans before and after the native check reproduced the same **234-file inventory and 567,789-value union**, including supported abandoned-reservation recovery. The native check independently validated this full history. Every owned process tree was empty on completion.

| Admission phase | Seconds | Outcome |
| --- | ---: | --- |
| Source/symbol/runtime compatibility | 0.969 | Passed |
| Public native comparison check | 20.828 | Passed; zero fights/values |
| Entire admission through sealing | 158.828 | Passed within 600 seconds |

The package retained **40,335,855 bytes (38.47 MiB)**, plus its small external pin. The request conservatively charges the full **600 seconds /512 MiB**, leaving **10,200 seconds /5.5 GiB** of the new **10,800-second /6-GiB** cumulative envelope for one execution, both audits and publication. The native child is capped at **10,080 seconds /5.25 GiB**. All later audit work shares the remaining execution deadline.

The authenticated conservative cost projection remains **8,587.387 seconds /3,134.161 MiB** at 72,672 fights, based on the previous 3,528-fight execution including its audits. It fits the envelope but is not a guaranteed upper bound. Limits, retries and stopping rules are unchanged; unused allowance cannot buy an extension or replacement.

Engineering work remains separately disclosed under the accepted accounting decision. Complete older engineering totals remain unknown, and the **18,180-second /13,584-MiB** prior ledger is preserved.

## Captured package and next step

- [Admitted request](../TestResults/reference-exploration-offset-comparison-admission-20260922/request.json), [template](../TestResults/reference-exploration-offset-comparison-admission-20260922/template.json), [native check](../TestResults/reference-exploration-offset-comparison-admission-20260922/native-check.json) and [admission receipt](../TestResults/reference-exploration-offset-comparison-admission-20260922/admission.json).
- [Package manifest](../TestResults/reference-exploration-offset-comparison-admission-20260922/files.json), [external pin](../TestResults/reference-exploration-offset-comparison-admission-20260922-pin.json) and [retained launcher](../TestResults/reference-exploration-offset-comparison-admission-20260922/run-reference-exploration-comparison.py).
- [Admission-helper tests](../TestResults/reference-exploration-offset-admission-engineering-20260922/helper-tests.log) and [read-only package verification](../TestResults/reference-exploration-offset-admission-engineering-20260922/admission-verification.log).

```text
Plan SHA-256:      7e6203052cd8a1984b80bbe34dbb1e4b21fe3f882c8ab314e21987d289a25c90
Admission manifest:160db185b5c8ed49e045338d0590d2a8a51e57ab93432955d89e5db4b9fd5ff6
Request SHA-256:   dcca850da20a21aeeff850e7adb9951f8adf4b80292dab2a82d759e31194bed1
Template SHA-256:  7494c7e59161017488642a188aaf181babd6fcb354fe2eb1f07f23958bc7cdeb
Execution hash:    bcb065d7b4bc0f0e7061c868a05e017b118555a379e4610dd62053a14665e734
```

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/prepare-reference-exploration-admission.py' self-test --offset
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/prepare-reference-exploration-admission.py' prepare --offset
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'Balance Harness/analysis/prepare-reference-exploration-admission.py' verify --offset --manifest-sha256 '160db185b5c8ed49e045338d0590d2a8a51e57ab93432955d89e5db4b9fd5ff6'
```

Preparation above records the one completed admission; it cannot be repeated or resumed. Read-only verification reproduced its conclusions without another native preparation. The fresh prelaunch history check subsequently passed with the same 567,789 exclusions and 234 files. These are historical admission commands; the prelaunch verifier requires an absent scientific output and must not be used as a post-execution check.

The [execution closeout](Tower-Practical-Reference-Exploration-Offset-Comparison-Execution.md) records the completed single launch, both passing audits and 3,409.204 seconds of execution. With admission precharged, cumulative cost was 4,009.204 seconds /1,626.14 MiB. The full permanent history is now 584,171 exclusions across 236 files. Failed strength gates end this offset experiment without promotion or extension. The captured cohort's separate balance failure remains unchanged.

No application configuration changes, migrations, database operations or deployments occurred. Source syntax, scoped whitespace, updated local links and final package/source checks are recorded in the [completion verification](../TestResults/reference-exploration-offset-admission-engineering-20260922/completion-verification.json).
