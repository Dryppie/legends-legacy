# Loadout placement: terminal Windows job I/O observations

Current status: optional process observations retain kernel job I/O totals after the assigned process tree has exited. Verification passes 170 Python tests. Whole-process coverage remains incomplete; the compressed launch guards and the 1,806/1,800-second resource gate stay closed.

## Implementation

`build/bounded_windows_process.py` queries a separate terminal job I/O observation when a caller supplies an observer. The query runs after confirmed job drain and before closing the owned handles; the observer receives the result after handle cleanup. Calls without an observer make no additional query and retain the existing return contract, ownership, deadlines and termination behavior.

The implementation uses `QueryInformationJobObject` information class 8 and the existing native I/O counter layout, prefixed by basic job accounting. Microsoft's contract includes processes previously associated with the job, so the observation survives worker exit and covers the job's lifetime rather than a still-running root PID. [QueryInformationJobObject](https://learn.microsoft.com/en-us/windows/win32/api/jobapi2/nf-jobapi2-queryinformationjobobject), [job accounting structure](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_and_io_accounting_information).

The independently versioned `tower-owned-job-io-v1` block retains read, write and other operation counts and transfer-byte counts, with the process counts and observation boundary. Those are kernel I/O counters. They are kept separate from application counters and retained file lengths; the implementation makes no physical-disk, durable-byte, scratch-peak or per-file attribution claim. [IO_COUNTERS](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-io_counters).

An unassigned child, unconfirmed drain, inconsistent terminal process count or failed query produces `coverage=Unknown` with null counters and a reason. A known zero stays distinct from an unknown value. I/O query failure does not erase memory observations or replace an original process error. The new block always carries `wholeProcessCoverage=false` and `usableForAdmission=false`.

`Balance Harness/analysis/proposal_work_accounting.py` validates and retains the optional block with the existing bound process observation. It rejects incomplete fields, invalid or overflowing counters, terminal claims on undrained jobs, and unsupported coverage. Legacy process observations without the block remain valid. Kernel totals are never added to worker or owner application counters because their measurement scopes overlap.

This gives the owner a kernel observation window extending beyond a worker's application receipt, through receipt publication and process exit. It does not retroactively add the receipt's own writes to that receipt's application counters, nor account for the supervisor/owner process. Scientific defaults, archive inventory, admission and native source are unchanged. Future accounting use must authenticate the changed process wrapper and Python accounting module.

## Verification

The fresh [verification package](../TestResults/loadout-placement-job-io-verification-20260924) retains snapshots, logs and literal fixtures. `build/test-proposal-job-io.py` adds 16 tests covering Windows structure layout and full-width fields, unknown versus zero values, inconsistent queries, errors and cancellation, optional behavior, legacy compatibility, strict retention validation and copied observation integrity.

The final `job-io-final` fixture runs real owned processes. A bound Python worker writes its receipt, emits later output and starts a descendant that writes and deletes another file before exiting. The processes write and delete 98,304 literal scratch bytes. The test verifies that terminal transfer counts cover at least those writes plus the receipt, that the job is drained, that the process totals agree, and that the query precedes all three handle closes while publication of the observation follows them. It does not infer a scratch high-water mark from those transfer counts.

The initial fixture incorrectly assumed exactly two associated processes; Windows reported three. The corrected test requires at least the root/descendant pair and agreement between independently queried kernel process totals. The initial log and files remain as attempted evidence. Only `job-io-final` and `job-io-final.log` establish the final fixture result.

The existing worker I/O (16), worker receipts (18), owner supervisor (15), owner closeout (13), collector/process (38), owner files (22), owner arithmetic/guards (25) and provenance (7) groups also pass: **170 distinct Python tests**, with no skips. Real owner fixtures retain the new I/O block automatically; the separately labeled routed supervisor fixture continues to use synthetic worker observations. Historical native exchanges and the isolated native DLL were read-only inputs. Native sources did not change, so no backend rebuild or backend test run was needed. No required command remains blocked.

```text
python -B -X utf8 build/test-proposal-job-io.py -v
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

Export variables designated fresh destinations. The native exchange check used its external manifest pin. These are completed verification commands; mutation tests must not be rerun inside sealed packages.

## Remaining work and operational effect

Outstanding boundaries include exact worker receipt-publication application counters, supervisor-observation persistence, owner console output and process exit, whole-owner memory lifetime, transient and external scratch storage, remaining owner/native reads and parsing, and metadata/path-check and lease-handle operations. A fresh full native production-audit success fixture is still needed. Kernel job totals supplement these measurements; they do not close whole-process accounting or justify a resource forecast.

A separately justified prospective replacement resource model is required before measurement. No scientific launch, native preparation, qualification, production entropy draw, scientific reservation, timing pair, live-history scan or combat occurred. The first failed pair stays failed; history and cumulative charges remain unchanged. No migrations, application configuration changes, deployment or retained-runtime replacement occurred. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.
