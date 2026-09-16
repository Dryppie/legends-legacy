# Fresh-first V5 implemented and verified without combat

16 September 2026. **VerifiedZeroCombat**. All **85 backend facts passed** through `build/run-tests.ps1`; the independent saved-output audit passed. Zero fresh balance values, preparations, fights, combat replays or retries.

The new opt-in `independent-discovery-refinement-fresh-first-v5` spends a sixteen-attempt budget on **twelve fresh proposals followed by up to four local edits**. V4 spends seven on fresh construction and nine on edits. This implements the allocation follow-up from the saved V4 diagnosis; it does not establish that 75/25 is optimal or produces a stronger finalist.

For budget N, V5 reserves `N - floor(N/4)` initial proposal positions for fresh construction. It requires equal candidate/attempt caps, from one through sixteen. The remaining positions use the existing single-Essence, single-slot operator on the best completed parent. Rejections and duplicates consume positions. Without a completed parent, the existing charged fresh fallback applies; no extra position or refill is granted.

V5 preserves V4's edit algorithm and random stream, fixed ability order, fresh recipe sequence, legality checks, ranking, nominations and selection. Defaults and V1–V4 remain unchanged. Explicit comparison `tower-discovery-refinement-comparison-v4` and gate `tower-discovery-comparison-gate-v4` identify the new allocation; existing driver receipt and authorization checks reject mismatches before allocation.

## Verification and measurements

- **85/85 tests passed:** 68 existing facts, ten allocation facts, six version/integration facts and one full-serialization parity fact. The latter compared V1, V2, V3 and V4 against the sealed tested V4 assembly under identical synthetic inputs; all hashes matched, including legacy incomplete outcomes.
- The sixteen-candidate synthetic V5 fixture completed with exactly twelve baseline-prefix fresh recipes and four canonical local edits. Parent choice, unchanged edit/random offset, all supported small budgets, metadata determinism, exhaustion without refill and cancellation/checkpoint charge preservation passed.
- Synthetic binding, reconstruction, incomplete-discovery stopping and authorization rejection passed. Test journal callbacks are fabricated fixtures, not executed fights or production reservations.
- The existing maximum-neighbourhood fixture inspected all **6,400** options in **103.4661 ms**, with the expected skips. This single fixture timing is not a combat throughput or policy-strength comparison.
- Harness compilation took **3.484 s**, test compilation **1.953 s**, test execution **16.047 s**, and the audit **0.250 s** under the owned process wrapper. Harness build: zero warnings/errors. Test build: one existing CS0649 warning for the synthetic fixture's unassigned `Cancel` field; zero errors.
- Relevant source/dependency hashes and the sealed trajectory checked out. **759 unrelated pre-existing dirty files** passed preservation checks with no concurrent changes. Scoped source and Markdown whitespace checks passed. No required command was blocked or skipped.

The [protocol](Tower-Fresh-First-Refinement-Protocol.md) freezes this scope at 180 diagnostic seconds / 13 MiB within the unchanged cumulative 4,260-second / 4,613,734,400-byte caps. The harness is retained at its intermediate path and loaded by the pinned test resolver, avoiding a redundant DLL copy; gameplay dependencies are referenced unchanged. Temporary fixtures are retained and counted, with 1 MiB reserved for shared test-runner output. Exact final usage, including inspection and publication, is in the [completion receipt](../TestResults/balance/tower-fresh-first-refinement-20260916/completion.json).

[TRX](../TestResults/balance/tower-fresh-first-refinement-20260916/control/tests.trx), [exact test command/environment](../TestResults/balance/tower-fresh-first-refinement-20260916/control/tests-command.json), [serialized parity](../TestResults/balance/tower-fresh-first-refinement-20260916/legacy-parity.json), [saved schedule](../TestResults/balance/tower-fresh-first-refinement-20260916/fresh-first-schedule.json), [audit](../TestResults/balance/tower-fresh-first-refinement-20260916/audit.json). The evidence retains source/input pins and all build/test exit records.

Executed once; completed phases reject retries:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-fresh-first-refinement-20260916'
# In order: freeze, build, test-build, tests, audit, publish
& $python -B "$work/workflow.py" <phase>
# The tests phase invokes build/run-tests.ps1 -NoBuild with the frozen
# ten-class filter, isolated artifacts and pinned resolver environment.
```

Changed files: allocation/version logic in `TowerDiscoveryRefinementSearch`, `TowerBossGeneration`, comparison model/gate and six policy-registration files; two new test classes; this protocol/review, evidence and six active Markdown handoffs. No gameplay/content, configuration, migration or deployment changes.

V5 remains opt-in and combat strength remains unmeasured. A prospective comparison would need a newly frozen request and explicit fresh-seed/resource approval; no old study can be resumed or rebound. Keep nomination and selection rules unchanged for that allocation comparison. All **483,001 reservations** remain excluded. V19 retains 253 recipes, no confirmation and 512 unused values; reliability Unresolved, later Fail 1/3, deep recovery 0/3 and adoption **Hold**.
