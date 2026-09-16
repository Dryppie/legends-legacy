# Focused challenger study: Pass

Completed and independently verified **15 September 2026** under the [authorized frozen protocol](Tower-Focused-Challenger-Execution-Protocol.md) and [989-team selection](Tower-Focused-Challenger-Design-Review.md). Target: offline BalanceHarness with captured-v19 gameplay and the existing isolated guardian Health/Power +10% setting.

**253,184 completed fights / 253,184 durably charged attempts**, out of the fixed 253,184 maximum. **Zero fresh seeds, retries or resumes.** No live Kharad tuning, gameplay-content change, migration or deployment occurred. The exhaustive 43,879-team study remains unexecuted and its original request remains byte-for-byte unchanged.

## Balance result

The result is **SupportedWithinFocusedSet** for the exact 989 selected teams. The native outcome is `Pass`; it is **not complete retained-family Pass**. The 42,890 retained teams outside this selection remain unconfirmed, and no conclusion certifies all legal teams or current-checkout gameplay. Search reliability remains **Fail 1/3**, adoption **Hold**; sealed original v19 remains **Unresolved with 253 retained recipes and no internal confirmation**.

The strongest observed team was **team-040e60d3dbc5c127321653c47ed3a9d3**, with **61/256 wins (23.83%)**, adjusted interval **14.55%–36.50%**. **2** teams have a lower bound reaching 10%; **0** have an upper bound above 50%; **0** observed rates exceed 50%. Totals: **2,549 wins, 250,629 defeats, 6 draws**.

This is the same recipe that led the earlier baseline and +10% midpoint studies. No selected challenger overtook it. The new result uses a separate reserved schedule; its win count must not be pooled with the earlier midpoint result or interpreted as a further boss change.

| Group | Teams | Maximum wins /256 | Viability lower bounds >=10% |
| --- | ---: | ---: | ---: |
| Targeted challengers | 861 | 61 | 2 |
| Lightly tested subset of targeted challengers | 128 | 32 | 0 |
| Preselected remainder sample | 128 | 0 | 0 |

These groups overlap only where labeled as a subset. The remainder sample was frozen before combat; its findings do not rule out a rare omitted winner. Historical outcomes selected teams but were never pooled into these 256-trial estimates. Every selected team received the full reserved schedule, shared across teams. The two-sided Bonferroni-Wilson calculation uses alpha .025 across all 989 teams; coverage is approximate and does not establish a lifetime guarantee across repeated studies.

## Execution and measured costs

The run completed in **88.39 minutes**, including the native executor's preflight, batch verification, final reconstruction and publication. The design's linear estimate was 90.85 minutes; the measured difference is **-2.46 minutes**. This comparison concerns throughput for this fixed study, not a newly measured before/after software speedup. It used 89.60% fewer fights than the exhaustive maximum; the exhaustive runtime remains unmeasured.

The package occupied **3,245,328,649 bytes (3.022 GiB)** before report publication, below 8 GiB. The durable budget clock had charged **122.90 minutes** before this report, including the conservative 15-minute initial charge and all later wall time. Native execution received a reduced archive allowance after actual new setup files and a 64-MiB final-record reserve. Final publication counts are in `control/completion.json`.

Existing `TowerCompleteFamilyRun` accounting, four compact storage batches (256,256,256,221 teams), durable per-attempt charging, cancellation guards and exact archive verification were reused unchanged. The separate experiment adapter forces every selected team to the full sample and retains original cell/scenario/participant hashes. Its native performance trace is saved in [performance.json](../TestResults/balance/tower-focused-challenger-run-20260915/study/performance.json), including phase timings, CPU, memory, allocation and storage operations. Progress checks read the ledger size; they never scan the growing campaign.

| Instrumented phase | Seconds |
| --- | ---: |
| Preflight | 42.39 |
| Execution batches, including their internal checks | 4803.48 |
| Batch reconstruction | 225.20 |
| Final reconstruction | 217.70 |

Within execution, the compact writer's **7,912 storage checks took 44.23 seconds**, or **0.84%** of instrumented execution time. This is the measured cost of that specific accounting scope, not all filesystem or verification work. Engine simulation took **3010.27 seconds**. These nested costs must not be added to the table as separate phases. Native CPU time was **4171.66 seconds** and peak working set **1.288 GiB**. There is no identical-input unoptimized reference run in this study, so it establishes no new causal speedup ratio.

## Verification

- The repository wrapper passed **52/52 existing zero-engine accounting/cancellation/contract cases**, with exact names and TRX retained.
- One captured adapter build passed; all four gameplay DLL hashes match the sealed v19 versions.
- Native binding prepared **all 989 teams once**, with exact saved participant hashes. Four synthetic assessment cases and all 257 interval rows passed. Independent binding verified the exact roster, history, transfer, bounds and original request before combat.
- Native execution verified every completed batch and reconstructed the complete study before sealing. Standalone native verification ran under an engine-entry guard.
- Independent verification recounted **all 253,184 saved compact records / 7,912 chunks**, checked seed order and participant hash for every record, reproduced every outcome count and all 989 confidence intervals, verified the durable attempt sequence, and rehashed the exact final archive inventory. Maximum interval difference: **2.88e-11**.
- All ten original reservation files and the exhaustive launch request remain unchanged. The registered focused ledger preserves the full 481,891-value union. All **256 transferred values were used** by this study; **32 first-stage values and the original 512 remain unused**. The old launch request's live-history check now rejects the new registered ledger; it cannot treat the consumed values as fresh.

Standalone native verification took **229.51 seconds** and independent verification **126.81 seconds**. Binding and verification together used **425.48 seconds** of the 1,800-second diagnostic limit, with **zero diagnostic fights**. The repository test wrapper took **39.09 seconds** and the captured adapter build **2.41 seconds**, reported separately from that diagnostic allowance.

One read-only progress command could not start because its Python executable path was mistyped. The corrected status read succeeded; the running study was unaffected and no test, diagnostic or fight was repeated. The observation error is retained in `control/monitor-path-error.json`.

## Changed files and reproducibility

Changes are limited to the new execution protocol/review, seven active Markdown handoffs and the new study package. `producing/FocusedProgram.cs` is a frozen experiment-specific adapter; production source and gameplay DLLs were not edited. Existing dirty backend/UI work was preserved. Checkout changes observed during execution are retained separately and were not reverted. No migrations, production configuration changes or deployment implications.

Recorded commands from the repository root (create-only; completed/stopped phases must not be rerun):

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-focused-challenger-run-20260915'
& $py -B "$work/workflow.py" freeze
& $py -B "$work/workflow.py" tests
& $py -B "$work/workflow.py" build
& $py -B "$work/workflow.py" request
& $py -B "$work/workflow.py" bind
& $py -B "$work/audit.py" binding
& $py -B "$work/workflow.py" run
& $py -B "$work/workflow.py" verify
& $py -B "$work/audit.py" audit
& $py -B "$work/finish.py"
```

The test phase invokes `build/run-tests.ps1 -Filter FullyQualifiedName~BalanceHarnessTowerCompleteFamilyTests` with its recorded artifact directory. Exact commands, limits, exits and durations are saved in `control/*-started.json` and `control/*-result.json`. No command changes an old experiment's cap or launches the exhaustive request. The package manifest seals scripts, source, executable, tests, control records and the study archive.
