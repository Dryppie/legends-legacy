# V3 comparison after verified history recovery

16 September 2026. Target: offline BalanceHarness. This is a new comparison, with a new request, master and output directory. The failed V3 study stays sealed and Pending. Preparation is authorized; fresh seed allocation and combat require the specific approval below.

## Frozen scientific design

Compare `independent-team-coverage-v1` against `independent-discovery-refinement-novel-v3`, using `tower-discovery-refinement-comparison-v2`. Both arms use the same captured gameplay assemblies, content, settings, templates, equipment, timestamp and seed schedules. Keep equipped ability order fixed and ordinal. No Kharad tuning or gameplay changes. Search may use completed discovery evidence only; selection and confirmation never feed back into discovery.

| Stage | Maximum teams | Fights per team | Maximum attempts |
| --- | ---: | ---: | ---: |
| Baseline discovery | 16 | 4 | 64 |
| V3 discovery | 16 | 4 | 64 |
| Selection: two nominees per arm | 4 | 8 | 32 |
| One finalist per arm and two controls | 4 | 32 | 128 |

Maximum **288 charged attempts**, zero retries, resumes or combat replays. An arm that fails to complete its 16-candidate discovery stops the comparison before nomination; no refill. Duplicate later-stage recipes retain all origins and can reduce the fight count. A finalist can play 44 fights across the three stages. Selection uses wins, then frozen discovery rank and ID. Controls remain `team-040e60d3dbc5c127321653c47ed3a9d3` and `team-49f6979895354870c89362d4abf214bb`.

Primary endpoint: V3-minus-baseline confirmation win rate with the existing adjusted paired interval. Boss health and duration are descriptive. This pilot cannot establish reliability or global optimality; adoption remains Hold.

Request exactly **45 fresh values**: one generation label, four discovery seeds, eight selection seeds and 32 confirmation seeds. Use the unchanged durable allocator and V1 derivation namespace with master **2026091604**. No candidate derivation during readiness. Exclude all **482,911 existing reservations**, including the failed V3 study's 45 unused values, the earlier failure's 40, and V19's separate 512 unused values. Preserve V19's 253 recipes.

## Bound recovery and execution identity

Use the exact harness DLL verified by the [history recovery work](Tower-History-Recovery-Review.md): SHA-256 `5363f2f77b2016593974a0d3d52f86717d717cb8377d17bbe358985960d6513a`. Its sealed package hash is `cc3b89a6d93665122dc7327124d260a2f9602b16681fc82803243281abb19388`. Reuse its 45 passing backend tests and measured 38.2906-second history validation; do not repeat them. The DLL also contains its diagnostic entry point; the unchanged sealed comparison host invokes the production APIs directly. No claim is made that launching the DLL itself would run this comparison.

Pin the failed V3 study's Complete `seed-ledger.json` as authoritative history, its unchanged Pending `history-input.json`, the external `tower-history-recovery-20260916/recovery.json`, the source inventory, and the saved 178-file registry snapshot. Set the explicit `pendingHistoryRecoveries` mapping in the request. The receipt only permits reading all 45 values as permanent exclusions; it cannot complete, resume or launch the old study. Production binding and launch retain their complete live scans, hash checks, cancellation, durable charging and archive verification.

New paths under `TestResults/balance`:

- Readiness: `tower-refinement-v3-recovered-comparison-readiness-20260916`.
- Study: `tower-refinement-v3-recovered-comparison-study-20260916`.
- Execution evidence: `tower-refinement-v3-recovered-comparison-execution-20260916`.

## Exact readiness and resource envelope

Current cumulative ceilings: **3,840 diagnostic seconds / 4 GiB + 128 MiB**. Start from the history recovery receipt's 3,624.952272355634 seconds and reconcile actual sealed output size plus its retained fixture charge. Freeze code, scripts, commands, source inventory and checks before execution. Readiness allows **40 seconds / 24 MiB** within existing authority, including setup, copies, inspection, audit and publication. Source/script editing is excluded. No fresh seed derivation, full registry scan, generated team, actor preparation or combat. Stop dependent phases at the first failure; zero retries.

1. Setup and dirty-checkout snapshot: five seconds maximum.
2. Freeze and copy inputs: 12 seconds maximum. Verify seals, all live harness source hashes, reused 45-test TRX, the source study, recovery and registry snapshot. Copy captured runtime assets, replacing only the harness with the tested DLL. Copy the unchanged sealed comparison host and its source/dependency metadata; no rebuild or new test run.
3. Native `inspect`: ten seconds maximum. Validate the complete request, recovery pins, gameplay identity, 482,911-value ledger, controls and version-specific driver receipt. Create only an unsigned authorization template.
4. Independent audit: ten seconds maximum. Check request/template, exact provenance, recovery/source membership and hashes, preserved reservations and the full prospective resource envelope. Reuse the unchanged reader against 128 saved confirmation records and four rates. No combat replay.
5. Publish and seal: five seconds maximum, including a one-second closure allowance. All phases share the 40-second ceiling; phase ceilings are not additive entitlements.

Proposed execution requires **45 fresh values, 240 additional cumulative diagnostic seconds and 64 MiB additional cumulative output**. If approved, cumulative ceilings become **4,080 seconds / 4 GiB + 192 MiB**. These are prospective extensions for the new comparison; old experiment caps are unchanged.

- Study: **360 seconds / 84 MiB**. Binding retains its 150-second allowance, conservatively charged in full. Launch, its history refresh, combat and native reconstruction share the remaining 210 seconds. An outer token covers the complete study.
- Complete execution task: **400 seconds / 4 MiB evidence**. Native process containment/cleanup allows 364 seconds; independent audit at most 30 seconds; publication uses the remaining bounded time. The wrapper owns the process tree and records cancellation/failure.
- Before requesting approval, prove that actual readiness output plus the full **88 MiB execution reservation** and **400 execution seconds** fit the proposed cumulative ceilings. Recalculate the component minimum from fourteen compact full history arrays, one runtime and four content copies. Reports and temporary writes remain outside this minimum; completion is not guaranteed.

After explicit approval, write approval and authorization receipts into the new execution directory, bound to the readiness seal and request hash. Run the frozen `execution.py` once. It invokes ReserveAndBind/Run and independently verifies source assets, transcript, schedules, charges, fixed order, policy identities, stage counts, nominations, finalist selection, family origins and adjusted confirmation statistics. No fourth standalone global scan is scheduled. Retain every derived value on failure.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-v3-recovered-comparison-readiness-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" inspect
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
# Only after the exact seed/time/output approval and bound receipts exist:
& $python -B "$work/execution.py"
```

On readiness failure, only `publish.py failure` is permitted. No sealed experiment changes, V19 confirmation, ability-order tuning, deployment, migrations or configuration changes. V19 reliability Unresolved; later reliability Fail 1/3, deep recovery 0/3; adoption Hold.
