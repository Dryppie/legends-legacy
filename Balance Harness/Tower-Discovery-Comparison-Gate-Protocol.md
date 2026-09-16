# Discovery comparison gate: frozen zero-combat verification

16 September 2026. Offline `LL/tools/BalanceHarness` only. Incoming receipt: [discovery refinement](../TestResults/balance/tower-discovery-refinement-20260916/completion.json), **2,843.503 diagnostic seconds used / 156.497 remaining**, cumulative **3,000 seconds / 4 GiB**. This scope permits at most **80 diagnostic seconds / 128 MiB** new output, including compilation, tests, fixture processing, audit and publication. Source editing is excluded as in the preceding scope. Zero fights, runtime preparations, fresh values, retries, resumes or replays. Preserve all **482,821 reservations**, fixed ability order, sealed history and adoption Hold.

## Decision and implementation

Keep the existing comparison requirement of 16 completed evaluations per arm. If either baseline or refinement discovery is incomplete, cancelled, invalid or inconsistent, **stop the entire comparison before returning any nominations**. No screening, confirmation, partial-arm quality comparison, candidate refill, legality relaxation or proposal-cap increase. Legitimate `missing-team-roles` rejections remain charged. This decision resolves the previously open incomplete-arm handling rule without changing search or statistical design.

Add a narrowly scoped `TowerDiscoveryComparisonGate` for an already verified pair of team-coverage baseline and discovery-refinement reports. Both inputs must match except policy/method and retain one generation label, 16 candidates and 16 proposals. Persist a create-new, write-through gate receipt before returning nominees or throwing a stop. Include input/report hashes, source statuses and stop reasons, proposal/evaluation/battle counts, proposal-result counts and gate issues. A receipt write failure, existing receipt or cancellation cannot return nominees. Complete pairs use the unchanged ranking to select two teams per arm.

The gate does not replace archive verification, the durable combat-attempt journal or a full comparison driver. Sealed earlier comparison adapters stay unchanged. No combat execution route, allocator, default or gameplay change is part of this scope.

## Exact diagnostics

Use the new isolated directory `TestResults/balance/tower-discovery-comparison-gate-20260916`. Freeze current source, the new gate/tests, this protocol, scripts, captured dependencies, five saved result fixtures, input, previous seals, ledger and test entry point before execution. Preserve the pre-edit dirty-checkout snapshot. Reuse the captured compiler dependencies; no ordinary gameplay build or restore.

1. Compile the captured harness plus the gate diagnostic, then the isolated test assembly. Run **105 tests** once through `build/run-tests.ps1 -NoBuild -ArtifactsPath`: the prior 95 plus ten gate tests covering complete ranking/publication, either arm incomplete, cancellation/invalid/missing generation, relabeled partial output, duplicate/missing/unmatched evaluations, generation/proposal identity, measurement/fitness/order corruption, battle/cache counts, input/cap mismatch, and receipt failure/overwrite/cancellation.
2. Read the saved baseline `team.json` and each of `refinement.json`, `repeat.json`, `reordered.json`, `reverse.json` from the preceding sealed scope. **Do not regenerate or reevaluate any team.** For each pair, construct explicitly synthetic report envelopes from the saved counts and schedules, call the real nomination boundary once, and persist its gate receipt and returned nominees or stop. Expected results: three Ready/four-nominee pairs and one StoppedDiscovery/zero-nominee pair, with 16/14 refinement counts and two `missing-team-roles` charges for reverse. No later-stage calls exist in this diagnostic.
3. Independently audit all four receipts in Python against saved source counts, identities, hashes, schedules and ranking; complete nominations must equal the unchanged ranking, and reverse must preserve the two rejection charges and return no nominees. Validate the unchanged 482,821-value ledger union and all **69 predecessor packages**. Persist elapsed/CPU/allocation/working-set and zero-combat trace measurements. These are gate-processing costs, not a search or campaign speedup.
4. Update active Markdown and publish a measured completion receipt and manifest, preserving old protocols, results and captured documents. Retain failure receipts and do not run dependent phases after a failure or limit. No retries; absent success receipts identify work not executed.

The workflow reserves at most 65 seconds for freeze/build/tests/fixture/audit and the remaining scope allowance for publication, including one closure second. Add a 1 MiB allowance for shared test artifacts to cumulative output accounting. Check cumulative and per-scope limits at phase boundaries and enforce subprocess deadlines. New output must also fit the unchanged cumulative 4 GiB cap.

## Run-once commands

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-discovery-comparison-gate-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Never rerun a sealed/failed directory. No new combat or fresh-seed approval is implied. V19 remains Unresolved with all 253 recipes and its original unused 512 confirmation values preserved; reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. No Kharad tuning, large confirmation or deployment.
