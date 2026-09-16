# Complete-family external setup — preservation verified

Completed **15 September 2026** under the [separate read-only protocol](Tower-Complete-Family-Setup-Preservation-Protocol.md). Status **SetupPreservationVerified**. The preservation gap left by the [setup scope](Tower-Complete-Family-Setup-Review.md) is closed: all **10,019 indexed files across eight sealed packages** match, protected checkout/input/producing files match, and all **481,603 reservations**, including the **512 unused original v19 confirmation values**, remain intact. **Zero tests, builds, preparations, fights, fresh seed candidates/reservations, retries or resumes** occurred in this closure.

The old stopped audit and its **SetupVerifiedPreservationIncomplete** result remain unchanged. This new result records the completed preservation check without rewriting the earlier failure. Reliability **Fail 1/3**, adoption **Hold**. Original v19 remains sealed Unresolved with no confirmation in that experiment.

## What passed

The [audit receipt](../TestResults/balance/tower-complete-family-setup-preservation-20260915/audit-result.json) and [complete checked-file records](../TestResults/balance/tower-complete-family-setup-preservation-20260915/checked-files.jsonl) retain every expected/actual hash. Package membership also matches each frozen inventory exactly, with its own seal file additional to the indexed members:

| Preserved package | Indexed files |
| --- | ---: |
| Complete-family controller | 2,069 |
| Captured source verification | 1,404 |
| Parity v1 | 1,200 |
| Parity v2 | 1,458 |
| Parity v3 | 408 |
| Parity v4 | 580 |
| UTC family binding | 1,494 |
| External setup, including its failed preservation record | 1,406 |
| **Total** | **10,019** |

The audit additionally verified **205 external input bindings**, **184 producing files**, and the setup scope's engineering hashes. It reconciled the original **4,280-file checkout baseline** with that scope's **12 recorded final changed-file hashes**. Harness implementation, tests, historical Markdown and non-UI checkout files matched their applicable recorded versions. The saved 71-test TRX matched its exact passing-name allowlist; native and independent setup success receipts, their result hash and zero-work assertions also matched. These are checks of retained evidence, not new test or native executions.

The live registry contains exactly **145 history paths / 102 distinct file hashes**, reading **484,510,010 bytes** of existing history. Its Int32-array union equals the authoritative midpoint ledger and native sorted-union hash `10915c9ff420d84850243103793d57e41b60de8d7cf42d2a2350159b7359daa1`. All **481,603 values** and the explicit **512-value unused v19 subset** remain included. Registry traversal rejected linked entries and visited **659,568 directories**, below its two-million-directory limit. No allocator ran and no registered history file was added.

## Concurrent UI work

The frozen checkout policy treated `LL/src/Presentation/ll/`, `docs/ui-rework-verification/` and the exact top-level `UI_REWORK_IMPLEMENTATION_PLAN.md` as concurrent work outside this Tower task. Protection of frozen Tower inputs took precedence over those checkout exceptions. Every other protected hash remained strict.

The audit recorded **17 UI changes relative to the setup baseline: 10 changed files and seven additions**, without modifying them. The [UI observations](../TestResults/balance/tower-complete-family-setup-preservation-20260915/ui-observations.json) and [README snapshot](../TestResults/balance/tower-complete-family-setup-preservation-20260915/ui-readme-at-freeze.md) preserve their observed state. The previously flagged README's hash still matches the hash recorded at the old stop, `81f22bf978c8f721754207b21dab9c69e52e4c33b952e61816d41f55d51caff8`; its older baseline hash remains in both records. This resolves the classification gap. It neither certifies the UI work nor requires concurrent UI bytes to stop changing.

## Measurements and commands

| Phase | Seconds |
| --- | ---: |
| Freeze | 0.078 |
| Eight sealed-package audits | 7.078 |
| Protected external/producing/engineering bindings | 1.078 |
| Checkout reconciliation and UI observations | 2.016 |
| Saved result/TRX checks | Below the timer's displayed resolution |
| History traversal, hashing and union reconstruction | 44.391 |
| **Complete audit** | **54.563** |

The history phase includes traversal, hashing, parsing and union checks; its duration is not a measurement of traversal alone. These are preservation costs, not a before/after throughput comparison or an additional discovery speedup. No combat scaling fixture, source preparation or old diagnostic was rerun.

Executed once per phase from the repository root:

```powershell
$package = 'TestResults/balance/tower-complete-family-setup-preservation-20260915'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$package/preservation.py" freeze
& $python -B "$package/preservation.py" audit
& $python -B "$package/preservation.py" seal
```

`scope.json` freezes the script, protocol, reference seals, source/history expectations, command and limits before the audit. Durable start markers and exclusive output creation prevent repetition in this package. The script checks deadlines during traversal/hashing and retains partial evidence on failure. These commands document the completed execution; do not rerun the sealed directory. A later reproduction requires a new output root and frozen scope.

The measured freeze/audit plus **60 seconds static review** and the full **120-second seal allowance** charge **234.641 seconds / 3.91 minutes**, below 30 minutes. This scope limits new retained output to **128 MiB**, below the original 4-GiB cap. Finalization records exact package bytes, whole changed checkout documents and a **2 MiB metadata allowance** in `final-verification.json`, with all changed Markdown snapshots and a final inventory. The prior setup's conservative **428.003-second** time basis remains charged; adding this closure gives **662.644 seconds** before any later setup work. Actual subsequent setup costs must still be added to a future execution binding. Old caps were not increased.

## Changed files and remaining work

Changes are this review, its frozen protocol and seven active Markdown handoffs: discovery implementation/plan, acceptance policy, replication plan, strategy, retained-family design and harness README. The new ignored TestResults package contains the audit script, immutable scope, UI snapshot, measurements, checked-file records and sealed document copies. Markdown links/whitespace are checked during finalization. No implementation/test source, gameplay/content, seed ledger, dependency declaration, migration, shared configuration or deployment changed. Backend tests and builds were intentionally not rerun because this scope only verifies preserved evidence.

The preservation gate is now closed, and the completed parity and seed-free setup checks remain valid in their captured scopes. Next engineering work is the durable external reservation wrapper and final runnable executable/protocol/setup-charge binding, initially without deriving new values. The future **288 fresh values remain unallocated**; reservation and full-study execution require their own concrete scope. The allocator rule alone does not durably reserve values, and the retained setup executable exposes inspection only.

Full-family second-stage selection capacity, combat across the 256-cell batch boundary and full-study throughput remain unmeasured. Preserve all 43,879 recipes, 560 anchors, historical failures and existing caps. No new balance result, full confirmation, gameplay application or adoption follows from this closure.
