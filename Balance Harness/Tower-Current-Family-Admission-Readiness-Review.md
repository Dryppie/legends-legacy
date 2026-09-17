# Current-family Tower admission: captured runtime and bounded request

17 September 2026. Target: offline BalanceHarness. **The runtime and request are prepared for one native admission, capped at 1,800 seconds /2 GiB overall, with zero combat and zero fresh values. That admission has not run.** This step captured binaries/content, checked runtime/static compatibility, reran 45 adapter fixtures and verified six saved-row accounting fixtures. No party was prepared through the production materializer, no native family was frozen, and no allocation, tuning, deployment or old-budget extension occurred.

The [readiness package](../TestResults/current-tower-admission-readiness-20260917) contains the exact [request](../TestResults/current-tower-admission-readiness-20260917/request.proposed.json), [launch plan](../TestResults/current-tower-admission-readiness-20260917/launch-plan.json), runtime/content inventories and [captured context](../TestResults/current-tower-admission-readiness-20260917/captured-context.json). It follows the [implemented adapter](Tower-Current-Family-Admission-Implementation-Review.md) and unchanged [calibration design](Tower-Current-Gameplay-Calibration-Design.md). The exact candidate remains **AdoptFixedTeam**, balance **NotAssessed**, V19 **Unresolved**, and permanent exclusions **497,371**.

## Runtime binding and compatibility

The capture has **26 files**: **25 are byte-identical to the retained fixed-team runtime**, including `Application.dll`, `Common.dll`, `Domain.dll`, `Services.LL.dll`, dependencies, runtime configuration and dependency metadata. Only `BalanceHarness.dll` comes from the verified adapter build. This resolves the isolated test build's four gameplay-DLL hash differences without rebuilding or replacing the baseline gameplay code.

- Captured harness SHA-256: `9023b40a46351460699ec56006eabc734b9a4af53e9e92069123823650027493`.
- Captured execution hash: `a757afdaf01e530672d8e0c77a9693fbbb703b2a9145fb6c0e913ab8a9c6beb2`.
- Effective Tower settings hash: `f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74`.
- Original **16 content files** remain byte-identical in the isolated content copy. Its minimal `appsettings.json` contains only threat settings, the Tower checkpoint interval and the existing idle cadence required by the settings reader; no raw application configuration or secrets were copied.

The producing runtime passed the adapter's actual `CheckDesign`, `CheckRuntime` and `Load` paths. The loader read **46,077 projections**, **51,624 source occurrences**, **973 forced input cells** and **162 incompatible occurrences**, and checked all three required control vectors. Seven preparation/adapter entry or async-state methods were JIT-resolved without invoking `CreateInput`, `PrepareAsync`, `Scan` or `Run`. The native CLI help command also succeeded. The three inspected processes exited with no active children, under the retained Windows Job Object owner.

These checks establish loading and contract compatibility, not complete production preparation or its throughput. The fixture assembly was copied separately and all captured runtime members were installed beside it. **45/45 adapter fixtures passed** via:

```powershell
./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessCurrentFamilyAdmissionTests' -ArtifactsPath 'TestResults/current-tower-admission-compatibility-20260917/test-build'
```

The first attempt exited without discovering tests because the copied output lacked the SDK's generated test-project import metadata. It is retained as an empty log and is **not counted as a passing run**. Copying the existing project metadata enabled the actual 45-case run; no rebuild or gameplay assembly substitution occurred. The active capture script includes that correction, while `prepare-source.py` preserves the source that produced this capture. Results are in the [compatibility TRX](../TestResults/current-tower-admission-compatibility-20260917/compatibility-tests.trx) and [successful wrapper log](../TestResults/current-tower-admission-compatibility-20260917/fixture-tests-with-metadata.log). No required command remains blocked.

## One bounded native admission

| Scope | Ceiling / behavior |
| --- | --- |
| Entire future operation | **1,800 seconds /2,048 MiB**, including setup checks, captured runtime/content, native output, logs, independent saved counts and closeout. |
| Native request | **1,500 seconds /1,920 MiB**; one preparation attempt per projected cell, built-in saved-row audit and sealing. |
| Remaining capacity | **300 seconds** for enclosing work; at most **64 MiB** readiness package plus **64 MiB** logs/oversight/closeout. No allowance transfer from earlier closed studies. |
| Process control | Retained suspended-process Windows Job Object, no breakaway, deadline termination and empty-job confirmation. The wrapper checks combined bytes during execution; this is sampled monitoring, not an OS disk quota. Native output growth has its own pre-write checks. |
| Retry policy | New output directory, one launch claim, **zero retries/resumes/replays**. Stop on drift, native failure, deadline or bytes; retain partial evidence. No failed-cell thinning or cap increase. |
| Scientific scope | **Zero fights, entropy draws or new reservations**. No screen, confirmation, ordering search or live-content changes. |

The [enclosing launcher](analysis/current-tower-native-admission.py) fixes the command, producing inputs and limits, invokes the native adapter once, and independently checks saved counts/origins/aliases before closing. It never launches the saved verifier a second time or repeats preparation. Its independent count reader checks hashes and accounting; it does not independently recreate native participants or prove gameplay behavior.

**Six accounting fixtures passed**: complete aliases and forced counts, conflicting aliases after re-signing, missing rows, changed origins after re-signing, false readiness, and preserved invalid-row accounting. They use fabricated rows over the static inventory, contain no production-prepared evidence, and remain outside the scientific registry. See [fixture source](../TestResults/current-tower-admission-compatibility-20260917/launcher-checks.py) and [results](../TestResults/current-tower-admission-compatibility-20260917/launcher-checks.log). The enclosing native `run` path has not been exercised; this is a prepared operation with tested accounting and a previously verified process owner, not an end-to-end native success claim.

Current read-only verification command:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'TestResults/current-tower-admission-readiness-20260917/execute-source.py' verify-ready
```

The next bounded operation, **not executed during this readiness step**, is:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'TestResults/current-tower-admission-readiness-20260917/execute-source.py' run
```

It claims `TestResults/current-tower-native-admission-20260917` and writes native evidence beneath `native/`. Current capture/editing/fixture work is separately unmetered engineering, disclosed here; the future operation's full 1,800-second /2-GiB allowance would close on completion or failure. Neither the earlier 4,800-second /2,304-MiB chain nor any old scientific cap funds it. The allowance is a limit, not a prediction that all 46,077 preparations will finish.

## Context disposition and decision

The [disposition plan](../TestResults/current-tower-admission-readiness-20260917/context-disposition-plan.json) lists every affected source ordinal. No exception has been waived or reclassified.

| Group | Occurrences | Remaining decision |
| --- | ---: | --- |
| Historical character/identity mismatch | 90 | Preserve original non-neutral contexts; decide explicitly whether separate-context admission/coverage is required. |
| Later character/identity, floor/schema and slot/Essence mismatch | 21 | All carry forced-control provenance. Resolve the original contexts and coverage obligation before complete family freeze. |
| Duplicate source creature | 51 | Retain the original recipes and native-legality evidence; never interpret invalidity as weak combat performance. |

If all projections prepare, expected native status is **`AdmittedWithContextExceptions`**, exit **3**, with `ReadyForFamilyFreeze=false`. That is a completed admission with unresolved coverage, not a balance Pass or a process failure to retry. Invalid projections produce **`AdmissionIssues`**, exit **1**, and remain in the ledger. Either result requires review before family freeze; a partial/resource-stopped run cannot substitute for complete admission. The wrapper accepts only the corresponding complete result/exit pairing and preserves stopped evidence.

**Next: execute the single bounded native admission, then review native legality, identities, resource cost and all context exceptions.** Screen and relevant-family confirmation remain separate unimplemented execution scopes. This readiness does not establish method reliability, acquisition feasibility or global optimality.

## Changes and preservation

Added [capture orchestration](analysis/current-tower-admission-readiness.py), [runtime inspection](analysis/current-tower-admission-context.ps1), [enclosing admission launcher](analysis/current-tower-native-admission.py), this review and the isolated readiness/compatibility packages. Updated current Markdown handoffs. Backend/gameplay source and old controllers are unchanged; original design, implementation, runtime, scientific receipts and reservation ledgers remain sealed. Copies and source snapshots are pinned separately rather than refreshing old hashes.

Verification covers runtime/content hashes before and after fixtures, native static contract loading, the actual 45-case TRX, six accounting fixtures, Python/PowerShell syntax, current local links/whitespace, prior-source preservation and sealed historical evidence. No migration, dependency installation, application configuration change or deployment is required. The sanitized settings file belongs only to the offline captured content directory.
