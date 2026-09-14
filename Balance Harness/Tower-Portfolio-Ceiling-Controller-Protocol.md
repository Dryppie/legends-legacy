# Captured-v19 ceiling screen — executable controller protocol

Frozen **14 September 2026**, following the [controller verification](Tower-Portfolio-Ceiling-Controller-Review.md). This closes implementation of the four-factor execution adapter while preserving the [frozen statistical design](Tower-Portfolio-Ceiling-Recalibration-Plan.md). Status: **ControllerVerifiedSeedBindingPending**. No fresh schedule or combat launch is authorized by this document.

The authoritative [machine protocol](../TestResults/balance/tower-ceiling-controller-20260914/executable-protocol.json) has SHA-256 `bd47026c676c803e3f58355835e74ff7e050e5d50fea9448fc6590034c13706c`. The final captured `BalanceHarness.dll` has SHA-256 `272e861a725bd3bbfec0ccb4f3595f50b3d4b53056bca29d6c0d2d34f6c0bc32`. The protocol binds every executable file, all three controller source files, the entry point/compiler, source ledger/manifest and prepared manifest. All four gameplay DLLs match the completed captured-v19 confirmation.

## Fixed execution contract

- Factors **1.00, 1.04, 1.08, 1.12**, each with the exact same **253 recipes** and ordered **128 shared seeds**. The complete study is **1,012 cells / 129,536 maximum attempted fights**.
- Preserve recipe identity, Essence order, nominations, roles, two five-player parties, prepared combatants, settings and content. Only the previously prepared guardian Health/Power factors differ.
- Sequential `prepared-v1`, chunks of 32; factors ascending, recipe IDs ordinal, then the exact declared seed order. No factor pruning or outcome-dependent extensions.
- One outer durable attempt journal across all variants. Flush each start before simulation and each completion after a returned result. Failed/interrupted starts remain charged. **Zero retries, replays, discovery or automatic resume.**
- One **14,400-second active-work budget** and **8 GiB total new-output budget**. Include preparation/allocation work recorded before binding, binding/copies, runtime, logs, verification, final reconstruction and metadata. All declared external setup files plus request bytes count in addition to archived copies. Human review waiting time is excluded. Root storage uses the existing nested ownership accountant and mandatory final audits.
- Each factor must finish exactly 32,384 starts/completions and pass complete native reconstruction before the next starts. Every saved roster must match the reviewed materialization. Failure preserves the output and terminates the study.
- Final selection uses all 1,012 cells at alpha **.05 / 1,012**, draws as non-wins. Baseline is measured but ineligible. Require all upper bounds ≤50%, at least one lower bound ≥10%, strongest observed rate in [15%,40%]; choose the rate closest to 30%, then smaller factor. Keep every cell and breach. At most **CandidateForFullFamilyConfirmation**; no eligible variant means **Unresolved**.

## Binding before execution

The [binding-request template](../TestResults/balance/tower-ceiling-controller-20260914/binding-request.template.json) intentionally has no ledger hash, history inventory, setup inventory or elapsed setup charge. It cannot launch. No new values were computed or reserved by controller preparation.

After separate authorization, use a new binding directory to audit every registered seed-history input and reserve exactly 128 values by the frozen allocator policy/master **2026091421**. Exclude all **481,219** retained reservations, including the 512 unused original v19 confirmation values, and any newly registered history. The controller verifies the externally supplied accepted sequence; it does not expose allocation.

The ledger schema is `{ version, historical, shared }`: version `tower-captured-v19-ceiling-screen-v1`, sorted distinct complete historical values and exactly 128 ordered shared values. In the binding request:

1. Bind the reviewed preparation path and manifest hash.
2. Bind the external ledger path and hash. `historyFiles` maps every audited existing history path to its SHA-256. A changed declared history blocks check/run.
3. Set `priorSetupSeconds` from measured active audit/allocation work. `setupFiles` maps every new pre-binding file, including the ledger, logs and allocation receipt, to its hash. The request's own bytes are counted automatically; exclude it from that map to avoid a self-hash. Record all new outputs; do not create unaccounted external log files.
4. Call bind once using an absent study output. It durably copies reservations before subsequent setup, copies evidence/content/executable, re-materializes all bound cells without combat, and publishes the immutable protocol and setup charge.
5. Review that concrete bound protocol and exact schedule before separately authorizing the once-only launch. A changed input or exhausted limit preserves evidence and requires a new explicit decision; never overwrite or resume it.

The exact future command arrays are in the machine protocol. Equivalent PowerShell, **not executed by this task**:

```powershell
$controller = 'TestResults/balance/tower-ceiling-controller-20260914/final-capture/executable/BalanceHarness.dll'
$binding = 'TestResults/balance/tower-ceiling-screen-binding-20260914/binding-request.json'
$study = 'TestResults/balance/tower-captured-v19-ceiling-screen-20260914'

# Separately authorized preparation, after the complete external ledger/audit is ready:
dotnet $controller tower-ceiling-bind $binding $study
dotnet $controller tower-ceiling-check $study

# Separately authorized launch after reviewing the bound protocol:
dotnet $controller tower-ceiling-run $study
dotnet $controller tower-ceiling-verify $study
```

The controller reconstructs and verifies all results before publishing completion. The last command is a read-only independent invocation of that verifier; it creates no new fights or report files. It still requires the bound producing executable and preserved preparation. Keep terminal capture within the declared setup/study accounting if it is saved to disk.

## Evidence and limits of readiness

All **70 focused tests** pass. The final producing executable repeats all **1,012 exact input/participant matches**, passes the native synthetic controller completion/interruption fixtures and refuses an unbound launch. The saved native compact archives reconstruct **all 129,536 reports**, with unchanged roster-verification code in the final build. Performance details and exact commands are in the review and evidence receipts.

The positive fresh-schedule binding and real four-factor simulation remain unexecuted. Zero-combat readiness is not a measured combat speedup or a new balance result. Complete registered-history/setup coverage is an explicit audit obligation before binding, not something hashes can infer from unregistered files. The full **43,879-entry** acceptance family and current-gameplay version remain separate work. Reliability **Fail 1/3**, adoption **Hold**, existing family **Fail / Fail** and all old sealed studies remain unchanged.
