# Affinity-creation recognition: native execution and independent audit

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The frozen affinity-creation recognition plan now has a separate native execution profile, independent Python auditor and owned launcher.** The profile is `tower-affinity-creation-recognition-v1`. It admits only the [existing plan](Tower-Affinity-Creation-Recognition-Plan.json), SHA-256 `170065b593a49609e72142d766443c3a47f7a271b937cc88d52436de2b4792a2`. The earlier recognition profile retains its own exact plan and family bindings. Commands reject a request for the other profile before running a handler.

This step adds execution support and tests it with literal engineering fixtures. **No real combat, production entropy draw, live-history reservation, admission or policy promotion occurred.** The next step is a separately versioned admission adapter that verifies the captured gameplay runtime, complete live history and resource feasibility before creating a runnable request. The completed benchmark-validation pilot remains **Inconclusive**.

## Frozen design and reporting

The [builder report](Tower-Affinity-Creation-Recognition-Implementation.md), canonical plan and population-sampling receipts are unchanged. Their `FrozenDesignNativeIntegrationRequired` and `runnableRequest: false` fields describe the original planning record. A later admission must create separate native definition/request files rather than rewrite that plan or redraw membership.

The native family hash is `9fcf23a8934986ad08b2a34a3096230369d9d0817e8e70dc5b2d6b46d715c7e5`. It binds all 108 exact team/root cells in order, including root-specific scenarios. Each of twelve roots uses 256 shared combat values for its nine teams. Panels are disjoint across roots; repeated recipes retain separate root identities. The prospective workload remains **27,648 attempted fights using 3,072 fresh values**.

Reporting preserves 108 win rates, 216 signed paired candidate/reference contrasts and the approximate Wilson family of 540. Draws count as non-wins. The benchmark remains the fixed third reference. Lower-stratum observations receive weight 13/2, giving the prespecified all-17 estimate `(sum of four nominee/near-miss gains + 6.5 × sum of two lower gains) / 17`. All 132 unmeasured outcomes remain null. Combat intervals are not population-mean intervals.

The only completed decision is `CompleteDiagnosticOnly`, with `DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion`. Native confirmation/adoption logic rejects either recognition profile. These results cannot qualify a team, promote a search policy or establish performance on future roots.

## Execution and audit boundary

The implementation reuses the established reservation, archive and owned-process lifecycle. The new version supplies its own exact plan/family pins. Separate Python entry points preserve the earlier auditor and launcher unchanged, so a future admission can retain the exact profile-specific sources.

After a successful admission and a fresh freeze under exclusive registry/output leases, execution permits one 24,576-byte cryptographic batch. All 6,144 words are classified. The first 3,072 fresh values populate the panels; every exposed fresh value, including the unused tail, is permanently reserved. Exhaustion or interruption cannot trigger refill, resume, replacement roots or retry.

The launcher assigns each child to a hidden Windows Job before resuming it. It retains ownership through native execution, native reconstruction, independent Python reconstruction and the final native publication barrier. Both audits must agree before publication. The independent auditor reads every compressed terminal report, reconstructs ordering, pairing, reservation classification and reporting arithmetic, and verifies archived identities without invoking native code or combat. Post-publication verification checks complete inventory, final resource accounting and native reconstruction again.

The new profile enforces the established envelope:

| Stage | Elapsed cap | Storage cap |
| --- | ---: | ---: |
| Native preparation, reservation, execution and cleanup | 6,000 seconds | 3 GiB |
| Both audits and publication | 1,200 seconds | 512 MiB |
| Combined scientific allowance | 7,200 seconds | 3.5 GiB |

Admission charges are additional: one 600-second/512-MiB charge, or two explicitly preserved charges when a preceding failed admission exists. The latter does not increase the scientific allowance. No actual admission has been charged or performed here. These enforced limits are **not** evidence that real combat will fit: a new admission must authenticate source timings/storage and establish a defensible forecast. Engineering fixtures do not establish real-combat throughput.

Public commands are:

```text
tower-affinity-creation-recognition-context <content-root>
tower-affinity-creation-recognition-check <request.json>
tower-affinity-creation-recognition-audit <archive>
tower-affinity-creation-recognition-verify <archive>
```

`-run` and `-publication-check` belong to the owned workflow. The [launcher](../build/run-affinity-creation-recognition.py) requires a sealed admission, its external manifest hash and that admission's retained `runtime/BalanceHarness.dll`. There is no runnable production request for this profile yet. A current-checkout build is not a substitute for captured-runtime compatibility.

## Changed files and design decisions

- [TowerAffinityCreationRecognition.cs](../LL/tools/BalanceHarness/TowerAffinityCreationRecognition.cs) defines the new version, exact hashes and version-specific dispatch. `Program.cs` routes its command prefix.
- The existing `TowerFrozenPoolRecognition.cs` and `TowerFrozenPoolRecognitionRun.cs` share diagnostic arithmetic and orchestration across the two explicitly admitted profiles. `TowerFixedFamilyConfirmation.cs`, `...Protocol.cs`, `...Run.cs` and `...Reservation.cs` use the selected profile for panels, request validation and freeze authentication. Existing profile constants are preserved.
- [audit-affinity-creation-recognition.py](analysis/audit-affinity-creation-recognition.py) and [run-affinity-creation-recognition.py](../build/run-affinity-creation-recognition.py) are separate profile-bound Python entry points. No legacy Python implementation was changed.
- [BalanceHarnessAffinityCreationRecognitionTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityCreationRecognitionTests.cs), [test-affinity-creation-recognition.py](../build/test-affinity-creation-recognition.py) and [test-affinity-creation-recognition-owned.py](../build/test-affinity-creation-recognition-owned.py) cover the new profile. Three files in the separate `BalanceHarness.ProcessFixture` executable route guarded literal fixtures; production commands cannot select that test mode.
- This report and current-status banners in nine harness/assessment/admission documents record progress. Their historical bodies are preserved.

There are **no migrations, application configuration changes, deployments, gameplay edits or default-policy changes**. Unrelated uncommitted application work is preserved.

## Verification

Verification results and commands are retained in the [native verification package](../TestResults/affinity-creation-recognition-native-verification-20260924/verification.json). Native regression tests cover both recognition profiles and the existing fixed-family and three-reference confirmation profiles. New cases reject cross-profile plans, families and CLI requests before side effects. Reservation tests cover nine interruption boundaries; arithmetic tests cover paired outcomes, missing/reordered evidence, lower-stratum weights and explicit nulls.

**122 backend tests passed**, with zero failures or skips: 17 affinity-recognition, 14 earlier recognition, 73 fixed-family confirmation and 18 three-reference confirmation cases. The required `build/run-tests.ps1` wrapper retained the [TRX results](../TestResults/affinity-creation-recognition-native-verification-20260924/backend-tests.trx). No required verification command remains blocked.

**42 Python cases passed:** 18 new protocol/owner cases plus its complete saved-report test, and 22 legacy cases plus its complete saved-report test. The [owned fixture receipt](../TestResults/tower-affinity-recognition-owned-fixture-20260924/verification.json) passed in **192.734 seconds**, with **69,873,082 bytes** observed before writing its summary receipt. It records **27,648 literal reports, zero actual combat and zero production entropy draws**. The tested harness DLL hash is `bcfd818a72244dbe3526a075627d2ba4c91d66dce9484fcc64405acdcb771811`; its final archive manifest is `6c03bfe9eda04a2ce54cf6ee9712c1f107df1afd4068edb371bada5cbf3b150b`. Every owned-process receipt reports a successful exit, no timeout and zero active descendants.

The full owned fixture exercises the production process owner, leases, native worker, archive, both audits, publication barrier and post-publication native verification. Only fixture command routing, literal inputs/outcomes/entropy and the admission prerequisite are substituted. Empty fixture content and a combat trace guard prevent simulator use. These are operational compatibility checks, not new scientific evidence.

The initial sandboxed backend build could not read the user NuGet configuration. The same required wrapper passed after filesystem access was granted. The build emitted 43 existing warnings and no errors. Python tests caught an old plan hash in the initial copied auditor; the corrected source passed before final independent and owned verification. The earlier fixture and failed logs are retained, and the final fixture captures the corrected auditor rather than modifying the earlier archive.

Principal verification commands (`python` means the bundled interpreter; synthetic output names are single-use):

```powershell
$env:AFFINITY_CREATION_RECOGNITION_FIXTURE = "$PWD/TestResults/tower-affinity-recognition-fixture-20260924"
$env:FROZEN_POOL_RECOGNITION_FIXTURE = "$PWD/TestResults/tower-recognition-regression-fixture-20260924"
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAffinityCreationRecognitionTests|FullyQualifiedName~BalanceHarnessFrozenPoolRecognitionTests|FullyQualifiedName~BalanceHarnessFixedFamilyConfirmationTests|FullyQualifiedName~BalanceHarnessThreeReferenceConfirmationTests' -ArtifactsPath '.artifacts/affinity-recognition-native-20260924'
python -B -X utf8 build/test-affinity-creation-recognition.py
python -B -X utf8 build/test-frozen-pool-recognition.py
python -B -X utf8 build/test-affinity-creation-recognition-owned.py --fixture-host '.artifacts/affinity-recognition-native-20260924/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll' --source-fixture 'TestResults/tower-affinity-recognition-fixture-20260924' --output 'TestResults/tower-affinity-recognition-owned-fixture-20260924'
```

The `SavedRowsTests` class from each Python test module was also run against its final fixture, checking a full independent recount, unchanged file hashes and rejection of reordered direct rows. The [saved-row test helper](../TestResults/affinity-creation-recognition-native-verification-20260924/check-saved-rows.py) provides that isolated invocation without repeating the already-passing owner tests. Scoped `git diff --check`, source/receipt hashes and local report links are checked during closeout.

## Remaining admission work

Create a new admission adapter for this exact cohort. It must authenticate the source publication, frozen plan and sampling receipts; retain captured gameplay dependencies/content unchanged; substitute only verified harness artifacts; verify native compatibility and all 108 transports; bind the complete current reservation history; and qualify native/audit time and storage separately. It must carry failed attempts as charges, seal the complete package and supply the external admission hash required by the launcher. Actual diagnostic execution is a subsequent, separate step.

Scientific accounting is unchanged: recorded charges remain **30,405.312 seconds /25,525,204,135 bytes**, with declared maxima **67,680 seconds /43,486,543,872 bytes**. This turn's engineering tests and documentation checks are separate. The last complete live-history verification remains **737,749 values across 254 files**; it was not rescanned here. No scientific outcome is inferred from literal fixture reports.
