# Captured-v19 ceiling screen — bound and verified, unstarted

Completed **14 September 2026** for offline `LL/tools/BalanceHarness`. Status: **PreparedVerifiedAwaitingLaunch**. Exactly **128 fresh shared seeds** are durably reserved and bound to all **1,012 cells**: four guardian factors × 253 complete recipes. **Zero fights ran**, and there were **zero retries**. The concrete experiment is ready for review before separate launch authorization.

The [authoritative ledger](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/seed-ledger.json) now contains **481,347 distinct reservations**: all 481,219 previous values plus the 128 new screen values. All **512 unused original v19 confirmation values** and all **128 unused new screen values** remain reserved, even if the screen never runs. Historical ledgers and sealed experiments remain unchanged.

## Exact bindings

| Artifact | SHA-256 |
| --- | --- |
| [Bound protocol](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/protocol.json) | `2497175ae4e323937fe561495666b1bf1eb2c1e5398bc5281bce4a570606f319` |
| [Seed ledger](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/seed-ledger.json) | `ed43c2bd3a0ccd6f9b488f6817f04a07e7c35e72eec4a59283707d1db41b3c34` |
| Producing BalanceHarness DLL | `272e861a725bd3bbfec0ccb4f3595f50b3d4b53056bca29d6c0d2d34f6c0bc32` |
| Prepared materialization manifest | `f9a82a4dd136a4bc600f8f7634cf2281d3ef51ce186ae3492b5562c509a48516` |

The existing [controller protocol](Tower-Portfolio-Ceiling-Controller-Protocol.md) and statistical design remain unchanged. All Application, Common, Domain and Services.LL DLLs retain captured-v19 hashes. Each factor has all 253 exact recipes, including 112 controls and 141 generated lineups, with the original identities, Essence order, roles and nominations preserved. Each ten-character lineup contains two five-player parties. The new schedule contains no previous reservation.

## Preparation and checks

The [history audit](../TestResults/balance/tower-ceiling-screen-binding-20260914/history-survey.json) inspected **142 registered history files / 100 distinct ledger hashes**, including nested retained copies. Their union remained exactly **481,219** values; no additional intervening reservations were found. The scan names and every consumed path/hash are retained. All registered histories are copied into the study and checked again before execution; unknown unregistered values cannot be inferred.

The [preparation protocol](../TestResults/balance/tower-ceiling-screen-binding-20260914/preparation-protocol.json) froze the allocator, script hashes, one bind command, two read-only native checks, independent verification, zero-fight limit, 30-minute preparation bound and 4 GiB preparation-output bound before allocation. An independent SHA-256 implementation first reproduced all 512 already-reserved confirmation values. The fixed master **2026091421** then selected exactly 128 fresh values in **128 proposals, zero rejections**. The existing captured controller independently validated that accepted order during binding and both native checks. No allocator command was added to the harness.

| Measurement | Result |
| --- | --- |
| Complete registered-history audit | **30.547 seconds** |
| Durable 128-value allocation and receipt | **0.172 seconds** |
| Native binding, including all 1,012 prepared-participant comparisons | **14.407 seconds** |
| First native prepared check | **6.453 seconds** |
| Independent definition, schedule, content, identity and exact-inventory verification | **1.656 seconds** |
| Final native prepared check | **6.469 seconds** |
| New fights / retries | **0 / 0** |

The final [setup and verification receipt](../TestResults/balance/tower-captured-v19-ceiling-screen-20260914/setup-charge.json) retains command arguments, exit codes, stdout/stderr, independent checks, preservation results, exact output size and remaining budgets. Setup is conservatively charged from the preparation scope timestamp, including preparation authoring/review time and the preliminary inventory. Later waiting for user launch authorization does not consume active workload time.

The controller sources and executable are unchanged from the **70-test verified build**. No backend code was edited, so that suite was not repeated; the positive binding path and both native prepared checks were executed against the exact captured executable. All required checks passed. There is no four-factor fight-throughput measurement or new balance outcome in this task.

The native commands executed from the repository root were:

```powershell
dotnet 'TestResults/balance/tower-ceiling-controller-20260914/final-capture/executable/BalanceHarness.dll' `
  tower-ceiling-bind 'TestResults/balance/tower-ceiling-screen-binding-20260914/binding-request.json' `
  'TestResults/balance/tower-captured-v19-ceiling-screen-20260914'

# Executed twice, around the independent verification.
dotnet 'TestResults/balance/tower-ceiling-controller-20260914/final-capture/executable/BalanceHarness.dll' `
  tower-ceiling-check 'TestResults/balance/tower-captured-v19-ceiling-screen-20260914'
```

These record the completed preparation; binding must not be repeated against this reserved schedule or directory. The final receipt also retains the closure audit source and exact documentation hashes. Concurrent checkout edits, including `FastCombatEngine.cs` and `CombatStyleEngineTests.cs`, were preserved and are outside this captured study. No command was blocked or left unrun within this preparation scope.

## Resource and execution boundary

The unchanged global limits are **129,536 attempted fights, 14,400 active seconds and 8 GiB total new study output**, including all preparation charges. The protocol records **13,659,649 bytes** of immutable external setup inputs; those remain charged in addition to their archived copies and the study directory. The final receipt supplies the remaining time/bytes after preparation and verification. Detailed post-binding receipts live in the controller-owned `setup-charge.json`; no extra files were added to the frozen external setup directory.

The future order remains factors **1.00, 1.04, 1.08, 1.12**, then ordinal recipe IDs, then the declared shared seed order. One durable attempt journal, deadline and storage owner span the whole study. There are no retries, replays, pruning, resumes or cap extensions. Every completed factor and every archived roster must verify before selection across all 1,012 cells.

**Launch has not been authorized or executed.** After reviewing the bound protocol and final receipt, the separately authorized command is:

```powershell
dotnet 'TestResults/balance/tower-ceiling-controller-20260914/final-capture/executable/BalanceHarness.dll' `
  tower-ceiling-run 'TestResults/balance/tower-captured-v19-ceiling-screen-20260914'
```

Do not repeat allocation or binding. Preserve the external setup directory, prepared materialization and captured executable. Before a later launch, reconcile any intervening registered-history changes; do not silently replace the bound seeds. Any saved external console log must remain within declared study accounting.

A qualifying result can identify only **CandidateForFullFamilyConfirmation**. The 43,879-entry retained inventory still needs its own complete validation protocol. Reliability **Fail 1/3**, adoption **Hold** and the previous confirmation **Fail / Fail** remain unchanged. No live gameplay/configuration, migration, catalog promotion or deployment changed. The final receipt records preserved unrelated checkout work.
