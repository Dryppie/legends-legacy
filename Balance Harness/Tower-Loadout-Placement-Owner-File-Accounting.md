# Loadout placement: Python owner file accounting

Current status: opt-in owner file boundaries verified by 92 Python tests. Whole-process coverage is incomplete; both compressed launch guards and the 1,806/1,800-second resource gate remain closed.

The offline Balance Harness owner now has optional accounting for its native copies, JSON publication writes, hashes and redirected child logs. Default execution does not activate the collector. No scientific launch, preparation, qualification, resource timing sample or combat was performed.

## Changed boundaries and meaning

`Balance Harness/analysis/proposal_work_accounting.py` adds `OwnerFileCounters`. It uses the existing incomplete, externally bound counter receipt contract. Successful `shutil.copyfile` calls report destination logical lengths, without inventing Python stream reads or writes. Failed copies retain an unknown-progress counter and observe any remaining destination length. Overwrite behavior, return values and native exceptions are preserved.

JSON publication retains the owner's exclusive text open, UTF-8 encoding, LF newlines, `json.dump(indent=2)`, flush, fsync and close. Text writes count the UTF-8 length of characters accepted by TextIO, using `textAcceptedUtf8Bytes`, which does not assert durable or physical I/O. Serialization failures retain accepted prefixes. Write failures explicitly leave partial progress unknown; flush, fsync and overall operation completion are separately recorded. Close failure prevents operation completion. Hash reads continue to use the existing application-read counter.

`build/run-proposal-affinity-study.py` adds a nested, exception-safe `file_accounting` context. All owner copy calls use its helper; existing JSON publication and digest helpers consult it. All four process phases forward the optional collector. There is no command-line activation, automatic receipt attachment or admission exception. Existing admission authentication still checks the owner and process-wrapper hashes, so changed code requires a newly authenticated admission package if future separately authorized work becomes admissible. Historical admitted copies are untouched.

`build/bounded_windows_process.py` observes only a log successfully created by the current call, after its process wrapper, cleanup and observer return or fail. It also records an empty created log when subsequent setup fails. Pre-existing logs are neither overwritten nor attributed to the new run. Inherited child handles bypass Python stream writes, so `childLogFinalObservedBytes` is a final file-length observation only. A completed wrapper scope can contain a nonzero exit or timeout; the separate process receipt determines process outcome and drain status.

Destination-length gauges and their maxima are sampled observations of tracked files. They exclude source files, unobserved worker files, transient growth between observations, deleted scratch and physical allocation. A failed observation records unknown coverage instead of zero. These counters cannot establish a whole-directory storage peak. Do not sum them with native copy lengths, stream I/O, other worker gauges or memory peaks and call the result physical I/O or an enclosing peak.

## Verification

The fresh [verification package](../TestResults/loadout-placement-owner-file-accounting-verification-20260924) retains before/after source snapshots, all attempted test logs, literal file fixtures and tiny owned-process observations. The final fixture has 16 copied logical bytes, 50 accepted JSON text bytes, 50 hash-read bytes and 11 final child-log bytes. Its observed destination total is 77 bytes; fixture inputs and its accounting receipt, binding, process observation and manifest are outside that sample. The receipt binds the literal input hash and current owner source hash; the binding pins the owner, accounting module and process helper. This is a correctness fixture, not a scientific request or resource measurement.

The new 22 tests verify default/counting byte equivalence, exclusive creation, escaped Unicode, nested scope restoration, overwrite and copy errors, partial failed copies, partial JSON serialization, write/flush/fsync/close failures, unknown length observations, and the closed compressed guard. Tiny Windows Job tests cover stdout/stderr, inherited descendant output, existing logs, null-input setup and process-creation failures, observer/controller failures, nonzero exit and timeout. Final log observation follows job cleanup. A test initially assumed exactly two processes for the root/descendant case; the runtime's job reported three. It now requires at least two and retains the exact output and drain checks. The unsuccessful run and its separate fixture are retained unchanged.

Existing collector/retained-owner/process tests (38), owner arithmetic and guard tests (25), and selected-member provenance tests (7) pass. The 38-test group was repeated with fresh process evidence after the final collector adjustment. Counts are 92 distinct tests, not a sum of repeated runs. Backend code is unchanged, so no backend build or test run was needed in this stage. No required command remains blocked.

```text
python -B -X utf8 build/test-proposal-owner-files.py -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
git diff --check -- <changed files>
```

`LL_OWNER_FILE_FIXTURE` and `LL_WORK_ACCOUNTING_OBSERVATIONS` designated new output directories for each retained run. The older native exchange was read-only. These commands record completed verification, not authorization to mutate any sealed package.

## Remaining work and operational effect

Next, emit and retain bound opt-in worker receipts across the real native, native-audit, independent-audit and publication boundaries. Integrate owner counters and process observations into that enclosing lifecycle, including receipt/manifest writes and lease cleanup. Owner reads other than hash reads, parsing/materialization, whole-owner memory, transient scratch and full production native-audit coverage remain incomplete. The existing retained diagnostic owner's own final publication tail remains excluded. The new file observer does not close these gaps or establish a resource forecast or compression speedup.

A separately justified prospective replacement resource model is still required before measurement. No preparations, qualification, scientific reservations, production entropy, timing pair, live-history scan or gameplay changes were made. The first failed pair remains failed and all prior charges are unchanged. All 276 inherited historical pins and 106 current-source pins authenticated before edits. Only line 3 changes in the nine status documents; historical bodies remain byte-identical. There are no migrations, application configuration changes or deployments, and no retained runtime was rebuilt or replaced.
