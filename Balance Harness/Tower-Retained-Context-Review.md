# Captured-v19 UTC context policy — implemented; runtime proof stopped

Completed **14 September 2026** under the [frozen zero-combat protocol](Tower-Retained-Context-Protocol.md). The offline harness now has a separate UTC-instant confirmation identity and strict complete-family/anchor validation. **All 44 scoped tests pass.** The once-only captured runtime proof stopped after **two preparations / 11.141 seconds** on its first affected recipe. No confirmation binding or interval calculation ran. Status: **ContextPolicyImplementedProofBlocked**.

The preceding [43,879-entry audit](Tower-Retained-Family-Audit-Review.md) remains complete and unchanged. Its 47,834 origins, all 253 midpoint recipes, every one of the 90 distinct offset-group recipes and all **481,603 seed reservations**, including the original unused 512, remain preserved. **Zero fights, fresh seeds, retries or resumes** occurred. Reliability **Fail 1/3**, adoption **Hold** and earlier balance results remain unchanged.

## Implementation and identity contract

`LL/tools/BalanceHarness/TowerConfirmationContext.cs` adds `tower-confirmation-context-utc-v1`. The new context key includes the explicit policy version and UTC instant, scenario identity, schema, floor, preparation and equipment budget. It preserves production recipe hashes and every character/ordered Essence identity. Original scenario records and legacy audit/input/context hashes retain their original timestamp notation. The old audit code, current battle preparation and gameplay assemblies are unchanged.

The validation helper requires complete entry counts, unique original and new identities, valid participant hashes, every declared anchor origin exactly once and at least one anchor per required context. A collision fails; no recipe is silently merged or removed. These are pure identity/mapping operations. They do not allocate seeds, implement a combat controller or establish runtime equivalence by themselves.

`LL/tests/EssenceSystem.Tests/BalanceHarnessTowerContextTests.cs` adds **24 zero-combat cases** covering offsets including midnight crossings, a one-tick distinction, equal local clocks at different instants, context/character/Essence distinctions, overlapping anchor origins, incomplete/corrupt/ambiguous mappings and cancellation. Together with the existing **20 retained-audit tests**, the wrapper completed **44 passed / 0 failed / 0 skipped** in **21.813 seconds** including compilation. The build reported 34 existing warnings outside the new files. The isolated captured diagnostic and metadata inspector builds passed with no warnings/errors.

## Runtime check and preserved failure

The diagnostic executable used the preceding captured harness source plus the new helper and dedicated zero-combat entry. All four gameplay DLL hashes match the preceding sealed audit. Before preparation, it checked **196 frozen input bindings** and verified the preceding audit's entire sealed inventory.

The frozen workload allowed all 90 affected recipes at original +01:00 and UTC, plus six fixed offset/different-instant cases: at most 186 preparations, no combat. The command instead stopped at inventory key `777a459f22025be350b088bf30c5d34f912db6444a307b89b98968a28b41c990`, after the first two preparations. Both passed their saved participant-digest checks. The combined comparison of normalized input, plan, full combatant fields and participant digests failed. It retained **54 preceding mapping rows**, not a complete new family. **Zero completed offset pairs** are certified; the remaining 184 preparations did not run.

The failure handler retained the exception, preparation count, stage timings and partial compressed mapping. It did **not** retain the individual comparison digests, differing field paths, CPU, allocations or peak-memory measurements on failure. These are diagnostic limitations. Do not attribute the failure to a particular field or conclude that timestamp offsets change combat. The executable, frozen sources and failed output were not edited or retried. `native-failed-files.json` seals the partial output explicitly; there is no successful native `files.json` or proof receipt.

## What the captured assembly shows

The captured executor disassembly shows **five reads of `CombatEncounterPlan.StartsAt`**, each immediately assigned to `CombatResult.StartedAt` after execution. `ExecuteCoreAsync` and `ResolveRandomSeed` do not read the start time; the latter uses the explicit random seed before its fallback. The World Tower factory passes the requested start time into encounter-plan metadata. This supports UTC grouping, but is not a completed runtime/combat parity certificate.

A separately frozen **read-only IL inspection**, with no object preparation or engine invocation, exposed a defect in the broad field fingerprint:

- `PlayerEssence` initializes `AbsorbedAt` and `UpdatedAt` from the current clock. Captured `EquippedEssenceSnapshot.ToPlayerEssence` does not restore those values.
- Captured equipment-modifier rehydration assigns new GUIDs to modifier IDs.
- The diagnostic's recursive field fingerprint includes these values, so it can distinguish two preparations of identical gameplay inputs.

These are verified properties of the captured assembly, and a plausible explanation for the failed comparison. The missing per-component failure evidence prevents identifying the exact observed mismatch. The old saved participant descriptions intentionally cover a narrower state; passing them alone cannot replace the failed proof.

Before a new binding, define an explicit combat-state comparison against the captured engine's actual inputs, justify any excluded bookkeeping fields, test a same-input control, and persist each comparison and differing path **before** asserting equality. Then freeze a separate diagnostic. Do not change gameplay to make a diagnostic hash match, silently weaken this frozen check or discard any of the 90 recipes. The new UTC policy remains unbound pending that evidence.

## Measurements, commands and remaining boundary

| Check | Measured seconds | Result |
| --- | ---: | --- |
| Freeze input/source/executable bindings | 1.469 | 196 input / 157 producing-file bindings |
| Captured producing build | 2.613 | Passed, no warnings/errors |
| Once-only native command, including orchestration checks | 11.141 | Stopped, two preparations, zero fights |
| Native elapsed time at failure | 10.972 | Detailed nested timings retained |
| Input seal verification inside native command | 9.770 | Completed |
| Mapping and first offset comparison | 0.743 | Stopped after 54 saved rows |
| Captured IL extraction inside native command | 0.023 | Completed |
| Read-only metadata inspector build | 0.950 | Passed, no warnings/errors |
| Read-only captured metadata inspection | 0.047 | Completed, zero preparations |

Timings nested inside the native command are not additive. This stopped diagnostic establishes no new throughput improvement or balance result. Compilation/correctness-test time and engineering review/editing are separate from the **1,800-second diagnostic / 4-GiB output** envelope. Final accounting and preservation are recorded below.

Commands ran from the repository root:

```powershell
./build/run-tests.ps1 `
  -Filter 'FullyQualifiedName~BalanceHarnessTowerContextTests|FullyQualifiedName~BalanceHarnessTowerRetainedAuditTests' `
  -ArtifactsPath TestResults/balance/tower-retained-context-20260914/test-build

$context = 'TestResults/balance/tower-retained-context-20260914'
dotnet build "$context/compiler/CapturedContext.csproj" -c Release -o "$context/executable"
dotnet "$context/executable/BalanceHarness.dll" proof "$context/request.json" "$context/native"
# workflow.py invoked the proof exactly once and retained its exit code 1.
dotnet build "$context/inspector/Inspector.csproj" -c Release -o "$context/inspector-bin"
dotnet "$context/inspector-bin/Inspector.dll" "$context/executable" "$context/captured-snapshot-il.txt"
```

These are producing-command records, not permission to overwrite or repeat the completed/failed package. The exact requests, scripts, source/executable hashes, commands, test TRX, captured IL, failure analysis and partial archive are retained under the [new evidence package](../TestResults/balance/tower-retained-context-20260914). The original audit seal remains `d346801a04691964966f3ac780dc52f39dac432a89f5d5c6693602ff187b5884`.

The proposed 560-anchor, 2,434,784-attempt confirmation remains a conditional design only. No complete UTC cell mapping, bound anchor family, interval table, fresh schedule or executable confirmation contract was produced. No old caps, gameplay, migration, package dependency, shared configuration or deployment changed. No command is blocked by permissions; the remaining blocker is verification.

## Final preservation and accounting

The final **22.492-second** preservation/history pass checked **4,183 initial checkout files**, all **196 frozen input bindings**, **157 producing-file bindings** and the **five partial native output files**. They match their recorded identities. All **113 preceding reviews**, the original audit implementation, old protocols and sealed studies remain unchanged. The new policy class matches its captured producing copy.

The current registered seed-history paths still number **145**, with **102 distinct hashes**. Reconstructing their entire array union produced exactly the current midpoint ledger's **481,603 values**, with zero added reservations. The original unused 512-value v19 reservation remains excluded.

Ten unrelated frontend/UI-verification files changed and five UI-verification images appeared after the initial checkout snapshot. They were recorded and left intact; the whole concurrent checkout is not claimed to be byte-identical. This task adds the context helper, its tests, the frozen protocol and this review, and updates seven active Markdown handoffs. It does not modify `Program.cs`, prior audit code, battle execution or gameplay content.

The final diagnostic charge is **under 216 seconds / 3.6 minutes**, including all measured diagnostic phases and preservation, a **60-second conservative allowance** for untimed short static reads/metadata and the full **120-second final-seal allowance**. These allowances are accounting charges, not measured durations. The output charge is **under 376 MiB**, including captured/build output, the whole changed files, shared TRX and 2 MiB reserved for final metadata. Both are inside the 1,800-second / 4-GiB limits.

`final-verification.json` records exact measurements, resource charges, verification boundaries and the incomplete result. Scoped `git diff --check` and new Markdown links are checked before sealing. `updated-files.json` binds this documentation and the implementation. `evidence-files.json` seals the complete new package, including both builds, IL inspection, failure and partial mappings; `seal-timing.json` records actual sealing time. No retry or dependent binding was performed.
