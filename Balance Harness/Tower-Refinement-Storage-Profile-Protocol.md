# Refinement comparison: opt-in storage profile verification

16 September 2026. Target: offline BalanceHarness. Incoming [resource receipt](../TestResults/balance/tower-refinement-resource-readiness-20260916/completion.json): **3,131.148469456 / 3,240 diagnostic seconds**, **108.851530544 seconds remaining**. This scope allows **60 seconds / 48 MiB**, including retained temporary fixtures and a 1 MiB shared-test allowance, inside the unchanged cumulative **4 GiB** cap. Normal phases share 56 seconds; four seconds are reserved for publication. Editing is excluded; builds, tests, measurements, audits and publication are charged. Zero real fights, runtime preparations, fresh values, replays and retries.

## Opt-in behavior

Add `shared-executable-compact-json-v1` to the refinement launch request. The field is omitted by default, preserving the legacy serialized request/options. It is included in request/authorization hashes. Unknown profiles and a returned archive with a different profile are rejected. An async-context-local serializer removes whitespace only; all integers remain ordinary JSON arrays and canonical semantic hashes remain unchanged.

The comparison retains one bounded executable bundle and an exact manifest under its own run directory. Each direct-child stage uses schema 2 and the fixed `../executable` reference; schema 1/default archives retain their existing behavior. Reference files bind the manifest bytes. Verification checks execution identity, exact membership including hidden files, every file hash, path containment and reparse points through the existing traversal plus ancestor checks. The complete outer inventory includes the shared files once. Moving the complete comparison preserves verification; individual stages require their shared sibling bundle. No pointers to mutable producing folders, hard links or deletion of historical evidence.

Executable copying receives the remaining byte limit and checks it before each write. Cancellation leaves partial evidence; no retry/resume. The new mode preserves durable attempts, Pending reservations, the controller's limits and reconstruction. Compact JSON changes representation only, not schedules, recipes, nominations, ranking, combat or gameplay.

## Exact verification

Package: `TestResults/balance/tower-refinement-storage-profile-20260916`. Freeze source, scripts, this protocol, tests and captured dependencies before execution. Both builds share one isolated output directory to avoid creating a second gameplay-dependency copy. Use the captured gameplay assemblies and existing restore assets; no restore, gameplay build or build-server reuse. Fixtures stay under a dedicated temporary directory outside the real registry; retain the native case as a ZIP, never as a discoverable fake ledger tree inside the registry.

Run once, sequentially:

1. Freeze, at most 4 seconds: verify the immediate sealed resource package, consumed inputs and all unchanged harness source; capture changed files and prior test receipt. No old full historical audit or real registry refresh.
2. Harness build, at most 10 seconds; test build, at most 8 seconds. Compile the new profile and captured synthetic fixtures against unchanged gameplay.
3. Exactly **32 backend facts**, at most 20 seconds, through `build/run-tests.ps1`: ten launcher, twelve controller/model and ten new shared-executable/profile tests. New cases cover async/nested/default JSON isolation and canonical parity; legacy field omission/unknown profiles; bounded copy/cancellation/no overwrite; shared creation cap/no retry; changed/missing/extra files and execution identity; reference escape/manifest tampering; relocation; opt-in versus legacy schedules/nominations/family/quality/durable attempt parity; returned-label cancellation; and authorized-profile downgrade rejection. Existing traversal reparse-point tests remain earlier evidence; no new junction fixture is scheduled.
4. Captured zero-combat measurement, at most 10 seconds: one complete opt-in synthetic binding/launch/reconstruction using literal labels and a three-byte fake executable through the injected runtime. Save the full ZIP, metrics and all four verified references. Read the existing 482,821-value historical union once; compare indented and compact serialization, exact recovered values and canonical hashes in memory. Measure bytes without writing fourteen full arrays or copying four real executable bundles. No generation, real registry traversal, production allocator or combat engine.
5. Independent audit, at most 5 seconds: check the sealed ZIP's binding/controller/final inventories, four references and shared bundle; compare schedule counts and unchanged 240 synthetic attempt pairs. Recount the real historical digits and compact-array byte length independently. Reconstruct the producing executable asset list from its dependency manifest and calculate the new component floor: fourteen compact historical arrays, one executable bundle and four content copies. Compare with the old measured floor and remaining output. A smaller floor is not a full-run upper bound or proof of combat throughput.
6. Publish using the reserved four seconds: verify unrelated dirty files, links and whitespace; update six active handoffs and record success or the first failure, all charged time/output and remaining limitations. Preserve the first failure, skip dependent phases and do not retry or increase caps.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-storage-profile-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

On first failure use only `publish.py failure`. Real compact-runtime integration and a complete time/output allowance still precede a production request. Neither 45 fresh values nor 288 real attempts are authorized. Preserve all 482,821 reservations, v19's unused 512 values and 253 recipes, v19 Unresolved, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold. No Kharad tuning, ability-order optimization, migration, configuration change or deployment.
