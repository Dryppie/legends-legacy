# Refinement shared archive integration: frozen zero-combat check

16 September 2026. Offline BalanceHarness only. Incoming [receipt](../TestResults/balance/tower-refinement-storage-profile-20260916/completion.json): 3,161.618469456 / 3,240 diagnostic seconds; **78.381530544 seconds remaining**. This scope is **40 seconds / 24 MiB**, including all retained fixture files, under the unchanged cumulative **4 GiB** output ceiling. Normal phases share 36 seconds; publication reserves four. Editing is excluded; builds, execution, audits and publication are charged. Zero fresh balance seeds, combat, preparations, replays, retries, global registry traversals or changes to sealed packages.

The preceding profile verification passed all 32 backend facts through `build/run-tests.ps1` and a synthetic comparison. Reuse those exact tests and binaries. This scope changes no harness or gameplay implementation and does not rerun that suite. Compile only a small diagnostic host, named `EssenceSystem.Tests` for the existing friend-assembly access, against the pinned producing DLLs with copy-local disabled. It is a native integration diagnostic, not another backend test run. The host resolves dependencies from the preceding sealed output; it does not rebuild or modify it.

Package: `TestResults/balance/tower-refinement-storage-integration-20260916`.

1. Freeze, at most four seconds: verify the immediate sealed package and root harness-source equality, prior 32 passing test receipt, content pins, scripts, host, cached restore inputs and protocol. Preserve a dirty-checkout hash snapshot. No restore or full historical audit.
2. Build once, at most eight seconds: compile the isolated host with existing restore assets, no build servers, no restore and no copied gameplay DLLs.
3. Native diagnostic once, at most twenty seconds: retain the real 25-file executable bundle through `TowerBossStudy.RetainExecutable`; construct two discovery and two balance storage fixtures with the captured content and fixed order, using the already-used synthetic labels 17, 101–104, 201–208 and 301–332. Fixture historical arrays are empty; no history is deleted or rebound. Open all four actual `TowerBulkCampaign` stages with schema 2, nested owned accounting, compact JSON and bound shared references. Finalize and reopen each **zero-batch storage fixture**; these are not completed discovery/balance results. No `RunAsync`, battle preparation, batch creation, allocator or engine calls. Verify zero attempts and exact parent-accounting/scanned-byte equality. Alter one newly copied runtime-config file, require both reference verification and the owner's full audit to reject it, restore its exact bytes and verify again. Retain all evidence; save detailed performance trace, bytes, hashes, CPU/allocation/working-set measurements. Read the existing 482,821-value union once for its canonical hash and compact byte count.
4. Independent audit once, at most four seconds: verify all four exact final/frozen inventories, producing-file hashes, shared identity and fixed references, content equality and zero accounting; count storage independently. Recalculate the full-history component floor and remaining resources. Distinguish actual measured fixture bytes from projected full-study bytes. No claim that these fixtures exercise the adaptive/combat path or establish its full output upper bound.
5. Publish within four reserved seconds: preserve the first failure if any, skip dependent phases and never retry. Update six active Markdown handoffs, record exact cumulative resources and verify unrelated files, links and whitespace. A fresh-value/real-comparison request is not authorized by this diagnostic. If a complete comparison allowance cannot be established, report that limitation rather than inventing one.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-storage-integration-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" native
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

On first failure, run only `publish.py failure`. Preserve 482,821 reservations, v19's 512 unused confirmation values and 253 recipes. V19 remains Unresolved, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold. Fixed ability order and gameplay remain unchanged. No configuration changes, migrations or deployment.
