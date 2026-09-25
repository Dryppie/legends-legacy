# Paired-pool recognition: native execution and independent audit

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The frozen paired-pool plan now has a separate native execution profile, independent Python auditor and process-owned launcher.** The profile is `tower-affinity-preservation-recognition-v1`. It accepts only the [existing plan](Tower-Affinity-Preservation-Recognition-Plan.json), SHA-256 `8456bb84f4f061eba78d35371602ec8a49ac81e5e60a550552c02bda3d3fddf6`. The two earlier recognition profiles retain their own bindings, arithmetic and resource limits.

This is implementation and engineering verification. No actual combat, production entropy, live-history reservation, resource admission or policy promotion occurs in this step. The next step is a separate admission adapter that authenticates the captured gameplay runtime, complete reservation history and resource forecast before producing a runnable request. The completed preservation pilot remains **NoObservedOutputDifferentiation**.

## Frozen family and estimates

The [builder report](Tower-Affinity-Preservation-Recognition-Implementation.md), canonical plan and population-sampling receipts are unchanged. Their `FrozenDesignNativeIntegrationRequired` and `runnableRequest: false` fields describe that immutable planning record. Native support does not rewrite the plan, redraw its sample or make it an admitted request.

The native team-family hash is `2b77f4d612e3adef132bd68abe221971dbd4481e8850c97818a02887b41822c3`. It binds all 145 measured root/recipe cells in order. Root sizes are **13, 13, 12, 13, 11, 11, 12, 12, 11, 12, 12, 13**. Every measured recipe in a root shares that root's 256 combat values. The twelve panels are disjoint; repeated recipes retain separate root identities. Shared recipes within a root are measured once for both arms. The prospective workload is **37,120 attempted fights using 3,072 fresh values**.

The result retains 145 win rates, 327 candidate/reference contrasts and approximate Wilson family 799. Draws count as non-wins. The benchmark is the fixed source benchmark, not a reference chosen after measurement. All 176 unmeasured outcomes remain null. Metadata covers all 321 root/recipe cells with both-arm membership, provenance and exact inclusion probabilities; exports retain the measured scenarios and root identities.

For each arm, the generated-pool mean is the sum of benchmark-relative gains divided by inclusion probability and by the fixed pool size of 17. Mandatory cells have probability one; each remaining membership stratum has its frozen sample size and population size. Shared cells use identical outcomes and weights, so they cancel in the preserving-minus-original pool difference. The new `pairedPool` result contains per-root and equal-weight all-root summaries, both frozen nominee means and validation challengers. Nominee means explicitly average **all five** frozen nominees, including references where nominated. The legacy `populations` array is empty for this profile; legacy result serialization omits `pairedPool` and stays unchanged.

Uncertainty is reported in separate components:

- Sampling variance uses the frozen stratified finite-population formula, with the estimand coefficient inside each sampled value and the finite-population correction. Census strata contribute zero. It conditions on fixed full-panel recipe outcomes.
- Combat variance uses the per-seed weighted paired outcomes, retaining all covariance induced by common seeds and shared cells. It conditions on the selected sample.
- Cross-arm sampling and combat covariances are explicit. The pool-difference variance respects covariance rather than treating arms as independent. All-root variances sum the twelve root components and divide by 144.

These two components are not added into a combined variance or confidence interval. Individual Wilson intervals do not become population-mean intervals. Reporting is diagnostic for twelve fixed roots: no full-pool maximum imputation, future-root inference, qualification, selector fitting, policy promotion or default change. The only completed decision is `CompleteDiagnosticOnly`.

## Execution and independent verification

The implementation reuses the established reservation, archive and owned-process lifecycle. The profile supplies exact root offsets, plan/family pins, assessment and resource limits. The independent auditor and launcher are separate entry points, leaving both earlier Python profiles unchanged.

After a separate successful admission, execution freezes its request under exclusive registry/output leases and permits one 24,576-byte cryptographic batch. All 6,144 exposed words are classified. The first 3,072 fresh values populate the panels; every exposed fresh value, including the unused tail, is permanently reserved. Exhaustion or interruption cannot trigger refill, resume, replacement roots or retry.

The launcher assigns children to a hidden Windows Job before resuming them. It retains ownership through native execution, native reconstruction, independent Python reconstruction and the native publication barrier. Both auditors must agree on the complete result before publication. The Python auditor independently reads all compressed terminal reports and reconstructs pairing, root boundaries, reservation classification, weighted estimates, both variance components and provenance. Post-publication native verification checks the final inventory, resource accounting and reconstructed result again.

The new profile enforces this envelope; it is not yet admitted:

| Stage | Elapsed cap | Retained storage cap |
| --- | ---: | ---: |
| Native preparation, reservation, execution and cleanup | 9,000 seconds | 5.5 GiB |
| Both audits and publication | 1,800 seconds | 512 MiB |
| Combined execution allowance | 10,800 seconds | 6 GiB |

Admission adds one 600-second/512-MiB charge, or two explicitly preserved charges if a preceding admission failed. Those charges do not increase the execution allowance. Existing recognition profiles retain 6,000 seconds/3 GiB for native execution and 1,200 seconds/512 MiB for audits. These enforced caps are not evidence that real combat fits: the new admission must authenticate source timings and storage, forecast both phases separately, and reject an infeasible request. Engineering fixtures cannot establish real-combat throughput.

Public commands use prefix `tower-affinity-preservation-recognition` with actions `context <content-root>`, `check <request.json>`, `audit <archive>` and `verify <archive>`. The `run` and `publication-check` actions belong to the owned workflow. The [launcher](../build/run-affinity-preservation-recognition.py) requires a sealed admission, its external manifest hash and its retained `runtime/BalanceHarness.dll`. A current-checkout build does not establish captured-runtime compatibility.

## Changed files and boundaries

- [TowerAffinityPreservationRecognition.cs](../LL/tools/BalanceHarness/TowerAffinityPreservationRecognition.cs) adds the version, frozen family, root boundaries, weighted paired assessment and report. `TowerAffinityCreationRecognition.cs` and `Program.cs` dispatch the additional profile.
- `TowerFixedFamilyConfirmation.cs`, `TowerFixedFamilyConfirmationProtocol.cs` and `TowerFixedFamilyConfirmationRun.cs` select versioned panel boundaries and resource limits. `TowerFrozenPoolRecognition.cs` and `TowerFrozenPoolRecognitionRun.cs` retain legacy behavior while dispatching the new assessment and validating its launch, receipts and publication.
- [audit-affinity-preservation-recognition.py](analysis/audit-affinity-preservation-recognition.py) independently reconstructs the reports and statistics; [run-affinity-preservation-recognition.py](../build/run-affinity-preservation-recognition.py) owns execution, both audits and publication.
- [BalanceHarnessAffinityPreservationRecognitionTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityPreservationRecognitionTests.cs), [test-affinity-preservation-recognition.py](../build/test-affinity-preservation-recognition.py) and [test-affinity-preservation-recognition-owned.py](../build/test-affinity-preservation-recognition-owned.py) exercise the new profile. Three existing `BalanceHarness.ProcessFixture` files route literal engineering fixtures in the separate test executable. Production commands cannot select that fixture mode.
- This report and line-three status banners in nine harness/assessment/admission documents record progress. Historical document bodies are preserved.

There are no migrations, application configuration changes, deployments, gameplay edits or default-policy changes. Unrelated working-tree changes are preserved.

## Next admission

Build a separately versioned admission adapter for this exact paired cohort. Authenticate the preservation publication, stage review, frozen plan and sampling receipts. Retain captured gameplay dependencies/content unchanged, substitute only verified harness artifacts, check native compatibility and all 145 transports, bind complete current reservation history, and qualify the native/audit time and storage forecasts separately. Preserve failed admission charges, seal the complete package and supply the external manifest hash required by the launcher. Actual diagnostic execution follows that admission as a separate step.

Scientific accounting is unchanged: recorded charges remain **35,591.656 seconds /29,290,708,486 bytes**, with declared maxima **89,640 seconds /56,186,896,384 bytes**. This step's engineering tests and documentation checks are separate. The last complete live-history verification remains **760,272 values across 258 files**; it was not rescanned here. No scientific outcome is inferred from literal reports.

## Verification results

**142 backend tests passed**, with zero failures or skips: 20 new paired-recognition cases, 17 affinity-creation recognition cases, 14 frozen-pool recognition cases, 73 fixed-family confirmation cases and 18 three-reference confirmation cases. They ran through the required `build/run-tests.ps1` wrapper, using a separate artifact directory. The [retained TRX](../TestResults/affinity-preservation-recognition-native-verification-20260924/backend-tests.trx) records the complete run. Tests cover exact root boundaries and family pins, paired arithmetic, covariance, cross-profile rejection before side effects, nulls, explicit resource charges and nine reservation interruption boundaries.

**64 Python cases passed**: 23 new protocol/owner cases, one complete saved-report reconstruction/mutation case, and 40 legacy regressions. The independent saved-report test authenticated the literal archive, reconstructed every terminal report, checked that auditing left all files unchanged, and rejected reordered direct rows even without relying on an outer inventory.

The [full owned fixture](../TestResults/tower-preservation-recognition-owned-fixture-20260924-02/verification.json) passed in **253.063 seconds**, with **95,713,635 bytes** observed before writing its summary receipt. It exercised 37,120 literal reports, native worker orchestration, reservation with literal entropy, native and independent audits, publication and native post-publication verification. Every owned-process receipt reports successful exit, no timeout and zero active descendants. The tested harness DLL hash is `2577c25c5cdba2da955e1ddac2bb62e1755dab682ec24a05ea204d091e2d3f29`; the final fixture archive manifest is `52ba8e85cf1b3a01f143cade4559f49b908ff7cae5f07546817c051f0e6e18c1`.

Only synthetic inputs/outcomes/entropy, fixture command routing and the admission prerequisite were substituted. The separate fixture host uses empty content and a combat trace guard. These checks demonstrate engineering compatibility, not combat efficacy, captured-runtime compatibility or a real-combat upper bound.

The first owned fixture correctly stopped at the publication barrier: Python had re-encoded tiny variance values with a lowercase exponent, which changed the native canonical JSON hash despite numerical agreement. The corrected auditor embeds the original native result text only after every field has independently passed and the text still parses to the checked result. A regression case covers exponent spelling, integer-valued doubles, negative zero, changed values and single-use output. The failed fixture and its logs remain intact with no published result; the final fixture uses a fresh synthetic directory and the corrected auditor. The earlier native literal archive retains its original auditor bytes, because changing archived inputs would invalidate its evidence. Its arithmetic was independently recounted; the final owned fixture verifies the corrected serialization through the complete native barrier.

The initial sandboxed build could not read the user's NuGet configuration; the same wrapper succeeded after filesystem access was granted. The successful build emitted 44 warnings and zero errors, including the copied test's nullable filename-key warning. No required command remains blocked. The initial failure log is retained. All 52 historical pins and the previous 33-member handoff were authenticated; canonical plans, sampling draws and scientific archives were preserved. Only line three changed in the nine status documents.

Principal commands (`python` denotes the bundled interpreter; synthetic output names are single-use):

```powershell
$env:AFFINITY_PRESERVATION_RECOGNITION_FIXTURE = "$PWD/TestResults/tower-preservation-recognition-literal-fixture-20260924"
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAffinityPreservationRecognitionTests|FullyQualifiedName~BalanceHarnessAffinityCreationRecognitionTests|FullyQualifiedName~BalanceHarnessFrozenPoolRecognitionTests|FullyQualifiedName~BalanceHarnessFixedFamilyConfirmationTests|FullyQualifiedName~BalanceHarnessThreeReferenceConfirmationTests' -ArtifactsPath '.artifacts/preservation-recognition-native-20260924'
python -B -X utf8 build/test-affinity-preservation-recognition.py
python -B -X utf8 build/test-affinity-creation-recognition.py
python -B -X utf8 build/test-frozen-pool-recognition.py
python -B -X utf8 TestResults/affinity-preservation-recognition-native-verification-20260924/check-saved-rows.py
python -B -X utf8 build/test-affinity-preservation-recognition-owned.py --fixture-host '.artifacts/preservation-recognition-native-20260924/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll' --source-fixture 'TestResults/tower-preservation-recognition-literal-fixture-20260924' --output 'TestResults/tower-preservation-recognition-owned-fixture-20260924-02'
```

Source/runtime hashes, test logs, before/after snapshots, local report-link checks and scoped whitespace checks are retained in the [verification package](../TestResults/affinity-preservation-recognition-native-verification-20260924/verification.json). The [handoff](../TestResults/affinity-preservation-recognition-native-handoff-20260924.json) binds the sealed package and the separate admission next step.
