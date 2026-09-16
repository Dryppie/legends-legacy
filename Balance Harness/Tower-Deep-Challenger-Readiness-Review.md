# Deep challenger readiness verified

15 September 2026. The offline BalanceHarness is prepared under the [readiness protocol](Tower-Deep-Challenger-Readiness-Protocol.md) and the [study design](Tower-Deep-Challenger-Study-Protocol.md). **PreparedAwaiting331ValueException. Zero fights, fresh seed candidates or reservations.** Both earlier failed preparations stay sealed.

## What now works

The control adapter declares one already reserved historical seed only in its temporary preparation input. It leaves exported control recipes and the future template seed-free, retaining ordinary input validation. Both fixed controls now prepare successfully and match their sealed participant hashes exactly. Their identities are **team-040e60d3dbc5c127321653c47ed3a9d3** and **team-49f6979895354870c89362d4abf214bb**, selected from the earlier 61/256 and 59/256 observations; these counts are not pooled into a new estimate.

The check reused the proven **4,608 evaluations / 4,750 proposals** by exact hashes and object equality. Generation was not run again. Captured +10% content-derived mechanics/coverage match the existing deep policy. All **481,891 reservations** remain present, including unused original512 and unused32. The seed-free template cannot run.

## Measured verification

| Check | Result |
| --- | --- |
| Captured-gameplay adapter build | Passed, 3.719 seconds |
| Focused preparation tests through `build/run-tests.ps1` | **3/3 passed**, 1.906 seconds for wrapper execution |
| Once-only native readiness check | Passed, **113.688 seconds** |
| Once-only independent saved-output audit | Passed, **41.125 seconds** |
| All diagnostic workloads including both earlier failures | **168.016 / 1,800 seconds** |
| Three preparation directories before report publication | **0.833 / 1 GiB** |
| Cumulative setup charge before publication | **74.88 / 180 minutes** |

The independent audit verified temporary seed declaration, control selection, generation-evidence reuse, input/producing pins and the full reservation union. All 257 maximum-family rate intervals and the paired capacity-edge cases agree within **9.52e-11**. The previous 91 passing synthetic generation/selection/accounting cases are retained evidence; they were not rerun for this adapter-only change.

Development receipts retain an initial test assertion compile error, a wrapper invocation that discovered no tests and copied stale results (not counted as a pass), and three failed tests caused by combining captured assemblies with newer checkout content. The corrected fixture locator, retained SDK metadata and explicit captured-content binding produced three actual passing cases before the diagnostic freeze. No failure receipt was overwritten, and neither frozen diagnostic was retried.

## Prepared combat scope and remaining limits

Use the unchanged deep method on **3 x 1,536 candidates x 8 trials = 36,864 discovery fights**, then **3 x 32 x 64 = 6,144 screening fights**, and at most **256 x 256 = 65,536 confirmation fights**: **108,544 maximum**, zero retries/resumes. Both controls, original and screened nominees, and every observed ceiling breach are retained; overflow preserves all required teams and stops before confirmation. Historical controls never enter independent generation.

Limits remain **10,800 charged seconds and 4 GiB total**, with native execution capped at **5,400 seconds and 3 GiB**. The outer remaining allowance can shorten native execution. All earlier setup time and all three preparation directories count; the limit is not reset. Waiting for the user's allocation decision is excluded. The complete controller still has not run a real new discovery campaign, so exact runtime and search recovery remain unmeasured.

The existing focused 989-team result remains Pass within that selection; 42,890 other retained teams are outside it. Historical portfolio reliability remains **Fail 1/3**, adoption **Hold**; sealed v19 remains **Unresolved / 253 recipes retained / no internal confirmation**. This readiness result establishes no new balance acceptance or measured speedup.

## Exact commands and required exception

Executed build/test/diagnostic commands are saved with arguments, environment, logs and durations in `control`. The passing wrapper command uses `build/run-tests.ps1 -NoBuild -Filter FullyQualifiedName~BalanceHarnessTowerDeepPreparationTests -ArtifactsPath <work>/producing/tests`, with `LL_TEST_API_ROOT` bound to the sealed original v19 content. The focused assembly contains exactly the three new preparation tests. Source and executable are pinned before the diagnostic commands below; do not repeat them against this sealed package.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-deep-challenger-readiness-20260915'
& $py -B "$work/workflow.py" freeze
& $py -B "$work/workflow.py" check
& $py -B "$work/audit.py" preparation
```

The user's earlier **zero fresh balance seeds** restriction still prevents allocation. The concrete exception is **331 new values: 3 generation roots + 8 discovery + 64 screening + 256 confirmation seeds**, excluding all 481,891 prior reservations; expected total **482,222**. No original unused value is reassigned or released. After explicit approval of that exception and the frozen study, execute once:

```powershell
& $py -B "$work/workflow.py" reserve-331 --approved-331
& $py -B "$work/workflow.py" run
& $py -B "$work/workflow.py" verify
& $py -B "$work/audit.py" execution
& $py -B "$work/finish.py" execution
```

Allocation and combat have not run. Their receipts will go to a separate execution directory; study output will go to a separate study directory. Pending intents and every allocation/attempt charge remain durable. Any failure closes that execution without retry or resume.

## Changed files and implications

Added the temporary-control-input helper in `TowerDeepChallenger.cs` and `BalanceHarnessTowerDeepPreparationTests.cs`. Added this protocol/review, isolated adapter/test assembly and saved workflows; updated seven active Markdown handoffs. The existing generation, search, scoring, recipes, combat rules and resource caps were not edited. Unrelated dirty backend/UI changes were preserved against a pre-edit checkout inventory. No migration, production configuration change, gameplay-content edit or deployment. All required readiness checks passed; allocation/run commands remain pending the explicit exception.
