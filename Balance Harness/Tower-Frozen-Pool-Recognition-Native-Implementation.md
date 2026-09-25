# Frozen-pool recognition: native execution and admission

23 September 2026. The offline Balance Harness now has a distinct `tower-frozen-pool-recognition-v1` protocol for the [frozen plan](Tower-Frozen-Pool-Recognition-Plan.json). It implements execution, diagnostic results, native reconstruction, an independent Python audit, an owned launcher and captured-runtime admission. The existing 44,000- and 52,000-fight confirmation profiles retain their original contracts.

**Subsequent execution is complete:** the [real 27,648-fight diagnostic](Tower-Frozen-Pool-Recognition-Execution.md) passed both audits and publication verification. The following records the preceding implementation and no-combat admission. No real fight or combat-seed allocation occurred during that implementation step; the 27,648-report execution fixtures below are synthetic.

The plan's SHA-256 remains `f8e8b206cd6b0cf46f420ab4d6d4d4a9568e5e46186ae8a8ae18a552fa0957b8`. Its original `runnableRequest: false` remains part of its immutable planning record. The admission adapter creates a separate native definition and request; it does not rewrite the plan, sample new recipes or change the original sampling receipts.

## Execution and interpretation

There are twelve root-specific families of nine exact teams. Execution follows root, frozen team order, then common seed order. Each root receives 256 values; all nine teams use that same panel. The twelve panels are disjoint and excluded from the complete historical reservation union. There are 108 cells, 3,072 used combat values and exactly 27,648 attempted fights. A repeated party in different roots keeps a distinct `rNN-partyId` evidence, export and cache identity.

The new result has 108 win rates and 216 candidate/reference contrasts. Draws are non-wins. Signed comparisons count gains and losses on matching seeds. The approximate Wilson family is 540: 108 win rates and two binary components for each of 216 contrasts. The benchmark remains the exact `96b94357…` reference; it is never selected from the new outcomes.

Every root reports nominee, near-miss and lower-stratum summaries separately. The lower stratum uses inclusion weight 13/2. The all-17 candidate mean is `(sum of four nominee/near-miss gains + 6.5 × sum of two sampled lower gains) / 17`. These are point estimates. Individual combat intervals are not presented as confidence intervals for the finite population mean. All 132 unmeasured entries retain explicit null independent outcomes.

The only completed decision is `CompleteDiagnosticOnly`, with interpretation `DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion`. There are no qualifiers, recommendations, best-team promotion or practical-policy changes. This is a development diagnosis of the frozen pools, not a claim about performance on future roots. The adaptive configuration's existing `AbandonThisConfiguration` decision remains in effect.

## Entropy, resources and ownership

After exact recipes, producing runtime, content, settings and live history are frozen, execution makes one 24,576-byte cryptographic draw. It classifies all 6,144 signed little-endian words, removes historical collisions and within-batch duplicates, and assigns the first 3,072 fresh values to the twelve panels. Every exposed fresh value, including the unused tail, is permanently reserved. A shortfall, interrupted reservation or failed run cannot trigger a refill, resume, replacement root or retry.

The new study has its own explicit allowances. It inherits no unused campaign allowance:

| Stage | Elapsed cap | Retained storage cap |
| --- | ---: | ---: |
| Preserved first admission failure | 600 seconds | 512 MiB |
| Corrected admission, charged in full at start even on failure | 600 seconds | 512 MiB |
| Native setup, reservation, execution and cleanup | 6,000 seconds | 3 GiB |
| Both audits, publication, manifest and closeout | 1,200 seconds | 512 MiB |
| Cumulative allowance including the failed admission | 8,400 seconds | 4.5 GiB |

The initial profile allowed one 600-second/512-MiB admission. Its first concrete attempt rejected an older capture because that directory also contains a preserved failure receipt. It reached no native preparation, combat entropy or fight. The failure and full allowance remain retained in `TestResults/recognition-admission-20260923`. The explicit `--repair-capture-binding` admission profile authenticates the successful adaptive pilot's admission (`e931606aad5690e12774f5f33e2f50487d68ac802d6f21def93546973317d6a4`) and carries both admission charges in the native request. It permits one distinct repaired package/output identity. There is no refund, automatic retry or transfer into the unchanged 7,200-second/3.5-GiB scientific allowance.

The launcher holds exclusive registry/output leases through publication. Children are assigned to an owned Windows Job before they resume. Audit processes share the remaining audit deadline. Descendants must exit, both audits must agree, historical file pins must remain valid and the final native publication barrier must pass. Completion and manifest bytes, plus the terminal receipt's own bytes, are charged. A failed stage preserves its output and reservations.

Storage sampling handles the known `.pending` → published-file rename race. Ordinary missing paths, links, changed inputs, unexpected files and other I/O errors still fail. Final file authentication happens after writers have exited.

Historical sizing scales the completed adaptive pilot's 23,680 fights to 27,648 with a 1.5 multiplier. The native seconds, bytes and audit seconds must fit their separate caps. This is a planning estimate, not a guaranteed completion bound; literal fixtures do not establish real combat throughput.

## Admission and reproducibility

[prepare-frozen-pool-recognition.py](analysis/prepare-frozen-pool-recognition.py) authenticates the captured runtime package and the prior pilot inputs. It copies captured content and all gameplay dependencies unchanged, substitutes only four tested BalanceHarness files, verifies the DLL/portable-symbol identity and source checksums, and resolves the native entry points and state machines.

The compatibility step reconstructs the saved literal results, all 108 transports and 6,144-word classification. It creates 216 first/last-seed input projections using captured gameplay dependencies. The public native admission command then prepares all 108 exact teams without combat. Complete live history is reconstructed independently and checked by the native scanner; existing pending-reservation recovery receipts are carried forward and pinned.

Admission requires passing backend evidence, a complete direct-report fixture, and an owned-workflow receipt bound to the exact harness DLL. It creates a fresh request, resource forecast and sealed admission inventory. The launcher requires the external SHA-256 of that inventory and refuses a substituted runtime, launcher or owner. Native execution refreshes history again under the leases immediately before freezing and reserving.

The public native commands are:

```text
tower-frozen-pool-recognition-context <content-root>
tower-frozen-pool-recognition-check <request.json>
tower-frozen-pool-recognition-audit <archive>
tower-frozen-pool-recognition-verify <archive>
```

`-run` and `-publication-check` are owned internal workflow commands. Start a real admitted study only through `build/run-frozen-pool-recognition.py --request <admitted-request.json> --harness <admission/runtime/BalanceHarness.dll> --admission-pin <external-manifest-SHA256>`. Existing output directories are refused. Read-only verification does not reserve seeds or run combat.

## Changed files

- `LL/tools/BalanceHarness/TowerFrozenPoolRecognition.cs` defines the immutable profile, diagnostic estimands, null outcomes and exports; `TowerFrozenPoolRecognitionRun.cs` implements the owned worker and audit/publication commands.
- `TowerFixedFamilyConfirmation.cs`, `...Protocol.cs`, `...Run.cs`, `...Reservation.cs` and `...Archive.cs` dispatch profile-specific team counts, panel sizes, root identities and frozen bindings while preserving the historical versions. `Program.cs` routes the new command prefix.
- `Balance Harness/analysis/audit-frozen-pool-recognition.py` independently reconstructs direct compressed reports, pairing, reservation words, journals, transports, exports and every result field. `prepare-frozen-pool-recognition.py` and `frozen-pool-recognition-context.ps1` implement admission and captured compatibility.
- `build/run-frozen-pool-recognition.py` owns execution, both audits and publication. `build/test-frozen-pool-recognition.py` tests semantics and operational failures; `build/test-frozen-pool-recognition-owned.py` exercises the complete real-process workflow with literal reports.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessFrozenPoolRecognitionTests.cs` covers native pairing, weighting, reservation interruption and archive reconstruction. The separate `BalanceHarness.ProcessFixture` executable supplies literal outcomes/entropy and guards against combat; no production CLI can select those fixtures.
- The harness README, practical-search guide and builder report link this implementation status.

No database migration, application configuration change or deployment is involved. All retained combat evidence and unrelated application changes remain untouched. The pending 52,000-fight confirmation is a separate study.

## Verification

The [compatibility regression run](../TestResults/recognition-backend-20260923.log) passed **104 tests** across the new diagnostic and both existing fixed-family profiles. After adding explicit preservation of the failed admission charge, the [final native run](../TestResults/recognition-repaired-backend-20260923.log) passed **14 tests**. These counts describe separate runs and are not a count of unique tests.

The [final Python run](../TestResults/recognition-repaired-python-final-20260923.log) passed **23 tests**, including a direct recount of all 27,648 native fixture reports. Tests cover root identity, within-root pairing, cross-root exclusions, missing/faulted/reordered trials, signed entropy values, collisions, permanent unused reservations, weighted estimates, explicit nulls, captured dependency substitution, preserved recovery receipts, external admission pins, resource exhaustion, audit failure, mutation before publication, interrupted final writes and descendant cleanup. Native tests exercise all nine reservation interruption boundaries without permitting another draw.

The [complete owned-process fixture](../TestResults/tower-recognition-owned-fixture-repaired-20260923/verification.json) passed using the final harness DLL, SHA-256 `8659224ad7ee2e47faab61fb8a4221b621a075ac0113ba2586975428ffe78956`. It runs the actual owned native worker, complete reservation and archive, native reconstruction, independent Python audit, native publication barrier and native post-publication verification. It exercises the amended two-charge accounting. The fixture supplies literal combat inputs/outcomes and entropy in the separate test executable; a trace guard throws on any simulator call. This is complete operational evidence with **zero actual combat and zero production entropy draws**, not evidence of candidate strength or a real-combat runtime bound.

The unchanged [frozen plan was reverified](../TestResults/recognition-plan-reverification-20260923.log). The initial build could not read the user NuGet configuration inside the sandbox; rerunning the required wrapper with ordinary filesystem access passed. The first engineering compatibility attempt exposed a PowerShell reflection argument conversion issue; an explicit string conversion fixed it, and the corrected compatibility check passed. These failures were retained rather than rewritten as successful attempts.

Principal verification commands (`python` means the bundled interpreter):

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessFrozenPoolRecognitionTests|FullyQualifiedName~BalanceHarnessThreeReferenceConfirmationTests|FullyQualifiedName~BalanceHarnessFixedFamilyConfirmationTests' -ArtifactsPath 'TestResults/recognition-build-20260923'
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessFrozenPoolRecognitionTests' -ArtifactsPath 'TestResults/recognition-repaired-build-20260923'
python -B -X utf8 'build/test-frozen-pool-recognition.py' --fixture 'TestResults/recognition-repaired-fixture-20260923'
python -B -X utf8 'build/test-frozen-pool-recognition-owned.py' --fixture-host 'TestResults/recognition-repaired-build-20260923/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll' --source-fixture 'TestResults/recognition-final-fixture-20260923' --output 'TestResults/tower-recognition-owned-fixture-repaired-20260923'
```

The full native fixture test retained its output via `FROZEN_POOL_RECOGNITION_FIXTURE`. Existing fixture, admission and scientific output directories are never reused. Commands above that create artifacts are historical, single-use executions; choose fresh synthetic output names for new engineering tests.

## Concrete admission

The [corrected admission](../TestResults/recognition-admission-repaired-20260923/admission.json) passed in **157.140 seconds**, retaining **51,670,166 bytes**. It authenticated **213 producing source documents**, resolved **554 native methods**, reconstructed all literal compatibility projections and prepared all **108 teams**. Both history implementations agreed on **666,076 reserved values across 244 files**. The [native check](../TestResults/recognition-admission-repaired-20260923/native-check.json) reports `ContractValidNoReservation`, with zero fights and zero new values.

| Binding | SHA-256 |
| --- | --- |
| [Admission manifest](../TestResults/recognition-admission-repaired-20260923/files.json) | `1c57b77e14fff65453a33bcc170b0eb7c010cda86c91b53855f7268dcbf19556` |
| [Admitted request](../TestResults/recognition-admission-repaired-20260923/request.json) | `766de5619a0a419ee7270e33fe68d88b94f902b51bcefc3e484179ad0e2e02db` |
| [Native definition](../TestResults/recognition-admission-repaired-20260923/definition.json) | `d8546cae6c226e43712dfa96f9fc88c28c0ddd10a259c0e33bb9045901b729ce` |
| Captured execution identity with final harness | `32ea2c2a7a8996443bfb5dcec93f0f846e9635cf56716c78e1c73191a6394b3f` |

The [repair declaration](../TestResults/recognition-admission-repaired-20260923/repair-declaration.json) and copied preceding failure/charge are inside that manifest. The admitted request charges **1,200 seconds / 1 GiB** for the two admissions, leaving the unchanged **7,200 seconds / 3.5 GiB** scientific allowance. The [resource forecast](../TestResults/recognition-admission-repaired-20260923/resource-forecast.json) estimates 1,730.678 native seconds, 2,915,079,458 native bytes and 248.212 audit seconds after the 1.5 scaling margin. These estimates fit the caps but are not guaranteed upper bounds.

The separate [sealed-package and live-history verification](../TestResults/recognition-admission-verification-20260923.log) also passed in **47.641 seconds**, with an [owned process receipt](../TestResults/recognition-admission-verification-process-20260923.json) recording zero active descendants and no timeout. It made no new native preparations, fights or allocations. No required verification command remains blocked.

Read-only package and live-history verification can be repeated without preparing teams or reserving values:

```powershell
python -B -X utf8 'TestResults/recognition-admission-repaired-20260923/prepare.py' verify --package 'TestResults/recognition-admission-repaired-20260923' --manifest-sha256 1c57b77e14fff65453a33bcc170b0eb7c010cda86c91b53855f7268dcbf19556 --live
```

The admitted scientific output is `TestResults/balance/tower-frozen-pool-recognition-repaired-20260923`; it was absent at admission and was subsequently created by the single completed launch. The [execution report](Tower-Frozen-Pool-Recognition-Execution.md) records 6,144 permanent reservations, 3,072 used values, both audits and the resulting diagnosis. The result cannot promote a team or search policy.
