# Group-completion comparison readiness

15 September 2026. **Prepared and independently verified; no seed allocation or combat.** The new policy is ready for its bounded comparison with the preceding group-diversity policy. Comparative combat strength remains unmeasured. Adoption stays Hold.

## Frozen comparison

Both policies receive the same captured gameplay inputs and 44 distinct evaluated teams with four shared discovery seeds. Screen the top two per policy on eight shared seeds, then confirm one finalist per policy and two fixed controls on 32 shared seeds. Exact-context deduplication can reduce the total; no seats are refilled. Maximum **512 charged fights**, zero retries/resumes/replays. Ability order stays ordinal; controls remain outside search and screening.

The [protocol](Tower-Group-Completion-Comparison-Protocol.md) fixes all selection rules, descriptive metrics, uncertainty estimates and resource limits. Execution needs a new specific exception for **45 fresh values**; the previous approvals are exhausted. All **482,551** reservations remain intact, including v19's unused 512. An approved binding would retain 482,596.

## Verification and measurements

- **103/103 backend tests passed** through `build/run-tests.ps1`: 95 existing composition/joined/group-count/variation/diversity/completion cases and eight adapted comparison-controller cases. Engine guards and synthetic evaluators only.
- **4/4 Python metric fixtures passed**, distinguishing missing telemetry from zero and validating group counts and paired alignment.
- One preflight validated identical definitions except policy/method, prepared **two controls without combat**, refreshed the reservation union and saved **66 interval fixtures**. One independent audit verified these outputs and checked the diversity audit against all **1,956 archived schedule positions** plus completion traces for all **19 saved construction requests**.
- All **12,303 indexed files across 30 sealed packages** verified unchanged before/after.
- Diagnostic commands before publication: **105.858 seconds**; cumulative including carried-forward diagnostics: **969.789 seconds before publication**. The final [completion receipt](../TestResults/balance/tower-group-completion-comparison-preparation-20260915/control/completion.json) includes publication and a conservative one-second receipt/seal allowance. The preceding accounting correction, including the failed fixture, remains charged.
- Preparation output before publication: **88.71 MiB**. Source copies exactly match the owned checkout source and verified corrected fixtures. Gameplay DLLs/content were captured from the existing source; no dirty gameplay rebuild.

| Command phase | Measured seconds | Category |
| --- | ---: | --- |
| audit | 19.515 | Diagnostic |
| bootstrap | 2.406 | Diagnostic |
| build | 1.984 | Compilation |
| check | 77.781 | Diagnostic |
| metrics-tests | 0.203 | Diagnostic |
| test-build | 1.438 | Compilation |
| tests | 5.953 | Diagnostic |

## Reproducible commands and remaining boundary

Run from the repository root. Commands are once-only: retain the sealed package and inspect its command receipts; reproduce diagnostics only in a separately frozen new package, never rerun these paths.

```powershell
$comparison = 'TestResults/balance/tower-group-completion-comparison-preparation-20260915'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$comparison/workflow.py" bootstrap
& $python -B "$comparison/workflow.py" build
& $python -B "$comparison/workflow.py" test-build
./build/run-tests.ps1 -NoBuild -ArtifactsPath "$comparison/tests" -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests|FullyQualifiedName~BalanceHarnessGroupCountTests|FullyQualifiedName~BalanceHarnessGroupVariationTests|FullyQualifiedName~BalanceHarnessGroupDiversityTests|FullyQualifiedName~BalanceHarnessGroupCompletionTests|FullyQualifiedName~BalanceHarnessGroupCompletionComparisonTests'
& $python -B "$comparison/workflow.py" metrics-tests
& $python -B "$comparison/workflow.py" check
& $python -B "$comparison/workflow.py" audit
```

The actual test command was dispatched through `workflow.py tests`, which freezes binaries, preserves TRX and charges time. After specific approval, the prepared controller dispatches `bind`, `run`, `verify`, then `audit-execution`; it requires matching protocol-hash authorization, unchanged registry/input hashes and successful preceding phases. No execution command has run or failed in this preparation. Builds used cached metadata with `--no-restore`; no required preparation command was blocked.

This remains one shallow search restart. Four discovery fights per team give noisy rankings; the 32-seed paired confirmation will have broad uncertainty. Verified category completion does not prove stronger combat performance. The earlier measured filesystem optimization is unchanged; this is not a new speedup benchmark.

Changed files: the new frozen comparison protocol and readiness review, six active handoffs, and the separate preparation evidence package containing adapted controller/audit/metrics scripts, copied source, isolated executable/tests and receipts. No game source/content, defaults, configuration or migrations were changed; no deployment implications. Unrelated checkout work is preserved. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.
