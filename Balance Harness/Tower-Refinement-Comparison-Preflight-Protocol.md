# Refinement comparison: seed-free input binding

16 September 2026. Target: offline BalanceHarness. Incoming [driver verification](Tower-Refinement-Comparison-Driver-Verification-Review.md) used **2,977.793469456 / 3,000 diagnostic seconds**; **22.206530544 seconds remain**. This scope permits **21 seconds / 96 MiB** within the unchanged cumulative **4 GiB** limit. Source editing is excluded; all builds, tests, audits and publication are charged. No fresh values, fights, runtime preparations, replays or retries. No old evidence may be modified.

Add `TowerRefinementComparisonPreflight`: verify a pinned request, captured content/settings/gameplay identities, two fixed-order controls with the generated context, the sealed registry snapshot, verified controller receipt and complete **482,821-value** reservation union. Save a binding marked **BoundAwaitingAuthorization**, `runAuthorized=false`, and `requiresLiveRegistryRefresh=true`. It cannot allocate seeds or run combat. The existing driver/model/search remain unchanged. This is an input-binding component; an authorized allocator and live execution launcher remain separate work.

Freeze in `TestResults/balance/tower-refinement-comparison-preflight-20260916` before execution. Run once, sequentially:

1. **Freeze** (at most 3 seconds): verify consumed files against the sealed driver, preparation and verification manifests; capture all C# source and the new tests; pin the request, scripts, protocol, gameplay/compiler inputs, current ledger and prior verification receipts. Preserve a fresh dirty-file snapshot.
2. **Build** (at most 7 seconds) and **test-build** (at most 4 seconds): compile isolated .NET 10 executables against the captured gameplay assemblies and existing restore assets; no restore or ordinary gameplay build.
3. **Tests** (at most 5 seconds): exactly eight new backend facts through `build/run-tests.ps1`. Cases: gameplay changes with harness-only changes allowed; missing gameplay identity; complete ledger/count mismatch; pending ledger; missing/relative pins; cancellation before reads; changed bytes; fixed-order controls/changed identity. Synthetic inputs only. Reuse the existing driver source and prior 117-test evidence without repeating those tests.
4. **Captured** (at most 4 seconds): one seed-free preflight on real saved inputs. Persist binding, canonical controls, timings, CPU, allocations and peak memory. Fail on any battle trace event. No materialization.
5. **Audit** (at most 2 seconds): independently check content, controls, history union, gameplay identities and the pending-authorization flags; rehash every consumed input and producing binary. Check unrelated dirty files and document updates at publication.
6. **Publish**: reserve four seconds (including one closure second), update six active handoffs and seal success or failure evidence. Normal phases share **17 seconds**; subprocess cleanup is inside each envelope. Stop dependent execution on the first failure and preserve it; never repeat a started phase.

The verified Windows owned-job wrapper bounds child processes. Reuse its six passing fixtures by exact hash. Unlike the preceding scope, this scope performs a **targeted consumed-input preservation audit**, not a new full historical scan. The [prior full audit](../TestResults/balance/tower-refinement-comparison-driver-verification-20260916/control/final-preservation-metrics.json) verified 74 packages in 15.047 seconds. Keep that evidence, explicitly distinguish it from this targeted pass, and do not imply every historical byte was rechecked now. Any future allocation needs a fresh live registry scan; this snapshot cannot prove no later reservation exists.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-preflight-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

On failure run only `publish.py failure`. No new combat is authorized. Earlier seed approvals are exhausted. V19 retains 253 recipes and Unresolved status; preserve its unused 512 confirmation values. Reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. No gameplay changes, Kharad tuning, ability-order optimization, configuration change, migration or deployment. No strength or throughput improvement claim follows from this binding.
