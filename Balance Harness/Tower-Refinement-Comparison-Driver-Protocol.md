# Discovery refinement comparison driver: frozen zero-combat protocol

16 September 2026. Target: offline `LL/tools/BalanceHarness`. Proceed with integration of the verified nomination gate into a new opt-in comparison controller. All sealed drivers and studies remain unchanged. No gameplay changes, ability-order tuning, runtime preparation, fights, replays, retries or fresh seed reservations. Synthetic labels and attempt-journal events are fixture data only.

Incoming [completion receipt](../TestResults/balance/tower-discovery-comparison-gate-closure-20260916/completion.json): **2,928.817 / 3,000 diagnostic seconds**, **71.183 seconds remaining**. This scope is capped at **65 diagnostic seconds and 128 MiB**, including a conservative 16 MiB allowance for temporary/shared test artifacts. The cumulative **4 GiB** cap remains. Source editing is excluded; compilation, tests, fixture execution, audits and publication are charged. Stop on the first failed check or limit; preserve failure and skip dependent execution. No retry.

## Implementation contract

Derive a separately named model from the sealed core-portfolio comparison model, using baseline team coverage versus discovery refinement. Preserve one generation label, 16 candidates/proposals per arm, four discovery samples per candidate, top two per arm, eight selection samples and 32 confirmation samples. Deduplicate recipes while retaining origins; select one finalist per arm with existing tie rules; add both unchanged controls. At most 288 combat attempts would be needed by a future separately authorized study. This protocol runs **zero**.

The controller must verify each stage archive, retain durable attempt charges, use campaign-owned storage accounting, write the pair gate before nominations, freeze nominations before selection and finalists before confirmation, and retain performance/failure evidence. Incomplete baseline stops before starting the candidate; incomplete candidate stops before screening. Both have durable stop receipts, no refill and no partial quality claim. The success verifier reconstructs the result from archived stage evidence and checks the exact final inventory. A future launcher still needs authorized seeds, historical registry/content/executable binding and a separate frozen execution protocol; this engineering check supplies none of those permissions.

## Exact diagnostics, one execution each

Evidence directory: `TestResults/balance/tower-refinement-comparison-driver-20260916`.

1. Freeze exact source, scripts, protocol, fixtures, test entry point, ledger and captured compiler dependency hashes. Check consumed predecessor artifacts against their sealed manifests before use. Preserve the dirty-checkout snapshot.
2. Isolated harness build and isolated test build, no restore or ordinary gameplay build.
3. **117 tests** through `build/run-tests.ps1`: the preceding 105 plus 12 controller tests. New cases: complete stage order/reconstruction/tamper; partial baseline; partial candidate; archive verification failure; report mismatch; partial selection; partial confirmation; cancellation at returned outcome/no retry; stage overrun/undercharge; storage before attempt; receipt I/O failure; changed inputs/invalid deadline/precancellation. Journal callbacks simulate events without invoking the engine. Test outputs remain preserved.
4. Native captured diagnostic: three fabricated cases, **complete**, **partial-baseline** (14/16) and **partial-candidate** (14/16). Persist all controller outputs, calls, receipts, traces, CPU/allocation/memory timing and a reconstruction of the complete case. No candidate generation or combat evaluation.
5. Independent Python audit: schedules/input equality, proposal counts, ranked nominations, deduplication, stage order, screening/finalists, paired outcomes, exact journals/manifests and 482,821 reservation union.
6. One mandatory final content-preservation audit of **72 predecessor packages**, including all failed scopes, using unchanged legacy assertions and the verified four-worker helper with a fresh per-pass digest cache. No verified completion without this pass.
7. Publish measured report, update six active Markdown handoffs, check unrelated work and links/whitespace, then seal the new scope.

Phases before preservation share **25 seconds** (freeze 8, build 12, test-build 8, tests 8, captured 6, audit 6). Preservation receives at most **40 seconds**, with all normal phases stopping by **60 cumulative seconds**. Reserve five seconds for success/failure publication, including one charged closure second. Cumulative prior time/output are carried forward, with no reset. Existing eight preservation-helper fixtures are reused by exact verified source/receipt identity, without repetition.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-driver-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/workflow.py" preserve
& $python -B "$work/publish.py"
```

Run sequentially only after the previous receipt succeeds. Any failure uses `publish.py failure` instead of dependent phases. These are reproducible command records; never rerun them against a sealed/failed directory. Preserve all **482,821 reservations**, including the original unused 512 v19 values. V19 remains Unresolved with all 253 recipes; reliability Fail 1/3, deep recovery 0/3 and adoption Hold.
