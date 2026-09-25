# Loadout placement: original worker sidecar bounds

Current status: opt-in v4 bindings cap original worker receipt, publication and persistence files in Python and native workers. Verification passes 427 Python and 234 backend tests. Worker logs, native temporary/lease files, external caches, serialization memory and the remaining observer lifetime still lack complete bounds. Both compressed guards and the 1,806/1,800-second resource gate remain closed.

The target is the offline Balance Harness worker receipt boundary. `tower-proposal-worker-binding-v4` adds one required `sidecarByteLimits` object with exactly four nonnegative signed-64-bit integers: `receipt`, `publication`, `persistence` and `total`. V4 requires both publication paths. Earlier bindings reject the new field and retain their existing behavior and receipt schemas.

Python `SidecarWrites` and native `TowerWorkerSidecars` wrap the three exclusive original writers. Every append must fit its per-file cap and the shared total cap before the underlying write is invoked. The wrappers expose no seek or truncate operation. Python counts actual accepted short returns; native `Stream.Write` accepts the requested buffer only on a completed call. A failed operation poisons subsequent writes, and cleanup still closes all owned handles. Unknown failed-write prefixes cannot produce a successful completion. After all publication attempts, a successful scope verifies that all three files exist, are unlinked regular files and have the expected closed lengths.

These are cooperative append-only writer bounds, not OS confinement. They do not prevent another process from changing a closed file or creating an unrelated file. A failed publication may leave partial or empty sidecars; earlier bytes remain retained. Primary worker errors survive later publication and cleanup errors. Legacy receipt counters keep their explicit terminal-persistence exclusions; no recursive self-accounting claim is introduced.

`RetainedOwner.run_worker(..., sidecar_byte_limits=...)` binds the caps and independently checks each original sidecar's returned length and their sum before retention. `StudyWorkSupervisor(..., worker_observation_persistence=True, worker_sidecar_limits=...)` requires declarations for all four phases and sends v4 bindings to both languages. With declared supervisor storage enabled, its storage binding and final ownership observation also retain these limits. The sum of phase totals is labelled a declared bound for original sidecars, separate from the three managed directory caps and from observed peaks.

The existing routed supervisor fixture now understands v4. Its native bodies remain literal test callbacks, while publication goes through the bounded sidecar writer. The independent Python worker uses the production worker receipt context. Native correctness tests exercise the actual native wrapper separately, and a real `tower-proposal-study-audit` command verifies the new binding on a complete fabricated-outcome fixture.

Validation includes:

- **22 new backend cases** plus 212 existing cases, run through `build/run-tests.ps1` against a fresh isolated build. The two disjoint test groups pass 121 and 113 cases, for **234 total**, with no skips. The build reports 45 existing warnings and no errors.
- **24 new Python cases** covering malformed/legacy declarations, all four caps, short writes, hidden prefixes, poisoned writers, closed-length changes, error precedence, supervisor dispatch, oversized returned sidecars, native exports and the real v4 audit command.
- **403 existing Python cases**, including the three full native audit success/mutation cases. These audit commands use the newly retained runtime. Legacy v1/v2 exchange regressions continue using their authenticated historical exports/runtime, while current backend tests also cover the old native receipt APIs.

The fresh native audit fixture constructs actual input bindings with fabricated draws and passes production audit. It contains 12 physical held-out members, not the prospective maximum 36. No encounter preparation or combat occurs. Production audit reconstructs 15,744 trial bindings. This is one fresh complete synthetic correctness fixture, not a resource experiment or qualification sample.

There are 44 retained real process observations: the prior 43 regression observations plus one v4 native audit job. There are also 44 retained synthetic process observations: the previous 40 plus four new routed sidecar-owner observations. Native literal-body unit cases are not counted as independently observed process jobs. Correctness elapsed observations are not qualification timing samples.

The [resource specification v4](Tower-Loadout-Placement-Resource-Boundaries-v4.json) has 11 domains and 34 references. The [updated inventory](Tower-Loadout-Placement-Worker-Sidecar-Boundaries.json) has 19 families and 77 references. Both retain the distinction between scoped cooperative bounds and missing whole-process coverage. Their scientific limits, coefficients and qualified forecast remain null.

The [verification package](../TestResults/loadout-placement-worker-sidecars-verification-20260925) retains exact commands, logs, source snapshots, native exports, the new runtime/fixture and fresh process receipts. The first backend build could not read the user's NuGet configuration inside the sandbox; the authorized test-runner retry succeeded. Initial Python failures exposed a closure-local path bug, which was corrected, and two test expectations that incorrectly rejected sealed failure evidence. All preliminary logs remain preserved. No required command remains blocked.

```text
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-worker-sidecars-20260925 -Filter <receipt and sidecar groups>
./build/run-tests.ps1 -NoBuild -ArtifactsPath .artifacts/loadout-worker-sidecars-20260925 -Filter <remaining accounting groups>
python -B -X utf8 build/test-proposal-worker-sidecars.py -v
python -B -X utf8 TestResults/loadout-placement-worker-sidecars-verification-20260925/run-verification.py
python -B -X utf8 TestResults/loadout-placement-worker-sidecars-verification-20260925/run-supplemental.py
git diff --check -- <changed files>
```

Changed files comprise Python accounting/supervisor integration, the native worker binding and new bounded writer, new Python/backend tests, the routed fixture helper, this report, versioned specifications/inventories, LF attributes and only line 3 of nine status documents. Historical document bodies, evidence and captured runtimes remain intact. The new native build is isolated and does not replace earlier runtimes.

Cumulative charges remain **79,339.66095319996 seconds** and **51,980,910,910 bytes**; declared maxima remain **168,240 seconds** and **107,122,524,160 bytes**. The first failed pair remains failed. No scientific launch, qualification, cost experiment, timing pair, live-history rescan, production entropy, scientific reservation, encounter preparation or combat occurred. There are no migrations, application configuration changes or deployments. V4 native diagnostics require the updated authenticated runtime; old captured runtimes must remain immutable and cannot consume v4 bindings.

Next bound worker stdout/stderr log lifetimes, then native pending/lease and runtime/cache domains, or provide defensible finite bounds. Complete the finite external observer's setup, terminal persistence, cleanup, console, exit and memory-lifetime obligation before proposing measurement. Serialization memory remains separate from the byte caps, which are checked after serialization. Unknown domains stay disqualifying. No current forecast, compression speedup or search improvement is established.
