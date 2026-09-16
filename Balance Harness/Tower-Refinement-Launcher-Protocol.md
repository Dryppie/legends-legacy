# Refinement launcher: frozen seed-free integration verification

16 September 2026. Target: offline BalanceHarness. Incoming [preflight receipt](../TestResults/balance/tower-refinement-preflight-order-correction-20260916/completion.json): **3,003.574469456 / 3,030 diagnostic seconds**, **26.425530544 seconds remain**. This new scope allows **25 seconds / 96 MiB**, including temporary fixtures, inside the unchanged cumulative **4 GiB** cap. Source editing is excluded; builds, tests, measurements and publication are charged. Zero actual fights, preparations, fresh balance values, replays and retries.

Add `TowerRefinementComparisonLaunch` and `TowerRefinementReservation`. Production `Check` validates the preflight and refreshes the complete live registry without deriving seeds. `ReserveAndBind` requires exact external request/protocol authorization for 45 values and 288 attempts. It uses the existing allocation lease, bounded storage and durable Start/Candidate journal, preserves Pending through failures, binds both definitions, rechecks external history, and publishes the binding inventory last. `Run` requires that same authorization, exact immutable binding and freshly checked history; it deducts the entire binding-time allowance, dispatches the existing comparison driver, reconstructs its archives and publishes a bounded final inventory. No automatic invocation or default-policy change.

The complete registry previously took **57.467 seconds**, as recorded in the [registry review](Tower-History-Registry-Performance-Review.md). It cannot fit here and is deliberately **not executed**. Test the actual scanner on tiny filesystem trees, and allocation/launch through injected literal labels and the existing fabricated controller transport. Synthetic files remain under a dedicated temporary directory outside the production registry; the retained case is zipped so fake ledger names never enter the live registry. No default production candidate derivation or combat engine call. Legacy allocators that do not share `complete-family-allocation` still require exclusive operation; the scanner does not provide an atomic snapshot against unrelated writers.

Freeze in `TestResults/balance/tower-refinement-launcher-20260916`, then run once, sequentially:

1. Freeze (at most 3 seconds): check the prior sealed package and consumed inputs; require existing C# unchanged except the two new implementation files. Capture source, unchanged fixture dependencies, ten new tests, protocol and scripts. Preserve dirty files and the 482,821-value ledger. Create a new dedicated temporary fixture root.
2. Harness build (at most 8 seconds), test build (at most 5 seconds): isolated captured gameplay references and existing restore assets, build-server/node reuse disabled, no restore or gameplay build.
3. Tests (at most 9 seconds): exactly ten facts via `build/run-tests.ps1`. Wrong authorization/envelope; new/Pending registry inputs; stage order and no binding retry; durable charge and returned-label cancellation; changed registry with retained reservations; interruption before Complete; transcript tampering; candidate/storage caps; changed history before dispatch; complete synthetic launch with archive reconstruction and binding-time charging.
4. Captured synthetic integration (at most 5 seconds): one complete binding/launch/reconstruction with literal 1+4+8+32 labels and fabricated observations. Persist its transcript, binding, archive inventory, allocation/timing/CPU/memory measurements and a zipped copy of its tiny registry. No real registry refresh or captured gameplay materialization. Reuse earlier actual preflight evidence by unchanged source and input identity.
5. Independent audit (at most 2 seconds): inspect the ZIP, rebuild the four literal schedules and 90 journal events, check complete ledger and both definition policies, verify all archive hashes, binding-time charge, pending-authorization boundary and zero real workload. Recheck consumed input and producing-binary hashes.
6. Publish: four seconds reserved, including one closure second. Normal phases share **21 seconds**. Check unrelated dirty files, links and whitespace, update six active handoffs and seal success/failure with all measured time/output. The first failure stops dependent diagnostics. No retries or cap extension. Child-process cleanup remains inside each phase envelope.

Temporary fixtures count toward new output and remain evidence; no production ledger is written. Their exact bytes plus a 1 MiB shared-test allowance are carried forward. Recheck the immediate package and consumed inputs, without repeating the old 74-package audit. Existing eight preflight tests, 117 driver tests and six process-wrapper fixtures remain earlier evidence, not additional new executions.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-launcher-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

On failure use only `publish.py failure`. A live registry check and specific authorization/resource envelope are still required before production allocation or combat. Preserve all **482,821 reservations**, including v19's 512 unused values. V19 retains 253 recipes and Unresolved status; reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. Fixed ability order, gameplay, configuration and migrations stay unchanged; no deployment.
