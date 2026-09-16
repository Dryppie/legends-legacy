# Completion versus allocation: replacement readiness

15 September 2026. **Preparation independently verified.** Zero new fights or seed values. The failed comparison and registry packages remain sealed. This replacement uses a verified registry snapshot only for guarded, non-runnable preparation; live scans before seed allocation, execution and native verification remain intact. Adoption stays Hold.

## Concrete comparison and preparation boundary

The [frozen protocol](Tower-Group-Allocation-Comparison-V2-Protocol.md) keeps the same two policies, captured gameplay inputs and fixed ordinal ability order. Completion and allocation each receive **44 distinct teams × four discovery seeds**; screen the top two per policy on eight shared seeds; confirm one finalist per policy and two fixed controls on 32 shared seeds. Maximum **512 charged fights**, with no retries/resumes/replays, no seat refills and no reselecting finalists from confirmation results. Stage-only exact-context deduplication retains every origin.

The proposed exception remains **45 fresh values**: one generation, four discovery, eight screen and 32 confirmation labels. All **482,596** reservations are preserved; approved binding would retain **482,641**. Previous approvals are exhausted. Neither the future study nor execution directory exists yet.

Preparation checks the pinned registry snapshot, every recorded ledger hash and the complete reserved-value union. Its fixture labels were already reserved and cannot run combat. It does not claim that current live membership cannot change. Binding still performs a complete live scan and exact membership/hash comparison **before creating the study or allocating a value**; new, missing or changed ledgers stop it. Run and native verification retain their complete live checks. The adapter's binding/execution method bodies are unchanged, with the exact source proof in `adapter-change-proof.json`.

## Verification and measurements

**7/7 new backend tests passed** through `build/run-tests.ps1`, covering valid/pinned/tampered snapshots, changed/missing ledgers, cancellation and a new live ledger invalidating the binding equality gate. Reused unchanged: **131 backend cases**, **four metric fixtures** and **six independent-reader fixtures**, with source, binary and result hashes verified. The isolated adapter/test builds used cached restore metadata and captured gameplay DLLs; no dirty gameplay rebuild or restore.

One native preflight validated equal definitions except policy/method, prepared **two controls without combat**, and saved **66 interval fixtures**. The independent audit verified the definitions, reservation union, control inputs and intervals; **1,956 archived diversity positions**, **564 allocation positions**, and **19 completion plus 19 allocation traces**. No new candidate generation or gameplay evaluation occurred.

| Phase | Seconds | Category |
| --- | ---: | --- |
| audit | 8.015 | Diagnostic |
| build | 3.812 | Compilation |
| check | 4.782 | Diagnostic |
| freeze | 4.657 | Diagnostic |
| test-build | 1.641 | Compilation |
| tests | 2.828 | Diagnostic |

New diagnostic time before publication: **20.282 seconds**; cumulative before publication: **1532.849 seconds**. Remaining before publication: **267.151 seconds**. The [completion receipt](../TestResults/balance/tower-group-allocation-comparison-v2-preparation-20260915/control/completion.json) includes publication and a conservative one-second receipt/seal charge, and records the final execution-planning gate. No old diagnostic cap increased.

New output before publication: **82.65 MiB**, plus a conservative **1 MiB** temporary-test charge. Carried output: **1,818,924,029 bytes**. The cumulative ceiling is **4 GiB**, with preparation/execution evidence below 512 MiB and the proposed study below 1.5 GiB. All **15,986 indexed files across 40 predecessor packages** verified unchanged before/after. Owned source and fixtures matched the frozen evidence; unrelated dirty work was left in place.

Avoiding a redundant preparation traversal saves work by changing only the non-runnable preparation boundary. It is not a measured speedup of the scanner: the previous archive pair still showed 71.12% fewer managed allocations and 13.29% slower traversal. Future live checks still need realistic allowances.

## Execution gate, commands and remaining limitations

Successful preparation is only one gate. The frozen planning threshold requires **210 seconds remaining** before seeking the seed exception and again at binding; it was derived from the preceding comparable binding/run/verification/audit totals plus closure margin. It is not a guarantee of runtime. All execution and publication still fit inside the actual remaining portion of the cumulative 1,800-second cap, or stop with evidence preserved.

The preparation and conservative planning check both passed before publication. The final receipt determines whether the specific 45-value / maximum-512-fight exception can now be requested.

Commands describe the once-only frozen preparation; inspect receipts instead of rerunning sealed paths.

```powershell
$comparison = 'TestResults/balance/tower-group-allocation-comparison-v2-preparation-20260915'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$comparison/workflow.py" freeze
& $python -B "$comparison/workflow.py" build
& $python -B "$comparison/workflow.py" test-build
& $python -B "$comparison/workflow.py" tests
& $python -B "$comparison/workflow.py" check
& $python -B "$comparison/workflow.py" audit
& $python -B "$comparison/publish.py"
```

The test command and exact filter/artifact path are recorded in `control/tests-command.json`; the seven-case TRX and all reuse hashes are retained. After specific protocol-hash authorization, the prepared once-only commands are `bind`, `run`, `verify`, then `audit-execution`, followed by bounded evidence closure. No execution command has run. No preparation command was blocked by missing seed authorization.

This remains one shallow restart with noisy four-fight discovery rankings and broad uncertainty from 32 paired confirmation seeds. It does not establish reliability, balance acceptance or a default-policy promotion. Changed files: the new protocol/readiness review, six active Markdown handoffs and a separate package containing the preparation-only adapter/helper, seven tests, audit/workflow, copied verified source and captured evidence. No production source, gameplay, ability order, configuration or migrations changed; no deployment implications. V19's 253 recipes and unused 512 confirmation values remain preserved. Historical reliability Fail 1/3, deep recovery 0/3, v19 Unresolved and adoption Hold remain unchanged.
