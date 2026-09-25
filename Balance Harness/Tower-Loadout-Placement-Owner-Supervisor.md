# Loadout placement: scientific owner and enclosing supervisor

Current status: the admitted study owner has an opt-in supervisor API for four bound workers and persistence of the final owner observation. Verification passes 138 Python tests. Whole-process coverage remains incomplete; both compressed launch guards and the 1,806/1,800-second resource gate stay closed.

## Implementation

`build/run-proposal-affinity-study.py` accepts an optional `accounting` adapter. `Balance Harness/analysis/proposal_owner_supervisor.py` implements that adapter with `StudyWorkSupervisor`. Existing admission and request validation run before the adapter can create files. The default CLI does not activate accounting. The original validation, resource rules, exclusive Windows leases, inventory rules and compressed-storage rejection remain unchanged.

An opted-in caller must supply a fresh, disjoint sidecar directory and a freshly authenticated admission package containing the current supervisor and accounting modules beside the captured independent auditor. The adapter verifies both local and captured module bytes against admission pins. The retained native runtime and owner/process-wrapper pins remain subject to the original admission checks. Existing sealed admissions cannot silently accept changed code.

The owner binds the native study, native audit, independent audit and publication workers to the request, phase, producer and accounting module. It retains each worker receipt and process observation outside the scientific archive. Native commands use their existing optional work-binding arguments. The independent worker uses the authenticated captured auditor beside its accounting module, preserving the archive inventory. Successful processes with missing or invalid accounting receipts cannot advance. Failed processes retain the original launcher's process receipt and failure path, and a secondary publication failure does not replace the original worker error.

The optional owner collector observes instrumented launcher file operations and final child-log samples. The enclosing supervisor persists the owner's final observation in a managed directory, seals and rechecks its manifest, then returns a separate `tower-proposal-supervisor-observation-v1` object. Its counters cover the owner-observation writes, flushes, fsyncs, closes and verification reads. That returned supervisor observation explicitly excludes its own persistence. Owner and worker counters remain separate; sampled directory lengths are not relabeled as I/O.

Resource checks include both the study directory and sidecars. Audit/publication growth is checked against the same combined native baseline, and all later work uses the original shared audit deadline. Existing `completion.json` and `closeout.json` retain their archive-only byte semantics: the native verifier requires final retained bytes to equal the study directory's exact length. The supervisor separately reports the combined storage sample. Its persistence uses the same resource checks, and the original overall watchdog remains active through the final success output. Supervisor publication failure suppresses that success output.

## Verification

The fresh [verification package](../TestResults/loadout-placement-owner-supervisor-verification-20260924) retains before/after source snapshots, test logs and literal fixtures. `build/test-proposal-owner-supervisor.py` adds 15 tests for authenticated module binding, disjoint output paths, unchanged compressed rejection and default behavior, four bound worker phases, shared deadlines, archive receipt compatibility, native and audit sidecar storage limits, missing receipts, failed workers, final publication failure and watchdog lifetime.

The final `supervised-owner-final` fixture runs real admission validation, Windows leases, launcher copying/hashing/publication and supervisor accounting. Its four worker commands are intercepted and return explicitly synthetic process observations and literal receipts. Its retained runtime is non-executable fixture bytes. This is not a native scientific study or a full production-audit success fixture. Both its default and supervised variants satisfy the independent auditor's resource-receipt checks and the native verifier's exact archive-length invariant. The full native verifier is not invoked on this intentionally incomplete fixture.

Review found that an earlier integration draft included sidecar bytes in archive receipts, conflicting with the native verifier. The final implementation separates archive receipt values from combined resource checks. Preliminary logs and fixture exports remain retained as attempted evidence; only `supervised-owner-final` and `supervisor-final.log` establish the final integration result.

The existing owner-closeout (13), bound-worker (18), collector/retained-owner/process (38), owner-file (22), owner arithmetic/guard (25), and provenance (7) tests also pass: **138 distinct Python tests**, with no skips. Affected owner suites were rerun after the archive-counter correction into fresh destinations. Existing tiny-process regressions exercise real Windows process ownership and cleanup. Their durations are correctness observations, not resource-protocol timing samples. Prior native exchange files and the isolated DLL remain read-only inputs. Backend sources did not change, so no backend build or test run was required. No required command remains blocked.

```text
python -B -X utf8 build/test-proposal-owner-supervisor.py -v
python -B -X utf8 build/test-proposal-owner-closeout.py -v
python -B -X utf8 build/test-proposal-worker-receipts.py -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 build/test-proposal-owner-files.py -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
git diff --check -- <changed files>
```

Export variables designated fresh destinations. Worker exchange verification used its external manifest pin. These record completed verification; mutation tests must never be rerun inside sealed packages.

## Remaining work and operational effect

The next accounting boundaries are supervisor-observation persistence and final console/process exit, whole-owner memory lifetime, scratch outside managed storage, worker binding-authentication and receipt-publication I/O, remaining owner/native reads and parsing, independent CLI output writes, and lease acquisition/handle operations and failures. A fresh full native production-audit success fixture is still needed. All observations remain `wholeProcessCoverage=false` and `usableForAdmission=false`; combined retained-storage samples do not establish transient peak storage or complete I/O coverage.

A separately justified prospective replacement resource model is required before measurement. No scientific launch, native preparation, qualification, production entropy draw, scientific reservation, timing pair, live-history scan or combat occurred. The first failed pair stays failed; history and cumulative charges remain unchanged. No migrations, application configuration changes, deployment or retained-runtime replacement occurred. Future opt-in use must authenticate the changed launcher and additional modules. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.
