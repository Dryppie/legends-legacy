# Complete retained-family launch v2: compact receipts verified

Completed **15 September 2026** under the [frozen protocol](Tower-Complete-Family-Launch-V2-Protocol.md) and [fixture correctness amendment](../TestResults/balance/tower-complete-family-launch-v2-20260915/producing/correctness-amendment.md). **All three requested engineering steps are complete:** compact receipts implemented, full-size synthetic run/verify paths verified, and a corrected captured executable built and checked against real inputs. The exact [execution decision](../TestResults/balance/tower-complete-family-launch-v2-20260915/control/execution-decision.json) is ready for review. **No full study has been started.**

## Fix and measured regression result

`TowerCompleteFamilyLaunch.cs` now leaves full assessments in the study archive and writes a typed control summary with outcome, logical trials, cell/selection counts and raw assessment/inventory hashes. It compares the saved assessment with the verified controller result before publication. The verification receipt must reproduce the earlier run summary. The 64-KiB per-record limit, failure reserve, total control cap, deadlines, durable starts and no-overwrite policy remain intact.

The public commands invoke shared internal outer run/verify methods. Tests inject only synthetic load/study callbacks, exercising the actual start, completion, failure and result-publication paths. No fixture bypass is exposed through the CLI. The existing controller's preparation, combat, selection, compact batches, durable fight charging and archive reconstruction are unchanged.

| Synthetic outcome | Cells | Full-result envelope bytes | Run receipt bytes | Verify receipt bytes | Run / verify seconds |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completed selection | 43,879 | 24,054,350 | 2,488 | 2,676 | 0.629 / 0.519 |
| Capacity stop | 43,879 | 30,496,359 | 2,523 | 2,717 | 1.463 / 1.149 |

Full assessments remain **22,905,151 and 29,179,834 bytes** in the synthetic archives. The corrected control receipts fit comfortably below **65,536 bytes**. The full-result column measures the old-style serialization envelope on the same synthetic data; it is not a historical production measurement. Timings include fixture archive work and are not study throughput estimates. Exact metrics are retained in [receipt-metrics.json](../TestResults/balance/tower-complete-family-launch-v2-20260915/producing/receipt-metrics.json).

## Verification and original failure evidence

The first repository-wrapper invocation ran 160 cases: **159 passed; one assertion failed**. Both full-size run/verify scenarios passed on that first invocation. The failing `record-budget` case correctly received `InvalidDataException` from admission, while the fixture expected a later file-exists `IOException`. Only that assertion was corrected; production code and the already-built captured executable were unchanged.

A separately frozen four-case check through `build/run-tests.ps1` passed. The other **156 unchanged passing cases** are reused. Thus **160 distinct cases are covered successfully across 164 executions**, with one original assertion failure preserved in `tests-first.trx`, its source snapshot and log. Do not describe this as a single clean 160-case run. Both fixture invocations count against the original 650-second test allowance; their combined time is **61.610 seconds**. There was **one unit-fixture recheck and zero native/study retries**.

Additional coverage checks tampered summary counts/hashes, altered request/status/assessment/inventory, cancellation, exhausted control output, disagreement between saved and returned assessment, and rejection before any study callback when a prerequisite failed. All are zero-engine fixtures.

| Native or independent check | Result |
| --- | --- |
| Captured host build | Exit 0, **4.953 seconds**; reused after the test-only correction |
| Corrected real-input check | Exit 0, **237.235 seconds** external wall time |
| Independent audit | **114.203 seconds** |
| Preserved predecessors | **13 packages / 15,419 indexed files**, including the blocked first launch |
| Historical registry | **147 paths / 481,891 values**; original unused 512 preserved |
| Initial study | **10 files / 16,662,786 bytes**; hashes and timestamps unchanged |
| Dirty checkout | **4,311 baseline files**; 76 permitted UI observations left untouched |
| Preparations / fights / fresh values / native retries / resumes | **0 / 0 / 0 / 0 / 0** |

The captured executable retains the original Application, Common, Domain and Services.LL hashes and original runtime/OS/architecture identity. Only the harness differs. The [native result and detailed trace](../TestResults/balance/tower-complete-family-launch-v2-20260915/control/check-result.json) and [audit](../TestResults/balance/tower-complete-family-launch-v2-20260915/control/audit-result.json) preserve actual observations. The native check took longer than the previous candidate's 57.70-second check; this scope does not isolate the cause or claim a speedup. Earlier measured filesystem results remain **622.54x incremental accounting / 4.50x sixteen-write lifecycle at 9,216 archives**, with no established whole-study speedup.

## Complete resource accounting

The unchanged reservation protocol retains **5,391 external files / 1,559,084,956 bytes / 2,117.2720002000005 seconds**. The corrected request adds **3,959 files / 1,165,097,359 bytes**, including the complete stopped launch, reservation observer and corrected producing packages. Its setup time includes the stopped scope's **280.47 charged seconds**, this scope's measured baseline/freeze/tests/build and a conservative capture/seal allowance. Previous unused future-run allowances are not counted as completed work.

The fixed late allowance remains **2,400 seconds / 64 MiB**: check, audit, reporting, run administration and future standalone verification; 62 MiB control output plus 2 MiB whole changed Markdown. A ready snapshot seals current control evidence while allowing future single-use run/verify files after approval.

Effective setup time is **4939.400000 seconds**, leaving **81460.600000 seconds** for the native study. Native study storage is limited to **65,928,185,557 bytes (61.400 GiB)**. These tighten the original **2,434,784 attempts / 24-hour total / 64-GiB total / zero-retry** envelope. This scope's charged diagnostic time is **574.049 seconds**, including conservative closeout allowance, under 1,800 seconds. Final bytes are recorded in `control/final-verification.json`; the package is below 4 GiB. No resource limit was reached.

## Changes and reproduction record

Changed code: `LL/tools/BalanceHarness/TowerCompleteFamilyLaunch.cs`; the existing request fixture constructor in `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerCompleteLaunchTests.cs`; and new full-size `BalanceHarnessTowerCompleteReceiptTests.cs`. Seven active Markdown handoffs and this protocol/review were updated. No gameplay content, configuration, migration or deployment change. Unrelated work and all sealed evidence remain preserved.

Commands executed from the repository root:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$package = 'TestResults/balance/tower-complete-family-launch-v2-20260915'
& $python -B "$package/producing/workflow.py" freeze
& $python -B "$package/producing/workflow.py" tests       # preserved fixture assertion failure
& $python -B "$package/producing/workflow.py" build
& $python -B "$package/producing/correct-fixture.py" freeze
& $python -B "$package/producing/correct-fixture.py" execute
& $python -B "$package/producing/workflow.py" request
& $python -B "$package/producing/workflow.py" native
& $python -B "$package/producing/workflow.py" audit
& $python -B "$package/control/write-review.py"
& $python -B "$package/producing/workflow.py" seal
```

Both test invocations use the required repository wrapper through PowerShell 7 and separate artifact directories. Exact argument arrays, filters, source hashes, TRX records, logs and exit codes are in the producing package. Build access to the existing host NuGet configuration was approved. These are historical commands, not instructions to overwrite or rerun existing evidence.

## Concrete execution decision

The proposed study checks the complete **43,879-team** retained family. Stage one uses **43,319 non-anchor teams x 32 trials = 1,386,208 fights**. Stage two retains all 560 anchors and eligible teams up to the existing **4,096-team capacity x 256 trials = 1,048,576 fights**. If selection exceeds that capacity, the existing controller stops with the complete evidence and an inconclusive capacity result. No recipes are dropped or caps increased.

Use the exact already-reserved **32 + 256 values**, allocating no new seeds. Await user approval before these commands:

```powershell
$exe = 'TestResults/balance/tower-complete-family-launch-v2-20260915/producing/executable/BalanceHarness.dll'
$request = 'TestResults/balance/tower-complete-family-launch-v2-20260915/control/launch-request.json'
dotnet $exe tower-complete-family-launch-run $request
# Only after a completed run; once, within the reserved 900-second verification allowance:
dotnet $exe tower-complete-family-launch-verify $request
```

Request SHA-256: **`edf212dc351e7501eb57562f4403576d4dce6621e2bf8a4b13086c4079c4c2b0`**. Producing seal: `0dcb30ee120c58298c688bd0b01d7f44335e1a905ad0889b6465ffad86f3ef2e`. Those same request bytes were used for the successful native input check. No full-study run, completed-study verification, new allocation or combat command was invoked here. The first launch request stays blocked; the corrected request points to the unchanged reserved study.

Reliability remains **Fail 1/3**, adoption **Hold**; the engineering work adds no new balance evidence. V19 remains sealed **Unresolved**, with all 253 required recipes and no confirmation inside that original experiment. Full-family stage capacity, combat across the 256-cell boundary and whole-study throughput remain unmeasured. The study can legitimately stop at its frozen capacity, time or storage bound; no completion-time promise is made.
