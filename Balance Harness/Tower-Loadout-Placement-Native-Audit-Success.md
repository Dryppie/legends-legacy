# Loadout placement: full native audit success fixture

Current status: a fresh compressed fixture passes the production native audit directly and through the unchanged native CLI under an enclosing owner job. Verification passes 193 backend tests and 283 Python tests. Remaining operation and scratch coverage is incomplete; both compressed launch guards and the 1,806/1,800-second resource gate remain closed.

## What changed

`LL/tests/EssenceSystem.Tests/BalanceHarnessNativeAuditSuccessTests.cs` builds a complete, explicitly literal placement-study archive. It captures repository content, derives the full inventory and generation mechanics, retains the producing runtime, and materializes actual `TowerBattleRunner.CreateInput` identities. It writes compressed native search/study evidence, recipe and battle archives, attempt journals, freeze and provisional result through the existing formats. Deterministic bytes provide synthetic reservation evidence; the fixture never calls `Reserve`, `Inspect`, `RunOwned`, encounter `PrepareAsync`, a combat evaluator or production randomness.

Only outcome generation is injected during construction. Every outcome is a fabricated draw. The verification calls the real `TowerProposalStudy.Audit`, including production content/runtime validation, reservation reconstruction, all 24 `TowerProposalRacingNative.VerifyAsync` calls, input hashes and cache keys, compressed evidence reading, held-out reconstruction and endpoint comparison. No audit callback is replaced. The audit reconstructs **12 roots, 24 trajectories, 12 placement catalogues, 15,744 trial bindings and one study endpoint**. The bindings comprise 12,672 search records and 3,072 held-out records. All three logical outputs merge into one physical held-out member per root, so this fixture covers 12 physical members; it does not cover the maximum 36-member layout.

`build/test-proposal-native-audit-success.py` authenticates that sealed fixture and runs the actual retained `BalanceHarness.dll tower-proposal-study-audit` command. A trusted diagnostic Python driver creates a real nested worker job inside `OwnerProcessMonitor`'s enclosing Windows job. It validates the native v3 work receipt, receipt-publication observation and observation-persistence receipt, compares reconstruction counts and the complete result, then verifies the input manifest again. The outer job observes the driver's authentication, native child, receipt tail, driver persistence, console and exit. Nested kernel I/O totals overlap; they are not added together or equated with application counters or physical disk bytes.

Two negative cases use fresh private copies. Adding whitespace to captured content fails the production content-hash check. Changing a trial's input hash and resealing its archive manifest passes the file-hash check but fails production input authentication. Each native worker and diagnostic owner exits unsuccessfully while receipt publication and process observation remain complete. The immutable success fixture is verified before and after the cases. Exact changed members and the mutated manifest are retained so the rejected copies can be reconstructed.

Production code, accounting modules, launch defaults, admission logic, codecs and process helpers are unchanged. Only the test, its diagnostic driver, this report, scoped LF attributes and line 3 of the nine existing status documents change. Historical document bodies remain byte-identical.

## Verification

The [verification package](../TestResults/loadout-placement-native-audit-success-verification-20260924) retains the fresh native fixture, actual process observations, v3 receipts, mutation evidence, test logs, TRX files, source snapshots and pinned runtimes. The [handoff](../TestResults/loadout-placement-native-audit-success-handoff-20260924.json) records continuation constraints and unchanged cumulative accounting.

Initial authentication checked 437 historical pins, 136 current source hashes and 62 isolated runtime files. The new full-audit backend test passes, as do the 192 existing receipt/publication/read/write/content accounting tests: **193 backend tests**. The three new native process cases and all 280 preceding Python regressions pass: **283 Python tests**, with no skips. The Python regressions reuse authenticated prior native exchanges; the new audit cases use the fresh fixture's retained runtime.

```text
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-native-audit-success-initial-20260924 -Filter 'FullyQualifiedName~BalanceHarnessNativeAuditSuccessTests'
./build/run-tests.ps1 -NoBuild -ArtifactsPath .artifacts/loadout-native-audit-success-initial-20260924 -Filter 'FullyQualifiedName~BalanceHarnessReceiptPublicationTests|FullyQualifiedName~BalanceHarnessWorkerReceiptTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessWriteAccountingTests|FullyQualifiedName~BalanceHarnessFileAccountingTests|FullyQualifiedName~BalanceHarnessContentAccountingTests'
python -B -X utf8 build/test-proposal-native-audit-success.py -v
python -B -X utf8 TestResults/loadout-placement-native-audit-success-verification-20260924/run-verification.py
git diff --check -- <changed files>
```

The native process test requires `LL_NATIVE_AUDIT_FIXTURE`, its external `LL_NATIVE_AUDIT_FIXTURE_PIN`, and optionally a fresh `LL_NATIVE_AUDIT_PROCESS_EXPORT`. The backend export uses `LL_NATIVE_AUDIT_SUCCESS_EXPORT`; existing destinations are rejected. Exact commands, environment and native arguments are retained in `commands.json` and `test-runs.json`.

The first sandboxed build could not read the user NuGet configuration. Its authorized retry succeeded with 45 warnings and no errors; the new test contributes one nullable warning on the callback-populated trial binding. The regression run reused that isolated build through the required test runner. No required command remains blocked. Process/test durations are correctness observations, not resource qualification samples or a replacement forecast.

## Boundary and next step

This closes the missing full **native audit success** case. The enclosing owner here is a diagnostic driver for that audit phase. It does not execute the full scientific launcher, independent auditor, publication or admission pipeline. The fixture's request supplies structural audit bindings, not a runnable admitted scientific package. Literal draws establish audit/receipt correctness only; they establish no team strength, search improvement or compression speedup.

Fixture construction and mutation-copy preparation occur outside the enclosing owner observation. The outer monitor still excludes its own preparation, later persistence/verification, console and exit. Complete application-operation attribution and external/transient scratch coverage remain unresolved; the terminal persistence receipt also excludes its own writes. The next implementation step is a concrete inventory of remaining reads/parsing, metadata/lease operations and scratch lifetimes, followed by a prospective resource model with explicit boundaries and failure treatment before any measurement.

The first failed pair remains failed. Recorded cumulative charges remain **79,339.66095319996 seconds** and **51,980,910,910 bytes**; declared maxima remain **168,240 seconds** and **107,122,524,160 bytes**. The audit floor of 1,806 seconds still exceeds the 1,800-second limit. No scientific launch, qualification, timing pair, live-history scan, production entropy draw, scientific reservation, encounter preparation or combat occurred. Input materialization for literal correctness evidence did occur. No current resource forecast or scientific admission is established.

There are no database migrations, application configuration changes or deployments. Historical runtime captures and sealed evidence remain unchanged. Further measurement requires a separately justified prospective replacement resource model; the existing compressed guards remain closed.
