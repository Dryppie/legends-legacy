# Discovery comparison gate: measured completion

16 September 2026. **VerifiedDiscoveryComparisonGate**. Zero fights, runtime preparations, new values, retries and replays. All **482,821 reservations** preserved.

## Behavior and verification

The new `TowerDiscoveryComparisonGate` accepts the fixed team-coverage baseline / discovery-refinement input pair, with one label and 16 candidates/proposals per arm. It checks shared inputs, statuses, stop reasons, counts, identities, canonical recipes and measured fitness consistency. It writes a create-new, write-through receipt with input/report hashes and per-arm diagnostics before returning any nominees. Incomplete or inconsistent discovery stops the entire pair. Receipt failure, overwrite and cancellation cannot return nominees. Complete output uses the existing ranking unchanged.

All 105 tests passed. Three saved complete fixture pairs retain the unchanged top-two nominations per arm. The saved 14/16 reverse fixture durably records its two missing-team-roles rejections and stops the entire comparison before any nominations. No teams were regenerated or reevaluated by the saved-fixture diagnostic; combat strength remains unmeasured.

| Saved refinement fixture | Proposals | Evaluations | Pair gate | Total nominees |
| --- | ---: | ---: | --- | ---: |
| refinement | 16 | 16 | Ready | 4 |
| repeat | 16 | 16 | Ready | 4 |
| reordered | 16 | 16 | Ready | 4 |
| reverse | 16 | 14 | StoppedDiscovery | 0 |

The ten gate tests cover complete ranking/publication, either arm incomplete, cancelled/invalid/missing generation, relabeled partial output, duplicate/missing/unmatched evaluations, generation/proposal identity, invalid cells/fitness/order, battle/cache counts, changed inputs/caps and receipt I/O/overwrite/cancellation. The preceding 95 tests also passed through `build/run-tests.ps1`. Fabricated report battle counts describe saved synthetic measurements; **zero actual fights** ran.

Python independently checked all four saved pairs, canonical input/report hashes, proposal-result counts, schedules, statuses, unchanged ranked nominations and the 482,821-value reservation union. All **71 predecessor packages** verified unchanged. The diagnostic never calls later stages. Its total native cost was **0.1568 seconds**, **0.1406 CPU seconds**, **37,707,248 allocated bytes** and **69,689,344 peak working-set bytes**. These single-run costs include reading/writing fixtures and receipts; they are not search or campaign speedup measurements.

## Preservation performance

The exact input manifests, consumed files and prior eight-fixture preservation receipt were verified before compilation. Full final verification checked 71 packages in 16.531s, hashing 26,584 files (4,515,045,630 bytes) with four workers and a fresh per-pass cache. All original assertions remain in use. Both earlier failed scopes are included. This scope moved the full history audit to a single mandatory final pass; publication requires its success. No previous partial digest cache was reused. The earlier 20/35-second partial timeouts are not comparable complete baselines, so no speedup ratio is claimed.

## Remaining boundary

The incomplete-arm rule is now fixed: stop the entire comparison, retain all charges and diagnostics, and do not screen or confirm partial discovery. The opt-in nomination gate is available for a separately frozen baseline/refinement comparison driver; integration into that full driver and combat preparation/execution have not run. Any next scope must carry the latest remaining time/output allowance and requires a separate fresh-seed exception before combat. No refill, cap increase, legality relaxation or partial-study quality claim. Adoption remains Hold.

The gate consumes already verified archives; it does not replace archive verification or the durable combat-attempt journal. Historical sealed adapters are unchanged. No full gameplay build, full backend suite, new policy combat comparison, runtime preparation or live history scan ran. Search, allocation, gameplay, defaults and ability order remain unchanged.


## Resources and reproduction

The [frozen protocol](Tower-Discovery-Comparison-Gate-Closure-Protocol.md) capped this scope at 90 diagnostic seconds / 128 MiB. Incoming usage was **2,901.113 / 3,000 seconds**, leaving **98.887 seconds**, with the cumulative 4 GiB cap unchanged. The two earlier failed preservation scopes remain sealed and charged. The [completion receipt](../TestResults/balance/tower-discovery-comparison-gate-closure-20260916/completion.json) records measured cumulative usage and remaining allowance, including one publication closure second and 1 MiB for shared test artifacts. Source editing is excluded; compilation, tests, fixtures, audits and publication are charged.

| Completed phase before publication | Seconds |
| --- | ---: |
| audit | 0.375 |
| build | 3.766 |
| captured | 0.313 |
| freeze | 1.281 |
| preserve | 16.594 |
| test-build | 2.125 |
| tests | 1.953 |

Failure receipts: none. No retry or budget reset. Missing success receipts identify unrun dependent commands.

Executed once under the frozen scope; never repeat these against a sealed or failed directory:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-discovery-comparison-gate-closure-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/workflow.py" preserve
& $python -B "$work/publish.py"
```

Exact command arguments, nine test filters, source/binary hashes, TRX, logs, gate receipts and independent audit are retained in the evidence directory. Changed files in this scope: protocol/review and isolated verification scripts; six active Markdown handoffs updated. Existing gate/test source and the bounded preservation wrapper are unchanged from the sealed failed scopes. Unrelated dirty work is preserved. No configuration change, migration or deployment.

V19 remains Unresolved with 253 recipes and the original unused 512 confirmation values preserved. Reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. No Kharad tuning, ability-order optimization or large confirmation.
