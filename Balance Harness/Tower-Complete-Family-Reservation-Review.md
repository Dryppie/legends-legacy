# Complete retained-family approved reservation — measured completion review

**ReservedAndBoundNoCombat — 15 September 2026.** The user's “Please proceed” approved exactly **288 new values with zero fights**, under the frozen ten-minute/64-MiB/zero-retry limits. The approved captured command executed once and published its protocol. Independent verification passes. **32 first-stage + 256 second-stage values are durably reserved; total history is 481,891.** All previous 481,603 exclusions and the original unused 512 remain intact. No combat preparation, fight, retry, resume or full study occurred. Reliability remains **Fail 1/3**, adoption **Hold**; original v19 remains sealed Unresolved with all 253 recipes and no confirmation inside that experiment.

The [execution protocol](Tower-Complete-Family-Reservation-Protocol.md), [frozen scope](../TestResults/balance/tower-complete-family-reservation-20260915/scope.json) and [approved request](../TestResults/balance/tower-complete-family-reservation-request-20260915.json) identify this operation. Request SHA-256: `bc264a3468b7a127ca7f8d27ba15ab9d9dfb87eba394a54d565d4dfa6e621a5c`. The unchanged captured executable is from the preceding request package, seal `5ddd0e9d91babb95759c0f68a2ae713ceb5ddf5323a218be64bff984202aed7d`.

## What completed

The [complete reservation ledger](../TestResults/balance/tower-complete-family-reserved-20260915/seed-ledger.json), [exact schedules](../TestResults/balance/tower-complete-family-reserved-20260915/seeds.json), [durable transcript](../TestResults/balance/tower-complete-family-reserved-20260915/allocation-journal.jsonl) and [published protocol](../TestResults/balance/tower-complete-family-reserved-20260915/protocol.json) are retained in the new study directory. The command wrote intent and Pending history before allocation, durable starts/candidates, complete schedules and receipts, then published the protocol last. The independent audit reconstructs only recorded derivations and verifies **576 paired journal rows**, **288 candidates**, **0 rejections**, exact ordering, exclusions, completion markers and every frozen file.

The allocator remains `sha256-us-int32le-reject-v1`, domain `tower-captured-v19-complete-family-v1`, master **2026091423**, first/second labels, counts 32/256 and at most 100,000 candidates per stage. All 288 reserved values are distinct and outside the complete old history. No additional candidate beyond the recorded ordinals was derived by independent verification. The global registry now contains **147 paths**: the unchanged original 145 plus the new study's `history-input.json` and `seed-ledger.json`.

| Measurement | Result |
| --- | --- |
| Approved-input preflight | 1.390 s; 5,391 effective setup files / 1,559,084,956 bytes |
| Native command wall time | **93.656 s**; exit 0 |
| Native internal completion time | 93.440 s |
| Independent audit | **54.391 s** |
| New reservations | **288 = 32 + 256**, zero rejections |
| Complete reserved union | **481,891**, including all old exclusions and the unused original 512 |
| Native study output | **16,662,786 bytes / 10 files** |
| Predecessor preservation | **11 sealed packages / 13,999 indexed files** |
| Checkout preservation | 4,306 baseline files; 0 observed UI changes left untouched |
| Combat preparations / fights / retries / resumes | **0 / 0 / 0 / 0** |

The [execution result](../TestResults/balance/tower-complete-family-reservation-20260915/execution-result.json), [native receipt](../TestResults/balance/tower-complete-family-reservation-20260915/native-result.json), [independent audit](../TestResults/balance/tower-complete-family-reservation-20260915/audit-result.json) and [study file/timestamp inventory](../TestResults/balance/tower-complete-family-reservation-20260915/study-files.json) preserve measurements. The actual native reservation result contains elapsed time, candidate/rejection counts and protocol hash; it does not persist the in-memory detailed phase trace. Saved filesystem timestamps are retained as boundary observations, not precise phase timings. No historical timing percentage or performance speedup is inferred.

Complete reserved-union hash: `22dd529beeacf77f93311ddf0ab2421e3fd1850daddd78e669518250e1b616bb`. First schedule hash: `ecdcca334a5d547d05a647fef7a92c5fb425621de81e885cc9cb633afb7a215f`. Second schedule hash: `e295009b4a333f1fa71f2297bf1ff2c2b29f0f89eed015f237a5ab855e40c898`. Published protocol hash: **`c0b8ccab56ba01002e2d4bd8268a587329b07d7605348117f5a7354af4fba003`**.

## Caps, accounting and preservation

The request and native executable were not changed. Its 600-second/64-MiB bound remains intact; a stricter **400-second external watchdog** left room for independent verification and reporting inside this operation's total 600-second allowance. Charged active work is **209.437 seconds**, comprising measured preflight/native/audit and 60 seconds reserved for static/closeout work. This fits inside the same 600 seconds already charged by the published protocol and is not charged twice.

The protocol binds **5,391 external setup inputs / 1,559,084,956 bytes**, including the exact original request. Its frozen setup time is **2,117.2720002000005 seconds**: the preceding 1,517.2720002000005-second basis plus the full 600-second allowance. The study's generated files are separately counted in native owned storage. This scope's final receipt counts the study, observer evidence, whole changed Markdown and a 2 MiB metadata allowance together against **67,108,864 new bytes**; exact totals are in `final-verification.json`.

Observer evidence is stored outside the study to preserve its exact frozen initial inventory. Its bytes and the changed documents are explicitly recorded for later full-study resource accounting; they are **not silently inserted into the immutable published setup map or protocol**. The next full-study launch must carry this additional output charge forward. That accounting and the execution decision remain outside this reservation-only approval.

The independent audit verifies complete package membership/hashes, all protected source inputs and current producing hashes, unchanged captured gameplay and request identity, setup bytes/time, the initial study inventory, the live registry and old historical exclusions. No combat start, attempt journal or batch exists. Concurrent UI paths are handled only as checkout observations; protected evidence hashes remain mandatory. Older failed/stopped scopes, v19 and every old ledger remain unchanged.

## Changes and reproducible verification

No implementation, gameplay, content, configuration, migration or deployment changes were made. The generated study contains ten reservation/binding files. The observer evidence package contains the frozen command, baseline, measurements, independent checks and copies of final Markdown. Seven active handoff documents plus this protocol/review were updated. **No new test run or build was needed:** producing inputs were unchanged and the exact saved **108 passing zero-combat tests through `build/run-tests.ps1`** were verified before execution. The captured DLLs were reused.

Completed commands, from the repository root:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$package = 'TestResults/balance/tower-complete-family-reservation-20260915'
& $python -B "$package/workflow.py" freeze
& $python -B "$package/workflow.py" execute
& $python -B "$package/workflow.py" verify
& $python -B "$package/update-documents.py"
& $python -B "$package/workflow.py" seal
```

The execution phase invoked the exact approved command once:

```powershell
dotnet 'TestResults/balance/tower-complete-family-request-20260915/executable/BalanceHarness.dll' tower-complete-family-reserve-bind 'TestResults/balance/tower-complete-family-reservation-request-20260915.json'
```

These are reproduction records, **not instructions to rerun completed outputs**. No command failed, no limit was reached and no reservation was repeated. All required reservation verification completed. Combat run/preparation and completed-study verification commands were intentionally not invoked because no full study was authorized or executed.

The next scientific action would use these exact reserved schedules under a separately approved full-study scope, preserving the **2,434,784-attempt / 24-hour / 64-GiB / zero-retry** envelope and additional observer accounting. Full-family second-stage selection capacity, combat across the 256-cell boundary and full-study throughput remain unverified. No balance conclusion changes from reserving values alone.
