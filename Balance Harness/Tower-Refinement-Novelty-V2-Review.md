# Refinement novelty — FailurePreserved

16 September 2026. Execution stopped without retry at ['captured-failure.json']. Observed test counts: {'total': '28', 'executed': '28', 'passed': '28', 'failed': '0', 'error': '0', 'timeout': '0', 'aborted': '0', 'inconclusive': '0', 'passedButRunAborted': '0', 'notRunnable': '0', 'notExecuted': '0', 'disconnected': '0', 'warning': '0', 'completed': '0', 'inProgress': '0', 'pending': '0'}. Dependent phases were skipped.

The corrected probe reproduced v2's saved synthetic hash exactly. Proposal 12 (`loadout-distribute`) copied a source loadout to six slots that already had that loadout, yielding the existing parent from proposal 5. This was a no-op, not a combat result or evidence of build strength.

Opt-in `independent-discovery-refinement-novel-v3` constructs a distribution from a finite neighbourhood: start at the sampled donor/count, visit each donor and prefix length at most once, retain required roles, and accept only legal parties absent from this arm's completed measurements. The bound is library count times party size (at most 128 × 10 = 1,280 construction checks). It consumes the same random draws, emits at most one proposal, records actual targets/check counts and never calls the evaluator during construction. If exhausted, the unchanged parent reaches ordinary duplicate charging. Other operators can still produce duplicate or invalid proposals; this is not a universal completion guarantee.

V1/v2 behavior, final inventory/family/role validation, canonical ability order, ancestry, cancellation and the 16-proposal cap remain enforced. The comparison launcher remains on its original v1 contract.

Do not launch a combat comparison. Resolve the remaining recorded construction or verification limitation in a new frozen zero-combat scope. Keep all existing caps and seed reservations; the current comparison launcher still selects v1.

No fights, preparations, combat replays, fresh values or retries ran. All 482,866 reservations remain, including the failed comparison's 40 unused values and v19's separate 512 unused values and 253 recipes. V19 reliability Unresolved, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. Synthetic scores establish construction behavior only; no strength or whole-run throughput improvement is claimed.

## Measurements and verification


| Executed phase | Seconds |
| --- | ---: |
| Corrected probe build/search | 1.906 |
| freeze | 0.078 |
| build | 2.906 |
| test-build | 1.656 |
| tests | 2.140 |
| captured | 0.062 |

The [completion receipt](../TestResults/balance/tower-refinement-novelty-v2-20260916/completion.json) includes publication cost and exact remaining cumulative resources. The TRX and logs retain every observed result. Gameplay binaries were reused by hash; source edits were confined to the harness.

## Reproduction and changed files

The [frozen protocol](Tower-Refinement-Novelty-V2-Protocol.md), `probe/freeze.json` and `freeze.json` pin inputs and sources. From the repository root, the commands executed once were:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/diagnose.py'
# Invoke each workflow phase separately and stop on any nonzero exit:
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/workflow.py' freeze
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/workflow.py' build
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/workflow.py' test-build
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/workflow.py' tests
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/workflow.py' captured
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/workflow.py' audit
& $python -B 'TestResults/balance/tower-refinement-novelty-v2-20260916/workflow.py' publish
```

This package is sealed and once-only; reproduce from its pinned sources/assets in a new explicitly bounded output directory. Exact build/test commands and environments are saved in `control/*-command.json`. Tests ran through `build/run-tests.ps1 -NoBuild` with the isolated artifact path and the three refinement test-class filters. No unplanned suites or combat commands were run.

Changes: `TowerDiscoveryRefinementSearch.cs` adds bounded novel distribution; `TowerLoadoutComposition.cs` records construction checks and dispatches v3; `TowerBossGeneration.cs` supplies completed arm identities and cancellation. Five policy/validation files recognize explicit v3. `BalanceHarnessRefinementNoveltyTests.cs` adds eight tests; 20 existing cases remain unchanged. This review/protocol and six active handoffs are updated. No migrations, configuration, gameplay-content or deployment changes.
