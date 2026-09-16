# Refinement preflight: fixed-order correction and pending verification

16 September 2026. **Source corrected; verification not authorized or executed.** Target: offline BalanceHarness only.

The [verification closure](Tower-Refinement-Comparison-Preflight-Closure-Review.md) proved that the test build exits cleanly with build servers disabled. Its eight backend tests produced **seven passes and one failure**. `Controls_use_fixed_order_and_reject_changed_identity` exposed an ordering error: the preflight built a generated-context choice from the historical, uncanonicalized control before calling the scenario validator, which requires fixed ordinal order for composition-only policies. The saved [failure detail](../TestResults/balance/tower-refinement-comparison-preflight-closure-20260916/control/test-failure-analysis.json) retains the exact stack. The closure remains sealed and FailurePreserved.

Corrected `LL/tools/BalanceHarness/TowerRefinementComparisonPreflight.cs` to canonicalize the control first, then use that same canonical scenario for its party choice and context comparison. Output hashes already used this canonicalization. No order search, gameplay, control composition, search method, driver, reservations or test expectations change. The unchanged eight-test suite includes the failed regression. The working-source correction has **not** been rebuilt or retested. Captured preflight and independent audit remain unrun.

## Proposed bounded verification

Incoming [completion receipt](../TestResults/balance/tower-refinement-comparison-preflight-closure-20260916/completion.json): **2,992.261469456 / 3,000 diagnostic seconds**, **7.738530544 seconds remaining**. Request an explicit **30-second increase to the cumulative cap, from 3,000 to 3,030 seconds**, making 37.738530544 seconds available. This proposed scope uses at most **30 seconds / 96 MiB**, retaining the cumulative **4 GiB** cap. **Zero fights, runtime preparations, fresh values, replays and retries.** No diagnostics start without that specific resource approval. Source/script/document preparation is excluded as before.

Prepared directory: `TestResults/balance/tower-refinement-preflight-order-correction-20260916`. Freeze setup hashes before requesting approval; after approval record the exact grant there, then execute once:

1. Freeze (at most 3 seconds): verify the sealed immediate failed scope and consumed source/gameplay/compiler inputs. Require root C# and tests to match the captured version except this exact canonicalization correction. Copy current harness source and unchanged fixtures/tests to the new scope. Preserve dirty-file hashes and all 482,821 reservations.
2. Harness build (at most 10 seconds) and test build (at most 5 seconds): use the captured gameplay assemblies and existing restore assets, with `--disable-build-servers -nr:false -m:1 -p:UseSharedCompilation=false` and process-local server/node-reuse disabling. No restore, gameplay rebuild or machine-wide process shutdown.
3. Tests (at most 5 seconds): run exactly eight facts against the corrected binary through `build/run-tests.ps1 -NoBuild`, requiring eight passes, zero failures and a fresh matching TRX. This verifies corrected source in a new scope; never retry or modify either sealed failed package.
4. Captured preflight (at most 4 seconds): one seed-free check of the real saved inputs; retain canonical controls, binding and detailed performance trace. Reject all battle events. No materialization.
5. Independent audit (at most 2 seconds): reconstruct the content/control/history identities and pending-authorization flags; verify actual producing binaries and all consumed pins. No full historical audit or live registry scan.
6. Publish: reserve four seconds, including one closure second. Normal phases share 26 seconds. Stop dependent execution on first failure or exhaustion, preserve all evidence, update active handoffs and report exact remaining resources. Zero-active-process cleanup remains mandatory within every phase allowance.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-preflight-order-correction-20260916'
# Execute only after the explicit 30-second cumulative-cap increase is recorded.
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

On failure, run only `publish.py failure`. The input binding cannot authorize combat or allocate seeds. Future live-registry/allocation/launcher integration and any fresh-seed request remain separate. No v19 rerun or confirmation: preserve all 253 recipes and its 512 unused confirmation values. Reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. No configuration, migration or deployment changes.
