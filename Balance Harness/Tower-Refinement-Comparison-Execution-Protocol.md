# Refinement comparison: bounded execution request

16 September 2026. Offline BalanceHarness. **Prepared request only: allocation and combat require explicit approval.** Earlier fresh-value approvals are exhausted. The current cumulative diagnostic ceiling is 3,240 seconds; this request asks to add **360 seconds**, making it **3,600 seconds**. It does not increase the cumulative **4 GiB** output ceiling or any historical experiment cap.

## Frozen scientific comparison

Compare the existing team-coverage baseline with discovery refinement, on the captured content/gameplay assemblies already checked in the [storage integration](Tower-Refinement-Storage-Integration-Review.md). Keep ordinal ability order fixed. No gameplay/Kharad tuning, confirmation-based reselection, reliability or adoption claim.

Allocate exactly **45 fresh values** through the durable production allocator: one generation label, four discovery seeds, eight selection seeds and 32 confirmation seeds. Preserve all 482,821 existing reservations, including v19's 512 unused values and 253 recipes. The two policies share identical content, settings, context and schedules; only their frozen search policy/method differs.

| Stage | Maximum teams | Fights per team | Maximum fights |
| --- | ---: | ---: | ---: |
| Baseline discovery | 16 | 4 | 64 |
| Refinement discovery | 16 | 4 | 64 |
| Selection: two nominees per policy, deduplicated | 4 | 8 | 32 |
| Confirmation: one selected finalist per policy plus two saved controls, deduplicated | 4 | 32 | 128 |

**Maximum 288 charged fight attempts**, including any interrupted attempt; **zero retries, resumes or replays**. Failed/incomplete discovery stops before selection. Select each policy's finalist by selection wins, then frozen discovery rank and ID. Report paired differences and the controller's adjusted intervals; boss-health summaries are descriptive only. Preserve the full family, including duplicates' origins. Adoption remains Hold regardless of pilot outcome.

## Concrete paths and resource limits

Readiness package: `TestResults/balance/tower-refinement-comparison-readiness-20260916`.

New study: `TestResults/balance/tower-refinement-comparison-study-20260916`. New execution evidence: `TestResults/balance/tower-refinement-comparison-execution-20260916`.

`request.json` binds the exact preflight pins, canonical execution hash, this protocol hash, unused master 2026091602 and `shared-executable-compact-json-v1`. Use the retained producing harness and gameplay DLLs from the verified storage-profile package; compile only a thin host with copy-local disabled. One self-contained shared executable bundle belongs to the new study.

- Study deadline: **360 seconds**, including binding and execution/verification. Binding gets **150 seconds**, conservatively charged in full; launch gets the remaining **210 seconds**, including its own live registry refresh. A global host token also limits elapsed study time to 360 seconds.
- Execution task: **400 seconds total**, including owned-process cleanup, independent audit and publication; native process gets at most 364 seconds, independent audit 30 seconds, publication at most five seconds. Start only if the newly approved cumulative ceiling can cover the entire 400-second task.
- Study output: **84 MiB**. Readiness plus execution evidence: **4 MiB combined**. Together at most **88 MiB new output**, inside the unchanged cumulative 4 GiB cap. Start only when the complete reserved envelope fits. Keep existing per-write/boundary caps, mandatory audits and attempt journals.
- The measured full-history component minimum is **73.01 MiB**, leaving about 10.99 MiB inside the study cap for remaining metadata, search output, records and margins. This is a bounded attempt, **not a guarantee of completion or a proven full-output upper bound**. A resource failure stops the run and retains the evidence; no automatic allowance increase.

The prior complete registry Check took 56.8624 seconds. Binding performs a full refresh and membership/hash recheck; launch performs another refresh. Their costs differ. The limits above allow for those operations but are not a measured prediction. Do not repeat a standalone global Check before binding, prune history or bypass any refresh to save time.

## Once-only preparation, within existing authority

Current incoming usage: **3,166.320469456 / 3,240 diagnostic seconds**, leaving **73.679530544 seconds**. Freeze a **20-second / 3 MiB** readiness scope inside the combined 4 MiB evidence allowance; normal phases share 16 seconds and publication reserves four. No allocation, preparations, combat or registry scan. Editing is excluded; build, inspection, audits and publication are charged.

1. Freeze consumed artifacts, root-source equality, request, host/scripts, cached restore assets, immediate predecessor seal and prior 32/32 backend-test receipt: at most three seconds.
2. Compile the thin host without restore/build servers or gameplay rebuild: at most six seconds.
3. Run only its `inspect` operation: at most five seconds. Validate the exact request and pinned preflight, compute request/authorization-template hashes, and confirm the study does not exist. This calls no allocator or live registry scanner.
4. Independent preparation audit: at most two seconds. Recount the real history/hash; verify paths, budgets and matching request/template. Exercise the saved-record/interval reader against the preceding core-portfolio confirmation's 128 existing records and its four reported rates. No new fights or replay. Preserve hashes of those consumed fixture files. Reuse the 32 passed backend tests from `build/run-tests.ps1`; no source change justifies repeating them.
5. Publish readiness or the first failure; stop without retry. Update active Markdown and request approval only if all readiness phases pass. Preparation itself does not raise the time ceiling.

## After explicit approval only

Record the user's approval in the execution evidence directory as `approval.json`, binding the sealed readiness manifest and request hash, exactly 45 fresh values, 288 maximum attempts, zero retries, cumulative 3,600 seconds and cumulative 4 GiB. Copy the prepared authorization template to `authorization.json` only after that approval. The presence of a template alone is never authorization.

Run `execution.py` once. Verify seals/inputs/resource reservation, create the durable execution start, then call the already tested public `ReserveAndBind` and `Run` exactly once. The launcher performs native archive reconstruction before publishing completion. Do not precompute candidate seed values during preparation. Cancellation or failure retains Pending reservations and all derived values; do not reuse any partially bound study.

The independent post-run audit verifies exact top-level and controller inventories, all schedules/history and the production candidate transcript, raw compact-record counts/outcomes against evidence, durable attempt bytes, fixed ability order, selection winners, family origins and adjusted rates/paired differences. It reads only this new study and pinned prior history; no fourth global registry traversal and no engine call. Preserve a first failure and report partial charges/reservations without a success conclusion. Seal evidence and update Markdown within the same task limit.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-readiness-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" inspect
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
# Only after the specific approval and its exact bound files exist:
& $python -B "$work/execution.py"
```

On preparation failure run only `publish.py failure`. No sealed-v19 rerun/modification, 129,536-fight confirmation, ability-order optimization, gameplay/content changes, migration, configuration change or deployment. V19 remains Unresolved; later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain.
