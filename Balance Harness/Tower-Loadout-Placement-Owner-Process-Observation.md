# Loadout placement: enclosing owner process observation

Current status: an opt-in enclosing process monitor now observes owner console output, exit, lifetime job memory and kernel I/O through confirmed drain. Verification passes 254 Python tests. Whole-process accounting and native production-audit verification remain incomplete; both compressed guards and the 1,806/1,800-second resource gate stay closed.

## Implementation and boundaries

`Balance Harness/analysis/proposal_owner_process.py` adds the API-only `OwnerProcessMonitor`. It runs a caller-supplied trusted Python diagnostic driver under the existing suspended Windows owned-job helper. The driver remains responsible for invoking the original launcher and its admission checks. There is no new scientific CLI, automatic study launch, admission bypass or change to the existing launcher, supervisor, process helper or native code. The request pin is explicitly caller-declared; it is not a new admission receipt.

The enclosing job contains the owner and its descendants, including nested worker jobs. Windows aggregates child-job accounting into parent jobs, so the parent observation can include work after the inner supervisor has finished. See Microsoft's [nested jobs accounting and termination documentation](https://learn.microsoft.com/en-us/windows/win32/procthread/nested-jobs). The retained fixture verifies this behavior with real nested jobs and a routed owner. Parent-job, child-job and application counters overlap and must never be added together.

After the job drains and owned handles close, the monitor hashes the closed stdout/stderr log. Its `tower-proposal-owner-process-v1` observation retains the original process result and kernel observation, the console hash and length, driver/helper/module/interpreter hashes, command and working directory. An exclusive manifest binds `owner-process.json` and `console.log`. Publication uses checked partial writes, flush, file sync, close, exact inventory and hash verification. Existing destinations and repeated runs are rejected; caller-declared protected evidence roots must be disjoint in both directions. Failed publication cannot expose a success pin, and secondary publication/close errors preserve the original error.

Process outcome and observation completeness are separate. A captured nonzero exit or timeout can have complete diagnostic observations; an exit code of zero with missing kernel data cannot. Printed success cannot override a subsequent failing exit. The monitor never certifies a scientific outcome. Query failures remain null/unknown, and an unassigned job cannot claim owner lifetime memory even if a memory query returns a value. Start failures and resource-check failures retain the available observation and propagate the original exception.

`ownerJobPeakCommitBytes` describes the owner/descendant job lifetime. The helper's existing `ownerLifetimePeakCommitBytes` actually describes the enclosing monitor in this arrangement; the new envelope explicitly exposes it as `monitorLifetimePeakCommitBytes`, sampled before owned-handle cleanup. It is not the monitor's final lifetime peak. No simultaneous combined peak, incremental allocation, physical-disk or durable-byte claim is made.

The caller supplies an absolute monitor deadline. Cleanup and a publication reserve are subtracted before starting the owner. Cooperative deadline and optional external resource checks also run during publication and after verification. These checks do not turn the monitor itself into a hard-bounded process. The observation stops after owned-handle cleanup and the console hash; the monitor's own preparation/application I/O, later persistence, verification, console and exit remain excluded. Its own later tail is not recursively claimed as complete accounting. Whole-process scratch and complete application-operation attribution also remain unknown. Both `wholeProcessCoverage` and `usableForAdmission` remain false.

## Verification

The fresh [verification package](../TestResults/loadout-placement-owner-process-verification-20260924) retains source snapshots, commands, logs and fixtures. `build/test-proposal-owner-process.py` adds **27 tests** covering a real enclosing owner job, nested jobs, late descendants, final console failure, abrupt exit after printed success, timeout/drain, failed admission and the existing compressed guard, missing kernel queries, start/assignment failures, resource checks, immutable destinations, protected paths, publication failures, short/zero writes, original-error preservation, console tampering and untracked files.

The retained owner fixture uses real launcher admission checks, leases, owner/supervisor publication, final stdout and interpreter exit. Every scientific worker command is intercepted. Four tiny literal jobs exercise real nesting; the routed worker receipts and their inner supervisor observations remain synthetic. The outer observation covers the actual owner process, all four nested jobs, terminal-observation retention inside the owner, a deliberately deleted 128 KiB tail file, a 24 MiB tail allocation and final `atexit` output. Those operations occur after supervisor persistence. The test asserts their presence and coverage without treating the values as cost-study samples. The native fixture DLL is deliberately non-executable.

The initial test run exposed an overly strict fixture assumption about one process per Python command. The bundled interpreter produced additional associated processes. Assertions now require the expected minimum, verify all four nested job receipts, match terminal kernel and process totals, and require confirmed drain. No production behavior was changed to accommodate this fixture correction. The final new suite and exported regression run both pass.

Existing persistence (22), supervisor (15), native receipt publication (7), Python receipt publication (28), job I/O (16), worker I/O (16), worker receipts (18), owner closeout (13), work accounting (38), owner files (22), arithmetic/guards (25) and selected-members (7) groups also pass: **254 distinct Python tests**, with no skips. Native inputs and isolated binaries are authenticated read-only. No backend source changed, so no backend build or backend test run was needed. No required command remains blocked.

```text
python -B -X utf8 build/test-proposal-owner-process.py -v
python -B -X utf8 TestResults/loadout-placement-owner-process-verification-20260924/run-verification.py
git diff --check -- <changed files>
```

`test-runs.json` records every exact command and fresh export destination. Do not rerun export commands into sealed destinations. Initial authentication covered 331 historical pins, 130 current source hashes and 31 isolated runtime files. Only line 3 changed in the nine status documents; their historical bodies remain byte-identical.

## Remaining work and operational effect

Next account for worker publication-observation persistence and remaining application operations, then exercise a fresh full native production-audit success fixture under the enclosing owner boundary. External/transient scratch and the explicitly excluded monitor work still need a defensible treatment in a prospective resource model. This API is diagnostic groundwork; adopting it in any future admitted launch requires authenticated driver/monitor inputs and an explicit resource envelope.

The 1,806-second audit floor still exceeds the 1,800-second limit. A separately justified prospective replacement resource model is required before measurement; no current forecast, compression speedup or search improvement is established. The first failed pair remains failed. Historical evidence and cumulative charges are unchanged. No scientific launch, native preparation, qualification, timing pair, live-history scan, production entropy draw, scientific reservation or combat occurred. No migrations, application configuration changes, deployments or retained-runtime replacement occurred.
