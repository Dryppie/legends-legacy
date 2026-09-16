# Complete retained-family controller — stopped parity setup review

Recorded **14 September 2026** under the [frozen parity protocol](Tower-Complete-Family-Parity-Protocol.md). Target: offline `LL/tools/BalanceHarness`. Status **ParitySetupFailedNoCombat**. **215 fixture tests passed**, but a schema error in the new diagnostic input-preparation script stopped work before producing inputs were frozen. **Zero preparations, engine fights, new reservations, retries or resumes occurred.** Captured production orchestration parity remains unverified.

## Failure and retained evidence

The setup script streamed the retained inventory to select the two predeclared cells and checked the existing history. While constructing the 32-fight reference definition, it assumed the cell had `source` and a single remaining sample-count field. The actual `TowerBalanceCellDefinition` has **`role` and `minimumSamples`**. Its schema assertion therefore failed with `AssertionError: {'minimumSamples', 'role'}`. This was an error in the diagnostic setup, not a completed comparison.

The failure occurred **before** writing `reference-definition.json`, `case-request.json` or `inputs.json`. No captured candidate executable was built, no reference or candidate command started, no roster was prepared, and no report comparison ran. The selected cell identities remain saved, but their attempted in-memory scenario extraction was not persisted before the failure. The frozen single extraction was not repeated.

The [failure receipt](../TestResults/balance/tower-complete-family-parity-20260914/setup-failure.json), exact failing `inputs.py`/`inputs-failed.py`, source baseline, protocol, passing TRX and build output are retained. A separately named **`inputs-corrected-unexecuted.py`** uses the exact five-field cell schema and adds failure timing persistence. It was syntax-parsed only; no setup or diagnostic was rerun. It must not be treated as verified producing input.

## Implementation and verification

[TowerCompleteFamilyRun.cs](../LL/tools/BalanceHarness/TowerCompleteFamilyRun.cs) now exposes internal preparation, batch execution, definition, batch-verification and reconstruction methods so a bounded diagnostic can call the same adapters as production. The public run still performs the same complete preflight and calls them in the same order. Preparation still checks every saved participant hash before the first engine call; it now records a `complete.prepare-family` timing scope and prepared-cell counter. No public bypass, altered validator, fresh-seed exception or cap change was added.

The unchanged **215-test allowlist**, including all **52 complete-family controller tests**, passed through `build/run-tests.ps1`. Both combat-capable staged cancellation/resume cases were excluded, and the complete TRX matched the frozen names. The wrapper took **44.098 seconds**; the build reported **19.45 seconds**, **34 existing warnings / zero errors**, and the test suite **21 seconds**. These are fixture results with injected accounting events, not production combat parity.

Executed commands from the repository root:

```powershell
& './TestResults/balance/tower-complete-family-parity-20260914/run-tests.ps1'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-complete-family-parity-20260914/inputs.py'
```

The first completed; the second stopped at the schema assertion. Do not repeat the second command in this package. Captured build, reference execution, candidate full preparation/execution and independent parity verification **could not proceed after that failure**. No timing or gameplay improvement is claimed.

## Limits and next boundary

The intended diagnostic remains **32 reference + 288 candidate = 320 new fights**, using only existing screen/midpoint values, plus complete 43,879-cell preparation. The 256 second-stage references would come from sealed midpoint reports. **None of that workload ran.** A new frozen attempt must verify the corrected cell schema and producing inputs before executing the same bounded cases; it must preserve this stopped package. The helper extraction also still needs its captured build and runtime verification.

The failed setup did not persist its elapsed timer. A conservative charge of **96.599 seconds** uses wall time from creation of its script to creation of the failure receipt, including time after the process stopped. This upper bound is not a measured workload duration. It exceeds the planned 60-second setup reserve, so that reserve cannot be certified from the retained timing evidence; no native work proceeded. The overall 30-minute / 4-GiB envelope is accounted separately at closure, including builds/output and conservative allowances.

This turn changes one harness source file, adds the protocol/review and updates seven active Markdown handoffs. It changes no tests, gameplay/content, dependency, migration, shared configuration, old study cap or deployment. Reliability **Fail 1/3**, adoption **Hold**. The earlier captured source verification remains valid for its captured source; the earlier eight-test-fight/two-resume scope violation remains sealed and unamended. The current registry of **481,603 reservations**, including the original unused 512, remains required.

## Final preservation and accounting

The **21.532-second** preservation audit checked **4,228 baseline checkout files**, all **118 prior reviews**, every file in the prior source-verification (**1,404**), UTC (**1,494**) and stopped-controller (**2,069**) packages, and **203 external input bindings**. Nine unrelated files changed and eight UI-verification images appeared concurrently; those changes remain intact and are listed in `preservation.json`.

History remains **145 files / 102 distinct hashes**, with an independently reconstructed union of exactly **481,603 reservations**, matching the authoritative ledger. The original 512 unused v19 values remain reserved. The shared TRX matched the captured passing result at the preservation boundary.

Measured baseline/preservation phases total **22.188 seconds**. Adding the failed setup's conservative **96.599-second upper bound**, **60 seconds** for short static work and the full **120-second seal allowance** charges **298.787 seconds / 4.98 minutes**, below the 30-minute diagnostic ceiling. This does not certify the failed setup's separate 60-second reserve. Tests were separately timed at 44.098 seconds. All test/build output, source/failed-script evidence, final document copies, whole changed files, shared TRX and a 2-MiB metadata allowance are charged, totaling **less than 0.4 GiB** within 4 GiB.

The package's `final-verification.json` records exact charges and unexecuted gates. `updated-files.json` and `final-documents/` preserve the final source and handoffs; whitespace/link checks run before `evidence-files.json` is written last. No further input extraction, tests, preparation or diagnostic follows the seal. Passing preservation and overall resource checks does not change **ParitySetupFailedNoCombat**.
