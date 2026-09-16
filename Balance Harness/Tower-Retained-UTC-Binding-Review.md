# Captured-v19 runtime v2 proof and UTC family binding — completed review

Completed **14 September 2026** under the [frozen protocol](Tower-Retained-Runtime-V2-Protocol.md). Target: offline `LL/tools/BalanceHarness`. Result **DesignBoundNoSeedsNoCombat**. All **188 preparations / 97 comparisons**, all **90 timestamp-offset recipes**, all **43,879 recipe mappings / 47,834 origins** and the fixed **560-anchor union** passed native and independent verification. **59 tests pass. Zero fights, fresh values, retries or resumes.** Reliability **Fail 1/3**, adoption **Hold**; no confirmation or gameplay change follows.

Evidence is under [the new package](../TestResults/balance/tower-retained-runtime-v2-20260914/confirmation-design.json). The protocol freezes **203 input bindings**, **158 producing files**, the independent verification script and its canonicalizer. Four gameplay DLL hashes match the captured-v19 audit, and content remains the isolated +10% midpoint. The sealed v19 experiment, prior confirmation/screen/midpoint results and all earlier failed diagnostics retain their original scope.

## What the proof establishes

The earlier v1 attempt stopped after **two preparations / one same-input comparison / 3.188 seconds**. Its durable evidence identified 90 differences in `ItemInstance.AcquiredAtUtc`; the corrected v2 passed a saved-state regression but had not run real preparation. This new run used the existing v2 comparator without changing repository C# source. The isolated driver differs from its predecessor only in its output explanation of five exclusions and build location.

V2 compares every reflected combatant instance field except exactly `PlayerEssence.AbsorbedAt`, `PlayerEssence.UpdatedAt`, `InstanceAttributeModifier.Id`, `ItemAttributeModifier.Id` and `ItemInstance.AcquiredAtUtc`. Every excluded value remains in a separate bookkeeping map. Actor, equipped-Essence and equipment IDs, mutable health/barrier, attributes, modifiers, abilities and combat counters remain compared; missing and null remain distinct. Captured rehydration/executor IL and the previous saved regression justify these narrow exclusions. No runtime was mutated to make a match.

All **90 +01:00/UTC pairs**, **two same-input controls**, **four extra offset controls** and **one different-instant control** passed. Equivalent instants preserve normalized input, plan and context hashes, all saved participant digests and every retained runtime field. The one-tick control preserves prepared actors while changing normalized input, plan and context. Each comparison saved both states, bookkeeping, component hashes and all differences before assertion, with durable flush and immediate read-back verification. Independent verification recomputed both state hashes, exact differences and flags for every record; **zero retained differences** remain. The separate bookkeeping records were checked against the five exact declaring fields.

The legacy context counts remain **43,789 + 90**. The UTC policy maps them to one context without merging any recipes. The independent pass rebuilt every production recipe/equipment/legacy identity from the original inventory, checked each saved row and all original origins, then recomputed every UTC context/cell identity. Every midpoint participant hash still matches. Anchor provenance resolves **327 historical + 253 midpoint − 20 overlaps = 560 distinct cells**, with all **580 required reasons** represented exactly once. The native validator independently accepts the complete bound family.

## Measured execution and design arithmetic

| Check | Result |
| --- | --- |
| Native whole proof command | **83.485 seconds**, 188 preparations, 97 comparisons, zero fights |
| Native measured core / CPU | **83.202 / 76.484 seconds** |
| Native peak working set | **1.104 GiB** |
| Cumulative managed allocation | **99.594 GiB**, cumulative allocation rather than retained output or peak memory |
| Independent comparisons, mappings, anchors and interval checks | **161.793 seconds** |
| Correctness wrapper | **59/59 passed**, **22.676 seconds**, 34 existing warnings, zero errors |
| Captured producing build | **3.348 seconds**, zero warnings/errors |

Detailed nested phase measurements, allocations, memory and file bindings are persisted in [native performance](../TestResults/balance/tower-retained-runtime-v2-20260914/native/performance.json); they describe this proof only. The earlier failed control is not a comparable throughput benchmark, so no speedup is inferred from its shorter duration. The previously measured storage improvements remain **622.54× incremental accounting / 4.50× sixteen-write lifecycle** at representative archive scale, with their original limitations in the [performance review](Tower-Discovery-Performance-Review.md). This proof adds no new whole-run performance claim.

The [bound design](Tower-Retained-Family-Confirmation-Design.md) retains 32 first-stage samples for each of 43,319 non-anchors and a separate 256-sample second stage for all anchors plus every unresolved cell, capped at 4,096 cells. Both stages retain alpha .025 and whole-stage multiplicity. All **290 interval rows** were independently checked: first-stage clearance is **0–1/32 wins**; at the maximum second-stage family, simultaneous ceiling/viability eligibility is **48–91/256 wins**. Maximum independent bound difference is **2.46e-12**. These are approximate Wilson intervals, not exact or lifetime adaptive guarantees. A smaller actual second stage uses its complete selected-family divisor.

The prospective ceiling is **1,386,208 + 1,048,576 = 2,434,784 attempted fights**. The proposed **288 fresh values are unallocated**. All **481,603 reservations**, including the original unused 512, remain excluded. Existing **20,000-cell / 500,000-fight** caps are unchanged. A dedicated future contract would enforce its own fixed 24-hour / 64-GiB envelope; this diagnostic does not increase any old cap.

Selection capacity remains unproven. Only 0–1 wins out of 32 clear the first stage, while the second stage can accommodate at most **3,536 unresolved non-anchors** beside its 560 fixed anchors. Exceeding that count requires **Inconclusive / SecondStageCapacityExceeded** after the complete first stage; no recipe trimming or cap increase is permitted. Verified binding and arithmetic do not establish that a future study will reach a conclusive result within these limits.

## Reproduction and remaining boundary

Run from the repository root. The exact frozen source, project, scripts and request are retained. These commands describe the completed once-only execution; **do not rerun this sealed output root**. Any repetition requires a separately scoped root, copied producing capture, new freeze and its own diagnostic charge.

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTowerRuntimeComparisonTests|FullyQualifiedName~BalanceHarnessTowerContextTests|FullyQualifiedName~BalanceHarnessTowerRetainedAuditTests' -ArtifactsPath TestResults/balance/tower-retained-runtime-v2-20260914/test-build-1
dotnet build TestResults/balance/tower-retained-runtime-v2-20260914/compiler/CapturedProof.csproj -c Release -o TestResults/balance/tower-retained-runtime-v2-20260914/executable
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' TestResults/balance/tower-retained-runtime-v2-20260914/workflow.py freeze
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' TestResults/balance/tower-retained-runtime-v2-20260914/workflow.py proof
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' TestResults/balance/tower-retained-runtime-v2-20260914/verify.py
```

The dedicated complete-family controller remains to be implemented and tested with zero-combat fixtures: exact bindings, complete history exclusions, selection/multiplicity, capacity stops, one durable attempt journal, global deadline/storage ownership, cancellation and complete archive reconstruction. Any changed execution path needs separately frozen bounded parity verification before a future launch. No proposed seeds, runnable combat controller or fresh schedule were created here. Runtime preparation parity does not certify combat replay, updated-checkout gameplay, acquisition feasibility, unsearched builds or later floors.

This task adds the frozen v2 protocol and this review, updates six active handoffs and the bound confirmation design, and retains ignored diagnostic artifacts. It changes no C# source, gameplay/content, migrations, package dependency, shared configuration, catalog or deployment. All requested execution and verification commands completed. Final preservation, exact resource charges and package seal are recorded below after closure.

## Final preservation and resource accounting

The final **65.691-second** preservation pass checked **4,203 baseline checkout files**, all **115 prior reviews**, **203 frozen inputs**, **158 producing files**, **103 native files** and all **2,577 files** in the preceding runtime-proof seal. All checked bindings and prior reviews remain unchanged. Fourteen unrelated frontend files changed and one UI-verification file appeared during this task; their paths are recorded in `preservation.json` and their work was retained. Repository C# and gameplay files match this task's baseline.

The complete registered seed history still contains **145 files / 102 distinct hashes**. Reconstructing all reserved arrays yields exactly **481,603 values**, matching the authoritative midpoint ledger, with **zero additions**. The copied test TRX records all 59 passes and matched the shared wrapper result at preservation time.

Measured diagnostic phases before final sealing total **329.728 seconds**. Adding **60 seconds** conservatively for short static reads/metadata and the full **120-second seal allowance** gives a diagnostic charge of **509.728 seconds / 8.50 minutes**, below 1,800 seconds. Compilation and correctness tests are timed separately. The two allowances are accounting charges, not measured durations.

Output, including the isolated test build, producing capture, full durable comparisons, bindings, whole changed Markdown files, shared TRX and a 2-MiB final-metadata allowance, is charged at **under 500 MiB**, below 4 GiB. The native command and independent verification were also within their separate 900-second/1-GiB and 600-second limits. No limit was reached, no requested diagnostic failed, and no workload was retried.

`final-verification.json` records exact charged seconds and bytes, build receipts and the remaining limitations. Scoped `git diff --check` and the new protocol/review links are checked before sealing. `updated-files.json` binds the final nine Markdown files, `final-documents/` preserves their exact text, and `evidence-files.json` seals the full new package. `seal-timing.json` records actual sealing time. This completes the authorized zero-combat proof and seed-free design binding; confirmation execution remains unstarted.
