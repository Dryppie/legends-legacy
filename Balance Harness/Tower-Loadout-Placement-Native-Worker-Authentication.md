# Loadout placement: native worker authentication accounting

Current status: bound native workers count binding reads/parsing and request/assembly authentication before and after work. Verification passes 123 backend and 154 Python tests. Whole-process coverage remains incomplete; the compressed launch guards and the 1,806/1,800-second resource gate stay closed.

## Implementation

`LL/tools/BalanceHarness/TowerProposalWorkReceipt.cs` activates the worker collector before binding authentication. Its binding read passes through the existing counted stream, while JSON parser input and completion are recorded separately. The original strict byte deserializer is preserved, including duplicate-property, unknown-field, required-constructor, case-sensitivity and malformed-byte behavior. Binding rejection still precedes worker invocation and receipt creation.

Both authentication passes now charge their actual request and assembly file reads through the existing hash helper. Attempted, completed and failed authentications are separate counters. A request mismatch or missing request prevents the later assembly read, so that unperformed read is not charged. The native producer and accounting module are the same assembly; one assembly read authenticates both identities in each pass.

The collector remains active across the awaited action and final authentication. Its scope restores the enclosing collector before receipt publication, and on every rejection or failure. Nested asynchronous collectors retain their own work without stealing the worker's authentication counters. The receipt schema, exclusive reservation before work, original-error behavior, command dispatch and unbound defaults are unchanged. The receipt's own serialization, write, flush and close remain explicitly outside its snapshot.

`LL/tests/EssenceSystem.Tests/BalanceHarnessWorkerReceiptTests.cs` adds eight cases and strengthens existing assertions for exact reads, parser input, all three native phases, async/nested scope isolation, missing or changed requests, early rejection, original errors and cancellation. Its fresh exchange metadata identifies the expanded native authentication coverage. `build/test-proposal-worker-receipts.py` verifies that coverage against the actual binding, request and assembly lengths; older exports keep their original narrower expectations.

## Verification

The fresh [verification package](../TestResults/loadout-placement-native-worker-auth-verification-20260924) retains before/after source snapshots, build logs, test receipts and literal fixtures. Builds use only `.artifacts/loadout-native-worker-auth-20260924`; older runtimes and packages remain immutable.

The selected backend suites pass **123 distinct tests**: worker receipts (30), native file accounting (30), content accounting (23), native writes (22) and work counters (18). After correcting an analyzer warning in a new test assertion, the 30 worker-receipt cases passed again against the final test source. The other 93 cases were unchanged. Initial and final TRX files and both native fixture exports are retained; `native-exchange-final` is the final verification input. Existing unrelated build warnings remain.

The final native fixture performs a literal request read under the real worker boundary. Its receipt records 607 JSON file bytes: the 559-byte binding plus three 16-byte request reads, including the action read. Parser input is 575 bytes, and two authentication passes read 10,447,872 assembly bytes. Python independently checks those counts against file lengths and authenticates the request, binding, producer and manifest hashes. The receipt remains incomplete and non-admitting. This is not a full native production-audit success fixture and contains no combat or scientific preparation.

All eight Python groups pass: worker I/O (16), worker receipts (18), owner supervisor (15), owner closeout (13), collector/process (38), owner files (22), owner arithmetic/guards (25) and provenance (7), totaling **154 distinct tests**, with no skips. The historical native exchange additionally passes the same reader check; that repeated case is not counted twice. Existing tiny-process tests exercise real Windows process ownership and cleanup. The supervisor fixture still routes synthetic workers and remains labeled accordingly.

```text
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-native-worker-auth-20260924 -Filter <five accounting/worker suites>
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-native-worker-auth-20260924 -Filter FullyQualifiedName~BalanceHarnessWorkerReceiptTests
python -B -X utf8 build/test-proposal-worker-io.py -v
python -B -X utf8 build/test-proposal-worker-receipts.py -v
python -B -X utf8 build/test-proposal-owner-supervisor.py -v
python -B -X utf8 build/test-proposal-owner-closeout.py -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 build/test-proposal-owner-files.py -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
git diff --check -- <changed files>
```

The first sandboxed build could not read the user NuGet configuration. Its authorized retry and the final targeted run completed; no required command remains blocked. Export variables designated fresh destinations and the Python exchange check used the final external manifest pin. These are completed verification commands, not instructions to rerun mutation tests inside sealed evidence.

## Remaining work and operational effect

Outstanding boundaries include worker receipt publication, supervisor-observation persistence, console output and process exit, whole-owner memory lifetime, transient and external scratch storage, remaining owner/native reads and parsing, and metadata/path-check and lease-handle operations. A fresh full native production-audit success fixture is still needed. Content-byte counters do not establish complete operating-system I/O, memory or scratch coverage. All observations remain `wholeProcessCoverage=false` and `usableForAdmission=false`.

A separately justified prospective replacement resource model is required before measurement. No scientific launch, native preparation, qualification, production entropy draw, scientific reservation, timing pair, live-history scan or combat occurred. The first failed pair stays failed; history and cumulative charges are unchanged. No migrations, application configuration changes, deployment or retained-runtime replacement occurred. Future accounting use must authenticate a compatible newly built native producer. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.
