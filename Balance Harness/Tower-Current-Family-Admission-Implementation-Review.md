# Current-family Tower admission: implementation and fixture verification

**Subsequent readiness complete — 17 September 2026:** the [runtime capture and bounded request](Tower-Current-Family-Admission-Readiness-Review.md) resolve the isolated test-build mismatch by retaining the original gameplay DLLs with the new harness. That combination passed 45 adapter fixtures and static inventory loading; a single 1,800-second /2-GiB enclosing admission is prepared but has not run. The implementation-close observations and sealed snapshots below retain their original scope.

17 September 2026. Target: offline `LL/tools/BalanceHarness` and `LL/tests/EssenceSystem.Tests`. **The seed-free admission adapter is implemented and fixture-tested. The real 46,077-input family has not been prepared.** No new combat, fresh values, reservations, gameplay/content changes or cap increases occurred. Candidate `399bc776…` remains **AdoptFixedTeam**; balance remains **NotAssessed**, V19 **Unresolved**, and all **497,371 exclusions** remain preserved.

The adapter consumes the unchanged [sealed calibration design](Tower-Current-Gameplay-Calibration-Design.md), whose manifest hash is `bf454b4228637406847c10186e75a277bb47d825cc77a10fa6011221adf656b6`. It has no screen, confirmation, allocation or resume route. This implementation establishes the admission mechanics; it does not establish full-family native legality or execution feasibility.

## Implemented behavior

| Part | Contract |
| --- | --- |
| Inputs | Exact sealed template, 46,077 canonical input projections, 51,624 origin occurrences, 973 forced input cells and all 162 context exceptions. All three confirmed control party IDs are mandatory and checked against their Essence vectors. |
| Production preparation | Reuses `TowerBattleRunner.CreateInput` and `PrepareAsync`. Exported scenarios keep empty seed arrays. The native API receives the existing fixed preparation-only sentinel `0`, as in earlier seedless checks; this is not an entropy draw, reservation or combat panel. A combat-entry guard aborts immediately. |
| Runtime/content | Binds a new request execution hash and the producing harness; requires the original design hashes for `Application`, `Common`, `Domain`, `Services.LL`, all 16 content files and effective settings. These are checked before and after preparation. |
| Evidence | One streamed compressed row per projected input, including rejected inputs. Retains origin ordinals, mandatory reasons, scenario/native identity, input and participant hashes, complete prepared participant descriptions and native alias targets. Original origin records and exceptions are copied unchanged. |
| Alias handling | Same native cell identity may merge only when both prepared input and participant hashes match. Every input row and contributing origin remains visible. Conflicting aliases become explicit invalid rows and block readiness. |
| Saved audit | Reconstructs counts, native identity, alias/origin/control mapping and readiness from saved rows, verifies participant hashes and exact output inventory, and validates the producing execution contract. It performs no second production preparation or combat. It is a saved-evidence consistency audit, not independent proof of native gameplay behavior. |
| Resource stops | Request limits can be lower than the hard ceiling of 1,800 seconds /2 GiB. Output writes and copies check projected growth, with 64 KiB reserved for closeout. Cancellation/deadline/storage failures retain partial evidence and a failure marker; completed verification rejects stopped output. Time checks are cooperative; a future execution wrapper must enforce the enclosing wall/output allowance, including captured runtime and logs. |
| Compatibility | Existing retained, captured-family, fixed-team and generic staged controllers and their limits are unchanged. Only the new command prefix is routed here. Existing outputs are refused; no retry/resume override exists. |

If every projection prepares, the current input package still returns **`AdmittedWithContextExceptions`**, exit **3**, with `ReadyForFamilyFreeze=false`: its **162 incompatible occurrences** have not been adjudicated. A projected-input failure returns **`AdmissionIssues`**, exit **1**. Only a completely admitted inventory without outstanding exceptions can return `Admitted` /exit **0**; the pinned current package cannot reach that status by silently discarding exceptions. Exceptions include the non-neutral historical contexts and later incompatible forced-control occurrences described in the design. No ignore-exceptions flag was added.

## Public interface

```text
BalanceHarness tower-current-family-admit <request.json> <new-output>
BalanceHarness tower-current-family-admission-verify <completed-output>
```

`TowerCurrentAdmissionRequest` requires `version` (`tower-current-family-admission-v1`), `designRoot`, `contentRoot`, `executionHash`, `maximumSeconds` and `maximumBytes`. Unknown fields and out-of-range limits are rejected. Output must be new, outside input roots and free of linked path components. The first command copies and authenticates the design inputs, prepares projections once, audits saved rows and seals the result. The second uses only the completed output, so it does not need the original source/content directories or load the producing gameplay runtime.

**No runnable real-family request was created in this step.** A rebuilt harness has a different identity from the design's retained harness. All four gameplay DLLs in this isolated test build also have different binary hashes from the design, despite preservation of gameplay source; this build directory therefore cannot pass the native admission runtime gate as it stands. Capture the new harness with the retained original gameplay assemblies and pinned content, check compatibility, and bind the resulting execution identity before constructing the request. Preserve both old evidence and test output. The saved verifier authenticates internal bindings and hashes; an enclosing receipt must pin the completed manifest for later trusted use.

## Verification and retained evidence

**172 distinct focused cases passed.** The initial regression run passed **162/162** (35 new adapter cases plus 127 existing retained-family, fixed-team confirmation and complete-family cases). After producer-binding checks and nullable annotations were finalized, the adapter run passed **45/45**, including ten additional producer-identity cases. The repeated 35 cases are counted once. The final adapter build has **zero new adapter warnings**; nine existing test-source warnings remain.

The new cases cover complete saved-audit parity, origins, forced controls, canonical ordering, aliases and alias conflicts, invalid native preparation, incompatible contexts, changed cohorts, missing/extra/tampered rows, strict JSON, producer/gameplay binding, combat rejection, cancellation, deadline and byte stops, and preservation of old captured-family limits. Synthetic fixtures exercise these contracts; they do not substitute for the deferred real-family admission.

Both completed runs used the repository wrapper; these are their recorded commands. Any future test work should use a new artifacts directory and retain its own results.

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessCurrentFamilyAdmissionTests|FullyQualifiedName~BalanceHarnessTowerRetainedAuditTests|FullyQualifiedName~BalanceHarnessFixedTeamConfirmationTests|FullyQualifiedName~BalanceHarnessTowerCompleteFamilyTests' -ArtifactsPath 'TestResults/current-tower-admission-implementation-20260917/test-build'
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessCurrentFamilyAdmissionTests' -ArtifactsPath 'TestResults/current-tower-admission-implementation-20260917/test-build'
```

The first sandboxed attempt stopped before compilation because access to the existing user NuGet configuration was denied. The same wrapper succeeded with approved access; the failed log is retained. No required verification command remains blocked. The [regression TRX](../TestResults/current-tower-admission-implementation-20260917/regression-tests.trx), [final adapter TRX](../TestResults/current-tower-admission-implementation-20260917/adapter-tests.trx), build logs and preservation checks are retained in the [implementation package](../TestResults/current-tower-admission-implementation-20260917). Build products are engineering output, not a captured admission runtime or scientific result.

The [engineering checker](../TestResults/current-tower-admission-implementation-20260917/finish-checks.py) and [verification receipt](../TestResults/current-tower-admission-implementation-20260917/final-checks.json) covered every static vector/origin/control mapping, all sealed design members, the 22,806-file pre-edit preservation baseline, local Markdown links, source pins, test counts and whitespace at implementation close. **22,791 baseline files were byte-identical**; only `Program.cs` and 14 active Markdown handoffs differed. The [completion receipt](../TestResults/current-tower-admission-implementation-20260917/completion.json) seals **31 review files**, with manifest SHA-256 `23c465c2ee6a05bbeaf2277ee47d2e033276f4ee3a027bf6b4f705433a87f5ba`. The seal excludes the isolated `test-build` products, which remain available separately.

The saved [source and documentation snapshots](../TestResults/current-tower-admission-implementation-20260917/source-snapshot) preserve the exact implementation-close state. Later working-document updates do not replace those snapshots or refresh historical pins. `finish-checks.py` writes reports and source pins, so do not rerun it inside the completed package. The design's old live-source reader also predates the adapter. Use the following seal check for this already completed package; the completion guard ensures the script takes its read-only verification branch:

```powershell
$reviewRoot = 'TestResults/current-tower-admission-implementation-20260917'
if (-not (Test-Path -LiteralPath "$reviewRoot/completion.json")) { throw 'Completed review receipt is required.' }
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 "$reviewRoot/seal-review.py"
```

## Changed files and next step

Added [admission and saved-row logic](../LL/tools/BalanceHarness/TowerCurrentFamilyAdmission.cs), [bounded execution and public commands](../LL/tools/BalanceHarness/TowerCurrentFamilyAdmissionRun.cs), and [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessCurrentFamilyAdmissionTests.cs). Added two dispatch/help lines to [Program.cs](../LL/tools/BalanceHarness/Program.cs), this review and its engineering evidence; updated 14 active Markdown handoffs. The complete origin/exception ledger and a separate versioned admission route keep the existing scientific contracts intact.

**Next: capture/pin the new adapter runtime and prepare the bounded real native admission request**, including enclosing resource accounting and an explicit disposition process for the 162 incompatible occurrences. A later authorized admission can then establish native identities and measured preparation cost. Family freeze remains blocked until context coverage is resolved. Screen/confirmation implementation, seed allocation, tuning and their execution gates remain separate future work; no completion estimate or balance Pass is implied.

No migration, dependency, application configuration or deployment change is required. The new offline commands need a rebuilt harness; no service was deployed and no database was changed.
