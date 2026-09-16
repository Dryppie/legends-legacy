# Independent seed-registry verification closure

15 September 2026. **Independent verification completed.** Zero fights, control preparations, fresh seeds or retries. The completed native measurements and 131 passing backend tests were reused; neither sealed failed package was rerun or modified. Adoption remains Hold.

## Completed work and evidence

The [frozen protocol](Tower-History-Registry-Independent-Closure-Protocol.md) defines one independent reader with a larger allowance based on the measured full-tree cost. Six in-memory fixtures cover its union/type rules, pending reservations, empty inputs, exact ledger names, reparse rejection and exact subtree exclusion. **6/6 reader fixtures passed.** No C# source changed; the existing **131 backend cases**, run through `build/run-tests.ps1`, retain their captured binaries, source hashes and TRX.

The independent traversal finished in **85.484 seconds**, inspecting **702,171 directories**, **4,639,586 files** and **5,341,756 entries**. It verified exact membership for all **164 ledger files**, every content hash, **121 distinct content hashes**, and the complete **482,596-value reservation union**. Hash/union verification took **8.281 seconds**. All **162 prior successful-preflight entries** retained their hashes; the **2 newer ledger paths** are listed explicitly in `independent-audit.json`.

The native scan's excluded subtree was preserved exactly. The new closure package was included and contains no registry ledger filenames. Compared with the older native scan, directory count changed by **+2** and ordinary-file count by **+22** as new non-ledger evidence was added; ledger membership and content remained exact. The audit did not use the old path list to prune enumeration or accept missing/new ledgers.

Saved inventory reconstruction also confirms **9,216 candidate archives, 101,429 files and 55,313 directories** in the matched v19 subtree. The prior native measurements remain valid; this closure adds independent verification and does not add another performance repetition.

The durable `progress.jsonl` records elapsed/CPU time and traversal counters at each phase and every 65,536 entries. The iterative union reader avoids constructing a temporary set for each integer and preserves the previous reader's JSON semantics. Deadline or validation errors retain partial progress rather than discarding it.

All **15,937 indexed files across 39 predecessor packages** verified unchanged before/after. Frozen tested source still matches the checkout. Unrelated dirty work was left in place.

## Performance decision and limits

The preceding native result remains **57.467 seconds for complete enumeration**, plus **0.406 seconds for ledger hashing** and **3.200 seconds for the union**. The archive pair used **71.12% fewer managed allocated bytes** but was **13.29% slower**. Those are one reference-first pair's observations, not evidence of lower peak memory or a whole-preflight speedup.

Retain the tested lower-allocation implementation as an uncommitted candidate for now, with the latency limitation explicit. No further buffer tuning or stronger performance claim is justified by this single pair. Independent correctness verification is a distinct gate; it does not turn the slower timing into a win or authorize a shorter timeout. A later preparation must budget the complete scan and its surrounding work using measured cost. Comparison readiness and combat strength remain unmeasured by this closure.

New diagnostic time before publication: **97.781 seconds**; cumulative before publication: **1508.036 seconds**. New output before publication: **0.16 MiB**. Carried output: **1,817,480,644 bytes**. The [completion receipt](../TestResults/balance/tower-history-registry-independent-closure-20260915/control/completion.json) includes publication and a conservative one-second receipt/seal allowance. Limits remain **1,800 cumulative diagnostic seconds and 4 GiB**. No old package or cap was increased.

## Reproducible commands and next boundary

These are the once-only commands for the frozen package. Inspect the sealed receipts instead of rerunning the same paths.

```powershell
$closure = 'TestResults/balance/tower-history-registry-independent-closure-20260915'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$closure/workflow.py" freeze
& $python -B "$closure/workflow.py" fixtures
& $python -B "$closure/workflow.py" audit
& $python -B "$closure/publish.py"
```

All declared commands completed. Next prepare a separately frozen replacement comparison, reusing unchanged passing test evidence and reserving realistic time for each complete registry check. The failed comparison preparation stays closed. Full preparation must pass before requesting any specific 45-fresh-value exception; this closure authorizes no combat.

Changed files: this protocol/review, six active Markdown handoffs and the independent reader/fixtures/workflow/progress/evidence package. No production source, gameplay, search policy, ability order, configuration or migrations changed in this closure; no deployment implications. Preserve all 482,596 reservations, v19's unused 512 confirmation values and 253 recipes. Historical reliability Fail 1/3, deep recovery 0/3, v19 Unresolved and adoption Hold remain unchanged.
