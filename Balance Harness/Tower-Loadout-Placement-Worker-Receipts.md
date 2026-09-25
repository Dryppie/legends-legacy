# Loadout placement: bound worker receipts

Current status: opt-in worker receipt emission and diagnostic owner retention are implemented. Whole-process coverage remains incomplete; the compressed launch guards and the 1,806/1,800-second resource gate remain closed.

The native study run, native audit and publication-check commands accept an optional `--work-binding <absolute-path> <external-sha256>` suffix. The independent auditor accepts `--work-binding <absolute-path> --work-binding-pin <external-sha256>`. Existing invocations retain their behavior. This extension does not supply admission, process ownership, lease ownership or permission to launch a study.

## Contract and implementation

`TowerProposalWorkReceipt.cs` and `proposal_work_accounting.worker_receipt` authenticate the new `tower-proposal-worker-binding-v1` contract. It binds the phase, absolute study root, exact request bytes, executing producer, accounting module and absolute receipt destination. Native producer/module identity is the loaded BalanceHarness assembly; the independent auditor separately binds its script and accounting module. This is file-hash binding, not signed attestation or complete runtime identity; existing runtime admission remains necessary.

Bindings have an externally supplied SHA-256, a 16 KiB size limit, required exact fields and duplicate/unknown-key rejection. Binding and receipt paths must be outside the study archive, without linked ancestors. The receipt uses exclusive creation before work begins, so reused output fails before the action. Request and producer identities are checked again after successful work; Python also rechecks its accounting module. A changed identity produces a failed receipt.

Only the worker action activates counters. It includes the original command's result publication, though only previously instrumented operations contribute counters. Binding validation and receipt serialization, flush and close require an enclosing accounting boundary. Native receipts use durable flush; Python receipts handle short writes, flush and fsync. Failure and cancellation retain partial counters. If receipt publication also fails, the original worker exception remains primary with the publication failure attached. A successful action cannot silently ignore publication failure. Missing, partial or unbound output cannot be treated as a valid receipt.

`TowerProposalStudyRun.cs` routes the optional suffix around the original command; its run, audit, publication, admission and lease logic are unchanged. The independent auditor now exposes a testable `main` and wraps its original audit/output path only when both binding options are present. Its normal output bytes remain unchanged. Every emitted counter receipt retains `wholeProcessCoverage=false` and `usableForAdmission=false`.

`RetainedOwner.run_worker` creates and retains the binding for its active phase, invokes the caller's explicit worker callback, authenticates the resulting receipt, then copies its exact bytes into managed owner storage. It retains valid failed receipts, records missing or invalid receipts, and preserves an invocation exception when retention also fails. Process ownership and drain observations remain required independently. The ordinary scientific launcher does not activate this diagnostic integration.

## Verification

The fresh [verification package](../TestResults/loadout-placement-worker-receipts-verification-20260924) contains before/after source snapshots, attempted and final logs, backend TRX results, native receipt exchanges and diagnostic owner/process evidence. No historical package was mutated.

Verification passes **74 distinct backend tests** and **110 Python tests**. The 22 new backend cases cover all three native phases, binding mutations, duplicates, existing output, async failure, cancellation, post-action request mutation, nested collector restoration and actual command rejection of invalid literal inputs. Combat is forbidden by the tests. The remaining 52 backend regressions cover existing counters and native proposal verification.

The 18 new Python tests cover equivalent binding and failure boundaries, unchanged auditor output, receipt publication failures, owner retention errors, a real independent-auditor rejection, and a real native-audit rejection in tiny owned Windows Jobs. Four additional literal worker invocations exercise ordered owner phases and cleanup: all four receipts survive deletion of their external originals. The native exchange is independently authenticated against its external manifest pin, original request hash and actual assembly hash; its successful literal action records exactly 16 JSON read bytes. These are correctness checks, not scientific preparation or resource timing measurements. Existing Python collector/owner/process tests (38), owner-file tests (22), owner arithmetic/guards (25), and selected-member provenance tests (7) also pass.

The first backend build encountered the known sandbox denial for the user's NuGet.Config. The approved retry ran 74 tests and exposed one fixture error from assigning a parented JsonNode twice. That fixture was corrected; all 22 worker-receipt tests then passed against the rebuilt isolated test assembly. The 52 unaffected regressions passed in the first run. Logs and both TRX files preserve this sequence; counts do not double-count repeated tests. No required command remains blocked.

```text
build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessWorkerReceiptTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessProposalNativeTests' -ArtifactsPath .artifacts/loadout-worker-receipts-20260924
build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessWorkerReceiptTests' -ArtifactsPath .artifacts/loadout-worker-receipts-20260924
python -B -X utf8 build/test-proposal-worker-receipts.py -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 build/test-proposal-owner-files.py -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
```

Export variables designate new directories; the cross-language check also requires the external manifest pin and isolated native DLL. These are records of completed verification, not permission to modify sealed packages.

## Remaining work and operational effect

Integrate the diagnostic binding/retention path with owner file counters and process observations across the enclosing lifecycle. Close binding verification, receipt/manifest publication and lease-cleanup tails, and account for remaining reads, parsing/materialization, owner memory and transient scratch. Native audit success still needs a fresh fixture through the complete production audit path. The independent CLI output writer remains an uninstrumented write boundary. The diagnostic owner's terminal publication remains excluded. No complete process resource forecast or compression speedup is established.

Both compressed guards and the failed resource gate remain unchanged. A separately justified prospective replacement model is required before measurement. The first failed pair stays failed; all history, reservations and cumulative charges are preserved. There were no scientific native preparations, qualifications, campaigns, production entropy draws, live-history scans, migrations, configuration changes or deployments. Only isolated test binaries were rebuilt; no retained runtime was replaced. Any future admissible use of changed producer/auditor code requires freshly authenticated admission and module bindings. Only line 3 of the nine status documents changes; their historical bodies remain byte-identical.
